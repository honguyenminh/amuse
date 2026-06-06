using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.UpdatePlaylist;

public static class UpdatePlaylistEndpoint
{
    public static IEndpointRouteBuilder MapUpdatePlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/api/v1/discovery/playlists/{id:guid}", async (
                Guid id,
                UpdatePlaylistRequest request,
                UpdatePlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, request, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("UpdatePlaylist")
            .WithSummary("Update an owned playlist. Making a public playlist private cuts fork links and removes follows.")
            .Produces<PlaylistDetailDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
