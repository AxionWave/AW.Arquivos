namespace Arquivos.Core.Entities;

/// <summary>
/// Um envio de arquivo. Cada POST gera um registro novo e um token único,
/// mesmo que o conteúdo binário seja idêntico a um envio anterior.
/// </summary>
public sealed class Arquivo
{
    public Guid Id { get; set; }

    /// <summary>Identificador público opaco. Nunca reutilizado.</summary>
    public string Token { get; set; } = string.Empty;

    public string NomeOriginal { get; set; } = string.Empty;
    public string NomeArmazenado { get; set; } = string.Empty;
    public string? Extensao { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public long TamanhoBytes { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string CaminhoStorage { get; set; } = string.Empty;
    public string ProvedorStorage { get; set; } = "local";

    public int EmpresaId { get; set; }
    public int? UsuarioUploadId { get; set; }

    /// <summary>Sigla do sistema que enviou (ASC, ORI, LYR, CORE, …).</summary>
    public string? SistemaOrigem { get; set; }
    public string? ModuloOrigem { get; set; }
    public string? ReferenciaExterna { get; set; }
    public string? Descricao { get; set; }

    public Dictionary<string, string> Metadados { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? IpOrigem { get; set; }
    public string? UserAgent { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTimeOffset DataCriacao { get; set; }
    public DateTimeOffset DataAtualizacao { get; set; }
    public DateTimeOffset? DataDesativacao { get; set; }
    public int? UsuarioDesativacaoId { get; set; }
}
