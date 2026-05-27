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

        var releaseRows = await db.Releases
            .AsNoTracking()
            .Where(r => r.ArtistId == typedId)
            .OrderByDescending(r => r.ReleaseDate)
            .Select(r => new
            {
                ReleaseId = r.Id.Value,
                ReleaseSlug = r.Slug.Value,
                r.Title,
                ArtistId = r.ArtistId.Value,
                r.ReleaseType,
                r.ReleaseDate,
                r.CoverArtKey,
            })
            .ToListAsync(cancellationToken);

        var releases = releaseRows
            .Select(r => new ReleaseSummary(
                r.ReleaseId,
                r.ReleaseSlug,
                r.Title,
                r.ArtistId,
                artist.Name,
                artist.Slug.Value,
                r.ReleaseType,
                r.ReleaseDate,
                BrowseHomeHandler.CoverArtUrlFor(storage, r.CoverArtKey)))
            .ToArray();

        var response = new GetArtistDetailResponse(
            artist.Id.Value,
            artist.Slug.Value,
            artist.Name,
            artist.Bio,
            BrowseHomeHandler.CoverArtUrlFor(storage, artist.AvatarKey),
            BrowseHomeHandler.CoverArtUrlFor(storage, artist.CoverKey),
            releases);

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
    IReadOnlyList<ReleaseSummary> Releases);
