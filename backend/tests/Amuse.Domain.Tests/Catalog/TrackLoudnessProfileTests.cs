using Amuse.Domain.Catalog;

namespace Amuse.Domain.Tests.Catalog;

public sealed class TrackLoudnessProfileTests
{
    private static readonly DateTimeOffset AnalyzedAt = DateTimeOffset.Parse("2026-06-06T00:00:00+00:00");

    [Fact]
    public void FromAnalysis_boosts_quiet_track_to_target_integrated_loudness()
    {
        var profile = TrackLoudnessProfile.FromAnalysis(
            integratedLufs: -23.0,
            truePeakDbtp: -10.0,
            loudnessRangeLu: 8.0,
            thresholdLufs: -34.0,
            analyzedAt: AnalyzedAt);

        Assert.Equal(9.0, profile.LinearGainLu, precision: 2);
    }

    [Fact]
    public void FromAnalysis_caps_gain_when_true_peak_would_exceed_target()
    {
        var profile = TrackLoudnessProfile.FromAnalysis(
            integratedLufs: -20.0,
            truePeakDbtp: -0.5,
            loudnessRangeLu: 10.0,
            thresholdLufs: -30.0,
            analyzedAt: AnalyzedAt);

        Assert.Equal(-0.5, profile.LinearGainLu, precision: 2);
    }

    [Fact]
    public void FromAnalysis_reduces_loud_track()
    {
        var profile = TrackLoudnessProfile.FromAnalysis(
            integratedLufs: -10.0,
            truePeakDbtp: -2.0,
            loudnessRangeLu: 6.0,
            thresholdLufs: -20.0,
            analyzedAt: AnalyzedAt);

        Assert.Equal(-4.0, profile.LinearGainLu, precision: 2);
    }

    [Fact]
    public void FromAnalysis_accepts_positive_true_peak_from_clipped_master()
    {
        var profile = TrackLoudnessProfile.FromAnalysis(
            integratedLufs: -14.0,
            truePeakDbtp: 2.5,
            loudnessRangeLu: 5.0,
            thresholdLufs: -24.0,
            analyzedAt: AnalyzedAt);

        Assert.Equal(2.5, profile.TruePeakDbtp, precision: 2);
        Assert.Equal(-3.5, profile.LinearGainLu, precision: 2);
    }

    [Fact]
    public void ComputeSafeLinearGainLu_matches_ffmpeg_linear_condition()
    {
        var gain = TrackLoudnessProfile.ComputeSafeLinearGainLu(
            integratedLufs: -27.61,
            truePeakDbtp: -9.05,
            targetIntegratedLufs: -14.0,
            targetTruePeakDbtp: -1.0);

        Assert.Equal(8.05, gain, precision: 2);
    }
}
