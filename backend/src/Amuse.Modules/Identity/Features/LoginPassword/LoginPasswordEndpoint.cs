using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.LoginPassword;

public static class LoginPasswordEndpoint
{
    public static RouteGroupBuilder MapLoginPasswordEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/login/password", async (
                LoginPasswordRequest request,
                LoginPasswordHandler handler,
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
            .WithName("LoginPassword")
            .WithSummary("Sign in with email and password.")
            .Produces<AuthTokenResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
