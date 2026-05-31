using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Platform.Features.ListOrganizationApplications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ListClosedOrganizations;

public static class ListClosedOrganizationsEndpoint
{
    public static RouteGroupBuilder MapListClosedOrganizationsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/organizations/closed", async (
                ListClosedOrganizationsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequireOrganizationManage)
            .WithName("ListClosedOrganizations")
            .WithSummary("List soft-deleted (closed) organizations for platform recovery.")
            .Produces<IReadOnlyList<OrganizationApplicationResponse>>();

        return group;
    }
}
