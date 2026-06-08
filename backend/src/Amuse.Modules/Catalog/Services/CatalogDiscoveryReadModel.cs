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
        int limit,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit, 1, MaxSearchLimit);
        var pattern = $"%{query.Trim()}%";

        var artistRows = await db.Artists.AsNoTracking()
            .Where(a => EF.Functions.ILike(a.Name, pattern)
                        && db.Releases.Any(r =>
                            r.ArtistId == a.Id && r.LifecycleStatus == ReleaseLifecycleStatus.Published))
            .OrderBy(a => a.Name)
            .Take(take)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Slug,
                a.ManagingOrganizationId,
            })
            .ToListAsync(cancellationToken);

        var releaseRows = await db.Releases.AsNoTracking()
            .Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published
                        && EF.Functions.ILike(r.Title, pattern))
            .Join(
                db.Artists.AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new
                {
                    ReleaseId = release.Id,
                    release.Title,
                    release.Slug,
                    release.CoverArtKey,
                    release.OrganizationId,
                    ArtistId = artist.Id,
                    ArtistName = artist.Name,
                    ArtistSlug = artist.Slug,
                })
            .OrderBy(r => r.Title)
            .Take(take)
            .ToListAsync(cancellationToken);

        var trackRows = await db.Tracks.AsNoTracking()
            .Where(t => t.LifecycleStatus == TrackLifecycleStatus.Published
                        && EF.Functions.ILike(t.Title, pattern))
            .Join(
                db.Releases.AsNoTracking().Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published),
                track => track.ReleaseId,
                release => release.Id,
                (track, release) => new { track, release })
            .Join(
                db.Artists.AsNoTracking(),
                x => x.release.ArtistId,
                artist => artist.Id,
                (x, artist) => new
                {
                    TrackId = x.track.Id,
                    TrackTitle = x.track.Title,
                    ReleaseId = x.release.Id,
                    ReleaseTitle = x.release.Title,
                    ReleaseSlug = x.release.Slug,
                    ReleaseCover = x.release.CoverArtKey,
                    x.release.OrganizationId,
                    ArtistId = artist.Id,
                    ArtistName = artist.Name,
                    ArtistSlug = artist.Slug,
                })
            .OrderBy(t => t.TrackTitle)
            .Take(take)
            .ToListAsync(cancellationToken);

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

        return CatalogDiscoverySearchRanking.RankAndPartition(items, take);
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

}
