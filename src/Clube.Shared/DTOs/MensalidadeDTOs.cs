using System;
using Clube.Domain.Enums;

namespace Clube.Shared.DTOs;

public class MensalidadeDto
{
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public string NomeClube { get; set; } = string.Empty;
    public Guid AssociadoId { get; set; }
    public string NomeAssociado { get; set; } = string.Empty;
    public string CpfAssociado { get; set; } = string.Empty;
    public string Competencia { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public StatusMensalidade Status { get; set; }
    public string CodigoPixCopiaECola { get; set; } = string.Empty;
    public string ComprovantePagamento { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
}

public class GerarLoteMensalidadesRequest
{
    public string Competencia { get; set; } = string.Empty; // MM/yyyy
    public decimal? ValorCustomizado { get; set; }
    public DateTime DataVencimento { get; set; }
}

public class BaixarMensalidadeRequest
{
    public DateTime DataPagamento { get; set; } = DateTime.Now;
    public string Comprovante { get; set; } = string.Empty;
}
