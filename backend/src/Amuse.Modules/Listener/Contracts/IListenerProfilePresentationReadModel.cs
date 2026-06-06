using Amuse.Domain.Listener;

namespace Amuse.Modules.Listener.Contracts;

public sealed record ListenerPresentationRow(
    Guid ListenerProfileId,
    string? DisplayName,
    int? AvatarAccentSeed,
    string? AvatarObjectKey);

public interface IListenerProfilePresentationReadModel
{
    Task<IReadOnlyDictionary<Guid, ListenerPresentationRow>> GetPresentationsAsync(
        IEnumerable<ListenerProfileId> listenerIds,
        CancellationToken cancellationToken);
}
