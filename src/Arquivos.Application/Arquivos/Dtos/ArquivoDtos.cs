namespace Arquivos.Application.Arquivos.Dtos;

public sealed class UploadArquivoRequest
{
    public required Stream Conteudo { get; init; }
    public required string NomeOriginal { get; init; }
    public string? ContentType { get; init; }
    public long? TamanhoInformado { get; init; }
    public string? Descricao { get; init; }
    public string? SistemaOrigem { get; init; }
    public string? ModuloOrigem { get; init; }
    public string? ReferenciaExterna { get; init; }
    public Dictionary<string, string>? Metadados { get; init; }
    public int? EmpresaIdOverride { get; init; }
    public string? IpOrigem { get; init; }
    public string? UserAgent { get; init; }
}

public sealed class AtualizarArquivoRequest
{
    public Stream? Conteudo { get; init; }
    public string? NomeOriginal { get; init; }
    public string? ContentType { get; init; }
    public string? Descricao { get; init; }
    public string? SistemaOrigem { get; init; }
    public string? ModuloOrigem { get; init; }
    public string? ReferenciaExterna { get; init; }
    public Dictionary<string, string>? Metadados { get; init; }
}

public sealed class ArquivoResponse
{
    public required string Token { get; init; }
    public required string NomeOriginal { get; init; }
    public string? Extensao { get; init; }
    public required string ContentType { get; init; }
    public required long TamanhoBytes { get; init; }
    public required string ChecksumSha256 { get; init; }
    public required int EmpresaId { get; init; }
    public int? UsuarioUploadId { get; init; }
    public string? SistemaOrigem { get; init; }
    public string? ModuloOrigem { get; init; }
    public string? ReferenciaExterna { get; init; }
    public string? Descricao { get; init; }
    public Dictionary<string, string> Metadados { get; init; } = new();
    public required bool Ativo { get; init; }
    public required DateTimeOffset DataCriacao { get; init; }
    public required DateTimeOffset DataAtualizacao { get; init; }
    public DateTimeOffset? DataDesativacao { get; init; }
}

public sealed class ListaArquivosResponse
{
    public required IReadOnlyList<ArquivoResponse> Items { get; init; }
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

public sealed class DownloadArquivoResult
{
    public required Stream Conteudo { get; init; }
    public required string NomeOriginal { get; init; }
    public required string ContentType { get; init; }
    public required long TamanhoBytes { get; init; }
}
