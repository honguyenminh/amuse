using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.ReplacePlaylistShares;

public static class ReplacePlaylistSharesEndpoint
{
    public static IEndpointRouteBuilder MapReplacePlaylistSharesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/v1/discovery/playlists/{id:guid}/shares", async (
                Guid id,
                ReplacePlaylistSharesRequest request,
                ReplacePlaylistSharesHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, request, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("ReplacePlaylistShares")
            .WithSummary("Replace share-grant emails on a private owned playlist.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
