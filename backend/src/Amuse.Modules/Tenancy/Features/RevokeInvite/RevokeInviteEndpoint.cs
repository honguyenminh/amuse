using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.RevokeInvite;

public static class RevokeInviteEndpoint
{
    public static RouteGroupBuilder MapRevokeInviteEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/organizations/{organizationId:guid}/members/invites/{inviteId:guid}", async (
                Guid organizationId,
                Guid inviteId,
                RevokeInviteHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(organizationId, inviteId, cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization(OrgPolicies.ManageMembership)
            .RequireOrgTenant()
            .WithName("RevokeOrganizationInvite")
            .WithSummary("Revoke a pending organization invite.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
