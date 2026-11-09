using System;
using System.Collections.Generic;

namespace Clube.Shared.DTOs;

public class OutboxItemDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TipoEntidade { get; set; } = string.Empty; // "Reserva", "Solicitacao", "Perfil"
    public string Acao { get; set; } = string.Empty; // "CREATE", "UPDATE", "CANCEL"
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class SyncPushRequest
{
    public Guid AssociadoId { get; set; }
    public List<OutboxItemDto> ItensOutbox { get; set; } = new List<OutboxItemDto>();
}

public class SyncPushResultItem
{
    public Guid OutboxId { get; set; }
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public Guid? EntidadeIdServidor { get; set; }
    public bool TemConflito { get; set; }
}

public class SyncPushResponse
{
    public bool Sucesso { get; set; }
    public List<SyncPushResultItem> Resultados { get; set; } = new List<SyncPushResultItem>();
    public DateTime SincronizadoEm { get; set; } = DateTime.UtcNow;
}

public class SyncPullResponse
{
    public DateTime TimestampServidor { get; set; } = DateTime.UtcNow;
    public ClubeDto? Clube { get; set; }
    public AssociadoDto? Associado { get; set; }
    public List<EventoDto> Eventos { get; set; } = new List<EventoDto>();
    public List<EspacoDto> Espacos { get; set; } = new List<EspacoDto>();
    public List<ReservaDto> MinhasReservas { get; set; } = new List<ReservaDto>();
    public List<MensalidadeDto> MinhasMensalidades { get; set; } = new List<MensalidadeDto>();
    public List<SolicitacaoDto> MinhasSolicitacoes { get; set; } = new List<SolicitacaoDto>();
}
