using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Features.CreateCheckoutSession;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class CreateCheckoutSessionHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public async Task CreateCheckout_rejects_amount_below_floor()
    {
        var trackId = Guid.CreateVersion7();
        var orgId = OrganizationId.New();
        var accountId = AccountId.New();
        var listenerProfileId = ListenerProfileId.New();

        var catalog = Substitute.For<ICatalogPurchaseReadModel>();
        catalog.GetCheckoutTrackAsync(Arg.Any<TrackId>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogCheckoutTrackRow(
                trackId,
                Guid.CreateVersion7(),
                orgId,
                "Track",
                500,
                1000,
                "USD",
                []));

        var tenancy = Substitute.For<ITenancyOrganizationReadModel>();
        tenancy.GetLifecycleStatusAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(OrganizationLifecycleStatus.Active);

        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var handler = new CreateCheckoutSessionHandler(
            new BillingDbContext(options),
            CreateIdentityDb(accountId),
            catalog,
            tenancy,
            TestPrincipalFactory.ListenerPersonaReadModel(accountId, listenerProfileId),
            Substitute.For<Amuse.Modules.Billing.Contracts.ICheckoutProvider>(),
            Options.Create(new CheckoutConfig
            {
                SuccessUrl = "http://localhost/success",
                CancelUrl = "http://localhost/cancel",
            }),
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            new CreateCheckoutSessionRequest(trackId, null, 100),
            TestPrincipalFactory.Listener(accountId, listenerProfileId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.InvalidCheckoutAmount, result.Error);
    }

    [Fact]
    public async Task CreateCheckout_rejects_banned_account()
    {
        var trackId = Guid.CreateVersion7();
        var orgId = OrganizationId.New();
        var accountId = AccountId.New();
        var listenerProfileId = ListenerProfileId.New();

        var catalog = Substitute.For<ICatalogPurchaseReadModel>();
        catalog.GetCheckoutTrackAsync(Arg.Any<TrackId>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogCheckoutTrackRow(
                trackId,
                Guid.CreateVersion7(),
                orgId,
                "Track",
                500,
                null,
                "USD",
                []));

        var tenancy = Substitute.For<ITenancyOrganizationReadModel>();
        tenancy.GetLifecycleStatusAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(OrganizationLifecycleStatus.Active);

        var identityDb = CreateIdentityDb(accountId, banned: true);

        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var handler = new CreateCheckoutSessionHandler(
            new BillingDbContext(options),
            identityDb,
            catalog,
            tenancy,
            TestPrincipalFactory.ListenerPersonaReadModel(accountId, listenerProfileId),
            Substitute.For<Amuse.Modules.Billing.Contracts.ICheckoutProvider>(),
            Options.Create(new CheckoutConfig
            {
                SuccessUrl = "http://localhost/success",
                CancelUrl = "http://localhost/cancel",
            }),
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            new CreateCheckoutSessionRequest(trackId, null, 500),
            TestPrincipalFactory.Listener(accountId, listenerProfileId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.AccountBanned, result.Error);
    }

    [Fact]
    public async Task CreateCheckout_rejects_when_org_is_suspended()
    {
        var trackId = Guid.CreateVersion7();
        var orgId = OrganizationId.New();
        var accountId = AccountId.New();
        var listenerProfileId = ListenerProfileId.New();

        var catalog = Substitute.For<ICatalogPurchaseReadModel>();
        catalog.GetCheckoutTrackAsync(Arg.Any<TrackId>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogCheckoutTrackRow(
                trackId,
                Guid.CreateVersion7(),
                orgId,
                "Track",
                500,
                null,
                "USD",
                []));

        var tenancy = Substitute.For<ITenancyOrganizationReadModel>();
        tenancy.GetLifecycleStatusAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(OrganizationLifecycleStatus.Suspended);

        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var handler = new CreateCheckoutSessionHandler(
            new BillingDbContext(options),
            CreateIdentityDb(accountId),
            catalog,
            tenancy,
            TestPrincipalFactory.ListenerPersonaReadModel(accountId, listenerProfileId),
            Substitute.For<Amuse.Modules.Billing.Contracts.ICheckoutProvider>(),
            Options.Create(new CheckoutConfig
            {
                SuccessUrl = "http://localhost/success",
                CancelUrl = "http://localhost/cancel",
            }),
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            new CreateCheckoutSessionRequest(trackId, null, 500),
            TestPrincipalFactory.Listener(accountId, listenerProfileId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.OrgSalesBlocked, result.Error);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static IdentityDbContext CreateIdentityDb(AccountId accountId, bool banned = false)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new IdentityDbContext(options);
        var account = Account.CreateWithId(
            accountId,
            IdpIssuer.From("test"),
            IdpSubject.From("subject"));

        if (banned)
            account.Ban(Now);

        db.Accounts.Add(account);
        db.SaveChanges();
        return db;
    }
}
