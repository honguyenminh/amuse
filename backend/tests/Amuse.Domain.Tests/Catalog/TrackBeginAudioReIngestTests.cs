using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class TrackBeginAudioReIngestTests
{
    private static readonly OrganizationId OrgId =
        OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001"));

    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-06-07T12:00:00+00:00");

    [Fact]
    public void BeginAudioReIngest_sets_master_and_marks_processing_from_draft()
    {
        var track = CreateDraftTrack();

        var result = track.BeginAudioReIngest("masters/track/new-master.wav");

        Assert.True(result.IsSuccess);
        Assert.Equal("masters/track/new-master.wav", track.AudioMasterKey);
        Assert.Equal(TrackLifecycleStatus.Processing, track.LifecycleStatus);
    }

    [Fact]
    public void BeginAudioReIngest_clears_existing_stream_and_loudness_on_reupload()
    {
        var track = CreateReadyTrackWithStream();
        track.SetLoudnessProfile(
            TrackLoudnessProfile.FromAnalysis(-20, -5, 8, -30, Now));

        var result = track.BeginAudioReIngest("masters/track/reupload.wav");

        Assert.True(result.IsSuccess);
        Assert.Null(track.AudioStreamKey);
        Assert.Null(track.LoudnessProfile);
        Assert.Equal("masters/track/reupload.wav", track.AudioMasterKey);
        Assert.Equal(TrackLifecycleStatus.Processing, track.LifecycleStatus);
    }

    [Fact]
    public void BeginAudioReIngest_succeeds_when_already_processing()
    {
        var track = CreateProcessingTrack();

        var result = track.BeginAudioReIngest("masters/track/replace.wav");

        Assert.True(result.IsSuccess);
        Assert.Equal("masters/track/replace.wav", track.AudioMasterKey);
        Assert.Equal(TrackLifecycleStatus.Processing, track.LifecycleStatus);
    }

    [Fact]
    public void BeginAudioReIngest_fails_for_published_track_with_stream()
    {
        var track = CreatePublishedTrackWithStream();

        var result = track.BeginAudioReIngest("masters/track/new-master.wav");

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidLifecycleTransition, result.Error);
    }

    [Fact]
    public void BeginAudioReIngest_fails_for_empty_master_key()
    {
        var track = CreateDraftTrack();

        var result = track.BeginAudioReIngest("   ");

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidAudioUploadRequest, result.Error);
    }

    [Fact]
    public void BeginAudioReIngest_fails_for_key_exceeding_max_length()
    {
        var track = CreateDraftTrack();
        var tooLong = new string('a', Track.MaxKeyLength + 1);

        var result = track.BeginAudioReIngest(tooLong);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidTrack, result.Error);
    }

    private static Track CreateDraftTrack()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Re-ingest",
            Slug.From("re-ingest"),
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
        track.SetAudioMaster("masters/track/original.wav");
        track.MarkProcessing();
        return track;
    }

    private static Track CreateReadyTrackWithStream()
    {
        var track = CreateDraftTrack();
        track.SetAudioMaster("masters/track/original.wav");
        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();
        return track;
    }

    private static Track CreatePublishedTrackWithStream()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Published Re-ingest",
            Slug.From("published-re-ingest"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Published",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioMaster("masters/track/original.wav");
        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();
        release.Publish(Now);
        return track;
    }
}
