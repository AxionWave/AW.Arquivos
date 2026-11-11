using System.Security.Cryptography;
using Arquivos.Application.Abstractions;
using Arquivos.Application.Arquivos;
using Microsoft.Extensions.Options;

namespace Arquivos.Infrastructure.Storage;

/// <summary>Grava em disco em {RootPath}/{empresaId}/{aaaa}/{mm}/{token}{extensão}.</summary>
public sealed class LocalFileStorage(IOptions<StorageOptions> options) : IArquivoStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);
    private readonly long _maxBytes = options.Value.MaxFileBytes;

    public async Task<StoredBlob> SaveAsync(Stream content, string token, int empresaId, string? extension, CancellationToken ct)
    {
        var ext = NormalizarExtensao(extension);
        var agora = DateTime.UtcNow;
        var pasta = Path.Combine(empresaId.ToString(), agora.ToString("yyyy"), agora.ToString("MM"));
        var nome = token + ext;
        var relativo = Path.Combine(pasta, nome).Replace('\\', '/');
        var destino = Caminho(relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        var temporario = destino + ".tmp";
        long total = 0;
        try
        {
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var saida = new FileStream(temporario, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                int lidos;
                while ((lidos = await content.ReadAsync(buffer, ct)) > 0)
                {
                    total += lidos;
                    if (total > _maxBytes)
                        throw new InvalidDataException($"Arquivo excede o limite de {_maxBytes} bytes.");
                    sha.AppendData(buffer, 0, lidos);
                    await saida.WriteAsync(buffer.AsMemory(0, lidos), ct);
                }
            }

            File.Move(temporario, destino);
            return new StoredBlob(relativo, nome, total, Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant());
        }
        catch
        {
            if (File.Exists(temporario)) File.Delete(temporario);
            throw;
        }
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var caminho = Caminho(relativePath);
        if (!File.Exists(caminho))
            throw new FileNotFoundException("Blob não encontrado.", relativePath);
        Stream stream = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        var caminho = Caminho(relativePath);
        if (File.Exists(caminho)) File.Delete(caminho);
        return Task.CompletedTask;
    }

    private string Caminho(string relativo)
    {
        var completo = Path.GetFullPath(Path.Combine(_root, relativo));
        if (!completo.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Caminho fora do diretório de armazenamento.");
        return completo;
    }

    private static string NormalizarExtensao(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
        var limpa = new string(extension.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray()).ToLowerInvariant();
        if (!limpa.StartsWith('.')) limpa = "." + limpa;
        return limpa.Length > 16 ? limpa[..16] : limpa;
    }
}
