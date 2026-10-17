using FrameTheory.Application;
using FrameTheory.Desktop;
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
}
