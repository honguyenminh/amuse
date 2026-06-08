using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class TrackCollaboratorSync
{
    internal static async Task<Result<IReadOnlyList<TrackCollaboratorAssignment>>> ResolveAssignmentsAsync(
        CatalogDbContext db,
        IReadOnlyList<TrackCollaboratorEntryRequest>? collaborators,
        CancellationToken cancellationToken)
    {
        var entries = collaborators ?? [];
        if (entries.Count == 0)
            return Result<IReadOnlyList<TrackCollaboratorAssignment>>.Success([]);

        var assignments = new List<TrackCollaboratorAssignment>(entries.Count);
        var linkedArtistIds = new HashSet<Guid>();

        foreach (var entry in entries)
        {
            if (entry.ArtistId is { } artistGuid && artistGuid != Guid.Empty)
            {
                if (!linkedArtistIds.Add(artistGuid))
                    return Result<IReadOnlyList<TrackCollaboratorAssignment>>.Failure(CatalogErrors.InvalidCollaborator);

                assignments.Add(new TrackCollaboratorAssignment(ArtistId.From(artistGuid), null));
                continue;
            }

            var displayName = entry.DisplayName?.Trim();
            if (string.IsNullOrEmpty(displayName)
                || displayName.Length > TrackCollaborator.MaxDisplayNameLength)
            {
                return Result<IReadOnlyList<TrackCollaboratorAssignment>>.Failure(CatalogErrors.InvalidCollaborator);
            }

            assignments.Add(new TrackCollaboratorAssignment(null, displayName));
        }

        var typedArtistIds = assignments
            .Where(a => a.ArtistId is not null)
            .Select(a => a.ArtistId!.Value)
            .Distinct()
            .ToArray();

        if (typedArtistIds.Length == 0)
            return Result<IReadOnlyList<TrackCollaboratorAssignment>>.Success(assignments);

        var count = await db.Artists
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(a => typedArtistIds.Contains(a.Id), cancellationToken);

        if (count != typedArtistIds.Length)
            return Result<IReadOnlyList<TrackCollaboratorAssignment>>.Failure(CatalogErrors.ArtistNotFound);

        return Result<IReadOnlyList<TrackCollaboratorAssignment>>.Success(assignments);
    }

    internal static async Task<IReadOnlyDictionary<TrackId, IReadOnlyList<ManageTrackCollaboratorResponse>>> LoadForTracksAsync(
        CatalogDbContext db,
        IReadOnlyList<TrackId> trackIds,
        CancellationToken cancellationToken)
    {
        if (trackIds.Count == 0)
            return new Dictionary<TrackId, IReadOnlyList<ManageTrackCollaboratorResponse>>();

        var rows = await db.TrackCollaborators
            .AsNoTracking()
            .Where(c => trackIds.Contains(c.TrackId))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        var artistIds = rows
            .Where(c => c.ArtistId is not null)
            .Select(c => c.ArtistId!.Value)
            .Distinct()
            .ToArray();

        var artistNames = artistIds.Length == 0
            ? new Dictionary<ArtistId, string>()
            : await db.Artists
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => artistIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        return rows
            .GroupBy(c => c.TrackId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ManageTrackCollaboratorResponse>)group
                    .Select(collaborator => ToResponse(collaborator, artistNames))
                    .ToArray());
    }

    internal static ManageTrackCollaboratorResponse ToResponse(
        TrackCollaborator collaborator,
        IReadOnlyDictionary<ArtistId, string> artistNames)
    {
        if (collaborator.ArtistId is { } artistId
            && artistNames.TryGetValue(artistId, out var artistName))
        {
            return new ManageTrackCollaboratorResponse(
                artistId.Value,
                artistName,
                IsPlaceholder: false,
                collaborator.Role,
                collaborator.DisplayOrder);
        }

        return new ManageTrackCollaboratorResponse(
            null,
            collaborator.DisplayName ?? string.Empty,
            IsPlaceholder: true,
            collaborator.Role,
            collaborator.DisplayOrder);
    }
}
