using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Shared;

internal static class ReleaseCollaboratorSync
{
    internal static async Task<Result<IReadOnlyList<ManageReleaseCollaboratorResponse>>> ReplaceAsync(
        CatalogDbContext db,
        ReleaseId releaseId,
        ArtistId primaryArtistId,
        IReadOnlyList<Guid>? collaboratorArtistIds,
        CancellationToken cancellationToken)
    {
        var ids = (collaboratorArtistIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Any(id => id == primaryArtistId.Value))
            return Result<IReadOnlyList<ManageReleaseCollaboratorResponse>>.Failure(
                CatalogErrors.InvalidCollaborator);

        var existing = await db.ReleaseCollaborators
            .Where(c => c.ReleaseId == releaseId)
            .ToListAsync(cancellationToken);

        db.ReleaseCollaborators.RemoveRange(existing);

        if (ids.Length == 0)
            return Result<IReadOnlyList<ManageReleaseCollaboratorResponse>>.Success([]);

        var typedIds = ids.Select(ArtistId.From).ToArray();
        var artists = await db.Artists
            .AsNoTracking()
            .Where(a => typedIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(cancellationToken);

        if (artists.Count != typedIds.Length)
            return Result<IReadOnlyList<ManageReleaseCollaboratorResponse>>.Failure(
                CatalogErrors.ArtistNotFound);

        var order = 1;
        var responses = new List<ManageReleaseCollaboratorResponse>(ids.Length);
        foreach (var artistId in ids)
        {
            var typedArtistId = ArtistId.From(artistId);
            var createResult = ReleaseCollaborator.Create(
                releaseId,
                typedArtistId,
                primaryArtistId,
                ReleaseCollaboratorRole.Featured,
                order);

            if (!createResult.IsSuccess)
                return Result<IReadOnlyList<ManageReleaseCollaboratorResponse>>.Failure(
                    createResult.Error!);

            db.ReleaseCollaborators.Add(createResult.Value!);
            var name = artists.First(a => a.Id == typedArtistId).Name;
            responses.Add(new ManageReleaseCollaboratorResponse(
                typedArtistId.Value,
                name,
                ReleaseCollaboratorRole.Featured,
                order));
            order++;
        }

        return Result<IReadOnlyList<ManageReleaseCollaboratorResponse>>.Success(responses);
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
