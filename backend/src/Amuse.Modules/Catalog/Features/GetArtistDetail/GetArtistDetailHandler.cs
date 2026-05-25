using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetArtistDetail;

internal sealed class GetArtistDetailHandler(CatalogDbContext db)
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
                a.AvatarUrl,
                a.CoverUrl,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var albums = await db.Albums
            .AsNoTracking()
            .Where(a => a.ArtistId == typedId)
            .OrderByDescending(a => a.ReleaseDate)
            .Select(a => new AlbumSummary(
                a.Id.Value,
                a.Slug.Value,
                a.Title,
                a.ArtistId.Value,
                artist.Name,
                artist.Slug.Value,
                a.ReleaseType,
                a.ReleaseDate,
                a.CoverArtUrl))
            .ToListAsync(cancellationToken);

        var response = new GetArtistDetailResponse(
            artist.Id.Value,
            artist.Slug.Value,
            artist.Name,
            artist.Bio,
            artist.AvatarUrl,
            artist.CoverUrl,
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
