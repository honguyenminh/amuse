using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Billing;

public sealed class PayoutProfileTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
    private static readonly OrganizationId OrganizationId = OrganizationId.New();
    private static readonly AccountId OperatorId = AccountId.New();

    [Fact]
    public void Submit_transitions_not_started_to_under_review_via_submitted()
    {
        var profile = CreateProfileWithCompleteDetails(PayoutVerificationStatus.NotStarted);

        Assert.True(profile.Submit(Now).IsSuccess);
        Assert.Equal(PayoutVerificationStatus.Submitted, profile.VerificationStatus);

        Assert.True(profile.EnterReview(Now).IsSuccess);
        Assert.Equal(PayoutVerificationStatus.UnderReview, profile.VerificationStatus);
    }

    [Fact]
    public void Approve_and_reject_require_under_review()
    {
        var profile = CreateProfileWithCompleteDetails(PayoutVerificationStatus.UnderReview);

        Assert.True(profile.Approve(OperatorId, Now).IsSuccess);
        Assert.Equal(PayoutVerificationStatus.Verified, profile.VerificationStatus);
        Assert.False(profile.BlocksWithdrawals);

        var rejected = CreateProfileWithCompleteDetails(PayoutVerificationStatus.UnderReview);
        Assert.True(rejected.Reject("Missing documents", Now).IsSuccess);
        Assert.Equal(PayoutVerificationStatus.Rejected, rejected.VerificationStatus);
        Assert.True(rejected.BlocksWithdrawals);
    }

    [Fact]
    public void Material_change_after_verified_moves_to_under_review()
    {
        var profile = CreateProfileWithCompleteDetails(PayoutVerificationStatus.UnderReview);
        profile.Approve(OperatorId, Now);

        var update = BuildDetails(profile, legalName: "Updated Legal Name LLC");
        var result = profile.ApplyDetails(update, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(PayoutVerificationStatus.UnderReview, profile.VerificationStatus);
        Assert.Null(profile.VerifiedAt);
        Assert.True(profile.BlocksWithdrawals);
    }

    [Fact]
    public void Submit_rejects_incomplete_profile()
    {
        var profile = Amuse.Domain.Billing.PayoutProfile.CreateDraft(
            OrganizationId,
            LegalEntityType.Individual,
            "Artist Name",
            Now).Value!;

        var result = profile.Submit(Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.PayoutProfileIncomplete, result.Error);
    }

    [Fact]
    public void ApplyDetails_locked_while_under_review()
    {
        var profile = CreateProfileWithCompleteDetails(PayoutVerificationStatus.UnderReview);
        var update = BuildDetails(profile);

        var result = profile.ApplyDetails(update, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.PayoutProfileUpdateLocked, result.Error);
    }

    [Fact]
    public void CompleteStripeVerification_transitions_submitted_to_verified()
    {
        var profile = CreateProfileWithCompleteDetails(PayoutVerificationStatus.NotStarted);
        profile.Submit(Now);

        Assert.True(profile.CompleteStripeVerification(Now).IsSuccess);
        Assert.Equal(PayoutVerificationStatus.Verified, profile.VerificationStatus);
        Assert.False(profile.BlocksWithdrawals);
    }

    [Fact]
    public void Rehydrated_status_resolves_state_after_ef_load()
    {
        var profile = CreateProfileWithCompleteDetails(PayoutVerificationStatus.UnderReview);

        typeof(Amuse.Domain.Billing.PayoutProfile)
            .GetField("_state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(profile, null);

        Assert.True(profile.Reject("Needs more docs", Now).IsSuccess);
        Assert.Equal(PayoutVerificationStatus.Rejected, profile.VerificationStatus);
    }

    private static Amuse.Domain.Billing.PayoutProfile CreateProfileWithCompleteDetails(
        PayoutVerificationStatus status)
    {
        var profile = Amuse.Domain.Billing.PayoutProfile.CreateDraft(
            OrganizationId,
            LegalEntityType.Individual,
            "Artist Name",
            Now).Value!;

        var update = BuildDetails(profile);
        profile.ApplyDetails(update, Now);

        if (status == PayoutVerificationStatus.NotStarted)
            return profile;

        profile.Submit(Now);
        profile.EnterReview(Now);

        if (status == PayoutVerificationStatus.UnderReview)
            return profile;

        if (status == PayoutVerificationStatus.Verified)
            profile.Approve(OperatorId, Now);

        return profile;
    }

    private static PayoutProfileDetailsUpdate BuildDetails(
        Amuse.Domain.Billing.PayoutProfile profile,
        string? legalName = null) =>
        new(
            profile.LegalEntityType,
            legalName ?? profile.LegalName,
            "123 Main Street",
            null,
            "Ho Chi Minh City",
            null,
            "700000",
            "VN",
            "protected-tax-id",
            null,
            PayoutRail.ManualBank,
            "protected-bank-account",
            "1234",
            "Vietcombank",
            ["billing/payout-docs/example-id.pdf"]);
}
