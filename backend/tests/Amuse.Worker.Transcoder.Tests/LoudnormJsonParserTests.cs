using Amuse.Worker.Transcoder;
using Xunit;

namespace Amuse.Worker.Transcoder.Tests;

public sealed class LoudnormJsonParserTests
{
    private const string SampleStdout = """
        {
            "input_i" : "-27.61",
            "input_tp" : "-9.05",
            "input_lra" : "8.40",
            "input_thresh" : "-38.10",
            "output_i" : "-16.00",
            "output_tp" : "-1.50",
            "output_lra" : "11.00",
            "output_thresh" : "-27.71",
            "normalization_type" : "dynamic",
            "target_offset" : "0.49"
        }
        """;

    private const string SampleStderr = """
        [Parsed_loudnorm_0 @ 0x00007f8c1c0f0c00]
        """ + SampleStdout;

    [Fact]
    public void Parse_reads_loudnorm_json_from_stdout()
    {
        var result = LoudnormJsonParser.Parse(SampleStdout, string.Empty);

        Assert.Equal(-27.61, result.IntegratedLufs, precision: 2);
        Assert.Equal(-9.05, result.TruePeakDbtp, precision: 2);
        Assert.Equal(8.40, result.LoudnessRangeLu, precision: 2);
        Assert.Equal(-38.10, result.ThresholdLufs, precision: 2);
        Assert.Equal("dynamic", result.NormalizationType);
    }

    [Fact]
    public void Parse_reads_loudnorm_json_from_stderr_when_stdout_empty()
    {
        var result = LoudnormJsonParser.Parse(string.Empty, SampleStderr);

        Assert.Equal(-27.61, result.IntegratedLufs, precision: 2);
    }

    [Fact]
    public void Parse_uses_last_json_block_when_multiple_present()
    {
        var stdout = """
            {"input_i":"-99.00","input_tp":"-50.00","input_lra":"1.00","input_thresh":"-70.00","normalization_type":"linear"}
            """ + SampleStderr;

        var result = LoudnormJsonParser.Parse(stdout, string.Empty);

        Assert.Equal(-27.61, result.IntegratedLufs, precision: 2);
    }

    [Fact]
    public void Parse_throws_when_json_missing()
    {
        Assert.Throws<InvalidOperationException>(() => LoudnormJsonParser.Parse("ffmpeg finished", string.Empty));
    }
}
