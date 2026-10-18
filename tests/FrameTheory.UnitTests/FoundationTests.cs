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
    public void DesktopKeyboardShortcutsSwitchWorkspacesAndStepTimeline()
    {
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector());

        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.AiWorkspace));
        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.VideoWorkspace));
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.NextFrame));
        Xunit.Assert.Equal(0, viewModel.TimelineEditor.CurrentFrame);
        Xunit.Assert.False(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.UndoAiEdit));
    }

    [Xunit.Fact]
    public void DesktopKeyboardShortcutsCoverEveryWorkspace()
    {
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector());

        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.VideoWorkspace));
        Xunit.Assert.Equal("Video", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.PhotoWorkspace));
        Xunit.Assert.Equal("Photo", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.ColorWorkspace));
        Xunit.Assert.Equal("Color", viewModel.SelectedWorkspace.Name);
        Xunit.Assert.True(viewModel.HandleShortcut(FrameTheory.Desktop.EditorShortcut.AiWorkspace));
        Xunit.Assert.Equal("AI", viewModel.SelectedWorkspace.Name);
    }

    [Xunit.Fact]
    public void DesktopWorkspaceSelectionPersistsAndRejectsInvalidSavedState()
    {
        var state = new TestWorkspaceStateStore(99);
        var first = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector(),
            workspaceStateStore: state);

        Xunit.Assert.Equal("Video", first.SelectedWorkspace.Name);

        first.SelectedWorkspaceIndex = 2;

        var reopened = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new TestCapabilityDetector(),
            workspaceStateStore: state);

        Xunit.Assert.Equal("Color", reopened.SelectedWorkspace.Name);
    }

    [Xunit.Fact]
    public async Task FileWorkspaceStateStoreRoundTripsAtomically()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-workspace-state-");
        try
        {
            var path = Path.Combine(directory.FullName, "state.json");
            var store = new FrameTheory.Desktop.FileWorkspaceStateStore(path);

            store.SaveSelectedWorkspaceIndex(3);

            Xunit.Assert.Equal(3, store.LoadSelectedWorkspaceIndex());
            Xunit.Assert.Contains("SelectedWorkspaceIndex", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void CapabilitySummaryProvidesActionableMediaDependencyGuidance()
    {
        var report = new CapabilityReport("Test", "x64",
        [
            new("FFmpeg", CapabilityState.Missing, "Not found on PATH."),
            new("ffprobe", CapabilityState.Missing, "Not found on PATH."),
            new("Storage", CapabilityState.Available, "ready")
        ]);

        Xunit.Assert.Contains("install FFmpeg and add it to PATH", report.Summary, StringComparison.Ordinal);
        Xunit.Assert.Contains("ffprobe: missing", report.Summary, StringComparison.Ordinal);
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
    public async Task CapabilityDetectorReportsNonzeroDependencyProbeAsUnknown()
    {
        var detector = new FrameTheory.Platform.SystemCapabilityDetector(
            "dotnet",
            "dotnet",
            "--frame-theory-invalid-probe",
            gpuExecutable: "dotnet",
            gpuArguments: ["--frame-theory-invalid-gpu-probe"]);

        var report = await detector.DetectAsync(CancellationToken.None);

        Xunit.Assert.All(report.Entries.Where(entry => entry.Name is "FFmpeg" or "ffprobe"), entry =>
        {
            Xunit.Assert.Equal(CapabilityState.Unknown, entry.State);
            Xunit.Assert.Contains("Exited with code", entry.Detail, StringComparison.Ordinal);
        });
        Xunit.Assert.Contains("FFmpeg: unknown (Exited with code", report.Summary, StringComparison.Ordinal);
        Xunit.Assert.Contains(report.Entries, entry => entry.Name == "GPU" && entry.State == CapabilityState.Unknown);
    }

    [Xunit.Fact]
    public async Task CapabilityDetectorUsesGpuProbeWithoutClaimingAcceleration()
    {
        var detector = new FrameTheory.Platform.SystemCapabilityDetector(
            "dotnet",
            "dotnet",
            "--version",
            gpuExecutable: "dotnet",
            gpuArguments: ["--version"]);

        var report = await detector.DetectAsync(CancellationToken.None);
        var gpu = Xunit.Assert.Single(report.Entries, entry => entry.Name == "GPU");

        Xunit.Assert.Equal(CapabilityState.Unknown, gpu.State);
        Xunit.Assert.DoesNotContain("not configured", gpu.Detail, StringComparison.OrdinalIgnoreCase);
        Xunit.Assert.Contains("CPU fallback", gpu.Detail, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public async Task CapabilityFailureIsVisibleAndRecordedLocally()
    {
        var diagnostics = new RecordingDiagnostics();
        var viewModel = new FrameTheory.Desktop.MainWindowViewModel(
            new FrameTheory.Desktop.WorkspaceCatalog(),
            new ThrowingCapabilityDetector(),
            diagnosticsLog: diagnostics);

        await viewModel.RefreshCapabilitiesAsync(CancellationToken.None);

        Xunit.Assert.Contains("Capability check failed", viewModel.CapabilityStatusText, StringComparison.Ordinal);
        Xunit.Assert.Equal("capability-check-failed", diagnostics.EventName);
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

    [Xunit.Fact]
    public void RationalFrameRateMapsVariableRatesWithoutFloatingPointClocks()
    {
        var normalized = new FrameTheory.Domain.RationalFrameRate(60, 2);
        var ntsc = new FrameTheory.Domain.RationalFrameRate(30000, 1001);

        Xunit.Assert.Equal("30/1", normalized.ToString());
        Xunit.Assert.Equal(90, normalized.FrameIndexAt(TimeSpan.FromSeconds(3)));
        Xunit.Assert.Equal(29, ntsc.FrameIndexAt(TimeSpan.FromSeconds(1)));
        Xunit.Assert.Equal(TimeSpan.FromSeconds(3), normalized.TimeAtFrame(90));
    }

    [Xunit.Fact]
    public void DomainFrameAndMediaTypesRejectInvalidBoundsAndRemainStable()
    {
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.RationalFrameRate(0, 1));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.RationalFrameRate(30, 0));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.RationalFrameRate(30, 1).FrameIndexAt(TimeSpan.FromSeconds(-1)));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.FrameRange(10, 0));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTheory.Domain.FrameRange(long.MaxValue, 2));

        var range = new FrameTheory.Domain.FrameRange(10, 5);
        Xunit.Assert.True(range.Contains(10));
        Xunit.Assert.False(range.Contains(15));

        var source = new Uri("file:///media/clip.mov");
        var firstId = FrameTheory.Domain.MediaId.Create(source, "sha256:abc");
        var secondId = FrameTheory.Domain.MediaId.Create(source, "sha256:abc");
        Xunit.Assert.Equal(firstId, secondId);

        Xunit.Assert.Throws<ArgumentException>(() => FrameTheory.Domain.MediaId.Create(new Uri("file:///media/clip.mov"), ""));
        Xunit.Assert.Throws<ArgumentException>(() => new FrameTheory.Domain.MediaSourceMetadata(
            firstId, source, FrameTheory.Domain.MediaKind.Video, 1920, 1080, null, 30));
    }

    private sealed class TestCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test OS", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }

    private sealed class TestWorkspaceStateStore(int selectedWorkspaceIndex) : FrameTheory.Desktop.IWorkspaceStateStore
    {
        private int _selectedWorkspaceIndex = selectedWorkspaceIndex;

        public int LoadSelectedWorkspaceIndex() => _selectedWorkspaceIndex;

        public void SaveSelectedWorkspaceIndex(int index) => _selectedWorkspaceIndex = index;
    }

    private sealed class ThrowingCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) =>
            Task.FromException<CapabilityReport>(new InvalidOperationException("synthetic capability failure"));
    }

    private sealed class RecordingDiagnostics : IDiagnosticsLog
    {
        public string LogPath => "synthetic";

        public string? EventName { get; private set; }

        public void Record(string eventName, Exception? exception = null) => EventName = eventName;
    }
}
