using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class ListenerProfileService(ListenerDbContext dbContext, IClock clock)
{
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

    public async Task<(ListenerProfile Profile, ListenerPreference? Preference)> GetForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var result = await TryGetForAccountAsync(accountId, cancellationToken);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error!.Message);

        return result.Value!;
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
