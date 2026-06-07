using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.ListMembers;

public static class ListMembersEndpoint
{
    public static RouteGroupBuilder MapListMembersEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/organizations/{organizationId:guid}/members", async (
                Guid organizationId,
                string? search,
                string? sortBy,
                string? sortDirection,
                int? page,
                int? pageSize,
                ListMembersHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = ListMembersQuery.From(search, sortBy, sortDirection, page, pageSize);
                var result = await handler.HandleAsync(organizationId, query, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadMembership)
            .RequireOrgTenant()
            .WithName("ListOrganizationMembers")
            .WithSummary("List active members of an organization with search, sort, and paging.")
            .Produces<OrganizationMemberListResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
