namespace Amuse.Domain.Catalog;

public sealed class Artist
{
    public const int MaxNameLength = 200;
    public const int MaxBioLength = 4000;
    public const int MaxKeyLength = 512;

    public ArtistId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Slug Slug { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarKey { get; private set; }
    public string? CoverKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Artist()
    {
    }

    private Artist(
        ArtistId id,
        string name,
        Slug slug,
        string? bio,
        string? avatarKey,
        string? coverKey,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Bio = bio;
        AvatarKey = avatarKey;
        CoverKey = coverKey;
        CreatedAt = createdAt;
    }

    public static Artist Create(
        ArtistId id,
        string name,
        Slug slug,
        DateTimeOffset createdAt,
        string? bio = null,
        string? avatarKey = null,
        string? coverKey = null)
    {
        var trimmedName = (name ?? throw new ArgumentNullException(nameof(name))).Trim();
        if (trimmedName.Length is 0 or > MaxNameLength)
            throw new ArgumentException(
                $"Artist name must be 1..{MaxNameLength} characters.",
                nameof(name));

        if (bio is { Length: > MaxBioLength })
            throw new ArgumentException($"Bio exceeds {MaxBioLength} characters.", nameof(bio));

        ValidateKey(avatarKey, nameof(avatarKey));
        ValidateKey(coverKey, nameof(coverKey));

        return new Artist(id, trimmedName, slug, bio, avatarKey, coverKey, createdAt);
    }

    private static void ValidateKey(string? key, string paramName)
    {
        if (key is null) return;
        if (key.Length is 0 or > MaxKeyLength)
            throw new ArgumentException(
                $"{paramName} must be 1..{MaxKeyLength} characters.",
                paramName);
    }
}
