using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.UnlikeTrack;

public static class UnlikeTrackEndpoint
{
    public static IEndpointRouteBuilder MapUnlikeTrackEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/v1/discovery/liked/{trackId:guid}", async (
                Guid trackId,
                UnlikeTrackHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("UnlikeTrack")
            .WithSummary("Remove a liked track.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
