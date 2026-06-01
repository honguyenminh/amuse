using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.UpdateOrganization;

public static class UpdateOrganizationEndpoint
{
    public static RouteGroupBuilder MapUpdateOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPatch("/organizations/{organizationId:guid}", async (
                Guid organizationId,
                UpdateOrganizationProfileRequest request,
                UpdateOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    request,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManageOrg)
            .RequireOrgTenant()
            .WithRequestValidation()
            .WithName("UpdateOrganizationProfile")
            .WithSummary("Update organization profile metadata for the active organization.")
            .Produces<OrganizationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
