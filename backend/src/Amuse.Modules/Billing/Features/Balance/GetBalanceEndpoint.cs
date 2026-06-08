using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.Balance;

public static class GetBalanceEndpoint
{
    public static IEndpointRouteBuilder MapGetBalanceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/billing/balance", async (
                GetBalanceHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadPayout)
            .WithName("GetOrgBalance")
            .WithSummary("Get per-currency seller balance (pending, available, in payout, receivable).")
            .Produces<OrgBalanceResponse>();

        return endpoints;
    }
}
