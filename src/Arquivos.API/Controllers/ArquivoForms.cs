using Microsoft.AspNetCore.Http;

namespace Arquivos.API.Controllers;

public sealed class UploadArquivoForm
{
    public IFormFile File { get; set; } = null!;
    public string? Descricao { get; set; }
    public string? SistemaOrigem { get; set; }
    public string? ModuloOrigem { get; set; }
    public string? ReferenciaExterna { get; set; }
    public string? Metadados { get; set; }
    public int? EmpresaId { get; set; }
    /// <summary>Só imagens (PNG, JPEG, WebP, GIF) de até 2 MB.</summary>
    public bool Publico { get; set; }
}

public sealed class AtualizarArquivoForm
{
    public IFormFile? File { get; set; }
    public string? NomeOriginal { get; set; }
    public string? Descricao { get; set; }
    public string? SistemaOrigem { get; set; }
    public string? ModuloOrigem { get; set; }
    public string? ReferenciaExterna { get; set; }
    public string? Metadados { get; set; }
}
