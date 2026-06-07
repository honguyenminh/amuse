using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Platform.Features.Common;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ForceTransferOwnership;

public static class ForceTransferOwnershipEndpoint
{
    public static RouteGroupBuilder MapForceTransferOwnershipEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations/{organizationId:guid}/force-transfer-ownership", async (
                Guid organizationId,
                TransferOwnershipRequest request,
                ForceTransferOwnershipHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    request,
                    httpContext.User,
                    cancellationToken);
                return PlatformResultExtensions.ToTenancyResult(result);
            })
            .RequireAuthorization(PlatformPolicies.RequireOrganizationManage)
            .WithRequestValidation()
            .WithName("ForceTransferOrganizationOwnership")
            .WithSummary("Force-transfer organization ownership (platform operators).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
