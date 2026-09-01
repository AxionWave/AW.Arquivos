using System.Security.Cryptography;

namespace Arquivos.Core.Tokens;

public static class ArquivoToken
{
    public const string Prefix = "arq_";

    /// <summary>Token opaco, único por envio. Não deriva do conteúdo do arquivo.</summary>
    public static string New()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        var b64 = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return Prefix + b64;
    }

    public static bool IsValidFormat(string? token) =>
        !string.IsNullOrWhiteSpace(token)
        && token.StartsWith(Prefix, StringComparison.Ordinal)
        && token.Length is >= 20 and <= 80
        && token.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');
}
