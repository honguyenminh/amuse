using System.Globalization;
using System.IdentityModel.Tokens.Jwt;

namespace Amuse.Modules.Identity.Auth;

internal static class AccessTokenClaims
{
    public static bool TryReadJtiAndExpiry(
        string? authorizationHeader,
        out string jti,
        out DateTimeOffset expiresAt)
    {
        jti = string.Empty;
        expiresAt = default;

        if (!TryExtractBearerToken(authorizationHeader, out var token))
            return false;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return false;

        var jwt = handler.ReadJwtToken(token);
        var jtiClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value
            ?? jwt.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;

        if (string.IsNullOrWhiteSpace(jtiClaim))
            return false;

        var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
        if (expClaim is null || !long.TryParse(expClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expUnix))
            return false;

        jti = jtiClaim;
        expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
        return true;
    }

    private static bool TryExtractBearerToken(string? authorizationHeader, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return false;

        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        token = authorizationHeader[prefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }
}
