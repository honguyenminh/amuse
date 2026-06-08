using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Amuse.Modules.Billing.Features.StripeWebhook;

internal sealed partial class StripeWebhookHandler(
    BillingDbContext billingDb,
    ICheckoutProvider checkoutProvider,
    PaidPurchaseCompletionService completionService,
    RefundCompletionService refundCompletionService,
    ChargebackCompletionService chargebackCompletionService,
    StripeWithdrawalExecutionService stripeWithdrawalExecution,
    IClock clock,
    IOptions<StripeConfig> stripeOptions,
    ILogger<StripeWebhookHandler> logger)
{
    public async Task<Result> HandleAsync(string json, string signatureHeader, CancellationToken cancellationToken)
    {
        var webhookSecret = stripeOptions.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
            return Result.Failure(BillingErrors.CheckoutNotConfigured);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
        }
        catch (StripeException ex)
        {
            LogSignatureValidationFailed(ex);
            return Result.Failure(BillingErrors.WebhookInvalid);
        }

        return stripeEvent.Type switch
        {
            EventTypes.CheckoutSessionCompleted => await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken),
            EventTypes.ChargeRefunded => await HandleChargeRefundedAsync(stripeEvent, cancellationToken),
            EventTypes.ChargeDisputeCreated => await HandleChargeDisputeCreatedAsync(stripeEvent, cancellationToken),
            EventTypes.ChargeDisputeClosed => HandleChargeDisputeClosed(stripeEvent),
            EventTypes.AccountUpdated => await HandleAccountUpdatedAsync(stripeEvent, cancellationToken),
            EventTypes.TransferCreated => await HandleTransferCreatedAsync(stripeEvent, cancellationToken),
            "transfer.failed" => await HandleTransferFailedAsync(stripeEvent, cancellationToken),
            _ => Result.Success(),
        };
    }

    private async Task<Result> HandleCheckoutCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Stripe.Checkout.Session session || string.IsNullOrWhiteSpace(session.Id))
            return Result.Failure(BillingErrors.WebhookInvalid);

        var paymentTransaction = await billingDb.PaymentTransactions
            .FirstOrDefaultAsync(t => t.CheckoutSessionId == session.Id, cancellationToken);

        if (paymentTransaction is null)
            return Result.Failure(BillingErrors.CheckoutSessionNotFound);

        var purchase = await billingDb.Purchases
            .FirstOrDefaultAsync(p => p.Id == paymentTransaction.PurchaseId, cancellationToken);

        if (purchase is null)
            return Result.Failure(BillingErrors.PurchaseNotFound);

        if (purchase.PaymentStatus == PaymentStatus.Paid)
            return Result.Success();

        var paymentResult = await checkoutProvider.GetCompletedPaymentAsync(session.Id, cancellationToken);
        if (!paymentResult.IsSuccess)
            return Result.Failure(paymentResult.Error!);

        var completeResult = await completionService.CompleteAsync(
            purchase,
            paymentTransaction,
            paymentResult.Value!,
            cancellationToken);

        return completeResult.IsSuccess
            ? Result.Success()
            : Result.Failure(completeResult.Error!);
    }

    private async Task<Result> HandleChargeRefundedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Charge charge || string.IsNullOrWhiteSpace(charge.Id))
            return Result.Failure(BillingErrors.WebhookInvalid);

        var paymentTransaction = await billingDb.PaymentTransactions
            .FirstOrDefaultAsync(t => t.ProviderReference == charge.Id, cancellationToken);

        if (paymentTransaction is null)
            return Result.Success();

        var purchase = await billingDb.Purchases
            .FirstOrDefaultAsync(p => p.Id == paymentTransaction.PurchaseId, cancellationToken);

        if (purchase is null || purchase.PaymentStatus == PaymentStatus.Refunded)
            return Result.Success();

        if (purchase.RefundFeeBearer is null)
        {
            purchase.BeginRefund(
                purchase.AccountId,
                RefundInitiatorRole.Platform,
                "Stripe charge.refunded webhook",
                RefundFeeBearer.Seller,
                DateTimeOffset.UtcNow);
        }

        var refundFeeMinor = charge.Refunds?.Data?.FirstOrDefault()?.BalanceTransaction?.Fee ?? 0;
        var complete = await refundCompletionService.CompleteAsync(
            purchase,
            paymentTransaction,
            refundFeeMinor,
            cancellationToken);

        return complete.IsSuccess
            ? Result.Success()
            : Result.Failure(complete.Error!);
    }

    private async Task<Result> HandleChargeDisputeCreatedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Dispute dispute || string.IsNullOrWhiteSpace(dispute.ChargeId))
            return Result.Failure(BillingErrors.WebhookInvalid);

        var paymentTransaction = await billingDb.PaymentTransactions
            .FirstOrDefaultAsync(t => t.ProviderReference == dispute.ChargeId, cancellationToken);

        if (paymentTransaction is null)
            return Result.Success();

        var purchase = await billingDb.Purchases
            .FirstOrDefaultAsync(p => p.Id == paymentTransaction.PurchaseId, cancellationToken);

        if (purchase is null)
            return Result.Failure(BillingErrors.PurchaseNotFound);

        var disputeFeeMinor = dispute.BalanceTransactions?.FirstOrDefault()?.Fee ?? 0;
        var details = new ChargebackDisputeDetails(
            dispute.Id,
            dispute.Amount,
            dispute.Currency,
            dispute.Reason,
            disputeFeeMinor);

        var complete = await chargebackCompletionService.CompleteAsync(
            purchase,
            paymentTransaction,
            details,
            cancellationToken);

        return complete.IsSuccess
            ? Result.Success()
            : Result.Failure(complete.Error!);
    }

    private Result HandleChargeDisputeClosed(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Dispute dispute)
            return Result.Failure(BillingErrors.WebhookInvalid);

        LogDisputeClosed(dispute.Id, dispute.Status, dispute.ChargeId);

        return Result.Success();
    }

    private async Task<Result> HandleAccountUpdatedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Account account || string.IsNullOrWhiteSpace(account.Id))
            return Result.Failure(BillingErrors.WebhookInvalid);

        if (!account.PayoutsEnabled
            || !account.DetailsSubmitted)
        {
            return Result.Success();
        }

        var profile = await billingDb.PayoutProfiles
            .SingleOrDefaultAsync(
                payoutProfile => payoutProfile.ExternalRecipientId == account.Id,
                cancellationToken);

        if (profile is null || profile.PayoutRail != PayoutRail.StripeGlobal)
            return Result.Success();

        if (profile.IsVerified)
            return Result.Success();

        var verifyResult = profile.CompleteStripeVerification(clock.UtcNow);
        if (!verifyResult.IsSuccess)
            return verifyResult;

        await billingDb.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> HandleTransferCreatedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Transfer transfer
            || string.IsNullOrWhiteSpace(transfer.Id)
            || transfer.Metadata is null
            || !transfer.Metadata.TryGetValue("withdrawal_id", out var withdrawalIdValue)
            || !Guid.TryParse(withdrawalIdValue, out var withdrawalId))
        {
            return Result.Success();
        }

        var withdrawal = await billingDb.WithdrawalRequests
            .SingleOrDefaultAsync(
                request => request.Id == WithdrawalRequestId.From(withdrawalId),
                cancellationToken);

        if (withdrawal is null || withdrawal.Status == WithdrawalStatus.Completed)
            return Result.Success();

        if (withdrawal.Status == WithdrawalStatus.Approved)
        {
            var processingResult = withdrawal.MarkProcessing();
            if (!processingResult.IsSuccess)
                return processingResult;
        }

        if (withdrawal.Status != WithdrawalStatus.Processing)
            return Result.Success();

        var completeResult = withdrawal.MarkCompleted(transfer.Id, proofObjectKey: null, clock.UtcNow);
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

    private async Task<Result> HandleTransferFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Transfer transfer
            || transfer.Metadata is null
            || !transfer.Metadata.TryGetValue("withdrawal_id", out var withdrawalIdValue)
            || !Guid.TryParse(withdrawalIdValue, out var withdrawalId))
        {
            return Result.Success();
        }

        var withdrawal = await billingDb.WithdrawalRequests
            .SingleOrDefaultAsync(
                request => request.Id == WithdrawalRequestId.From(withdrawalId),
                cancellationToken);

        if (withdrawal is null || withdrawal.Status is WithdrawalStatus.Completed or WithdrawalStatus.Failed)
            return Result.Success();

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
