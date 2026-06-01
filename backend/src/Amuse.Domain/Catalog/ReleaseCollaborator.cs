using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public sealed class ReleaseCollaborator
{
    public ReleaseId ReleaseId { get; private set; }
    public ArtistId ArtistId { get; private set; }
    public ReleaseCollaboratorRole Role { get; private set; }
    public int DisplayOrder { get; private set; }

    private ReleaseCollaborator()
    {
    }

    private ReleaseCollaborator(
        ReleaseId releaseId,
        ArtistId artistId,
        ReleaseCollaboratorRole role,
        int displayOrder)
    {
        ReleaseId = releaseId;
        ArtistId = artistId;
        Role = role;
        DisplayOrder = displayOrder;
    }

    public static Result<ReleaseCollaborator> Create(
        ReleaseId releaseId,
        ArtistId artistId,
        ArtistId primaryArtistId,
        ReleaseCollaboratorRole role,
        int displayOrder)
    {
        if (artistId == primaryArtistId)
            return Result<ReleaseCollaborator>.Failure(CatalogErrors.InvalidCollaborator);

        if (displayOrder <= 0)
            return Result<ReleaseCollaborator>.Failure(CatalogErrors.InvalidCollaborator);

        return Result<ReleaseCollaborator>.Success(
            new ReleaseCollaborator(releaseId, artistId, role, displayOrder));
    }
}
