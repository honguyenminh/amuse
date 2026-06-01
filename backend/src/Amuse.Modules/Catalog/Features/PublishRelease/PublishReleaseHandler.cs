using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.ManageReleases;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Services;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.PublishRelease;

internal sealed class PublishReleaseHandler(
    CatalogDbContext db,
    ReleasePublishingService publishingService,
    IObjectStorage storage)
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
        var publishResult = await publishingService.PublishForOrganizationAsync(
            typedId,
            orgResult.Value!,
            cancellationToken);

        if (!publishResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(publishResult.Error!);

        return await MapDetailAsync(publishResult.Value!, cancellationToken);
    }

    internal static async Task<Result<ManageReleaseDetailResponse>> MapDetailAsync(
        CatalogDbContext db,
        IObjectStorage storage,
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
                BrowseHomeHandler.CoverArtUrlFor(storage, release.CoverArtKey),
                collaborators,
                groupDisplay.Title,
                groupDisplay.Slug));
    }

    private async Task<Result<ManageReleaseDetailResponse>> MapDetailAsync(
        Release release,
        CancellationToken cancellationToken) =>
        await MapDetailAsync(db, storage, release, cancellationToken);
}
