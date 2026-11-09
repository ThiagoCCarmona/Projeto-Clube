using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Shared.DTOs;

namespace Clube.Application.Services;

public interface ISyncService
{
    Task<SyncPushResponse> ProcessarPushAsync(SyncPushRequest request);
    Task<SyncPullResponse> ProcessarPullAsync(Guid associadoId, DateTime? desde = null);
}

public class SyncService : ISyncService
{
    private readonly IClubeDbContext _context;
    private readonly ITenantContext _tenantContext;

    public SyncService(IClubeDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<SyncPushResponse> ProcessarPushAsync(SyncPushRequest request)
    {
        var response = new SyncPushResponse
        {
            Sucesso = true,
            SincronizadoEm = DateTime.UtcNow
        };

        var associado = await _context.Associados
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == request.AssociadoId);

        if (associado == null)
        {
            response.Sucesso = false;
            return response;
        }

        var clubeId = associado.ClubeId;

        foreach (var item in request.ItensOutbox)
        {
            var resultadoItem = new SyncPushResultItem
            {
                OutboxId = item.Id,
                Sucesso = false
            };

            try
            {
                if (item.TipoEntidade.Equals("Reserva", StringComparison.OrdinalIgnoreCase))
                {
                    var req = JsonSerializer.Deserialize<CriarReservaRequest>(item.PayloadJson);
                    if (req != null)
                    {
                        req.CriadoOffline = true;
                        
                        // Verifica colisão
                        var reservasExistentes = await _context.ReservasEspacos
                            .IgnoreQueryFilters()
                            .Where(r => r.EspacoId == req.EspacoId && r.Status != StatusReserva.Cancelada && r.Status != StatusReserva.Rejeitada)
                            .ToListAsync();

                        var temConfirmada = reservasExistentes.Any(r => 
                            r.Status == StatusReserva.Confirmada && r.ColideCom(req.DataHoraInicio, req.DataHoraFim));

                        var temPendente = reservasExistentes.Any(r => 
                            r.Status == StatusReserva.PendenteAprovacao && r.ColideCom(req.DataHoraInicio, req.DataHoraFim));

                        if (temConfirmada)
                        {
                            resultadoItem.Sucesso = false;
                            resultadoItem.TemConflito = true;
                            resultadoItem.Mensagem = "Conflito: o espaço já foi reservado e confirmado por outro associado para este horário.";
                        }
                        else
                        {
                            var novaReserva = new ReservaEspaco
                            {
                                Id = Guid.NewGuid(),
                                EspacoId = req.EspacoId,
                                ClubeId = clubeId,
                                AssociadoId = associado.Id,
                                FinalidadeUso = req.FinalidadeUso,
                                DataHoraInicio = req.DataHoraInicio,
                                DataHoraFim = req.DataHoraFim,
                                Observacoes = req.Observacoes,
                                CriadoOffline = true,
                                Status = temPendente ? StatusReserva.Conflito : StatusReserva.PendenteAprovacao,
                                DataCriacao = item.CriadoEm,
                                SincronizadoEm = DateTime.UtcNow
                            };

                            _context.ReservasEspacos.Add(novaReserva);
                            await _context.SaveChangesAsync();

                            resultadoItem.Sucesso = true;
                            resultadoItem.EntidadeIdServidor = novaReserva.Id;
                            resultadoItem.TemConflito = temPendente;
                            resultadoItem.Mensagem = temPendente 
                                ? "Reserva sincronizada, porém há concorrência com outra solicitação pendente no mesmo período." 
                                : "Reserva sincronizada com sucesso e enviada para aprovação do Presidente.";
                        }
                    }
                }
                else if (item.TipoEntidade.Equals("Solicitacao", StringComparison.OrdinalIgnoreCase))
                {
                    var req = JsonSerializer.Deserialize<CriarSolicitacaoRequest>(item.PayloadJson);
                    if (req != null)
                    {
                        var novaSolicitacao = new SolicitacaoReclamacao
                        {
                            Id = Guid.NewGuid(),
                            ClubeId = clubeId,
                            AssociadoId = associado.Id,
                            Tipo = req.Tipo,
                            Assunto = req.Assunto,
                            Mensagem = req.Mensagem,
                            Status = StatusSolicitacao.Aberta,
                            DataCriacao = item.CriadoEm
                        };

                        _context.SolicitacoesReclamacoes.Add(novaSolicitacao);
                        await _context.SaveChangesAsync();

                        resultadoItem.Sucesso = true;
                        resultadoItem.EntidadeIdServidor = novaSolicitacao.Id;
                        resultadoItem.Mensagem = "Solicitação/Reclamação sincronizada com sucesso.";
                    }
                }
            }
            catch (Exception ex)
            {
                resultadoItem.Sucesso = false;
                resultadoItem.Mensagem = $"Erro ao processar item: {ex.Message}";
            }

            response.Resultados.Add(resultadoItem);
        }

        return response;
    }

    public async Task<SyncPullResponse> ProcessarPullAsync(Guid associadoId, DateTime? desde = null)
    {
        var associado = await _context.Associados
            .IgnoreQueryFilters()
            .Include(a => a.Usuario)
            .Include(a => a.Dependentes)
                .ThenInclude(d => d.Usuario)
            .Include(a => a.Clube)
            .FirstOrDefaultAsync(a => a.Id == associadoId);

        if (associado == null)
            return new SyncPullResponse();

        var clubeId = associado.ClubeId;

        // Carrega dados do Clube
        var clubeDto = associado.Clube != null ? new ClubeDto
        {
            Id = associado.Clube.Id,
            Nome = associado.Clube.Nome,
            Sigla = associado.Clube.Sigla,
            Endereco = associado.Clube.Endereco,
            Bairro = associado.Clube.Bairro,
            Cidade = associado.Clube.Cidade,
            UF = associado.Clube.UF,
            Telefone = associado.Clube.Telefone,
            Email = associado.Clube.Email,
            ChavePix = associado.Clube.ChavePix,
            TipoChavePix = associado.Clube.TipoChavePix,
            ValorMensalidadePadrao = associado.Clube.ValorMensalidadePadrao,
            Ativo = associado.Clube.Ativo
        } : null;

        // Associado DTO
        var associadoDto = new AssociadoDto
        {
            Id = associado.Id,
            UsuarioId = associado.UsuarioId,
            ClubeId = associado.ClubeId,
            Nome = associado.Usuario?.Nome ?? string.Empty,
            CPF = associado.Usuario?.CPF ?? string.Empty,
            Email = associado.Usuario?.Email ?? string.Empty,
            Telefone = associado.Usuario?.Telefone ?? string.Empty,
            TipoAssociado = associado.TipoAssociado,
            StatusAprovacao = associado.StatusAprovacao,
            TemDireitoAVoto = associado.TemDireitoAVoto(),
            DataNascimento = associado.DataNascimento,
            Sexo = associado.Sexo,
            EstadoCivil = associado.EstadoCivil,
            Endereco = associado.Endereco,
            Bairro = associado.Bairro,
            Cidade = associado.Cidade,
            UF = associado.UF,
            Interesses = associado.Interesses,
            DataSolicitacao = associado.DataSolicitacao,
            DataAprovacao = associado.DataAprovacao,
            Dependentes = associado.Dependentes.Select(d => new DependenteDto
            {
                Id = d.Id,
                AssociadoTitularId = d.AssociadoTitularId,
                UsuarioId = d.UsuarioId,
                Nome = d.Usuario?.Nome ?? string.Empty,
                CPF = d.Usuario?.CPF ?? string.Empty,
                Email = d.Usuario?.Email ?? string.Empty,
                GrauParentesco = d.GrauParentesco,
                DataNascimento = d.DataNascimento
            }).ToList()
        };

        // Eventos ativos
        var eventos = await _context.Eventos
            .IgnoreQueryFilters()
            .Include(e => e.Inscricoes)
            .Where(e => e.ClubeId == clubeId && e.Ativo)
            .OrderBy(e => e.DataInicio)
            .ToListAsync();

        var eventosDtos = eventos.Select(e => new EventoDto
        {
            Id = e.Id,
            ClubeId = e.ClubeId,
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
            UsuarioInscrito = e.Inscricoes.Any(i => i.AssociadoId == associadoId && i.Confirmado)
        }).ToList();

        // Espaços
        var espacos = await _context.Espacos
            .IgnoreQueryFilters()
            .Where(esp => esp.ClubeId == clubeId && esp.Ativo)
            .ToListAsync();

        var espacosDtos = espacos.Select(esp => new EspacoDto
        {
            Id = esp.Id,
            ClubeId = esp.ClubeId,
            Nome = esp.Nome,
            Capacidade = esp.Capacidade,
            Descricao = esp.Descricao,
            ValorLocacao = esp.ValorLocacao,
            Ativo = esp.Ativo
        }).ToList();

        // Minhas Reservas
        var reservas = await _context.ReservasEspacos
            .IgnoreQueryFilters()
            .Include(r => r.Espaco)
            .Where(r => r.AssociadoId == associadoId)
            .OrderByDescending(r => r.DataHoraInicio)
            .ToListAsync();

        var reservasDtos = reservas.Select(r => new ReservaDto
        {
            Id = r.Id,
            EspacoId = r.EspacoId,
            NomeEspaco = r.Espaco?.Nome ?? "Espaço Social",
            ClubeId = r.ClubeId,
            AssociadoId = r.AssociadoId,
            FinalidadeUso = r.FinalidadeUso,
            DataHoraInicio = r.DataHoraInicio,
            DataHoraFim = r.DataHoraFim,
            Status = r.Status,
            Observacoes = r.Observacoes,
            CriadoOffline = r.CriadoOffline,
            DataCriacao = r.DataCriacao,
            TemConflito = r.Status == StatusReserva.Conflito
        }).ToList();

        // Minhas Mensalidades
        var mensalidades = await _context.Mensalidades
            .IgnoreQueryFilters()
            .Where(m => m.AssociadoId == associadoId)
            .OrderByDescending(m => m.DataVencimento)
            .ToListAsync();

        var mensalidadesDtos = mensalidades.Select(m => new MensalidadeDto
        {
            Id = m.Id,
            ClubeId = m.ClubeId,
            AssociadoId = m.AssociadoId,
            Competencia = m.Competencia,
            Valor = m.Valor,
            DataVencimento = m.DataVencimento,
            DataPagamento = m.DataPagamento,
            Status = m.Status,
            CodigoPixCopiaECola = m.CodigoPixCopiaECola,
            ComprovantePagamento = m.ComprovantePagamento,
            Observacoes = m.Observacoes
        }).ToList();

        // Minhas Solicitações
        var solicitacoes = await _context.SolicitacoesReclamacoes
            .IgnoreQueryFilters()
            .Where(s => s.AssociadoId == associadoId)
            .OrderByDescending(s => s.DataCriacao)
            .ToListAsync();

        var solicitacoesDtos = solicitacoes.Select(s => new SolicitacaoDto
        {
            Id = s.Id,
            ClubeId = s.ClubeId,
            AssociadoId = s.AssociadoId,
            Tipo = s.Tipo,
            Assunto = s.Assunto,
            Mensagem = s.Mensagem,
            RespostaDiretoria = s.RespostaDiretoria,
            Status = s.Status,
            DataCriacao = s.DataCriacao,
            DataResposta = s.DataResposta
        }).ToList();

        return new SyncPullResponse
        {
            TimestampServidor = DateTime.UtcNow,
            Clube = clubeDto,
            Associado = associadoDto,
            Eventos = eventosDtos,
            Espacos = espacosDtos,
            MinhasReservas = reservasDtos,
            MinhasMensalidades = mensalidadesDtos,
            MinhasSolicitacoes = solicitacoesDtos
        };
    }
}
