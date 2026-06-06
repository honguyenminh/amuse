using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Amuse.Worker.Transcoder;

internal sealed record LoudnormAnalysisResult(
    double IntegratedLufs,
    double TruePeakDbtp,
    double LoudnessRangeLu,
    double ThresholdLufs,
    string NormalizationType);

internal static partial class LoudnormJsonParser
{
    public static LoudnormAnalysisResult Parse(string ffmpegStdout, string ffmpegStderr)
    {
        var candidates = new[] { ffmpegStdout, ffmpegStderr, $"{ffmpegStdout}\n{ffmpegStderr}" };
        string? json = null;
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            try
            {
                json = ExtractJsonObject(candidate);
                break;
            }
            catch (InvalidOperationException)
            {
                // Try the next stream.
            }
        }

        if (json is null)
            throw new InvalidOperationException("loudnorm produced no JSON stats output.");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new LoudnormAnalysisResult(
            ReadDouble(root, "input_i"),
            ReadDouble(root, "input_tp"),
            ReadDouble(root, "input_lra"),
            ReadDouble(root, "input_thresh"),
            ReadString(root, "normalization_type"));
    }

    private static string ExtractJsonObject(string stderr)
    {
        var matches = JsonObjectRegex().Matches(stderr);
        if (matches.Count == 0)
            throw new InvalidOperationException("loudnorm JSON block not found in ffmpeg stderr.");

        return matches[^1].Value;
    }

    private static double ReadDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            throw new InvalidOperationException($"loudnorm JSON missing '{propertyName}'.");

        return property.ValueKind switch
        {
            JsonValueKind.String => double.Parse(
                property.GetString()!,
                NumberStyles.Float,
                CultureInfo.InvariantCulture),
            JsonValueKind.Number => property.GetDouble(),
            _ => throw new InvalidOperationException($"loudnorm JSON property '{propertyName}' is not numeric."),
        };
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"loudnorm JSON missing '{propertyName}'.");

        return property.GetString() ?? string.Empty;
    }

    [GeneratedRegex(@"\{[^{}]*\}", RegexOptions.Singleline)]
    private static partial Regex JsonObjectRegex();
}
