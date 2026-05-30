using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.GetOrganization;

public static class GetOrganizationEndpoint
{
    public static RouteGroupBuilder MapGetOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/organizations/{organizationId:guid}", async (
                Guid organizationId,
                GetOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(organizationId, httpContext.User, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("GetOrganization")
            .WithSummary("Get organization profile for a membership the caller belongs to.")
            .Produces<OrganizationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
