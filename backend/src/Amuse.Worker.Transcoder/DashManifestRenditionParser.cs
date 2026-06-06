using System.Xml.Linq;
using Amuse.Domain.Catalog;

namespace Amuse.Worker.Transcoder;

public static class DashManifestRenditionParser
{
    public sealed record ParsedRendition(
        string RepresentationId,
        string AdaptationSetId,
        AudioCodec Codec,
        int? BitrateKbps,
        int SampleRateHz,
        int Bandwidth);

    public static IReadOnlyList<ParsedRendition> Parse(string manifestXml)
    {
        var doc = XDocument.Parse(manifestXml);
        XNamespace ns = doc.Root?.Name.Namespace ?? "urn:mpeg:dash:schema:mpd:2011";

        var results = new List<ParsedRendition>();

        foreach (var adaptationSet in doc.Descendants(ns + "AdaptationSet"))
        {
            var adaptationSetId = (string?)adaptationSet.Attribute("id") ?? string.Empty;
            if (ResolveCodec(adaptationSet, ns) is not { } resolvedCodec) continue;

            foreach (var representation in adaptationSet.Elements(ns + "Representation"))
            {
                var representationId = (string?)representation.Attribute("id") ?? string.Empty;
                var bandwidth = ParseInt(representation.Attribute("bandwidth")?.Value) ?? 0;
                var sampleRate = ParseInt(representation.Attribute("audioSamplingRate")?.Value) ?? 48_000;
                var bitrateKbps = resolvedCodec == AudioCodec.Flac
                    ? (int?)null
                    : bandwidth > 0
                        ? bandwidth / 1000
                        : null;

                results.Add(
                    new ParsedRendition(
                        representationId,
                        MapAdaptationSetId(adaptationSetId, resolvedCodec),
                        resolvedCodec,
                        bitrateKbps,
                        sampleRate,
                        bandwidth));
            }
        }

        return results;
    }

    private static AudioCodec? ResolveCodec(XElement adaptationSet, XNamespace ns)
    {
        var codecs = (string?)adaptationSet.Attribute("codecs")
            ?? adaptationSet.Elements(ns + "Representation").FirstOrDefault()?.Attribute("codecs")?.Value
            ?? string.Empty;

        if (codecs.Contains("flac", StringComparison.OrdinalIgnoreCase))
            return AudioCodec.Flac;
        if (codecs.Contains("opus", StringComparison.OrdinalIgnoreCase))
            return AudioCodec.Opus;
        if (codecs.Contains("mp4a", StringComparison.OrdinalIgnoreCase)
            || codecs.Contains("aac", StringComparison.OrdinalIgnoreCase))
            return AudioCodec.Aac;

        var mime = (string?)adaptationSet.Attribute("mimeType") ?? string.Empty;
        if (mime.Contains("flac", StringComparison.OrdinalIgnoreCase))
            return AudioCodec.Flac;
        if (mime.Contains("opus", StringComparison.OrdinalIgnoreCase))
            return AudioCodec.Opus;
        if (mime.Contains("mp4", StringComparison.OrdinalIgnoreCase))
            return AudioCodec.Aac;

        return null;
    }

    private static string MapAdaptationSetId(string adaptationSetId, AudioCodec codec) =>
        codec switch
        {
            AudioCodec.Flac => "flac",
            AudioCodec.Opus => "opus",
            AudioCodec.Aac => "aac",
            _ => adaptationSetId,
        };

    private static int? ParseInt(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;
}
