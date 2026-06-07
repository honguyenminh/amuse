using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.CreatePlaylist;

public static class CreatePlaylistEndpoint
{
    public static IEndpointRouteBuilder MapCreatePlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/discovery/playlists", async (
                CreatePlaylistRequest request,
                CreatePlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(response => Results.Created(
                    $"/api/v1/discovery/playlists/{response.Id}",
                    response));
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("CreatePlaylist")
            .WithSummary("Create a new playlist owned by the authenticated listener.")
            .Produces<PlaylistDetailDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
