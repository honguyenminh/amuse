using Amuse.Domain.Billing.Withdrawals;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public sealed class WithdrawalRequest
{
    public const int MaxTransferReferenceLength = 256;
    public const int MaxProofObjectKeyLength = 512;

    public WithdrawalRequestId Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public WithdrawalStatus Status { get; private set; }
    public FxRateId? FxRateId { get; private set; }
    public string? TransferReference { get; private set; }
    public string? ProofObjectKey { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }

    private WithdrawalRequestState? _state;

    private WithdrawalRequestState State => _state ??= WithdrawalRequestStates.From(Status);

    private WithdrawalRequest()
    {
    }

    public static Result<WithdrawalRequest> CreateForManualRail(
        OrganizationId organizationId,
        Money amount,
        FxRateId? fxRateId,
        DateTimeOffset now) =>
        Create(
            organizationId,
            amount,
            fxRateId,
            WithdrawalStatus.PendingApproval,
            PendingApprovalWithdrawalState.Instance,
            now);

    public static Result<WithdrawalRequest> CreateForStripeRail(
        OrganizationId organizationId,
        Money amount,
        FxRateId? fxRateId,
        bool autoApproved,
        DateTimeOffset now)
    {
        if (autoApproved)
        {
            return Create(
                organizationId,
                amount,
                fxRateId,
                WithdrawalStatus.Approved,
                ApprovedWithdrawalState.Instance,
                now);
        }

        return Create(
            organizationId,
            amount,
            fxRateId,
            WithdrawalStatus.PendingApproval,
            PendingApprovalWithdrawalState.Instance,
            now);
    }

    private static Result<WithdrawalRequest> Create(
        OrganizationId organizationId,
        Money amount,
        FxRateId? fxRateId,
        WithdrawalStatus status,
        WithdrawalRequestState state,
        DateTimeOffset now)
    {
        if (amount.IsZero)
            return Result<WithdrawalRequest>.Failure(BillingErrors.WithdrawalBelowMinimum);

        return Result<WithdrawalRequest>.Success(new WithdrawalRequest
        {
            Id = WithdrawalRequestId.New(),
            OrganizationId = organizationId,
            AmountMinor = amount.AmountMinor,
            Currency = amount.Currency,
            Status = status,
            _state = state,
            FxRateId = fxRateId,
            RequestedAt = now,
        });
    }

    public Result MarkApproved() => State.Approve(this);

    public Result MarkProcessing() => State.BeginProcessing(this);

    public Result MarkCompleted(string transferReference, string? proofObjectKey, DateTimeOffset now) =>
        State.Complete(this, transferReference, proofObjectKey, now);

    public Result MarkFailed(DateTimeOffset now) => State.Fail(this, now);

    public static bool IsActiveStatus(WithdrawalStatus status) =>
        status is WithdrawalStatus.PendingApproval
            or WithdrawalStatus.Approved
            or WithdrawalStatus.Processing;

    internal void TransitionTo(WithdrawalRequestState next)
    {
        _state = next;
        Status = next.StatusValue;
    }

    internal Result ValidateCompletionFields(string transferReference, string? proofObjectKey)
    {
        var normalizedReference = transferReference.Trim();
        if (normalizedReference.Length is 0 or > MaxTransferReferenceLength)
            return Result.Failure(BillingErrors.WithdrawalInvalidTransferReference);

        if (proofObjectKey is not null)
        {
            var normalizedProof = proofObjectKey.Trim();
            if (normalizedProof.Length is 0 or > MaxProofObjectKeyLength)
                return Result.Failure(BillingErrors.WithdrawalInvalidProofObjectKey);
        }

        return Result.Success();
    }

    internal void ApplyCompletion(string transferReference, string? proofObjectKey, DateTimeOffset now)
    {
        TransferReference = transferReference.Trim();
        if (proofObjectKey is not null)
            ProofObjectKey = proofObjectKey.Trim();
        CompletedAt = now;
    }

    internal void SetFailedAt(DateTimeOffset now) => FailedAt = now;
}
