using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public static class TranscodeRetryPolicy
{
    public const string StaleFailureReason = "Timed out";

    public enum InflightDecision
    {
        None,
        Reuse,
        MarkStaleAndContinue,
    }

    public static InflightDecision EvaluateInflight(
        TranscodeJobSnapshot? inflight,
        DateTimeOffset now,
        TimeSpan staleProcessingTimeout)
    {
        if (inflight is null)
            return InflightDecision.None;

        if (IsStaleProcessing(inflight.Value, now, staleProcessingTimeout))
            return InflightDecision.MarkStaleAndContinue;

        return InflightDecision.Reuse;
    }

    public static bool IsInflight(TranscodeJobStatus status) =>
        status is TranscodeJobStatus.Queued or TranscodeJobStatus.Processing;

    public static bool IsStaleProcessing(
        TranscodeJobSnapshot job,
        DateTimeOffset now,
        TimeSpan staleProcessingTimeout) =>
        job.Status == TranscodeJobStatus.Processing
        && job.UpdatedAt < now - staleProcessingTimeout;

    public static Result EvaluateRetryEligibility(
        Track track,
        TranscodeJobSnapshot? latestJob)
    {
        if (string.IsNullOrWhiteSpace(track.AudioMasterKey))
            return Result.Failure(CatalogErrors.TrackHasNoAudio);

        if (latestJob is null)
            return Result.Failure(CatalogErrors.NoTranscodeJobToRetry);

        var job = latestJob.Value;

        if (!string.IsNullOrWhiteSpace(track.AudioStreamKey)
            || job.Status == TranscodeJobStatus.Succeeded)
        {
            return Result.Failure(CatalogErrors.TrackStreamAlreadyReady);
        }

        if (job.Status != TranscodeJobStatus.Failed)
            return Result.Failure(CatalogErrors.TranscodeRetryNotAllowed);

        return EnsureLifecycleForRetry(track);
    }

    public static Result EnsureLifecycleForRetry(Track track)
    {
        if (track.LifecycleStatus is TrackLifecycleStatus.Draft or TrackLifecycleStatus.Ready)
            return track.MarkProcessing();

        if (track.LifecycleStatus == TrackLifecycleStatus.Processing)
            return Result.Success();

        return Result.Failure(CatalogErrors.TranscodeRetryNotAllowed);
    }
}
