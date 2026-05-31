using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.DeleteOrganization;

public static class DeleteOrganizationEndpoint
{
    public static RouteGroupBuilder MapDeleteOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/organizations/{organizationId:guid}", async (
                Guid organizationId,
                DeleteOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization(OrgPolicies.ManageOrg)
            .RequireOrgTenant()
            .WithName("DeleteOrganization")
            .WithSummary("Soft-delete an organization (owner only).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
