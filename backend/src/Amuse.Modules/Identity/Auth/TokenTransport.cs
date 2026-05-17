using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amuse.Modules.Identity.Auth;

public static class TokenTransport
{
    public static bool IsWebClient(HttpContext httpContext) =>
        string.Equals(
            httpContext.Request.Headers[AuthConstants.ClientTypeHeader],
            AuthConstants.WebClient,
            StringComparison.OrdinalIgnoreCase);

    public static void SetRefreshCookie(HttpContext httpContext, string refreshToken, DateTimeOffset expiresAt)
    {
        var useSecureCookies = !httpContext.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment();

        httpContext.Response.Cookies.Append(
            AuthConstants.RefreshCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = useSecureCookies,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
            });
    }

    public static string? GetRefreshToken(HttpContext httpContext, string? bodyRefreshToken)
    {
        if (!string.IsNullOrWhiteSpace(bodyRefreshToken))
            return bodyRefreshToken;

        return httpContext.Request.Cookies[AuthConstants.RefreshCookieName];
    }

    public static void ClearRefreshCookie(HttpContext httpContext) =>
        httpContext.Response.Cookies.Delete(AuthConstants.RefreshCookieName);
}
