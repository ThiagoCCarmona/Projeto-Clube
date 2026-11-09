using System;
using System.Collections.Generic;

namespace Clube.Domain.Entities;

public class Clube
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public bool Ativo { get; set; } = true;
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<Associado> Associados { get; set; } = new List<Associado>();
    public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
    public ICollection<Espaco> Espacos { get; set; } = new List<Espaco>();
    public ICollection<Mensalidade> Mensalidades { get; set; } = new List<Mensalidade>();
}
