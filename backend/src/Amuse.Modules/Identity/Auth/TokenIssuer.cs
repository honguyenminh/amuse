using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amuse.Domain.Identity;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Amuse.Modules.Identity.Auth;

internal sealed class TokenIssuer(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public string CreateAccessToken(AccountId accountId, PersonaAccessContext persona, DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, accountId.Value.ToString()),
            new("ctx", persona.ContextType),
        };

        if (persona.OrgId is not null)
            claims.Add(new Claim("org_id", persona.OrgId.Value.ToString()));

        if (persona.ListenerId is not null)
            claims.Add(new Claim("listener_id", persona.ListenerId.Value.ToString()));

        if (!string.IsNullOrWhiteSpace(persona.OrgRoleLabel))
            claims.Add(new Claim("org_role", persona.OrgRoleLabel));

        foreach (var claim in persona.Claims)
            claims.Add(new Claim("claims", claim));

        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        return CreateJwt(claims, expires, includeJti: true);
    }

    public string CreateRefreshToken(AccountId accountId, RefreshSessionId sessionId, DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, accountId.Value.ToString()),
            new("sid", sessionId.Value.ToString()),
        };

        var expires = now.AddDays(_options.RefreshTokenDays);
        return CreateJwt(claims, expires, includeJti: false);
    }

    public string CreateOpaqueRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    private string CreateJwt(List<Claim> claims, DateTimeOffset expires, bool includeJti)
    {
        if (includeJti)
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
