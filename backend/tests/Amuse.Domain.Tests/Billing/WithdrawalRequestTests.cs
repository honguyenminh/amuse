using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Billing;

public sealed class WithdrawalRequestTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
    private static readonly OrganizationId OrganizationId = OrganizationId.New();

    [Fact]
    public void CreateForManualRail_starts_in_pending_approval()
    {
        var withdrawal = CreateWithdrawal();

        Assert.Equal(WithdrawalStatus.PendingApproval, withdrawal.Status);
    }

    [Fact]
    public void MarkApproved_transitions_from_pending_approval()
    {
        var withdrawal = CreateWithdrawal();

        Assert.True(withdrawal.MarkApproved().IsSuccess);
        Assert.Equal(WithdrawalStatus.Approved, withdrawal.Status);
    }

    [Fact]
    public void MarkApproved_rejects_non_pending_approval()
    {
        var withdrawal = CreateWithdrawal();
        withdrawal.MarkApproved();

        var result = withdrawal.MarkApproved();

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.InvalidWithdrawalStatusTransition, result.Error);
    }

    [Fact]
    public void MarkProcessing_allows_pending_approval_or_approved()
    {
        var fromPending = CreateWithdrawal();
        Assert.True(fromPending.MarkProcessing().IsSuccess);
        Assert.Equal(WithdrawalStatus.Processing, fromPending.Status);

        var fromApproved = CreateWithdrawal();
        fromApproved.MarkApproved();
        Assert.True(fromApproved.MarkProcessing().IsSuccess);
        Assert.Equal(WithdrawalStatus.Processing, fromApproved.Status);
    }

    [Fact]
    public void MarkProcessing_rejects_completed()
    {
        var withdrawal = CreateCompletedWithdrawal();

        var result = withdrawal.MarkProcessing();

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.InvalidWithdrawalStatusTransition, result.Error);
    }

    [Fact]
    public void MarkCompleted_requires_approved_or_processing()
    {
        var fromApproved = CreateWithdrawal();
        fromApproved.MarkApproved();

        Assert.True(fromApproved.MarkCompleted("REF-123", "proof/key.pdf", Now).IsSuccess);
        Assert.Equal(WithdrawalStatus.Completed, fromApproved.Status);
        Assert.Equal("REF-123", fromApproved.TransferReference);
        Assert.Equal("proof/key.pdf", fromApproved.ProofObjectKey);
        Assert.Equal(Now, fromApproved.CompletedAt);

        var fromProcessing = CreateWithdrawal();
        fromProcessing.MarkProcessing();
        Assert.True(fromProcessing.MarkCompleted("REF-456", null, Now).IsSuccess);
        Assert.Equal(WithdrawalStatus.Completed, fromProcessing.Status);
    }

    [Fact]
    public void MarkCompleted_rejects_pending_approval()
    {
        var withdrawal = CreateWithdrawal();

        var result = withdrawal.MarkCompleted("REF-123", null, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.InvalidWithdrawalStatusTransition, result.Error);
    }

    [Fact]
    public void MarkCompleted_rejects_invalid_transfer_reference()
    {
        var withdrawal = CreateWithdrawal();
        withdrawal.MarkApproved();

        var empty = withdrawal.MarkCompleted("   ", null, Now);
        Assert.False(empty.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalInvalidTransferReference, empty.Error);
    }

    [Fact]
    public void MarkFailed_allows_active_statuses()
    {
        var pending = CreateWithdrawal();
        Assert.True(pending.MarkFailed(Now).IsSuccess);
        Assert.Equal(WithdrawalStatus.Failed, pending.Status);
        Assert.Equal(Now, pending.FailedAt);

        var approved = CreateWithdrawal();
        approved.MarkApproved();
        Assert.True(approved.MarkFailed(Now).IsSuccess);

        var processing = CreateWithdrawal();
        processing.MarkProcessing();
        Assert.True(processing.MarkFailed(Now).IsSuccess);
    }

    [Fact]
    public void MarkFailed_rejects_terminal_statuses()
    {
        var completed = CreateCompletedWithdrawal();
        Assert.False(completed.MarkFailed(Now).IsSuccess);

        var failed = CreateWithdrawal();
        failed.MarkFailed(Now);
        Assert.False(failed.MarkFailed(Now).IsSuccess);
    }

    [Fact]
    public void Rehydrated_status_resolves_state_after_ef_load()
    {
        var withdrawal = CreateWithdrawal();
        withdrawal.MarkApproved();

        // Simulate EF setting Status without _state (private field reset on materialization).
        typeof(WithdrawalRequest)
            .GetField("_state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(withdrawal, null);

        Assert.True(withdrawal.MarkProcessing().IsSuccess);
        Assert.Equal(WithdrawalStatus.Processing, withdrawal.Status);
    }

    private static WithdrawalRequest CreateWithdrawal() =>
        WithdrawalRequest.CreateForManualRail(
            OrganizationId,
            Money.Create(2_000, "USD").Value!,
            fxRateId: null,
            Now).Value!;

    private static WithdrawalRequest CreateCompletedWithdrawal()
    {
        var withdrawal = CreateWithdrawal();
        withdrawal.MarkApproved();
        withdrawal.MarkCompleted("REF-DONE", null, Now);
        return withdrawal;
    }
}
