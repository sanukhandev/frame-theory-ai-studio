using FrameTheory.Application;
using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class DesktopImportTests
{
    [Xunit.Fact]
    public async Task CorruptMediaIsReportedInTheImportStatusInsteadOfEscapingTheUi()
    {
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new ThrowingImporter());

        await viewModel.ImportFilesAsync(["corrupt.png"], CancellationToken.None);

        Xunit.Assert.Contains("Failed", viewModel.MediaImportStatusText);
        Xunit.Assert.Contains("corrupt input", viewModel.MediaImportStatusText);
    }

    [Xunit.Fact]
    public async Task TimelinePreviewDecodesTheCurrentVideoSample()
    {
        var decoder = new StubPreviewDecoder();
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new StubCapabilityDetector(),
            new VideoImporter(),
            timelinePreviewDecoder: decoder);

        await viewModel.ImportFilesAsync(["preview.mp4"], CancellationToken.None);
        viewModel.TimelineEditor.SeekFrame(5);

        Xunit.Assert.True(await viewModel.PreviewCurrentTimelineFrameAsync(CancellationToken.None));
        Xunit.Assert.Equal(5, decoder.FrameIndex);
        Xunit.Assert.Equal("P6\n1 1\n255\n\0\0\0"u8.ToArray(), viewModel.TimelinePreviewPpm);
        Xunit.Assert.Contains("frame 5", viewModel.TimelinePreviewStatusText, StringComparison.Ordinal);
    }

    private sealed class StubCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }

    private sealed class ThrowingImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken) =>
            throw new MediaProbeException("corrupt input");
    }

    private sealed class VideoImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            var metadata = new MediaSourceMetadata(
                MediaId.Create(file, "preview-test"),
                file,
                MediaKind.Video,
                16,
                9,
                new RationalFrameRate(30, 1),
                30,
                TimeSpan.FromSeconds(1));
            return Task.FromResult(new ImportedMedia(metadata, null, []));
        }
    }

    private sealed class StubPreviewDecoder : IPreviewFrameDecoder
    {
        public long FrameIndex { get; private set; }

        public Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken)
        {
            FrameIndex = frameIndex;
            return Task.FromResult("P6\n1 1\n255\n\0\0\0"u8.ToArray());
        }
    }
}
