using System;
using Xunit;
using FluentAssertions;
using Clube.Domain.Entities;
using Clube.Domain.Enums;

namespace Clube.Domain.Tests;

public class MensalidadeTests
{
    [Fact]
    public void Mensalidade_Vencida_Nao_Paga_Deve_Estar_Atrasada()
    {
        var mensalidade = new Mensalidade
        {
            Id = Guid.NewGuid(),
            Valor = 150.00m,
            DataVencimento = DateTime.Today.AddDays(-5),
            Status = StatusMensalidade.Pendente
        };

        mensalidade.AtualizarStatusConformeVencimento(DateTime.Today);

        mensalidade.Status.Should().Be(StatusMensalidade.Atrasada);
    }

    [Fact]
    public void RegistrarPagamento_Deve_Marcar_Como_Paga_Com_Data()
    {
        var mensalidade = new Mensalidade
        {
            Id = Guid.NewGuid(),
            Valor = 150.00m,
            DataVencimento = DateTime.Today.AddDays(10),
            Status = StatusMensalidade.Pendente
        };

        var dataPagamento = DateTime.Now;
        mensalidade.RegistrarPagamento(dataPagamento, "Comprovante PIX 12345");

        mensalidade.Status.Should().Be(StatusMensalidade.Paga);
        mensalidade.DataPagamento.Should().Be(dataPagamento);
        mensalidade.ComprovantePagamento.Should().Be("Comprovante PIX 12345");
    }
}
