using System;
using Clube.Domain.Enums;
using Clube.Domain.Exceptions;

namespace Clube.Domain.Entities;

public class ReservaEspaco
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EspacoId { get; set; }
    public Guid ClubeId { get; set; }
    public Guid AssociadoId { get; set; }
    public FinalidadeUso FinalidadeUso { get; set; } = FinalidadeUso.Festa;
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public StatusReserva Status { get; set; } = StatusReserva.PendenteAprovacao;
    public string Observacoes { get; set; } = string.Empty;
    public bool CriadoOffline { get; set; } = false;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? SincronizadoEm { get; set; }

    public Espaco? Espaco { get; set; }
    public Clube? Clube { get; set; }
    public Associado? Associado { get; set; }

    public ReservaEspaco() { }

    public ReservaEspaco(
        Guid espacoId,
        Guid clubeId,
        Guid associadoId,
        FinalidadeUso finalidadeUso,
        DateTime dataHoraInicio,
        DateTime dataHoraFim,
        string observacoes = "",
        bool criadoOffline = false)
    {
        if (dataHoraFim <= dataHoraInicio)
            throw new DomainValidationException("A data/hora final deve ser posterior à data/hora inicial da reserva.");

        Id = Guid.NewGuid();
        EspacoId = espacoId;
        ClubeId = clubeId;
        AssociadoId = associadoId;
        FinalidadeUso = finalidadeUso;
        DataHoraInicio = dataHoraInicio;
        DataHoraFim = dataHoraFim;
        Observacoes = observacoes;
        CriadoOffline = criadoOffline;
        Status = StatusReserva.PendenteAprovacao;
        DataCriacao = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica colisão entre intervalos de tempo contínuos [Inicio, Fim]
    /// Duas faixas colidem se e somente se: Max(Inicio1, Inicio2) < Min(Fim1, Fim2)
    /// </summary>
    public bool ColideCom(DateTime outroInicio, DateTime outroFim)
    {
        // Se a reserva foi cancelada ou rejeitada, não bloqueia o espaço
        if (Status == StatusReserva.Cancelada || Status == StatusReserva.Rejeitada)
            return false;

        return DataHoraInicio < outroFim && outroInicio < DataHoraFim;
    }
}
