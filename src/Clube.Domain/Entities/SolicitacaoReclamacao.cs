using System;
using Clube.Domain.Enums;

namespace Clube.Domain.Entities;

public class SolicitacaoReclamacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubeId { get; set; }
    public Guid AssociadoId { get; set; }
    public TipoSolicitacao Tipo { get; set; } = TipoSolicitacao.Solicitacao;
    public string Assunto { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? RespostaDiretoria { get; set; }
    public StatusSolicitacao Status { get; set; } = StatusSolicitacao.Aberta;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataResposta { get; set; }

    public Clube? Clube { get; set; }
    public Associado? Associado { get; set; }

    public void Responder(string resposta)
    {
        RespostaDiretoria = resposta;
        Status = StatusSolicitacao.Concluida;
        DataResposta = DateTime.UtcNow;
    }
}
