using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.UnsaveRelease;

public static class UnsaveReleaseEndpoint
{
    public static IEndpointRouteBuilder MapUnsaveReleaseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/v1/discovery/library/releases/{releaseId:guid}", async (
                Guid releaseId,
                UnsaveReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("UnsaveRelease")
            .WithSummary("Remove a saved release from the listener library.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
