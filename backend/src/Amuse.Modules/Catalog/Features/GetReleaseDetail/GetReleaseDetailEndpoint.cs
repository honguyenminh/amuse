using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.GetReleaseDetail;

public static class GetReleaseDetailEndpoint
{
    public static IEndpointRouteBuilder MapGetReleaseDetailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/releases/{releaseId:guid}", async (
                Guid releaseId,
                GetReleaseDetailHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetCatalogReleaseDetail")
            .WithSummary("Return a release with its tracks. Public; no authentication required. Returns problem `catalog.release_not_found` if missing.")
            .Produces<GetReleaseDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
