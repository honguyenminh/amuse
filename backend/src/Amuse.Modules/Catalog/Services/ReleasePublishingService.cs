using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Services;

public sealed class ReleasePublishingService(
    CatalogDbContext db,
    TenancyDbContext tenancyDb,
    IClock clock)
{
    public async Task<Result<Release>> PublishForOrganizationAsync(
        ReleaseId releaseId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var scopeResult = await LoadReleaseForOrganizationAsync(releaseId, organizationId, cancellationToken);
        if (!scopeResult.IsSuccess)
            return Result<Release>.Failure(scopeResult.Error!);

        return await PublishLoadedReleaseAsync(scopeResult.Value!, cancellationToken);
    }

    public async Task<Result<Release>> PublishSystemAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken)
    {
        var release = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == releaseId, cancellationToken);

        if (release is null)
            return Result<Release>.Failure(CatalogErrors.ReleaseNotFound);

        return await PublishLoadedReleaseAsync(release, cancellationToken);
    }

    private async Task<Result<Release>> PublishLoadedReleaseAsync(
        Release release,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var publishResult = release.Publish(now);
        if (!publishResult.IsSuccess)
            return Result<Release>.Failure(publishResult.Error!);

        await SyncArtistVisibilityAsync(release, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<Release>.Success(release);
    }

    internal async Task SyncArtistVisibilityAsync(
        Release release,
        CancellationToken cancellationToken)
    {
        var organization = await tenancyDb.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == release.OrganizationId, cancellationToken);

        if (organization is null)
            return;

        var visibilityTier = ArtistVisibilityTierMapper.FromOrganizationTrustTier(organization.TrustTier);
        var artist = await db.Artists
            .FirstOrDefaultAsync(a => a.Id == release.ArtistId, cancellationToken);

        if (artist is not null)
            artist.SetVisibilityTier(visibilityTier);
    }

    public async Task<Result<Release>> LoadReleaseForOrganizationAsync(
        ReleaseId releaseId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var release = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == releaseId, cancellationToken);

        if (release is null)
            return Result<Release>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(organizationId, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<Release>.Failure(scopeResult.Error!);

        return Result<Release>.Success(release);
    }
}
