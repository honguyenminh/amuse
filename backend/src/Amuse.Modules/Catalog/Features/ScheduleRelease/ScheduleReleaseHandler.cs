using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.PublishRelease;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.ScheduleRelease;

internal sealed class ScheduleReleaseHandler(
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

        var scheduleResult = release.Schedule(clock.UtcNow);
        if (!scheduleResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(scheduleResult.Error!);

        await db.SaveChangesAsync(cancellationToken);
        return await PublishReleaseHandler.MapDetailAsync(db, mediaUrls, release, cancellationToken);
    }
}
