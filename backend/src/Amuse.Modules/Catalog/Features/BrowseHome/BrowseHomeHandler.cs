using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.BrowseHome;

internal sealed class BrowseHomeHandler(
    CatalogDbContext db,
    IMediaPublicUrlBuilder mediaUrls,
    ITenancyOrganizationReadModel organizationReadModel)
{
    private const int RecentReleaseCount = 8;
    private const int FeaturedArtistCount = 6;

    public async Task<Result<BrowseHomeResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var recentReleaseRows = await db.Releases
            .AsNoTracking()
            .Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .Join(
                db.Artists.AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new
                {
                    ReleaseId = release.Id.Value,
                    ReleaseSlug = release.Slug.Value,
                    release.Title,
                    ArtistId = artist.Id.Value,
                    ArtistName = artist.Name,
                    ArtistSlug = artist.Slug.Value,
                    release.ReleaseType,
                    release.ReleaseDate,
                    release.CoverArtKey,
                    release.OrganizationId,
                })
            .ToListAsync(cancellationToken);

        var releaseOrgIds = recentReleaseRows
            .Select(row => row.OrganizationId)
            .Distinct()
            .ToArray();
        var releaseTrustTiers = await organizationReadModel.GetTrustTiersAsync(releaseOrgIds, cancellationToken);

        var recentReleases = recentReleaseRows
            .Select(row =>
            {
                var trustTier = CatalogOrganizationTrustResolver.ResolveTrustTier(
                    row.OrganizationId,
                    releaseTrustTiers);
                return new
                {
                    Row = row,
                    TrustTier = trustTier,
                    IsVerified = CatalogOrganizationTrustResolver.IsPlatformVerified(trustTier),
                };
            })
            .OrderByDescending(entry => entry.IsVerified)
            .ThenByDescending(entry => entry.Row.ReleaseDate)
            .Take(RecentReleaseCount)
            .Select(entry => new ReleaseSummary(
                entry.Row.ReleaseId,
                entry.Row.ReleaseSlug,
                entry.Row.Title,
                entry.Row.ArtistId,
                entry.Row.ArtistName,
                entry.Row.ArtistSlug,
                entry.Row.ReleaseType,
                entry.Row.ReleaseDate,
                mediaUrls.BuildCoverArtUrl(entry.Row.CoverArtKey),
                entry.TrustTier))
            .ToArray();

        var featuredArtistRows = await db.Artists
            .AsNoTracking()
            .Where(a => db.Releases.Any(r =>
                r.ArtistId == a.Id && r.LifecycleStatus == ReleaseLifecycleStatus.Published))
            .OrderBy(a => a.Name)
            .Take(FeaturedArtistCount)
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.Name,
                a.AvatarKey,
                a.CoverKey,
                a.ManagingOrganizationId,
            })
            .ToListAsync(cancellationToken);

        var artistOrgIds = featuredArtistRows
            .Where(row => row.ManagingOrganizationId is not null)
            .Select(row => row.ManagingOrganizationId!.Value)
            .Distinct()
            .ToArray();
        var artistTrustTiers = await organizationReadModel.GetTrustTiersAsync(artistOrgIds, cancellationToken);

        var featuredArtists = featuredArtistRows
            .Select(row =>
            {
                var trustTier = CatalogOrganizationTrustResolver.ResolveTrustTier(
                    row.ManagingOrganizationId,
                    artistTrustTiers);
                return new ArtistSummary(
                    row.Id.Value,
                    row.Slug.Value,
                    row.Name,
                    mediaUrls.BuildCoverArtUrl(row.AvatarKey),
                    mediaUrls.BuildCoverArtUrl(row.CoverKey),
                    trustTier);
            })
            .ToArray();

        return Result<BrowseHomeResponse>.Success(new BrowseHomeResponse(
            recentReleases,
            featuredArtists));
    }
}

public sealed record BrowseHomeResponse(
    IReadOnlyList<ReleaseSummary> RecentReleases,
    IReadOnlyList<ArtistSummary> FeaturedArtists);
