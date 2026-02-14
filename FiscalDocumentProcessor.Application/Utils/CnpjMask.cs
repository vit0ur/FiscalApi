namespace FiscalDocumentProcessor.Application.Utils;

public static class CnpjMask
{
    public static string Mask(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length < 14)
            return cnpj ?? string.Empty;
        return $"{cnpj[0]}{cnpj[1]}.***.***/{cnpj[8]}***-**";
    }
}
