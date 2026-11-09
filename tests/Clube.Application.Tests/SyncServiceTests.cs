using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Application.Services;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Infrastructure.Data;
using Clube.Shared.DTOs;

namespace Clube.Application.Tests;

public class SyncServiceTests
{
    private (ClubeDbContext context, MockTenantContext tenant) CriarContexto()
    {
        var options = new DbContextOptionsBuilder<ClubeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenant = new MockTenantContext();
        var context = new ClubeDbContext(options, tenant);
        return (context, tenant);
    }

    [Fact]
    public async Task SyncPush_Com_Reserva_Offline_Valida_Deve_Gravar_Com_Sucesso()
    {
        var (context, tenant) = CriarContexto();
        var clubeId = Guid.NewGuid();
        tenant.CurrentClubeId = clubeId;

        var espacoId = Guid.NewGuid();
        var associadoId = Guid.NewGuid();

        context.Associados.Add(new Associado
        {
            Id = associadoId,
            ClubeId = clubeId,
            StatusAprovacao = StatusAprovacao.Aprovado
        });
        await context.SaveChangesAsync();

        var syncService = new SyncService(context, tenant);

        var payloadReserva = new CriarReservaRequest
        {
            EspacoId = espacoId,
            DataHoraInicio = new DateTime(2026, 12, 1, 10, 0, 0),
            DataHoraFim = new DateTime(2026, 12, 1, 18, 0, 0),
            FinalidadeUso = FinalidadeUso.Confraternizacao,
            Observacoes = "Criada offline na beira do rio"
        };

        var outboxItem = new OutboxItemDto
        {
            Id = Guid.NewGuid(),
            TipoEntidade = "Reserva",
            Acao = "CREATE",
            PayloadJson = JsonSerializer.Serialize(payloadReserva),
            CriadoEm = DateTime.UtcNow
        };

        var request = new SyncPushRequest
        {
            AssociadoId = associadoId,
            ItensOutbox = new List<OutboxItemDto> { outboxItem }
        };

        var response = await syncService.ProcessarPushAsync(request);

        response.Sucesso.Should().BeTrue();
        response.Resultados.Should().HaveCount(1);
        response.Resultados[0].Sucesso.Should().BeTrue();
        response.Resultados[0].TemConflito.Should().BeFalse();

        var reservaNoBanco = await context.ReservasEspacos.FirstOrDefaultAsync();
        reservaNoBanco.Should().NotBeNull();
        reservaNoBanco!.CriadoOffline.Should().BeTrue();
    }

    private class MockTenantContext : ITenantContext
    {
        public Guid? CurrentClubeId { get; set; }
        public bool IsSuperAdmin => !CurrentClubeId.HasValue;
    }
}
