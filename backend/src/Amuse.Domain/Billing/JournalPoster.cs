using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public static class JournalPoster
{
    public const int DefaultHoldDays = 3;

    public static Result<(LedgerJournal Journal, IReadOnlyList<PurchaseAllocationSnapshot> Snapshots)> PostPurchase(
        PurchaseId purchaseId,
        PurchaseWaterfallResult waterfall,
        IReadOnlyList<AllocationPayeeLine> allocationLines,
        DateTimeOffset paidAt,
        int holdDays)
    {
        if (allocationLines.Sum(l => l.AmountMinor) != waterfall.NetToSellersMinor)
            return Result<(LedgerJournal, IReadOnlyList<PurchaseAllocationSnapshot>)>.Failure(
                BillingErrors.InvalidLedgerJournal);

        var availableAt = paidAt.AddDays(holdDays);
        var journalId = LedgerJournalId.New();
        var cashMinor = waterfall.GrossMinor - waterfall.PspFeeMinor;
        var entries = new List<LedgerEntry>
        {
            Entry(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Debit, cashMinor, waterfall.Currency),
            Entry(journalId, LedgerAccountType.VatPayable, null, EntryDirection.Credit, waterfall.VatMinor, waterfall.Currency),
            Entry(journalId, LedgerAccountType.PlatformRevenue, null, EntryDirection.Credit, waterfall.PlatformFeeMinor, waterfall.Currency),
        };

        var aggregated = allocationLines
            .GroupBy(line => line.PayeeOrganizationId)
            .Select(group => new
            {
                OrganizationId = group.Key,
                AmountMinor = group.Sum(line => line.AmountMinor),
            })
            .Where(x => x.AmountMinor > 0)
            .ToArray();

        foreach (var payee in aggregated)
        {
            entries.Add(Entry(
                journalId,
                LedgerAccountType.SellerPayablePending,
                payee.OrganizationId.Value,
                EntryDirection.Credit,
                payee.AmountMinor,
                waterfall.Currency));
        }

        var journalResult = LedgerJournal.Create(
            JournalType.Purchase,
            ReferenceType.Purchase,
            purchaseId.Value,
            waterfall.Currency,
            paidAt,
            availableAt,
            entries);

        if (!journalResult.IsSuccess)
            return Result<(LedgerJournal, IReadOnlyList<PurchaseAllocationSnapshot>)>.Failure(journalResult.Error!);

        var snapshots = allocationLines
            .Select(line => PurchaseAllocationSnapshot.Create(
                purchaseId,
                line.TrackId,
                line.PayeeOrganizationId,
                line.ShareBps,
                line.AmountMinor,
                waterfall.Currency,
                paidAt))
            .ToArray();

        return Result<(LedgerJournal, IReadOnlyList<PurchaseAllocationSnapshot>)>.Success(
            (journalResult.Value!, snapshots));
    }

    public static Result<LedgerJournal> PostHoldRelease(
        PurchaseId purchaseId,
        string currency,
        DateTimeOffset postedAt,
        IEnumerable<(OrganizationId OrganizationId, long AmountMinor)> pendingCredits)
    {
        var journalId = LedgerJournalId.New();
        var entries = new List<LedgerEntry>();

        foreach (var credit in pendingCredits.Where(c => c.AmountMinor > 0))
        {
            entries.Add(Entry(
                journalId,
                LedgerAccountType.SellerPayablePending,
                credit.OrganizationId.Value,
                EntryDirection.Debit,
                credit.AmountMinor,
                currency));

            entries.Add(Entry(
                journalId,
                LedgerAccountType.SellerPayableAvailable,
                credit.OrganizationId.Value,
                EntryDirection.Credit,
                credit.AmountMinor,
                currency));
        }

        return LedgerJournal.Create(
            JournalType.HoldRelease,
            ReferenceType.Purchase,
            purchaseId.Value,
            currency,
            postedAt,
            availableAt: null,
            entries);
    }

    public static Result<LedgerJournal> PostWithdrawalReserve(
        WithdrawalRequestId withdrawalId,
        OrganizationId organizationId,
        Money amount,
        DateTimeOffset postedAt)
    {
        var journalId = LedgerJournalId.New();
        var entries = new List<LedgerEntry>
        {
            Entry(
                journalId,
                LedgerAccountType.SellerPayableAvailable,
                organizationId.Value,
                EntryDirection.Debit,
                amount.AmountMinor,
                amount.Currency),
            Entry(
                journalId,
                LedgerAccountType.SellerPayableInPayout,
                organizationId.Value,
                EntryDirection.Credit,
                amount.AmountMinor,
                amount.Currency),
        };

        return LedgerJournal.Create(
            JournalType.Withdrawal,
            ReferenceType.Withdrawal,
            withdrawalId.Value,
            amount.Currency,
            postedAt,
            availableAt: null,
            entries);
    }

    public static Result<LedgerJournal> PostWithdrawalComplete(
        WithdrawalRequestId withdrawalId,
        OrganizationId organizationId,
        Money amount,
        DateTimeOffset postedAt)
    {
        var journalId = LedgerJournalId.New();
        var entries = new List<LedgerEntry>
        {
            Entry(
                journalId,
                LedgerAccountType.SellerPayableInPayout,
                organizationId.Value,
                EntryDirection.Debit,
                amount.AmountMinor,
                amount.Currency),
            Entry(
                journalId,
                LedgerAccountType.PlatformCash,
                null,
                EntryDirection.Credit,
                amount.AmountMinor,
                amount.Currency),
        };

        return LedgerJournal.Create(
            JournalType.Withdrawal,
            ReferenceType.Withdrawal,
            withdrawalId.Value,
            amount.Currency,
            postedAt,
            availableAt: null,
            entries);
    }

    public static Result<(LedgerJournal Journal, RefundClawbackResult Clawback)> PostRefund(
        PurchaseId purchaseId,
        PurchaseWaterfallResult waterfall,
        IReadOnlyList<PurchaseAllocationSnapshot> snapshots,
        IReadOnlyList<LedgerEntry> ledgerEntries,
        long refundFeeMinor,
        RefundFeeBearer refundFeeBearer,
        DateTimeOffset postedAt)
    {
        return PostReversal(
            JournalType.Refund,
            ReferenceType.Refund,
            purchaseId,
            waterfall,
            snapshots,
            ledgerEntries,
            refundFeeMinor,
            refundFeeBearer,
            postedAt);
    }

    private static Result<(LedgerJournal Journal, RefundClawbackResult Clawback)> PostReversal(
        JournalType journalType,
        ReferenceType referenceType,
        PurchaseId purchaseId,
        PurchaseWaterfallResult waterfall,
        IReadOnlyList<PurchaseAllocationSnapshot> snapshots,
        IReadOnlyList<LedgerEntry> ledgerEntries,
        long refundFeeMinor,
        RefundFeeBearer refundFeeBearer,
        DateTimeOffset postedAt)
    {
        var clawback = RefundClawback.Compute(
            snapshots,
            ledgerEntries,
            refundFeeMinor,
            refundFeeBearer,
            waterfall.Currency);

        var journalId = LedgerJournalId.New();
        var cashMinor = waterfall.GrossMinor - waterfall.PspFeeMinor;
        var entries = new List<LedgerEntry>
        {
            Entry(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Credit, cashMinor, waterfall.Currency),
            Entry(journalId, LedgerAccountType.VatPayable, null, EntryDirection.Debit, waterfall.VatMinor, waterfall.Currency),
            Entry(journalId, LedgerAccountType.PlatformRevenue, null, EntryDirection.Debit, waterfall.PlatformFeeMinor, waterfall.Currency),
        };

        foreach (var line in clawback.LedgerLines)
        {
            entries.Add(Entry(
                journalId,
                line.AccountType,
                line.OrganizationId.Value,
                EntryDirection.Debit,
                line.AmountMinor,
                waterfall.Currency));
        }

        foreach (var receivable in clawback.Receivables)
        {
            entries.Add(Entry(
                journalId,
                LedgerAccountType.RefundLiability,
                receivable.OrganizationId.Value,
                EntryDirection.Debit,
                receivable.AmountMinor,
                waterfall.Currency));
        }

        if (refundFeeMinor > 0)
        {
            entries.Add(Entry(
                journalId,
                LedgerAccountType.PlatformCash,
                null,
                EntryDirection.Credit,
                refundFeeMinor,
                waterfall.Currency));

            if (refundFeeBearer == RefundFeeBearer.Platform)
            {
                entries.Add(Entry(
                    journalId,
                    LedgerAccountType.PlatformRevenue,
                    null,
                    EntryDirection.Debit,
                    refundFeeMinor,
                    waterfall.Currency));
            }
        }

        var journalResult = LedgerJournal.Create(
            journalType,
            referenceType,
            purchaseId.Value,
            waterfall.Currency,
            postedAt,
            availableAt: null,
            entries);

        if (!journalResult.IsSuccess)
            return Result<(LedgerJournal, RefundClawbackResult)>.Failure(journalResult.Error!);

        return Result<(LedgerJournal, RefundClawbackResult)>.Success((journalResult.Value!, clawback));
    }

    public static Result<(LedgerJournal Journal, RefundClawbackResult Clawback)> PostChargeback(
        PurchaseId purchaseId,
        PurchaseWaterfallResult waterfall,
        IReadOnlyList<PurchaseAllocationSnapshot> snapshots,
        IReadOnlyList<LedgerEntry> ledgerEntries,
        long disputeFeeMinor,
        DateTimeOffset postedAt)
    {
        return PostReversal(
            JournalType.Chargeback,
            ReferenceType.Chargeback,
            purchaseId,
            waterfall,
            snapshots,
            ledgerEntries,
            disputeFeeMinor,
            RefundFeeBearer.Seller,
            postedAt);
    }

    public static Result<LedgerJournal> PostWithdrawalFailed(
        WithdrawalRequestId withdrawalId,
        OrganizationId organizationId,
        Money amount,
        DateTimeOffset postedAt)
    {
        var journalId = LedgerJournalId.New();
        var entries = new List<LedgerEntry>
        {
            Entry(
                journalId,
                LedgerAccountType.SellerPayableInPayout,
                organizationId.Value,
                EntryDirection.Debit,
                amount.AmountMinor,
                amount.Currency),
            Entry(
                journalId,
                LedgerAccountType.SellerPayableAvailable,
                organizationId.Value,
                EntryDirection.Credit,
                amount.AmountMinor,
                amount.Currency),
        };

        return LedgerJournal.Create(
            JournalType.Withdrawal,
            ReferenceType.Withdrawal,
            withdrawalId.Value,
            amount.Currency,
            postedAt,
            availableAt: null,
            entries);
    }

    private static LedgerEntry Entry(
        LedgerJournalId journalId,
        LedgerAccountType accountType,
        Guid? organizationId,
        EntryDirection direction,
        long amountMinor,
        string currency)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        return LedgerEntry.Create(journalId, accountType, organizationId, direction, amountMinor, currency);
    }
}
