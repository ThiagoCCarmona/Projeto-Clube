using Xunit;
using FluentAssertions;
using Clube.Shared.Validation;

namespace Clube.Domain.Tests;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("52998224725")] // CPF válido conhecido com dígito verificador correto
    [InlineData("529.982.247-25")] // Formatado com pontuação
    public void Deve_Validar_Cpf_Correto(string cpf)
    {
        var valido = CpfValidator.Validar(cpf);
        valido.Should().BeTrue();
    }

    [Theory]
    [InlineData("11111111111")] // Todos os dígitos iguais
    [InlineData("00000000000")]
    [InlineData("12345678900")] // Dígito verificador incorreto
    [InlineData("123")] // Tamanho menor
    [InlineData("")] // Vazio
    [InlineData(null)] // Nulo
    public void Deve_Rejeitar_Cpf_Invalido(string? cpf)
    {
        var valido = CpfValidator.Validar(cpf);
        valido.Should().BeFalse();
    }

    [Fact]
    public void Deve_Formatar_Cpf_Apenas_Digitos()
    {
        var formatado = CpfValidator.ApenasDigitos("529.982.247-25");
        formatado.Should().Be("52998224725");
    }
}
