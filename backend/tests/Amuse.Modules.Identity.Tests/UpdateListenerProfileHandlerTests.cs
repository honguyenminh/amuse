using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Features.GetListenerProfile;
using Amuse.Modules.Listener.Features.UpdateListenerProfile;
using Amuse.Modules.Listener.Persistence;
using Amuse.Modules.Listener.Services;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amuse.Modules.Identity.Tests;

public sealed class UpdateListenerProfileHandlerTests
{
    [Fact]
    public async Task HandleAsync_persists_onboarding_updates_for_subsequent_reads()
    {
        await using var db = CreateListenerDb();
        var accountId = AccountId.From(Guid.Parse("019e8000-0000-7000-8000-000000000010"));
        var clock = new SystemClock();
        var ensureService = new EnsureListenerProfileService(db, clock);
        var profileService = new ListenerProfileService(db, clock);
        var mediaUrls = Substitute.For<IMediaPublicUrlBuilder>();

        await ensureService.EnsureAsync(accountId, CancellationToken.None);

        var updateHandler = new UpdateListenerProfileHandler(
            ensureService,
            profileService,
            mediaUrls,
            clock);

        var updateResult = await updateHandler.HandleAsync(
            new UpdateListenerProfileRequest(
                DisplayName: "Listener One",
                AvatarAccentSeed: 2,
                AllowUnverifiedArtists: false,
                ClearAvatar: null),
            TestPrincipalFactory.ForAccount(accountId.Value),
            CancellationToken.None);

        Assert.True(updateResult.IsSuccess);
        Assert.True(updateResult.Value!.OnboardingComplete);

        db.ChangeTracker.Clear();

        var getHandler = new GetListenerProfileHandler(profileService, mediaUrls);
        var getResult = await getHandler.HandleAsync(
            TestPrincipalFactory.ForAccount(accountId.Value),
            CancellationToken.None);

        Assert.True(getResult.IsSuccess);
        Assert.Equal("Listener One", getResult.Value!.DisplayName);
        Assert.Equal(2, getResult.Value.AvatarAccentSeed);
        Assert.False(getResult.Value.AllowUnverifiedArtists);
        Assert.True(getResult.Value.OnboardingComplete);
    }

    private static ListenerDbContext CreateListenerDb()
    {
        var options = new DbContextOptionsBuilder<ListenerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ListenerDbContext(options);
    }
}
