using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.GetTrackIngestion;

public static class GetTrackIngestionEndpoint
{
    public static IEndpointRouteBuilder MapGetTrackIngestionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/tracks/{trackId:guid}/ingestion", async (
                Guid trackId,
                GetTrackIngestionHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("GetCatalogTrackIngestion")
            .WithSummary("Get track ingestion and transcode job status for the active organization.")
            .Produces<TrackIngestionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
