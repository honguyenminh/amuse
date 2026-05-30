using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Platform.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ApproveOrganization;

public static class ApproveOrganizationEndpoint
{
    public static RouteGroupBuilder MapApproveOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations/{organizationId:guid}/approve", async (
                Guid organizationId,
                ApproveOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(organizationId, httpContext.User, cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization(PlatformPolicies.RequireOrganizationReview)
            .WithName("ApproveOrganization")
            .WithSummary("Approve a backing organization application.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
