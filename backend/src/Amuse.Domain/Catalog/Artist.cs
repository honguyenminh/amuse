namespace Amuse.Domain.Catalog;

public sealed class Artist
{
    public const int MaxNameLength = 200;
    public const int MaxBioLength = 4000;
    public const int MaxUrlLength = 1024;

    public ArtistId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Slug Slug { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? CoverUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Artist()
    {
    }

    private Artist(
        ArtistId id,
        string name,
        Slug slug,
        string? bio,
        string? avatarUrl,
        string? coverUrl,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Bio = bio;
        AvatarUrl = avatarUrl;
        CoverUrl = coverUrl;
        CreatedAt = createdAt;
    }

    public static Artist Create(
        ArtistId id,
        string name,
        Slug slug,
        DateTimeOffset createdAt,
        string? bio = null,
        string? avatarUrl = null,
        string? coverUrl = null)
    {
        var trimmedName = (name ?? throw new ArgumentNullException(nameof(name))).Trim();
        if (trimmedName.Length is 0 or > MaxNameLength)
            throw new ArgumentException(
                $"Artist name must be 1..{MaxNameLength} characters.",
                nameof(name));

        if (bio is { Length: > MaxBioLength })
            throw new ArgumentException($"Bio exceeds {MaxBioLength} characters.", nameof(bio));

        ValidateUrl(avatarUrl, nameof(avatarUrl));
        ValidateUrl(coverUrl, nameof(coverUrl));

        return new Artist(id, trimmedName, slug, bio, avatarUrl, coverUrl, createdAt);
    }

    private static void ValidateUrl(string? url, string paramName)
    {
        if (url is null) return;
        if (url.Length > MaxUrlLength)
            throw new ArgumentException($"{paramName} exceeds {MaxUrlLength} characters.", paramName);
    }
}
