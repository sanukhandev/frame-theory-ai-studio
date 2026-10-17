using System.Reflection;
using FrameTheory.Application;

namespace FrameTheory.UnitTests;

public sealed class FoundationTests
{
    [Xunit.Fact]
    public void FoundationAssembliesAreLoadable()
    {
        var expected = new[]
        {
            "FrameTheory.Application",
            "FrameTheory.AI",
            "FrameTheory.Captions",
            "FrameTheory.Color",
            "FrameTheory.Domain",
            "FrameTheory.Media",
            "FrameTheory.Photo",
            "FrameTheory.Platform",
            "FrameTheory.Storage"
        };

        var loaded = expected.Select(Assembly.Load).Select(assembly => assembly.GetName().Name).ToArray();

        Xunit.Assert.Equal(expected, loaded);
    }

    [Xunit.Fact]
    public void DesktopShellProvidesAllRequiredWorkspaces()
    {
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector());

        Xunit.Assert.Equal(new[] { "Video", "Photo", "Color", "AI" }, viewModel.Workspaces.Select(workspace => workspace.Name));
        Xunit.Assert.Equal("Video", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = 3;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = -1;
        viewModel.SelectedWorkspaceIndex = viewModel.Workspaces.Count;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);

        viewModel.SelectedWorkspaceIndex = -1;
        viewModel.SelectedWorkspaceIndex = viewModel.Workspaces.Count;

        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);
    }

    [Xunit.Fact]
    public async Task CapabilityDetectorReportsHostAndHonorsCancellation()
    {
        var detector = new FrameTheory.Platform.SystemCapabilityDetector();
        var report = await detector.DetectAsync(CancellationToken.None);

        Xunit.Assert.NotEmpty(report.Platform);
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "FFmpeg");
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "ffprobe");
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "Storage");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Xunit.Assert.ThrowsAsync<OperationCanceledException>(() => detector.DetectAsync(cancellation.Token));
    }

    [Xunit.Fact]
    public async Task MediaProcessRunnerPassesArgumentsWithoutShellParsing()
    {
        var runner = new FrameTheory.Media.FfmpegProcessRunner();
        var result = await runner.RunAsync("dotnet", ["--version"], CancellationToken.None);

        Xunit.Assert.Equal(0, result.ExitCode);
        Xunit.Assert.Contains("10.0", result.StandardOutput);
    }

    [Xunit.Fact]
    public async Task MediaProcessRunnerHonorsPreCanceledWork()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var runner = new FrameTheory.Media.FfmpegProcessRunner();

        await Xunit.Assert.ThrowsAsync<OperationCanceledException>(() =>
            runner.RunAsync("dotnet", ["--version"], cancellation.Token));
    }

    private sealed class TestCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test OS", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }
}
