using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.ListOrganizationAudit;

public static class ListOrganizationAuditsEndpoint
{
    public static RouteGroupBuilder MapListOrganizationAuditsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/organizations/{organizationId:guid}/audit", async (
                Guid organizationId,
                ListOrganizationAuditsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadOrg)
            .RequireOrgTenant()
            .WithName("ListOrganizationAudit")
            .WithSummary("List audit history for the active organization profile.")
            .Produces<TenancyAuditListResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
