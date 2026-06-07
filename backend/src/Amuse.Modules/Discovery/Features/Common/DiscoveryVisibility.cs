using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Discovery.Features.Common;

internal static class DiscoveryVisibility
{
    public static Result<PlaylistVisibility> TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<PlaylistVisibility>.Failure(DiscoveryErrors.InvalidPlaylistTitle);

        return raw.Trim().ToLowerInvariant() switch
        {
            "private" => Result<PlaylistVisibility>.Success(PlaylistVisibility.Private),
            "public" => Result<PlaylistVisibility>.Success(PlaylistVisibility.Public),
            _ => Result<PlaylistVisibility>.Failure(DiscoveryErrors.InvalidPlaylistTitle),
        };
    }

    public static string ToApiValue(PlaylistVisibility visibility) =>
        visibility switch
        {
            PlaylistVisibility.Private => "private",
            PlaylistVisibility.Public => "public",
            _ => "private",
        };
}
