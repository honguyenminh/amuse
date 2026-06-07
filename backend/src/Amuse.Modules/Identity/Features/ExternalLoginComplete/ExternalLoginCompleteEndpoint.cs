using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.ExternalLoginComplete;

public static class ExternalLoginCompleteEndpoint
{
    public static RouteGroupBuilder MapExternalLoginCompleteEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/external/complete", async (
                ExternalLoginCompleteRequest request,
                ExternalLoginCompleteHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
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
            .WithName("ExternalLoginComplete")
            .WithSummary("Complete external OAuth/OIDC login.")
            .Produces<AuthTokenResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
