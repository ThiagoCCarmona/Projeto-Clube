using System;
using Xunit;
using FluentAssertions;
using Clube.Domain.Entities;
using Clube.Domain.Enums;

namespace Clube.Domain.Tests;

public class AssociadoTests
{
    [Fact]
    public void SocioFundador_Deve_Ter_Direito_A_Voto()
    {
        var socio = new Associado
        {
            Id = Guid.NewGuid(),
            TipoAssociado = TipoAssociado.SocioFundadorComVoto,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        socio.TemDireitoAVoto().Should().BeTrue();
    }

    [Fact]
    public void SocioConvidado_Nao_Deve_Ter_Direito_A_Voto()
    {
        var socio = new Associado
        {
            Id = Guid.NewGuid(),
            TipoAssociado = TipoAssociado.SocioConvidadoSemVoto,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        socio.TemDireitoAVoto().Should().BeFalse();
    }

    [Fact]
    public void Simpatizante_Nao_Deve_Ter_Direito_A_Voto()
    {
        var socio = new Associado
        {
            Id = Guid.NewGuid(),
            TipoAssociado = TipoAssociado.Simpatizante,
            StatusAprovacao = StatusAprovacao.Aprovado
        };

        socio.TemDireitoAVoto().Should().BeFalse();
    }

    [Fact]
    public void Socio_Nao_Aprovado_Nao_Deve_Ter_Direito_A_Voto_Mesmo_Sendo_Fundador()
    {
        var socio = new Associado
        {
            Id = Guid.NewGuid(),
            TipoAssociado = TipoAssociado.SocioFundadorComVoto,
            StatusAprovacao = StatusAprovacao.Pendente
        };

        socio.TemDireitoAVoto().Should().BeFalse();
    }

    [Fact]
    public void Aprovar_Deve_Mudar_Status_Para_Aprovado()
    {
        var socio = new Associado
        {
            Id = Guid.NewGuid(),
            StatusAprovacao = StatusAprovacao.Pendente
        };

        socio.Aprovar();

        socio.StatusAprovacao.Should().Be(StatusAprovacao.Aprovado);
    }
}
