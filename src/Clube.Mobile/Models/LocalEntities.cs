using System;
using SQLite;
using Clube.Domain.Enums;

namespace Clube.Mobile.Models;

[Table("Clubes")]
public class LocalClube
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Bairro { get; set; }
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public decimal ValorMensalidadePadrao { get; set; }
    public string? ChavePix { get; set; }
    public bool Ativo { get; set; } = true;
}

[Table("UserSession")]
public class LocalUserSession
{
    [PrimaryKey]
    public string Key { get; set; } = "CurrentSession";
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public Guid? ClubeId { get; set; }
    public Guid? AssociadoId { get; set; }
    public TipoAssociado? TipoAssociado { get; set; }
    public StatusAprovacao? StatusAprovacao { get; set; }
    public bool TemDireitoAVoto { get; set; }
    public string? Token { get; set; }
    public DateTime LastSyncTime { get; set; } = DateTime.MinValue;
}

[Table("Associados")]
public class LocalAssociado
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public TipoAssociado TipoAssociado { get; set; }
    public StatusAprovacao StatusAprovacao { get; set; }
    public bool TemDireitoAVoto { get; set; }
    public string? Interesses { get; set; }
    public DateTime? DataNascimento { get; set; }
}

[Table("Dependentes")]
public class LocalDependente
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid AssociadoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Parentesco { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
}

[Table("Espacos")]
public class LocalEspaco
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int CapacidadePessoas { get; set; }
    public decimal TaxaReserva { get; set; }
    public bool Ativo { get; set; } = true;
}

[Table("Reservas")]
public class LocalReserva
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public Guid EspacoId { get; set; }
    public string NomeEspaco { get; set; } = string.Empty;
    public Guid AssociadoId { get; set; }
    public string NomeAssociado { get; set; } = string.Empty;
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public StatusReserva Status { get; set; }
    public FinalidadeUso FinalidadeUso { get; set; } = FinalidadeUso.Festa;
    public string? Observacoes { get; set; }
    public DateTime DataSolicitacao { get; set; }
}

[Table("Eventos")]
public class LocalEvento
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public PublicoAlvo PublicoAlvo { get; set; } = PublicoAlvo.SomenteAssociados;
    public decimal ValorEntrada { get; set; }
    public int? LimiteParticipantes { get; set; }
    public bool Inscrito { get; set; }
}

[Table("Mensalidades")]
public class LocalMensalidade
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public Guid AssociadoId { get; set; }
    public string Competencia { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public StatusMensalidade Status { get; set; }
    public string? CodigoPixCopiaECola { get; set; }
}

[Table("Solicitacoes")]
public class LocalSolicitacao
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid ClubeId { get; set; }
    public Guid AssociadoId { get; set; }
    public TipoSolicitacao Tipo { get; set; } = TipoSolicitacao.Solicitacao;
    public string Assunto { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public StatusSolicitacao Status { get; set; } = StatusSolicitacao.Aberta;
    public string? Resposta { get; set; }
    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
}

[Table("Outbox")]
public class LocalOutboxItem
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Synced { get; set; } = false;
    public string? ErrorMessage { get; set; }
}
