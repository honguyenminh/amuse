using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ScheduleRelease;

public static class ScheduleReleaseEndpoint
{
    public static IEndpointRouteBuilder MapScheduleReleaseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/releases/{releaseId:guid}/schedule", async (
                Guid releaseId,
                ScheduleReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.PublishCatalog)
            .WithName("ScheduleCatalogRelease")
            .WithSummary(
                "Schedule a release to publish automatically at releaseDate (UTC). Returns 400 with catalog.release_not_ready_to_schedule, catalog.release_date_not_in_future, or catalog.invalid_lifecycle_transition when scheduling is not allowed.")
            .Produces<ManageReleaseDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
