using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public sealed class PurchaseAllocationSnapshot
{
    public PurchaseAllocationSnapshotId Id { get; private set; }
    public PurchaseId PurchaseId { get; private set; }
    public Guid TrackId { get; private set; }
    public OrganizationId PayeeOrganizationId { get; private set; }
    public int ShareBps { get; private set; }
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private PurchaseAllocationSnapshot()
    {
    }

    public static PurchaseAllocationSnapshot Create(
        PurchaseId purchaseId,
        Guid trackId,
        OrganizationId payeeOrganizationId,
        int shareBps,
        long amountMinor,
        string currency,
        DateTimeOffset now)
    {
        if (shareBps <= 0 || shareBps > 10_000)
            throw new ArgumentOutOfRangeException(nameof(shareBps));

        if (amountMinor < 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        return new PurchaseAllocationSnapshot
        {
            Id = PurchaseAllocationSnapshotId.New(),
            PurchaseId = purchaseId,
            TrackId = trackId,
            PayeeOrganizationId = payeeOrganizationId,
            ShareBps = shareBps,
            AmountMinor = amountMinor,
            Currency = currency,
            CreatedAt = now,
        };
    }
}
