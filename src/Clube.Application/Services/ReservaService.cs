using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Domain.Exceptions;
using Clube.Shared.DTOs;

namespace Clube.Application.Services;

public interface IReservaService
{
    Task<List<ReservaDto>> ObterReservasPorClubeAsync(Guid? clubeId = null);
    Task<List<ReservaDto>> ObterReservasDoAssociadoAsync(Guid associadoId);
    Task<ReservaDto> CriarReservaAsync(Guid associadoId, CriarReservaRequest request);
    Task<bool> AprovarReservaAsync(Guid reservaId);
    Task<bool> RejeitarReservaAsync(Guid reservaId, string motivo = "");
    Task<bool> CancelarReservaAsync(Guid reservaId, Guid associadoId);
}

public class ReservaService : IReservaService
{
    private readonly IClubeDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ReservaService(IClubeDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<ReservaDto>> ObterReservasPorClubeAsync(Guid? clubeId = null)
    {
        var query = _context.ReservasEspacos
            .Include(r => r.Espaco)
            .Include(r => r.Associado)
                .ThenInclude(a => a!.Usuario)
            .AsNoTracking();

        if (clubeId.HasValue && _tenantContext.IsSuperAdmin)
        {
            query = query.Where(r => r.ClubeId == clubeId.Value);
        }

        var reservas = await query
            .OrderByDescending(r => r.DataHoraInicio)
            .ToListAsync();

        return reservas.Select(MapearParaDto).ToList();
    }

    public async Task<List<ReservaDto>> ObterReservasDoAssociadoAsync(Guid associadoId)
    {
        var reservas = await _context.ReservasEspacos
            .Include(r => r.Espaco)
            .Include(r => r.Associado)
                .ThenInclude(a => a!.Usuario)
            .Where(r => r.AssociadoId == associadoId)
            .OrderByDescending(r => r.DataHoraInicio)
            .AsNoTracking()
            .ToListAsync();

        return reservas.Select(MapearParaDto).ToList();
    }

    public async Task<ReservaDto> CriarReservaAsync(Guid associadoId, CriarReservaRequest request)
    {
        if (request.DataHoraFim <= request.DataHoraInicio)
            throw new DomainValidationException("A data/hora final deve ser posterior à data/hora inicial da reserva.");

        var espaco = await _context.Espacos
            .FirstOrDefaultAsync(e => e.Id == request.EspacoId);

        if (espaco == null)
            throw new DomainValidationException("Espaço solicitado não encontrado.");

        var clubeId = _tenantContext.CurrentClubeId ?? espaco.ClubeId;

        // 1. Verifica se já existe reserva CONFIRMADA para o mesmo espaço e horário
        var reservasExistentes = await _context.ReservasEspacos
            .Where(r => r.EspacoId == request.EspacoId && r.Status != StatusReserva.Cancelada && r.Status != StatusReserva.Rejeitada)
            .ToListAsync();

        var temConfirmadaConflitante = reservasExistentes.Any(r => 
            r.Status == StatusReserva.Confirmada && r.ColideCom(request.DataHoraInicio, request.DataHoraFim));

        if (temConfirmadaConflitante)
            throw new DomainValidationException("Já existe uma reserva confirmada para este espaço no período solicitado.");

        // 2. Verifica se há colisão com reservas ainda Pendentes (concorrência)
        var temPendenteConflitante = reservasExistentes.Any(r => 
            r.Status == StatusReserva.PendenteAprovacao && r.ColideCom(request.DataHoraInicio, request.DataHoraFim));

        var reserva = new ReservaEspaco
        {
            Id = Guid.NewGuid(),
            EspacoId = request.EspacoId,
            ClubeId = clubeId,
            AssociadoId = associadoId,
            FinalidadeUso = request.FinalidadeUso,
            DataHoraInicio = request.DataHoraInicio,
            DataHoraFim = request.DataHoraFim,
            Observacoes = request.Observacoes,
            CriadoOffline = request.CriadoOffline,
            Status = temPendenteConflitante ? StatusReserva.Conflito : StatusReserva.PendenteAprovacao,
            DataCriacao = DateTime.UtcNow
        };

        _context.ReservasEspacos.Add(reserva);
        await _context.SaveChangesAsync();

        return MapearParaDto(reserva);
    }

    public async Task<bool> AprovarReservaAsync(Guid reservaId)
    {
        var reserva = await _context.ReservasEspacos
            .FirstOrDefaultAsync(r => r.Id == reservaId);

        if (reserva == null)
            return false;

        reserva.Status = StatusReserva.Confirmada;

        // Rejeita automaticamente qualquer reserva pendente ou com conflito no mesmo horário
        var conflitantes = await _context.ReservasEspacos
            .Where(r => r.EspacoId == reserva.EspacoId && r.Id != reserva.Id && (r.Status == StatusReserva.PendenteAprovacao || r.Status == StatusReserva.Conflito))
            .ToListAsync();

        foreach (var c in conflitantes)
        {
            if (c.ColideCom(reserva.DataHoraInicio, reserva.DataHoraFim))
            {
                c.Status = StatusReserva.Rejeitada;
                c.Observacoes += " [Recusada automaticamente devido à homologação de outra reserva no mesmo horário]";
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejeitarReservaAsync(Guid reservaId, string motivo = "")
    {
        var reserva = await _context.ReservasEspacos.FirstOrDefaultAsync(r => r.Id == reservaId);
        if (reserva == null) return false;

        reserva.Status = StatusReserva.Rejeitada;
        if (!string.IsNullOrWhiteSpace(motivo))
            reserva.Observacoes += $" [Motivo: {motivo}]";

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelarReservaAsync(Guid reservaId, Guid associadoId)
    {
        var reserva = await _context.ReservasEspacos
            .FirstOrDefaultAsync(r => r.Id == reservaId && r.AssociadoId == associadoId);

        if (reserva == null) return false;

        reserva.Status = StatusReserva.Cancelada;
        await _context.SaveChangesAsync();
        return true;
    }

    private static ReservaDto MapearParaDto(ReservaEspaco r)
    {
        return new ReservaDto
        {
            Id = r.Id,
            EspacoId = r.EspacoId,
            NomeEspaco = r.Espaco?.Nome ?? "Espaço Social",
            ClubeId = r.ClubeId,
            AssociadoId = r.AssociadoId,
            NomeAssociado = r.Associado?.Usuario?.Nome ?? "Associado",
            FinalidadeUso = r.FinalidadeUso,
            DataHoraInicio = r.DataHoraInicio,
            DataHoraFim = r.DataHoraFim,
            Status = r.Status,
            Observacoes = r.Observacoes,
            CriadoOffline = r.CriadoOffline,
            DataCriacao = r.DataCriacao,
            TemConflito = r.Status == StatusReserva.Conflito
        };
    }
}
