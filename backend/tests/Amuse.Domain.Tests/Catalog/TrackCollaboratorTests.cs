using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class TrackCollaboratorTests
{
    private static readonly OrganizationId OrgId =
        OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001"));

    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-05-31T12:00:00+00:00");

    [Fact]
    public void ReplaceCollaborators_replaces_linked_and_placeholder_artists()
    {
        var primaryArtistId = ArtistId.New();
        var track = CreateTrack(primaryArtistId);
        var featured = ArtistId.New();

        var result = track.ReplaceCollaborators(
        [
            new TrackCollaboratorAssignment(featured, null),
            new TrackCollaboratorAssignment(null, "Guest MC"),
        ],
        primaryArtistId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, track.Collaborators.Count);
        Assert.Equal(featured, track.Collaborators[0].ArtistId);
        Assert.Null(track.Collaborators[0].DisplayName);
        Assert.Null(track.Collaborators[1].ArtistId);
        Assert.Equal("Guest MC", track.Collaborators[1].DisplayName);
    }

    [Fact]
    public void ReplaceCollaborators_fails_when_primary_artist_is_listed()
    {
        var primaryArtistId = ArtistId.New();
        var track = CreateTrack(primaryArtistId);

        var result = track.ReplaceCollaborators(
            [new TrackCollaboratorAssignment(primaryArtistId, null)],
            primaryArtistId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidCollaborator, result.Error);
        Assert.Empty(track.Collaborators);
    }

    [Fact]
    public void ReplaceCollaborators_deduplicates_placeholder_names_case_insensitively()
    {
        var primaryArtistId = ArtistId.New();
        var track = CreateTrack(primaryArtistId);

        var result = track.ReplaceCollaborators(
        [
            new TrackCollaboratorAssignment(null, "Guest"),
            new TrackCollaboratorAssignment(null, "guest"),
        ],
        primaryArtistId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidCollaborator, result.Error);
    }

    private static Track CreateTrack(ArtistId primaryArtistId)
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            primaryArtistId,
            "Collab Single",
            Slug.From("collab-single"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        return release.AddTrack(
            TrackId.New(),
            "Main",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;
    }
}
