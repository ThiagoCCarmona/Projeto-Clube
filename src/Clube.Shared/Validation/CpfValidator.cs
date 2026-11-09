using System.Text.RegularExpressions;

namespace Clube.Shared.Validation;

public static class CpfValidator
{
    public static string ApenasDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        return Regex.Replace(valor, @"[^\d]", "");
    }

    public static bool Validar(string? cpf)
    {
        var limpo = ApenasDigitos(cpf);

        if (limpo.Length != 11)
            return false;

        // Rejeita padrões com todos os dígitos idênticos (ex.: 00000000000, 11111111111)
        var todosIguais = true;
        for (int i = 1; i < 11; i++)
        {
            if (limpo[i] != limpo[0])
            {
                todosIguais = false;
                break;
            }
        }
        if (todosIguais)
            return false;

        // Cálculo do primeiro dígito verificador
        int soma1 = 0;
        for (int i = 0; i < 9; i++)
            soma1 += (limpo[i] - '0') * (10 - i);

        int resto1 = soma1 % 11;
        int digito1 = resto1 < 2 ? 0 : 11 - resto1;

        if ((limpo[9] - '0') != digito1)
            return false;

        // Cálculo do segundo dígito verificador
        int soma2 = 0;
        for (int i = 0; i < 10; i++)
            soma2 += (limpo[i] - '0') * (11 - i);

        int resto2 = soma2 % 11;
        int digito2 = resto2 < 2 ? 0 : 11 - resto2;

        return (limpo[10] - '0') == digito2;
    }
}
