using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;
using CatalogSlugHelper = Amuse.Modules.Catalog.Features.Common.CatalogSlugHelper;

namespace Amuse.Modules.Catalog.Features.GetArtistDetail;

internal sealed class GetArtistDetailHandler(CatalogDbContext db, IMediaPublicUrlBuilder mediaUrls)
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

        return await BuildResponseAsync(artist.Id, artist.Slug, artist.Name, artist.Bio, artist.AvatarKey, artist.CoverKey, cancellationToken);
    }

    public async Task<Result<GetArtistDetailResponse>> HandleBySlugAsync(
        string artistSlug,
        CancellationToken cancellationToken)
    {
        var parseResult = CatalogSlugHelper.TryParseArtistSlug(artistSlug);
        if (!parseResult.IsSuccess)
            return Result<GetArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var typedSlug = parseResult.Value!;

        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Slug == typedSlug)
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

        return await BuildResponseAsync(artist.Id, artist.Slug, artist.Name, artist.Bio, artist.AvatarKey, artist.CoverKey, cancellationToken);
    }

    private async Task<Result<GetArtistDetailResponse>> BuildResponseAsync(
        ArtistId typedId,
        Slug artistSlug,
        string artistName,
        string? bio,
        string? avatarKey,
        string? coverKey,
        CancellationToken cancellationToken)
    {
        var releaseRows = await db.Releases
            .AsNoTracking()
            .Where(r => r.ArtistId == typedId && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
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
                artistName,
                artistSlug.Value,
                r.ReleaseType,
                r.ReleaseDate,
                mediaUrls.BuildCoverArtUrl(r.CoverArtKey)))
            .ToArray();

        var response = new GetArtistDetailResponse(
            typedId.Value,
            artistSlug.Value,
            artistName,
            bio,
            mediaUrls.BuildCoverArtUrl(avatarKey),
            mediaUrls.BuildCoverArtUrl(coverKey),
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
