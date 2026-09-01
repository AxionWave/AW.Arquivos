using Arquivos.Application.Abstractions;
using Arquivos.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arquivos.Infrastructure.Persistence;

public sealed class ArquivoRepository(ArquivosDbContext db) : IArquivoRepository
{
    public Task AddAsync(Arquivo arquivo, CancellationToken ct) =>
        db.Arquivos.AddAsync(arquivo, ct).AsTask();

    public Task<Arquivo?> GetByTokenAsync(string token, CancellationToken ct) =>
        db.Arquivos.FirstOrDefaultAsync(a => a.Token == token, ct);

    public async Task<(IReadOnlyList<Arquivo> Items, int Total)> ListAsync(
        int empresaId,
        bool? ativo,
        string? sistemaOrigem,
        string? contentType,
        string? buscaNome,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var q = db.Arquivos.AsNoTracking().Where(a => a.EmpresaId == empresaId);

        if (ativo is not null)
            q = q.Where(a => a.Ativo == ativo);

        if (!string.IsNullOrWhiteSpace(sistemaOrigem))
        {
            var s = sistemaOrigem.Trim();
            q = q.Where(a => a.SistemaOrigem == s);
        }

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var ctType = contentType.Trim();
            q = q.Where(a => a.ContentType.StartsWith(ctType));
        }

        if (!string.IsNullOrWhiteSpace(buscaNome))
        {
            var term = buscaNome.Trim();
            q = q.Where(a => a.NomeOriginal.ToLower().Contains(term.ToLower()));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.DataCriacao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
