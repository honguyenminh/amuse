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
                "Includes optional `Loudness` metadata (integrated LUFS, true peak, and precomputed " +
                "`LinearGainLu`) for client-side volume normalization. " +
                "Returns problem `catalog.track_not_found` when the id is unknown, " +
                "`catalog.track_has_no_audio` when the master is not yet uploaded, or " +
                "`catalog.track_stream_not_ready` when packaging has not finished.")
            .Produces<TrackStreamInfoResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
