using Amuse.Worker.Transcoder;
using Xunit;

namespace Amuse.Worker.Transcoder.Tests;

public sealed class AudioProbeJsonParserTests
{
    [Fact]
    public void ParseDurationSeconds_reads_format_duration()
    {
        const string json = """
            {
              "format": {
                "duration": "183.456"
              }
            }
            """;

        var seconds = AudioProbeJsonParser.ParseDurationSeconds(json);

        Assert.Equal(183.456, seconds, precision: 3);
    }

    [Fact]
    public void ParseDurationSeconds_throws_when_duration_missing()
    {
        const string json = """{ "format": {} }""";

        Assert.Throws<InvalidOperationException>(() => AudioProbeJsonParser.ParseDurationSeconds(json));
    }
}
