using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Clube.Infrastructure.Data;
using Clube.Shared.DTOs;
using System.Collections.Generic;

namespace Clube.Api.IntegrationTests;

public class ReservasMultiTenantTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;

    public ReservasMultiTenantTests(WebApplicationFactory<Program> factory)
    {
        _dbName = $"test_reservas_{Guid.NewGuid():N}.db";

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
    public async Task ObterReservas_Sem_Autenticacao_Deve_Retornar_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/reservas");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ObterReservas_Autenticado_Como_Presidente_Foz_Deve_Retornar_200_Com_Reservas()
    {
        var client = _factory.CreateClient();

        // 1. Faz login como Presidente de Foz
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            CPF = "52998224725", // CPF do Carlos Kaneko
            Senha = "admin123"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        authData.Should().NotBeNull();

        // 2. Chama API de reservas com o Token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        var reservasResponse = await client.GetAsync("/api/reservas");

        reservasResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reservas = await reservasResponse.Content.ReadFromJsonAsync<List<ReservaDto>>();
        reservas.Should().NotBeNull();
        reservas.Should().NotBeEmpty();
        reservas!.Should().Contain(r => r.FinalidadeUso == Domain.Enums.FinalidadeUso.Feijoada);
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
