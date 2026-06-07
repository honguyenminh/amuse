using Amuse.Domain.Identity;
using Amuse.Modules.Audit;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Features.RevokeToken;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using JwtOptions = Amuse.Modules.Identity.Options.JwtOptions;

namespace Amuse.Modules.Identity.Tests;

public sealed class RevokeTokenHandlerTests
{
    [Fact]
    public async Task HandleAsync_revokes_access_token_jti_from_authorization_header()
    {
        await using var db = CreateIdentityDb();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var issuer = new TokenIssuer(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "amuse",
            Audience = "amuse-api",
            SigningKey = "DEV_ONLY_CHANGE_ME_32_CHARS_MINIMUM_KEY",
            AccessTokenMinutes = 15,
        }));

        var account = Account.Create(IdpIssuer.From("local"), IdpSubject.From("user-1"));
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var access = issuer.CreateAccessToken(
            account.Id,
            new PersonaAccessContext("platform", null, null, null, []),
            clock.UtcNow);

        var authorization = $"Bearer {access}";
        Assert.True(AccessTokenClaims.TryReadJtiAndExpiry(authorization, out var jti, out var expiresAt));

        var blacklist = Substitute.For<IJwtBlacklistStore>();
        var handler = new RevokeTokenHandler(db, blacklist, Substitute.For<IAuditWriter>(), clock);
        var result = await handler.HandleAsync(null, authorization, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await blacklist.Received(1).RevokeAsync(jti, expiresAt, CancellationToken.None);
        Assert.True(expiresAt > clock.UtcNow);
    }

    private static IdentityDbContext CreateIdentityDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
