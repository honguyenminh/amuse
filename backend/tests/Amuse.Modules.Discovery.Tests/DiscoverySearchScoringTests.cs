using Amuse.Modules.Discovery.Features.Search;

namespace Amuse.Modules.Discovery.Tests;

public sealed class DiscoverySearchScoringTests
{
    [Fact]
    public void ScoreTextMatch_prefers_exact_over_prefix_over_substring()
    {
        Assert.Equal(DiscoverySearchScoring.ExactTitleScore, DiscoverySearchScoring.ScoreTextMatch("alpha", "Alpha"));
        Assert.Equal(DiscoverySearchScoring.PrefixTitleScore, DiscoverySearchScoring.ScoreTextMatch("alpha", "Alphabet"));
        Assert.Equal(DiscoverySearchScoring.SubstringTitleScore, DiscoverySearchScoring.ScoreTextMatch("alpha", "My Alpha Song"));
    }

    [Fact]
    public void ScoreSlugMatch_is_capped_below_title_exact()
    {
        Assert.Equal(DiscoverySearchScoring.ExactSlugScore, DiscoverySearchScoring.ScoreSlugMatch("alpha", "alpha"));
        Assert.True(DiscoverySearchScoring.ExactSlugScore < DiscoverySearchScoring.ExactTitleScore);
    }

    [Fact]
    public void ComputeMatchScore_uses_best_title_or_slug_signal()
    {
        var candidate = new DiscoverySearchScoring.SearchMatchCandidate(
            DiscoverySearchKinds.Release,
            "Alphabet",
            ["alpha-song"],
            IsVerified: true);

        Assert.Equal(
            DiscoverySearchScoring.PrefixTitleScore,
            DiscoverySearchScoring.ComputeMatchScore("alpha", candidate));
    }

    [Fact]
    public void ApplyUnverifiedNerf_reduces_score_for_unverified_items()
    {
        var nerfed = DiscoverySearchScoring.ApplyUnverifiedNerf(
            DiscoverySearchScoring.SubstringTitleScore,
            isVerified: false,
            allowUnverifiedArtists: false);

        Assert.Equal(DiscoverySearchScoring.SubstringTitleScore - DiscoverySearchScoring.FullNerf, nerfed);
    }

    [Fact]
    public void ApplyUnverifiedNerf_uses_reduced_nerf_for_high_matches()
    {
        var nerfed = DiscoverySearchScoring.ApplyUnverifiedNerf(
            DiscoverySearchScoring.ExactTitleScore,
            isVerified: false,
            allowUnverifiedArtists: false);

        Assert.Equal(DiscoverySearchScoring.ExactTitleScore - DiscoverySearchScoring.ReducedNerf, nerfed);
    }

    [Fact]
    public void ApplyUnverifiedNerf_skips_penalty_when_preference_enabled()
    {
        var score = DiscoverySearchScoring.ApplyUnverifiedNerf(
            DiscoverySearchScoring.SubstringTitleScore,
            isVerified: false,
            allowUnverifiedArtists: true);

        Assert.Equal(DiscoverySearchScoring.SubstringTitleScore, score);
    }

    [Fact]
    public void ComputeFinalScore_lets_close_unverified_beat_weak_verified_match()
    {
        var unverifiedExact = DiscoverySearchScoring.ComputeFinalScore(
            "alpha",
            new DiscoverySearchScoring.SearchMatchCandidate(
                DiscoverySearchKinds.Release,
                "Alpha",
                [],
                IsVerified: false),
            allowUnverifiedArtists: false);

        var verifiedSubstring = DiscoverySearchScoring.ComputeFinalScore(
            "alpha",
            new DiscoverySearchScoring.SearchMatchCandidate(
                DiscoverySearchKinds.Release,
                "My Alpha Song",
                [],
                IsVerified: true),
            allowUnverifiedArtists: false);

        Assert.True(unverifiedExact > verifiedSubstring);
    }
}
