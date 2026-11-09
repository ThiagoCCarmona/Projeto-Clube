using System;
using System.Collections.Generic;
using Clube.Domain.Enums;

namespace Clube.Domain.Entities;

public class Evento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubeId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoEvento TipoEvento { get; set; } = TipoEvento.Geral;
    public PublicoAlvo PublicoAlvo { get; set; } = PublicoAlvo.SomenteAssociados;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Local { get; set; } = string.Empty;
    public decimal ValorIngresso { get; set; } = 0.00m;
    public bool RequerInscricao { get; set; } = true;
    public int? LimiteParticipantes { get; set; }
    public bool Ativo { get; set; } = true;

    public Clube? Clube { get; set; }
    public ICollection<InscricaoEvento> Inscricoes { get; set; } = new List<InscricaoEvento>();
}

public class InscricaoEvento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventoId { get; set; }
    public Guid AssociadoId { get; set; }
    public DateTime DataInscricao { get; set; } = DateTime.UtcNow;
    public int QuantidadeAcompanhantes { get; set; } = 0;
    public bool Confirmado { get; set; } = true;

    public Evento? Evento { get; set; }
    public Associado? Associado { get; set; }
}
