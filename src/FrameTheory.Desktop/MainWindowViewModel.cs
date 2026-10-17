using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Captions;
using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Platform;
using FrameTheory.Photo;
using FrameTheory.Storage;

namespace FrameTheory.Desktop;

public sealed record WorkspaceViewModel(string Name, string Description, string OfflineMessage);

public enum EditorShortcut
{
    VideoWorkspace,
    PhotoWorkspace,
    ColorWorkspace,
    AiWorkspace,
    PreviousFrame,
    NextFrame,
    UndoAiEdit
}

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
    private readonly IDiagnosticsLog _diagnosticsLog;
    private readonly IMediaImporter? _mediaImporter;
    private int _selectedWorkspaceIndex;
    private string _capabilityStatusText = "Checking local capabilities...";
    private string _mediaImportStatusText = "Drop local media files into the window to import previews.";
    private string _timelineExportStatusText = "Timeline export is available after importing video.";
    private string _captionStatusText = "No captions imported.";
    private string _analysisStatusText = "No local AI analysis prepared.";
    private readonly Dictionary<MediaId, Uri> _mediaSources = [];
    private readonly Dictionary<MediaId, ImportedMedia> _analysisAssets = [];
    private ITimelineVideoExporter? _timelineExporter;
    private readonly ExportQueueService _exportQueue;
    private ExportQueueJob? _activeExport;
    private readonly ILocalAnalysisSampler? _analysisSampler;
    private int _videoWidth;
    private int _videoHeight;

    public MainWindowViewModel(
        IWorkspaceCatalog workspaceCatalog,
        ICapabilityDetector capabilityDetector,
        IMediaImporter? mediaImporter = null,
        PhotoRecoveryViewModel? photoRecovery = null,
        PhotoEditorViewModel? photoEditor = null,
        ColorGradeInspectorViewModel? colorInspector = null,
        TimelineEditorViewModel? timelineEditor = null,
        ITimelineVideoExporter? timelineExporter = null,
        GeminiSettingsViewModel? geminiSettings = null,
        ILocalAnalysisSampler? analysisSampler = null,
        ExportQueueService? exportQueue = null,
        IDiagnosticsLog? diagnosticsLog = null)
    {
        Workspaces = workspaceCatalog.GetWorkspaces();
        _capabilityDetector = capabilityDetector;
        _diagnosticsLog = diagnosticsLog ?? new FileDiagnosticsLog();
        _mediaImporter = mediaImporter;
        PhotoRecovery = photoRecovery ?? new PhotoRecoveryViewModel(
            new ProjectRecoveryService(new JsonProjectRepository()),
            new JsonProjectRepository());
        PhotoEditor = photoEditor ?? new PhotoEditorViewModel(
            new PhotoImporter(new FfprobeMediaProbe(new FfmpegProcessRunner()), new FfmpegFrameDecoder(new FfmpegProcessRunner())),
            new PhotoExporter(new FfmpegProcessRunner(), new FfprobeMediaProbe(new FfmpegProcessRunner())));
        ColorInspector = colorInspector ?? new ColorGradeInspectorViewModel();
        TimelineEditor = timelineEditor ?? new TimelineEditorViewModel();
        GeminiSettings = geminiSettings ?? new GeminiSettingsViewModel(new GeminiConfigurationService(new OperatingSystemSecretStore()));
        _timelineExporter = timelineExporter;
        _exportQueue = exportQueue ?? new ExportQueueService();
        _analysisSampler = analysisSampler;
        if (Workspaces.Count == 0)
        {
            throw new InvalidOperationException("At least one workspace is required.");
        }
    }

    public IReadOnlyList<WorkspaceViewModel> Workspaces { get; }

    public IReadOnlyList<ImportedMediaViewModel> ImportedMedia { get; private set; } = [];

    public PhotoRecoveryViewModel PhotoRecovery { get; }

    public PhotoEditorViewModel? PhotoEditor { get; }

    public ColorGradeInspectorViewModel ColorInspector { get; }

    public TimelineEditorViewModel TimelineEditor { get; }

    public GeminiSettingsViewModel GeminiSettings { get; }

    public AiProposalViewModel AiProposal { get; } = new();

    public string AnalysisStatusText
    {
        get => _analysisStatusText;
        private set
        {
            if (value == _analysisStatusText)
            {
                return;
            }

            _analysisStatusText = value;
            OnPropertyChanged();
        }
    }

    public LocalMediaAnalysisReport? LastLocalAnalysisReport { get; private set; }

    public void ValidateAiPlan(string json)
    {
        var targetIds = _analysisAssets.Keys.Select(mediaId => mediaId.Value).ToHashSet(StringComparer.Ordinal);
        targetIds.UnionWith(["Video 1", "Audio 1", "title-track", "caption-track", "active-project"]);
        AiProposal.Validate(json, new AiPlanValidationContext("active-project", targetIds, Math.Max(1, TimelineEditor.TotalFrames)));
    }

    public void ApplyApprovedAiPlan()
    {
        var fonts = new Dictionary<string, string>(StringComparer.Ordinal);
        var brandFontPath = Environment.GetEnvironmentVariable("FRAME_THEORY_BRAND_FONT_PATH");
        if (!string.IsNullOrWhiteSpace(brandFontPath) && File.Exists(brandFontPath))
        {
            fonts["brand-sans"] = Path.GetFullPath(brandFontPath);
        }

        AiProposal.ApplyApproved(actions => AiEditPlanExecutor.Execute(actions, TimelineEditor, fonts, new Dictionary<string, FrameTheory.Color.ColorGrade>(StringComparer.Ordinal)));
    }

    public bool UndoLastAiEdit()
    {
        var undone = TimelineEditor.UndoAiEdit();
        AiProposal.SetUndoStatus(undone);
        return undone;
    }

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

    public bool HandleShortcut(EditorShortcut shortcut) => shortcut switch
    {
        EditorShortcut.VideoWorkspace => SelectWorkspace(0),
        EditorShortcut.PhotoWorkspace => SelectWorkspace(1),
        EditorShortcut.ColorWorkspace => SelectWorkspace(2),
        EditorShortcut.AiWorkspace => SelectWorkspace(3),
        EditorShortcut.PreviousFrame when SelectedWorkspaceIndex == 0 => MoveTimelineFrame(-1),
        EditorShortcut.NextFrame when SelectedWorkspaceIndex == 0 => MoveTimelineFrame(1),
        EditorShortcut.UndoAiEdit => UndoLastAiEdit(),
        _ => false
    };

    private bool SelectWorkspace(int index)
    {
        if (index < 0 || index >= Workspaces.Count)
        {
            return false;
        }

        SelectedWorkspaceIndex = index;
        return true;
    }

    private bool MoveTimelineFrame(int delta)
    {
        TimelineEditor.Step(delta);
        return true;
    }

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

    public string DiagnosticsStatusText => $"Local diagnostics: {_diagnosticsLog.LogPath}";

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

    public string TimelineExportStatusText
    {
        get => _timelineExportStatusText;
        private set
        {
            if (value == _timelineExportStatusText)
            {
                return;
            }

            _timelineExportStatusText = value;
            OnPropertyChanged();
        }
    }

    public string CaptionStatusText
    {
        get => _captionStatusText;
        private set
        {
            if (value == _captionStatusText)
            {
                return;
            }

            _captionStatusText = value;
            OnPropertyChanged();
        }
    }

    public async Task ImportFilesAsync(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (_mediaImporter is null)
        {
            _diagnosticsLog.Record("media-import-unavailable");
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
                _mediaSources[media.Metadata.Id] = new Uri(fullPath);
                _analysisAssets[media.Metadata.Id] = media;
                if (media.Metadata.Kind == MediaKind.Video && _videoWidth == 0)
                {
                    _videoWidth = media.Metadata.Width;
                    _videoHeight = media.Metadata.Height;
                }
                if (media.ThumbnailPpm is not null)
                {
                    ColorInspector.SetSource(media.ThumbnailPpm);
                }
                TimelineEditor.AddMedia(media.Metadata);
                TimelineEditor.AddBeatMarkers(media.Metadata.Id, media.WaveformPeaks);
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or MediaImportException or MediaProbeException or MediaDecodeException)
            {
                _diagnosticsLog.Record("media-import-failed", exception);
                failures.Add($"{Path.GetFileName(path)} ({exception.Message})");
            }
        }

        ImportedMedia = imported;
        OnPropertyChanged(nameof(ImportedMedia));
        MediaImportStatusText = imported.Count == 0 && failures.Count == 0
            ? "No local media files were dropped."
            : $"Imported {imported.Count} file(s)." + (failures.Count == 0 ? string.Empty : $" Failed: {string.Join(", ", failures)}");
    }

    public async Task<bool> PrepareLocalAnalysisAsync(GeminiAnalysisScope scopes, bool uploadConsent, CancellationToken cancellationToken)
    {
        if (_analysisSampler is null)
        {
            AnalysisStatusText = "Local analysis is unavailable; manual editing remains available.";
            return false;
        }

        if (_analysisAssets.Count == 0)
        {
            AnalysisStatusText = "Import local media before preparing AI analysis samples.";
            return false;
        }

        try
        {
            var request = new GeminiAnalysisRequest(_analysisAssets.Values.ToArray(), scopes, uploadConsent);
            LastLocalAnalysisReport = await _analysisSampler.PrepareAsync(
                request,
                new Progress<LocalAnalysisProgress>(update => AnalysisStatusText = $"{update.Stage} ({update.Fraction:P0})"),
                cancellationToken);
            OnPropertyChanged(nameof(LastLocalAnalysisReport));
            AnalysisStatusText = LastLocalAnalysisReport.Disclosure;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AnalysisStatusText = "Local AI analysis preparation canceled; no upload occurred.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or MediaDecodeException)
        {
            _diagnosticsLog.Record("local-analysis-failed", exception);
            AnalysisStatusText = $"Local AI analysis preparation failed: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> ExportTimelineAsync(string destinationPath, CancellationToken cancellationToken, SocialAspectRatio? aspectRatio = null)
    {
        if (_timelineExporter is null)
        {
            TimelineExportStatusText = "Timeline export is unavailable.";
            return false;
        }

        try
        {
            var plan = aspectRatio is null
                ? TimelineEditor.BuildRenderPlan(_videoWidth, _videoHeight, _mediaSources)
                : TimelineEditor.BuildRenderPlan(SocialVideoExportPreset.Create(aspectRatio.Value).Width, SocialVideoExportPreset.Create(aspectRatio.Value).Height, _mediaSources);
            var preset = new ExportPreset(plan.Width, plan.Height, plan.FrameRate, includeAudio: plan.AudioLayers.Count > 0);
            var job = _exportQueue.Enqueue(queueCancellation => _timelineExporter.ExportAsync(
                plan,
                destinationPath,
                preset,
                new Progress<ExportProgress>(update => TimelineExportStatusText = $"{update.Stage} ({update.Fraction:P0})"),
                queueCancellation));
            _activeExport = job;
            TimelineExportStatusText = $"Queued export {job.Id:N}.";
            using var cancellationRegistration = cancellationToken.Register(job.Cancel);
            var receipt = await job.Completion.WaitAsync(cancellationToken);
            TimelineExportStatusText = $"Exported {Path.GetFileName(receipt.OutputPath)} ({receipt.OutputBytes} bytes).";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TimelineExportStatusText = "Timeline export canceled.";
            return false;
        }
        catch (OperationCanceledException)
        {
            TimelineExportStatusText = "Timeline export canceled.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or MediaExportException or MediaProbeException)
        {
            _diagnosticsLog.Record("timeline-export-failed", exception);
            TimelineExportStatusText = $"Timeline export failed: {exception.Message}";
            return false;
        }
        finally
        {
            _activeExport = null;
        }
    }

    public void CancelTimelineExport()
    {
        if (_activeExport is null)
        {
            TimelineExportStatusText = "No timeline export is active.";
            return;
        }

        _activeExport.Cancel();
        TimelineExportStatusText = "Timeline export cancellation requested.";
    }

    public async Task<bool> ImportCaptionsAsync(string sourcePath, CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
            TimelineEditor.ImportCaptions(content);
            CaptionStatusText = $"Imported {TimelineEditor.Captions.Count} caption cue(s).";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CaptionStatusText = "Caption import canceled.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
        {
            _diagnosticsLog.Record("caption-import-failed", exception);
            CaptionStatusText = $"Caption import failed: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> ExportCaptionsAsync(string destinationPath, CancellationToken cancellationToken)
    {
        try
        {
            var content = TimelineEditor.ExportCaptions();
            await File.WriteAllTextAsync(destinationPath, content, cancellationToken);
            CaptionStatusText = $"Exported {TimelineEditor.Captions.Count} caption cue(s).";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CaptionStatusText = "Caption export canceled.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            _diagnosticsLog.Record("caption-export-failed", exception);
            CaptionStatusText = $"Caption export failed: {exception.Message}";
            return false;
        }
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
            _diagnosticsLog.Record("capability-check-failed", exception);
            CapabilityStatusText = $"Capability check failed: {exception.GetType().Name}.";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DesktopServices
{
    private readonly IDiagnosticsLog _diagnosticsLog;

    public DesktopServices(IDiagnosticsLog? diagnosticsLog = null) => _diagnosticsLog = diagnosticsLog ?? new FileDiagnosticsLog();

    public MainWindowViewModel CreateMainWindowViewModel()
    {
        var processRunner = new FfmpegProcessRunner();
        var decoder = new FfmpegFrameDecoder(processRunner);
        var importer = new FfmpegMediaImporter(
            new FfprobeMediaProbe(processRunner),
            decoder,
            processRunner);
        var probe = new FfprobeMediaProbe(processRunner);
        var repository = new JsonProjectRepository();
        return new(
            new WorkspaceCatalog(),
            new FrameTheory.Platform.SystemCapabilityDetector(),
            importer,
            new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), repository),
            new PhotoEditorViewModel(new PhotoImporter(probe, decoder), new PhotoExporter(processRunner, probe)),
            timelineExporter: new FfmpegTimelineVideoExporter(processRunner, new FfprobeMediaProbe(processRunner)),
            analysisSampler: new LocalAnalysisSampler(decoder),
            diagnosticsLog: _diagnosticsLog);
    }
}
