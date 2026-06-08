using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Features.AcquireFree;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class AcquireFreeHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public async Task AcquireFree_track_rejects_when_floor_above_zero()
    {
        var trackId = Guid.CreateVersion7();
        var releaseId = Guid.CreateVersion7();
        var orgId = OrganizationId.New();
        var accountId = AccountId.New();
        var listenerProfileId = ListenerProfileId.New();

        var catalog = Substitute.For<ICatalogPurchaseReadModel>();
        catalog.GetSellableTrackAsync(Arg.Any<TrackId>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogSellableTrackRow(trackId, releaseId, orgId, 500, null, "USD", true));

        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var handler = new AcquireFreeHandler(
            new BillingDbContext(options),
            CreateIdentityDb(accountId),
            catalog,
            TestPrincipalFactory.ListenerPersonaReadModel(accountId, listenerProfileId),
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            new FreeAcquisitionRequest(trackId, null),
            TestPrincipalFactory.Listener(accountId, listenerProfileId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.NotFreeEligible, result.Error);
    }

    [Fact]
    public async Task AcquireFree_track_persists_and_grants_complete_set_release()
    {
        var accountId = AccountId.New();
        var listenerProfileId = ListenerProfileId.New();
        var orgId = OrganizationId.New();
        var releaseId = Guid.CreateVersion7();
        var trackA = Guid.CreateVersion7();
        var trackB = Guid.CreateVersion7();

        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var billingDb = new BillingDbContext(options);

        var catalog = Substitute.For<ICatalogPurchaseReadModel>();
        catalog.GetSellableTrackAsync(TrackId.From(trackB), Arg.Any<CancellationToken>())
            .Returns(new CatalogSellableTrackRow(trackB, releaseId, orgId, 0, null, "USD", true));
        catalog.GetSellablePublishedTrackIdsForReleaseAsync(ReleaseId.From(releaseId), Arg.Any<CancellationToken>())
            .Returns(new[] { trackA, trackB });

        billingDb.Purchases.Add(
            Purchase.AcquireFreeTrack(
                accountId,
                orgId,
                trackA,
                Money.Create(0, "USD").Value!,
                Now).Value!);
        await billingDb.SaveChangesAsync();

        var handler = new AcquireFreeHandler(
            billingDb,
            CreateIdentityDb(accountId),
            catalog,
            TestPrincipalFactory.ListenerPersonaReadModel(accountId, listenerProfileId),
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            new FreeAcquisitionRequest(trackB, null),
            TestPrincipalFactory.Listener(accountId, listenerProfileId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.ReleaseEntitlementGranted);

        var purchases = await billingDb.Purchases.ToListAsync();
        Assert.Equal(3, purchases.Count);
        Assert.Contains(purchases, p => p.PurchasedUnit == PurchasedUnit.Release && p.ReleaseId == releaseId);
    }

    private static Identity.Persistence.IdentityDbContext CreateIdentityDb(AccountId accountId)
    {
        var options = new DbContextOptionsBuilder<Identity.Persistence.IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new Identity.Persistence.IdentityDbContext(options);
        db.Accounts.Add(Account.CreateWithId(
            accountId,
            IdpIssuer.From("test"),
            IdpSubject.From("subject")));
        db.SaveChanges();
        return db;
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}

internal static class TestPrincipalFactory
{
    public static System.Security.Claims.ClaimsPrincipal Listener(
        AccountId accountId,
        ListenerProfileId listenerProfileId)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                accountId.Value.ToString()),
            new System.Security.Claims.Claim("listener_id", listenerProfileId.Value.ToString()),
        };
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }

    public static IListenerPersonaReadModel ListenerPersonaReadModel(
        AccountId accountId,
        ListenerProfileId listenerProfileId)
    {
        var readModel = Substitute.For<IListenerPersonaReadModel>();
        readModel.GetProfileIdForAccountAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(listenerProfileId);
        return readModel;
    }
}
