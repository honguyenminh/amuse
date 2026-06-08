using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing.Withdrawals;

internal abstract class WithdrawalRequestState
{
    public abstract WithdrawalStatus StatusValue { get; }

    public virtual Result Approve(WithdrawalRequest withdrawal) =>
        Result.Failure(BillingErrors.InvalidWithdrawalStatusTransition);

    public virtual Result BeginProcessing(WithdrawalRequest withdrawal) =>
        Result.Failure(BillingErrors.InvalidWithdrawalStatusTransition);

    public virtual Result Complete(
        WithdrawalRequest withdrawal,
        string transferReference,
        string? proofObjectKey,
        DateTimeOffset now) =>
        Result.Failure(BillingErrors.InvalidWithdrawalStatusTransition);

    public virtual Result Fail(WithdrawalRequest withdrawal, DateTimeOffset now) =>
        Result.Failure(BillingErrors.InvalidWithdrawalStatusTransition);

    protected static void Transition(WithdrawalRequest withdrawal, WithdrawalRequestState next) =>
        withdrawal.TransitionTo(next);
}

internal sealed class RequestedWithdrawalState : WithdrawalRequestState
{
    public static readonly RequestedWithdrawalState Instance = new();

    public override WithdrawalStatus StatusValue => WithdrawalStatus.Requested;

    public override Result BeginProcessing(WithdrawalRequest withdrawal)
    {
        Transition(withdrawal, ProcessingWithdrawalState.Instance);
        return Result.Success();
    }

    public override Result Fail(WithdrawalRequest withdrawal, DateTimeOffset now)
    {
        Transition(withdrawal, FailedWithdrawalState.Instance);
        withdrawal.SetFailedAt(now);
        return Result.Success();
    }
}

internal sealed class PendingApprovalWithdrawalState : WithdrawalRequestState
{
    public static readonly PendingApprovalWithdrawalState Instance = new();

    public override WithdrawalStatus StatusValue => WithdrawalStatus.PendingApproval;

    public override Result Approve(WithdrawalRequest withdrawal)
    {
        Transition(withdrawal, ApprovedWithdrawalState.Instance);
        return Result.Success();
    }

    public override Result BeginProcessing(WithdrawalRequest withdrawal)
    {
        Transition(withdrawal, ProcessingWithdrawalState.Instance);
        return Result.Success();
    }

    public override Result Fail(WithdrawalRequest withdrawal, DateTimeOffset now)
    {
        Transition(withdrawal, FailedWithdrawalState.Instance);
        withdrawal.SetFailedAt(now);
        return Result.Success();
    }
}

internal sealed class ApprovedWithdrawalState : WithdrawalRequestState
{
    public static readonly ApprovedWithdrawalState Instance = new();

    public override WithdrawalStatus StatusValue => WithdrawalStatus.Approved;

    public override Result BeginProcessing(WithdrawalRequest withdrawal)
    {
        Transition(withdrawal, ProcessingWithdrawalState.Instance);
        return Result.Success();
    }

    public override Result Complete(
        WithdrawalRequest withdrawal,
        string transferReference,
        string? proofObjectKey,
        DateTimeOffset now)
    {
        var validation = withdrawal.ValidateCompletionFields(transferReference, proofObjectKey);
        if (!validation.IsSuccess)
            return validation;

        Transition(withdrawal, CompletedWithdrawalState.Instance);
        withdrawal.ApplyCompletion(transferReference, proofObjectKey, now);
        return Result.Success();
    }

    public override Result Fail(WithdrawalRequest withdrawal, DateTimeOffset now)
    {
        Transition(withdrawal, FailedWithdrawalState.Instance);
        withdrawal.SetFailedAt(now);
        return Result.Success();
    }
}

internal sealed class ProcessingWithdrawalState : WithdrawalRequestState
{
    public static readonly ProcessingWithdrawalState Instance = new();

    public override WithdrawalStatus StatusValue => WithdrawalStatus.Processing;

    public override Result Complete(
        WithdrawalRequest withdrawal,
        string transferReference,
        string? proofObjectKey,
        DateTimeOffset now)
    {
        var validation = withdrawal.ValidateCompletionFields(transferReference, proofObjectKey);
        if (!validation.IsSuccess)
            return validation;

        Transition(withdrawal, CompletedWithdrawalState.Instance);
        withdrawal.ApplyCompletion(transferReference, proofObjectKey, now);
        return Result.Success();
    }

    public override Result Fail(WithdrawalRequest withdrawal, DateTimeOffset now)
    {
        Transition(withdrawal, FailedWithdrawalState.Instance);
        withdrawal.SetFailedAt(now);
        return Result.Success();
    }
}

internal sealed class CompletedWithdrawalState : WithdrawalRequestState
{
    public static readonly CompletedWithdrawalState Instance = new();

    public override WithdrawalStatus StatusValue => WithdrawalStatus.Completed;
}

internal sealed class FailedWithdrawalState : WithdrawalRequestState
{
    public static readonly FailedWithdrawalState Instance = new();

    public override WithdrawalStatus StatusValue => WithdrawalStatus.Failed;
}

internal static class WithdrawalRequestStates
{
    public static WithdrawalRequestState From(WithdrawalStatus status) => status switch
    {
        WithdrawalStatus.Requested => RequestedWithdrawalState.Instance,
        WithdrawalStatus.PendingApproval => PendingApprovalWithdrawalState.Instance,
        WithdrawalStatus.Approved => ApprovedWithdrawalState.Instance,
        WithdrawalStatus.Processing => ProcessingWithdrawalState.Instance,
        WithdrawalStatus.Completed => CompletedWithdrawalState.Instance,
        WithdrawalStatus.Failed => FailedWithdrawalState.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown withdrawal status."),
    };
}
