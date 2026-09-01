using System.Security.Claims;
using System.Text.Json;
using Arquivos.Application.Abstractions;
using Arquivos.Core.Auth;
using Microsoft.AspNetCore.Http;

namespace Arquivos.Infrastructure.Auth;

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor http) : ICurrentUserAccessor
{
    public CurrentUser User => FromPrincipal(http.HttpContext?.User, http.HttpContext);

    public static CurrentUser FromPrincipal(ClaimsPrincipal? principal, HttpContext? http = null)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new CurrentUser(null, null, null, null, [], []);
        }

        var isService = string.Equals(
            Claim(principal, EnterpriseClaims.AuthKind),
            "service",
            StringComparison.OrdinalIgnoreCase);

        var userId = ParseInt(Claim(principal, EnterpriseClaims.UserId)
            ?? http?.Request.Headers["X-User-Id"].FirstOrDefault());
        var username = Claim(principal, EnterpriseClaims.Username)
            ?? principal.Identity?.Name
            ?? http?.Request.Headers["X-Username"].FirstOrDefault();
        var email = Claim(principal, EnterpriseClaims.Email)
            ?? http?.Request.Headers["X-User-Email"].FirstOrDefault();
        var empresaId = ParseInt(Claim(principal, EnterpriseClaims.EmpresaId)
            ?? http?.Request.Headers["X-Empresa-Id"].FirstOrDefault());
        var originSystem = Claim(principal, EnterpriseClaims.OriginSystem)
            ?? http?.Request.Headers["X-Origin-System"].FirstOrDefault();

        var roles = GetList(principal, EnterpriseClaims.Roles);
        if (roles.Count == 0)
        {
            var header = http?.Request.Headers["X-User-Roles"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header))
            {
                roles = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        var modulos = GetList(principal, EnterpriseClaims.Modulos);

        return new CurrentUser(userId, username, email, empresaId, roles, modulos, isService, originSystem);
    }

    private static string? Claim(ClaimsPrincipal p, string type) =>
        p.FindFirst(type)?.Value;

    private static int? ParseInt(string? value) =>
        int.TryParse(value, out var n) ? n : null;

    private static IReadOnlyList<string> GetList(ClaimsPrincipal p, string type)
    {
        var values = p.FindAll(type).Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        if (values.Count == 1 && values[0].TrimStart().StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(values[0]) ?? [];
            }
            catch
            {
                return values;
            }
        }
        return values;
    }
}
