using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Catalog;

public sealed class ReleaseGroup
{
    public const int MaxTitleLength = 300;
    public const int MaxDescriptionLength = 4000;

    public ReleaseGroupId Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public ArtistId ArtistId { get; private set; }
    public string Title { get; private set; } = null!;
    public Slug Slug { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ReleaseGroup()
    {
    }

    private ReleaseGroup(
        ReleaseGroupId id,
        OrganizationId organizationId,
        ArtistId artistId,
        string title,
        Slug slug,
        string? description,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizationId = organizationId;
        ArtistId = artistId;
        Title = title;
        Slug = slug;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Result<ReleaseGroup> Create(
        ReleaseGroupId id,
        OrganizationId organizationId,
        ArtistId artistId,
        string title,
        Slug slug,
        DateTimeOffset createdAt,
        string? description = null)
    {
        var trimmedTitle = (title ?? throw new ArgumentNullException(nameof(title))).Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
            return Result<ReleaseGroup>.Failure(CatalogErrors.InvalidReleaseGroup);

        if (description is { Length: > MaxDescriptionLength })
            return Result<ReleaseGroup>.Failure(CatalogErrors.InvalidReleaseGroup);

        return Result<ReleaseGroup>.Success(
            new ReleaseGroup(id, organizationId, artistId, trimmedTitle, slug, description, createdAt));
    }

    public Result UpdateMetadata(string title, string? description, DateTimeOffset now)
    {
        var trimmedTitle = (title ?? throw new ArgumentNullException(nameof(title))).Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
            return Result.Failure(CatalogErrors.InvalidReleaseGroup);

        if (description is { Length: > MaxDescriptionLength })
            return Result.Failure(CatalogErrors.InvalidReleaseGroup);

        Title = trimmedTitle;
        Description = description;
        UpdatedAt = now;
        return Result.Success();
    }

    public bool BelongsToArtist(ArtistId artistId) => ArtistId == artistId;
}
