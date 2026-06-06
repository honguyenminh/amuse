using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class ListenerProfileService(ListenerDbContext dbContext, IClock clock)
{
    public async Task<(ListenerProfile Profile, ListenerPreference? Preference)> GetForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ListenerProfiles
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        if (profile is null)
            throw new InvalidOperationException("Listener profile must exist before reading profile details.");

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
