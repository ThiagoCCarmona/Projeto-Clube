using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Application.Services;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Domain.Exceptions;
using Clube.Infrastructure.Data;
using Clube.Shared.DTOs;

namespace Clube.Application.Tests;

public class ReservaServiceTests
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
    public async Task CriarReserva_Com_Colisao_Confirmada_Deve_Lancar_Excecao()
    {
        var (context, tenant) = CriarContexto();
        var clubeId = Guid.NewGuid();
        tenant.CurrentClubeId = clubeId;

        var espacoId = Guid.NewGuid();
        var associadoId = Guid.NewGuid();

        context.Espacos.Add(new Espaco
        {
            Id = espacoId,
            ClubeId = clubeId,
            Nome = "Salão Principal",
            Capacidade = 200,
            Ativo = true
        });

        // Reserva existente confirmada para o final de semana
        context.ReservasEspacos.Add(new ReservaEspaco
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            EspacoId = espacoId,
            AssociadoId = Guid.NewGuid(),
            DataHoraInicio = new DateTime(2026, 10, 31, 8, 0, 0),
            DataHoraFim = new DateTime(2026, 11, 1, 18, 0, 0),
            Status = StatusReserva.Confirmada
        });
        await context.SaveChangesAsync();

        var service = new ReservaService(context, tenant);

        var request = new CriarReservaRequest
        {
            EspacoId = espacoId,
            DataHoraInicio = new DateTime(2026, 10, 31, 12, 0, 0),
            DataHoraFim = new DateTime(2026, 10, 31, 22, 0, 0),
            FinalidadeUso = FinalidadeUso.Churrasco
        };

        Func<Task> act = async () => await service.CriarReservaAsync(associadoId, request);

        await act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*Já existe uma reserva confirmada para este espaço no período solicitado*");
    }

    [Fact]
    public async Task CriarReserva_Valida_Deve_Gravar_Com_Status_Pendente()
    {
        var (context, tenant) = CriarContexto();
        var clubeId = Guid.NewGuid();
        tenant.CurrentClubeId = clubeId;

        var espacoId = Guid.NewGuid();
        var associadoId = Guid.NewGuid();

        context.Espacos.Add(new Espaco
        {
            Id = espacoId,
            ClubeId = clubeId,
            Nome = "Salão Principal",
            Capacidade = 200,
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new ReservaService(context, tenant);

        var request = new CriarReservaRequest
        {
            EspacoId = espacoId,
            DataHoraInicio = new DateTime(2026, 11, 15, 10, 0, 0),
            DataHoraFim = new DateTime(2026, 11, 15, 18, 0, 0),
            FinalidadeUso = FinalidadeUso.Festa,
            Observacoes = "Aniversário familiar"
        };

        var resultado = await service.CriarReservaAsync(associadoId, request);

        resultado.Should().NotBeNull();
        resultado.Status.Should().Be(StatusReserva.PendenteAprovacao);
        resultado.Observacoes.Should().Be("Aniversário familiar");
    }

    private class MockTenantContext : ITenantContext
    {
        public Guid? CurrentClubeId { get; set; }
        public bool IsSuperAdmin => !CurrentClubeId.HasValue;
    }
}
