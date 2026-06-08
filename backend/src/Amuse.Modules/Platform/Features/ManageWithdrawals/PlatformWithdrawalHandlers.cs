using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Features.Withdrawals;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Features.ManageWithdrawals;

internal sealed class ListPlatformWithdrawalsHandler(BillingDbContext billingDb)
{
    public async Task<Result<IReadOnlyList<PlatformWithdrawalRow>>> HandleAsync(
        WithdrawalStatus? status,
        CancellationToken cancellationToken)
    {
        var resolvedStatus = status ?? WithdrawalStatus.PendingApproval;

        var rows = await billingDb.WithdrawalRequests.AsNoTracking()
            .Where(withdrawal => withdrawal.Status == resolvedStatus)
            .OrderBy(withdrawal => withdrawal.RequestedAt)
            .Select(withdrawal => new PlatformWithdrawalRow(
                withdrawal.Id.Value,
                withdrawal.OrganizationId.Value,
                withdrawal.AmountMinor,
                withdrawal.Currency,
                withdrawal.Status,
                withdrawal.RequestedAt,
                withdrawal.TransferReference))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PlatformWithdrawalRow>>.Success(rows);
    }
}

internal sealed class ApproveWithdrawalHandler(
    BillingDbContext billingDb,
    StripeWithdrawalExecutionService stripeWithdrawalExecution)
{
    public async Task<Result> HandleAsync(Guid withdrawalId, CancellationToken cancellationToken)
    {
        var withdrawal = await billingDb.WithdrawalRequests
            .SingleOrDefaultAsync(request => request.Id == WithdrawalRequestId.From(withdrawalId), cancellationToken);

        if (withdrawal is null)
            return Result.Failure(BillingErrors.WithdrawalNotFound);

        var approveResult = withdrawal.MarkApproved();
        if (!approveResult.IsSuccess)
            return approveResult;

        await billingDb.SaveChangesAsync(cancellationToken);

        var profile = await billingDb.PayoutProfiles.AsNoTracking()
            .SingleOrDefaultAsync(
                payoutProfile => payoutProfile.OrganizationId == withdrawal.OrganizationId,
                cancellationToken);

        if (profile?.PayoutRail == PayoutRail.StripeGlobal
            && !string.IsNullOrWhiteSpace(profile.ExternalRecipientId))
        {
            var executeResult = await stripeWithdrawalExecution.TryExecuteAsync(
                withdrawal,
                profile.ExternalRecipientId!,
                cancellationToken);

            if (!executeResult.IsSuccess)
                return executeResult;
        }

        return Result.Success();
    }
}

internal sealed class CompleteWithdrawalHandler(BillingDbContext billingDb, IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid withdrawalId,
        CompleteWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        var withdrawal = await billingDb.WithdrawalRequests
            .SingleOrDefaultAsync(w => w.Id == WithdrawalRequestId.From(withdrawalId), cancellationToken);

        if (withdrawal is null)
            return Result.Failure(BillingErrors.WithdrawalNotFound);

        if (withdrawal.Status == WithdrawalStatus.PendingApproval)
        {
            var approveResult = withdrawal.MarkApproved();
            if (!approveResult.IsSuccess)
                return approveResult;
        }

        if (withdrawal.Status == WithdrawalStatus.Approved)
        {
            var processingResult = withdrawal.MarkProcessing();
            if (!processingResult.IsSuccess)
                return processingResult;
        }

        var completeResult = withdrawal.MarkCompleted(
            request.TransferReference,
            request.ProofObjectKey,
            clock.UtcNow);
        if (!completeResult.IsSuccess)
            return completeResult;

        var moneyResult = Money.Create(withdrawal.AmountMinor, withdrawal.Currency);
        if (!moneyResult.IsSuccess)
            return Result.Failure(moneyResult.Error!);

        var journalResult = JournalPoster.PostWithdrawalComplete(
            withdrawal.Id,
            withdrawal.OrganizationId,
            moneyResult.Value!,
            clock.UtcNow);
        if (!journalResult.IsSuccess)
            return Result.Failure(journalResult.Error!);

        billingDb.LedgerJournals.Add(journalResult.Value!);
        await billingDb.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class FailWithdrawalHandler(BillingDbContext billingDb, IClock clock)
{
    public async Task<Result> HandleAsync(Guid withdrawalId, CancellationToken cancellationToken)
    {
        var withdrawal = await billingDb.WithdrawalRequests
            .SingleOrDefaultAsync(w => w.Id == WithdrawalRequestId.From(withdrawalId), cancellationToken);

        if (withdrawal is null)
            return Result.Failure(BillingErrors.WithdrawalNotFound);

        var failResult = withdrawal.MarkFailed(clock.UtcNow);
        if (!failResult.IsSuccess)
            return failResult;

        var moneyResult = Money.Create(withdrawal.AmountMinor, withdrawal.Currency);
        if (!moneyResult.IsSuccess)
            return Result.Failure(moneyResult.Error!);

        var journalResult = JournalPoster.PostWithdrawalFailed(
            withdrawal.Id,
            withdrawal.OrganizationId,
            moneyResult.Value!,
            clock.UtcNow);
        if (!journalResult.IsSuccess)
            return Result.Failure(journalResult.Error!);

        billingDb.LedgerJournals.Add(journalResult.Value!);
        await billingDb.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
