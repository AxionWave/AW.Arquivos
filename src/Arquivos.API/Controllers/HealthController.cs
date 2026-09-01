using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquivos.API.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", product = "Arquivos", service = "arquivos" });
}
