using Amuse.Domain.Identity;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class ListenerPreferenceReadModel(ListenerDbContext db) : IListenerPreferenceReadModel
{
    public async Task<bool?> GetAllowUnverifiedArtistsAsync(
        AccountId accountId,
        CancellationToken cancellationToken) =>
        await db.ListenerPreferences.AsNoTracking()
            .Where(p => p.AccountId == accountId)
            .Select(p => p.AllowUnverifiedArtists)
            .FirstOrDefaultAsync(cancellationToken);
}
