using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.RetryTrackTranscode;

public static class RetryTrackTranscodeEndpoint
{
    public static IEndpointRouteBuilder MapRetryTrackTranscodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/tracks/{trackId:guid}/ingestion/retry-transcode", async (
                Guid trackId,
                RetryTrackTranscodeHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.UploadCatalog)
            .WithName("RetryCatalogTrackTranscode")
            .WithSummary("Manually retry transcoding for a failed or stuck track ingestion job.")
            .Produces<RetryTrackTranscodeResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
