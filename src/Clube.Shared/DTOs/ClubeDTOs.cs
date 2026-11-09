using System;

namespace Clube.Shared.DTOs;

public class ClubeDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ChavePix { get; set; } = string.Empty;
    public string TipoChavePix { get; set; } = string.Empty;
    public decimal ValorMensalidadePadrao { get; set; }
    public bool Ativo { get; set; }
    public int TotalAssociados { get; set; }
    public int AssociadosPendentes { get; set; }
}

public class CriarClubeRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ChavePix { get; set; } = string.Empty;
    public string TipoChavePix { get; set; } = "CNPJ";
    public decimal ValorMensalidadePadrao { get; set; } = 150.00m;

    // Dados do Presidente inicial do Clube
    public string NomePresidente { get; set; } = string.Empty;
    public string CpfPresidente { get; set; } = string.Empty;
    public string EmailPresidente { get; set; } = string.Empty;
    public string TelefonePresidente { get; set; } = string.Empty;
    public string SenhaPresidente { get; set; } = string.Empty;
}
