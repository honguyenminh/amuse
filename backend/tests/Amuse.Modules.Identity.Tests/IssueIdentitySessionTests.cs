using Amuse.Domain.Identity;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Listener.Persistence;
using Amuse.Modules.Listener.Services;
using Amuse.Modules.Platform.Persistence;
using Amuse.Modules.Platform.Services;
using Amuse.Modules.Tenancy.Persistence;
using Amuse.Modules.Tenancy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Tests;

public sealed class IssueIdentitySessionTests
{
    [Fact]
    public async Task Issue_rejects_invalid_persona_context()
    {
        await using var identityDb = CreateIdentityDb();
        await using var listenerDb = CreateListenerDb();
        await using var tenancyDb = CreateTenancyDb();
        await using var platformDb = CreatePlatformDb();

        var account = Account.Create(IdpIssuer.From("local"), IdpSubject.From("user-1"));
        identityDb.Accounts.Add(account);
        await identityDb.SaveChangesAsync();

        var jwt = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "amuse",
            Audience = "amuse-api",
            SigningKey = "DEV_ONLY_CHANGE_ME_32_CHARS_MINIMUM_KEY",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 14,
        });

        var issue = await IssueIdentitySession.IssueAsync(
            identityDb,
            new TokenIssuer(jwt),
            new TenancyPersonaReadModel(tenancyDb),
            new ListenerPersonaReadModel(listenerDb),
            new PlatformPersonaReadModel(platformDb),
            jwt.Value,
            account,
            PersonaContext.ForListener(Guid.CreateVersion7()),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.False(issue.IsSuccess);
    }

    private static IdentityDbContext CreateIdentityDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private static ListenerDbContext CreateListenerDb()
    {
        var options = new DbContextOptionsBuilder<ListenerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ListenerDbContext(options);
    }

    private static TenancyDbContext CreateTenancyDb()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TenancyDbContext(options);
    }

    private static PlatformDbContext CreatePlatformDb()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PlatformDbContext(options);
    }
}
