using Amuse.Domain.Listener;

namespace Amuse.Domain.Discovery;

public sealed record PlaylistViewContext(
    ListenerProfileId? ViewerProfileId,
    string? ViewerEmailNormalized);
