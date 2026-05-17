using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.GetCurrentAccount;

public static class GetCurrentAccountEndpoint
{
    public static RouteGroupBuilder MapGetCurrentAccountEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/me", async (
                GetCurrentAccountHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("GetCurrentAccount")
            .WithSummary("Get current account profile.")
            .Produces<GetCurrentAccountResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
