using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class FrameDecoderTests
{
    [Xunit.Fact]
    public async Task DecoderCachesFramesAndEvictsOldestEntries()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-decode-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            var runner = new StubProcessRunner();
            var decoder = new FfmpegFrameDecoder(runner, maximumCacheEntries: 1);
            var uri = new Uri(path);
            var frameRate = new RationalFrameRate(30, 1);

            var first = await decoder.DecodeFrameAsync(uri, frameRate, 1, CancellationToken.None);
            var cached = await decoder.DecodeFrameAsync(uri, frameRate, 1, CancellationToken.None);
            await decoder.DecodeFrameAsync(uri, frameRate, 2, CancellationToken.None);
            await decoder.DecodeFrameAsync(uri, frameRate, 1, CancellationToken.None);

            Xunit.Assert.Equal(first, cached);
            Xunit.Assert.Equal(3, runner.InvocationCount);
            Xunit.Assert.Contains("select=eq(n\\,1)", runner.ArgumentsSeen[0]);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DecoderRejectsInvalidInputsAndPreCanceledWork()
    {
        var decoder = new FfmpegFrameDecoder(new StubProcessRunner());
        await Xunit.Assert.ThrowsAsync<MediaDecodeException>(() =>
            decoder.DecodeFrameAsync(new Uri("https://example.test/clip.mp4"), new RationalFrameRate(30, 1), 0, CancellationToken.None));
        await Xunit.Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            decoder.DecodeFrameAsync(new Uri("file:///missing.mp4"), new RationalFrameRate(30, 1), -1, CancellationToken.None));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var directory = Directory.CreateTempSubdirectory("frame-theory-decode-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                decoder.DecodeFrameAsync(new Uri(path), new RationalFrameRate(30, 1), 0, cancellation.Token));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private sealed class StubProcessRunner : IMediaProcessRunner
    {
        public int InvocationCount { get; private set; }

        public List<string> ArgumentsSeen { get; } = [];

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            InvocationCount++;
            ArgumentsSeen.Add(string.Join(' ', arguments));
            File.WriteAllBytes(arguments[^1], "P6\n1 1\n255\n\0\0\0"u8.ToArray());
            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }
}
