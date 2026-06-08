using Amuse.Domain.Identity;

namespace Amuse.Domain.Billing;

public sealed class TaxInvoice
{
    public TaxInvoiceId Id { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public PurchaseId PurchaseId { get; private set; }
    public AccountId BuyerAccountId { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public long GrossMinor { get; private set; }
    public long VatMinor { get; private set; }
    public long NetExVatMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public int VatRateBps { get; private set; }

    private TaxInvoice()
    {
    }

    public static TaxInvoice Issue(
        string invoiceNumber,
        PurchaseId purchaseId,
        AccountId buyerAccountId,
        long grossMinor,
        long vatMinor,
        long netExVatMinor,
        string currency,
        int vatRateBps,
        DateTimeOffset issuedAt)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));

        if (grossMinor < 0 || vatMinor < 0 || netExVatMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(grossMinor));

        return new TaxInvoice
        {
            Id = TaxInvoiceId.New(),
            InvoiceNumber = invoiceNumber.Trim(),
            PurchaseId = purchaseId,
            BuyerAccountId = buyerAccountId,
            IssuedAt = issuedAt,
            GrossMinor = grossMinor,
            VatMinor = vatMinor,
            NetExVatMinor = netExVatMinor,
            Currency = currency,
            VatRateBps = vatRateBps,
        };
    }
}
