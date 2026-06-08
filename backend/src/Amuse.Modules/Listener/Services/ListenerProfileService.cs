using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class ListenerProfileService(ListenerDbContext dbContext, IClock clock)
{
    /// <summary>Read-only load. Do not mutate returned entities.</summary>
    public async Task<Result<(ListenerProfile Profile, ListenerPreference? Preference)>> TryGetForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ListenerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        if (profile is null)
            return Result<(ListenerProfile, ListenerPreference?)>.Failure(ListenerErrors.ProfileNotFound);

        var preference = await dbContext.ListenerPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        return Result<(ListenerProfile, ListenerPreference?)>.Success((profile, preference));
    }

    /// <summary>Tracked load for handlers that mutate profile or preference.</summary>
    public async Task<(ListenerProfile Profile, ListenerPreference? Preference)> GetForAccountForUpdateAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ListenerProfiles
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        if (profile is null)
            throw new InvalidOperationException(ListenerErrors.ProfileNotFound.Message);

        var preference = await dbContext.ListenerPreferences
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        return (profile, preference);
    }

    public async Task<ListenerPreference> GetOrCreatePreferenceAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var preference = await dbContext.ListenerPreferences
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        if (preference is not null)
            return preference;

        preference = ListenerPreference.Create(accountId, clock.UtcNow);
        dbContext.ListenerPreferences.Add(preference);
        return preference;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
