using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.ForkPlaylist;

public static class ForkPlaylistEndpoint
{
    public static IEndpointRouteBuilder MapForkPlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/discovery/playlists/{id:guid}/fork", async (
                Guid id,
                ForkPlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(response => Results.Created(
                    $"/api/v1/discovery/playlists/{response.Id}",
                    response));
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("ForkPlaylist")
            .WithSummary("Fork an accessible playlist into a new private playlist owned by the listener.")
            .Produces<PlaylistDetailDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
