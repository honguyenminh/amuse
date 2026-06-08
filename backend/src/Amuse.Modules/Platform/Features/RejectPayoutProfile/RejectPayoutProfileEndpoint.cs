using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.RejectPayoutProfile;

public static class RejectPayoutProfileEndpoint
{
    public static RouteGroupBuilder MapRejectPayoutProfileEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/payout-profiles/{organizationId:guid}/reject", async (
                Guid organizationId,
                RejectPayoutProfileRequest request,
                RejectPayoutProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    request,
                    httpContext.User,
                    cancellationToken);
                return result.ToResult();
            })
            .RequireAuthorization(PlatformPolicies.RequirePayoutManage)
            .WithRequestValidation()
            .WithName("RejectPayoutProfile")
            .WithSummary("Reject an organization payout profile with a reason.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
