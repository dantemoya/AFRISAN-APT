using System.Text.RegularExpressions;

namespace Afrisan.Nuevo.Contratos;

/// <summary>
/// Normaliza lo que se escribe o se lee de un QR para consultar un cilindro: el código corto
/// (AF-00001, también «AF-1», «af 1» o «1»), el código largo anterior (AFR-…), la dirección de
/// una ficha (…/cilindros/AF-00001) o el número de serie, que la API busca si no hay código igual.
/// </summary>
public static partial class CodigosCilindro
{
    public static string? Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var valor = texto.Trim();
        if (Uri.TryCreate(valor, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            valor = Ficha(uri.AbsolutePath) ?? "";
        else if (valor.StartsWith('/') || valor.StartsWith("cilindros/", StringComparison.OrdinalIgnoreCase))
            valor = Ficha(valor) ?? "";
        valor = valor.Trim().ToUpperInvariant();
        if (valor.Length is 0 or > 120 || valor.Any(char.IsControl)) return null;
        var compacto = Espacios().Replace(valor, "");
        if (Corto().Match(compacto) is { Success: true } corto) return $"AF-{int.Parse(corto.Groups[1].Value):D5}";
        return Largo().IsMatch(compacto) ? compacto : valor;
    }

    private static string? Ficha(string ruta)
    {
        var i = ruta.IndexOf("cilindros/", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        var resto = ruta[(i + "cilindros/".Length)..].Trim('/');
        return resto.Length == 0 || resto.Contains('/') ? null : Uri.UnescapeDataString(resto);
    }

    [GeneratedRegex(@"\s+")] private static partial Regex Espacios();
    [GeneratedRegex(@"\A(?:AF-?)?([0-9]{1,5})\z")] private static partial Regex Corto();
    [GeneratedRegex(@"\AAFR-[0-9A-F]{6,32}\z")] private static partial Regex Largo();
}
