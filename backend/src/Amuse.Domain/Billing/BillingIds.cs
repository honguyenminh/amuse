namespace Amuse.Domain.Billing;

public readonly record struct PurchaseId(Guid Value)
{
    public static PurchaseId New() => new(Guid.CreateVersion7());

    public static PurchaseId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Purchase id cannot be empty.", nameof(value));

        return new PurchaseId(value);
    }
}

public readonly record struct PaymentTransactionId(Guid Value)
{
    public static PaymentTransactionId New() => new(Guid.CreateVersion7());

    public static PaymentTransactionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Payment transaction id cannot be empty.", nameof(value));

        return new PaymentTransactionId(value);
    }
}

public readonly record struct LedgerJournalId(Guid Value)
{
    public static LedgerJournalId New() => new(Guid.CreateVersion7());

    public static LedgerJournalId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Ledger journal id cannot be empty.", nameof(value));

        return new LedgerJournalId(value);
    }
}

public readonly record struct LedgerEntryId(Guid Value)
{
    public static LedgerEntryId New() => new(Guid.CreateVersion7());

    public static LedgerEntryId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Ledger entry id cannot be empty.", nameof(value));

        return new LedgerEntryId(value);
    }
}

public readonly record struct PayoutProfileId(Guid Value)
{
    public static PayoutProfileId New() => new(Guid.CreateVersion7());

    public static PayoutProfileId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Payout profile id cannot be empty.", nameof(value));

        return new PayoutProfileId(value);
    }
}

public readonly record struct WithdrawalRequestId(Guid Value)
{
    public static WithdrawalRequestId New() => new(Guid.CreateVersion7());

    public static WithdrawalRequestId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Withdrawal request id cannot be empty.", nameof(value));

        return new WithdrawalRequestId(value);
    }
}

public readonly record struct TaxInvoiceId(Guid Value)
{
    public static TaxInvoiceId New() => new(Guid.CreateVersion7());

    public static TaxInvoiceId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Tax invoice id cannot be empty.", nameof(value));

        return new TaxInvoiceId(value);
    }
}

public readonly record struct BannedPaymentInstrumentId(Guid Value)
{
    public static BannedPaymentInstrumentId New() => new(Guid.CreateVersion7());

    public static BannedPaymentInstrumentId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Banned payment instrument id cannot be empty.", nameof(value));

        return new BannedPaymentInstrumentId(value);
    }
}

public readonly record struct SellerReceivableId(Guid Value)
{
    public static SellerReceivableId New() => new(Guid.CreateVersion7());

    public static SellerReceivableId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Seller receivable id cannot be empty.", nameof(value));

        return new SellerReceivableId(value);
    }
}

public readonly record struct FxRateId(Guid Value)
{
    public static FxRateId New() => new(Guid.CreateVersion7());

    public static FxRateId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Fx rate id cannot be empty.", nameof(value));

        return new FxRateId(value);
    }
}

public readonly record struct PurchaseAllocationSnapshotId(Guid Value)
{
    public static PurchaseAllocationSnapshotId New() => new(Guid.CreateVersion7());

    public static PurchaseAllocationSnapshotId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Purchase allocation snapshot id cannot be empty.", nameof(value));

        return new PurchaseAllocationSnapshotId(value);
    }
}

public readonly record struct CreditNoteId(Guid Value)
{
    public static CreditNoteId New() => new(Guid.CreateVersion7());

    public static CreditNoteId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Credit note id cannot be empty.", nameof(value));

        return new CreditNoteId(value);
    }
}
