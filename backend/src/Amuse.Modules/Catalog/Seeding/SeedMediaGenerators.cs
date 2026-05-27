using System.Buffers.Binary;

namespace Amuse.Modules.Catalog.Seeding;

/// <summary>
/// Hand-rolled BMP and WAV encoders so dev-only catalog seeding can produce deterministic
/// visual + audible content without pulling in image-processing dependencies. Sizes are
/// intentionally tiny so the seed step uploads in well under a second.
/// </summary>
internal static class SeedMediaGenerators
{
    /// <summary>
    /// Returns a 256x256 24-bit BMP with a top-to-bottom gradient. The pair of (HSL) colours
    /// is deterministically derived from <paramref name="seedKey"/>, so every artist/release
    /// gets a stable, distinctive cover and the cover-art theme extractor sees real signal.
    /// </summary>
    public static byte[] GenerateGradientBmp(string seedKey)
    {
        const int width = 256;
        const int height = 256;
        var hue = StableHue(seedKey);

        var top = HslToRgb(hue, 0.65, 0.30);
        var bottom = HslToRgb((hue + 35) % 360, 0.85, 0.62);

        var rowBytes = width * 3;
        var pad = (4 - rowBytes % 4) % 4;
        var rowSize = rowBytes + pad;
        var pixelDataSize = rowSize * height;
        const int headerSize = 14 + 40;
        var fileSize = headerSize + pixelDataSize;

        var buffer = new byte[fileSize];
        var span = buffer.AsSpan();

        // BITMAPFILEHEADER (14 bytes).
        span[0] = (byte)'B';
        span[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(span[2..], fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(span[10..], headerSize);

        // BITMAPINFOHEADER (40 bytes).
        BinaryPrimitives.WriteInt32LittleEndian(span[14..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(span[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(span[22..], height);
        BinaryPrimitives.WriteInt16LittleEndian(span[26..], 1); // planes
        BinaryPrimitives.WriteInt16LittleEndian(span[28..], 24); // bpp
        BinaryPrimitives.WriteInt32LittleEndian(span[34..], pixelDataSize);

        // Pixel data: BMP is bottom-up. Row 0 in the file is the bottom row of the image.
        for (var fileRow = 0; fileRow < height; fileRow++)
        {
            // y is the image-coordinate row (0 = top).
            var y = height - 1 - fileRow;
            var t = y / (double)(height - 1);
            var r = Lerp(top.R, bottom.R, t);
            var g = Lerp(top.G, bottom.G, t);
            var b = Lerp(top.B, bottom.B, t);

            var rowStart = headerSize + fileRow * rowSize;
            for (var x = 0; x < width; x++)
            {
                var p = rowStart + x * 3;
                buffer[p] = b;
                buffer[p + 1] = g;
                buffer[p + 2] = r;
            }
        }

        return buffer;
    }

    /// <summary>
    /// Returns a 16-bit PCM WAV (mono, 22050 Hz) of a sine wave at <paramref name="frequencyHz"/>
    /// for <paramref name="durationSeconds"/>. Amplitude is gently shaped with linear fade-in/out
    /// to avoid speaker pops.
    /// </summary>
    public static byte[] GenerateSineWaveWav(double frequencyHz, double durationSeconds)
    {
        const int sampleRate = 22050;
        const short bitsPerSample = 16;
        const short channels = 1;

        var sampleCount = (int)Math.Round(sampleRate * durationSeconds);
        var dataBytes = sampleCount * channels * (bitsPerSample / 8);
        var fileSize = 44 + dataBytes;

        var buffer = new byte[fileSize];
        var span = buffer.AsSpan();

        // RIFF header
        WriteAscii(span, 0, "RIFF");
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], fileSize - 8);
        WriteAscii(span, 8, "WAVE");

        // fmt subchunk
        WriteAscii(span, 12, "fmt ");
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16); // PCM chunk size
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1); // PCM format
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], channels);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], sampleRate * channels * bitsPerSample / 8);
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], (short)(channels * bitsPerSample / 8));
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], bitsPerSample);

        // data subchunk
        WriteAscii(span, 36, "data");
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], dataBytes);

        var fadeSamples = Math.Min(sampleCount / 10, sampleRate / 5);
        var twoPiF = 2 * Math.PI * frequencyHz;
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (double)sampleRate;
            var envelope = 1.0;
            if (i < fadeSamples) envelope = i / (double)fadeSamples;
            else if (i > sampleCount - fadeSamples) envelope = (sampleCount - i) / (double)fadeSamples;

            var sample = Math.Sin(twoPiF * t) * envelope * 0.5; // half-scale to avoid clipping
            var pcm = (short)Math.Round(sample * short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(span[(44 + i * 2)..], pcm);
        }

        return buffer;
    }

    private static void WriteAscii(Span<byte> destination, int offset, string ascii)
    {
        for (var i = 0; i < ascii.Length; i++) destination[offset + i] = (byte)ascii[i];
    }

    private static byte Lerp(byte a, byte b, double t) =>
        (byte)Math.Clamp(Math.Round(a + (b - a) * t), 0, 255);

    private static int StableHue(string seed)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var ch in seed)
            {
                hash ^= ch;
                hash *= 16777619u;
            }
            return (int)(hash % 360u);
        }
    }

    private static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
    {
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var hh = h / 60.0;
        var x = c * (1 - Math.Abs(hh % 2 - 1));
        double r, g, b;
        if (hh < 1) (r, g, b) = (c, x, 0);
        else if (hh < 2) (r, g, b) = (x, c, 0);
        else if (hh < 3) (r, g, b) = (0, c, x);
        else if (hh < 4) (r, g, b) = (0, x, c);
        else if (hh < 5) (r, g, b) = (x, 0, c);
        else (r, g, b) = (c, 0, x);
        var m = l - c / 2;
        return (
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }
}
