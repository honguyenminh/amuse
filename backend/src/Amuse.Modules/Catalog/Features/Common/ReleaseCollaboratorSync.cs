using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class ReleaseCollaboratorSync
{
    internal static async Task<Result<IReadOnlyList<ArtistId>>> ResolveArtistIdsAsync(
        CatalogDbContext db,
        IReadOnlyList<Guid>? collaboratorArtistIds,
        CancellationToken cancellationToken)
    {
        var ids = (collaboratorArtistIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return Result<IReadOnlyList<ArtistId>>.Success([]);

        var typedIds = ids.Select(ArtistId.From).ToArray();
        var count = await db.Artists
            .AsNoTracking()
            .CountAsync(a => typedIds.Contains(a.Id), cancellationToken);

        if (count != typedIds.Length)
            return Result<IReadOnlyList<ArtistId>>.Failure(CatalogErrors.ArtistNotFound);

        return Result<IReadOnlyList<ArtistId>>.Success(typedIds);
    }

    internal static async Task<IReadOnlyList<ManageReleaseCollaboratorResponse>> LoadAsync(
        CatalogDbContext db,
        ReleaseId releaseId,
        CancellationToken cancellationToken)
    {
        return await db.ReleaseCollaborators
            .AsNoTracking()
            .Where(c => c.ReleaseId == releaseId)
            .OrderBy(c => c.DisplayOrder)
            .Join(
                db.Artists.AsNoTracking(),
                collaborator => collaborator.ArtistId,
                artist => artist.Id,
                (collaborator, artist) => new ManageReleaseCollaboratorResponse(
                    collaborator.ArtistId.Value,
                    artist.Name,
                    collaborator.Role,
                    collaborator.DisplayOrder))
            .ToListAsync(cancellationToken);
    }
}
