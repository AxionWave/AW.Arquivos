namespace Arquivos.Application.Abstractions;

public sealed record StoredBlob(string RelativePath, string StoredFileName, long SizeBytes, string Sha256Hex);

public interface IArquivoStorage
{
    Task<StoredBlob> SaveAsync(Stream content, string token, int empresaId, string? extension, CancellationToken ct);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
}
