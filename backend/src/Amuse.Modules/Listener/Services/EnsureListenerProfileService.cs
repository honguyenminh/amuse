using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class EnsureListenerProfileService(ListenerDbContext dbContext, IClock clock)
{
    public async Task<ListenerProfile> EnsureAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ListenerProfiles
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        if (existing is not null)
            return existing;

        var profile = ListenerProfile.Create(accountId, clock.UtcNow);
        dbContext.ListenerProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }
}
