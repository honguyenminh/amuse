using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.BrowseHome;

internal sealed class BrowseHomeHandler(CatalogDbContext db, IObjectStorage storage)
{
    private const int RecentAlbumCount = 8;
    private const int FeaturedArtistCount = 6;

    public async Task<Result<BrowseHomeResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var recentAlbumRows = await db.Albums
            .AsNoTracking()
            .OrderByDescending(a => a.ReleaseDate)
            .Take(RecentAlbumCount)
            .Join(
                db.Artists.AsNoTracking(),
                album => album.ArtistId,
                artist => artist.Id,
                (album, artist) => new
                {
                    AlbumId = album.Id.Value,
                    AlbumSlug = album.Slug.Value,
                    album.Title,
                    ArtistId = artist.Id.Value,
                    ArtistName = artist.Name,
                    ArtistSlug = artist.Slug.Value,
                    album.ReleaseType,
                    album.ReleaseDate,
                    album.CoverArtKey,
                })
            .ToListAsync(cancellationToken);

        var recentAlbums = recentAlbumRows
            .Select(row => new AlbumSummary(
                row.AlbumId,
                row.AlbumSlug,
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
            recentAlbums,
            featuredArtists));
    }

    internal static string? CoverArtUrlFor(IObjectStorage storage, string? key) =>
        string.IsNullOrEmpty(key) ? null : storage.GetPublicUrl(MediaBucket.Covers, key);
}

public sealed record BrowseHomeResponse(
    IReadOnlyList<AlbumSummary> RecentAlbums,
    IReadOnlyList<ArtistSummary> FeaturedArtists);
