using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Devices;
using Clube.Mobile.Models;
using Clube.Shared.DTOs;

namespace Clube.Mobile.Services;

public class SyncResult
{
    public bool Success { get; set; }
    public bool IsOffline { get; set; }
    public int ItensEnviados { get; set; }
    public int ItensRecebidos { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public interface IMobileSyncService
{
    bool IsOnline { get; }
    event EventHandler<bool>? ConnectivityChanged;
    Task<SyncResult> SincronizarAsync();
}

public class MobileSyncService : IMobileSyncService
{
    private readonly ILocalDatabaseService _localDb;
    private readonly IApiClient _apiClient;

    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    public event EventHandler<bool>? ConnectivityChanged;

    public MobileSyncService(ILocalDatabaseService localDb, IApiClient apiClient)
    {
        _localDb = localDb;
        _apiClient = apiClient;

        Connectivity.Current.ConnectivityChanged += (s, e) =>
        {
            var online = e.NetworkAccess == NetworkAccess.Internet;
            ConnectivityChanged?.Invoke(this, online);
        };
    }

    public async Task<SyncResult> SincronizarAsync()
    {
        if (!IsOnline)
        {
            return new SyncResult
            {
                Success = false,
                IsOffline = true,
                Mensagem = "Dispositivo Offline. As alterações salvas localmente serão enviadas assim que houver conexão."
            };
        }

        var session = await _localDb.GetSessionAsync();
        if (session != null && !string.IsNullOrEmpty(session.Token))
        {
            _apiClient.SetAuthToken(session.Token);
        }

        var itensEnviados = 0;
        var itensRecebidos = 0;

        // 1. PUSH OUTBOX
        var pending = await _localDb.GetPendingOutboxAsync();
        if (pending.Any())
        {
            var pushRequest = new SyncPushRequest
            {
                AssociadoId = session?.AssociadoId ?? Guid.Empty,
                ItensOutbox = pending.Select(p => new OutboxItemDto
                {
                    Id = p.Id,
                    Acao = p.Action,
                    TipoEntidade = p.EntityType,
                    PayloadJson = p.PayloadJson,
                    CriadoEm = p.CreatedAt
                }).ToList()
            };

            var pushResp = await _apiClient.PushOutboxAsync(pushRequest);
            if (pushResp != null && pushResp.Resultados.Any())
            {
                foreach (var res in pushResp.Resultados)
                {
                    await _localDb.MarkOutboxSyncedAsync(res.OutboxId, res.Sucesso ? null : res.Mensagem);
                    if (res.Sucesso) itensEnviados++;
                }
            }
        }

        // 2. PULL DELTA
        var clubeId = session?.ClubeId;
        var pullResp = await _apiClient.PullUpdatesAsync(clubeId, session?.LastSyncTime == DateTime.MinValue ? null : session?.LastSyncTime);

        if (pullResp != null)
        {
            if (pullResp.Clube != null)
            {
                var c = pullResp.Clube;
                var localClube = new LocalClube
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Sigla = c.Sigla,
                    Endereco = c.Endereco,
                    Bairro = c.Bairro,
                    Cidade = c.Cidade,
                    UF = c.UF,
                    Telefone = c.Telefone,
                    Email = c.Email,
                    ValorMensalidadePadrao = c.ValorMensalidadePadrao,
                    ChavePix = c.ChavePix,
                    Ativo = c.Ativo
                };
                await _localDb.SaveClubesAsync(new[] { localClube });
                itensRecebidos++;
            }

            if (pullResp.Associado != null)
            {
                var a = pullResp.Associado;
                var localAssoc = new LocalAssociado
                {
                    Id = a.Id,
                    ClubeId = a.ClubeId,
                    UsuarioId = a.UsuarioId,
                    Nome = a.Nome,
                    CPF = a.CPF,
                    Email = a.Email,
                    Telefone = a.Telefone,
                    TipoAssociado = a.TipoAssociado,
                    StatusAprovacao = a.StatusAprovacao,
                    TemDireitoAVoto = a.TemDireitoAVoto,
                    Interesses = a.Interesses,
                    DataNascimento = a.DataNascimento
                };
                await _localDb.SaveAssociadoAsync(localAssoc);

                if (a.Dependentes != null && a.Dependentes.Any())
                {
                    var deps = a.Dependentes.Select(d => new LocalDependente
                    {
                        Id = d.Id,
                        AssociadoId = d.AssociadoTitularId,
                        Nome = d.Nome,
                        CPF = d.CPF,
                        Parentesco = d.GrauParentesco,
                        DataNascimento = d.DataNascimento
                    });
                    await _localDb.SaveDependentesAsync(deps);
                }
                itensRecebidos++;
            }

            if (pullResp.Espacos != null && pullResp.Espacos.Any())
            {
                var espacosLocais = pullResp.Espacos.Select(e => new LocalEspaco
                {
                    Id = e.Id,
                    ClubeId = e.ClubeId,
                    Nome = e.Nome,
                    Descricao = e.Descricao,
                    CapacidadePessoas = e.Capacidade,
                    TaxaReserva = e.ValorLocacao,
                    Ativo = e.Ativo
                });
                await _localDb.SaveEspacosAsync(espacosLocais);
                itensRecebidos += pullResp.Espacos.Count;
            }

            if (pullResp.Eventos != null && pullResp.Eventos.Any())
            {
                var eventosLocais = pullResp.Eventos.Select(ev => new LocalEvento
                {
                    Id = ev.Id,
                    ClubeId = ev.ClubeId,
                    Titulo = ev.Titulo,
                    Descricao = ev.Descricao,
                    DataHoraInicio = ev.DataInicio,
                    DataHoraFim = ev.DataFim,
                    PublicoAlvo = ev.PublicoAlvo,
                    ValorEntrada = ev.ValorIngresso,
                    LimiteParticipantes = ev.LimiteParticipantes,
                    Inscrito = ev.UsuarioInscrito
                });
                await _localDb.SaveEventosAsync(eventosLocais);
                itensRecebidos += pullResp.Eventos.Count;
            }

            if (pullResp.MinhasReservas != null && pullResp.MinhasReservas.Any())
            {
                var reservasLocais = pullResp.MinhasReservas.Select(r => new LocalReserva
                {
                    Id = r.Id,
                    ClubeId = r.ClubeId,
                    EspacoId = r.EspacoId,
                    NomeEspaco = r.NomeEspaco,
                    AssociadoId = r.AssociadoId,
                    NomeAssociado = r.NomeAssociado,
                    DataHoraInicio = r.DataHoraInicio,
                    DataHoraFim = r.DataHoraFim,
                    Status = r.Status,
                    FinalidadeUso = r.FinalidadeUso,
                    Observacoes = r.Observacoes,
                    DataSolicitacao = r.DataCriacao
                });
                await _localDb.SaveReservasAsync(reservasLocais);
                itensRecebidos += pullResp.MinhasReservas.Count;
            }

            if (pullResp.MinhasMensalidades != null && pullResp.MinhasMensalidades.Any())
            {
                var mensLocais = pullResp.MinhasMensalidades.Select(m => new LocalMensalidade
                {
                    Id = m.Id,
                    ClubeId = m.ClubeId,
                    AssociadoId = m.AssociadoId,
                    Competencia = m.Competencia,
                    Valor = m.Valor,
                    DataVencimento = m.DataVencimento,
                    DataPagamento = m.DataPagamento,
                    Status = m.Status,
                    CodigoPixCopiaECola = m.CodigoPixCopiaECola
                });
                await _localDb.SaveMensalidadesAsync(mensLocais);
                itensRecebidos += pullResp.MinhasMensalidades.Count;
            }

            if (pullResp.MinhasSolicitacoes != null && pullResp.MinhasSolicitacoes.Any())
            {
                var solLocais = pullResp.MinhasSolicitacoes.Select(s => new LocalSolicitacao
                {
                    Id = s.Id,
                    ClubeId = s.ClubeId,
                    AssociadoId = s.AssociadoId,
                    Tipo = s.Tipo,
                    Assunto = s.Assunto,
                    Mensagem = s.Mensagem,
                    Status = s.Status,
                    Resposta = s.RespostaDiretoria,
                    DataAbertura = s.DataCriacao,
                    DataFechamento = s.DataResposta
                });
                await _localDb.SaveSolicitacoesAsync(solLocais);
                itensRecebidos += pullResp.MinhasSolicitacoes.Count;
            }

            if (session != null)
            {
                session.LastSyncTime = pullResp.TimestampServidor;
                await _localDb.SaveSessionAsync(session);
            }
        }

        return new SyncResult
        {
            Success = true,
            IsOffline = false,
            ItensEnviados = itensEnviados,
            ItensRecebidos = itensRecebidos,
            Mensagem = $"Sincronização concluída com sucesso! Enviados: {itensEnviados}, Recebidos: {itensRecebidos}."
        };
    }
}
