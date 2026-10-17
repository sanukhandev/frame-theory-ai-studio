using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.Application;
using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Storage;

namespace FrameTheory.Desktop;

public sealed record WorkspaceViewModel(string Name, string Description, string OfflineMessage);

public sealed record ImportedMediaViewModel(string Name, string Summary, byte[]? ThumbnailPpm = null)
{
    public override string ToString() => $"{Name} — {Summary}";
}

public interface IWorkspaceCatalog
{
    IReadOnlyList<WorkspaceViewModel> GetWorkspaces();
}

public sealed class WorkspaceCatalog : IWorkspaceCatalog
{
    public IReadOnlyList<WorkspaceViewModel> GetWorkspaces() =>
    [
        new("Video", "Frame-accurate editing, playback and export.", "Manual editing remains available offline."),
        new("Photo", "Non-destructive raster editing and image export.", "Local media stays on this device."),
        new("Color", "Shared cinematic grades for images and video.", "Color operations do not require Gemini."),
        new("AI", "Optional Gemini planning with review before apply.", "No key or network is required for manual editing.")
    ];
}

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ICapabilityDetector _capabilityDetector;
    private readonly IMediaImporter? _mediaImporter;
    private int _selectedWorkspaceIndex;
    private string _capabilityStatusText = "Checking local capabilities...";
    private string _mediaImportStatusText = "Drop local media files into the window to import previews.";

    public MainWindowViewModel(
        IWorkspaceCatalog workspaceCatalog,
        ICapabilityDetector capabilityDetector,
        IMediaImporter? mediaImporter = null,
        PhotoRecoveryViewModel? photoRecovery = null,
        ColorGradeInspectorViewModel? colorInspector = null,
        TimelineEditorViewModel? timelineEditor = null)
    {
        Workspaces = workspaceCatalog.GetWorkspaces();
        _capabilityDetector = capabilityDetector;
        _mediaImporter = mediaImporter;
        PhotoRecovery = photoRecovery ?? new PhotoRecoveryViewModel(
            new ProjectRecoveryService(new JsonProjectRepository()),
            new JsonProjectRepository());
        ColorInspector = colorInspector ?? new ColorGradeInspectorViewModel();
        TimelineEditor = timelineEditor ?? new TimelineEditorViewModel();
        if (Workspaces.Count == 0)
        {
            throw new InvalidOperationException("At least one workspace is required.");
        }
    }

    public IReadOnlyList<WorkspaceViewModel> Workspaces { get; }

    public IReadOnlyList<ImportedMediaViewModel> ImportedMedia { get; private set; } = [];

    public PhotoRecoveryViewModel PhotoRecovery { get; }

    public ColorGradeInspectorViewModel ColorInspector { get; }

    public TimelineEditorViewModel TimelineEditor { get; }

    public int SelectedWorkspaceIndex
    {
        get => _selectedWorkspaceIndex;
        set
        {
            if (value < 0 || value >= Workspaces.Count || value == _selectedWorkspaceIndex)
            {
                return;
            }

            _selectedWorkspaceIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedWorkspace));
        }
    }

    public WorkspaceViewModel SelectedWorkspace => Workspaces[SelectedWorkspaceIndex];

    public string CapabilityStatusText
    {
        get => _capabilityStatusText;
        private set
        {
            if (value == _capabilityStatusText)
            {
                return;
            }

            _capabilityStatusText = value;
            OnPropertyChanged();
        }
    }

    public string MediaImportStatusText
    {
        get => _mediaImportStatusText;
        private set
        {
            if (value == _mediaImportStatusText)
            {
                return;
            }

            _mediaImportStatusText = value;
            OnPropertyChanged();
        }
    }

    public async Task ImportFilesAsync(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (_mediaImporter is null)
        {
            MediaImportStatusText = "Media import is unavailable.";
            return;
        }

        var imported = new List<ImportedMediaViewModel>();
        var failures = new List<string>();
        foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var fullPath = Path.GetFullPath(path);
                var media = await _mediaImporter.ImportAsync(new Uri(fullPath), cancellationToken);
                imported.Add(new ImportedMediaViewModel(
                    Path.GetFileName(fullPath),
                    $"{media.Metadata.Kind}; {media.Metadata.Width}x{media.Metadata.Height}; thumbnail {media.ThumbnailPpm?.Length ?? 0} bytes; waveform {media.WaveformPeaks.Count} peaks",
                    media.ThumbnailPpm));
                if (media.ThumbnailPpm is not null)
                {
                    ColorInspector.SetSource(media.ThumbnailPpm);
                }
                TimelineEditor.AddMedia(media.Metadata);
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or MediaImportException or MediaProbeException or MediaDecodeException)
            {
                failures.Add($"{Path.GetFileName(path)} ({exception.Message})");
            }
        }

        ImportedMedia = imported;
        OnPropertyChanged(nameof(ImportedMedia));
        MediaImportStatusText = imported.Count == 0 && failures.Count == 0
            ? "No local media files were dropped."
            : $"Imported {imported.Count} file(s)." + (failures.Count == 0 ? string.Empty : $" Failed: {string.Join(", ", failures)}");
    }

    public async Task RefreshCapabilitiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var report = await Task.Run(() => _capabilityDetector.DetectAsync(cancellationToken), cancellationToken);
            CapabilityStatusText = report.Summary;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CapabilityStatusText = "Capability check canceled.";
        }
        catch (Exception exception)
        {
            CapabilityStatusText = $"Capability check failed: {exception.GetType().Name}.";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DesktopServices
{
    public MainWindowViewModel CreateMainWindowViewModel()
    {
        var processRunner = new FfmpegProcessRunner();
        var importer = new FfmpegMediaImporter(
            new FfprobeMediaProbe(processRunner),
            new FfmpegFrameDecoder(processRunner),
            processRunner);
        var repository = new JsonProjectRepository();
        return new(
            new WorkspaceCatalog(),
            new FrameTheory.Platform.SystemCapabilityDetector(),
            importer,
            new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), repository));
    }
}
