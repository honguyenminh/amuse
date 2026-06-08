using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.DownloadTrack;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class DownloadTrackHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public async Task Download_returns_forbidden_when_listener_does_not_own_track()
    {
        var trackId = Guid.CreateVersion7();
        var releaseId = Guid.CreateVersion7();
        var accountId = AccountId.New();
        var listenerProfileId = ListenerProfileId.New();

        var catalog = Substitute.For<ICatalogDiscoveryReadModel>();
        catalog.GetTrackDownloadRowAsync(Arg.Any<TrackId>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogTrackDownloadRow(trackId, releaseId, "masters/track.wav", "Track"));

        var entitlements = Substitute.For<IEntitlementReadModel>();
        entitlements.OwnsTrackAsync(accountId, trackId, releaseId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler(
            catalog,
            entitlements,
            personaReadModel: TestPrincipalFactory.ListenerPersonaReadModel(accountId, listenerProfileId));

        var result = await handler.HandleAsync(
            trackId,
            TestPrincipalFactory.Listener(accountId, listenerProfileId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.DownloadForbidden, result.Error);
    }

    [Fact]
    public async Task Download_returns_signed_url_when_listener_owns_track()
    {
        var trackId = Guid.CreateVersion7();
        var releaseId = Guid.CreateVersion7();
        var accountId = AccountId.New();
        var listenerProfileId = ListenerProfileId.New();
        const string masterKey = "masters/owned-track.wav";

        var catalog = Substitute.For<ICatalogDiscoveryReadModel>();
        catalog.GetTrackDownloadRowAsync(Arg.Any<TrackId>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogTrackDownloadRow(trackId, releaseId, masterKey, "Owned Track"));

        var entitlements = Substitute.For<IEntitlementReadModel>();
        entitlements.OwnsTrackAsync(accountId, trackId, releaseId, Arg.Any<CancellationToken>())
            .Returns(true);

        var storage = Substitute.For<IObjectStorage>();
        storage.GetSignedUrl(MediaBucket.Audio, masterKey, Arg.Any<TimeSpan>())
            .Returns("https://example.test/signed");

        var handler = CreateHandler(
            catalog,
            entitlements,
            storage,
            TestPrincipalFactory.ListenerPersonaReadModel(accountId, listenerProfileId));

        var result = await handler.HandleAsync(
            trackId,
            TestPrincipalFactory.Listener(accountId, listenerProfileId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://example.test/signed", result.Value!.Url);
        Assert.Equal("audio/wav", result.Value.ContentType);
        Assert.Equal("Owned Track.wav", result.Value.FileName);
        Assert.Equal(Now.AddMinutes(30), result.Value.ExpiresAt);
    }

    private static DownloadTrackHandler CreateHandler(
        ICatalogDiscoveryReadModel catalog,
        IEntitlementReadModel entitlements,
        IObjectStorage? storage = null,
        IListenerPersonaReadModel? personaReadModel = null)
    {
        storage ??= Substitute.For<IObjectStorage>();
        personaReadModel ??= TestPrincipalFactory.ListenerPersonaReadModel(
            AccountId.New(),
            ListenerProfileId.New());
        return new DownloadTrackHandler(
            catalog,
            entitlements,
            storage,
            new FixedClock(Now),
            Options.Create(new MediaOptions { SignedUrlMinutes = 30 }),
            personaReadModel);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
