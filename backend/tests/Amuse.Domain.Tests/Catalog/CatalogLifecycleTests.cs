using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class CatalogLifecycleTests
{
    private static readonly OrganizationId OrgId =
        OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001"));

    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-05-31T12:00:00+00:00");

    [Fact]
    public void Publish_release_succeeds_when_all_tracks_ready()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Test Album",
            Slug.From("test-album"),
            ReleaseType.Album,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Track One",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();

        var result = release.Publish(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReleaseLifecycleStatus.Published, release.LifecycleStatus);
        Assert.Equal(TrackLifecycleStatus.Published, track.LifecycleStatus);
    }

    [Fact]
    public void Publish_release_fails_when_track_not_ready()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Draft Album",
            Slug.From("draft-album"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        release.AddTrack(
            TrackId.New(),
            "Incomplete",
            1,
            TrackDuration.FromMilliseconds(120_000));

        var result = release.Publish(Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.TracksNotReady, result.Error);
    }

    [Fact]
    public void MarkProcessing_transitions_from_draft()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Processing Test",
            Slug.From("processing-test"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Song",
            1,
            TrackDuration.FromMilliseconds(200_000)).Value!;

        Assert.True(track.MarkProcessing().IsSuccess);
        Assert.Equal(TrackLifecycleStatus.Processing, track.LifecycleStatus);
        Assert.False(track.MarkProcessing().IsSuccess);
    }

    [Fact]
    public void Schedule_release_succeeds_when_tracks_ready_and_date_in_future()
    {
        var future = Now.AddDays(7);
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Future Album",
            Slug.From("future-album"),
            ReleaseType.Album,
            future,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Track One",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();

        var result = release.Schedule(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReleaseLifecycleStatus.Scheduled, release.LifecycleStatus);
    }

    [Fact]
    public void Schedule_release_fails_when_release_date_not_in_future()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Past Album",
            Slug.From("past-album"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Track One",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();

        var result = release.Schedule(Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.ReleaseDateNotInFuture, result.Error);
    }

    [Fact]
    public void Cancel_schedule_returns_release_to_draft()
    {
        var future = Now.AddDays(3);
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Scheduled Album",
            Slug.From("scheduled-album"),
            ReleaseType.Single,
            future,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Track One",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();

        Assert.True(release.Schedule(Now).IsSuccess);
        Assert.True(release.CancelSchedule(Now.AddHours(1)).IsSuccess);
        Assert.Equal(ReleaseLifecycleStatus.Draft, release.LifecycleStatus);
    }

    [Fact]
    public void Publish_from_scheduled_succeeds_when_tracks_ready()
    {
        var future = Now.AddDays(3);
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Scheduled Album",
            Slug.From("scheduled-publish-album"),
            ReleaseType.Single,
            future,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Track One",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();

        Assert.True(release.Schedule(Now).IsSuccess);

        var result = release.Publish(Now.AddDays(3));

        Assert.True(result.IsSuccess);
        Assert.Equal(ReleaseLifecycleStatus.Published, release.LifecycleStatus);
    }

    [Fact]
    public void CanBeDeleted_is_true_for_unpublished_lifecycle_statuses()
    {
        var future = Now.AddDays(7);
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Deletable",
            Slug.From("deletable"),
            ReleaseType.Single,
            future,
            Now).Value!;

        Assert.True(release.CanBeDeleted());

        var track = release.AddTrack(
            TrackId.New(),
            "Track",
            1,
            TrackDuration.FromMilliseconds(120_000)).Value!;

        Assert.True(track.MarkProcessing().IsSuccess);
        Assert.True(release.CanBeDeleted());

        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();
        Assert.True(release.CanBeDeleted());

        Assert.True(release.Schedule(Now).IsSuccess);
        Assert.True(release.CanBeDeleted());
    }

    [Fact]
    public void CanBeDeleted_is_false_when_published_or_hidden()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Published",
            Slug.From("published-delete"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Track",
            1,
            TrackDuration.FromMilliseconds(120_000)).Value!;
        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();
        release.Publish(Now);

        Assert.False(release.CanBeDeleted());

        release.Hide(Now);
        Assert.False(release.CanBeDeleted());
    }

    [Fact]
    public void RemoveTrack_succeeds_on_deletable_release()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Remove Track",
            Slug.From("remove-track"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var trackId = TrackId.New();
        release.AddTrack(trackId, "To Remove", 1, TrackDuration.FromMilliseconds(90_000));

        var result = release.RemoveTrack(trackId, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Empty(release.Tracks);
    }

    [Fact]
    public void RemoveTrack_fails_when_release_not_deletable()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Locked",
            Slug.From("locked-remove"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var trackId = TrackId.New();
        var track = release.AddTrack(
            trackId,
            "Track",
            1,
            TrackDuration.FromMilliseconds(90_000)).Value!;
        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();
        release.Publish(Now);

        var result = release.RemoveTrack(trackId, Now.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.ReleaseNotDeletable, result.Error);
    }

    [Fact]
    public void MarkReady_preserves_published_lifecycle()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Published Stream",
            Slug.From("published-stream"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Song",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioMaster("masters/track/master.wav");
        track.SetAudioStream("dash/track/original/manifest.mpd");
        track.MarkReady();
        release.Publish(Now);
        track.SetAudioStream("dash/track/new-manifest/manifest.mpd");

        var result = track.MarkReady();

        Assert.True(result.IsSuccess);
        Assert.Equal(TrackLifecycleStatus.Published, track.LifecycleStatus);
    }

    [Fact]
    public void TrackDuration_rejects_duration_above_max()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TrackDuration.FromMilliseconds(TrackDurationLimits.MaxMilliseconds + 1));
    }

    [Fact]
    public void SetDurationFromUploadedAudio_updates_published_track()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Published Duration",
            Slug.From("published-duration"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Song",
            1,
            TrackDuration.FromMilliseconds(1)).Value!;

        track.SetAudioMaster("masters/track/master.wav");
        track.SetAudioStream("dash/track/manifest.mpd");
        track.MarkReady();
        release.Publish(Now);

        var result = track.SetDurationFromUploadedAudio(TrackDuration.FromMilliseconds(214_000));

        Assert.True(result.IsSuccess);
        Assert.Equal(TrackLifecycleStatus.Published, track.LifecycleStatus);
        Assert.Equal(214_000, track.Duration.Milliseconds);
    }
}
