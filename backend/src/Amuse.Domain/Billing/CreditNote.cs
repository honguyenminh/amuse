namespace Amuse.Domain.Billing;

public sealed class CreditNote
{
    public CreditNoteId Id { get; private set; }
    public string CreditNoteNumber { get; private set; } = null!;
    public TaxInvoiceId TaxInvoiceId { get; private set; }
    public PurchaseId PurchaseId { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public long GrossMinor { get; private set; }
    public long VatMinor { get; private set; }
    public long NetExVatMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public int VatRateBps { get; private set; }
    public RefundFeeBearer RefundFeeBearer { get; private set; }
    public long RefundFeeMinor { get; private set; }

    private CreditNote()
    {
    }

    public static CreditNote Issue(
        string creditNoteNumber,
        TaxInvoiceId taxInvoiceId,
        PurchaseId purchaseId,
        long grossMinor,
        long vatMinor,
        long netExVatMinor,
        string currency,
        int vatRateBps,
        RefundFeeBearer refundFeeBearer,
        long refundFeeMinor,
        DateTimeOffset issuedAt)
    {
        if (string.IsNullOrWhiteSpace(creditNoteNumber))
            throw new ArgumentException("Credit note number is required.", nameof(creditNoteNumber));

        if (grossMinor < 0 || vatMinor < 0 || netExVatMinor < 0 || refundFeeMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(grossMinor));

        return new CreditNote
        {
            Id = CreditNoteId.New(),
            CreditNoteNumber = creditNoteNumber.Trim(),
            TaxInvoiceId = taxInvoiceId,
            PurchaseId = purchaseId,
            IssuedAt = issuedAt,
            GrossMinor = grossMinor,
            VatMinor = vatMinor,
            NetExVatMinor = netExVatMinor,
            Currency = currency,
            VatRateBps = vatRateBps,
            RefundFeeBearer = refundFeeBearer,
            RefundFeeMinor = refundFeeMinor,
        };
    }
}
