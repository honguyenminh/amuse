using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Discovery;

public readonly record struct PlaylistTitle(string Value)
{
    public const int MaxLength = 200;

    public static Result<PlaylistTitle> TryCreate(string? raw)
    {
        var trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > MaxLength)
            return Result<PlaylistTitle>.Failure(DiscoveryErrors.InvalidPlaylistTitle);

        return Result<PlaylistTitle>.Success(new PlaylistTitle(trimmed));
    }
}
