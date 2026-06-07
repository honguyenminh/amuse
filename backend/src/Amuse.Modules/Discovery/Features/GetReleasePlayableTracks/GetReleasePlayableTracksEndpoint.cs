using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.GetReleasePlayableTracks;

public static class GetReleasePlayableTracksEndpoint
{
    public static IEndpointRouteBuilder MapGetReleasePlayableTracksEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/playables/release/{id:guid}/tracks", async (
                Guid id,
                GetReleasePlayableTracksHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetReleasePlayableTracks")
            .WithSummary("Get ordered playable tracks for a published release.")
            .Produces<PlayableTracksResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
