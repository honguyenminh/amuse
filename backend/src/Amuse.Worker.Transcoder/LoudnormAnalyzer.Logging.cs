using Microsoft.Extensions.Logging;

namespace Amuse.Worker.Transcoder;

internal sealed partial class LoudnormAnalyzer
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "loudnorm analyze predicted dynamic normalization for track {TrackId} (job {JobId}); storing TP-capped linear gain.")]
    private partial void LogDynamicNormalizationPredicted(Guid trackId, Guid jobId);
}
