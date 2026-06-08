using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ApprovePayoutProfile;

public static class ApprovePayoutProfileEndpoint
{
    public static RouteGroupBuilder MapApprovePayoutProfileEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/payout-profiles/{organizationId:guid}/approve", async (
                Guid organizationId,
                ApprovePayoutProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(organizationId, httpContext.User, cancellationToken);
                return result.ToResult();
            })
            .RequireAuthorization(PlatformPolicies.RequirePayoutManage)
            .WithName("ApprovePayoutProfile")
            .WithSummary("Approve an organization payout profile (Gate B).")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
