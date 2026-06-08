using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.DownloadTrack;

public static class DownloadTrackEndpoint
{
    public static IEndpointRouteBuilder MapDownloadTrackEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/billing/downloads/tracks/{trackId:guid}", async (
                Guid trackId,
                DownloadTrackHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("DownloadOwnedTrack")
            .WithSummary(
                "Return a short-lived signed URL to download the track master for an owned track or release purchase.")
            .Produces<TrackDownloadResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
