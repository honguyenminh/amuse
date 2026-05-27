using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetReleaseDetail;

internal sealed class GetReleaseDetailHandler(CatalogDbContext db, IObjectStorage storage)
{
    public async Task<Result<GetReleaseDetailResponse>> HandleAsync(
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        if (releaseId == Guid.Empty)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var typedId = ReleaseId.From(releaseId);

        var release = await db.Releases
            .AsNoTracking()
            .Include(r => r.Tracks)
            .Where(r => r.Id == typedId)
            .FirstOrDefaultAsync(cancellationToken);

        if (release is null)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == release.ArtistId)
            .Select(a => new { a.Id, a.Slug, a.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var tracks = release.Tracks
            .OrderBy(t => t.TrackNumber)
            .Select(t => new TrackResponse(
                t.Id.Value,
                t.Title,
                t.TrackNumber,
                t.Duration.Milliseconds,
                HasAudio: !string.IsNullOrEmpty(t.AudioMasterKey)))
            .ToArray();

        var response = new GetReleaseDetailResponse(
            release.Id.Value,
            release.Slug.Value,
            release.Title,
            artist.Id.Value,
            artist.Name,
            artist.Slug.Value,
            release.ReleaseType,
            release.ReleaseDate,
            BrowseHomeHandler.CoverArtUrlFor(storage, release.CoverArtKey),
            tracks);

        return Result<GetReleaseDetailResponse>.Success(response);
    }
}

public sealed record GetReleaseDetailResponse(
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
