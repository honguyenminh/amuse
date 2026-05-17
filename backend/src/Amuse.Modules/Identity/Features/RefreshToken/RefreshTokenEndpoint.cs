using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Identity.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.RefreshToken;

public static class RefreshTokenEndpoint
{
    public static RouteGroupBuilder MapRefreshTokenEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/refresh", async (
                RefreshTokenRequest request,
                RefreshTokenHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var refresh = TokenTransport.GetRefreshToken(httpContext, request.RefreshToken);
                var result = await handler.HandleAsync(request, refresh, cancellationToken);
                return result.ToResult(tokens =>
                {
                    if (TokenTransport.IsWebClient(httpContext))
                    {
                        TokenTransport.SetRefreshCookie(httpContext, tokens.RefreshToken!, tokens.RefreshExpiresAt);
                        return Results.Ok(tokens with { RefreshToken = null });
                    }

                    return Results.Ok(tokens);
                });
            })
            .WithRequestValidation()
            .WithName("RefreshToken")
            .WithSummary(
                "Mint a new access token for the given persona context using the refresh session. " +
                "Use when the access token expires or when switching org/listener/platform persona.")
            .Produces<Features.Shared.AuthTokenResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
