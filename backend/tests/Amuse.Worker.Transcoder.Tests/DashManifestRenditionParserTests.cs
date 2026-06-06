using Amuse.Domain.Catalog;
using Amuse.Worker.Transcoder;
using Xunit;

namespace Amuse.Worker.Transcoder.Tests;

public sealed class DashManifestRenditionParserTests
{
    [Fact]
    public void Parse_extracts_flac_opus_and_aac_representations()
    {
        const string manifest = """
            <?xml version="1.0" encoding="utf-8"?>
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011">
              <Period>
                <AdaptationSet id="0" codecs="flac" mimeType="audio/mp4">
                  <Representation id="0" bandwidth="800000" audioSamplingRate="48000"/>
                </AdaptationSet>
                <AdaptationSet id="1" codecs="opus" mimeType="audio/mp4">
                  <Representation id="1" bandwidth="64000" audioSamplingRate="48000"/>
                  <Representation id="2" bandwidth="128000" audioSamplingRate="48000"/>
                </AdaptationSet>
                <AdaptationSet id="2" codecs="mp4a.40.2" mimeType="audio/mp4">
                  <Representation id="4" bandwidth="96000" audioSamplingRate="48000"/>
                </AdaptationSet>
              </Period>
            </MPD>
            """;

        var parsed = DashManifestRenditionParser.Parse(manifest);

        Assert.Equal(4, parsed.Count);
        Assert.Contains(parsed, r => r.Codec == AudioCodec.Flac && r.AdaptationSetId == "flac");
        Assert.Equal(2, parsed.Count(r => r.Codec == AudioCodec.Opus));
        Assert.Single(parsed, r => r.Codec == AudioCodec.Aac && r.BitrateKbps == 96);
    }
}
