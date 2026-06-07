using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class ReleaseAggregateTests
{
    private static readonly OrganizationId OrgId =
        OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001"));

    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-05-31T12:00:00+00:00");

    [Fact]
    public void UpdateTrack_succeeds_and_updates_release_timestamp()
    {
        var release = CreateRelease();
        var track = AddTrack(release, 1, "Track One");

        var result = release.UpdateTrack(
            track.Id,
            "Renamed",
            2,
            explicitFlag: true,
            isrc: "USABC1234567",
            lyrics: null,
            languageCode: "en",
            versionTitle: null,
            composerCredits: null,
            Now.AddMinutes(5));

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", track.Title);
        Assert.Equal(2, track.TrackNumber);
        Assert.True(track.ExplicitFlag);
        Assert.Equal(Now.AddMinutes(5), release.UpdatedAt);
    }

    [Fact]
    public void UpdateTrack_fails_when_track_number_conflicts_with_sibling()
    {
        var release = CreateRelease();
        var first = AddTrack(release, 1, "First");
        var second = AddTrack(release, 2, "Second");

        var result = release.UpdateTrack(
            second.Id,
            "Second",
            first.TrackNumber,
            explicitFlag: false,
            isrc: null,
            lyrics: null,
            languageCode: null,
            versionTitle: null,
            composerCredits: null,
            Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.DuplicateTrackNumber, result.Error);
        Assert.Equal(2, second.TrackNumber);
    }

    [Fact]
    public void UpdateTrack_fails_when_track_not_in_release()
    {
        var release = CreateRelease();
        AddTrack(release, 1, "Only Track");

        var result = release.UpdateTrack(
            TrackId.New(),
            "Missing",
            1,
            explicitFlag: false,
            isrc: null,
            lyrics: null,
            languageCode: null,
            versionTitle: null,
            composerCredits: null,
            Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.TrackNotFound, result.Error);
    }

    [Fact]
    public void ReplaceCollaborators_replaces_ordered_featured_artists()
    {
        var primaryArtistId = ArtistId.New();
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            primaryArtistId,
            "Collab Album",
            Slug.From("collab-album"),
            ReleaseType.Album,
            Now,
            Now).Value!;

        var featuredOne = ArtistId.New();
        var featuredTwo = ArtistId.New();

        var result = release.ReplaceCollaborators([featuredOne, featuredTwo]);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, release.Collaborators.Count);
        Assert.Equal(featuredOne, release.Collaborators[0].ArtistId);
        Assert.Equal(1, release.Collaborators[0].DisplayOrder);
        Assert.Equal(featuredTwo, release.Collaborators[1].ArtistId);
        Assert.Equal(2, release.Collaborators[1].DisplayOrder);
        Assert.All(release.Collaborators, c => Assert.Equal(ReleaseCollaboratorRole.Featured, c.Role));
    }

    [Fact]
    public void ReplaceCollaborators_clears_existing_collaborators()
    {
        var primaryArtistId = ArtistId.New();
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            primaryArtistId,
            "Collab Album",
            Slug.From("collab-clear"),
            ReleaseType.Album,
            Now,
            Now).Value!;

        Assert.True(release.ReplaceCollaborators([ArtistId.New()]).IsSuccess);
        Assert.Single(release.Collaborators);

        var result = release.ReplaceCollaborators([]);

        Assert.True(result.IsSuccess);
        Assert.Empty(release.Collaborators);
    }

    [Fact]
    public void ReplaceCollaborators_fails_when_primary_artist_is_listed()
    {
        var primaryArtistId = ArtistId.New();
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            primaryArtistId,
            "Self Collab",
            Slug.From("self-collab"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var result = release.ReplaceCollaborators([primaryArtistId, ArtistId.New()]);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidCollaborator, result.Error);
        Assert.Empty(release.Collaborators);
    }

    [Fact]
    public void ReplaceCollaborators_deduplicates_artist_ids()
    {
        var primaryArtistId = ArtistId.New();
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            primaryArtistId,
            "Dedup Collab",
            Slug.From("dedup-collab"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var featured = ArtistId.New();
        var result = release.ReplaceCollaborators([featured, featured]);

        Assert.True(result.IsSuccess);
        Assert.Single(release.Collaborators);
        Assert.Equal(featured, release.Collaborators[0].ArtistId);
    }

    private static Release CreateRelease() =>
        Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Aggregate Test",
            Slug.From("aggregate-test"),
            ReleaseType.Single,
            Now,
            Now).Value!;

    private static Track AddTrack(Release release, int trackNumber, string title) =>
        release.AddTrack(
            TrackId.New(),
            title,
            trackNumber,
            TrackDuration.FromMilliseconds(180_000)).Value!;
}
