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

public interface IMensalidadeService
{
    Task<List<MensalidadeDto>> ListarMensalidadesAsync(Guid? clubeId = null, StatusMensalidade? status = null, string? competencia = null);
    Task<List<MensalidadeDto>> ObterMensalidadesDoAssociadoAsync(Guid associadoId);
    Task<int> GerarLoteMensalidadesAsync(GerarLoteMensalidadesRequest request);
    Task<bool> BaixarMensalidadeAsync(Guid mensalidadeId, BaixarMensalidadeRequest request);
    Task<ResumoFinanceiroDto> ObterResumoFinanceiroAsync(Guid? clubeId = null);
}

public class ResumoFinanceiroDto
{
    public decimal TotalArrecadado { get; set; }
    public decimal TotalPendente { get; set; }
    public decimal TotalAtrasado { get; set; }
    public int QuantidadePaga { get; set; }
    public int QuantidadePendente { get; set; }
    public int QuantidadeAtrasada { get; set; }
    public decimal TaxaAdimplenciaPercentual { get; set; }
}

public class MensalidadeService : IMensalidadeService
{
    private readonly IClubeDbContext _context;
    private readonly ITenantContext _tenantContext;

    public MensalidadeService(IClubeDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<MensalidadeDto>> ListarMensalidadesAsync(Guid? clubeId = null, StatusMensalidade? status = null, string? competencia = null)
    {
        var query = _context.Mensalidades
            .Include(m => m.Clube)
            .Include(m => m.Associado)
                .ThenInclude(a => a!.Usuario)
            .AsNoTracking();

        if (clubeId.HasValue && _tenantContext.IsSuperAdmin)
        {
            query = query.Where(m => m.ClubeId == clubeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(competencia))
        {
            query = query.Where(m => m.Competencia == competencia);
        }

        var lista = await query
            .OrderByDescending(m => m.DataVencimento)
            .ToListAsync();

        return lista.Select(MapearParaDto).ToList();
    }

    public async Task<List<MensalidadeDto>> ObterMensalidadesDoAssociadoAsync(Guid associadoId)
    {
        var lista = await _context.Mensalidades
            .Include(m => m.Clube)
            .Include(m => m.Associado)
                .ThenInclude(a => a!.Usuario)
            .Where(m => m.AssociadoId == associadoId)
            .OrderByDescending(m => m.DataVencimento)
            .AsNoTracking()
            .ToListAsync();

        return lista.Select(MapearParaDto).ToList();
    }

    public async Task<int> GerarLoteMensalidadesAsync(GerarLoteMensalidadesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Competencia))
            throw new DomainValidationException("Competência (MM/AAAA) é obrigatória.");

        var clubeId = _tenantContext.CurrentClubeId;
        if (!clubeId.HasValue)
            throw new DomainValidationException("Clube não identificado no contexto.");

        var clube = await _context.Clubes.FirstOrDefaultAsync(c => c.Id == clubeId.Value);
        if (clube == null)
            throw new DomainValidationException("Clube não encontrado.");

        var valorMensalidade = request.ValorCustomizado ?? clube.ValorMensalidadePadrao;

        // Fatura apenas Sócios Titulares Aprovados (Fundadores e Convidados)
        var associadosElegiveis = await _context.Associados
            .Where(a => a.ClubeId == clubeId.Value &&
                        a.StatusAprovacao == StatusAprovacao.Aprovado &&
                        (a.TipoAssociado == TipoAssociado.SocioFundadorComVoto || a.TipoAssociado == TipoAssociado.SocioConvidadoSemVoto))
            .ToListAsync();

        int gerados = 0;
        foreach (var socio in associadosElegiveis)
        {
            // Evita duplicidade de cobrança para a mesma competência
            var jaExiste = await _context.Mensalidades
                .AnyAsync(m => m.AssociadoId == socio.Id && m.Competencia == request.Competencia);

            if (!jaExiste)
            {
                var txId = $"MENS{socio.Id.ToString().Substring(0, 4)}{request.Competencia.Replace("/", "")}";
                var chavePix = string.IsNullOrWhiteSpace(clube.ChavePix) ? "00000000000" : clube.ChavePix;
                var pixCode = PixPayloadGenerator.GerarPayload(chavePix, clube.Nome, clube.Cidade, valorMensalidade, txId);

                var mensalidade = new Mensalidade
                {
                    Id = Guid.NewGuid(),
                    ClubeId = clubeId.Value,
                    AssociadoId = socio.Id,
                    Competencia = request.Competencia,
                    Valor = valorMensalidade,
                    DataVencimento = request.DataVencimento,
                    Status = StatusMensalidade.Pendente,
                    CodigoPixCopiaECola = pixCode,
                    DataEmissao = DateTime.UtcNow
                };

                _context.Mensalidades.Add(mensalidade);
                gerados++;
            }
        }

        await _context.SaveChangesAsync();
        return gerados;
    }

    public async Task<bool> BaixarMensalidadeAsync(Guid mensalidadeId, BaixarMensalidadeRequest request)
    {
        var mensalidade = await _context.Mensalidades.FirstOrDefaultAsync(m => m.Id == mensalidadeId);
        if (mensalidade == null) return false;

        mensalidade.RegistrarPagamento(request.DataPagamento, request.Comprovante);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ResumoFinanceiroDto> ObterResumoFinanceiroAsync(Guid? clubeId = null)
    {
        var query = _context.Mensalidades.AsNoTracking();
        if (clubeId.HasValue && _tenantContext.IsSuperAdmin)
        {
            query = query.Where(m => m.ClubeId == clubeId.Value);
        }

        var lista = await query.ToListAsync();

        // Atualiza status de atraso em memória
        var hoje = DateTime.Today;
        foreach (var m in lista)
            m.AtualizarStatusConformeVencimento(hoje);

        var pagas = lista.Where(m => m.Status == StatusMensalidade.Paga).ToList();
        var pendentes = lista.Where(m => m.Status == StatusMensalidade.Pendente).ToList();
        var atrasadas = lista.Where(m => m.Status == StatusMensalidade.Atrasada).ToList();

        var total = lista.Count;
        var adimplentes = pagas.Count;

        return new ResumoFinanceiroDto
        {
            TotalArrecadado = pagas.Sum(m => m.Valor),
            TotalPendente = pendentes.Sum(m => m.Valor),
            TotalAtrasado = atrasadas.Sum(m => m.Valor),
            QuantidadePaga = pagas.Count,
            QuantidadePendente = pendentes.Count,
            QuantidadeAtrasada = atrasadas.Count,
            TaxaAdimplenciaPercentual = total > 0 ? Math.Round((decimal)adimplentes / total * 100m, 1) : 100m
        };
    }

    private static MensalidadeDto MapearParaDto(Mensalidade m)
    {
        return new MensalidadeDto
        {
            Id = m.Id,
            ClubeId = m.ClubeId,
            NomeClube = m.Clube?.Nome ?? string.Empty,
            AssociadoId = m.AssociadoId,
            NomeAssociado = m.Associado?.Usuario?.Nome ?? string.Empty,
            CpfAssociado = m.Associado?.Usuario?.CPF ?? string.Empty,
            Competencia = m.Competencia,
            Valor = m.Valor,
            DataVencimento = m.DataVencimento,
            DataPagamento = m.DataPagamento,
            Status = m.Status,
            CodigoPixCopiaECola = m.CodigoPixCopiaECola,
            ComprovantePagamento = m.ComprovantePagamento,
            Observacoes = m.Observacoes
        };
    }
}
