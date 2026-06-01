using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.ManageTracks;

internal sealed class CreateTrackHandler(CatalogDbContext db, CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageTrackResponse>> HandleAsync(
        Guid releaseId,
        CreateTrackRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var typedReleaseId = ReleaseId.From(releaseId);
        var release = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == typedReleaseId, cancellationToken);

        if (release is null)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(scopeResult.Error!);

        TrackDuration duration;
        try
        {
            duration = TrackDuration.FromMilliseconds(request.DurationMs);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result<ManageTrackResponse>.Failure(CatalogErrors.InvalidTrack);
        }

        var addResult = release.AddTrack(
            TrackId.New(),
            request.Title,
            request.TrackNumber,
            duration,
            request.ExplicitFlag,
            request.Isrc,
            request.Lyrics,
            request.LanguageCode,
            request.VersionTitle,
            request.ComposerCredits);

        if (!addResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(addResult.Error!);

        var track = addResult.Value!;
        db.Tracks.Add(track);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteCreateAsync(
            CatalogAuditTables.Track,
            track.Id.Value,
            CatalogAuditSnapshotMapper.FromTrack(track),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result<ManageTrackResponse>.Success(TrackMapper.ToResponse(track));
    }
}

internal sealed class UpdateTrackHandler(CatalogDbContext db, CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageTrackResponse>> HandleAsync(
        Guid trackId,
        UpdateTrackRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(orgResult.Error!);

        if (trackId == Guid.Empty)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.TrackNotFound);

        var typedTrackId = TrackId.From(trackId);
        var track = await db.Tracks
            .FirstOrDefaultAsync(t => t.Id == typedTrackId, cancellationToken);

        if (track is null)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.TrackNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(scopeResult.Error!);

        var duplicateNumber = await db.Tracks
            .AsNoTracking()
            .AnyAsync(
                t => t.ReleaseId == track.ReleaseId
                     && t.TrackNumber == request.TrackNumber
                     && t.Id != track.Id,
                cancellationToken);

        if (duplicateNumber)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.DuplicateTrackNumber);

        var before = CatalogAuditSnapshotMapper.FromTrack(track);

        var updateResult = track.UpdateMetadata(
            request.Title,
            request.TrackNumber,
            request.ExplicitFlag,
            request.Isrc,
            request.Lyrics,
            request.LanguageCode,
            request.VersionTitle,
            request.ComposerCredits);

        if (!updateResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(updateResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Track,
            track.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromTrack(track),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result<ManageTrackResponse>.Success(TrackMapper.ToResponse(track));
    }
}

internal sealed class DeleteTrackHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IClock clock,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result> HandleAsync(
        Guid trackId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result.Failure(orgResult.Error!);

        if (trackId == Guid.Empty)
            return Result.Failure(CatalogErrors.TrackNotFound);

        var typedTrackId = TrackId.From(trackId);
        var track = await db.Tracks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typedTrackId, cancellationToken);

        if (track is null)
            return Result.Failure(CatalogErrors.TrackNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result.Failure(scopeResult.Error!);

        var release = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == track.ReleaseId, cancellationToken);

        if (release is null)
            return Result.Failure(CatalogErrors.ReleaseNotFound);

        var before = CatalogAuditSnapshotMapper.FromTrack(track);
        var removeResult = release.RemoveTrack(typedTrackId, clock.UtcNow);
        if (!removeResult.IsSuccess)
            return Result.Failure(removeResult.Error!);

        var removedTrack = removeResult.Value!;
        await CatalogMediaCleanup.DeleteTrackMediaAsync(db, storage, removedTrack, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteDeleteAsync(
            CatalogAuditTables.Track,
            removedTrack.Id.Value,
            before,
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result.Success();
    }
}

internal static class TrackMapper
{
    internal static ManageTrackResponse ToResponse(Track track) =>
        new(
            track.Id.Value,
            track.Title,
            track.TrackNumber,
            track.Duration.Milliseconds,
            track.ExplicitFlag,
            track.Isrc,
            track.Lyrics,
            track.LanguageCode,
            track.VersionTitle,
            track.ComposerCredits,
            track.LifecycleStatus,
            HasAudioMaster: !string.IsNullOrEmpty(track.AudioMasterKey),
            HasAudioStream: !string.IsNullOrEmpty(track.AudioStreamKey));
}
