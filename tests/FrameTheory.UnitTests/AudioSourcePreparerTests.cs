using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class AudioSourcePreparerTests
{
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

    private sealed class StubProcessRunner : IMediaProcessRunner
    {
        public int InvocationCount { get; private set; }

        public string Arguments { get; private set; } = string.Empty;

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            InvocationCount++;
            Arguments = string.Join(' ', arguments);
            File.WriteAllBytes(arguments[^1], [82, 73, 70, 70]);
            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }
}
