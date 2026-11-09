using System;
using Xunit;
using FluentAssertions;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Domain.Exceptions;

namespace Clube.Domain.Tests;

public class ReservaEspacoTests
{
    [Fact]
    public void Deve_Identificar_Colisao_Quando_Horarios_Se_Sobrepoem()
    {
        var reserva1 = new ReservaEspaco
        {
            Id = Guid.NewGuid(),
            EspacoId = Guid.NewGuid(),
            ClubeId = Guid.NewGuid(),
            DataHoraInicio = new DateTime(2026, 10, 31, 8, 0, 0),
            DataHoraFim = new DateTime(2026, 11, 1, 18, 0, 0),
            Status = StatusReserva.Confirmada
        };

        var reservaConcorrenteInicio = new DateTime(2026, 10, 31, 12, 0, 0);
        var reservaConcorrenteFim = new DateTime(2026, 10, 31, 20, 0, 0);

        var haConflito = reserva1.ColideCom(reservaConcorrenteInicio, reservaConcorrenteFim);

        haConflito.Should().BeTrue();
    }

    [Fact]
    public void Nao_Deve_Identificar_Colisao_Quando_Horarios_Sao_Disjuntos()
    {
        var reserva1 = new ReservaEspaco
        {
            Id = Guid.NewGuid(),
            EspacoId = Guid.NewGuid(),
            ClubeId = Guid.NewGuid(),
            DataHoraInicio = new DateTime(2026, 10, 31, 8, 0, 0),
            DataHoraFim = new DateTime(2026, 10, 31, 12, 0, 0),
            Status = StatusReserva.Confirmada
        };

        var reservaSeguinteInicio = new DateTime(2026, 10, 31, 13, 0, 0);
        var reservaSeguinteFim = new DateTime(2026, 10, 31, 18, 0, 0);

        var haConflito = reserva1.ColideCom(reservaSeguinteInicio, reservaSeguinteFim);

        haConflito.Should().BeFalse();
    }

    [Fact]
    public void Deve_Lancar_Excecao_Se_DataFinal_Menor_Ou_Igual_DataInicial()
    {
        Action act = () => new ReservaEspaco(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            FinalidadeUso.Churrasco,
            new DateTime(2026, 11, 1, 18, 0, 0),
            new DateTime(2026, 11, 1, 10, 0, 0),
            "Teste inválido");

        act.Should().Throw<DomainValidationException>()
           .WithMessage("*A data/hora final deve ser posterior à data/hora inicial*");
    }
}
