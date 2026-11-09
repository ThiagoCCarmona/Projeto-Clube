using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Infrastructure.Security;

namespace Clube.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ClubeDbContext context)
    {
        // Garante criação do banco SQLite
        await context.Database.EnsureCreatedAsync();

        // Se já possui dados, retorna
        if (await context.Clubes.IgnoreQueryFilters().AnyAsync())
            return;

        // 1. Super Admin Global
        var superAdmin = new Usuario
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ClubeId = null,
            Nome = "Super Administrador",
            CPF = "00000000000",
            Email = "superadmin@projetoclube.com.br",
            Telefone = "45999990000",
            SenhaHash = PasswordHasher.Hash("admin123"),
            Perfil = PerfilUsuario.SuperAdmin,
            Ativo = true
        };
        context.Usuarios.Add(superAdmin);

        // 2. Clube 1: Rotary Club Foz Ponte (Foz do Iguaçu - PR)
        var clubeFoz = new Domain.Entities.Clube
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Nome = "Rotary Club Foz Ponte da Amizade",
            Sigla = "RFP",
            Endereco = "Av. das Cataratas, 1500",
            Bairro = "Vila Yolanda",
            Cidade = "Foz do Iguaçu",
            UF = "PR",
            Telefone = "4535221010",
            Email = "contato@rotaryfozponte.com.br",
            ChavePix = "45999990001",
            TipoChavePix = "Telefone",
            ValorMensalidadePadrao = 150.00m,
            Ativo = true
        };
        context.Clubes.Add(clubeFoz);

        // 3. Clube 2: Iate Clube Guaíra (Guaíra - PR)
        var clubeGuaira = new Domain.Entities.Clube
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Nome = "Iate Clube Guaíra",
            Sigla = "ICG",
            Endereco = "Rua Beira Rio, 100",
            Bairro = "Centro Náutico",
            Cidade = "Guaíra",
            UF = "PR",
            Telefone = "4436421200",
            Email = "contato@iateclubeguaira.com.br",
            ChavePix = "iate.guaira@pix.com.br",
            TipoChavePix = "Email",
            ValorMensalidadePadrao = 180.00m,
            Ativo = true
        };
        context.Clubes.Add(clubeGuaira);

        // 4. Usuários de Foz
        // Presidente (Carlos Kaneko)
        var presFoz = new Usuario
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ClubeId = clubeFoz.Id,
            Nome = "Carlos Massami Kaneko (Presidente)",
            CPF = "52998224725",
            Email = "presidente.foz@clube.com.br",
            Telefone = "45999991111",
            SenhaHash = PasswordHasher.Hash("admin123"),
            Perfil = PerfilUsuario.Presidente,
            Ativo = true
        };
        context.Usuarios.Add(presFoz);

        // Tesoureiro
        var tesFoz = new Usuario
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            ClubeId = clubeFoz.Id,
            Nome = "Marcos Silveira (Tesoureiro)",
            CPF = "11144477735",
            Email = "tesoureiro.foz@clube.com.br",
            Telefone = "45999992222",
            SenhaHash = PasswordHasher.Hash("admin123"),
            Perfil = PerfilUsuario.Tesoureiro,
            Ativo = true
        };
        context.Usuarios.Add(tesFoz);

        // Sócio Fundador (Thiago Carmona)
        var socioFundadorUser = new Usuario
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            ClubeId = clubeFoz.Id,
            Nome = "Thiago Carmona (Sócio Fundador)",
            CPF = "99988877766",
            Email = "socio.fundador@clube.com.br",
            Telefone = "45999993333",
            SenhaHash = PasswordHasher.Hash("admin123"),
            Perfil = PerfilUsuario.Associado,
            Ativo = true
        };
        context.Usuarios.Add(socioFundadorUser);

        var socioFundador = new Associado
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            UsuarioId = socioFundadorUser.Id,
            ClubeId = clubeFoz.Id,
            TipoAssociado = TipoAssociado.SocioFundadorComVoto,
            StatusAprovacao = StatusAprovacao.Aprovado,
            DataNascimento = new DateTime(1985, 5, 20),
            Sexo = "Masculino",
            EstadoCivil = "Casado",
            Endereco = "Rua Santos Dumont, 500",
            Bairro = "Centro",
            Cidade = "Foz do Iguaçu",
            UF = "PR",
            Interesses = "Pesca,Gastronomia,Bebidas",
            DataSolicitacao = DateTime.UtcNow.AddMonths(-12),
            DataAprovacao = DateTime.UtcNow.AddMonths(-11)
        };
        context.Associados.Add(socioFundador);

        // Dependente do Sócio Fundador
        var depUser = new Usuario
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            ClubeId = clubeFoz.Id,
            Nome = "Ana Carmona (Dependente)",
            CPF = "88877766655",
            Email = "dependente@clube.com.br",
            Telefone = "45999994444",
            SenhaHash = PasswordHasher.Hash("admin123"),
            Perfil = PerfilUsuario.Dependente,
            Ativo = true
        };
        context.Usuarios.Add(depUser);

        var dep = new Dependente
        {
            Id = Guid.NewGuid(),
            AssociadoTitularId = socioFundador.Id,
            UsuarioId = depUser.Id,
            GrauParentesco = "Cônjuge",
            DataNascimento = new DateTime(1988, 8, 15)
        };
        context.Dependentes.Add(dep);

        // Sócio Convidado (Pendente de análise para demonstrar aprovação do Presidente)
        var socioConvidadoUser = new Usuario
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            ClubeId = clubeFoz.Id,
            Nome = "Mateus Convidado",
            CPF = "55544433322",
            Email = "socio.convidado@clube.com.br",
            Telefone = "45999995555",
            SenhaHash = PasswordHasher.Hash("admin123"),
            Perfil = PerfilUsuario.Associado,
            Ativo = true
        };
        context.Usuarios.Add(socioConvidadoUser);

        var socioConvidado = new Associado
        {
            Id = Guid.NewGuid(),
            UsuarioId = socioConvidadoUser.Id,
            ClubeId = clubeFoz.Id,
            TipoAssociado = TipoAssociado.SocioConvidadoSemVoto,
            StatusAprovacao = StatusAprovacao.Pendente,
            DataNascimento = new DateTime(1992, 10, 10),
            Sexo = "Masculino",
            EstadoCivil = "Solteiro",
            Endereco = "Av. Brasil, 800",
            Bairro = "Centro",
            Cidade = "Foz do Iguaçu",
            UF = "PR",
            Interesses = "Comida,Eventos,Churrasco",
            DataSolicitacao = DateTime.UtcNow.AddDays(-2)
        };
        context.Associados.Add(socioConvidado);

        // Simpatizante
        var simpatizanteUser = new Usuario
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeFoz.Id,
            Nome = "Lucas Simpatizante",
            CPF = "33322211100",
            Email = "simpatizante@clube.com.br",
            Telefone = "45999996666",
            SenhaHash = PasswordHasher.Hash("admin123"),
            Perfil = PerfilUsuario.Simpatizante,
            Ativo = true
        };
        context.Usuarios.Add(simpatizanteUser);

        var simpatizante = new Associado
        {
            Id = Guid.NewGuid(),
            UsuarioId = simpatizanteUser.Id,
            ClubeId = clubeFoz.Id,
            TipoAssociado = TipoAssociado.Simpatizante,
            StatusAprovacao = StatusAprovacao.Aprovado,
            DataNascimento = new DateTime(1995, 3, 25),
            Sexo = "Masculino",
            EstadoCivil = "Solteiro",
            Endereco = "Rua Almirante Barroso, 300",
            Bairro = "Maracanã",
            Cidade = "Foz do Iguaçu",
            UF = "PR",
            Interesses = "Feijoada,Ações Beneficentes",
            DataSolicitacao = DateTime.UtcNow.AddDays(-10),
            DataAprovacao = DateTime.UtcNow.AddDays(-9)
        };
        context.Associados.Add(simpatizante);

        // 5. Espaço e Reservas (Salão Social)
        var salaoFoz = new Espaco
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ClubeId = clubeFoz.Id,
            Nome = "Salão Social Principal",
            Capacidade = 300,
            Descricao = "Salão climatizado com cozinha industrial completa, churrasqueira, mesas, cadeiras e som ambiente para festas e feijoadas.",
            ValorLocacao = 300.00m,
            Ativo = true
        };
        context.Espacos.Add(salaoFoz);

        // Reserva já confirmada (exemplo citado no vídeo: 31/10 até 01/11 Feijoada Beneficente)
        var reservaConfirmada = new ReservaEspaco
        {
            Id = Guid.NewGuid(),
            EspacoId = salaoFoz.Id,
            ClubeId = clubeFoz.Id,
            AssociadoId = socioFundador.Id,
            FinalidadeUso = FinalidadeUso.Feijoada,
            DataHoraInicio = new DateTime(2026, 10, 31, 8, 0, 0),
            DataHoraFim = new DateTime(2026, 11, 1, 20, 0, 0),
            Status = StatusReserva.Confirmada,
            Observacoes = "Preparativos na véspera e almoço festivo beneficente com entrega no domingo.",
            CriadoOffline = false,
            DataCriacao = DateTime.UtcNow.AddDays(-15)
        };
        context.ReservasEspacos.Add(reservaConfirmada);

        // 6. Eventos citados no vídeo
        var eventoAlmoco = new Evento
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeFoz.Id,
            Titulo = "Almoço Especial de Dia das Mães",
            Descricao = "Confraternização gastronômica exclusiva para famílias associadas com música ao vivo e homenagem.",
            TipoEvento = TipoEvento.AlmocoDiaDasMaes,
            PublicoAlvo = PublicoAlvo.SomenteAssociados,
            DataInicio = new DateTime(2026, 5, 10, 12, 0, 0),
            DataFim = new DateTime(2026, 5, 10, 16, 0, 0),
            Local = "Salão Social Principal",
            ValorIngresso = 50.00m,
            RequerInscricao = true,
            LimiteParticipantes = 200,
            Ativo = true
        };
        context.Eventos.Add(eventoAlmoco);

        var eventoFeijoada = new Evento
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeFoz.Id,
            Titulo = "Grande Feijoada Beneficente Comunitária",
            Descricao = "Evento aberto à comunidade e simpatizantes para arrecadação de fundos em prol de projetos sociais locais.",
            TipoEvento = TipoEvento.FeijoadaBeneficente,
            PublicoAlvo = PublicoAlvo.AbertoAoPublico,
            DataInicio = new DateTime(2026, 11, 1, 11, 30, 0),
            DataFim = new DateTime(2026, 11, 1, 15, 30, 0),
            Local = "Salão Social Principal e Quiosques",
            ValorIngresso = 60.00m,
            RequerInscricao = true,
            LimiteParticipantes = 400,
            Ativo = true
        };
        context.Eventos.Add(eventoFeijoada);

        // 7. Mensalidades modelo
        var mensPaga = new Mensalidade
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeFoz.Id,
            AssociadoId = socioFundador.Id,
            Competencia = "10/2026",
            Valor = 150.00m,
            DataVencimento = new DateTime(2026, 10, 10),
            DataPagamento = new DateTime(2026, 10, 8),
            Status = StatusMensalidade.Paga,
            CodigoPixCopiaECola = "00020126580014BR.GOV.BCB.PIX0111459999900015204000053039865406150.005802BR5925ROTARY CLUB FOZ PONTE6014FOZ DO IGUACU62070503***6304ABCD",
            ComprovantePagamento = "PIX-E2E-202610081010-CONFIRMADO",
            Observacoes = "Pago via PIX no prazo",
            DataEmissao = new DateTime(2026, 10, 1)
        };
        context.Mensalidades.Add(mensPaga);

        var mensPendente = new Mensalidade
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeFoz.Id,
            AssociadoId = socioFundador.Id,
            Competencia = "11/2026",
            Valor = 150.00m,
            DataVencimento = new DateTime(2026, 11, 15),
            Status = StatusMensalidade.Pendente,
            CodigoPixCopiaECola = "00020126580014BR.GOV.BCB.PIX0111459999900015204000053039865406150.005802BR5925ROTARY CLUB FOZ PONTE6014FOZ DO IGUACU62070503***6304EF12",
            Observacoes = "Aguardando pagamento",
            DataEmissao = new DateTime(2026, 11, 1)
        };
        context.Mensalidades.Add(mensPendente);

        // 8. Solicitação / Reclamação SAC modelo
        var solicitacao = new SolicitacaoReclamacao
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeFoz.Id,
            AssociadoId = socioFundador.Id,
            Tipo = TipoSolicitacao.Solicitacao,
            Assunto = "Sugestão de Oficina de Pesca Esportiva",
            Mensagem = "Gostaria de sugerir a realização de uma oficina prática de pesca esportiva e preservação ambiental para os dependentes jovens do clube.",
            Status = StatusSolicitacao.Aberta,
            DataCriacao = DateTime.UtcNow.AddDays(-3)
        };
        context.SolicitacoesReclamacoes.Add(solicitacao);

        await context.SaveChangesAsync();
    }
}
