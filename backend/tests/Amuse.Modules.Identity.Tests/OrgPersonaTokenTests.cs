using System.IdentityModel.Tokens.Jwt;
using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Tenancy.Persistence;
using Amuse.Modules.Tenancy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Tests;

public sealed class OrgPersonaTokenTests
{
    [Fact]
    public async Task Indie_org_token_includes_upload_and_publish_to_discover()
    {
        await using var tenancyDb = CreateTenancyDb();
        await using var identityDb = CreateIdentityDb();

        var account = Account.Create(IdpIssuer.From("local"), IdpSubject.From("org-user"));
        identityDb.Accounts.Add(account);

        var org = Organization.RegisterIndieGroup("My Indie", account.Id, DateTimeOffset.UtcNow).Value!;
        var owner = OrganizationMember.CreateOwner(
            org.Id,
            account.Id,
            OrgClaimPresets.OwnerPresetLabel,
            OrgClaimPresets.OwnerAdmin);
        tenancyDb.Organizations.Add(org);
        tenancyDb.OrganizationMembers.Add(owner);
        await identityDb.SaveChangesAsync();
        await tenancyDb.SaveChangesAsync();

        var readModel = new TenancyPersonaReadModel(tenancyDb, NullPlatformOperatorLookup.Instance);
        var persona = await readModel.GetOrgContextAsync(account.Id, org.Id, CancellationToken.None);
        Assert.True(persona.IsSuccess);

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "amuse",
            Audience = "amuse-api",
            SigningKey = "DEV_ONLY_CHANGE_ME_32_CHARS_MINIMUM_KEY",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 14,
        });
        var issuer = new TokenIssuer(jwtOptions);
        var token = issuer.CreateAccessToken(account.Id, persona.Value!, DateTimeOffset.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var claims = jwt.Claims.Where(c => c.Type == "claims").Select(c => c.Value).ToList();

        Assert.Contains("upload:catalog:all", claims);
        Assert.Contains("publish_public:catalog:all", claims);
    }

    private static IdentityDbContext CreateIdentityDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private static TenancyDbContext CreateTenancyDb()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TenancyDbContext(options);
    }
}
