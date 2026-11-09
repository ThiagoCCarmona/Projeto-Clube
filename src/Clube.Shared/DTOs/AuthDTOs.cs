using System;
using System.Collections.Generic;
using Clube.Domain.Enums;

namespace Clube.Shared.DTOs;

public class LoginRequest
{
    public string CPF { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public Guid? ClubeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public TipoAssociado? TipoAssociado { get; set; }
    public StatusAprovacao? StatusAprovacao { get; set; }
    public bool TemDireitoAVoto { get; set; }
}

public class RegistroPublicoRequest
{
    public Guid ClubeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public TipoAssociado TipoPretendido { get; set; } = TipoAssociado.SocioConvidadoSemVoto; // Convidado ou Simpatizante
    public DateTime? DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string Interesses { get; set; } = string.Empty; // tags: "Pesca,Gastronomia,Bebidas"
    public List<NovoDependenteDto> Dependentes { get; set; } = new List<NovoDependenteDto>();
}

public class NovoDependenteDto
{
    public string Nome { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GrauParentesco { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
}
