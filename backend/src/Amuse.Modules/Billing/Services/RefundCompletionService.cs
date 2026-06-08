using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Services;

internal sealed class RefundCompletionService(
    BillingDbContext billingDb,
    IOptions<PlatformFeeConfig> platformFeeOptions,
    IOptions<TaxConfig> taxOptions,
    IClock clock)
{
    public async Task<Result> CompleteAsync(
        Purchase purchase,
        PaymentTransaction paymentTransaction,
        long refundFeeMinor,
        CancellationToken cancellationToken)
    {
        if (purchase.PaymentStatus == PaymentStatus.Refunded)
            return Result.Success();

        if (purchase.PaymentStatus != PaymentStatus.Paid)
            return Result.Failure(BillingErrors.RefundNotEligible);

        var existingRefundJournal = await billingDb.LedgerJournals.AsNoTracking()
            .AnyAsync(
                journal => journal.JournalType == JournalType.Refund
                           && journal.ReferenceType == ReferenceType.Refund
                           && journal.ReferenceId == purchase.Id.Value,
                cancellationToken);

        if (existingRefundJournal)
            return Result.Success();

        if (purchase.RefundFeeBearer is null)
            return Result.Failure(BillingErrors.RefundFeeBearerRequired);

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

        var now = clock.UtcNow;
        var journalResult = JournalPoster.PostRefund(
            purchase.Id,
            waterfall.Value!,
            snapshots,
            ledgerEntries,
            refundFeeMinor,
            purchase.RefundFeeBearer.Value,
            now);

        if (!journalResult.IsSuccess)
            return Result.Failure(journalResult.Error!);

        var refundPayment = paymentTransaction.MarkRefunded(now);
        if (!refundPayment.IsSuccess)
            return Result.Failure(refundPayment.Error!);

        var markRefunded = purchase.MarkRefunded(now);
        if (!markRefunded.IsSuccess)
            return Result.Failure(markRefunded.Error!);

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

        var latestCreditNote = await billingDb.CreditNotes.AsNoTracking()
            .OrderByDescending(note => note.IssuedAt)
            .Select(note => note.CreditNoteNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var creditNoteNumber = CreditNoteNumber.NextFromLatest(latestCreditNote, now);
        var creditNote = CreditNote.Issue(
            creditNoteNumber,
            taxInvoice.Id,
            purchase.Id,
            taxInvoice.GrossMinor,
            taxInvoice.VatMinor,
            taxInvoice.NetExVatMinor,
            taxInvoice.Currency,
            taxInvoice.VatRateBps,
            purchase.RefundFeeBearer.Value,
            refundFeeMinor,
            now);

        billingDb.CreditNotes.Add(creditNote);
        await billingDb.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
