using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.ManageReleases;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.PublishRelease;

public sealed class PublishReleaseHandler(
    CatalogDbContext db,
    ITenancyOrganizationReadModel organizationReadModel,
    IClock clock,
    IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<ManageReleaseDetailResponse>> HandleAsync(
        Guid releaseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var typedId = ReleaseId.From(releaseId);
        var loadResult = await LoadReleaseForOrganizationAsync(
            typedId,
            orgResult.Value!,
            cancellationToken);

        if (!loadResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(loadResult.Error!);

        var publishResult = await PublishLoadedAsync(loadResult.Value!, cancellationToken);
        if (!publishResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(publishResult.Error!);

        return await MapDetailAsync(publishResult.Value!, cancellationToken);
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

        return await PublishLoadedAsync(release, cancellationToken);
    }

    private async Task<Result<Release>> PublishLoadedAsync(
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

    private async Task SyncArtistVisibilityAsync(
        Release release,
        CancellationToken cancellationToken)
    {
        var trustTier = await organizationReadModel.GetTrustTierAsync(
            release.OrganizationId,
            cancellationToken);

        if (trustTier is null)
            return;

        var visibilityTier = ArtistVisibilityTierMapper.FromOrganizationTrustTier(trustTier.Value);
        var artist = await db.Artists
            .FirstOrDefaultAsync(a => a.Id == release.ArtistId, cancellationToken);

        if (artist is not null)
            artist.SetVisibilityTier(visibilityTier);
    }

    private async Task<Result<Release>> LoadReleaseForOrganizationAsync(
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

    internal static async Task<Result<ManageReleaseDetailResponse>> MapDetailAsync(
        CatalogDbContext db,
        IMediaPublicUrlBuilder mediaUrls,
        Release release,
        CancellationToken cancellationToken)
    {
        var artistName = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == release.ArtistId)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var collaborators = await ReleaseCollaboratorSync.LoadAsync(
            db,
            release.Id,
            cancellationToken);

        var groupDisplay = await ReleaseGroupLookup.LoadDisplayAsync(db, release.ReleaseGroupId, cancellationToken);

        return Result<ManageReleaseDetailResponse>.Success(
            ReleaseMapper.ToDetail(
                release,
                artistName,
                mediaUrls.BuildCoverArtUrl(release.CoverArtKey),
                collaborators,
                groupDisplay.Title,
                groupDisplay.Slug));
    }

    private async Task<Result<ManageReleaseDetailResponse>> MapDetailAsync(
        Release release,
        CancellationToken cancellationToken) =>
        await MapDetailAsync(db, mediaUrls, release, cancellationToken);
}
