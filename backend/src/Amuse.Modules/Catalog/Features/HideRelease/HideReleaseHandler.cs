using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.ManageReleases;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.HideRelease;

internal sealed class HideReleaseHandler(
    CatalogDbContext db,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock)
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
        var release = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == typedId, cancellationToken);

        if (release is null)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(scopeResult.Error!);

        var hideResult = release.Hide(clock.UtcNow);
        if (!hideResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(hideResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

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
}
