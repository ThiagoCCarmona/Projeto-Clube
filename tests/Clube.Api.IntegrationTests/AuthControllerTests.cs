using System;
using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Clube.Infrastructure.Data;
using Clube.Shared.DTOs;

namespace Clube.Api.IntegrationTests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _dbName = $"test_auth_{Guid.NewGuid():N}.db";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ClubeDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<ClubeDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={_dbName}");
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ClubeDbContext>();
                DbInitializer.SeedAsync(db).GetAwaiter().GetResult();
            });
        });
    }

    [Fact]
    public async Task Login_Com_Credenciais_Validas_Deve_Retornar_Token_E_200()
    {
        var client = _factory.CreateClient();

        var request = new LoginRequest
        {
            CPF = "00000000000",
            Senha = "admin123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Sucesso.Should().BeTrue();
        content.Token.Should().NotBeNullOrWhiteSpace();
        content.Nome.Should().Be("Super Administrador");
    }

    [Fact]
    public async Task Login_Com_Senha_Invalida_Deve_Retornar_401()
    {
        var client = _factory.CreateClient();

        var request = new LoginRequest
        {
            CPF = "00000000000",
            Senha = "senha_errada"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_dbName))
                File.Delete(_dbName);
        }
        catch { }
    }
}
