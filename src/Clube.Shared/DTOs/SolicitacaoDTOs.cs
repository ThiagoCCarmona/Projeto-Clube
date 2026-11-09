using System;
using Clube.Domain.Enums;

namespace Clube.Shared.DTOs;

public class SolicitacaoDto
{
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public Guid AssociadoId { get; set; }
    public string NomeAssociado { get; set; } = string.Empty;
    public TipoSolicitacao Tipo { get; set; }
    public string Assunto { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? RespostaDiretoria { get; set; }
    public StatusSolicitacao Status { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataResposta { get; set; }
}

public class CriarSolicitacaoRequest
{
    public TipoSolicitacao Tipo { get; set; } = TipoSolicitacao.Solicitacao;
    public string Assunto { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
}

public class ResponderSolicitacaoRequest
{
    public string Resposta { get; set; } = string.Empty;
}
