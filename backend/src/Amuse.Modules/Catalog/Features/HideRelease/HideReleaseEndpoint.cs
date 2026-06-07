using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.HideRelease;

public static class HideReleaseEndpoint
{
    public static IEndpointRouteBuilder MapHideReleaseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/releases/{releaseId:guid}/hide", async (
                Guid releaseId,
                HideReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithName("HideCatalogRelease")
            .WithSummary("Hide a published release and its tracks from public catalog.")
            .Produces<ManageReleaseDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
