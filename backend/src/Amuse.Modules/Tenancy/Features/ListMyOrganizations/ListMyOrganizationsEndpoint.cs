using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.ListMyOrganizations;

public static class ListMyOrganizationsEndpoint
{
    public static RouteGroupBuilder MapListMyOrganizationsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/organizations", async (
                ListMyOrganizationsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("ListMyOrganizations")
            .WithSummary("List organizations the signed-in account belongs to.")
            .Produces<IReadOnlyList<OrganizationResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
