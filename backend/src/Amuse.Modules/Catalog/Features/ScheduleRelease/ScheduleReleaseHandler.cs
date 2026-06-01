using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.PublishRelease;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Services;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;

namespace Amuse.Modules.Catalog.Features.ScheduleRelease;

internal sealed class ScheduleReleaseHandler(
    CatalogDbContext db,
    ReleasePublishingService publishingService,
    IObjectStorage storage,
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
        var loadResult = await publishingService.LoadReleaseForOrganizationAsync(
            typedId,
            orgResult.Value!,
            cancellationToken);

        if (!loadResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(loadResult.Error!);

        var release = loadResult.Value!;
        var scheduleResult = release.Schedule(clock.UtcNow);
        if (!scheduleResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(scheduleResult.Error!);

        await db.SaveChangesAsync(cancellationToken);
        return await PublishReleaseHandler.MapDetailAsync(db, storage, release, cancellationToken);
    }
}
