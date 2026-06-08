using Amuse.Domain.Billing;
using Amuse.Modules.Billing.Features.Withdrawals;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Binding;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ManageWithdrawals;

public static class ManageWithdrawalsEndpoint
{
    public static RouteGroupBuilder MapManageWithdrawalsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/withdrawals", async (
                string? status,
                ListPlatformWithdrawalsHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!CamelCaseEnumQuery.TryParseWithdrawalStatus(status, out var parsedStatus))
                {
                    return Results.Problem(
                        title: BillingErrors.InvalidWithdrawalStatusFilter.Code,
                        detail: BillingErrors.InvalidWithdrawalStatusFilter.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        extensions: new Dictionary<string, object?>
                        {
                            ["code"] = BillingErrors.InvalidWithdrawalStatusFilter.Code,
                        });
                }

                var result = await handler.HandleAsync(parsedStatus, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequirePayoutManage)
            .WithName("ListPlatformWithdrawals")
            .WithSummary("List withdrawal requests for platform payout ops (DA1 manual rail).")
            .Produces<IReadOnlyList<PlatformWithdrawalRow>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/withdrawals/{withdrawalId:guid}/approve", async (
                Guid withdrawalId,
                ApproveWithdrawalHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(withdrawalId, cancellationToken);
                return result.ToResult();
            })
            .RequireAuthorization(PlatformPolicies.RequirePayoutManage)
            .WithName("ApproveWithdrawal")
            .WithSummary("Approve a pending withdrawal request.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/withdrawals/{withdrawalId:guid}/complete", async (
                Guid withdrawalId,
                CompleteWithdrawalRequest request,
                CompleteWithdrawalHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(withdrawalId, request, cancellationToken);
                return result.ToResult();
            })
            .RequireAuthorization(PlatformPolicies.RequirePayoutManage)
            .WithRequestValidation()
            .WithName("CompleteWithdrawal")
            .WithSummary("Mark withdrawal completed after manual bank transfer; posts payout journal.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/withdrawals/{withdrawalId:guid}/fail", async (
                Guid withdrawalId,
                FailWithdrawalHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(withdrawalId, cancellationToken);
                return result.ToResult();
            })
            .RequireAuthorization(PlatformPolicies.RequirePayoutManage)
            .WithName("FailWithdrawal")
            .WithSummary("Mark withdrawal failed and return reserved funds to available balance.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
