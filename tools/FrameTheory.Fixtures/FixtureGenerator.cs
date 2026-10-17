using System.Text;

namespace FrameTheory.Fixtures;

public sealed record FixtureSet(string DirectoryPath, IReadOnlyList<string> Files);

public static class FixtureGenerator
{
    public static FixtureSet Generate(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        var files = new List<string>
        {
            GenerateColorChart(directoryPath),
            GenerateTone(directoryPath),
            GenerateMotionCounter(directoryPath)
        };
        return new FixtureSet(directoryPath, files);
    }

    private static string GenerateColorChart(string directoryPath)
    {
        const int width = 8;
        const int height = 4;
        var path = Path.Combine(directoryPath, "test_chart.ppm");
        using var stream = File.Create(path);
        var header = Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        stream.Write(header);
        for (var pixelIndex = 0; pixelIndex < width * height; pixelIndex++)
        {
            var red = (byte)((pixelIndex * 37) % 256);
            var green = (byte)((pixelIndex * 73) % 256);
            var blue = (byte)((pixelIndex * 109) % 256);
            stream.Write([red, green, blue]);
        }

        return path;
    }

    private static string GenerateTone(string directoryPath)
    {
        const int sampleRate = 48000;
        const int durationSeconds = 1;
        const short channels = 1;
        const short bitsPerSample = 16;
        var sampleCount = sampleRate * durationSeconds;
        var dataLength = sampleCount * channels * (bitsPerSample / 8);
        var path = Path.Combine(directoryPath, "tone_1khz.wav");
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * (bitsPerSample / 8));
        writer.Write((short)(channels * (bitsPerSample / 8)));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            var phase = 2 * Math.PI * 1000 * sampleIndex / sampleRate;
            writer.Write((short)(Math.Sin(phase) * short.MaxValue * 0.25));
        }

        return path;
    }

    private static string GenerateMotionCounter(string directoryPath)
    {
        const int frameCount = 90;
        var path = Path.Combine(directoryPath, "motion_counter.csv");
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("frame,time_seconds");
        for (var frame = 0; frame < frameCount; frame++)
        {
            writer.WriteLine($"{frame},{frame / 30d:0.000000}");
        }

        return path;
    }
}
