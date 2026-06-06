using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.ReorderPlaylistItems;

public static class ReorderPlaylistItemsEndpoint
{
    public static IEndpointRouteBuilder MapReorderPlaylistItemsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/api/v1/discovery/playlists/{id:guid}/items/reorder", async (
                Guid id,
                ReorderPlaylistItemsRequest request,
                ReorderPlaylistItemsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, request, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("ReorderPlaylistItems")
            .WithSummary("Reorder a track within an owned playlist.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
