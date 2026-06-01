using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Persistence;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Services;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Tests;

public sealed class OrganizationLifecycleServiceTests
{
  [Fact]
  public async Task Approve_transitions_backing_org_to_approved()
  {
    await using var db = CreateDb();
    var accountId = AccountId.From(Guid.CreateVersion7());
    var org = Organization.RegisterBackingOrg("Label Co", accountId, DateTimeOffset.UtcNow).Value!;
    db.Organizations.Add(org);
    await db.SaveChangesAsync();

    var clock = new FixedClock(DateTimeOffset.Parse("2026-05-27T12:00:00+00:00"));
    IOrganizationLifecycleCommands service = new OrganizationLifecycleService(
      db,
      new EmptyCreatorContactLookup(),
      new NoOpCatalogManagedArtistVisibility(),
      clock);

    var result = await service.ApproveBackingOrganizationAsync(
      org.Id,
      PlatformOperatorId.Root,
      CancellationToken.None);

    Assert.True(result.IsSuccess);

    var reloaded = await db.Organizations.SingleAsync(o => o.Id == org.Id);
    Assert.Equal(OrganizationOnboardingStatus.Approved, reloaded.OnboardingStatus);
    Assert.Equal(OrganizationTrustTier.PlatformVerified, reloaded.TrustTier);
  }

  private static TenancyDbContext CreateDb()
  {
    var options = new DbContextOptionsBuilder<TenancyDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new TenancyDbContext(options);
  }

  private sealed class FixedClock(DateTimeOffset now) : IClock
  {
    public DateTimeOffset UtcNow => now;
  }

  private sealed class EmptyCreatorContactLookup : IOrganizationCreatorContactLookup
  {
    public Task<IReadOnlyDictionary<Guid, OrganizationApplicationOwner>> GetByAccountIdsAsync(
      IReadOnlyCollection<AccountId> accountIds,
      CancellationToken cancellationToken) =>
      Task.FromResult<IReadOnlyDictionary<Guid, OrganizationApplicationOwner>>(
        new Dictionary<Guid, OrganizationApplicationOwner>());
  }

  private sealed class NoOpCatalogManagedArtistVisibility : ICatalogManagedArtistVisibility
  {
    public Task SyncManagedArtistsForOrganizationAsync(
      OrganizationId organizationId,
      OrganizationTrustTier organizationTrustTier,
      CancellationToken cancellationToken = default) =>
      Task.CompletedTask;
  }
}
