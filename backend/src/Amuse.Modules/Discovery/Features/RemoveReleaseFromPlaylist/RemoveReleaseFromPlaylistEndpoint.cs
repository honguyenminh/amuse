using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.RemoveReleaseFromPlaylist;

public static class RemoveReleaseFromPlaylistEndpoint
{
    public static IEndpointRouteBuilder MapRemoveReleaseFromPlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/v1/discovery/playlists/{id:guid}/releases/{releaseId:guid}", async (
                Guid id,
                Guid releaseId,
                RemoveReleaseFromPlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, releaseId, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("RemoveReleaseFromPlaylist")
            .WithSummary("Remove all tracks from a release in an owned playlist.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
