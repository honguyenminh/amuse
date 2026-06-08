using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Billing;

public sealed class PurchaseAcquisitionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
    private static readonly AccountId AccountId = AccountId.New();
    private static readonly OrganizationId OrganizationId = OrganizationId.New();

    [Fact]
    public void AcquireFreeTrack_creates_free_active_entitlement()
    {
        var trackId = Guid.CreateVersion7();
        var money = Money.Create(0, "USD").Value!;

        var result = Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackId, money, Now);

        Assert.True(result.IsSuccess);
        var purchase = result.Value!;
        Assert.Equal(PaymentStatus.Free, purchase.PaymentStatus);
        Assert.Equal(EntitlementStatus.Active, purchase.EntitlementStatus);
        Assert.Equal(0, purchase.PriceSnapshotMinor);
        Assert.Equal(trackId, purchase.TrackId);
        Assert.Equal(Now, purchase.PaidAt);
    }

    [Fact]
    public void AcquireFreeTrack_rejects_non_zero_amount()
    {
        var trackId = Guid.CreateVersion7();
        var money = Money.Create(100, "USD").Value!;

        var result = Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackId, money, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.NotFreeEligible, result.Error);
    }

    [Fact]
    public void AcquireFreeRelease_creates_release_entitlement()
    {
        var releaseId = Guid.CreateVersion7();
        var money = Money.Create(0, "VND").Value!;

        var result = Purchase.AcquireFreeRelease(AccountId, OrganizationId, releaseId, money, Now);

        Assert.True(result.IsSuccess);
        var purchase = result.Value!;
        Assert.Equal(PurchasedUnit.Release, purchase.PurchasedUnit);
        Assert.Equal(releaseId, purchase.ReleaseId);
        Assert.Equal("VND", purchase.Currency);
    }
}

public sealed class EntitlementQueryTests
{
    private static readonly AccountId AccountId = AccountId.New();
    private static readonly OrganizationId OrganizationId = OrganizationId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public void OwnsTrack_true_when_release_entitlement_exists()
    {
        var releaseId = Guid.CreateVersion7();
        var trackId = Guid.CreateVersion7();
        var money = Money.Create(0, "USD").Value!;
        var releasePurchase = Purchase.AcquireFreeRelease(
            AccountId, OrganizationId, releaseId, money, Now).Value!;

        Assert.True(EntitlementQuery.OwnsTrack([releasePurchase], trackId, releaseId));
    }

    [Fact]
    public void OwnsRelease_true_when_all_sellable_tracks_owned()
    {
        var releaseId = Guid.CreateVersion7();
        var trackA = Guid.CreateVersion7();
        var trackB = Guid.CreateVersion7();
        var money = Money.Create(0, "USD").Value!;

        var purchases = new[]
        {
            Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackA, money, Now).Value!,
            Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackB, money, Now).Value!,
        };

        Assert.True(EntitlementQuery.OwnsRelease(purchases, releaseId, [trackA, trackB]));
    }

    [Fact]
    public void Duplicate_guard_blocks_second_active_track_entitlement()
    {
        var trackId = Guid.CreateVersion7();
        var money = Money.Create(0, "USD").Value!;
        var first = Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackId, money, Now).Value!;

        Assert.True(EntitlementQuery.HasActiveTrackEntitlement([first], trackId));
    }
}

public sealed class ReleaseEntitlementCompletionTests
{
    private static readonly AccountId AccountId = AccountId.New();
    private static readonly OrganizationId OrganizationId = OrganizationId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public void TryGrantOnCompleteTrackSet_grants_release_when_last_track_acquired()
    {
        var releaseId = Guid.CreateVersion7();
        var trackA = Guid.CreateVersion7();
        var trackB = Guid.CreateVersion7();
        var money = Money.Create(0, "USD").Value!;

        var existing = new List<Purchase>
        {
            Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackA, money, Now).Value!,
            Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackB, money, Now).Value!,
        };

        var result = ReleaseEntitlementCompletion.TryGrantOnCompleteTrackSet(
            AccountId,
            OrganizationId,
            releaseId,
            [trackA, trackB],
            existing,
            money,
            Now);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(PurchasedUnit.Release, result.Value!.PurchasedUnit);
        Assert.Equal(releaseId, result.Value.ReleaseId);
    }

    [Fact]
    public void TryGrantOnCompleteTrackSet_skips_when_set_incomplete()
    {
        var releaseId = Guid.CreateVersion7();
        var trackA = Guid.CreateVersion7();
        var trackB = Guid.CreateVersion7();
        var money = Money.Create(0, "USD").Value!;

        var existing = new List<Purchase>
        {
            Purchase.AcquireFreeTrack(AccountId, OrganizationId, trackA, money, Now).Value!,
        };

        var result = ReleaseEntitlementCompletion.TryGrantOnCompleteTrackSet(
            AccountId,
            OrganizationId,
            releaseId,
            [trackA, trackB],
            existing,
            money,
            Now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }
}
