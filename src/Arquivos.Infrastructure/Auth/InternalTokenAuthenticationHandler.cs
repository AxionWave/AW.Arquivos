using System.Security.Claims;
using System.Text.Encodings.Web;
using Arquivos.Core.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arquivos.Infrastructure.Auth;

/// <summary>
/// Autenticação service-to-service via header X-Internal-Service-Token
/// (mesmo valor de GATEWAY_INTERNAL_TOKEN). Usado por outras APIs na rede interna.
/// </summary>
public sealed class InternalTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "InternalToken";
    public const string HeaderName = "X-Internal-Service-Token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var providedValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var provided = providedValues.ToString();
        var expected = Environment.GetEnvironmentVariable("GATEWAY_INTERNAL_TOKEN")
            ?? configuration["Gateway:InternalServiceToken"];

        if (string.IsNullOrWhiteSpace(expected))
        {
            return Task.FromResult(AuthenticateResult.Fail("internal_service_token_not_configured"));
        }

        if (!FixedTimeEquals(provided, expected))
        {
            return Task.FromResult(AuthenticateResult.Fail("invalid_internal_service_token"));
        }

        var claims = new List<Claim>
        {
            new(EnterpriseClaims.AuthKind, "service")
        };

        AddIfPresent(claims, EnterpriseClaims.UserId, Request.Headers["X-User-Id"].FirstOrDefault());
        AddIfPresent(claims, EnterpriseClaims.Username, Request.Headers["X-Username"].FirstOrDefault());
        AddIfPresent(claims, EnterpriseClaims.Email, Request.Headers["X-User-Email"].FirstOrDefault());
        AddIfPresent(claims, EnterpriseClaims.EmpresaId, Request.Headers["X-Empresa-Id"].FirstOrDefault());
        AddIfPresent(claims, EnterpriseClaims.OriginSystem, Request.Headers["X-Origin-System"].FirstOrDefault());

        var roles = Request.Headers["X-User-Roles"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(roles))
        {
            foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                claims.Add(new Claim(EnterpriseClaims.Roles, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static void AddIfPresent(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            claims.Add(new Claim(type, value.Trim()));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length) return false;
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
