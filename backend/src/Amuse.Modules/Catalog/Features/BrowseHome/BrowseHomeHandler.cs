using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.BrowseHome;

internal sealed class BrowseHomeHandler(CatalogDbContext db)
{
    private const int RecentAlbumCount = 8;
    private const int FeaturedArtistCount = 6;

    public async Task<Result<BrowseHomeResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var recentAlbums = await db.Albums
            .AsNoTracking()
            .OrderByDescending(a => a.ReleaseDate)
            .Take(RecentAlbumCount)
            .Join(
                db.Artists.AsNoTracking(),
                album => album.ArtistId,
                artist => artist.Id,
                (album, artist) => new AlbumSummary(
                    album.Id.Value,
                    album.Slug.Value,
                    album.Title,
                    artist.Id.Value,
                    artist.Name,
                    artist.Slug.Value,
                    album.ReleaseType,
                    album.ReleaseDate,
                    album.CoverArtUrl))
            .ToListAsync(cancellationToken);

        var featuredArtists = await db.Artists
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Take(FeaturedArtistCount)
            .Select(a => new ArtistSummary(
                a.Id.Value,
                a.Slug.Value,
                a.Name,
                a.AvatarUrl,
                a.CoverUrl))
            .ToListAsync(cancellationToken);

        return Result<BrowseHomeResponse>.Success(new BrowseHomeResponse(
            recentAlbums,
            featuredArtists));
    }
}

public sealed record BrowseHomeResponse(
    IReadOnlyList<AlbumSummary> RecentAlbums,
    IReadOnlyList<ArtistSummary> FeaturedArtists);
