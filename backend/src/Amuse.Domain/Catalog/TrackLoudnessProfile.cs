namespace Amuse.Domain.Catalog;

/// <summary>
/// Loudness analysis from the audio master (EBU R128 / ffmpeg loudnorm pass 1).
/// <see cref="LinearGainLu"/> is the TP-capped gain clients apply when normalization is enabled.
/// </summary>
public sealed class TrackLoudnessProfile
{
    public const double DefaultTargetIntegratedLufs = -14.0;
    public const double DefaultTargetTruePeakDbtp = -1.0;

    public double IntegratedLufs { get; }
    public double TruePeakDbtp { get; }
    public double LoudnessRangeLu { get; }
    public double ThresholdLufs { get; }
    public double TargetIntegratedLufs { get; }
    public double TargetTruePeakDbtp { get; }
    public double LinearGainLu { get; }
    public DateTimeOffset AnalyzedAt { get; }

    private TrackLoudnessProfile()
    {
        AnalyzedAt = DateTimeOffset.UtcNow;
    }

    private TrackLoudnessProfile(
        double integratedLufs,
        double truePeakDbtp,
        double loudnessRangeLu,
        double thresholdLufs,
        double targetIntegratedLufs,
        double targetTruePeakDbtp,
        double linearGainLu,
        DateTimeOffset analyzedAt)
    {
        IntegratedLufs = integratedLufs;
        TruePeakDbtp = truePeakDbtp;
        LoudnessRangeLu = loudnessRangeLu;
        ThresholdLufs = thresholdLufs;
        TargetIntegratedLufs = targetIntegratedLufs;
        TargetTruePeakDbtp = targetTruePeakDbtp;
        LinearGainLu = linearGainLu;
        AnalyzedAt = analyzedAt;
    }

    public static TrackLoudnessProfile FromAnalysis(
        double integratedLufs,
        double truePeakDbtp,
        double loudnessRangeLu,
        double thresholdLufs,
        DateTimeOffset analyzedAt,
        double targetIntegratedLufs = DefaultTargetIntegratedLufs,
        double targetTruePeakDbtp = DefaultTargetTruePeakDbtp)
    {
        if (!IsFiniteInRange(integratedLufs, -70.0, 0.0))
            throw new ArgumentOutOfRangeException(nameof(integratedLufs));

        if (!IsFiniteInRange(truePeakDbtp, -99.0, 0.0))
            throw new ArgumentOutOfRangeException(nameof(truePeakDbtp));

        if (!IsFiniteInRange(loudnessRangeLu, 0.0, 99.0))
            throw new ArgumentOutOfRangeException(nameof(loudnessRangeLu));

        if (!IsFiniteInRange(thresholdLufs, -99.0, 0.0))
            throw new ArgumentOutOfRangeException(nameof(thresholdLufs));

        if (!IsFiniteInRange(targetIntegratedLufs, -70.0, -5.0))
            throw new ArgumentOutOfRangeException(nameof(targetIntegratedLufs));

        if (!IsFiniteInRange(targetTruePeakDbtp, -9.0, 0.0))
            throw new ArgumentOutOfRangeException(nameof(targetTruePeakDbtp));

        if (analyzedAt.Offset != TimeSpan.Zero)
            throw new ArgumentException("AnalyzedAt must be UTC with an explicit offset.", nameof(analyzedAt));

        var linearGainLu = ComputeSafeLinearGainLu(
            integratedLufs,
            truePeakDbtp,
            targetIntegratedLufs,
            targetTruePeakDbtp);

        return new TrackLoudnessProfile(
            integratedLufs,
            truePeakDbtp,
            loudnessRangeLu,
            thresholdLufs,
            targetIntegratedLufs,
            targetTruePeakDbtp,
            linearGainLu,
            analyzedAt);
    }

    public static double ComputeSafeLinearGainLu(
        double integratedLufs,
        double truePeakDbtp,
        double targetIntegratedLufs,
        double targetTruePeakDbtp)
    {
        var integratedGainLu = targetIntegratedLufs - integratedLufs;
        var truePeakHeadroomLu = targetTruePeakDbtp - truePeakDbtp;
        return Math.Min(integratedGainLu, truePeakHeadroomLu);
    }

    private static bool IsFiniteInRange(double value, double min, double max) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= min && value <= max;
}
