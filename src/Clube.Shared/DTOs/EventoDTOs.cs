using System;
using Clube.Domain.Enums;

namespace Clube.Shared.DTOs;

public class EventoDto
{
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public string NomeClube { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoEvento TipoEvento { get; set; }
    public PublicoAlvo PublicoAlvo { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Local { get; set; } = string.Empty;
    public decimal ValorIngresso { get; set; }
    public bool RequerInscricao { get; set; }
    public int? LimiteParticipantes { get; set; }
    public int TotalConfirmados { get; set; }
    public bool UsuarioInscrito { get; set; }
}

public class CriarEventoRequest
{
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
}

public class InscricaoEventoRequest
{
    public int QuantidadeAcompanhantes { get; set; } = 0;
}
