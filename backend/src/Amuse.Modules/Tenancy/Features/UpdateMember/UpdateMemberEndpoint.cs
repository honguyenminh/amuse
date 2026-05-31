using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.UpdateMember;

public static class UpdateMemberEndpoint
{
    public static RouteGroupBuilder MapUpdateMemberEndpoint(this RouteGroupBuilder group)
    {
        group.MapPatch("/organizations/{organizationId:guid}/members/{memberId:guid}", async (
                Guid organizationId,
                Guid memberId,
                UpdateOrganizationMemberRequest request,
                UpdateMemberHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(organizationId, memberId, request, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManageMemberPermissions)
            .RequireOrgTenant()
            .WithRequestValidation()
            .WithName("UpdateOrganizationMember")
            .WithSummary("Update member preset label and claims.")
            .Produces<OrganizationMemberResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
