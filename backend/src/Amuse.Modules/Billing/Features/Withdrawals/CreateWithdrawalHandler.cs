using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Features.Withdrawals;

internal sealed class CreateWithdrawalHandler(
    BillingDbContext billingDb,
    ILedgerBalanceReadModel ledgerBalance,
    IFxRateReadModel fxRates,
    IClock clock,
    IOptions<WithdrawalAutoApproveConfig> withdrawalConfig,
    StripeWithdrawalExecutionService stripeWithdrawalExecution)
{
    public async Task<Result<WithdrawalRow>> HandleAsync(
        CreateWithdrawalRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(orgResult.Error!);

        var orgId = orgResult.Value!;
        var now = clock.UtcNow;

        var moneyResult = Money.Create(request.AmountMinor, request.Currency);
        if (!moneyResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(moneyResult.Error!);

        var profile = await billingDb.PayoutProfiles
            .SingleOrDefaultAsync(p => p.OrganizationId == orgId, cancellationToken);

        if (profile is null)
            return Result<WithdrawalRow>.Failure(BillingErrors.PayoutProfileNotFound);

        var gateBResult = WithdrawalRules.ValidateGateB(profile.IsVerified, profile.BlocksWithdrawals);
        if (!gateBResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(gateBResult.Error!);

        var hasReceivable = await billingDb.SellerReceivables.AsNoTracking()
            .AnyAsync(
                receivable => receivable.OrganizationId == orgId && !receivable.IsSettled,
                cancellationToken);

        var receivableResult = WithdrawalRules.ValidateNoReceivable(hasReceivable);
        if (!receivableResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(receivableResult.Error!);

        var hasActive = await billingDb.WithdrawalRequests.AsNoTracking()
            .AnyAsync(
                withdrawal => withdrawal.OrganizationId == orgId
                    && (withdrawal.Status == WithdrawalStatus.PendingApproval
                        || withdrawal.Status == WithdrawalStatus.Approved
                        || withdrawal.Status == WithdrawalStatus.Processing),
                cancellationToken);

        var activeResult = WithdrawalRules.ValidateNoActiveWithdrawal(hasActive);
        if (!activeResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(activeResult.Error!);

        var lastCompleted = await billingDb.WithdrawalRequests.AsNoTracking()
            .Where(withdrawal =>
                withdrawal.OrganizationId == orgId
                && withdrawal.Status == WithdrawalStatus.Completed
                && withdrawal.CompletedAt != null)
            .OrderByDescending(withdrawal => withdrawal.CompletedAt)
            .Select(withdrawal => withdrawal.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var cooldownResult = WithdrawalRules.ValidateCooldown(
            lastCompleted,
            now,
            withdrawalConfig.Value.CooldownDays);
        if (!cooldownResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(cooldownResult.Error!);

        var fxResult = await fxRates.GetUsdEquivalentAsync(
            moneyResult.Value!.Currency,
            moneyResult.Value!.AmountMinor,
            cancellationToken);
        if (!fxResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(fxResult.Error!);

        var minimumResult = WithdrawalRules.ValidateMinimumUsdEquivalent(fxResult.Value!.UsdEquivalentMinor);
        if (!minimumResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(minimumResult.Error!);

        var balanceSnapshot = await ledgerBalance.GetBalanceAsync(orgId, cancellationToken);
        var currencyBalance = balanceSnapshot.Balances
            .FirstOrDefault(balance => string.Equals(balance.Currency, moneyResult.Value!.Currency, StringComparison.Ordinal));

        var availableMinor = currencyBalance?.AvailableMinor ?? 0;
        var availableResult = WithdrawalRules.ValidateAvailableBalance(
            moneyResult.Value!.AmountMinor,
            availableMinor);
        if (!availableResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(availableResult.Error!);

        var autoApproved = WithdrawalRules.ShouldAutoApproveStripeWithdrawal(
            profile.PayoutRail,
            fxResult.Value!.UsdEquivalentMinor,
            withdrawalConfig.Value.MaxAutoApproveUsdMinor);

        var withdrawalResult = profile.PayoutRail == PayoutRail.StripeGlobal
            ? WithdrawalRequest.CreateForStripeRail(
                orgId,
                moneyResult.Value!,
                fxResult.Value!.Rate.Id,
                autoApproved,
                now)
            : WithdrawalRequest.CreateForManualRail(
                orgId,
                moneyResult.Value!,
                fxResult.Value!.Rate.Id,
                now);
        if (!withdrawalResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(withdrawalResult.Error!);

        var reserveResult = JournalPoster.PostWithdrawalReserve(
            withdrawalResult.Value!.Id,
            orgId,
            moneyResult.Value!,
            now);
        if (!reserveResult.IsSuccess)
            return Result<WithdrawalRow>.Failure(reserveResult.Error!);

        billingDb.WithdrawalRequests.Add(withdrawalResult.Value!);
        billingDb.LedgerJournals.Add(reserveResult.Value!);
        await billingDb.SaveChangesAsync(cancellationToken);

        if (profile.PayoutRail == PayoutRail.StripeGlobal
            && autoApproved
            && !string.IsNullOrWhiteSpace(profile.ExternalRecipientId))
        {
            var executeResult = await stripeWithdrawalExecution.TryExecuteAsync(
                withdrawalResult.Value!,
                profile.ExternalRecipientId!,
                cancellationToken);

            if (!executeResult.IsSuccess)
                return Result<WithdrawalRow>.Failure(executeResult.Error!);
        }

        return Result<WithdrawalRow>.Success(ToRow(withdrawalResult.Value!));
    }

    internal static WithdrawalRow ToRow(WithdrawalRequest withdrawal) =>
        new(
            withdrawal.Id.Value,
            withdrawal.AmountMinor,
            withdrawal.Currency,
            withdrawal.Status,
            withdrawal.TransferReference,
            withdrawal.ProofObjectKey,
            withdrawal.RequestedAt,
            withdrawal.CompletedAt,
            withdrawal.FailedAt);
}
