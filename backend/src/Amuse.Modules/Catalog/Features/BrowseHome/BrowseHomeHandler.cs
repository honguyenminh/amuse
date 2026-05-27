using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.BrowseHome;

internal sealed class BrowseHomeHandler(CatalogDbContext db, IObjectStorage storage)
{
    private const int RecentReleaseCount = 8;
    private const int FeaturedArtistCount = 6;

    public async Task<Result<BrowseHomeResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var recentReleaseRows = await db.Releases
            .AsNoTracking()
            .OrderByDescending(r => r.ReleaseDate)
            .Take(RecentReleaseCount)
            .Join(
                db.Artists.AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new
                {
                    ReleaseId = release.Id.Value,
                    ReleaseSlug = release.Slug.Value,
                    release.Title,
                    ArtistId = artist.Id.Value,
                    ArtistName = artist.Name,
                    ArtistSlug = artist.Slug.Value,
                    release.ReleaseType,
                    release.ReleaseDate,
                    release.CoverArtKey,
                })
            .ToListAsync(cancellationToken);

        var recentReleases = recentReleaseRows
            .Select(row => new ReleaseSummary(
                row.ReleaseId,
                row.ReleaseSlug,
                row.Title,
                row.ArtistId,
                row.ArtistName,
                row.ArtistSlug,
                row.ReleaseType,
                row.ReleaseDate,
                CoverArtUrlFor(storage, row.CoverArtKey)))
            .ToArray();

        var featuredArtistRows = await db.Artists
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Take(FeaturedArtistCount)
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.Name,
                a.AvatarKey,
                a.CoverKey,
            })
            .ToListAsync(cancellationToken);

        var featuredArtists = featuredArtistRows
            .Select(a => new ArtistSummary(
                a.Id.Value,
                a.Slug.Value,
                a.Name,
                CoverArtUrlFor(storage, a.AvatarKey),
                CoverArtUrlFor(storage, a.CoverKey)))
            .ToArray();

        return Result<BrowseHomeResponse>.Success(new BrowseHomeResponse(
            recentReleases,
            featuredArtists));
    }

    internal static string? CoverArtUrlFor(IObjectStorage storage, string? key) =>
        string.IsNullOrEmpty(key) ? null : storage.GetPublicUrl(MediaBucket.Covers, key);
}

public sealed record BrowseHomeResponse(
    IReadOnlyList<ReleaseSummary> RecentReleases,
    IReadOnlyList<ArtistSummary> FeaturedArtists);
