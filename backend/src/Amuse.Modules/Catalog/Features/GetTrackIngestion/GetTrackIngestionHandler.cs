using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetTrackIngestion;

internal sealed class GetTrackIngestionHandler(CatalogDbContext db)
{
    public async Task<Result<TrackIngestionResponse>> HandleAsync(
        Guid trackId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<TrackIngestionResponse>.Failure(orgResult.Error!);

        if (trackId == Guid.Empty)
            return Result<TrackIngestionResponse>.Failure(CatalogErrors.TrackNotFound);

        var typedTrackId = TrackId.From(trackId);
        var track = await db.Tracks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typedTrackId, cancellationToken);

        if (track is null)
            return Result<TrackIngestionResponse>.Failure(CatalogErrors.TrackNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<TrackIngestionResponse>.Failure(scopeResult.Error!);

        var latestJob = await db.AudioTranscodeJobs
            .AsNoTracking()
            .Where(j => j.TrackId == typedTrackId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new
            {
                j.Id,
                j.Status,
                j.LastError,
                j.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<TrackIngestionResponse>.Success(
            new TrackIngestionResponse(
                track.Id.Value,
                track.LifecycleStatus,
                track.AudioMasterKey,
                track.AudioStreamKey,
                latestJob?.Id,
                latestJob?.Status,
                latestJob?.LastError,
                latestJob?.UpdatedAt));
    }
}
