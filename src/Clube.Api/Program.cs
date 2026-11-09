using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Clube.Application.Interfaces;
using Clube.Application.Services;
using Clube.Infrastructure.Data;
using Clube.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurações de Banco de Dados (SQLite por padrão)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=app_clubes.db";

builder.Services.AddDbContext<ClubeDbContext>((sp, options) =>
{
    options.UseSqlite(connectionString);
});

// Registra IClubeDbContext como o ClubeDbContext
builder.Services.AddScoped<IClubeDbContext>(sp => sp.GetRequiredService<ClubeDbContext>());

// 2. Multi-Tenant Context
builder.Services.AddScoped<ITenantContext, TenantContext>();

// 3. Serviços de Segurança e Criptografia
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? "ClubeSecretKey2026_ProjetoClubesSuperSecureTokenKey_32char!";

builder.Services.AddSingleton<ITokenService>(new TokenService(jwtSecret));

// 4. Autenticação JWT
var keyBytes = Encoding.UTF8.GetBytes(jwtSecret.PadRight(32, '!'));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Serviços de Aplicação
builder.Services.AddScoped<IClubeService, ClubeService>();
builder.Services.AddScoped<IAssociadoService, AssociadoService>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IMensalidadeService, MensalidadeService>();
builder.Services.AddScoped<ISolicitacaoService, SolicitacaoService>();
builder.Services.AddScoped<ISyncService, SyncService>();

// 6. Controllers e CORS
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 7. OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Executa Seed no Startup (se banco novo)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClubeDbContext>();
    await DbInitializer.SeedAsync(dbContext);
}

// Configurações do Pipeline HTTP
app.MapOpenApi();

app.UseCors("AllowAll");

// Middleware para resolução de Tenant a partir das claims do JWT
app.Use(async (context, next) =>
{
    var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
    var user = context.User;

    if (user.Identity != null && user.Identity.IsAuthenticated)
    {
        var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
            ?? user.FindFirst("role")?.Value;

        if (role == "SuperAdmin")
        {
            tenantContext.CurrentClubeId = null; // Acesso global
        }
        else
        {
            var clubeIdClaim = user.FindFirst("clubeId")?.Value;
            if (!string.IsNullOrEmpty(clubeIdClaim) && Guid.TryParse(clubeIdClaim, out var cId))
            {
                tenantContext.CurrentClubeId = cId;
            }
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necessário para os testes de integração WebApplicationFactory<Program>
public partial class Program { }
