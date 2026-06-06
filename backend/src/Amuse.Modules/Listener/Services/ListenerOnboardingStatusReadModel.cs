using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class ListenerOnboardingStatusReadModel(ListenerDbContext dbContext)
    : IListenerOnboardingStatusReadModel
{
    public async Task<bool> IsOnboardingCompleteAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ListenerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        if (profile is null)
            return false;

        var preference = await dbContext.ListenerPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        return ListenerOnboarding.IsComplete(profile, preference);
    }
}
