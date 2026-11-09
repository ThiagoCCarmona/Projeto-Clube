using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Shared.DTOs;

namespace Clube.Application.Services;

public interface ISolicitacaoService
{
    Task<List<SolicitacaoDto>> ListarSolicitacoesAsync(Guid? clubeId = null, StatusSolicitacao? status = null);
    Task<List<SolicitacaoDto>> ObterSolicitacoesDoAssociadoAsync(Guid associadoId);
    Task<SolicitacaoDto> CriarSolicitacaoAsync(Guid associadoId, CriarSolicitacaoRequest request);
    Task<bool> ResponderSolicitacaoAsync(Guid solicitacaoId, string resposta);
}

public class SolicitacaoService : ISolicitacaoService
{
    private readonly IClubeDbContext _context;
    private readonly ITenantContext _tenantContext;

    public SolicitacaoService(IClubeDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<SolicitacaoDto>> ListarSolicitacoesAsync(Guid? clubeId = null, StatusSolicitacao? status = null)
    {
        var query = _context.SolicitacoesReclamacoes
            .Include(s => s.Associado)
                .ThenInclude(a => a!.Usuario)
            .AsNoTracking();

        if (clubeId.HasValue && _tenantContext.IsSuperAdmin)
        {
            query = query.Where(s => s.ClubeId == clubeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var lista = await query
            .OrderByDescending(s => s.DataCriacao)
            .ToListAsync();

        return lista.Select(MapearParaDto).ToList();
    }

    public async Task<List<SolicitacaoDto>> ObterSolicitacoesDoAssociadoAsync(Guid associadoId)
    {
        var lista = await _context.SolicitacoesReclamacoes
            .Include(s => s.Associado)
                .ThenInclude(a => a!.Usuario)
            .Where(s => s.AssociadoId == associadoId)
            .OrderByDescending(s => s.DataCriacao)
            .AsNoTracking()
            .ToListAsync();

        return lista.Select(MapearParaDto).ToList();
    }

    public async Task<SolicitacaoDto> CriarSolicitacaoAsync(Guid associadoId, CriarSolicitacaoRequest request)
    {
        var associado = await _context.Associados
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == associadoId);

        var clubeId = _tenantContext.CurrentClubeId ?? (associado?.ClubeId ?? Guid.Empty);

        var solicitacao = new SolicitacaoReclamacao
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            AssociadoId = associadoId,
            Tipo = request.Tipo,
            Assunto = request.Assunto,
            Mensagem = request.Mensagem,
            Status = StatusSolicitacao.Aberta,
            DataCriacao = DateTime.UtcNow
        };

        _context.SolicitacoesReclamacoes.Add(solicitacao);
        await _context.SaveChangesAsync();

        return MapearParaDto(solicitacao);
    }

    public async Task<bool> ResponderSolicitacaoAsync(Guid solicitacaoId, string resposta)
    {
        var solicitacao = await _context.SolicitacoesReclamacoes
            .FirstOrDefaultAsync(s => s.Id == solicitacaoId);

        if (solicitacao == null) return false;

        solicitacao.Responder(resposta);
        await _context.SaveChangesAsync();
        return true;
    }

    private static SolicitacaoDto MapearParaDto(SolicitacaoReclamacao s)
    {
        return new SolicitacaoDto
        {
            Id = s.Id,
            ClubeId = s.ClubeId,
            AssociadoId = s.AssociadoId,
            NomeAssociado = s.Associado?.Usuario?.Nome ?? string.Empty,
            Tipo = s.Tipo,
            Assunto = s.Assunto,
            Mensagem = s.Mensagem,
            RespostaDiretoria = s.RespostaDiretoria,
            Status = s.Status,
            DataCriacao = s.DataCriacao,
            DataResposta = s.DataResposta
        };
    }
}
