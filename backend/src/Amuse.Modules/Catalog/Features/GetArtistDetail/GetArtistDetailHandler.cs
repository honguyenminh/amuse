using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetArtistDetail;

internal sealed class GetArtistDetailHandler(CatalogDbContext db, IObjectStorage storage)
{
    public async Task<Result<GetArtistDetailResponse>> HandleAsync(
        Guid artistId,
        CancellationToken cancellationToken)
    {
        if (artistId == Guid.Empty)
            return Result<GetArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var typedId = ArtistId.From(artistId);

        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == typedId)
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.Name,
                a.Bio,
                a.AvatarKey,
                a.CoverKey,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var albumRows = await db.Albums
            .AsNoTracking()
            .Where(a => a.ArtistId == typedId)
            .OrderByDescending(a => a.ReleaseDate)
            .Select(a => new
            {
                AlbumId = a.Id.Value,
                AlbumSlug = a.Slug.Value,
                a.Title,
                ArtistId = a.ArtistId.Value,
                a.ReleaseType,
                a.ReleaseDate,
                a.CoverArtKey,
            })
            .ToListAsync(cancellationToken);

        var albums = albumRows
            .Select(a => new AlbumSummary(
                a.AlbumId,
                a.AlbumSlug,
                a.Title,
                a.ArtistId,
                artist.Name,
                artist.Slug.Value,
                a.ReleaseType,
                a.ReleaseDate,
                BrowseHomeHandler.CoverArtUrlFor(storage, a.CoverArtKey)))
            .ToArray();

        var response = new GetArtistDetailResponse(
            artist.Id.Value,
            artist.Slug.Value,
            artist.Name,
            artist.Bio,
            BrowseHomeHandler.CoverArtUrlFor(storage, artist.AvatarKey),
            BrowseHomeHandler.CoverArtUrlFor(storage, artist.CoverKey),
            albums);

        return Result<GetArtistDetailResponse>.Success(response);
    }
}

public sealed record GetArtistDetailResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Bio,
    string? AvatarUrl,
    string? CoverUrl,
    IReadOnlyList<AlbumSummary> Albums);
