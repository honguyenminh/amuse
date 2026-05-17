using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Identity.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.RevokeToken;

public static class RevokeTokenEndpoint
{
    public static RouteGroupBuilder MapRevokeTokenEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/revoke", async (
                RevokeTokenRequest? request,
                RevokeTokenHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var refresh = TokenTransport.GetRefreshToken(httpContext, request?.RefreshToken);
                var authorization = httpContext.Request.Headers.Authorization.ToString();
                var result = await handler.HandleAsync(refresh, authorization, cancellationToken);
                TokenTransport.ClearRefreshCookie(httpContext);
                return result.ToResult();
            })
            .WithName("RevokeToken")
            .WithSummary(
                "Revoke refresh session. Pass Authorization: Bearer with the current access token to blacklist its jti immediately.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}

public sealed record RevokeTokenRequest(string? RefreshToken);
