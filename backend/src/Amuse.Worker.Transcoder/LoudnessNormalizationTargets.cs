namespace Amuse.Worker.Transcoder;

internal static class LoudnessNormalizationTargets
{
    public const double IntegratedLufs = -14.0;
    public const double TruePeakDbtp = -1.0;
    public const double LoudnessRangeLu = 11.0;
}
