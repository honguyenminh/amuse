using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.GetPlaylist;

public static class GetPlaylistEndpoint
{
    public static IEndpointRouteBuilder MapGetPlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/playlists/{id:guid}", async (
                Guid id,
                GetPlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetPlaylist")
            .WithSummary("Get playlist details. Public playlists are visible to everyone; private playlists require owner or share-grant access.")
            .Produces<PlaylistDetailDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
