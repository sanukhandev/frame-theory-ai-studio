using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class PreviewProxyTests
{
    [Xunit.Fact]
    public void PreviewProxyRequiresProcessRunner()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => new FfmpegPreviewProxyService(null!, Path.GetTempPath()));
    }

    [Xunit.Fact]
    public void PreviewProxyRequiresCacheDirectory()
    {
        Xunit.Assert.Throws<ArgumentException>(() => new FfmpegPreviewProxyService(new StubProcessRunner(), " "));
    }

    [Xunit.Fact]
    public async Task CreatesAndReusesBoundedProxy()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-proxy-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var runner = new StubProcessRunner();
            var service = new FfmpegPreviewProxyService(runner, Path.Combine(directory.FullName, "cache"));

            var first = await service.CreateAsync(new Uri(sourcePath), "source-v1", 8, 8, CancellationToken.None);
            var second = await service.CreateAsync(new Uri(sourcePath), "source-v1", 8, 8, CancellationToken.None);

            Xunit.Assert.False(first.Reused);
            Xunit.Assert.True(second.Reused);
            Xunit.Assert.Equal(first.OutputPath, second.OutputPath);
            Xunit.Assert.Equal(1, runner.InvocationCount);
            Xunit.Assert.Contains("-vf scale=trunc(min(iw\\,8)/2)*2:trunc(min(ih\\,8)/2)*2:force_original_aspect_ratio=decrease", runner.ArgumentsSeen[0]);
            Xunit.Assert.Contains("-an", runner.ArgumentsSeen[0]);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task EvictsOlderEntriesWithinCacheBudget()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-proxy-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            var cachePath = Path.Combine(directory.FullName, "cache");
            var service = new FfmpegPreviewProxyService(new StubProcessRunner(), cachePath, maximumCacheBytes: 4);

            await service.CreateAsync(new Uri(sourcePath), "source-v1", 8, 8, CancellationToken.None);
            await service.CreateAsync(new Uri(sourcePath), "source-v2", 8, 8, CancellationToken.None);

            Xunit.Assert.Single(Directory.EnumerateFiles(cachePath, "*.mp4"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task SourceReplacementDoesNotReuseOldProxyForSameFingerprint()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-proxy-change-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source-v1");
            var runner = new StubProcessRunner();
            var service = new FfmpegPreviewProxyService(runner, Path.Combine(directory.FullName, "cache"));

            var first = await service.CreateAsync(new Uri(sourcePath), "stable-id", 8, 8, CancellationToken.None);
            await File.WriteAllTextAsync(sourcePath, "source-v2-with-a-different-length");
            var second = await service.CreateAsync(new Uri(sourcePath), "stable-id", 8, 8, CancellationToken.None);

            Xunit.Assert.False(first.Reused);
            Xunit.Assert.False(second.Reused);
            Xunit.Assert.NotEqual(first.OutputPath, second.OutputPath);
            Xunit.Assert.Equal(2, runner.InvocationCount);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task RejectsRemoteSourcesAndPreCanceledWork()
    {
        var service = new FfmpegPreviewProxyService(new StubProcessRunner(), Path.GetTempPath());
        await Xunit.Assert.ThrowsAsync<PreviewProxyException>(() =>
            service.CreateAsync(new Uri("https://example.test/source.mp4"), "source", 8, 8, CancellationToken.None));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var directory = Directory.CreateTempSubdirectory("frame-theory-proxy-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            await File.WriteAllTextAsync(sourcePath, "source");
            await Xunit.Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.CreateAsync(new Uri(sourcePath), "source", 8, 8, cancellation.Token));
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
            cancellationToken.ThrowIfCancellationRequested();
            InvocationCount++;
            ArgumentsSeen.Add(string.Join(' ', arguments));
            File.WriteAllBytes(arguments[^1], "test"u8.ToArray());
            return Task.FromResult(new MediaProcessResult(0, string.Empty, string.Empty));
        }
    }
}
