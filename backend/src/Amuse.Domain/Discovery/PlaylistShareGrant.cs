namespace Amuse.Domain.Discovery;

public sealed class PlaylistShareGrant
{
    public ShareGrantEmail Email { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }

    private PlaylistShareGrant()
    {
    }

    internal static PlaylistShareGrant Create(ShareGrantEmail email, DateTimeOffset grantedAt) =>
        new()
        {
            Email = email,
            GrantedAt = grantedAt,
        };

    internal static PlaylistShareGrant Rehydrate(ShareGrantEmail email, DateTimeOffset grantedAt) =>
        new()
        {
            Email = email,
            GrantedAt = grantedAt,
        };
}
