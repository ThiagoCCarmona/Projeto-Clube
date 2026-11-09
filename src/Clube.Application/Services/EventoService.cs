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

public interface IEventoService
{
    Task<List<EventoDto>> ListarEventosAsync(Guid? clubeId = null, Guid? associadoId = null);
    Task<EventoDto?> ObterPorIdAsync(Guid id, Guid? associadoId = null);
    Task<EventoDto> CriarEventoAsync(CriarEventoRequest request);
    Task<bool> InscreverAsync(Guid eventoId, Guid associadoId, int acompanhantes = 0);
    Task<bool> CancelarInscricaoAsync(Guid eventoId, Guid associadoId);
}

public class EventoService : IEventoService
{
    private readonly IClubeDbContext _context;
    private readonly ITenantContext _tenantContext;

    public EventoService(IClubeDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<EventoDto>> ListarEventosAsync(Guid? clubeId = null, Guid? associadoId = null)
    {
        var query = _context.Eventos
            .Include(e => e.Clube)
            .Include(e => e.Inscricoes)
            .Where(e => e.Ativo)
            .AsNoTracking();

        if (clubeId.HasValue && _tenantContext.IsSuperAdmin)
        {
            query = query.Where(e => e.ClubeId == clubeId.Value);
        }

        var lista = await query
            .OrderBy(e => e.DataInicio)
            .ToListAsync();

        return lista.Select(e => MapearParaDto(e, associadoId)).ToList();
    }

    public async Task<EventoDto?> ObterPorIdAsync(Guid id, Guid? associadoId = null)
    {
        var e = await _context.Eventos
            .Include(x => x.Clube)
            .Include(x => x.Inscricoes)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return e != null ? MapearParaDto(e, associadoId) : null;
    }

    public async Task<EventoDto> CriarEventoAsync(CriarEventoRequest request)
    {
        if (request.DataFim <= request.DataInicio)
            throw new DomainValidationException("Data/hora de término deve ser posterior ao início do evento.");

        var clubeId = _tenantContext.CurrentClubeId;
        if (!clubeId.HasValue)
            throw new DomainValidationException("Clube não identificado no contexto.");

        var evento = new Evento
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId.Value,
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            TipoEvento = request.TipoEvento,
            PublicoAlvo = request.PublicoAlvo,
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Local = request.Local,
            ValorIngresso = request.ValorIngresso,
            RequerInscricao = request.RequerInscricao,
            LimiteParticipantes = request.LimiteParticipantes,
            Ativo = true
        };

        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();

        return (await ObterPorIdAsync(evento.Id))!;
    }

    public async Task<bool> InscreverAsync(Guid eventoId, Guid associadoId, int acompanhantes = 0)
    {
        var evento = await _context.Eventos
            .Include(e => e.Inscricoes)
            .FirstOrDefaultAsync(e => e.Id == eventoId);

        if (evento == null || !evento.Ativo) return false;

        var inscricao = evento.Inscricoes.FirstOrDefault(i => i.AssociadoId == associadoId);
        if (inscricao != null)
        {
            inscricao.Confirmado = true;
            inscricao.QuantidadeAcompanhantes = acompanhantes;
            inscricao.DataInscricao = DateTime.UtcNow;
        }
        else
        {
            _context.InscricoesEventos.Add(new InscricaoEvento
            {
                Id = Guid.NewGuid(),
                EventoId = eventoId,
                AssociadoId = associadoId,
                QuantidadeAcompanhantes = acompanhantes,
                Confirmado = true,
                DataInscricao = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelarInscricaoAsync(Guid eventoId, Guid associadoId)
    {
        var inscricao = await _context.InscricoesEventos
            .FirstOrDefaultAsync(i => i.EventoId == eventoId && i.AssociadoId == associadoId);

        if (inscricao == null) return false;

        inscricao.Confirmado = false;
        await _context.SaveChangesAsync();
        return true;
    }

    private static EventoDto MapearParaDto(Evento e, Guid? associadoId)
    {
        return new EventoDto
        {
            Id = e.Id,
            ClubeId = e.ClubeId,
            NomeClube = e.Clube?.Nome ?? string.Empty,
            Titulo = e.Titulo,
            Descricao = e.Descricao,
            TipoEvento = e.TipoEvento,
            PublicoAlvo = e.PublicoAlvo,
            DataInicio = e.DataInicio,
            DataFim = e.DataFim,
            Local = e.Local,
            ValorIngresso = e.ValorIngresso,
            RequerInscricao = e.RequerInscricao,
            LimiteParticipantes = e.LimiteParticipantes,
            TotalConfirmados = e.Inscricoes.Count(i => i.Confirmado),
            UsuarioInscrito = associadoId.HasValue && e.Inscricoes.Any(i => i.AssociadoId == associadoId.Value && i.Confirmado)
        };
    }
}
