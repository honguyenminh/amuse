using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ListPurchases;

public static class ListPlatformPurchasesEndpoint
{
    public static RouteGroupBuilder MapListPlatformPurchasesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/purchases", async (
                string? query,
                string? paymentStatus,
                int? limit,
                ListPlatformPurchasesHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(query, paymentStatus, limit, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequirePurchaseManage)
            .WithName("ListPlatformPurchases")
            .WithSummary("Search purchases for platform refund operations.")
            .Produces<IReadOnlyList<PlatformPurchaseRow>>();

        return group;
    }
}
