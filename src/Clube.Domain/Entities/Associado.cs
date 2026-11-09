using System;
using System.Collections.Generic;
using Clube.Domain.Enums;

namespace Clube.Domain.Entities;

public class Associado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public Guid ClubeId { get; set; }
    public TipoAssociado TipoAssociado { get; set; } = TipoAssociado.SocioConvidadoSemVoto;
    public StatusAprovacao StatusAprovacao { get; set; } = StatusAprovacao.Pendente;
    public DateTime? DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string Interesses { get; set; } = string.Empty; // ex: "Pesca,Comida,Bebida"
    public string? MotivoRejeicao { get; set; }
    public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAprovacao { get; set; }

    public Usuario? Usuario { get; set; }
    public Clube? Clube { get; set; }
    public ICollection<Dependente> Dependentes { get; set; } = new List<Dependente>();
    public ICollection<ReservaEspaco> Reservas { get; set; } = new List<ReservaEspaco>();
    public ICollection<Mensalidade> Mensalidades { get; set; } = new List<Mensalidade>();
    public ICollection<SolicitacaoReclamacao> Solicitacoes { get; set; } = new List<SolicitacaoReclamacao>();
    public ICollection<InscricaoEvento> InscricoesEventos { get; set; } = new List<InscricaoEvento>();

    public bool TemDireitoAVoto()
    {
        return StatusAprovacao == StatusAprovacao.Aprovado && TipoAssociado == TipoAssociado.SocioFundadorComVoto;
    }

    public void Aprovar()
    {
        StatusAprovacao = StatusAprovacao.Aprovado;
        DataAprovacao = DateTime.UtcNow;
        MotivoRejeicao = null;
    }

    public void Rejeitar(string motivo)
    {
        StatusAprovacao = StatusAprovacao.Rejeitado;
        MotivoRejeicao = motivo;
    }
}
