using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.Withdrawals;

public static class WithdrawalsEndpoint
{
    public static IEndpointRouteBuilder MapWithdrawalsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/billing/withdrawals", async (
                ListWithdrawalsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadPayout)
            .WithName("ListWithdrawals")
            .WithSummary("List withdrawal requests for the organization.")
            .Produces<IReadOnlyList<WithdrawalRow>>();

        endpoints.MapPost("/api/v1/billing/withdrawals", async (
                CreateWithdrawalRequest request,
                CreateWithdrawalHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManagePayoutWithdraw)
            .WithRequestValidation()
            .WithName("CreateWithdrawal")
            .WithSummary(
                "Request a withdrawal (DA1 manual rail: pending platform approval). Validates Gate B, cooldown, minimum USD equivalent, and available balance.")
            .Produces<WithdrawalRow>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
