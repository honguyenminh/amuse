using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public sealed class SellerReceivable
{
    public SellerReceivableId Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public PurchaseId PurchaseId { get; private set; }
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public bool IsSettled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }

    private SellerReceivable()
    {
    }

    public static SellerReceivable Create(
        OrganizationId organizationId,
        PurchaseId purchaseId,
        long amountMinor,
        string currency,
        DateTimeOffset now)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        return new SellerReceivable
        {
            Id = SellerReceivableId.New(),
            OrganizationId = organizationId,
            PurchaseId = purchaseId,
            AmountMinor = amountMinor,
            Currency = currency,
            IsSettled = false,
            CreatedAt = now,
        };
    }

    public void MarkSettled(DateTimeOffset now)
    {
        IsSettled = true;
        SettledAt = now;
    }
}
