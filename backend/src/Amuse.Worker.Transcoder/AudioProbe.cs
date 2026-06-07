using Amuse.Domain.Catalog;

namespace Amuse.Worker.Transcoder;

internal sealed class AudioProbe(
    Func<string, Guid, Guid, CancellationToken, Task<FfmpegRunResult>> runFfprobeAsync)
{
    public async Task<TrackDuration> ProbeDurationAsync(
        string inputUrl,
        Guid trackId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var args = $"-v error -show_entries format=duration -of json \"{inputUrl}\"";
        var result = await runFfprobeAsync(args, trackId, jobId, cancellationToken);
        var json = string.IsNullOrWhiteSpace(result.Stdout) ? result.Stderr : result.Stdout;
        var seconds = AudioProbeJsonParser.ParseDurationSeconds(json);
        var milliseconds = (int)Math.Round(seconds * 1000, MidpointRounding.AwayFromZero);
        return TrackDuration.FromMilliseconds(milliseconds);
    }
}
