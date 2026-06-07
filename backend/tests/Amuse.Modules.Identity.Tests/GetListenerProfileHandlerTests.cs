using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Features.GetListenerProfile;
using Amuse.Modules.Listener.Persistence;
using Amuse.Modules.Listener.Services;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amuse.Modules.Identity.Tests;

public sealed class GetListenerProfileHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_profile_not_found_when_listener_profile_missing()
    {
        await using var db = CreateListenerDb();
        var accountId = AccountId.From(Guid.Parse("019e8000-0000-7000-8000-000000000001"));
        var handler = new GetListenerProfileHandler(
            new ListenerProfileService(db, new SystemClock()),
            Substitute.For<IMediaPublicUrlBuilder>());

        var principal = TestPrincipalFactory.ForAccount(accountId.Value);
        var result = await handler.HandleAsync(principal, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListenerErrors.ProfileNotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_does_not_create_profile_on_get()
    {
        await using var db = CreateListenerDb();
        var accountId = AccountId.From(Guid.Parse("019e8000-0000-7000-8000-000000000002"));
        var handler = new GetListenerProfileHandler(
            new ListenerProfileService(db, new SystemClock()),
            Substitute.For<IMediaPublicUrlBuilder>());

        var principal = TestPrincipalFactory.ForAccount(accountId.Value);
        _ = await handler.HandleAsync(principal, CancellationToken.None);

        Assert.Equal(0, await db.ListenerProfiles.CountAsync());
    }

    private static ListenerDbContext CreateListenerDb()
    {
        var options = new DbContextOptionsBuilder<ListenerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ListenerDbContext(options);
    }
}

internal static class TestPrincipalFactory
{
    internal static System.Security.Claims.ClaimsPrincipal ForAccount(Guid accountId)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, accountId.ToString()),
            new System.Security.Claims.Claim("sub", accountId.ToString()),
            new System.Security.Claims.Claim("ctx", "listener"),
        ],
        authenticationType: "test");

        return new System.Security.Claims.ClaimsPrincipal(identity);
    }
}
