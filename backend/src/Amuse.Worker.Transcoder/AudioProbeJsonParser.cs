using System.Globalization;
using System.Text.Json;

namespace Amuse.Worker.Transcoder;

public static class AudioProbeJsonParser
{
    public static double ParseDurationSeconds(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("ffprobe returned empty JSON.");

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("format", out var format)
            || !format.TryGetProperty("duration", out var durationElement))
        {
            throw new InvalidOperationException("ffprobe JSON did not contain format.duration.");
        }

        var durationText = durationElement.GetString();
        if (string.IsNullOrWhiteSpace(durationText)
            || !double.TryParse(durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || seconds <= 0
            || !double.IsFinite(seconds))
        {
            throw new InvalidOperationException($"ffprobe returned invalid duration '{durationText}'.");
        }

        return seconds;
    }
}
