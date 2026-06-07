using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class TrackLoudnessProfileAssignmentTests
{
    private static readonly OrganizationId OrgId =
        OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001"));

    private static readonly DateTimeOffset AnalyzedAt = DateTimeOffset.Parse("2026-06-06T00:00:00+00:00");

    [Fact]
    public void SetLoudnessProfile_succeeds_while_processing()
    {
        var track = CreateProcessingTrack();
        var profile = TrackLoudnessProfile.FromAnalysis(-20, -5, 8, -30, AnalyzedAt);

        var result = track.SetLoudnessProfile(profile);

        Assert.True(result.IsSuccess);
        Assert.Equal(4.0, track.LoudnessProfile!.LinearGainLu, precision: 2);
    }

    [Fact]
    public void SetLoudnessProfile_succeeds_on_published_track_without_stream()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Published",
            Slug.From("published"),
            ReleaseType.Single,
            AnalyzedAt,
            AnalyzedAt).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Published",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioMaster("masters/track/master.wav");
        track.MarkReady();
        release.Publish(AnalyzedAt);

        var profile = TrackLoudnessProfile.FromAnalysis(-20, -5, 8, -30, AnalyzedAt);
        var result = track.SetLoudnessProfile(profile);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SetLoudnessProfile_fails_on_published_track_with_existing_stream()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Published Stream",
            Slug.From("published-stream"),
            ReleaseType.Single,
            AnalyzedAt,
            AnalyzedAt).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Published Stream",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioMaster("masters/track/master.wav");
        track.SetAudioStream("dash/track/manifest/manifest.mpd");
        track.MarkReady();
        release.Publish(AnalyzedAt);

        var profile = TrackLoudnessProfile.FromAnalysis(-20, -5, 8, -30, AnalyzedAt);
        var result = track.SetLoudnessProfile(profile);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidLifecycleTransition, result.Error);
    }

    [Fact]
    public void SetLoudnessProfile_fails_without_audio_master()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Demo",
            Slug.From("demo"),
            ReleaseType.Single,
            AnalyzedAt,
            AnalyzedAt).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Demo",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.MarkProcessing();

        var profile = TrackLoudnessProfile.FromAnalysis(-20, -5, 8, -30, AnalyzedAt);
        var result = track.SetLoudnessProfile(profile);

        Assert.False(result.IsSuccess);
    }

    private static Track CreateProcessingTrack()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Demo",
            Slug.From("demo"),
            ReleaseType.Single,
            AnalyzedAt,
            AnalyzedAt).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Demo",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        track.SetAudioMaster("releases/demo/01-demo.wav");
        track.MarkProcessing();
        return track;
    }
}
