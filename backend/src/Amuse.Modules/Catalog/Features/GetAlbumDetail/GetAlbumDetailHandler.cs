using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetAlbumDetail;

internal sealed class GetAlbumDetailHandler(CatalogDbContext db, IObjectStorage storage)
{
    public async Task<Result<GetAlbumDetailResponse>> HandleAsync(
        Guid albumId,
        CancellationToken cancellationToken)
    {
        if (albumId == Guid.Empty)
            return Result<GetAlbumDetailResponse>.Failure(CatalogErrors.AlbumNotFound);

        var typedId = AlbumId.From(albumId);

        var album = await db.Albums
            .AsNoTracking()
            .Include(a => a.Tracks)
            .Where(a => a.Id == typedId)
            .FirstOrDefaultAsync(cancellationToken);

        if (album is null)
            return Result<GetAlbumDetailResponse>.Failure(CatalogErrors.AlbumNotFound);

        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == album.ArtistId)
            .Select(a => new { a.Id, a.Slug, a.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetAlbumDetailResponse>.Failure(CatalogErrors.AlbumNotFound);

        var tracks = album.Tracks
            .OrderBy(t => t.TrackNumber)
            .Select(t => new TrackResponse(
                t.Id.Value,
                t.Title,
                t.TrackNumber,
                t.Duration.Milliseconds,
                HasAudio: !string.IsNullOrEmpty(t.AudioMasterKey)))
            .ToArray();

        var response = new GetAlbumDetailResponse(
            album.Id.Value,
            album.Slug.Value,
            album.Title,
            artist.Id.Value,
            artist.Name,
            artist.Slug.Value,
            album.ReleaseType,
            album.ReleaseDate,
            BrowseHomeHandler.CoverArtUrlFor(storage, album.CoverArtKey),
            tracks);

        return Result<GetAlbumDetailResponse>.Success(response);
    }
}

public sealed record GetAlbumDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    Guid ArtistId,
    string ArtistName,
    string ArtistSlug,
    ReleaseType ReleaseType,
    DateTimeOffset ReleaseDate,
    string? CoverArtUrl,
    IReadOnlyList<TrackResponse> Tracks);
