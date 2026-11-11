using Arquivos.Application.Arquivos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Arquivos.API.Controllers;

/// <summary>Imagens marcadas como públicas no envio (logos em e-mails e páginas sem login).</summary>
[ApiController]
[AllowAnonymous]
[Route("api/arquivos/publico")]
public sealed class PublicoController(IArquivoService arquivos) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Baixar(string token, CancellationToken ct)
    {
        var arquivo = await arquivos.DownloadPublicoAsync(token, ct);
        Response.Headers[HeaderNames.CacheControl] = "public, max-age=3600";
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        Response.Headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'";
        Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        var etag = arquivo.ChecksumSha256 is null ? null : new EntityTagHeaderValue($"\"{arquivo.ChecksumSha256}\"");
        return File(arquivo.Conteudo, arquivo.ContentType, lastModified: null, entityTag: etag);
    }
}
