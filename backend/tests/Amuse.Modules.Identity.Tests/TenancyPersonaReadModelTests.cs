using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Platform.Persistence;
using Amuse.Modules.Platform.Services;
using Amuse.Modules.Tenancy.Persistence;
using Amuse.Modules.Tenancy.Services;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Tests;

public sealed class TenancyPersonaReadModelTests
{
    [Fact]
    public async Task Platform_root_lists_and_assumes_unowned_org()
    {
        await using var tenancyDb = CreateTenancyDb();
        await using var platformDb = CreatePlatformDb();

        var owner = AccountId.From(Guid.CreateVersion7());
        var rootAccount = AccountId.From(Guid.Parse("00000000-0000-7000-8000-000000000001"));

        var org = Organization.RegisterIndieGroup("Other Org", owner, DateTimeOffset.UtcNow).Value!;
        tenancyDb.Organizations.Add(org);
        await tenancyDb.SaveChangesAsync();

        platformDb.PlatformOperators.Add(
            PlatformOperator.Create(
                PlatformOperatorId.Root,
                rootAccount,
                [],
                DateTimeOffset.UtcNow));
        await platformDb.SaveChangesAsync();

        var readModel = new TenancyPersonaReadModel(
            tenancyDb,
            new PlatformOperatorLookup(platformDb));

        var listings = await readModel.ListAvailableOrgsAsync(rootAccount, CancellationToken.None);
        Assert.Contains(listings, listing => listing.OrganizationId == org.Id.Value);

        var persona = await readModel.GetOrgContextAsync(rootAccount, org.Id, CancellationToken.None);
        Assert.True(persona.IsSuccess);
        Assert.Contains("manage:org:all", persona.Value!.Claims);
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
