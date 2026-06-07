using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.GetLikedPlayableTracks;

public static class GetLikedPlayableTracksEndpoint
{
    public static IEndpointRouteBuilder MapGetLikedPlayableTracksEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/playables/liked/tracks", async (
                GetLikedPlayableTracksHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("GetLikedPlayableTracks")
            .WithSummary("Expand the listener's liked collection into playable tracks.")
            .Produces<PlayableTracksResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
