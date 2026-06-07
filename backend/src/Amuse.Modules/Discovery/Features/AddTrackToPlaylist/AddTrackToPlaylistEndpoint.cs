using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.AddTrackToPlaylist;

public static class AddTrackToPlaylistEndpoint
{
    public static IEndpointRouteBuilder MapAddTrackToPlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/discovery/playlists/{id:guid}/items", async (
                Guid id,
                AddPlaylistItemRequest request,
                AddTrackToPlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, request, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("AddTrackToPlaylist")
            .WithSummary("Add a playable track to an owned playlist.")
            .Produces<AddPlaylistItemResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
