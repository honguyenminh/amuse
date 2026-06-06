using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.GetPlaylistPlayableTracks;

public static class GetPlaylistPlayableTracksEndpoint
{
    public static IEndpointRouteBuilder MapGetPlaylistPlayableTracksEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/playables/playlist/{id:guid}/tracks", async (
                Guid id,
                GetPlaylistPlayableTracksHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetPlaylistPlayableTracks")
            .WithSummary("Get ordered playable tracks for a playlist.")
            .Produces<PlayableTracksResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
