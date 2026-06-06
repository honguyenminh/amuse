using Amuse.Domain.Listener;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class ListenerProfilePresentationReadModel(ListenerDbContext db)
    : IListenerProfilePresentationReadModel
{
    public async Task<IReadOnlyDictionary<Guid, ListenerPresentationRow>> GetPresentationsAsync(
        IEnumerable<ListenerProfileId> listenerIds,
        CancellationToken cancellationToken)
    {
        var ids = listenerIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, ListenerPresentationRow>();

        var rows = await db.ListenerProfiles.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ListenerPresentationRow(
                p.Id.Value,
                p.DisplayName,
                p.AvatarAccentSeed,
                p.AvatarObjectKey))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ListenerProfileId);
    }
}
