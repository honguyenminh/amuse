using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Messaging;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
    IClock clock,
    IOptions<TranscodeJobRecoveryOptions> recoveryOptions)
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

        var now = clock.UtcNow;
        var staleTimeout = recoveryOptions.Value.StaleProcessingTimeout;

        var existingInflight = await TranscodeJobQueries.GetLatestInflightForTrackAsync(
            db,
            typedTrackId,
            cancellationToken);

        switch (TranscodeRetryPolicy.EvaluateInflight(existingInflight?.ToSnapshot(), now, staleTimeout))
        {
            case TranscodeRetryPolicy.InflightDecision.Reuse when existingInflight is not null:
                if (existingInflight.Status == AudioTranscodeJobStatus.Queued)
                    await EnsureOutboxDispatchAsync(existingInflight, now, cancellationToken);

                return Result<RetryTrackTranscodeResponse>.Success(
                    new RetryTrackTranscodeResponse(
                        trackId,
                        existingInflight.Id,
                        existingInflight.MasterKey,
                        existingInflight.StreamKey,
                        existingInflight.Status,
                        existingInflight.AttemptCount,
                        true));

            case TranscodeRetryPolicy.InflightDecision.MarkStaleAndContinue when existingInflight is not null:
                existingInflight.MarkFailed(TranscodeRetryPolicy.StaleFailureReason, now);
                await db.SaveChangesAsync(cancellationToken);
                break;
        }

        var latestJob = await TranscodeJobQueries.GetLatestForTrackAsync(
            db,
            typedTrackId,
            cancellationToken);

        var eligibility = TranscodeRetryPolicy.EvaluateRetryEligibility(track, latestJob?.ToSnapshot());
        if (!eligibility.IsSuccess)
            return Result<RetryTrackTranscodeResponse>.Failure(eligibility.Error!);

        var streamKey = $"dash/{trackId}/{Guid.CreateVersion7()}/manifest.mpd";
        var job = AudioTranscodeJob.Enqueue(track.Id, track.AudioMasterKey, streamKey, now);
        db.AudioTranscodeJobs.Add(job);
        db.CatalogOutboxMessages.Add(
            CatalogOutboxMessage.EnqueueAudioTranscode(
                new AudioTranscodeJobMessage(job.Id, trackId, job.MasterKey, job.StreamKey),
                now));

        await db.SaveChangesAsync(cancellationToken);

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

    private async Task EnsureOutboxDispatchAsync(
        AudioTranscodeJob job,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var jobIdString = job.Id.ToString();
        var pendingOutbox = await db.CatalogOutboxMessages
            .Where(m => m.ProcessedAt == null
                && m.MessageType == CatalogOutboxMessage.AudioTranscodeJobType)
            .Select(m => m.PayloadJson)
            .ToListAsync(cancellationToken);

        if (pendingOutbox.Any(payload => payload.Contains(jobIdString, StringComparison.Ordinal)))
            return;

        db.CatalogOutboxMessages.Add(
            CatalogOutboxMessage.EnqueueAudioTranscode(
                new AudioTranscodeJobMessage(job.Id, job.TrackId.Value, job.MasterKey, job.StreamKey),
                now));
        await db.SaveChangesAsync(cancellationToken);
    }
}
