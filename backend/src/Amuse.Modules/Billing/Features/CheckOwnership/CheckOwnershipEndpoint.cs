using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.CheckOwnership;

public static class CheckOwnershipEndpoint
{
    public static IEndpointRouteBuilder MapCheckOwnershipEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/billing/entitlements/ownership", async (
                Guid? trackId,
                Guid? releaseId,
                CheckOwnershipHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    trackId,
                    releaseId,
                    httpContext.User,
                    cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("CheckOwnership")
            .WithSummary(
                "Check whether the signed-in listener owns a track and/or release. " +
                "For track checks, provide both trackId and releaseId.")
            .Produces<OwnershipCheckResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
