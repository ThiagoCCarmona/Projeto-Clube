using System;
using Clube.Domain.Enums;

namespace Clube.Shared.DTOs;

public class EspacoDto
{
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Capacidade { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorLocacao { get; set; }
    public bool Ativo { get; set; }
}

public class ReservaDto
{
    public Guid Id { get; set; }
    public Guid EspacoId { get; set; }
    public string NomeEspaco { get; set; } = string.Empty;
    public Guid ClubeId { get; set; }
    public Guid AssociadoId { get; set; }
    public string NomeAssociado { get; set; } = string.Empty;
    public FinalidadeUso FinalidadeUso { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public StatusReserva Status { get; set; }
    public string Observacoes { get; set; } = string.Empty;
    public bool CriadoOffline { get; set; }
    public DateTime DataCriacao { get; set; }
    public bool TemConflito { get; set; }
}

public class CriarReservaRequest
{
    public Guid EspacoId { get; set; }
    public FinalidadeUso FinalidadeUso { get; set; } = FinalidadeUso.Festa;
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public string Observacoes { get; set; } = string.Empty;
    public bool CriadoOffline { get; set; } = false;
}
