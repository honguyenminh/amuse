using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.ListMyPurchases;

public static class ListMyPurchasesEndpoint
{
    public static IEndpointRouteBuilder MapListMyPurchasesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/billing/purchases/me", async (
                ListMyPurchasesHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("ListMyPurchases")
            .WithSummary("List the signed-in listener's purchased tracks and releases.")
            .Produces<MyPurchasesResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
