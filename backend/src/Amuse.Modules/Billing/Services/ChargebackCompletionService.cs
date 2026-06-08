using System.Text.Json;
using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Services;

internal sealed record ChargebackDisputeDetails(
    string DisputeId,
    long AmountMinor,
    string Currency,
    string? ReasonCode,
    long DisputeFeeMinor);

internal sealed class ChargebackCompletionService(
    BillingDbContext billingDb,
    IdentityDbContext identityDb,
    IAuditWriter auditWriter,
    IOptions<PlatformFeeConfig> platformFeeOptions,
    IOptions<TaxConfig> taxOptions,
    IClock clock)
{
    public async Task<Result> CompleteAsync(
        Purchase purchase,
        PaymentTransaction paymentTransaction,
        ChargebackDisputeDetails dispute,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var existingChargebackJournal = await billingDb.LedgerJournals.AsNoTracking()
            .AnyAsync(
                journal => journal.JournalType == JournalType.Chargeback
                           && journal.ReferenceType == ReferenceType.Chargeback
                           && journal.ReferenceId == purchase.Id.Value,
                cancellationToken);

        if (existingChargebackJournal)
            return Result.Success();

        if (purchase.PaymentStatus is PaymentStatus.ChargedBack)
            return Result.Success();

        var banAccount = await BanBuyerAccountAsync(purchase.AccountId, now, cancellationToken);
        if (!banAccount.IsSuccess)
            return banAccount;

        await BanPaymentInstrumentIfNeededAsync(
            paymentTransaction.PaymentMethodFingerprint,
            dispute.DisputeId,
            now,
            cancellationToken);

        if (purchase.PaymentStatus == PaymentStatus.Paid)
        {
            var reversal = await ReversePaidPurchaseAsync(
                purchase,
                paymentTransaction,
                dispute.DisputeFeeMinor,
                now,
                cancellationToken);

            if (!reversal.IsSuccess)
                return reversal;
        }

        await WriteChargebackAuditAsync(purchase, dispute, now, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> BanBuyerAccountAsync(
        AccountId accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var account = await identityDb.Accounts
            .SingleOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account is null)
            return Result.Failure(IdentityErrors.AccountNotFound);

        var banResult = account.Ban(now);
        if (!banResult.IsSuccess && banResult.Error != IdentityErrors.AccountBanned)
            return banResult;

        await identityDb.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task BanPaymentInstrumentIfNeededAsync(
        string? paymentMethodFingerprint,
        string disputeId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodFingerprint))
            return;

        var fingerprint = paymentMethodFingerprint.Trim();
        var alreadyBanned = await billingDb.BannedPaymentInstruments.AsNoTracking()
            .AnyAsync(
                instrument => instrument.PaymentMethodFingerprint == fingerprint,
                cancellationToken);

        if (alreadyBanned)
            return;

        billingDb.BannedPaymentInstruments.Add(
            BannedPaymentInstrument.Create(fingerprint, $"chargeback:{disputeId}", now));
        await billingDb.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result> ReversePaidPurchaseAsync(
        Purchase purchase,
        PaymentTransaction paymentTransaction,
        long disputeFeeMinor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var taxInvoice = await billingDb.TaxInvoices
            .FirstOrDefaultAsync(invoice => invoice.PurchaseId == purchase.Id, cancellationToken);

        if (taxInvoice is null)
            return Result.Failure(BillingErrors.TaxInvoiceNotFound);

        var snapshots = await billingDb.PurchaseAllocationSnapshots
            .Where(snapshot => snapshot.PurchaseId == purchase.Id)
            .ToListAsync(cancellationToken);

        if (snapshots.Count == 0)
            return Result.Failure(BillingErrors.InvalidLedgerJournal);

        var pspFeeMinor = paymentTransaction.PspFeeMinor ?? 0;
        var waterfall = PurchaseWaterfall.Compute(
            paymentTransaction.GrossMinor,
            paymentTransaction.Currency,
            pspFeeMinor,
            taxOptions.Value.DefaultVatBps,
            platformFeeOptions.Value.DefaultRateBps);

        if (!waterfall.IsSuccess)
            return Result.Failure(waterfall.Error!);

        var ledgerEntries = await billingDb.LedgerEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var journalResult = JournalPoster.PostChargeback(
            purchase.Id,
            waterfall.Value!,
            snapshots,
            ledgerEntries,
            disputeFeeMinor,
            now);

        if (!journalResult.IsSuccess)
            return Result.Failure(journalResult.Error!);

        var markPayment = paymentTransaction.MarkChargedBack(now);
        if (!markPayment.IsSuccess)
            return Result.Failure(markPayment.Error!);

        var markPurchase = purchase.MarkChargedBack(now);
        if (!markPurchase.IsSuccess)
            return Result.Failure(markPurchase.Error!);

        var revoke = purchase.RevokeEntitlement();
        if (!revoke.IsSuccess)
            return Result.Failure(revoke.Error!);

        billingDb.LedgerJournals.Add(journalResult.Value!.Journal);

        foreach (var receivable in journalResult.Value!.Clawback.Receivables)
        {
            billingDb.SellerReceivables.Add(SellerReceivable.Create(
                receivable.OrganizationId,
                purchase.Id,
                receivable.AmountMinor,
                waterfall.Value!.Currency,
                now));
        }

        await billingDb.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task WriteChargebackAuditAsync(
        Purchase purchase,
        ChargebackDisputeDetails dispute,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var afterJson = JsonSerializer.Serialize(new
        {
            disputeId = dispute.DisputeId,
            amountMinor = dispute.AmountMinor,
            currency = dispute.Currency,
            reasonCode = dispute.ReasonCode,
            disputeFeeMinor = dispute.DisputeFeeMinor,
        });

        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "chargeback_received",
            TableName = "billing.purchase",
            TargetId = purchase.Id.Value,
            AfterJson = afterJson,
            ChangedAt = now,
            ActorAccountId = null,
            Reason = dispute.ReasonCode,
        }, cancellationToken);
    }
}
