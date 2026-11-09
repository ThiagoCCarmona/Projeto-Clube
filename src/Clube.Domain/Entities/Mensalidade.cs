using System;
using Clube.Domain.Enums;

namespace Clube.Domain.Entities;

public class Mensalidade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubeId { get; set; }
    public Guid AssociadoId { get; set; }
    public string Competencia { get; set; } = string.Empty; // MM/yyyy
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public StatusMensalidade Status { get; set; } = StatusMensalidade.Pendente;
    public string CodigoPixCopiaECola { get; set; } = string.Empty;
    public string ComprovantePagamento { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;

    public Clube? Clube { get; set; }
    public Associado? Associado { get; set; }

    public void AtualizarStatusConformeVencimento(DateTime dataReferencia)
    {
        if (Status == StatusMensalidade.Pendente && dataReferencia.Date > DataVencimento.Date)
        {
            Status = StatusMensalidade.Atrasada;
        }
    }

    public void RegistrarPagamento(DateTime dataPagamento, string comprovante = "")
    {
        Status = StatusMensalidade.Paga;
        DataPagamento = dataPagamento;
        ComprovantePagamento = comprovante;
    }
}
