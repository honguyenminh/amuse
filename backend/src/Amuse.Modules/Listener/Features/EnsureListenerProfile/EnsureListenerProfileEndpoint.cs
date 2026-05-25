using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Listener.Features.EnsureListenerProfile;

public static class EnsureListenerProfileEndpoint
{
    public static IEndpointRouteBuilder MapEnsureListenerProfileEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/listener/profile/ensure", async (
                EnsureListenerProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("EnsureListenerProfile")
            .WithSummary("Ensure a listener profile exists for the signed-in account.")
            .Produces<EnsureListenerProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
