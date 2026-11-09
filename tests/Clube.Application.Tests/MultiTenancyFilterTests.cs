using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Infrastructure.Data;
using Clube.Infrastructure.Security;

namespace Clube.Application.Tests;

public class MultiTenancyFilterTests
{
    [Fact]
    public async Task Deve_Filtrar_Associados_Por_Tenant_Corrente()
    {
        var options = new DbContextOptionsBuilder<ClubeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var clubeAId = Guid.NewGuid();
        var clubeBId = Guid.NewGuid();

        var tenantMock = new MockTenantContext { CurrentClubeId = null };

        // 1. Inserir dados sem filtro de tenant
        using (var dbSetup = new ClubeDbContext(options, tenantMock))
        {
            dbSetup.Clubes.Add(new Domain.Entities.Clube { Id = clubeAId, Nome = "Rotary Foz Ponte" });
            dbSetup.Clubes.Add(new Domain.Entities.Clube { Id = clubeBId, Nome = "Iate Guaíra" });

            dbSetup.Associados.Add(new Associado { Id = Guid.NewGuid(), ClubeId = clubeAId, StatusAprovacao = StatusAprovacao.Aprovado });
            dbSetup.Associados.Add(new Associado { Id = Guid.NewGuid(), ClubeId = clubeAId, StatusAprovacao = StatusAprovacao.Pendente });
            dbSetup.Associados.Add(new Associado { Id = Guid.NewGuid(), ClubeId = clubeBId, StatusAprovacao = StatusAprovacao.Aprovado });

            await dbSetup.SaveChangesAsync();
        }

        // 2. Consultar como Clube A
        var tenantClubeA = new MockTenantContext { CurrentClubeId = clubeAId };
        using (var dbClubeA = new ClubeDbContext(options, tenantClubeA))
        {
            var associadosA = await dbClubeA.Associados.ToListAsync();
            associadosA.Should().HaveCount(2);
            associadosA.All(x => x.ClubeId == clubeAId).Should().BeTrue();
        }

        // 3. Consultar como Clube B
        var tenantClubeB = new MockTenantContext { CurrentClubeId = clubeBId };
        using (var dbClubeB = new ClubeDbContext(options, tenantClubeB))
        {
            var associadosB = await dbClubeB.Associados.ToListAsync();
            associadosB.Should().HaveCount(1);
            associadosB.All(x => x.ClubeId == clubeBId).Should().BeTrue();
        }
    }

    [Fact]
    public void TokenService_Deve_Gerar_E_Validar_Claims_JWT()
    {
        var tokenService = new TokenService("chave_super_secreta_para_testes_jwt_1234567890!");
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            ClubeId = Guid.NewGuid(),
            Nome = "Carlos Kaneko",
            Email = "carlos@clube.com.br",
            CPF = "52998224725",
            Perfil = PerfilUsuario.Presidente
        };

        var token = tokenService.GerarToken(usuario);
        token.Should().NotBeNullOrWhiteSpace();

        var principal = tokenService.ValidarToken(token);
        principal.Should().NotBeNull();
        principal!.FindFirst("sub")?.Value.Should().Be(usuario.Id.ToString());
        principal.FindFirst("clubeId")?.Value.Should().Be(usuario.ClubeId.ToString());
        principal.FindFirst("role")?.Value.Should().Be(PerfilUsuario.Presidente.ToString());
    }

    private class MockTenantContext : ITenantContext
    {
        public Guid? CurrentClubeId { get; set; }
        public bool IsSuperAdmin => !CurrentClubeId.HasValue;
    }
}
