using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.RemoveMember;

public static class RemoveMemberEndpoint
{
    public static RouteGroupBuilder MapRemoveMemberEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/organizations/{organizationId:guid}/members/{memberId:guid}", async (
                Guid organizationId,
                Guid memberId,
                RemoveMemberHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    memberId,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization(OrgPolicies.ManageMembership)
            .RequireOrgTenant()
            .WithName("RemoveOrganizationMember")
            .WithSummary("Remove an organization member.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
