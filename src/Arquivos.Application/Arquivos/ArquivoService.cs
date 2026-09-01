using Arquivos.Application.Abstractions;
using Arquivos.Application.Arquivos.Dtos;
using Arquivos.Core.Entities;
using Arquivos.Core.Exceptions;
using Arquivos.Core.Tokens;
using Microsoft.Extensions.Options;

namespace Arquivos.Application.Arquivos;

public sealed class StorageOptions
{
    public const string Section = "Storage";
    public string RootPath { get; set; } = "storage";
    public long MaxFileBytes { get; set; } = 52_428_800;
    public string Provider { get; set; } = "local";
}

public sealed class ArquivoService(
    IArquivoRepository repository,
    IArquivoStorage storage,
    ICurrentUserAccessor currentUser,
    IOptions<StorageOptions> storageOptions) : IArquivoService
{
    public async Task<ArquivoResponse> UploadAsync(UploadArquivoRequest request, CancellationToken ct)
    {
        var user = RequireAuth();
        var empresaId = ResolveEmpresaId(user, request.EmpresaIdOverride);
        var nome = SanitizeFileName(request.NomeOriginal);
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArquivoValidationException("Nome do arquivo é obrigatório.");

        var contentType = NormalizeContentType(request.ContentType);
        var max = storageOptions.Value.MaxFileBytes;
        if (request.TamanhoInformado is > 0 && request.TamanhoInformado > max)
            throw new ArquivoValidationException($"Arquivo excede o limite de {max} bytes.");

        if (request.Conteudo.CanSeek && request.Conteudo.Length == 0)
            throw new ArquivoValidationException("Arquivo vazio não é permitido.");

        var token = await NewUniqueTokenAsync(ct);
        var extension = Path.GetExtension(nome);
        if (extension.Length > 32) extension = extension[..32];

        StoredBlob blob;
        try
        {
            blob = await storage.SaveAsync(request.Conteudo, token, empresaId, extension, ct);
        }
        catch (InvalidDataException ex)
        {
            throw new ArquivoValidationException(ex.Message);
        }

        if (blob.SizeBytes == 0)
        {
            await TryDeleteAsync(blob.RelativePath, ct);
            throw new ArquivoValidationException("Arquivo vazio não é permitido.");
        }

        if (blob.SizeBytes > max)
        {
            await TryDeleteAsync(blob.RelativePath, ct);
            throw new ArquivoValidationException($"Arquivo excede o limite de {max} bytes.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new Arquivo
        {
            Id = Guid.NewGuid(),
            Token = token,
            NomeOriginal = nome,
            NomeArmazenado = blob.StoredFileName,
            Extensao = string.IsNullOrEmpty(extension) ? null : extension.ToLowerInvariant(),
            ContentType = contentType,
            TamanhoBytes = blob.SizeBytes,
            ChecksumSha256 = blob.Sha256Hex,
            CaminhoStorage = blob.RelativePath,
            ProvedorStorage = storageOptions.Value.Provider,
            EmpresaId = empresaId,
            UsuarioUploadId = user.UserId,
            SistemaOrigem = NormalizeOptional(request.SistemaOrigem ?? user.OriginSystem, 32),
            ModuloOrigem = NormalizeOptional(request.ModuloOrigem, 64),
            ReferenciaExterna = NormalizeOptional(request.ReferenciaExterna, 128),
            Descricao = NormalizeOptional(request.Descricao, 500),
            Metadados = request.Metadados ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            IpOrigem = NormalizeOptional(request.IpOrigem, 64),
            UserAgent = NormalizeOptional(request.UserAgent, 512),
            Ativo = true,
            DataCriacao = now,
            DataAtualizacao = now
        };

        try
        {
            await repository.AddAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch
        {
            await TryDeleteAsync(blob.RelativePath, ct);
            throw;
        }

        return Map(entity);
    }

    public async Task<ArquivoResponse> GetMetadataAsync(string token, CancellationToken ct)
    {
        var arquivo = await LoadOwnedAsync(token, ct);
        return Map(arquivo);
    }

    public async Task<DownloadArquivoResult> DownloadAsync(string token, CancellationToken ct)
    {
        var arquivo = await LoadOwnedAsync(token, ct);
        if (!arquivo.Ativo)
            throw new ArquivoDesativadoException();

        var stream = await storage.OpenReadAsync(arquivo.CaminhoStorage, ct);
        return new DownloadArquivoResult
        {
            Conteudo = stream,
            NomeOriginal = arquivo.NomeOriginal,
            ContentType = arquivo.ContentType,
            TamanhoBytes = arquivo.TamanhoBytes
        };
    }

    public async Task<ArquivoResponse> UpdateAsync(string token, AtualizarArquivoRequest request, CancellationToken ct)
    {
        var arquivo = await LoadOwnedAsync(token, ct);
        if (!arquivo.Ativo)
            throw new ArquivoDesativadoException();

        var max = storageOptions.Value.MaxFileBytes;
        string? oldPath = null;

        if (request.Conteudo is not null)
        {
            if (request.Conteudo.CanSeek && request.Conteudo.Length == 0)
                throw new ArquivoValidationException("Arquivo vazio não é permitido.");

            var extension = Path.GetExtension(request.NomeOriginal ?? arquivo.NomeOriginal);
            if (extension.Length > 32) extension = extension[..32];

            StoredBlob blob;
            try
            {
                blob = await storage.SaveAsync(request.Conteudo, ArquivoToken.New(), arquivo.EmpresaId, extension, ct);
            }
            catch (InvalidDataException ex)
            {
                throw new ArquivoValidationException(ex.Message);
            }

            if (blob.SizeBytes == 0)
            {
                await TryDeleteAsync(blob.RelativePath, ct);
                throw new ArquivoValidationException("Arquivo vazio não é permitido.");
            }

            if (blob.SizeBytes > max)
            {
                await TryDeleteAsync(blob.RelativePath, ct);
                throw new ArquivoValidationException($"Arquivo excede o limite de {max} bytes.");
            }

            oldPath = arquivo.CaminhoStorage;
            arquivo.NomeArmazenado = blob.StoredFileName;
            arquivo.CaminhoStorage = blob.RelativePath;
            arquivo.TamanhoBytes = blob.SizeBytes;
            arquivo.ChecksumSha256 = blob.Sha256Hex;
            arquivo.Extensao = string.IsNullOrEmpty(extension) ? arquivo.Extensao : extension.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(request.ContentType))
                arquivo.ContentType = NormalizeContentType(request.ContentType);
        }

        if (!string.IsNullOrWhiteSpace(request.NomeOriginal))
            arquivo.NomeOriginal = SanitizeFileName(request.NomeOriginal);

        if (request.Descricao is not null)
            arquivo.Descricao = NormalizeOptional(request.Descricao, 500);

        if (request.SistemaOrigem is not null)
            arquivo.SistemaOrigem = NormalizeOptional(request.SistemaOrigem, 32);

        if (request.ModuloOrigem is not null)
            arquivo.ModuloOrigem = NormalizeOptional(request.ModuloOrigem, 64);

        if (request.ReferenciaExterna is not null)
            arquivo.ReferenciaExterna = NormalizeOptional(request.ReferenciaExterna, 128);

        if (request.Metadados is not null)
            arquivo.Metadados = request.Metadados;

        arquivo.DataAtualizacao = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);

        if (oldPath is not null && !string.Equals(oldPath, arquivo.CaminhoStorage, StringComparison.Ordinal))
            await TryDeleteAsync(oldPath, ct);

        return Map(arquivo);
    }

    public async Task<ArquivoResponse> SetAtivoAsync(string token, bool ativo, CancellationToken ct)
    {
        var user = RequireAuth();
        var arquivo = await LoadOwnedAsync(token, ct);

        if (arquivo.Ativo == ativo)
            return Map(arquivo);

        arquivo.Ativo = ativo;
        arquivo.DataAtualizacao = DateTimeOffset.UtcNow;
        if (ativo)
        {
            arquivo.DataDesativacao = null;
            arquivo.UsuarioDesativacaoId = null;
        }
        else
        {
            arquivo.DataDesativacao = arquivo.DataAtualizacao;
            arquivo.UsuarioDesativacaoId = user.UserId;
        }

        await repository.SaveChangesAsync(ct);
        return Map(arquivo);
    }

    public async Task<ListaArquivosResponse> ListAsync(
        bool? ativo,
        string? sistemaOrigem,
        string? contentType,
        string? buscaNome,
        int page,
        int pageSize,
        int? empresaIdFiltro,
        CancellationToken ct)
    {
        var user = RequireAuth();
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        int empresaId;
        if (user.IsSuperAdmin && empresaIdFiltro is > 0)
            empresaId = empresaIdFiltro.Value;
        else
            empresaId = ResolveEmpresaId(user, empresaIdFiltro);

        var (items, total) = await repository.ListAsync(
            empresaId, ativo, sistemaOrigem, contentType, buscaNome, page, pageSize, ct);

        return new ListaArquivosResponse
        {
            Items = items.Select(Map).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task<Arquivo> LoadOwnedAsync(string token, CancellationToken ct)
    {
        var user = RequireAuth();
        if (!ArquivoToken.IsValidFormat(token))
            throw new ArquivoNotFoundException();

        var arquivo = await repository.GetByTokenAsync(token, ct)
            ?? throw new ArquivoNotFoundException();

        if (!user.IsSuperAdmin)
        {
            var empresaId = ResolveEmpresaId(user, null, required: false);
            if (empresaId < 1 || arquivo.EmpresaId != empresaId)
                throw new ArquivoNotFoundException();
        }

        return arquivo;
    }

    private Core.Auth.CurrentUser RequireAuth()
    {
        var user = currentUser.User;
        if (!user.IsAuthenticated)
            throw new TenantAccessDeniedException("JWT ausente ou inválido.");
        return user;
    }

    private static int ResolveEmpresaId(Core.Auth.CurrentUser user, int? overrideId, bool required = true)
    {
        if (user.IsSuperAdmin && overrideId is > 0)
            return overrideId.Value;

        if (overrideId is > 0 && user.IsServiceCaller && user.EmpresaId is null)
            return overrideId.Value;

        if (user.EmpresaId is > 0)
            return user.EmpresaId.Value;

        if (overrideId is > 0 && user.IsSuperAdmin)
            return overrideId.Value;

        if (!required)
            return -1;

        throw new ArquivoValidationException(
            "empresaId é obrigatório. Informe no JWT, no header X-Empresa-Id ou no campo empresaId.");
    }

    private async Task<string> NewUniqueTokenAsync(CancellationToken ct)
    {
        for (var i = 0; i < 8; i++)
        {
            var token = ArquivoToken.New();
            if (await repository.GetByTokenAsync(token, ct) is null)
                return token;
        }

        throw new InvalidOperationException("Não foi possível gerar um token único.");
    }

    private async Task TryDeleteAsync(string path, CancellationToken ct)
    {
        try { await storage.DeleteAsync(path, ct); }
        catch { /* best-effort cleanup */ }
    }

    private static string SanitizeFileName(string? name)
    {
        var file = Path.GetFileName((name ?? string.Empty).Replace('\\', '/').Trim());
        foreach (var c in Path.GetInvalidFileNameChars())
            file = file.Replace(c, '_');
        return file.Length > 512 ? file[..512] : file;
    }

    private static string NormalizeContentType(string? contentType)
    {
        var ct = (contentType ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(ct) || ct.Length > 255)
            return "application/octet-stream";
        return ct;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.Length > max ? v[..max] : v;
    }

    private static ArquivoResponse Map(Arquivo a) => new()
    {
        Token = a.Token,
        NomeOriginal = a.NomeOriginal,
        Extensao = a.Extensao,
        ContentType = a.ContentType,
        TamanhoBytes = a.TamanhoBytes,
        ChecksumSha256 = a.ChecksumSha256,
        EmpresaId = a.EmpresaId,
        UsuarioUploadId = a.UsuarioUploadId,
        SistemaOrigem = a.SistemaOrigem,
        ModuloOrigem = a.ModuloOrigem,
        ReferenciaExterna = a.ReferenciaExterna,
        Descricao = a.Descricao,
        Metadados = a.Metadados,
        Ativo = a.Ativo,
        DataCriacao = a.DataCriacao,
        DataAtualizacao = a.DataAtualizacao,
        DataDesativacao = a.DataDesativacao
    };
}
