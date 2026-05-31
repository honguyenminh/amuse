using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.LeaveOrganization;

public static class LeaveOrganizationEndpoint
{
    public static RouteGroupBuilder MapLeaveOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations/{organizationId:guid}/membership/leave", async (
                Guid organizationId,
                LeaveOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization()
            .RequireOrgTenant()
            .WithName("LeaveOrganization")
            .WithSummary("Leave the organization as the signed-in member.")
            .WithDescription(
                "Soft-removes the caller's active membership. Organization owners cannot leave; transfer ownership first.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
