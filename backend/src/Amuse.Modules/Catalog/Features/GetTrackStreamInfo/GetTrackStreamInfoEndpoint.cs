using Amuse.Modules.Catalog.Features.Common;
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
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, httpContext.User, cancellationToken);
                return result.ToCatalogResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("GetCatalogTrackStreamInfo")
            .WithSummary(
                "Return a short-lived signed URL for streaming a track. " +
                "Owners (track or parent release purchase) receive the full rendition ladder including lossless; " +
                "other listeners on publicly published tracks are capped at ~128 kbps lossy renditions. " +
                "Includes optional `Loudness` metadata (integrated LUFS, true peak, and precomputed " +
                "`LinearGainLu`) for client-side volume normalization. " +
                "Returns problem `catalog.track_not_found` when the id is unknown, " +
                "`catalog.track_has_no_audio` when the master is not yet uploaded, " +
                "`catalog.track_stream_not_ready` when packaging has not finished, or " +
                "`catalog.stream_playback_forbidden` when the track is not publicly streamable and the caller does not own it.")
            .Produces<TrackStreamInfoResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
