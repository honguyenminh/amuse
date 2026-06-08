using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using CatalogSlugHelper = Amuse.Modules.Catalog.Features.Common.CatalogSlugHelper;

namespace Amuse.Modules.Catalog.Features.GetArtistDetail;

internal sealed class GetArtistDetailHandler(
    CatalogDbContext db,
    IMediaPublicUrlBuilder mediaUrls,
    ITenancyOrganizationReadModel organizationReadModel)
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
                a.ManagingOrganizationId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        return await BuildResponseAsync(
            artist.Id,
            artist.Slug,
            artist.Name,
            artist.Bio,
            artist.AvatarKey,
            artist.CoverKey,
            artist.ManagingOrganizationId,
            cancellationToken);
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
                a.ManagingOrganizationId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        return await BuildResponseAsync(
            artist.Id,
            artist.Slug,
            artist.Name,
            artist.Bio,
            artist.AvatarKey,
            artist.CoverKey,
            artist.ManagingOrganizationId,
            cancellationToken);
    }

    private async Task<Result<GetArtistDetailResponse>> BuildResponseAsync(
        ArtistId typedId,
        Slug artistSlug,
        string artistName,
        string? bio,
        string? avatarKey,
        string? coverKey,
        OrganizationId? managingOrganizationId,
        CancellationToken cancellationToken)
    {
        var trustTier = await CatalogOrganizationTrustResolver.ResolveTrustTierAsync(
            organizationReadModel,
            managingOrganizationId,
            cancellationToken);

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
                r.OrganizationId,
            })
            .ToListAsync(cancellationToken);

        var releaseOrgIds = releaseRows
            .Select(row => row.OrganizationId)
            .Distinct()
            .ToArray();
        var releaseTrustTiers = await organizationReadModel.GetTrustTiersAsync(releaseOrgIds, cancellationToken);

        var releases = releaseRows
            .Select(row =>
            {
                var releaseTrustTier = CatalogOrganizationTrustResolver.ResolveTrustTier(
                    row.OrganizationId,
                    releaseTrustTiers);
                return new ReleaseSummary(
                    row.ReleaseId,
                    row.ReleaseSlug,
                    row.Title,
                    row.ArtistId,
                    artistName,
                    artistSlug.Value,
                    row.ReleaseType,
                    row.ReleaseDate,
                    mediaUrls.BuildCoverArtUrl(row.CoverArtKey),
                    releaseTrustTier);
            })
            .ToArray();

        var response = new GetArtistDetailResponse(
            typedId.Value,
            artistSlug.Value,
            artistName,
            bio,
            mediaUrls.BuildCoverArtUrl(avatarKey),
            mediaUrls.BuildCoverArtUrl(coverKey),
            trustTier,
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
    string TrustTier,
    IReadOnlyList<ReleaseSummary> Releases);
