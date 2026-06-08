using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing;

public static class WithdrawalRules
{
    public const long MinWithdrawalUsdMinor = 1_000;

    public static Result ValidateGateB(bool profileVerified, bool blocksWithdrawals)
    {
        if (!profileVerified || blocksWithdrawals)
            return Result.Failure(BillingErrors.PayoutProfileNotVerified);

        return Result.Success();
    }

    public static Result ValidateNoReceivable(bool hasOutstandingReceivable)
    {
        if (hasOutstandingReceivable)
            return Result.Failure(BillingErrors.WithdrawalReceivableOutstanding);

        return Result.Success();
    }

    public static Result ValidateCooldown(DateTimeOffset? lastCompletedAt, DateTimeOffset now, int cooldownDays)
    {
        if (lastCompletedAt is null)
            return Result.Success();

        var cooldownEndsAt = lastCompletedAt.Value.AddDays(cooldownDays);
        if (now < cooldownEndsAt)
            return Result.Failure(BillingErrors.WithdrawalCooldownActive);

        return Result.Success();
    }

    public static Result ValidateMinimumUsdEquivalent(long usdEquivalentMinor)
    {
        if (usdEquivalentMinor < MinWithdrawalUsdMinor)
            return Result.Failure(BillingErrors.WithdrawalBelowMinimum);

        return Result.Success();
    }

    public static Result ValidateAvailableBalance(long requestedMinor, long availableMinor)
    {
        if (requestedMinor > availableMinor)
            return Result.Failure(BillingErrors.WithdrawalInsufficientBalance);

        return Result.Success();
    }

    public static Result ValidateNoActiveWithdrawal(bool hasActiveWithdrawal)
    {
        if (hasActiveWithdrawal)
            return Result.Failure(BillingErrors.WithdrawalAlreadyInProgress);

        return Result.Success();
    }

    public static bool ShouldAutoApproveStripeWithdrawal(
        PayoutRail payoutRail,
        long usdEquivalentMinor,
        long maxAutoApproveUsdMinor) =>
        payoutRail == PayoutRail.StripeGlobal
        && usdEquivalentMinor <= maxAutoApproveUsdMinor;
}
