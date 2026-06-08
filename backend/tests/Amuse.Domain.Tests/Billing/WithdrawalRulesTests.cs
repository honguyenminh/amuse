using Amuse.Domain.Billing;

namespace Amuse.Domain.Tests.Billing;

public sealed class WithdrawalRulesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public void ValidateMinimumUsdEquivalent_rejects_below_ten_dollars()
    {
        var result = WithdrawalRules.ValidateMinimumUsdEquivalent(999);
        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalBelowMinimum.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateMinimumUsdEquivalent_accepts_ten_dollars()
    {
        var result = WithdrawalRules.ValidateMinimumUsdEquivalent(1_000);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateCooldown_rejects_when_within_seven_days()
    {
        var lastCompleted = Now.AddDays(-3);
        var result = WithdrawalRules.ValidateCooldown(lastCompleted, Now, cooldownDays: 7);
        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalCooldownActive.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateCooldown_allows_after_seven_days()
    {
        var lastCompleted = Now.AddDays(-8);
        var result = WithdrawalRules.ValidateCooldown(lastCompleted, Now, cooldownDays: 7);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateGateB_rejects_unverified_profile()
    {
        var result = WithdrawalRules.ValidateGateB(profileVerified: false, blocksWithdrawals: true);
        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.PayoutProfileNotVerified.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateNoReceivable_blocks_when_outstanding()
    {
        var result = WithdrawalRules.ValidateNoReceivable(hasOutstandingReceivable: true);
        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalReceivableOutstanding.Code, result.Error!.Code);
    }

    [Fact]
    public void ShouldAutoApproveStripeWithdrawal_auto_approves_stripe_global_under_threshold()
    {
        var result = WithdrawalRules.ShouldAutoApproveStripeWithdrawal(
            PayoutRail.StripeGlobal,
            usdEquivalentMinor: 500_000,
            maxAutoApproveUsdMinor: 500_000);

        Assert.True(result);
    }

    [Fact]
    public void ShouldAutoApproveStripeWithdrawal_rejects_stripe_global_above_threshold()
    {
        var result = WithdrawalRules.ShouldAutoApproveStripeWithdrawal(
            PayoutRail.StripeGlobal,
            usdEquivalentMinor: 500_001,
            maxAutoApproveUsdMinor: 500_000);

        Assert.False(result);
    }

    [Fact]
    public void ShouldAutoApproveStripeWithdrawal_never_auto_approves_manual_bank()
    {
        var result = WithdrawalRules.ShouldAutoApproveStripeWithdrawal(
            PayoutRail.ManualBank,
            usdEquivalentMinor: 1_000,
            maxAutoApproveUsdMinor: 500_000);

        Assert.False(result);
    }
}
