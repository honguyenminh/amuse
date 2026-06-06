using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Services;

internal sealed class BusinessPortalProfileService(TenancyDbContext dbContext, IClock clock)
{
    public async Task<BusinessPortalProfile> GetOrCreateAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.BusinessPortalProfiles
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        if (profile is not null)
            return profile;

        profile = BusinessPortalProfile.Create(accountId, clock.UtcNow);
        dbContext.BusinessPortalProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<BusinessPortalProfile?> GetAsync(
        AccountId accountId,
        CancellationToken cancellationToken) =>
        await dbContext.BusinessPortalProfiles
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}

internal sealed class BusinessPortalProfileLookup(TenancyDbContext dbContext)
    : IBusinessPortalProfileLookup
{
    public async Task<IReadOnlyDictionary<Guid, BusinessPortalProfileSnapshot>> GetByAccountIdsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
            return new Dictionary<Guid, BusinessPortalProfileSnapshot>();

        var ids = accountIds.Select(AccountId.From).ToArray();
        var profiles = await dbContext.BusinessPortalProfiles.AsNoTracking()
            .Where(p => ids.Contains(p.AccountId))
            .Select(p => new BusinessPortalProfileSnapshot(
                p.AccountId.Value,
                p.DisplayName,
                p.AvatarAccentSeed,
                p.AvatarObjectKey))
            .ToListAsync(cancellationToken);

        return profiles.ToDictionary(p => p.AccountId);
    }
}

internal sealed class BusinessPortalProfileOnboardingReadModel(TenancyDbContext dbContext)
    : IBusinessPortalProfileOnboardingReadModel
{
    public async Task<bool> IsCompleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.BusinessPortalProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        return profile?.IsComplete == true;
    }
}
