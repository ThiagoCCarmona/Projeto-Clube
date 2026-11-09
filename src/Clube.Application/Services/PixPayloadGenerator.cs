using System;
using System.Globalization;
using System.Text;

namespace Clube.Application.Services;

public static class PixPayloadGenerator
{
    private const string ID_PAYLOAD_FORMAT_INDICATOR = "00";
    private const string ID_MERCHANT_ACCOUNT_INFORMATION = "26";
    private const string ID_MERCHANT_ACCOUNT_INFORMATION_GUI = "00";
    private const string ID_MERCHANT_ACCOUNT_INFORMATION_KEY = "01";
    private const string ID_MERCHANT_CATEGORY_CODE = "52";
    private const string ID_TRANSACTION_CURRENCY = "53";
    private const string ID_TRANSACTION_AMOUNT = "54";
    private const string ID_COUNTRY_CODE = "58";
    private const string ID_MERCHANT_NAME = "59";
    private const string ID_MERCHANT_CITY = "60";
    private const string ID_ADDITIONAL_DATA_FIELD_TEMPLATE = "62";
    private const string ID_ADDITIONAL_DATA_FIELD_TEMPLATE_TXID = "05";
    private const string ID_CRC16 = "63";

    public static string GerarPayload(
        string chavePix,
        string nomeRecebedor,
        string cidadeRecebedor,
        decimal valor,
        string txId = "***")
    {
        var nomeFormatado = RemoverAcentos(nomeRecebedor.ToUpperInvariant());
        if (nomeFormatado.Length > 25) nomeFormatado = nomeFormatado.Substring(0, 25);

        var cidadeFormatada = RemoverAcentos(cidadeRecebedor.ToUpperInvariant());
        if (cidadeFormatada.Length > 15) cidadeFormatada = cidadeFormatada.Substring(0, 15);

        var merchantAccountInfo = 
            FormatarCampo(ID_MERCHANT_ACCOUNT_INFORMATION_GUI, "BR.GOV.BCB.PIX") +
            FormatarCampo(ID_MERCHANT_ACCOUNT_INFORMATION_KEY, chavePix);

        var valorString = valor.ToString("F2", CultureInfo.InvariantCulture);

        var additionalData = FormatarCampo(ID_ADDITIONAL_DATA_FIELD_TEMPLATE_TXID, txId);

        var sb = new StringBuilder();
        sb.Append(FormatarCampo(ID_PAYLOAD_FORMAT_INDICATOR, "01"));
        sb.Append(FormatarCampo(ID_MERCHANT_ACCOUNT_INFORMATION, merchantAccountInfo));
        sb.Append(FormatarCampo(ID_MERCHANT_CATEGORY_CODE, "0000"));
        sb.Append(FormatarCampo(ID_TRANSACTION_CURRENCY, "986")); // BRL
        sb.Append(FormatarCampo(ID_TRANSACTION_AMOUNT, valorString));
        sb.Append(FormatarCampo(ID_COUNTRY_CODE, "BR"));
        sb.Append(FormatarCampo(ID_MERCHANT_NAME, nomeFormatado));
        sb.Append(FormatarCampo(ID_MERCHANT_CITY, cidadeFormatada));
        sb.Append(FormatarCampo(ID_ADDITIONAL_DATA_FIELD_TEMPLATE, additionalData));
        sb.Append(ID_CRC16 + "04");

        var payloadSemCrc = sb.ToString();
        var crc = CalcularCRC16(payloadSemCrc);

        return payloadSemCrc + crc;
    }

    private static string FormatarCampo(string id, string valor)
    {
        var tamanho = valor.Length.ToString("D2");
        return $"{id}{tamanho}{valor}";
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizedString = texto.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CalcularCRC16(string payload)
    {
        ushort crc = 0xFFFF;
        var bytes = Encoding.UTF8.GetBytes(payload);

        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc = (ushort)(crc << 1);
            }
        }

        return crc.ToString("X4");
    }
}
