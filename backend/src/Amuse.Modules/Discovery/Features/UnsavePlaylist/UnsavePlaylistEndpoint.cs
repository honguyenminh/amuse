using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.UnsavePlaylist;

public static class UnsavePlaylistEndpoint
{
    public static IEndpointRouteBuilder MapUnsavePlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/v1/discovery/playlists/{id:guid}/save", async (
                Guid id,
                UnsavePlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("UnsavePlaylist")
            .WithSummary("Remove a saved playlist from the listener library.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
