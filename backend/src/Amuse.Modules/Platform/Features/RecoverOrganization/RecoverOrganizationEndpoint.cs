using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Platform.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.RecoverOrganization;

public static class RecoverOrganizationEndpoint
{
    public static RouteGroupBuilder MapRecoverOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations/{organizationId:guid}/recover", async (
                Guid organizationId,
                RecoverOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(organizationId, httpContext.User, cancellationToken);
                return PlatformResultExtensions.ToTenancyResult(result);
            })
            .RequireAuthorization(PlatformPolicies.RequireOrganizationManage)
            .WithName("RecoverOrganization")
            .WithSummary("Recover a soft-deleted organization (platform operators).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
