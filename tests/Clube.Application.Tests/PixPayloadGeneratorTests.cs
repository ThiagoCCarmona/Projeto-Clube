using Xunit;
using FluentAssertions;
using Clube.Application.Services;

namespace Clube.Application.Tests;

public class PixPayloadGeneratorTests
{
    [Fact]
    public void Deve_Gerar_Payload_Pix_Valido_Com_Chave_E_Valor()
    {
        var chave = "45999990001";
        var nomeRecebedor = "ROTARY CLUB FOZ PONTE";
        var cidade = "FOZ DO IGUACU";
        var valor = 150.00m;
        var txId = "MENSALIDADE1026";

        var payload = PixPayloadGenerator.GerarPayload(chave, nomeRecebedor, cidade, valor, txId);

        payload.Should().NotBeNullOrWhiteSpace();
        payload.Should().StartWith("000201"); // Indicador do formato payload
        payload.Should().Contain("BR.GOV.BCB.PIX"); // Merchant Account Information
        payload.Should().Contain("45999990001"); // Chave PIX
        payload.Should().Contain("150.00"); // Valor formatado
        payload.Should().Contain("ROTARY CLUB FOZ PONTE"); // Nome recebedor
        payload.Should().Contain("FOZ DO IGUACU"); // Cidade
    }
}
