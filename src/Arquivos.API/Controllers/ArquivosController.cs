using System.Text.Json;
using Arquivos.Application.Arquivos;
using Arquivos.Application.Arquivos.Dtos;
using Arquivos.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquivos.API.Controllers;

[ApiController]
[Authorize]
[Route("api/arquivos")]
public sealed class ArquivosController(IArquivoService arquivos, ICurrentUserAccessor currentUser) : ControllerBase
{
    /// <summary>Smoke: claims do JWT ou da chamada interna.</summary>
    [HttpGet("me")]
    public IActionResult Me()
    {
        var u = currentUser.User;
        return Ok(new
        {
            product = "Arquivos",
            sigla = "ARQ",
            u.UserId,
            u.Username,
            u.Email,
            u.EmpresaId,
            u.Roles,
            u.Modulos,
            u.IsServiceCaller,
            u.OriginSystem
        });
    }

    /// <summary>
    /// Envia um arquivo. Sempre cria um registro novo e devolve um token único,
    /// mesmo que o conteúdo seja idêntico a um envio anterior.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<ArquivoResponse>> Upload([FromForm] UploadArquivoForm form, CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new { error = "validacao", message = "Campo file é obrigatório." });

        await using var stream = form.File.OpenReadStream();
        var result = await arquivos.UploadAsync(new UploadArquivoRequest
        {
            Conteudo = stream,
            NomeOriginal = form.File.FileName,
            ContentType = form.File.ContentType,
            TamanhoInformado = form.File.Length,
            Descricao = form.Descricao,
            SistemaOrigem = form.SistemaOrigem ?? Request.Headers["X-Origin-System"].FirstOrDefault(),
            ModuloOrigem = form.ModuloOrigem,
            ReferenciaExterna = form.ReferenciaExterna,
            Metadados = ParseMetadados(form.Metadados),
            EmpresaIdOverride = form.EmpresaId,
            IpOrigem = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        }, ct);

        return CreatedAtAction(nameof(GetByToken), new { token = result.Token }, result);
    }

    [HttpGet]
    public async Task<ActionResult<ListaArquivosResponse>> List(
        [FromQuery] bool? ativo,
        [FromQuery] string? sistemaOrigem,
        [FromQuery] string? contentType,
        [FromQuery] string? nome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? empresaId = null,
        CancellationToken ct = default)
    {
        var result = await arquivos.ListAsync(ativo, sistemaOrigem, contentType, nome, page, pageSize, empresaId, ct);
        return Ok(result);
    }

    [HttpGet("{token}")]
    public async Task<ActionResult<ArquivoResponse>> GetByToken(string token, CancellationToken ct)
    {
        return Ok(await arquivos.GetMetadataAsync(token, ct));
    }

    [HttpGet("{token}/download")]
    public async Task<IActionResult> Download(string token, [FromQuery] bool inline = false, CancellationToken ct = default)
    {
        var downloaded = await arquivos.DownloadAsync(token, ct);
        if (inline)
            return File(downloaded.Conteudo, downloaded.ContentType, enableRangeProcessing: true);

        return File(downloaded.Conteudo, downloaded.ContentType, downloaded.NomeOriginal, enableRangeProcessing: true);
    }

    /// <summary>Substitui o conteúdo e/ou metadados. O token permanece o mesmo.</summary>
    [HttpPut("{token}")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<ArquivoResponse>> Update(
        string token,
        [FromForm] AtualizarArquivoForm form,
        CancellationToken ct)
    {
        Stream? stream = null;
        if (form.File is { Length: > 0 })
            stream = form.File.OpenReadStream();

        try
        {
            var result = await arquivos.UpdateAsync(token, new AtualizarArquivoRequest
            {
                Conteudo = stream,
                NomeOriginal = form.NomeOriginal ?? form.File?.FileName,
                ContentType = form.File?.ContentType,
                Descricao = form.Descricao,
                SistemaOrigem = form.SistemaOrigem,
                ModuloOrigem = form.ModuloOrigem,
                ReferenciaExterna = form.ReferenciaExterna,
                Metadados = ParseMetadados(form.Metadados)
            }, ct);
            return Ok(result);
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
    }

    [HttpPatch("{token}/desativar")]
    public async Task<ActionResult<ArquivoResponse>> Desativar(string token, CancellationToken ct) =>
        Ok(await arquivos.SetAtivoAsync(token, false, ct));

    [HttpPatch("{token}/ativar")]
    public async Task<ActionResult<ArquivoResponse>> Ativar(string token, CancellationToken ct) =>
        Ok(await arquivos.SetAtivoAsync(token, true, ct));

    private static Dictionary<string, string>? ParseMetadados(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
        }
        catch (JsonException)
        {
            throw new Arquivos.Core.Exceptions.ArquivoValidationException(
                "metadados deve ser um JSON objeto de string → string.");
        }
    }
}
