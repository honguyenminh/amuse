using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.DeletePlaylist;

public static class DeletePlaylistEndpoint
{
    public static IEndpointRouteBuilder MapDeletePlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/api/v1/discovery/playlists/{id:guid}", async (
                Guid id,
                DeletePlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("DeletePlaylist")
            .WithSummary("Delete an owned playlist and its items, shares, and follows.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
