using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class FrameDecoderTests
{
    [Xunit.Fact]
    public void DecoderRequiresProcessRunner()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegFrameDecoder(null!));
    }

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
            Xunit.Assert.Equal(1, decoder.MaximumCacheEntries);
            Xunit.Assert.Equal(1, decoder.CacheEntryCount);
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

    [Xunit.Fact]
    public async Task DecoderProvidesSeparateCachedRgbaPreviewFrames()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-decode-");
        try
        {
            var path = Path.Combine(directory.FullName, "photo.png");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            var runner = new StubProcessRunner();
            var decoder = new FfmpegFrameDecoder(runner);
            var uri = new Uri(path);

            var first = await decoder.DecodeRgbaFrameAsync(uri, new RationalFrameRate(1, 1), 0, CancellationToken.None);
            var cached = await decoder.DecodeRgbaFrameAsync(uri, new RationalFrameRate(1, 1), 0, CancellationToken.None);

            Xunit.Assert.Equal(first, cached);
            Xunit.Assert.StartsWith("P7", System.Text.Encoding.ASCII.GetString(first, 0, 2));
            Xunit.Assert.Contains("-pix_fmt rgba", runner.ArgumentsSeen.Single());
            Xunit.Assert.Equal(1, runner.InvocationCount);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DecoderInvalidatesCachedFramesWhenSourceChanges()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-decode-change-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "original");
            var runner = new StubProcessRunner();
            var decoder = new FfmpegFrameDecoder(runner);
            var uri = new Uri(path);

            await decoder.DecodeFrameAsync(uri, new RationalFrameRate(30, 1), 0, CancellationToken.None);
            await File.WriteAllTextAsync(path, "replacement with different length");
            await decoder.DecodeFrameAsync(uri, new RationalFrameRate(30, 1), 0, CancellationToken.None);

            Xunit.Assert.Equal(2, runner.InvocationCount);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DecoderRejectsSymlinkedTemporaryOutputBeforeReadingIt()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-decode-output-link-");
        try
        {
            var targetPath = Path.Combine(directory.FullName, "target.ppm");
            await File.WriteAllBytesAsync(targetPath, "P6\n1 1\n255\n\0\0\0"u8.ToArray());
            var decoder = new FfmpegFrameDecoder(new SymlinkOutputProcessRunner(targetPath));
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");

            await Xunit.Assert.ThrowsAsync<MediaDecodeException>(() =>
                decoder.DecodeFrameAsync(new Uri(sourcePath), new RationalFrameRate(30, 1), 0, CancellationToken.None));
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
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
            var pam = arguments[^1].EndsWith(".pam", StringComparison.OrdinalIgnoreCase);
            File.WriteAllBytes(arguments[^1], pam
                ? [.. "P7\nWIDTH 1\nHEIGHT 1\nDEPTH 4\nMAXVAL 255\nTUPLTYPE RGB_ALPHA\nENDHDR\n"u8, 0, 0, 0, 128]
                : "P6\n1 1\n255\n\0\0\0"u8.ToArray());
            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }

    private sealed class SymlinkOutputProcessRunner(string targetPath) : IMediaProcessRunner
    {
        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            try
            {
                File.CreateSymbolicLink(arguments[^1], targetPath);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (PlatformNotSupportedException)
            {
            }
            catch (IOException)
            {
            }

            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }
}
