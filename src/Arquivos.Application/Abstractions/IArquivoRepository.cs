using Arquivos.Core.Entities;

namespace Arquivos.Application.Abstractions;

public interface IArquivoRepository
{
    Task AddAsync(Arquivo arquivo, CancellationToken ct);
    Task<Arquivo?> GetByTokenAsync(string token, CancellationToken ct);
    Task<(IReadOnlyList<Arquivo> Items, int Total)> ListAsync(
        int empresaId,
        bool? ativo,
        string? sistemaOrigem,
        string? contentType,
        string? buscaNome,
        int page,
        int pageSize,
        CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
