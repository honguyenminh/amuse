using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.ListInvites;

public static class ListInvitesEndpoint
{
    public static RouteGroupBuilder MapListInvitesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/organizations/{organizationId:guid}/members/invites", async (
                Guid organizationId,
                ListInvitesHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(organizationId, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadMembership)
            .RequireOrgTenant()
            .WithName("ListOrganizationInvites")
            .WithSummary("List pending organization invites.")
            .Produces<IReadOnlyList<OrganizationInviteResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
