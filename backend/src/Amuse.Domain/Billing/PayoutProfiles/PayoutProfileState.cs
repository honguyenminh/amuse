using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing.PayoutProfiles;

internal abstract class PayoutProfileState
{
    public abstract PayoutVerificationStatus StatusValue { get; }

    public virtual Result ApplyDetails(PayoutProfile profile, bool materialChange, DateTimeOffset now) =>
        Result.Failure(BillingErrors.PayoutProfileUpdateLocked);

    public virtual Result Submit(PayoutProfile profile, DateTimeOffset now) =>
        Result.Failure(BillingErrors.PayoutProfileInvalidStatusTransition);

    public virtual Result EnterReview(PayoutProfile profile, DateTimeOffset now) =>
        Result.Failure(BillingErrors.PayoutProfileInvalidStatusTransition);

    public virtual Result Approve(PayoutProfile profile, AccountId verifiedBy, DateTimeOffset now) =>
        Result.Failure(BillingErrors.PayoutProfileInvalidStatusTransition);

    public virtual Result Reject(PayoutProfile profile, string reason, DateTimeOffset now) =>
        Result.Failure(BillingErrors.PayoutProfileInvalidStatusTransition);

    public virtual Result CompleteStripeVerification(PayoutProfile profile, DateTimeOffset now) =>
        Result.Failure(BillingErrors.PayoutProfileInvalidStatusTransition);

    protected static void Transition(PayoutProfile profile, PayoutProfileState next) =>
        profile.TransitionTo(next);
}

internal sealed class NotStartedPayoutProfileState : PayoutProfileState
{
    public static readonly NotStartedPayoutProfileState Instance = new();

    public override PayoutVerificationStatus StatusValue => PayoutVerificationStatus.NotStarted;

    public override Result ApplyDetails(PayoutProfile profile, bool materialChange, DateTimeOffset now) =>
        Result.Success();

    public override Result Submit(PayoutProfile profile, DateTimeOffset now)
    {
        if (!profile.IsCompleteForSubmission())
            return Result.Failure(BillingErrors.PayoutProfileIncomplete);

        Transition(profile, SubmittedPayoutProfileState.Instance);
        profile.ClearRejection();
        profile.Touch(now);
        return Result.Success();
    }

    public override Result CompleteStripeVerification(PayoutProfile profile, DateTimeOffset now)
    {
        Transition(profile, VerifiedPayoutProfileState.Instance);
        profile.ApplyStripeVerification(now);
        return Result.Success();
    }
}

internal sealed class SubmittedPayoutProfileState : PayoutProfileState
{
    public static readonly SubmittedPayoutProfileState Instance = new();

    public override PayoutVerificationStatus StatusValue => PayoutVerificationStatus.Submitted;

    public override Result EnterReview(PayoutProfile profile, DateTimeOffset now)
    {
        Transition(profile, UnderReviewPayoutProfileState.Instance);
        profile.Touch(now);
        return Result.Success();
    }

    public override Result CompleteStripeVerification(PayoutProfile profile, DateTimeOffset now)
    {
        Transition(profile, VerifiedPayoutProfileState.Instance);
        profile.ApplyStripeVerification(now);
        return Result.Success();
    }
}

internal sealed class UnderReviewPayoutProfileState : PayoutProfileState
{
    public static readonly UnderReviewPayoutProfileState Instance = new();

    public override PayoutVerificationStatus StatusValue => PayoutVerificationStatus.UnderReview;

    public override Result Approve(PayoutProfile profile, AccountId verifiedBy, DateTimeOffset now)
    {
        Transition(profile, VerifiedPayoutProfileState.Instance);
        profile.ApplyOperatorVerification(verifiedBy, now);
        return Result.Success();
    }

    public override Result Reject(PayoutProfile profile, string reason, DateTimeOffset now)
    {
        var normalizedReason = reason.Trim();
        if (normalizedReason.Length is 0 or > PayoutProfile.MaxRejectionReasonLength)
            return Result.Failure(BillingErrors.PayoutProfileInvalidRejectionReason);

        Transition(profile, RejectedPayoutProfileState.Instance);
        profile.ApplyRejection(normalizedReason, now);
        return Result.Success();
    }

    public override Result CompleteStripeVerification(PayoutProfile profile, DateTimeOffset now)
    {
        Transition(profile, VerifiedPayoutProfileState.Instance);
        profile.ApplyStripeVerification(now);
        return Result.Success();
    }
}

internal sealed class VerifiedPayoutProfileState : PayoutProfileState
{
    public static readonly VerifiedPayoutProfileState Instance = new();

    public override PayoutVerificationStatus StatusValue => PayoutVerificationStatus.Verified;

    public override Result ApplyDetails(PayoutProfile profile, bool materialChange, DateTimeOffset now)
    {
        if (materialChange)
        {
            Transition(profile, UnderReviewPayoutProfileState.Instance);
            profile.ClearVerification();
        }

        profile.Touch(now);
        return Result.Success();
    }

    public override Result CompleteStripeVerification(PayoutProfile profile, DateTimeOffset now) =>
        Result.Success();
}

internal sealed class RejectedPayoutProfileState : PayoutProfileState
{
    public static readonly RejectedPayoutProfileState Instance = new();

    public override PayoutVerificationStatus StatusValue => PayoutVerificationStatus.Rejected;

    public override Result ApplyDetails(PayoutProfile profile, bool materialChange, DateTimeOffset now)
    {
        profile.Touch(now);
        return Result.Success();
    }

    public override Result Submit(PayoutProfile profile, DateTimeOffset now)
    {
        if (!profile.IsCompleteForSubmission())
            return Result.Failure(BillingErrors.PayoutProfileIncomplete);

        Transition(profile, SubmittedPayoutProfileState.Instance);
        profile.ClearRejection();
        profile.Touch(now);
        return Result.Success();
    }
}

internal static class PayoutProfileStates
{
    public static PayoutProfileState From(PayoutVerificationStatus status) => status switch
    {
        PayoutVerificationStatus.NotStarted => NotStartedPayoutProfileState.Instance,
        PayoutVerificationStatus.Submitted => SubmittedPayoutProfileState.Instance,
        PayoutVerificationStatus.UnderReview => UnderReviewPayoutProfileState.Instance,
        PayoutVerificationStatus.Verified => VerifiedPayoutProfileState.Instance,
        PayoutVerificationStatus.Rejected => RejectedPayoutProfileState.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown payout verification status."),
    };
}
