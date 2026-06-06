using Amuse.Domain.Catalog;
using Microsoft.Extensions.Logging;

namespace Amuse.Worker.Transcoder;

internal sealed class LoudnormAnalyzer(
    Func<string, Guid, Guid, CancellationToken, Task<FfmpegRunResult>> runFfmpegAsync,
    ILogger<LoudnormAnalyzer> logger)
{
    public async Task<TrackLoudnessProfile> AnalyzeAsync(
        string inputUrl,
        Guid trackId,
        Guid jobId,
        DateTimeOffset analyzedAt,
        CancellationToken cancellationToken)
    {
        // loudnorm JSON is logged at AV_LOG_INFO on stderr; -loglevel error suppresses it.
        // stats_file is unavailable on older distro ffmpeg builds, so use info for this pass only.
        var filter =
            $"loudnorm=I={LoudnessNormalizationTargets.IntegratedLufs}" +
            $":TP={LoudnessNormalizationTargets.TruePeakDbtp}" +
            $":LRA={LoudnessNormalizationTargets.LoudnessRangeLu}" +
            ":print_format=json";

        var args =
            $"-hide_banner -loglevel info -y -nostdin -i \"{inputUrl}\" -vn " +
            $"-af \"{filter}\" -f null -";

        var result = await runFfmpegAsync(args, trackId, jobId, cancellationToken);
        var analysis = LoudnormJsonParser.Parse(result.Stdout, result.Stderr);

        if (!string.Equals(analysis.NormalizationType, "linear", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "loudnorm analyze predicted dynamic normalization for track {TrackId} (job {JobId}); storing TP-capped linear gain.",
                trackId,
                jobId);
        }

        return TrackLoudnessProfile.FromAnalysis(
            analysis.IntegratedLufs,
            analysis.TruePeakDbtp,
            analysis.LoudnessRangeLu,
            analysis.ThresholdLufs,
            analyzedAt,
            LoudnessNormalizationTargets.IntegratedLufs,
            LoudnessNormalizationTargets.TruePeakDbtp);
    }
}
