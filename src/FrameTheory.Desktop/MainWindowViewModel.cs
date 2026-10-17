using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Captions;
using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Platform;
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
    private string _timelineExportStatusText = "Timeline export is available after importing video.";
    private string _captionStatusText = "No captions imported.";
    private string _analysisStatusText = "No local AI analysis prepared.";
    private readonly Dictionary<MediaId, Uri> _mediaSources = [];
    private readonly Dictionary<MediaId, ImportedMedia> _analysisAssets = [];
    private ITimelineVideoExporter? _timelineExporter;
    private readonly ILocalAnalysisSampler? _analysisSampler;
    private int _videoWidth;
    private int _videoHeight;

    public MainWindowViewModel(
        IWorkspaceCatalog workspaceCatalog,
        ICapabilityDetector capabilityDetector,
        IMediaImporter? mediaImporter = null,
        PhotoRecoveryViewModel? photoRecovery = null,
        ColorGradeInspectorViewModel? colorInspector = null,
        TimelineEditorViewModel? timelineEditor = null,
        ITimelineVideoExporter? timelineExporter = null,
        GeminiSettingsViewModel? geminiSettings = null,
        ILocalAnalysisSampler? analysisSampler = null)
    {
        Workspaces = workspaceCatalog.GetWorkspaces();
        _capabilityDetector = capabilityDetector;
        _mediaImporter = mediaImporter;
        PhotoRecovery = photoRecovery ?? new PhotoRecoveryViewModel(
            new ProjectRecoveryService(new JsonProjectRepository()),
            new JsonProjectRepository());
        ColorInspector = colorInspector ?? new ColorGradeInspectorViewModel();
        TimelineEditor = timelineEditor ?? new TimelineEditorViewModel();
        GeminiSettings = geminiSettings ?? new GeminiSettingsViewModel(new GeminiConfigurationService(new OperatingSystemSecretStore()));
        _timelineExporter = timelineExporter;
        _analysisSampler = analysisSampler;
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
            var receipt = await _timelineExporter.ExportAsync(
                plan,
                destinationPath,
                preset,
                new Progress<ExportProgress>(update => TimelineExportStatusText = $"{update.Stage} ({update.Fraction:P0})"),
                cancellationToken);
            TimelineExportStatusText = $"Exported {Path.GetFileName(receipt.OutputPath)} ({receipt.OutputBytes} bytes).";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TimelineExportStatusText = "Timeline export canceled.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or MediaExportException or MediaProbeException)
        {
            TimelineExportStatusText = $"Timeline export failed: {exception.Message}";
            return false;
        }
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
        var decoder = new FfmpegFrameDecoder(processRunner);
        var importer = new FfmpegMediaImporter(
            new FfprobeMediaProbe(processRunner),
            decoder,
            processRunner);
        var repository = new JsonProjectRepository();
        return new(
            new WorkspaceCatalog(),
            new FrameTheory.Platform.SystemCapabilityDetector(),
            importer,
            new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), repository),
            timelineExporter: new FfmpegTimelineVideoExporter(processRunner, new FfprobeMediaProbe(processRunner)),
            analysisSampler: new LocalAnalysisSampler(decoder));
    }
}
