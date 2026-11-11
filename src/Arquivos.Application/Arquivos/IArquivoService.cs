using Arquivos.Application.Arquivos.Dtos;

namespace Arquivos.Application.Arquivos;

public interface IArquivoService
{
    Task<ArquivoResponse> UploadAsync(UploadArquivoRequest request, CancellationToken ct);
    Task<ArquivoResponse> GetMetadataAsync(string token, CancellationToken ct);
    Task<DownloadArquivoResult> DownloadAsync(string token, CancellationToken ct);
    /// <summary>Sem login: só arquivos ativos marcados como públicos.</summary>
    Task<DownloadArquivoResult> DownloadPublicoAsync(string token, CancellationToken ct);
    Task<ArquivoResponse> UpdateAsync(string token, AtualizarArquivoRequest request, CancellationToken ct);
    Task<ArquivoResponse> SetAtivoAsync(string token, bool ativo, CancellationToken ct);
    Task<ListaArquivosResponse> ListAsync(
        bool? ativo,
        string? sistemaOrigem,
        string? contentType,
        string? buscaNome,
        int page,
        int pageSize,
        int? empresaIdFiltro,
        CancellationToken ct);
}
