using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class AudioSourcePreparerTests
{
    [Xunit.Fact]
    public void AudioSourcePreparerRequiresProcessRunner()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegAudioSourcePreparer(null!));
    }

    [Xunit.Fact]
    public void PreparedAudioSourceRejectsRemoteUri()
    {
        Xunit.Assert.Throws<ArgumentException>(() => new PreparedAudioSource(new Uri("https://example.test/audio.wav"), null));
    }

    [Xunit.Fact]
    public async Task VideoSourceIsExtractedToOwnedWavAndCleanedUp()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-audio-source-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var runner = new StubProcessRunner();
            var preparer = new FfmpegAudioSourcePreparer(runner);

            await using var prepared = await preparer.PrepareAsync(new Uri(sourcePath), CancellationToken.None);

            Xunit.Assert.EndsWith(".wav", prepared.Source.LocalPath, StringComparison.OrdinalIgnoreCase);
            Xunit.Assert.True(File.Exists(prepared.Source.LocalPath));
            Xunit.Assert.Contains("-map 0:a:0", runner.Arguments);
            Xunit.Assert.Contains("-c:a pcm_s16le", runner.Arguments);
            var extractedPath = prepared.Source.LocalPath;
            await prepared.DisposeAsync();
            Xunit.Assert.False(File.Exists(extractedPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task AudioSourceIsPassedThroughWithoutStartingFfmpeg()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-audio-source-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "clip.wav");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var runner = new StubProcessRunner();
            var prepared = await new FfmpegAudioSourcePreparer(runner).PrepareAsync(new Uri(sourcePath), CancellationToken.None);

            await using (prepared)
            {
                Xunit.Assert.Equal(new Uri(sourcePath), prepared.Source);
                Xunit.Assert.Equal(0, runner.InvocationCount);
            }
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task OversizedExtractedAudioIsRejectedAndDeleted()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-audio-source-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var runner = new StubProcessRunner { OutputLength = 2L * 1024 * 1024 * 1024 + 1 };

            var exception = await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                new FfmpegAudioSourcePreparer(runner).PrepareAsync(new Uri(sourcePath), CancellationToken.None));

            Xunit.Assert.Contains("2 GiB", exception.Message, StringComparison.Ordinal);
            Xunit.Assert.False(File.Exists(runner.OutputPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task CanceledExtractionDeletesPartialOutput()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-audio-source-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var runner = new StubProcessRunner { Failure = new OperationCanceledException() };

            await Xunit.Assert.ThrowsAsync<OperationCanceledException>(() =>
                new FfmpegAudioSourcePreparer(runner).PrepareAsync(new Uri(sourcePath), CancellationToken.None));

            Xunit.Assert.False(File.Exists(runner.OutputPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task UnsupportedContainerIsRejectedBeforeFfmpeg()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-audio-source-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "clip.txt");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var runner = new StubProcessRunner();

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                new FfmpegAudioSourcePreparer(runner).PrepareAsync(new Uri(sourcePath), CancellationToken.None));

            Xunit.Assert.Equal(0, runner.InvocationCount);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DanglingAudioSymlinkIsRejectedBeforeMissingFileFallback()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-audio-source-dangling-");
        try
        {
            var linkedPath = Path.Combine(directory.FullName, "linked.wav");
            File.CreateSymbolicLink(linkedPath, Path.Combine(directory.FullName, "missing.wav"));

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() =>
                new FfmpegAudioSourcePreparer(new StubProcessRunner())
                    .PrepareAsync(new Uri(linkedPath), CancellationToken.None));
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

        public string Arguments { get; private set; } = string.Empty;

        public long OutputLength { get; init; } = 4;

        public Exception? Failure { get; init; }

        public string OutputPath { get; private set; } = string.Empty;

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            InvocationCount++;
            Arguments = string.Join(' ', arguments);
            OutputPath = arguments[^1];
            using var output = File.Create(OutputPath);
            output.SetLength(OutputLength);
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }
}
