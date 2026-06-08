using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class StripeWithdrawalExecutionService(
    BillingDbContext billingDb,
    IGlobalPayoutProvider payoutProvider,
    ILogger<StripeWithdrawalExecutionService> logger)
{
    public async Task<Result> TryExecuteAsync(
        WithdrawalRequest withdrawal,
        string externalRecipientId,
        CancellationToken cancellationToken)
    {
        if (withdrawal.Status != WithdrawalStatus.Approved)
            return Result.Success();

        var processingResult = withdrawal.MarkProcessing();
        if (!processingResult.IsSuccess)
            return processingResult;

        var paymentResult = await payoutProvider.SubmitOutboundPaymentAsync(
            new OutboundPaymentRequest(
                withdrawal.Id.Value,
                externalRecipientId,
                withdrawal.AmountMinor,
                withdrawal.Currency),
            cancellationToken);

        if (!paymentResult.IsSuccess)
        {
            withdrawal.MarkFailed(DateTimeOffset.UtcNow);
            var moneyResult = Money.Create(withdrawal.AmountMinor, withdrawal.Currency);
            if (moneyResult.IsSuccess)
            {
                var journalResult = JournalPoster.PostWithdrawalFailed(
                    withdrawal.Id,
                    withdrawal.OrganizationId,
                    moneyResult.Value!,
                    DateTimeOffset.UtcNow);

                if (journalResult.IsSuccess)
                    billingDb.LedgerJournals.Add(journalResult.Value!);
            }

            await billingDb.SaveChangesAsync(cancellationToken);
            LogStripeOutboundPayoutFailed(withdrawal.Id.Value, paymentResult.Error?.Code);
            return Result.Failure(paymentResult.Error!);
        }

        withdrawal.MarkCompleted(paymentResult.Value!.TransferId, proofObjectKey: null, DateTimeOffset.UtcNow);

        var completeMoney = Money.Create(withdrawal.AmountMinor, withdrawal.Currency);
        if (!completeMoney.IsSuccess)
            return Result.Failure(completeMoney.Error!);

        var completeJournal = JournalPoster.PostWithdrawalComplete(
            withdrawal.Id,
            withdrawal.OrganizationId,
            completeMoney.Value!,
            DateTimeOffset.UtcNow);
        if (!completeJournal.IsSuccess)
            return Result.Failure(completeJournal.Error!);

        billingDb.LedgerJournals.Add(completeJournal.Value!);
        await billingDb.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
