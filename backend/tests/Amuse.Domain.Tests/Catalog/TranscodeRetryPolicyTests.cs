using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class TranscodeRetryPolicyTests
{
    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-06-07T12:00:00+00:00");

    private static readonly TimeSpan StaleTimeout = TimeSpan.FromMinutes(45);

    [Fact]
    public void EvaluateInflight_returns_none_when_no_job()
    {
        var decision = TranscodeRetryPolicy.EvaluateInflight(null, Now, StaleTimeout);

        Assert.Equal(TranscodeRetryPolicy.InflightDecision.None, decision);
    }

    [Fact]
    public void EvaluateInflight_reuses_fresh_queued_job()
    {
        var job = CreateSnapshot(TranscodeJobStatus.Queued, Now);

        var decision = TranscodeRetryPolicy.EvaluateInflight(job, Now, StaleTimeout);

        Assert.Equal(TranscodeRetryPolicy.InflightDecision.Reuse, decision);
    }

    [Fact]
    public void EvaluateInflight_marks_stale_processing_job_for_failure()
    {
        var job = CreateSnapshot(
            TranscodeJobStatus.Processing,
            Now - StaleTimeout - TimeSpan.FromMinutes(1));

        var decision = TranscodeRetryPolicy.EvaluateInflight(job, Now, StaleTimeout);

        Assert.Equal(TranscodeRetryPolicy.InflightDecision.MarkStaleAndContinue, decision);
    }

    [Fact]
    public void EvaluateInflight_reuses_processing_job_within_timeout()
    {
        var job = CreateSnapshot(
            TranscodeJobStatus.Processing,
            Now - TimeSpan.FromMinutes(10));

        var decision = TranscodeRetryPolicy.EvaluateInflight(job, Now, StaleTimeout);

        Assert.Equal(TranscodeRetryPolicy.InflightDecision.Reuse, decision);
    }

    [Fact]
    public void EvaluateRetryEligibility_fails_without_audio_master()
    {
        var track = CreateDraftTrack();

        var result = TranscodeRetryPolicy.EvaluateRetryEligibility(
            track,
            CreateSnapshot(TranscodeJobStatus.Failed, Now));

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.TrackHasNoAudio, result.Error);
    }

    [Fact]
    public void EvaluateRetryEligibility_fails_when_stream_already_ready()
    {
        var track = CreateDraftTrack();
        track.SetAudioMaster("masters/track/master.wav");
        track.SetAudioStream("dash/track/manifest.mpd");

        var result = TranscodeRetryPolicy.EvaluateRetryEligibility(
            track,
            CreateSnapshot(TranscodeJobStatus.Failed, Now));

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.TrackStreamAlreadyReady, result.Error);
    }

    [Fact]
    public void EvaluateRetryEligibility_fails_when_latest_job_succeeded()
    {
        var track = CreateDraftTrack();
        track.SetAudioMaster("masters/track/master.wav");

        var result = TranscodeRetryPolicy.EvaluateRetryEligibility(
            track,
            CreateSnapshot(TranscodeJobStatus.Succeeded, Now));

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.TrackStreamAlreadyReady, result.Error);
    }

    [Fact]
    public void EvaluateRetryEligibility_fails_when_latest_job_not_failed()
    {
        var track = CreateDraftTrack();
        track.SetAudioMaster("masters/track/master.wav");

        var result = TranscodeRetryPolicy.EvaluateRetryEligibility(
            track,
            CreateSnapshot(TranscodeJobStatus.Queued, Now));

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.TranscodeRetryNotAllowed, result.Error);
    }

    [Fact]
    public void EvaluateRetryEligibility_transitions_draft_track_to_processing()
    {
        var track = CreateDraftTrack();
        track.SetAudioMaster("masters/track/master.wav");

        var result = TranscodeRetryPolicy.EvaluateRetryEligibility(
            track,
            CreateSnapshot(TranscodeJobStatus.Failed, Now));

        Assert.True(result.IsSuccess);
        Assert.Equal(TrackLifecycleStatus.Processing, track.LifecycleStatus);
    }

    [Fact]
    public void EvaluateRetryEligibility_allows_processing_track()
    {
        var track = CreateProcessingTrack();

        var result = TranscodeRetryPolicy.EvaluateRetryEligibility(
            track,
            CreateSnapshot(TranscodeJobStatus.Failed, Now));

        Assert.True(result.IsSuccess);
        Assert.Equal(TrackLifecycleStatus.Processing, track.LifecycleStatus);
    }

    [Fact]
    public void EvaluateRetryEligibility_fails_for_published_track()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001")),
            ArtistId.New(),
            "Published Retry",
            Slug.From("published-retry"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Published",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioMaster("masters/track/master.wav");
        release.MarkPublishedForDevelopment(Now);

        var result = TranscodeRetryPolicy.EvaluateRetryEligibility(
            track,
            CreateSnapshot(TranscodeJobStatus.Failed, Now));

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.TranscodeRetryNotAllowed, result.Error);
    }

    private static TranscodeJobSnapshot CreateSnapshot(
        TranscodeJobStatus status,
        DateTimeOffset updatedAt) =>
        new(
            Guid.CreateVersion7(),
            status,
            updatedAt,
            "masters/track/master.wav",
            "dash/track/manifest.mpd",
            1);

    private static Track CreateDraftTrack()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001")),
            ArtistId.New(),
            "Retry Policy",
            Slug.From("retry-policy"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        return release.AddTrack(
            TrackId.New(),
            "Song",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;
    }

    private static Track CreateProcessingTrack()
    {
        var track = CreateDraftTrack();
        track.SetAudioMaster("masters/track/master.wav");
        track.MarkProcessing();
        return track;
    }
}
