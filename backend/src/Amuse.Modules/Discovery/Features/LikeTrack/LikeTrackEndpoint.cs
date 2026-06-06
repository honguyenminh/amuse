using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.LikeTrack;

public static class LikeTrackEndpoint
{
    public static IEndpointRouteBuilder MapLikeTrackEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/v1/discovery/liked/{trackId:guid}", async (
                Guid trackId,
                LikeTrackHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("LikeTrack")
            .WithSummary("Like a playable track.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
