using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Services;

internal sealed class CatalogDiscoveryReadModel(
    CatalogDbContext db,
    ITenancyOrganizationReadModel organizationReadModel) : ICatalogDiscoveryReadModel
{
    private const int MaxSearchLimit = 50;

    public async Task<bool> TrackExistsAndPlayableAsync(TrackId trackId, CancellationToken cancellationToken) =>
        await db.Tracks.AsNoTracking().AnyAsync(
            t => t.Id == trackId
                 && t.LifecycleStatus == TrackLifecycleStatus.Published
                 && t.AudioMasterKey != null
                 && t.AudioMasterKey != "",
            cancellationToken);

    public async Task<bool> ReleaseExistsAndPublishedAsync(ReleaseId releaseId, CancellationToken cancellationToken) =>
        await db.Releases.AsNoTracking().AnyAsync(
            r => r.Id == releaseId && r.LifecycleStatus == ReleaseLifecycleStatus.Published,
            cancellationToken);

    public async Task<IReadOnlyList<TrackId>> GetPublishedTrackIdsForReleaseOrderedAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken) =>
        await db.Tracks.AsNoTracking()
            .Where(t =>
                t.ReleaseId == releaseId
                && t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.AudioMasterKey != null
                && t.AudioMasterKey != "")
            .OrderBy(t => t.TrackNumber)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

    public async Task<CatalogSearchResult> SearchAsync(
        string query,
        int candidateLimit,
        IReadOnlySet<string>? kinds,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(candidateLimit, 1, MaxSearchLimit);
        var pattern = $"%{query.Trim()}%";
        var includeArtist = kinds is null || kinds.Contains("artist");
        var includeRelease = kinds is null || kinds.Contains("release");
        var includeTrack = kinds is null || kinds.Contains("track");

        var artistRows = includeArtist
            ? await SearchArtistsAsync(pattern, take, cancellationToken)
            : [];

        var releaseRows = includeRelease
            ? await SearchReleasesAsync(pattern, take, cancellationToken)
            : [];

        var trackRows = includeTrack
            ? await SearchTracksAsync(pattern, take, cancellationToken)
            : [];

        var organizationIds = artistRows
            .Where(row => row.ManagingOrganizationId is not null)
            .Select(row => row.ManagingOrganizationId!.Value)
            .Concat(releaseRows.Select(row => row.OrganizationId))
            .Concat(trackRows.Select(row => row.OrganizationId))
            .Distinct()
            .ToArray();

        var trustTiers = await organizationReadModel.GetTrustTiersAsync(organizationIds, cancellationToken);

        var items = new List<CatalogSearchItem>();

        foreach (var row in artistRows)
        {
            var trustTier = CatalogOrganizationTrustResolver.ResolveTrustTier(
                row.ManagingOrganizationId,
                trustTiers);
            items.Add(new CatalogSearchItem(
                CatalogSearchItemKind.Artist,
                row.Id.Value,
                row.Name,
                null,
                row.Slug.Value,
                null,
                row.Id.Value,
                null,
                null,
                trustTier,
                CatalogOrganizationTrustResolver.IsPlatformVerified(trustTier)));
        }

        foreach (var row in releaseRows)
        {
            var trustTier = CatalogOrganizationTrustResolver.ResolveTrustTier(
                row.OrganizationId,
                trustTiers);
            items.Add(new CatalogSearchItem(
                CatalogSearchItemKind.Release,
                row.ReleaseId.Value,
                row.Title,
                row.ArtistName,
                row.ArtistSlug.Value,
                row.Slug.Value,
                row.ArtistId.Value,
                row.ReleaseId.Value,
                row.CoverArtKey,
                trustTier,
                CatalogOrganizationTrustResolver.IsPlatformVerified(trustTier)));
        }

        foreach (var row in trackRows)
        {
            var trustTier = CatalogOrganizationTrustResolver.ResolveTrustTier(
                row.OrganizationId,
                trustTiers);
            items.Add(new CatalogSearchItem(
                CatalogSearchItemKind.Track,
                row.TrackId.Value,
                row.TrackTitle,
                $"{row.ArtistName} — {row.ReleaseTitle}",
                row.ArtistSlug.Value,
                row.ReleaseSlug.Value,
                row.ArtistId.Value,
                row.ReleaseId.Value,
                row.ReleaseCover,
                trustTier,
                CatalogOrganizationTrustResolver.IsPlatformVerified(trustTier)));
        }

        return new CatalogSearchResult(items);
    }

    public async Task<IReadOnlyList<CatalogTrackPlayableRow>> GetPlayableTracksForReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken)
    {
        var release = await db.Releases.AsNoTracking()
            .Where(r => r.Id == releaseId && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .Select(r => new { r.Id, r.Title, r.Slug, r.ArtistId })
            .FirstOrDefaultAsync(cancellationToken);

        if (release is null)
            return [];

        var artist = await db.Artists.AsNoTracking()
            .Where(a => a.Id == release.ArtistId)
            .Select(a => new { a.Name, a.Slug })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return [];

        return await db.Tracks.AsNoTracking()
            .Where(t =>
                t.ReleaseId == releaseId
                && t.LifecycleStatus == TrackLifecycleStatus.Published)
            .OrderBy(t => t.TrackNumber)
            .Select(t => new CatalogTrackPlayableRow(
                t.Id.Value,
                t.Title,
                t.TrackNumber,
                t.Duration.Milliseconds,
                t.AudioMasterKey != null && t.AudioMasterKey != "",
                release.Id.Value,
                release.Title,
                artist.Name,
                artist.Slug.Value,
                release.Slug.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CatalogTrackPlayableRow>> GetPlayableTrackRowsAsync(
        IEnumerable<Guid> trackIds,
        CancellationToken cancellationToken)
    {
        var ids = trackIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, CatalogTrackPlayableRow>();

        var typedIds = ids.Select(TrackId.From).ToArray();

        var rows = await db.Tracks.AsNoTracking()
            .Where(t => typedIds.Contains(t.Id) && t.LifecycleStatus == TrackLifecycleStatus.Published)
            .Join(
                db.Releases.AsNoTracking().Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published),
                track => track.ReleaseId,
                release => release.Id,
                (track, release) => new { track, release })
            .Join(
                db.Artists.AsNoTracking(),
                x => x.release.ArtistId,
                artist => artist.Id,
                (x, artist) => new CatalogTrackPlayableRow(
                    x.track.Id.Value,
                    x.track.Title,
                    x.track.TrackNumber,
                    x.track.Duration.Milliseconds,
                    x.track.AudioMasterKey != null && x.track.AudioMasterKey != "",
                    x.release.Id.Value,
                    x.release.Title,
                    artist.Name,
                    artist.Slug.Value,
                    x.release.Slug.Value))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.TrackId);
    }

    public async Task<IReadOnlyDictionary<Guid, CatalogReleaseSummaryRow>> GetReleaseSummariesAsync(
        IEnumerable<Guid> releaseIds,
        CancellationToken cancellationToken)
    {
        var ids = releaseIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, CatalogReleaseSummaryRow>();

        var typedIds = ids.Select(ReleaseId.From).ToArray();

        var rows = await db.Releases.AsNoTracking()
            .Where(r => typedIds.Contains(r.Id) && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .Join(
                db.Artists.AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new CatalogReleaseSummaryRow(
                    release.Id.Value,
                    release.Title,
                    artist.Name,
                    artist.Slug.Value,
                    release.Slug.Value,
                    release.CoverArtKey))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ReleaseId);
    }

    public async Task<CatalogTrackDownloadRow?> GetTrackDownloadRowAsync(
        TrackId trackId,
        CancellationToken cancellationToken) =>
        await db.Tracks.AsNoTracking()
            .Where(t =>
                t.Id == trackId
                && t.AudioMasterKey != null
                && t.AudioMasterKey != "")
            .Select(t => new CatalogTrackDownloadRow(
                t.Id.Value,
                t.ReleaseId.Value,
                t.AudioMasterKey!,
                t.Title))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<ArtistSearchRow>> SearchArtistsAsync(
        string pattern,
        int take,
        CancellationToken cancellationToken)
    {
        var publishedReleases = db.Releases.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published);

        var artistsByName = await db.Artists.IgnoreQueryFilters().AsNoTracking()
            .Where(a => EF.Functions.ILike(a.Name, pattern))
            .Where(a => publishedReleases.Any(r => r.ArtistId == a.Id))
            .OrderBy(a => a.Name)
            .Take(take)
            .Select(a => new ArtistSearchRow(
                a.Id,
                a.Name,
                a.Slug,
                a.ManagingOrganizationId))
            .ToListAsync(cancellationToken);

        var artistIdsBySlug = await db.Database.SqlQuery<Guid>($"""
            SELECT DISTINCT a.id
            FROM catalog.artist AS a
            INNER JOIN catalog.release AS r ON r.artist_id = a.id
            WHERE r.lifecycle_status = 'published'::catalog.release_lifecycle_status
              AND a.slug ILIKE {pattern}
            LIMIT {take}
            """).ToListAsync(cancellationToken);

        var artistsBySlug = await LoadArtistSearchRowsAsync(artistIdsBySlug, cancellationToken);

        return artistsByName
            .Concat(artistsBySlug)
            .DistinctBy(row => row.Id)
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    private async Task<IReadOnlyList<ReleaseSearchRow>> SearchReleasesAsync(
        string pattern,
        int take,
        CancellationToken cancellationToken)
    {
        var releasesByTitle = await db.Releases.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .Where(r => EF.Functions.ILike(r.Title, pattern))
            .OrderBy(r => r.Title)
            .Take(take)
            .Join(
                db.Artists.IgnoreQueryFilters().AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new ReleaseSearchRow(
                    release.Id,
                    release.Title,
                    release.Slug,
                    release.CoverArtKey,
                    release.OrganizationId,
                    artist.Id,
                    artist.Name,
                    artist.Slug))
            .ToListAsync(cancellationToken);

        var releaseIdsBySlug = await db.Database.SqlQuery<Guid>($"""
            SELECT r.id
            FROM catalog.release AS r
            WHERE r.lifecycle_status = 'published'::catalog.release_lifecycle_status
              AND r.slug ILIKE {pattern}
            LIMIT {take}
            """).ToListAsync(cancellationToken);

        var releasesBySlug = await LoadReleaseSearchRowsAsync(releaseIdsBySlug, cancellationToken);

        return releasesByTitle
            .Concat(releasesBySlug)
            .DistinctBy(row => row.ReleaseId)
            .OrderBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    private async Task<IReadOnlyList<TrackSearchRow>> SearchTracksAsync(
        string pattern,
        int take,
        CancellationToken cancellationToken)
    {
        var trackIdsByTitle = await db.Tracks.IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.LifecycleStatus == TrackLifecycleStatus.Published)
            .Where(t => EF.Functions.ILike(t.Title, pattern))
            .OrderBy(t => t.Title)
            .Take(take)
            .Select(t => t.Id.Value)
            .ToListAsync(cancellationToken);

        var trackIdsBySlug = await db.Database.SqlQuery<Guid>($"""
            SELECT DISTINCT t.id
            FROM catalog.track AS t
            INNER JOIN catalog.release AS r ON r.id = t.release_id
            INNER JOIN catalog.artist AS a ON a.id = r.artist_id
            WHERE t.lifecycle_status = 'published'::catalog.track_lifecycle_status
              AND r.lifecycle_status = 'published'::catalog.release_lifecycle_status
              AND (r.slug ILIKE {pattern} OR a.slug ILIKE {pattern})
            LIMIT {take}
            """).ToListAsync(cancellationToken);

        var matchingTrackIds = trackIdsByTitle
            .Concat(trackIdsBySlug)
            .Distinct()
            .Take(take)
            .Select(TrackId.From)
            .ToArray();

        if (matchingTrackIds.Length == 0)
            return [];

        return await db.Tracks.IgnoreQueryFilters().AsNoTracking()
            .Where(t => matchingTrackIds.Contains(t.Id))
            .OrderBy(t => t.Title)
            .Take(take)
            .Join(
                db.Releases.IgnoreQueryFilters().AsNoTracking()
                    .Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published),
                track => track.ReleaseId,
                release => release.Id,
                (track, release) => new { track, release })
            .Join(
                db.Artists.IgnoreQueryFilters().AsNoTracking(),
                x => x.release.ArtistId,
                artist => artist.Id,
                (x, artist) => new TrackSearchRow(
                    x.track.Id,
                    x.track.Title,
                    x.release.Id,
                    x.release.Title,
                    x.release.Slug,
                    x.release.CoverArtKey,
                    x.release.OrganizationId,
                    artist.Id,
                    artist.Name,
                    artist.Slug))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ArtistSearchRow>> LoadArtistSearchRowsAsync(
        IReadOnlyList<Guid> artistIds,
        CancellationToken cancellationToken)
    {
        if (artistIds.Count == 0)
            return [];

        var typedIds = artistIds.Select(ArtistId.From).ToArray();

        return await db.Artists.IgnoreQueryFilters().AsNoTracking()
            .Where(a => typedIds.Contains(a.Id))
            .Select(a => new ArtistSearchRow(
                a.Id,
                a.Name,
                a.Slug,
                a.ManagingOrganizationId))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ReleaseSearchRow>> LoadReleaseSearchRowsAsync(
        IReadOnlyList<Guid> releaseIds,
        CancellationToken cancellationToken)
    {
        if (releaseIds.Count == 0)
            return [];

        var typedIds = releaseIds.Select(ReleaseId.From).ToArray();

        return await db.Releases.IgnoreQueryFilters().AsNoTracking()
            .Where(r => typedIds.Contains(r.Id))
            .Join(
                db.Artists.IgnoreQueryFilters().AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new ReleaseSearchRow(
                    release.Id,
                    release.Title,
                    release.Slug,
                    release.CoverArtKey,
                    release.OrganizationId,
                    artist.Id,
                    artist.Name,
                    artist.Slug))
            .ToListAsync(cancellationToken);
    }

    private sealed record ArtistSearchRow(
        ArtistId Id,
        string Name,
        Slug Slug,
        OrganizationId? ManagingOrganizationId);

    private sealed record ReleaseSearchRow(
        ReleaseId ReleaseId,
        string Title,
        Slug Slug,
        string? CoverArtKey,
        OrganizationId OrganizationId,
        ArtistId ArtistId,
        string ArtistName,
        Slug ArtistSlug);

    private sealed record TrackSearchRow(
        TrackId TrackId,
        string TrackTitle,
        ReleaseId ReleaseId,
        string ReleaseTitle,
        Slug ReleaseSlug,
        string? ReleaseCover,
        OrganizationId OrganizationId,
        ArtistId ArtistId,
        string ArtistName,
        Slug ArtistSlug);
}
