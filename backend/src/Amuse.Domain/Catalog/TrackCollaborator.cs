using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public sealed class TrackCollaborator
{
    public const int MaxDisplayNameLength = 300;

    public TrackCollaboratorId Id { get; private set; }
    public TrackId TrackId { get; private set; }
    public ArtistId? ArtistId { get; private set; }
    public string? DisplayName { get; private set; }
    public TrackCollaboratorRole Role { get; private set; }
    public int DisplayOrder { get; private set; }

    private TrackCollaborator()
    {
    }

    private TrackCollaborator(
        TrackCollaboratorId id,
        TrackId trackId,
        ArtistId? artistId,
        string? displayName,
        TrackCollaboratorRole role,
        int displayOrder)
    {
        Id = id;
        TrackId = trackId;
        ArtistId = artistId;
        DisplayName = displayName;
        Role = role;
        DisplayOrder = displayOrder;
    }

    public static Result<TrackCollaborator> Create(
        TrackCollaboratorId id,
        TrackId trackId,
        ArtistId? artistId,
        string? displayName,
        ArtistId primaryArtistId,
        TrackCollaboratorRole role,
        int displayOrder)
    {
        if (displayOrder <= 0)
            return Result<TrackCollaborator>.Failure(CatalogErrors.InvalidCollaborator);

        if (artistId is { } linkedArtistId)
        {
            if (linkedArtistId == primaryArtistId)
                return Result<TrackCollaborator>.Failure(CatalogErrors.InvalidCollaborator);

            var normalizedName = NormalizeDisplayName(displayName);
            if (normalizedName is not null)
                return Result<TrackCollaborator>.Failure(CatalogErrors.InvalidCollaborator);

            return Result<TrackCollaborator>.Success(
                new TrackCollaborator(id, trackId, linkedArtistId, null, role, displayOrder));
        }

        var placeholderName = NormalizeDisplayName(displayName);
        if (placeholderName is null)
            return Result<TrackCollaborator>.Failure(CatalogErrors.InvalidCollaborator);

        return Result<TrackCollaborator>.Success(
            new TrackCollaborator(id, trackId, null, placeholderName, role, displayOrder));
    }

    private static string? NormalizeDisplayName(string? value)
    {
        if (value is null)
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length is 0 or > MaxDisplayNameLength)
            return null;

        return trimmed;
    }
}
