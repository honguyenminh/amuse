using Amuse.Domain.Discovery;

namespace Amuse.Modules.Discovery.Features.Shared;

internal static class DiscoveryKind
{
    public static string ToApiValue(PlaylistKind kind) =>
        kind switch
        {
            PlaylistKind.Liked => "liked",
            _ => "user",
        };
}
