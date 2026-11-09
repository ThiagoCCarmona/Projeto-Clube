using System;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Application.Services;
using Clube.Infrastructure.Data;
using Clube.WebAdmin.Components;
using Clube.WebAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurações de Banco de Dados (mesmo SQLite compartilhado com a API)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=app_clubes.db";

builder.Services.AddDbContext<ClubeDbContext>((sp, options) =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IClubeDbContext>(sp => sp.GetRequiredService<ClubeDbContext>());

// 2. Sessão WebAdmin e Multi-Tenant Context
builder.Services.AddScoped<WebAdminSession>();
builder.Services.AddScoped<ITenantContext, WebAdminTenantContext>();

// 3. Serviços de Aplicação
builder.Services.AddScoped<IClubeService, ClubeService>();
builder.Services.AddScoped<IAssociadoService, AssociadoService>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IMensalidadeService, MensalidadeService>();
builder.Services.AddScoped<ISolicitacaoService, SolicitacaoService>();

// 4. Blazor Server Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Executa Seed no Startup (se banco novo)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClubeDbContext>();
    await DbInitializer.SeedAsync(dbContext);
}

// Configura pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
