using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.Withdrawals;

internal sealed class ListWithdrawalsHandler(BillingDbContext billingDb)
{
    public async Task<Result<IReadOnlyList<WithdrawalRow>>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<IReadOnlyList<WithdrawalRow>>.Failure(orgResult.Error!);

        var rows = await billingDb.WithdrawalRequests.AsNoTracking()
            .Where(withdrawal => withdrawal.OrganizationId == orgResult.Value)
            .OrderByDescending(withdrawal => withdrawal.RequestedAt)
            .Select(withdrawal => new WithdrawalRow(
                withdrawal.Id.Value,
                withdrawal.AmountMinor,
                withdrawal.Currency,
                withdrawal.Status,
                withdrawal.TransferReference,
                withdrawal.ProofObjectKey,
                withdrawal.RequestedAt,
                withdrawal.CompletedAt,
                withdrawal.FailedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WithdrawalRow>>.Success(rows);
    }
}
