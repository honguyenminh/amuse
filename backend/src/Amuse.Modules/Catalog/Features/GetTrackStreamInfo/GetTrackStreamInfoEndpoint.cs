using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.GetTrackStreamInfo;

public static class GetTrackStreamInfoEndpoint
{
    public static IEndpointRouteBuilder MapGetTrackStreamInfoEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/tracks/{trackId:guid}/stream-info", async (
                Guid trackId,
                GetTrackStreamInfoHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("GetCatalogTrackStreamInfo")
            .WithSummary(
                "Return a short-lived signed URL for streaming a track. " +
                "Returns problem `catalog.track_not_found` when the id is unknown or " +
                "`catalog.track_has_no_audio` when the master is not yet uploaded.")
            .Produces<TrackStreamInfoResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
