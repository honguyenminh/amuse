using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.RetryTrackTranscode;

public sealed record RetryTrackTranscodeResponse(
    Guid TrackId,
    Guid JobId,
    string MasterKey,
    string StreamKey,
    AudioTranscodeJobStatus JobStatus,
    int AttemptCount,
    bool ReusedInflightJob);

internal sealed class RetryTrackTranscodeHandler(
    CatalogDbContext db,
    IAudioTranscodeJobQueue jobQueue,
    IClock clock)
{
    public async Task<Result<RetryTrackTranscodeResponse>> HandleAsync(
        Guid trackId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<RetryTrackTranscodeResponse>.Failure(CatalogErrors.TrackNotFound);

        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<RetryTrackTranscodeResponse>.Failure(orgResult.Error!);

        var typedTrackId = TrackId.From(trackId);
        var track = await db.Tracks.FirstOrDefaultAsync(t => t.Id == typedTrackId, cancellationToken);
        if (track is null)
            return Result<RetryTrackTranscodeResponse>.Failure(CatalogErrors.TrackNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<RetryTrackTranscodeResponse>.Failure(scopeResult.Error!);

        if (string.IsNullOrWhiteSpace(track.AudioMasterKey))
            return Result<RetryTrackTranscodeResponse>.Failure(CatalogErrors.TrackHasNoAudio);

        var existingInflight = await db.AudioTranscodeJobs
            .Where(j => j.TrackId == typedTrackId
                && (j.Status == AudioTranscodeJobStatus.Queued || j.Status == AudioTranscodeJobStatus.Processing))
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingInflight is not null)
        {
            return Result<RetryTrackTranscodeResponse>.Success(
                new RetryTrackTranscodeResponse(
                    trackId,
                    existingInflight.Id,
                    existingInflight.MasterKey,
                    existingInflight.StreamKey,
                    existingInflight.Status,
                    existingInflight.AttemptCount,
                    true));
        }

        var latestJob = await db.AudioTranscodeJobs
            .Where(j => j.TrackId == typedTrackId)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (latestJob is null)
            return Result<RetryTrackTranscodeResponse>.Failure(CatalogErrors.NoTranscodeJobToRetry);

        if (!string.IsNullOrWhiteSpace(track.AudioStreamKey) || latestJob.Status == AudioTranscodeJobStatus.Succeeded)
            return Result<RetryTrackTranscodeResponse>.Failure(CatalogErrors.TrackStreamAlreadyReady);

        if (latestJob.Status != AudioTranscodeJobStatus.Failed)
            return Result<RetryTrackTranscodeResponse>.Failure(CatalogErrors.TranscodeRetryNotAllowed);

        if (track.LifecycleStatus is TrackLifecycleStatus.Draft or TrackLifecycleStatus.Ready)
        {
            var processingResult = track.MarkProcessing();
            if (!processingResult.IsSuccess)
                return Result<RetryTrackTranscodeResponse>.Failure(processingResult.Error!);
        }
        else if (track.LifecycleStatus != TrackLifecycleStatus.Processing)
        {
            return Result<RetryTrackTranscodeResponse>.Failure(CatalogErrors.TranscodeRetryNotAllowed);
        }

        var streamKey = $"dash/{trackId}/{Guid.CreateVersion7()}/manifest.mpd";
        var job = AudioTranscodeJob.Enqueue(track.Id, track.AudioMasterKey, streamKey, clock.UtcNow);
        db.AudioTranscodeJobs.Add(job);

        await db.SaveChangesAsync(cancellationToken);

        await jobQueue.PublishAsync(
            new AudioTranscodeJobMessage(job.Id, trackId, job.MasterKey, job.StreamKey),
            cancellationToken);

        return Result<RetryTrackTranscodeResponse>.Success(
            new RetryTrackTranscodeResponse(
                trackId,
                job.Id,
                job.MasterKey,
                job.StreamKey,
                job.Status,
                job.AttemptCount,
                false));
    }
}
