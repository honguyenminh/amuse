using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Catalog;

public sealed class CatalogPricingTests
{
    private static readonly OrganizationId OrgId =
        OrganizationId.From(Guid.Parse("019e7000-0000-7000-8000-000000000001"));

    private static readonly DateTimeOffset Now =
        DateTimeOffset.Parse("2026-05-31T12:00:00+00:00");

    [Fact]
    public void SetPricing_accepts_pwyw_open_ceiling()
    {
        var track = CreateTrack();

        var result = track.SetPricing(
            isForSale: true,
            priceFloorMinor: 0,
            priceCeilingMinor: null,
            priceCurrency: "USD");

        Assert.True(result.IsSuccess);
        Assert.True(track.IsForSale);
        Assert.Equal(0, track.PriceFloorMinor);
        Assert.Null(track.PriceCeilingMinor);
        Assert.Equal("USD", track.PriceCurrency);
    }

    [Fact]
    public void SetPricing_rejects_ceiling_below_floor()
    {
        var track = CreateTrack();

        var result = track.SetPricing(
            isForSale: true,
            priceFloorMinor: 500,
            priceCeilingMinor: 100,
            priceCurrency: "USD");

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidPricingBounds, result.Error);
    }

    [Fact]
    public void SetPricing_requires_currency_when_for_sale()
    {
        var track = CreateTrack();

        var result = track.SetPricing(
            isForSale: true,
            priceFloorMinor: 100,
            priceCeilingMinor: null,
            priceCurrency: null);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidPricingCurrency, result.Error);
    }

    [Fact]
    public void Publish_fails_when_release_floor_exceeds_track_floors()
    {
        var release = CreateReleaseWithTracks(
            ("One", 200),
            ("Two", 300));

        Assert.True(release.SetPricing(true, 600, null, "USD", Now).IsSuccess);
        Assert.True(release.Tracks[0].SetPricing(true, 200, null, "USD").IsSuccess);
        Assert.True(release.Tracks[1].SetPricing(true, 300, null, "USD").IsSuccess);
        MarkTracksReady(release);

        var result = release.Publish(Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.ReleaseFloorExceedsTrackFloors, result.Error);
    }

    [Fact]
    public void Publish_fails_when_release_ceiling_exceeds_track_ceilings()
    {
        var release = CreateReleaseWithTracks(
            ("One", 200),
            ("Two", 300));

        Assert.True(release.SetPricing(true, 400, 700, "USD", Now).IsSuccess);
        Assert.True(release.Tracks[0].SetPricing(true, 200, 250, "USD").IsSuccess);
        Assert.True(release.Tracks[1].SetPricing(true, 300, 350, "USD").IsSuccess);
        MarkTracksReady(release);

        var result = release.Publish(Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.ReleaseCeilingExceedsTrackCeilings, result.Error);
    }

    [Fact]
    public void Publish_succeeds_when_release_pricing_matches_track_bounds()
    {
        var release = CreateReleaseWithTracks(
            ("One", 200),
            ("Two", 300));

        Assert.True(release.SetPricing(true, 500, 600, "USD", Now).IsSuccess);
        Assert.True(release.Tracks[0].SetPricing(true, 200, 250, "USD").IsSuccess);
        Assert.True(release.Tracks[1].SetPricing(true, 300, 350, "USD").IsSuccess);
        MarkTracksReady(release);

        var result = release.Publish(Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RoyaltySplit_replace_requires_shares_to_sum_to_100_percent()
    {
        var trackId = TrackId.New();
        var listingOrg = OrgId;

        var result = RoyaltySplit.ReplaceForTrack(
            trackId,
            listingOrg,
            [
                new RoyaltySplitEntry(OrganizationId.New(), 4000),
                new RoyaltySplitEntry(OrganizationId.New(), 4000),
            ],
            Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.RoyaltySplitSumInvalid, result.Error);
    }

    [Fact]
    public void RoyaltySplit_replace_rejects_duplicate_payees()
    {
        var trackId = TrackId.New();
        var payee = OrganizationId.New();

        var result = RoyaltySplit.ReplaceForTrack(
            trackId,
            OrgId,
            [
                new RoyaltySplitEntry(payee, 5000),
                new RoyaltySplitEntry(payee, 5000),
            ],
            Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.DuplicateRoyaltyPayee, result.Error);
    }

    [Fact]
    public void RoyaltySplit_replace_succeeds_when_shares_sum_to_100_percent()
    {
        var trackId = TrackId.New();

        var result = RoyaltySplit.ReplaceForTrack(
            trackId,
            OrgId,
            [
                new RoyaltySplitEntry(OrganizationId.New(), 6000),
                new RoyaltySplitEntry(OrganizationId.New(), 4000),
            ],
            Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(10_000, result.Value.Sum(split => split.ShareBps));
    }

    [Fact]
    public void Publish_fails_when_for_sale_track_has_invalid_split_sum()
    {
        var release = CreateReleaseWithTracks(("Song", 100));
        var track = release.Tracks[0];

        Assert.True(track.SetPricing(true, 100, 100, "USD").IsSuccess);
        MarkTracksReady(release);

        var invalidSplit = RoyaltySplit.Create(
            RoyaltySplitId.New(),
            track.Id,
            OrganizationId.New(),
            5000,
            OrgId,
            Now).Value!;

        var result = release.Publish(Now, [invalidSplit]);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.RoyaltySplitSumInvalid, result.Error);
    }

    private static Track CreateTrack()
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Pricing Test",
            Slug.From("pricing-test"),
            ReleaseType.Single,
            Now,
            Now).Value!;

        return release.AddTrack(
            TrackId.New(),
            "Song",
            1,
            TrackDuration.FromMilliseconds(180_000)).Value!;
    }

    private static Release CreateReleaseWithTracks(params (string Title, int FloorMinor)[] tracks)
    {
        var release = Release.Create(
            ReleaseId.New(),
            OrgId,
            ArtistId.New(),
            "Pricing Album",
            Slug.From("pricing-album"),
            ReleaseType.Album,
            Now,
            Now).Value!;

        var trackNumber = 1;
        foreach (var (title, _) in tracks)
        {
            release.AddTrack(
                TrackId.New(),
                title,
                trackNumber++,
                TrackDuration.FromMilliseconds(180_000));
        }

        return release;
    }

    private static void MarkTracksReady(Release release)
    {
        foreach (var track in release.Tracks)
        {
            track.SetAudioStream("dash/track/manifest.mpd");
            track.MarkReady();
        }
    }
}
