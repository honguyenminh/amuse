using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.RefundPurchase;

public static class RefundPurchaseEndpoint
{
    public static IEndpointRouteBuilder MapRefundPurchaseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/billing/purchases/{purchaseId:guid}/refund", async (
                Guid purchaseId,
                RefundPurchaseRequest request,
                RefundPurchaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    purchaseId,
                    request,
                    httpContext.User,
                    cancellationToken);

                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(BillingPolicies.RefundPurchase)
            .WithRequestValidation()
            .WithName("RefundPurchase")
            .WithSummary(
                "Refund a paid purchase. Seller orgs need manage:purchase:refund:all and payee allocation. Platform operators need manage:platform:purchases:all and set refundFeeBearer.")
            .Produces<RefundPurchaseResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
