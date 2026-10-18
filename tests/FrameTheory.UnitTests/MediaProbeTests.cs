using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class MediaProbeTests
{
    [Xunit.Fact]
    public async Task FfprobeAdapterParsesVideoAudioOrientationAndColorMetadata()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            var runner = new StubProcessRunner("""
                {
                  "streams": [
                    {
                      "codec_type": "video",
                      "width": 1920,
                      "height": 1080,
                      "r_frame_rate": "30000/1001",
                      "nb_frames": "30",
                      "color_primaries": "bt709",
                      "color_transfer": "bt709",
                      "color_space": "bt709",
                      "color_range": "tv",
                      "tags": { "rotate": "90" }
                    },
                    { "codec_type": "audio", "duration": "1.001" }
                  ],
                  "format": { "duration": "1.001" }
                }
                """);
            var probe = new FfprobeMediaProbe(runner, "ffprobe-test");

            var metadata = await probe.ProbeAsync(new Uri(path), CancellationToken.None);

            Xunit.Assert.Equal(MediaKind.Video, metadata.Kind);
            Xunit.Assert.Equal(1920, metadata.Width);
            Xunit.Assert.Equal(new RationalFrameRate(30000, 1001), metadata.FrameRate);
            Xunit.Assert.Equal(30, metadata.DurationFrames);
            Xunit.Assert.Equal(90, metadata.OrientationDegrees);
            Xunit.Assert.Equal("bt709/bt709/bt709/tv", metadata.ColorProfile);
            Xunit.Assert.True(metadata.HasAudio);
            Xunit.Assert.Equal("ffprobe-test", runner.Executable);
            Xunit.Assert.Contains(path, runner.Arguments);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FfprobeAdapterKeepsPositiveSubFrameVideoDurationTimelineSafe()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-subframe-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            var runner = new StubProcessRunner("""
                {
                  "streams": [
                    { "codec_type": "video", "width": 16, "height": 9, "r_frame_rate": "30/1" }
                  ],
                  "format": { "duration": "0.001" }
                }
                """);

            var metadata = await new FfprobeMediaProbe(runner).ProbeAsync(new Uri(path), CancellationToken.None);

            Xunit.Assert.Equal(1, metadata.DurationFrames);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FfprobeAdapterChangesMediaIdentityWhenSamePathContentChanges()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-identity-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "first source");
            var runner = new StubProcessRunner("""
                {
                  "streams": [{ "codec_type": "video", "width": 16, "height": 9, "r_frame_rate": "30/1", "nb_frames": "30" }],
                  "format": { "duration": "1" }
                }
                """);
            var probe = new FfprobeMediaProbe(runner);

            var first = await probe.ProbeAsync(new Uri(path), CancellationToken.None);
            await File.WriteAllTextAsync(path, "replacement source");
            var second = await probe.ProbeAsync(new Uri(path), CancellationToken.None);

            Xunit.Assert.NotEqual(first.Id, second.Id);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FfprobeAdapterRejectsRemoteAndMissingInputs()
    {
        var probe = new FfprobeMediaProbe(new StubProcessRunner("{}"));
        await Xunit.Assert.ThrowsAsync<MediaProbeException>(() =>
            probe.ProbeAsync(new Uri("https://example.test/clip.mp4"), CancellationToken.None));
        await Xunit.Assert.ThrowsAsync<FileNotFoundException>(() =>
            probe.ProbeAsync(new Uri("file:///missing/clip.mp4"), CancellationToken.None));
    }

    [Xunit.Fact]
    public async Task FfprobeAdapterRejectsMediaUnderSymlinkedParent()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-probe-external-");
        try
        {
            var linkedDirectory = Path.Combine(directory.FullName, "media");
            Directory.CreateSymbolicLink(linkedDirectory, externalDirectory.FullName);
            var path = Path.Combine(linkedDirectory, "clip.mp4");
            await File.WriteAllTextAsync(Path.Combine(externalDirectory.FullName, "clip.mp4"), "source");

            await Xunit.Assert.ThrowsAsync<MediaProbeException>(() =>
                new FfprobeMediaProbe(new StubProcessRunner("{}"))
                    .ProbeAsync(new Uri(path), CancellationToken.None));
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
            externalDirectory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FfprobeAdapterRejectsDanglingMediaSymlinkBeforeMissingFileFallback()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-dangling-");
        try
        {
            var linkedPath = Path.Combine(directory.FullName, "linked.mp4");
            File.CreateSymbolicLink(linkedPath, Path.Combine(directory.FullName, "missing.mp4"));

            await Xunit.Assert.ThrowsAsync<MediaProbeException>(() =>
                new FfprobeMediaProbe(new StubProcessRunner("{}"))
                    .ProbeAsync(new Uri(linkedPath), CancellationToken.None));
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

    [Xunit.Fact]
    public async Task FfprobeAdapterClassifiesStillImageExtensionAsImage()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-");
        try
        {
            var path = Path.Combine(directory.FullName, "photo.png");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            var runner = new StubProcessRunner("""
                {
                  "streams": [{ "codec_type": "video", "width": 16, "height": 8, "r_frame_rate": "25/1" }],
                  "format": { "duration": "0.04" }
                }
                """);
            var metadata = await new FfprobeMediaProbe(runner).ProbeAsync(new Uri(path), CancellationToken.None);

            Xunit.Assert.Equal(MediaKind.Image, metadata.Kind);
            Xunit.Assert.Null(metadata.FrameRate);
            Xunit.Assert.Equal(0, metadata.DurationFrames);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FfprobeAdapterIncludesBoundedProcessErrorDetails()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            var runner = new StubProcessRunner(string.Empty, 1, new string('x', 2048));

            var exception = await Xunit.Assert.ThrowsAsync<MediaProbeException>(() =>
                new FfprobeMediaProbe(runner).ProbeAsync(new Uri(path), CancellationToken.None));

            Xunit.Assert.Equal("ffprobe could not read the media file: ".Length + 1024, exception.Message.Length);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FfprobeAdapterKeepsGenericErrorWhenProcessHasNoDetails()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-probe-");
        try
        {
            var path = Path.Combine(directory.FullName, "clip.mp4");
            await File.WriteAllTextAsync(path, "synthetic placeholder");
            var runner = new StubProcessRunner(string.Empty, 1);

            var exception = await Xunit.Assert.ThrowsAsync<MediaProbeException>(() =>
                new FfprobeMediaProbe(runner).ProbeAsync(new Uri(path), CancellationToken.None));

            Xunit.Assert.Equal("ffprobe could not read the media file.", exception.Message);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private sealed class StubProcessRunner(string standardOutput, int exitCode = 0, string standardError = "") : IMediaProcessRunner
    {
        public string? Executable { get; private set; }

        public IReadOnlyList<string> Arguments { get; private set; } = [];

        public Task<MediaProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            Executable = executable;
            Arguments = arguments;
            return Task.FromResult(new MediaProcessResult(exitCode, standardOutput, standardError));
        }
    }
}
