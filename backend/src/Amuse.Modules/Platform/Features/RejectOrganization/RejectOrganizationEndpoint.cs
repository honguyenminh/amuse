using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Platform.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.RejectOrganization;

public static class RejectOrganizationEndpoint
{
    public static RouteGroupBuilder MapRejectOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations/{organizationId:guid}/reject", async (
                Guid organizationId,
                RejectOrganizationRequest request,
                RejectOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    request,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization(PlatformPolicies.RequireOrganizationReview)
            .WithRequestValidation()
            .WithName("RejectOrganization")
            .WithSummary("Reject a backing organization application.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
