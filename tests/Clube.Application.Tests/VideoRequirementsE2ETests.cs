using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Clube.Application.Interfaces;
using Clube.Application.Services;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Infrastructure.Data;
using Clube.Shared.DTOs;

namespace Clube.Application.Tests;

public class VideoRequirementsE2ETests
{
    private class TestTenantContext : ITenantContext
    {
        public Guid? CurrentClubeId { get; set; }
        public bool IsSuperAdmin { get; set; } = false;
        public string? UserRole { get; set; } = "Presidente";
    }

    private ClubeDbContext CreateDbContext(string dbName, ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<ClubeDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ClubeDbContext(options, tenantContext);
    }

    [Fact]
    public async Task SocioFundador_TemDireitoAVoto_Mas_ConvidadoESimpatizante_NaoTemDireitoAVoto()
    {
        // Cenário do vídeo: "tipo de associado: sócio associado/fundador(direito a voto), sócio convidado(não tem direito a voto), simpatizante"
        var tenant = new TestTenantContext { IsSuperAdmin = true };
        using var context = CreateDbContext("Db_VotingRights_" + Guid.NewGuid(), tenant);

        var clube = new Domain.Entities.Clube
        {
            Id = Guid.NewGuid(),
            Nome = "Rotary Club Foz do Iguaçu Ponte",
            Sigla = "RCFOZ",
            Cidade = "Foz do Iguaçu",
            UF = "PR"
        };
        context.Clubes.Add(clube);

        var socioFundador = new Associado
        {
            Id = Guid.NewGuid(),
            ClubeId = clube.Id,
            TipoAssociado = TipoAssociado.SocioFundadorComVoto,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        var socioConvidado = new Associado
        {
            Id = Guid.NewGuid(),
            ClubeId = clube.Id,
            TipoAssociado = TipoAssociado.SocioConvidadoSemVoto,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        var simpatizante = new Associado
        {
            Id = Guid.NewGuid(),
            ClubeId = clube.Id,
            TipoAssociado = TipoAssociado.Simpatizante,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        context.Associados.AddRange(socioFundador, socioConvidado, simpatizante);
        await context.SaveChangesAsync();

        // Asserts
        Assert.True(socioFundador.TemDireitoAVoto(), "Sócio Fundador deve ter prerrogativa estatutária de voto.");
        Assert.False(socioConvidado.TemDireitoAVoto(), "Sócio Convidado NÃO deve ter direito a voto.");
        Assert.False(simpatizante.TemDireitoAVoto(), "Simpatizante NÃO deve ter direito a voto.");
    }

    [Fact]
    public async Task Reserva_SalaoSocial_FeijoadaBeneficente_IntervaloContinuo_31Out_a_01Nov_Deve_Gravar_Com_Sucesso()
    {
        // Cenário do vídeo: "reserva de espaço, data inicio, hora inicio, data final e horário final; feijoada beneficente 31/10 a 01/11, salão social"
        var clubeId = Guid.NewGuid();
        var tenant = new TestTenantContext { CurrentClubeId = clubeId };
        using var context = CreateDbContext("Db_ReservaFeijoada_" + Guid.NewGuid(), tenant);

        var espaco = new Espaco
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            Nome = "Salão Social Principal",
            Capacidade = 300,
            ValorLocacao = 500m,
            Ativo = true
        };
        context.Espacos.Add(espaco);

        var associado = new Associado
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            TipoAssociado = TipoAssociado.SocioFundadorComVoto,
            StatusAprovacao = StatusAprovacao.Aprovado
        };
        context.Associados.Add(associado);
        await context.SaveChangesAsync();

        var service = new ReservaService(context, tenant);

        var request = new CriarReservaRequest
        {
            EspacoId = espaco.Id,
            FinalidadeUso = FinalidadeUso.Feijoada,
            DataHoraInicio = new DateTime(2026, 10, 31, 18, 0, 0),
            DataHoraFim = new DateTime(2026, 11, 1, 2, 0, 0),
            Observacoes = "Feijoada beneficente anual"
        };

        var reservaDto = await service.CriarReservaAsync(associado.Id, request);

        Assert.NotNull(reservaDto);
        Assert.Equal(FinalidadeUso.Feijoada, reservaDto.FinalidadeUso);
        Assert.Equal(new DateTime(2026, 10, 31, 18, 0, 0), reservaDto.DataHoraInicio);
        Assert.Equal(new DateTime(2026, 11, 1, 2, 0, 0), reservaDto.DataHoraFim);
        Assert.Equal(StatusReserva.PendenteAprovacao, reservaDto.Status);
    }

    [Fact]
    public async Task Tesouraria_GerarLoteMensalidades_Deve_Emitir_PixCopiaECola_Para_Socios_Elegiveis()
    {
        // Cenário do vídeo: "mensalidade, valor mensal, situação financeira; tesoureiro web"
        var clubeId = Guid.NewGuid();
        var tenant = new TestTenantContext { CurrentClubeId = clubeId };
        using var context = CreateDbContext("Db_Tesouraria_" + Guid.NewGuid(), tenant);

        var clube = new Domain.Entities.Clube
        {
            Id = clubeId,
            Nome = "Iate Clube Guaíra",
            Sigla = "ICG",
            Cidade = "Guaíra",
            UF = "PR",
            ChavePix = "guaira@iate.com.br",
            ValorMensalidadePadrao = 220.00m
        };
        context.Clubes.Add(clube);

        var socio1 = new Associado
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            TipoAssociado = TipoAssociado.SocioFundadorComVoto,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        var socio2 = new Associado
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            TipoAssociado = TipoAssociado.SocioConvidadoSemVoto,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        var simpatizante = new Associado
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            TipoAssociado = TipoAssociado.Simpatizante,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        context.Associados.AddRange(socio1, socio2, simpatizante);
        await context.SaveChangesAsync();

        var service = new MensalidadeService(context, tenant);

        var request = new GerarLoteMensalidadesRequest
        {
            Competencia = "11/2026",
            DataVencimento = new DateTime(2026, 11, 10)
        };

        var geradas = await service.GerarLoteMensalidadesAsync(request);

        // Apenas Sócio Fundador e Sócio Convidado devem ser faturados (Simpatizante não paga mensalidade estatutária)
        Assert.Equal(2, geradas);

        var mensalidades = await context.Mensalidades.ToListAsync();
        Assert.All(mensalidades, m =>
        {
            Assert.Equal("11/2026", m.Competencia);
            Assert.Equal(220.00m, m.Valor);
            Assert.False(string.IsNullOrWhiteSpace(m.CodigoPixCopiaECola), "Deve conter código PIX gerado");
            Assert.StartsWith("000201", m.CodigoPixCopiaECola); // Padrão EMV QRCode BACEN
        });
    }

    [Fact]
    public async Task Evento_SomenteAssociados_E_AbertoAoPublico_Filtram_Conforme_Audiencia()
    {
        // Cenário do vídeo: "Eventos(Almoço dia das mães, Jantar dia dos Pais, café da tarde) para o publico que destina o evento: publico somente associado, aberto ao publico"
        var clubeId = Guid.NewGuid();
        var tenant = new TestTenantContext { CurrentClubeId = clubeId };
        using var context = CreateDbContext("Db_Eventos_" + Guid.NewGuid(), tenant);

        var clube = new Domain.Entities.Clube
        {
            Id = clubeId,
            Nome = "Rotary Club Foz Ponte",
            Sigla = "RCFOZ",
            Cidade = "Foz do Iguaçu",
            UF = "PR"
        };
        context.Clubes.Add(clube);

        var eventoExclusivo = new Evento
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            Titulo = "Jantar do Dia dos Pais",
            TipoEvento = TipoEvento.JantarDiaDosPais,
            PublicoAlvo = PublicoAlvo.SomenteAssociados,
            DataInicio = DateTime.Today.AddDays(10),
            DataFim = DateTime.Today.AddDays(10).AddHours(4),
            ValorIngresso = 80m
        };

        var eventoPublico = new Evento
        {
            Id = Guid.NewGuid(),
            ClubeId = clubeId,
            Titulo = "Almoço do Dia das Mães",
            TipoEvento = TipoEvento.AlmocoDiaDasMaes,
            PublicoAlvo = PublicoAlvo.AbertoAoPublico,
            DataInicio = DateTime.Today.AddDays(20),
            DataFim = DateTime.Today.AddDays(20).AddHours(5),
            ValorIngresso = 50m
        };

        context.Eventos.AddRange(eventoExclusivo, eventoPublico);
        await context.SaveChangesAsync();

        var service = new EventoService(context, tenant);

        var eventos = await service.ListarEventosAsync();

        Assert.Equal(2, eventos.Count);
        Assert.Contains(eventos, e => e.PublicoAlvo == PublicoAlvo.SomenteAssociados && e.TipoEvento == TipoEvento.JantarDiaDosPais);
        Assert.Contains(eventos, e => e.PublicoAlvo == PublicoAlvo.AbertoAoPublico && e.TipoEvento == TipoEvento.AlmocoDiaDasMaes);
    }
}
