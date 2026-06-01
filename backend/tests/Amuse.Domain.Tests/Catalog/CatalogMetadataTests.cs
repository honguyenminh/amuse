using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class CatalogMetadataTests
{
    private static readonly OrganizationId OrgId =
        OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001"));

    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-06-01T10:00:00+00:00");

    [Fact]
    public void Release_create_accepts_extended_metadata()
    {
        var result = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Metadata Album",
            Slug.From("metadata-album"),
            ReleaseType.Album,
            Now,
            Now,
            description: "A release with richer metadata.",
            upc: "123456789012",
            primaryGenre: "Alternative",
            tags: "alt,indie",
            languageCode: "en",
            labelName: "Amuse Label",
            pLine: "P 2026 Amuse Label",
            cLine: "C 2026 Amuse Label",
            metadataComplete: true);

        Assert.True(result.IsSuccess);
        var release = result.Value!;
        Assert.Equal("123456789012", release.Upc);
        Assert.Equal("Alternative", release.PrimaryGenre);
        Assert.True(release.MetadataComplete);
    }

    [Fact]
    public void Track_update_rejects_invalid_isrc_length()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Track Metadata",
            Slug.From("track-metadata"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        var track = release.AddTrack(
            TrackId.New(),
            "Track",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;

        var invalidIsrc = new string('A', Track.MaxIsrcLength + 1);
        var update = track.UpdateMetadata(
            "Track",
            1,
            false,
            invalidIsrc,
            null,
            null,
            null,
            null);

        Assert.False(update.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidTrack, update.Error);
    }
}
