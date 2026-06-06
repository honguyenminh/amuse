using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.GetReleaseGroupDetail;
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
            .Where(r => r.Id == typedId && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .FirstOrDefaultAsync(cancellationToken);

        if (release is null)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        return await BuildResponseAsync(release, cancellationToken);
    }

    public async Task<Result<GetReleaseDetailResponse>> HandleBySlugsAsync(
        string artistSlug,
        string releaseSlug,
        CancellationToken cancellationToken)
    {
        var artistParse = CatalogSlugHelper.TryParseArtistSlug(artistSlug);
        var releaseParse = CatalogSlugHelper.TryParseReleaseSlug(releaseSlug);
        if (!artistParse.IsSuccess || !releaseParse.IsSuccess)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var artistId = await db.Artists
            .AsNoTracking()
            .Where(a => a.Slug == artistParse.Value!)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (artistId == default)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var release = await db.Releases
            .AsNoTracking()
            .Include(r => r.Tracks)
            .Where(r =>
                r.ArtistId == artistId
                && r.Slug == releaseParse.Value!
                && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .FirstOrDefaultAsync(cancellationToken);

        if (release is null)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        return await BuildResponseAsync(release, cancellationToken);
    }

    private async Task<Result<GetReleaseDetailResponse>> BuildResponseAsync(
        Release release,
        CancellationToken cancellationToken)
    {
        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == release.ArtistId)
            .Select(a => new { a.Id, a.Slug, a.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var tracks = release.Tracks
            .Where(t => t.LifecycleStatus == TrackLifecycleStatus.Published)
            .OrderBy(t => t.TrackNumber)
            .Select(t => new TrackResponse(
                t.Id.Value,
                t.Title,
                t.TrackNumber,
                t.Duration.Milliseconds,
                HasAudio: !string.IsNullOrEmpty(t.AudioMasterKey)))
            .ToArray();

        string? releaseGroupTitle = null;
        string? releaseGroupSlug = null;
        IReadOnlyList<ReleaseEditionSummary> otherEditions = [];

        if (release.ReleaseGroupId is { } groupId)
        {
            var group = await db.ReleaseGroups
                .AsNoTracking()
                .Where(g => g.Id == groupId)
                .Select(g => new { g.Title, Slug = g.Slug.Value })
                .FirstOrDefaultAsync(cancellationToken);

            if (group is not null)
            {
                releaseGroupTitle = group.Title;
                releaseGroupSlug = group.Slug;
                otherEditions = await db.Releases
                    .AsNoTracking()
                    .Where(r =>
                        r.ReleaseGroupId == groupId
                        && r.Id != release.Id
                        && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
                    .OrderByDescending(r => r.ReleaseDate)
                    .Select(r => new ReleaseEditionSummary(
                        r.Id.Value,
                        r.Slug.Value,
                        r.Title,
                        r.ReleaseType,
                        r.ReleaseDate,
                        BrowseHomeHandler.CoverArtUrlFor(storage, r.CoverArtKey)))
                    .ToListAsync(cancellationToken);
            }
        }

        var response = new GetReleaseDetailResponse(
            release.Id.Value,
            release.Slug.Value,
            release.Title,
            artist.Id.Value,
            artist.Name,
            artist.Slug.Value,
            release.ReleaseType,
            release.ReleaseDate,
            release.ReleaseGroupId?.Value,
            releaseGroupTitle,
            releaseGroupSlug,
            release.Description,
            release.Upc,
            release.PrimaryGenre,
            release.Tags,
            release.LanguageCode,
            release.LabelName,
            BrowseHomeHandler.CoverArtUrlFor(storage, release.CoverArtKey),
            tracks,
            otherEditions);

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
    Guid? ReleaseGroupId,
    string? ReleaseGroupTitle,
    string? ReleaseGroupSlug,
    string? Description,
    string? Upc,
    string? PrimaryGenre,
    string? Tags,
    string? LanguageCode,
    string? LabelName,
    string? CoverArtUrl,
    IReadOnlyList<TrackResponse> Tracks,
    IReadOnlyList<ReleaseEditionSummary> OtherEditions);
