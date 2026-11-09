using System;
using System.Collections.Generic;
using Clube.Domain.Enums;

namespace Clube.Shared.DTOs;

public class AssociadoDto
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid ClubeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public TipoAssociado TipoAssociado { get; set; }
    public StatusAprovacao StatusAprovacao { get; set; }
    public bool TemDireitoAVoto { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string Interesses { get; set; } = string.Empty;
    public string? MotivoRejeicao { get; set; }
    public DateTime DataSolicitacao { get; set; }
    public DateTime? DataAprovacao { get; set; }
    public List<DependenteDto> Dependentes { get; set; } = new List<DependenteDto>();
}

public class DependenteDto
{
    public Guid Id { get; set; }
    public Guid AssociadoTitularId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GrauParentesco { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
}

public class AprovarAssociadoRequest
{
    public TipoAssociado TipoAssociadoDefinido { get; set; } = TipoAssociado.SocioConvidadoSemVoto;
}

public class RejeitarAssociadoRequest
{
    public string Motivo { get; set; } = string.Empty;
}
