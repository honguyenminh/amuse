using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.TransferOwnership;

public static class TransferOwnershipEndpoint
{
    public static RouteGroupBuilder MapTransferOwnershipEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations/{organizationId:guid}/ownership/transfer", async (
                Guid organizationId,
                TransferOwnershipRequest request,
                TransferOwnershipHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    request,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization(OrgPolicies.ManageOrg)
            .RequireOrgTenant()
            .WithRequestValidation()
            .WithName("TransferOrganizationOwnership")
            .WithSummary("Transfer organization ownership to another active member.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
