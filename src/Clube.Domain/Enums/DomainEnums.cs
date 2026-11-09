namespace Clube.Domain.Enums;

public enum PerfilUsuario
{
    SuperAdmin = 1,
    Presidente = 2,
    Tesoureiro = 3,
    Associado = 4,
    Dependente = 5,
    Simpatizante = 6
}

public enum TipoAssociado
{
    SocioFundadorComVoto = 1,
    SocioConvidadoSemVoto = 2,
    Simpatizante = 3
}

public enum StatusAprovacao
{
    Pendente = 1,
    Aprovado = 2,
    Rejeitado = 3
}

public enum TipoEvento
{
    Geral = 0,
    AlmocoDiaDasMaes = 1,
    JantarDiaDosPais = 2,
    CafeDaTarde = 3,
    FeijoadaBeneficente = 4,
    Confraternizacao = 5,
    Outro = 99
}

public enum PublicoAlvo
{
    SomenteAssociados = 1,
    AbertoAoPublico = 2,
    Simpatizantes = 3
}

public enum FinalidadeUso
{
    Festa = 1,
    Churrasco = 2,
    Feijoada = 3,
    Confraternizacao = 4,
    Outro = 99
}

public enum StatusReserva
{
    PendenteAprovacao = 1,
    Confirmada = 2,
    Cancelada = 3,
    Rejeitada = 4,
    Conflito = 5
}

public enum StatusMensalidade
{
    Pendente = 1,
    Paga = 2,
    Atrasada = 3,
    Isenta = 4
}

public enum TipoSolicitacao
{
    Solicitacao = 1,
    Reclamacao = 2,
    Sugestao = 3
}

public enum StatusSolicitacao
{
    Aberta = 1,
    EmAnalise = 2,
    Concluida = 3
}
