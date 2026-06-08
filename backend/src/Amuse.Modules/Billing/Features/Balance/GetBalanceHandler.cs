using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Features.Balance;

internal sealed class GetBalanceHandler(
    BillingDbContext billingDb,
    ILedgerBalanceReadModel ledgerBalance,
    IClock clock,
    IOptions<WithdrawalAutoApproveConfig> withdrawalConfig)
{
    public async Task<Result<OrgBalanceResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<OrgBalanceResponse>.Failure(orgResult.Error!);

        var orgId = orgResult.Value!;
        var snapshot = await ledgerBalance.GetBalanceAsync(orgId, cancellationToken);

        var profile = await billingDb.PayoutProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrganizationId == orgId, cancellationToken);

        var gateBVerified = profile?.IsVerified ?? false;
        var blocksWithdrawals = profile?.BlocksWithdrawals ?? true;

        var hasReceivable = snapshot.Balances.Any(balance => balance.ReceivableMinor > 0);

        var lastCompleted = await billingDb.WithdrawalRequests.AsNoTracking()
            .Where(request =>
                request.OrganizationId == orgId
                && request.Status == WithdrawalStatus.Completed
                && request.CompletedAt != null)
            .OrderByDescending(request => request.CompletedAt)
            .Select(request => request.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        DateTimeOffset? cooldownEndsAt = null;
        if (lastCompleted is not null)
        {
            var endsAt = lastCompleted.Value.AddDays(withdrawalConfig.Value.CooldownDays);
            if (clock.UtcNow < endsAt)
                cooldownEndsAt = endsAt;
        }

        var rows = snapshot.Balances
            .Select(balance => new CurrencyBalanceRow(
                balance.Currency,
                balance.PendingMinor,
                balance.AvailableMinor,
                balance.InPayoutMinor,
                balance.ReceivableMinor,
                balance.UsdEquivalentMinor))
            .ToArray();

        return Result<OrgBalanceResponse>.Success(new OrgBalanceResponse(
            rows,
            gateBVerified,
            blocksWithdrawals,
            cooldownEndsAt,
            hasReceivable));
    }
}
