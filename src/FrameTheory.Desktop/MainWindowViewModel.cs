using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Captions;
using FrameTheory.Color;
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
    UndoAiEdit,
    UndoCurrentEdit,
    RedoCurrentEdit
}

public sealed record ImportedMediaViewModel(
    string Name,
    string Summary,
    byte[]? ThumbnailPpm = null,
    MediaKind? Kind = null,
    IReadOnlyList<float>? WaveformPeaks = null)
{
    public MediaId? Id { get; init; }

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
    private readonly IWorkspaceStateStore _workspaceStateStore;
    private int _selectedWorkspaceIndex;
    private string _capabilityStatusText = "Checking local capabilities...";
    private string _mediaImportStatusText = "Drop local media files into the window to import previews.";
    private string _timelineExportStatusText = "Timeline export is available after importing video.";
    private string _captionStatusText = "No captions imported.";
    private string _analysisStatusText = "No local AI analysis prepared.";
    private string _geminiUsageStatusText = "No Gemini usage recorded.";
    private string _geminiActivityLogText = "No Gemini activity recorded.";
    private string _referenceAnalysisStatusText = "No Gemini reference grade requested.";
    private ColorGrade? _pendingReferenceGrade;
    private string _roughCutStatusText = "No rough-cut plan prepared.";
    private string _aiRevisionStatusText = "No Gemini revision requested.";
    private string _aiRecipeStatusText = "No AI recipe loaded.";
    private readonly Dictionary<MediaId, Uri> _mediaSources = [];
    private readonly Dictionary<MediaId, ImportedMedia> _analysisAssets = [];
    private ITimelineVideoExporter? _timelineExporter;
    private readonly ExportQueueService _exportQueue;
    private readonly HttpClient _transcriptionHttpClient;
    private readonly IAudioSourcePreparer _audioSourcePreparer;
    private readonly IGeminiInteractionHistory _geminiInteractionHistory;
    private string? _lastGeminiInteractionId;
    private ExportQueueJob? _activeExport;
    private CancellationTokenSource? _activeBatchExport;
    private ExportQueueJob? _lastTimelineExport;
    private TimelineExportRequest? _lastTimelineExportRequest;
    private readonly ILocalAnalysisSampler? _analysisSampler;
    private readonly IPreviewFrameDecoder _timelinePreviewDecoder;
    private readonly ITimelinePreviewRenderer? _timelinePreviewRenderer;
    private readonly FfmpegPreviewProxyService? _timelinePreviewProxy;
    private readonly JsonProjectRepository _timelineProjectRepository = new();
    private readonly ProjectRecoveryService _timelineProjectRecovery;
    private int _videoWidth;
    private int _videoHeight;
    private byte[]? _timelinePreviewPpm;
    private string _timelinePreviewStatusText = "No timeline frame preview requested.";
    private string _motionAnalysisStatusText = "No timeline motion analysis requested.";
    private string _timelineProjectStatusText = "No timeline project opened.";
    private string _projectTitleText = "Untitled project";
    private ProjectSnapshot? _pendingTimelineSnapshot;
    private string? _pendingTimelineProjectPath;
    private MissingMediaViewModel? _selectedMissingTimelineMedia;
    private CancellationTokenSource? _timelinePlaybackCancellation;
    private bool _isTimelinePreviewPlaying;
    private bool _useTimelinePreviewProxy;
    private readonly Queue<double> _timelineDecodeMilliseconds = [];
    private string _timelinePerformanceStatusText = "No timeline performance samples recorded.";

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
        IDiagnosticsLog? diagnosticsLog = null,
        HttpClient? transcriptionHttpClient = null,
        IGeminiInteractionHistory? geminiInteractionHistory = null,
        IAudioSourcePreparer? audioSourcePreparer = null,
        IPreviewFrameDecoder? timelinePreviewDecoder = null,
        FfmpegPreviewProxyService? timelinePreviewProxy = null,
        ITimelinePreviewRenderer? timelinePreviewRenderer = null,
        IWorkspaceStateStore? workspaceStateStore = null)
    {
        Workspaces = workspaceCatalog.GetWorkspaces();
        _workspaceStateStore = workspaceStateStore ?? new NoopWorkspaceStateStore();
        var savedWorkspaceIndex = _workspaceStateStore.LoadSelectedWorkspaceIndex();
        _selectedWorkspaceIndex = savedWorkspaceIndex >= 0 && savedWorkspaceIndex < Workspaces.Count
            ? savedWorkspaceIndex
            : 0;
        _capabilityDetector = capabilityDetector;
        _diagnosticsLog = diagnosticsLog ?? new FileDiagnosticsLog();
        _transcriptionHttpClient = transcriptionHttpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _audioSourcePreparer = audioSourcePreparer ?? new FfmpegAudioSourcePreparer(new FfmpegProcessRunner());
        _geminiInteractionHistory = geminiInteractionHistory ?? new FileGeminiInteractionHistory();
        _mediaImporter = mediaImporter;
        _exportQueue = exportQueue ?? new ExportQueueService();
        PhotoRecovery = photoRecovery ?? new PhotoRecoveryViewModel(
            new ProjectRecoveryService(new JsonProjectRepository()),
            new JsonProjectRepository());
        PhotoEditor = photoEditor ?? new PhotoEditorViewModel(
            new PhotoImporter(new FfprobeMediaProbe(new FfmpegProcessRunner()), new FfmpegFrameDecoder(new FfmpegProcessRunner())),
            new PhotoExporter(new FfmpegProcessRunner(), new FfprobeMediaProbe(new FfmpegProcessRunner())),
            _exportQueue);
        ColorInspector = colorInspector ?? new ColorGradeInspectorViewModel();
        TimelineEditor = timelineEditor ?? new TimelineEditorViewModel();
        GeminiSettings = geminiSettings ?? new GeminiSettingsViewModel(new GeminiConfigurationService(new OperatingSystemSecretStore()));
        if (PhotoEditor is { } photoEditorViewModel)
        {
            photoEditorViewModel.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ActiveWorkspaceStatusText));
        }

        ColorInspector.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ActiveWorkspaceStatusText));
        AiProposal.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ActiveWorkspaceStatusText));
        _timelineExporter = timelineExporter;
        _analysisSampler = analysisSampler;
        _timelinePreviewDecoder = timelinePreviewDecoder ?? new FfmpegFrameDecoder(new FfmpegProcessRunner());
        _timelinePreviewProxy = timelinePreviewProxy;
        _timelinePreviewRenderer = timelinePreviewRenderer;
        _timelineProjectRecovery = new ProjectRecoveryService(_timelineProjectRepository);
        if (Workspaces.Count == 0)
        {
            throw new InvalidOperationException("At least one workspace is required.");
        }
    }

    public IReadOnlyList<WorkspaceViewModel> Workspaces { get; }

    public IReadOnlyList<ImportedMediaViewModel> ImportedMedia { get; private set; } = [];

    public string ImportedMediaSummary => ImportedMedia.Count == 0
        ? "No assets"
        : $"{ImportedMedia.Count} asset{(ImportedMedia.Count == 1 ? string.Empty : "s")}";

    public string ActiveWorkspaceStatusText => SelectedWorkspace.Name switch
    {
        "Video" => $"Project: {TimelineProjectStatusText} · Export: {TimelineExportStatusText}",
        "Photo" => PhotoEditor?.StatusText ?? "Photo editor unavailable.",
        "Color" => ColorInspector.StatusText,
        "AI" => AiProposal.StatusText,
        _ => "Workspace ready."
    };

    public byte[]? TimelinePreviewPpm
    {
        get => _timelinePreviewPpm;
        private set
        {
            _timelinePreviewPpm = value;
            OnPropertyChanged();
        }
    }

    public string TimelinePreviewStatusText
    {
        get => _timelinePreviewStatusText;
        private set
        {
            if (value == _timelinePreviewStatusText)
            {
                return;
            }

            _timelinePreviewStatusText = value;
            OnPropertyChanged();
        }
    }

    public string TimelinePerformanceStatusText
    {
        get => _timelinePerformanceStatusText;
        private set
        {
            if (value == _timelinePerformanceStatusText)
            {
                return;
            }

            _timelinePerformanceStatusText = value;
            OnPropertyChanged();
        }
    }

    public string TimelineProjectStatusText
    {
        get => _timelineProjectStatusText;
        private set
        {
            if (value == _timelineProjectStatusText)
            {
                return;
            }

            _timelineProjectStatusText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveWorkspaceStatusText));
        }
    }

    public string ProjectTitleText
    {
        get => _projectTitleText;
        private set
        {
            if (value == _projectTitleText)
            {
                return;
            }

            _projectTitleText = value;
            OnPropertyChanged();
        }
    }

    public string MotionAnalysisStatusText
    {
        get => _motionAnalysisStatusText;
        private set
        {
            if (value == _motionAnalysisStatusText)
            {
                return;
            }

            _motionAnalysisStatusText = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<MissingMediaViewModel> MissingTimelineMedia { get; private set; } = [];

    public MissingMediaViewModel? SelectedMissingTimelineMedia
    {
        get => _selectedMissingTimelineMedia;
        set
        {
            if (value == _selectedMissingTimelineMedia)
            {
                return;
            }

            _selectedMissingTimelineMedia = value;
            OnPropertyChanged();
        }
    }

    public bool IsTimelinePreviewPlaying
    {
        get => _isTimelinePreviewPlaying;
        private set
        {
            if (value == _isTimelinePreviewPlaying)
            {
                return;
            }

            _isTimelinePreviewPlaying = value;
            OnPropertyChanged();
        }
    }

    public bool UseTimelinePreviewProxy
    {
        get => _useTimelinePreviewProxy;
        set
        {
            if (value == _useTimelinePreviewProxy)
            {
                return;
            }

            _useTimelinePreviewProxy = value;
            OnPropertyChanged();
        }
    }

    public PhotoRecoveryViewModel PhotoRecovery { get; }

    public PhotoEditorViewModel? PhotoEditor { get; }

    public ColorGradeInspectorViewModel ColorInspector { get; }

    public TimelineEditorViewModel TimelineEditor { get; }

    public GeminiSettingsViewModel GeminiSettings { get; }

    public AiProposalViewModel AiProposal { get; } = new();

    public string AiRecipeStatusText
    {
        get => _aiRecipeStatusText;
        private set
        {
            if (value == _aiRecipeStatusText)
            {
                return;
            }

            _aiRecipeStatusText = value;
            OnPropertyChanged();
        }
    }

    public string AiRevisionStatusText
    {
        get => _aiRevisionStatusText;
        private set
        {
            if (value == _aiRevisionStatusText)
            {
                return;
            }

            _aiRevisionStatusText = value;
            OnPropertyChanged();
        }
    }

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

    public string GeminiUsageStatusText
    {
        get => _geminiUsageStatusText;
        private set
        {
            if (value == _geminiUsageStatusText)
            {
                return;
            }

            _geminiUsageStatusText = value;
            OnPropertyChanged();
        }
    }

    public string GeminiActivityLogText
    {
        get => _geminiActivityLogText;
        private set
        {
            if (value == _geminiActivityLogText)
            {
                return;
            }

            _geminiActivityLogText = value;
            OnPropertyChanged();
        }
    }

    public string ReferenceAnalysisStatusText
    {
        get => _referenceAnalysisStatusText;
        private set
        {
            if (value == _referenceAnalysisStatusText)
            {
                return;
            }

            _referenceAnalysisStatusText = value;
            OnPropertyChanged();
        }
    }

    public LocalMediaAnalysisReport? LastLocalAnalysisReport { get; private set; }

    public RoughCutPlan? LastRoughCutPlan { get; private set; }

    public string RoughCutStatusText
    {
        get => _roughCutStatusText;
        private set
        {
            if (value == _roughCutStatusText)
            {
                return;
            }

            _roughCutStatusText = value;
            OnPropertyChanged();
        }
    }

    public bool PrepareRoughCutPlan()
    {
        try
        {
            var videoTrack = TimelineEditor.TrackRows.SingleOrDefault(track => track.Kind == TimelineTrackKind.Video);
            if (videoTrack is null)
            {
                RoughCutStatusText = "Import at least two video clips before planning a rough cut.";
                return false;
            }

            var sources = videoTrack.Clips.Select(clip =>
            {
                var media = _analysisAssets
                    .Where(item => string.Equals(item.Key.Value, clip.MediaId, StringComparison.Ordinal))
                    .Select(item => item.Value)
                    .FirstOrDefault();
                if (media is null)
                {
                    throw new InvalidDataException($"Timeline media '{clip.MediaId}' has no imported analysis metadata.");
                }

                return new RoughCutSource(clip.MediaId, clip.StartFrame, clip.DurationFrames, media.WaveformPeaks);
            }).ToArray();
            LastRoughCutPlan = RoughCutPlanner.Create(sources);
            OnPropertyChanged(nameof(LastRoughCutPlan));
            RoughCutStatusText = LastRoughCutPlan.Summary + " Review before applying.";
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException)
        {
            _diagnosticsLog.Record("rough-cut-plan-failed", exception);
            RoughCutStatusText = $"Rough-cut planning failed: {exception.Message}";
            return false;
        }
    }

    public bool ApplyRoughCutPlan()
    {
        if (LastRoughCutPlan is null)
        {
            RoughCutStatusText = "Prepare a rough-cut plan before applying it.";
            return false;
        }

        try
        {
            TimelineEditor.ApplyRoughCut(LastRoughCutPlan);
            RoughCutStatusText = "Applied the reviewed rough cut as one undoable timeline change.";
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _diagnosticsLog.Record("rough-cut-apply-failed", exception);
            RoughCutStatusText = $"Rough-cut apply failed: {exception.Message}";
            return false;
        }
    }

    public void ValidateAiPlan(string json)
    {
        AiProposal.Validate(json, new AiPlanValidationContext("active-project", BuildAiTargetIds(), Math.Max(1, TimelineEditor.TotalFrames)));
    }

    public async Task<bool> SaveAiRecipeAsync(string path, string name, CancellationToken cancellationToken)
    {
        if (AiProposal.ValidatedJson is null)
        {
            AiRecipeStatusText = "Validate and review an AI plan before saving it as a recipe.";
            return false;
        }

        try
        {
            await new AiRecipeStore().SaveAtomicAsync(path, name, AiProposal.ValidatedJson, cancellationToken);
            AiRecipeStatusText = $"Saved validated recipe {Path.GetFileName(path)}.";
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException)
        {
            _diagnosticsLog.Record("ai-recipe-save-failed", exception);
            AiRecipeStatusText = $"AI recipe save failed: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> LoadAiRecipeAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var recipe = await new AiRecipeStore().LoadAsync(path, cancellationToken);
            ValidateAiPlan(recipe.PlanJson);
            if (AiProposal.ValidatedJson is null)
            {
                AiRecipeStatusText = $"Recipe {recipe.Name} was rejected for this project; no action was loaded.";
                return false;
            }

            AiRecipeStatusText = $"Loaded validated recipe {recipe.Name}; review and approve its actions before applying.";
            return true;
        }
        catch (OperationCanceledException)
        {
            AiRecipeStatusText = "AI recipe load canceled.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or AiPlanFormatException)
        {
            _diagnosticsLog.Record("ai-recipe-load-failed", exception);
            AiRecipeStatusText = $"AI recipe load failed: {exception.Message}";
            return false;
        }
    }

    public void ApplyApprovedAiPlan()
    {
        var fonts = new Dictionary<string, string>(StringComparer.Ordinal);
        var brandFontPath = Environment.GetEnvironmentVariable("FRAME_THEORY_BRAND_FONT_PATH");
        if (!string.IsNullOrWhiteSpace(brandFontPath))
        {
            var fullBrandFontPath = Path.GetFullPath(brandFontPath);
            if (!LocalPathSecurity.ContainsReparsePoint(fullBrandFontPath) && File.Exists(fullBrandFontPath))
            {
                fonts["brand-sans"] = fullBrandFontPath;
            }
        }

        AiProposal.ApplyApproved(actions => AiEditPlanExecutor.Execute(actions, TimelineEditor, fonts, new Dictionary<string, FrameTheory.Color.ColorGrade>(StringComparer.Ordinal)));
    }

    public async Task<bool> RequestAiRevisionAsync(string instruction, string currentPlanJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            AiRevisionStatusText = "Enter a revision request first.";
            return false;
        }

        try
        {
            var apiKey = await GeminiSettings.ReadApiKeyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                AiRevisionStatusText = "Gemini revision requires a stored key; manual proposal validation remains available offline.";
                return false;
            }

            var prompt = string.IsNullOrWhiteSpace(currentPlanJson)
                ? $"Return only a schema v1 JSON edit plan for this request: {instruction.Trim()}"
                : $"Return only a schema v1 JSON edit plan. Revise this current plan according to the request. Current plan: {currentPlanJson.Trim()} Request: {instruction.Trim()}";
            var client = new GeminiInteractionsClient(
                _transcriptionHttpClient,
                new GeminiClientOptions(apiKey, GeminiSettings.SelectedModelId, TimeSpan.FromMinutes(2), 3, TimeSpan.FromSeconds(1)),
                _geminiInteractionHistory,
                _diagnosticsLog);
            AiRevisionStatusText = "Requesting a bounded Gemini revision...";
            var result = await client.CreateFollowUpAsync(prompt, _lastGeminiInteractionId, cancellationToken);
            _lastGeminiInteractionId = result.InteractionId;
            GeminiUsageStatusText = FormatGeminiUsage(result.Usage, result.Attempts);
            RefreshGeminiActivityLog();
            ValidateAiPlan(result.OutputText);
            AiRevisionStatusText = AiProposal.Actions.Count == 0
                ? "Gemini returned a response that failed local plan validation; no mutation occurred."
                : $"Validated Gemini revision {result.InteractionId}; review and approve actions before applying.";
            return AiProposal.Actions.Count > 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RefreshGeminiActivityLog();
            AiRevisionStatusText = "Gemini revision canceled; no mutation occurred.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or HttpRequestException)
        {
            _diagnosticsLog.Record("gemini-revision-failed", exception);
            RefreshGeminiActivityLog();
            AiRevisionStatusText = $"Gemini revision failed: {exception.Message}";
            return false;
        }
    }

    public bool UndoLastAiEdit()
    {
        var undone = TimelineEditor.UndoAiEdit();
        AiProposal.SetUndoStatus(undone);
        return undone;
    }

    public bool UndoCurrentEdit() => SelectedWorkspace.Name switch
    {
        "Photo" => PhotoEditor?.Undo() == true,
        "Video" or "AI" => UndoLastAiEdit(),
        _ => false
    };

    public bool RedoCurrentEdit() => SelectedWorkspace.Name == "Photo" && PhotoEditor?.Redo() == true;

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
            _workspaceStateStore.SaveSelectedWorkspaceIndex(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedWorkspace));
            OnPropertyChanged(nameof(ActiveWorkspaceStatusText));
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
        EditorShortcut.UndoCurrentEdit => UndoCurrentEdit(),
        EditorShortcut.RedoCurrentEdit => RedoCurrentEdit(),
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

    private HashSet<string> BuildAiTargetIds()
    {
        var targetIds = _analysisAssets.Keys.Select(mediaId => mediaId.Value).ToHashSet(StringComparer.Ordinal);
        targetIds.UnionWith(["Video 1", "Audio 1", "title-track", "caption-track", "active-project"]);
        return targetIds;
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
            OnPropertyChanged(nameof(ActiveWorkspaceStatusText));
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

        var imported = ImportedMedia.ToList();
        var newlyImported = 0;
        var failures = new List<string>();
        foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (LocalPathSecurity.ContainsReparsePoint(fullPath))
                {
                    throw new InvalidDataException("Symlinked media imports are not accepted.");
                }

                var media = await _mediaImporter.ImportAsync(new Uri(fullPath), cancellationToken);
                if (imported.All(item => item.Id != media.Metadata.Id))
                {
                    imported.Add(new ImportedMediaViewModel(
                    Path.GetFileName(fullPath),
                    $"{media.Metadata.Kind}; {media.Metadata.Width}x{media.Metadata.Height}; thumbnail {media.ThumbnailPpm?.Length ?? 0} bytes; waveform {media.WaveformPeaks.Count} peaks",
                    media.ThumbnailPpm,
                    media.Metadata.Kind,
                    media.WaveformPeaks)
                    {
                        Id = media.Metadata.Id
                    });
                    newlyImported++;
                }
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
            catch (Exception exception) when (exception is IOException or InvalidDataException or InvalidOperationException or MediaImportException or MediaProbeException or MediaDecodeException)
            {
                _diagnosticsLog.Record("media-import-failed", exception);
                failures.Add($"{Path.GetFileName(path)} ({exception.Message})");
            }
        }

        ImportedMedia = imported;
        OnPropertyChanged(nameof(ImportedMedia));
        OnPropertyChanged(nameof(ImportedMediaSummary));
        MediaImportStatusText = newlyImported == 0 && failures.Count == 0
            ? "No local media files were dropped."
            : $"Imported {newlyImported} file(s); {imported.Count} file(s) available." + (failures.Count == 0 ? string.Empty : $" Failed: {string.Join(", ", failures)}");
    }

    public async Task<bool> PreviewCurrentTimelineFrameAsync(CancellationToken cancellationToken)
    {
        var seek = TimelineEditor.SeekFrame(TimelineEditor.CurrentFrame);
        var sample = seek.Samples.FirstOrDefault(item => item.TrackKind == TimelineTrackKind.Video && item.SourceFrame is not null);
        if (sample is null || sample.SourceFrame is not long sourceFrame ||
            !_mediaSources.TryGetValue(sample.MediaId, out var source) || !_analysisAssets.TryGetValue(sample.MediaId, out var media))
        {
            TimelinePreviewStatusText = "Import a video and place the playhead over a video clip first.";
            TimelinePreviewPpm = null;
            return false;
        }

        try
        {
            var previewSource = source;
            var proxyDescription = string.Empty;
            if (UseTimelinePreviewProxy)
            {
                if (_timelinePreviewProxy is null)
                {
                    TimelinePreviewStatusText = "Preview proxy support is unavailable in this host.";
                    TimelinePreviewPpm = null;
                    return false;
                }

                TimelinePreviewStatusText = $"Preparing preview proxy for {Path.GetFileName(source.LocalPath)}...";
                var proxy = await _timelinePreviewProxy.CreateAsync(
                    source,
                    media.Metadata.Id.Value,
                    Math.Max(2, Math.Min(media.Metadata.Width, 1280)),
                    Math.Max(2, Math.Min(media.Metadata.Height, 720)),
                    cancellationToken);
                previewSource = new Uri(proxy.OutputPath);
                proxyDescription = proxy.Reused ? " using cached preview proxy" : " using preview proxy";
            }

            var timelinePreviewRenderer = _timelinePreviewRenderer;
            var usesTimelineCompositor = timelinePreviewRenderer is not null && !UseTimelinePreviewProxy;
            TimelinePreviewStatusText = usesTimelineCompositor
                ? $"Rendering timeline frame {TimelineEditor.CurrentFrame} with compositor..."
                : $"Decoding frame {sourceFrame}{proxyDescription}...";
            var decodeStarted = Stopwatch.GetTimestamp();
            TimelinePreviewPpm = usesTimelineCompositor
                ? await timelinePreviewRenderer!.RenderFrameAsync(
                    TimelineEditor.BuildRenderPlan(_videoWidth, _videoHeight, _mediaSources),
                    TimelineEditor.CurrentFrame,
                    cancellationToken)
                : await _timelinePreviewDecoder.DecodeFrameAsync(
                    previewSource,
                    media.Metadata.FrameRate ?? new RationalFrameRate(30, 1),
                    sourceFrame,
                    cancellationToken);
            RecordTimelinePreviewPerformance(Stopwatch.GetElapsedTime(decodeStarted).TotalMilliseconds);
            var modeDescription = usesTimelineCompositor
                ? " using timeline compositor"
                : proxyDescription;
            TimelinePreviewStatusText = $"Previewed frame {sourceFrame} from {Path.GetFileName(source.LocalPath)}{modeDescription}.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TimelinePreviewStatusText = "Timeline preview canceled.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or MediaDecodeException or MediaExportException)
        {
            _diagnosticsLog.Record("timeline-preview-failed", exception);
            TimelinePreviewStatusText = $"Timeline preview failed: {exception.Message}";
            TimelinePreviewPpm = null;
            return false;
        }
    }

    public async Task<bool> PlayTimelinePreviewAsync(CancellationToken cancellationToken)
    {
        if (IsTimelinePreviewPlaying)
        {
            return false;
        }

        using var playbackCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timelinePlaybackCancellation = playbackCancellation;
        IsTimelinePreviewPlaying = true;
        var snapEnabled = TimelineEditor.SnapEnabled;
        TimelineEditor.SnapEnabled = false;
        try
        {
            var frameDuration = TimeSpan.FromSeconds(
                (double)TimelineEditor.ProjectFrameRate.Denominator / TimelineEditor.ProjectFrameRate.Numerator);
            while (true)
            {
                playbackCancellation.Token.ThrowIfCancellationRequested();
                if (!await PreviewCurrentTimelineFrameAsync(playbackCancellation.Token))
                {
                    return false;
                }

                if (TimelineEditor.CurrentFrame >= TimelineEditor.TotalFrames - 1)
                {
                    TimelinePreviewStatusText = "Timeline playback finished.";
                    return true;
                }

                await Task.Delay(frameDuration, playbackCancellation.Token);
                TimelineEditor.SeekFrame(TimelineEditor.CurrentFrame + 1);
            }
        }
        catch (OperationCanceledException) when (playbackCancellation.IsCancellationRequested)
        {
            TimelinePreviewStatusText = "Timeline playback canceled.";
            return false;
        }
        finally
        {
            TimelineEditor.SnapEnabled = snapEnabled;
            if (ReferenceEquals(_timelinePlaybackCancellation, playbackCancellation))
            {
                _timelinePlaybackCancellation = null;
            }

            IsTimelinePreviewPlaying = false;
        }
    }

    public void StopTimelinePreview() => _timelinePlaybackCancellation?.Cancel();

    private void RecordTimelinePreviewPerformance(double decodeMilliseconds)
    {
        const int maximumSamples = 32;
        _timelineDecodeMilliseconds.Enqueue(Math.Max(0, decodeMilliseconds));
        while (_timelineDecodeMilliseconds.Count > maximumSamples)
        {
            _timelineDecodeMilliseconds.Dequeue();
        }

        var averageMilliseconds = _timelineDecodeMilliseconds.Average();
        var decodeFps = averageMilliseconds > 0 ? 1000 / averageMilliseconds : 0;
        using var process = Process.GetCurrentProcess();
        TimelinePerformanceStatusText = $"Preview performance: latest_decode_ms={decodeMilliseconds:0.###}; decode_fps={decodeFps:0.###}; working_set_bytes={process.WorkingSet64}; samples={_timelineDecodeMilliseconds.Count}.";
    }

    public async Task<bool> AnalyzeTimelineMotionAsync(CancellationToken cancellationToken)
    {
        var originalFrame = TimelineEditor.CurrentFrame;
        var frames = new List<MotionFrame>();
        try
        {
            var initialSample = TimelineEditor.SeekFrame(originalFrame).Samples
                .FirstOrDefault(item => item.TrackKind == TimelineTrackKind.Video && item.SourceFrame is not null);
            if (initialSample is null || initialSample.SourceFrame is not long ||
                !_mediaSources.TryGetValue(initialSample.MediaId, out var source) ||
                !_analysisAssets.TryGetValue(initialSample.MediaId, out var media) || media.Metadata.FrameRate is not { } frameRate)
            {
                MotionAnalysisStatusText = "Import a video and place the playhead over a video clip first.";
                return false;
            }

            var endFrame = Math.Min(TimelineEditor.TotalFrames, checked(originalFrame + 12));
            for (var timelineFrame = originalFrame; timelineFrame < endFrame; timelineFrame++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sample = TimelineEditor.SeekFrame(timelineFrame).Samples
                    .FirstOrDefault(item => item.TrackKind == TimelineTrackKind.Video && item.SourceFrame is not null);
                if (sample is null || sample.MediaId != initialSample.MediaId || sample.SourceFrame is not long sourceFrame)
                {
                    break;
                }

                var ppm = await _timelinePreviewDecoder.DecodeFrameAsync(source, frameRate, sourceFrame, cancellationToken);
                var raster = PpmRaster.Parse(ppm);
                frames.Add(new MotionFrame(frames.Count, raster.Width, raster.Height, raster.RgbBytes));
            }

            if (frames.Count < 2)
            {
                MotionAnalysisStatusText = "Motion analysis needs at least two decodable frames from one video clip.";
                return false;
            }

            var vectors = new TranslationMotionTracker().Track(frames);
            var plan = StabilizationPlanner.Plan(vectors);
            var meanX = vectors.Average(vector => vector.OffsetX);
            var meanY = vectors.Average(vector => vector.OffsetY);
            var maxCompensation = plan.Transforms
                .Select(transform => Math.Max(Math.Abs(transform.OffsetX), Math.Abs(transform.OffsetY)))
                .DefaultIfEmpty()
                .Max();
            MotionAnalysisStatusText = $"Analyzed {frames.Count} frames; mean translation {meanX:0.##},{meanY:0.##}; max compensation {maxCompensation}px.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            MotionAnalysisStatusText = "Timeline motion analysis canceled.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or InvalidOperationException or MediaDecodeException)
        {
            _diagnosticsLog.Record("timeline-motion-analysis-failed", exception);
            MotionAnalysisStatusText = $"Timeline motion analysis failed: {exception.Message}";
            return false;
        }
        finally
        {
            TimelineEditor.SeekFrame(originalFrame);
        }
    }

    public async Task<bool> SaveTimelineProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(projectPath);
            var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A project directory is required.", nameof(projectPath));
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            var mediaReferences = _mediaSources.Select(item =>
            {
                if (!_analysisAssets.TryGetValue(item.Key, out var media))
                {
                    throw new InvalidDataException($"Imported media {item.Key.Value} has no metadata.");
                }

                var relativePath = Path.GetRelativePath(directory, item.Value.LocalPath);
                relativePath = Path.GetRelativePath(directory, ProjectRecoveryService.ResolveContainedPath(directory, relativePath));
                return new MediaReference(
                    item.Key,
                    relativePath,
                    $"probe:{media.Metadata.SourceUri.AbsoluteUri}");
            }).ToArray();
            var timelineState = TimelineProjectStateJson.Serialize(TimelineEditor.CaptureProjectState());
            var snapshot = new ProjectSnapshot(
                "active-project",
                TimelineEditor.ProjectFrameRate,
                Math.Max(1, _videoWidth),
                Math.Max(1, _videoHeight),
                "sRGB",
                mediaReferences,
                timelineStateJson: timelineState);
            await new JsonProjectRepository().SaveAtomicAsync(fullPath, snapshot, cancellationToken);
            ProjectTitleText = Path.GetFileNameWithoutExtension(fullPath);
            TimelineProjectStatusText = $"Saved timeline project {Path.GetFileName(fullPath)}.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TimelineProjectStatusText = "Timeline project save canceled; the previous file is unchanged.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            _diagnosticsLog.Record("timeline-project-save-failed", exception);
            TimelineProjectStatusText = $"Timeline project could not be saved: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> OpenTimelineProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        if (_mediaImporter is null)
        {
            TimelineProjectStatusText = "Timeline project open is unavailable because media import is unavailable.";
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(projectPath);
            var snapshot = await _timelineProjectRepository.LoadAsync(fullPath, cancellationToken);
            if (snapshot.TimelineStateJson is null)
            {
                throw new InvalidDataException("The project does not contain a saved timeline.");
            }

            var timelineState = TimelineProjectStateJson.Deserialize(snapshot.TimelineStateJson);
            var missing = _timelineProjectRecovery.FindMissingMedia(fullPath, snapshot);
            if (missing.Count > 0)
            {
                _pendingTimelineProjectPath = fullPath;
                _pendingTimelineSnapshot = snapshot;
                SetMissingTimelineMedia(missing);
                TimelineProjectStatusText = $"Timeline project has {missing.Count} missing media file(s); relink them before opening.";
                return false;
            }

            _pendingTimelineProjectPath = null;
            _pendingTimelineSnapshot = null;
            SetMissingTimelineMedia([]);
            var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A project directory is required.", nameof(projectPath));
            var imported = new List<(MediaReference Reference, ImportedMedia Media, Uri Source)>();
            foreach (var reference in snapshot.MediaReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = Path.GetFullPath(Path.Combine(directory, reference.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (LocalPathSecurity.ContainsReparsePoint(sourcePath))
                {
                    throw new InvalidDataException($"Saved media is beneath a symlinked path: {reference.RelativePath}");
                }

                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"Saved media is missing: {reference.RelativePath}", sourcePath);
                }

                var source = new Uri(sourcePath);
                var media = await _mediaImporter.ImportAsync(source, cancellationToken);
                imported.Add((reference, media, source));
            }

            var mediaIdMap = imported.ToDictionary(
                item => item.Reference.Id.Value,
                item => item.Media.Metadata.Id.Value,
                StringComparer.Ordinal);
            TimelineEditor.RestoreProjectState(timelineState, mediaIdMap);

            _mediaSources.Clear();
            _analysisAssets.Clear();
            var importedViewModels = new List<ImportedMediaViewModel>();
            foreach (var item in imported)
            {
                _mediaSources[item.Media.Metadata.Id] = item.Source;
                _analysisAssets[item.Media.Metadata.Id] = item.Media;
                importedViewModels.Add(new ImportedMediaViewModel(
                    Path.GetFileName(item.Source.LocalPath),
                    $"{item.Media.Metadata.Kind}; {item.Media.Metadata.Width}x{item.Media.Metadata.Height}; thumbnail {item.Media.ThumbnailPpm?.Length ?? 0} bytes; waveform {item.Media.WaveformPeaks.Count} peaks",
                    item.Media.ThumbnailPpm,
                    item.Media.Metadata.Kind,
                    item.Media.WaveformPeaks)
                {
                    Id = item.Media.Metadata.Id
                });
            }

            ImportedMedia = importedViewModels;
            OnPropertyChanged(nameof(ImportedMedia));
            OnPropertyChanged(nameof(ImportedMediaSummary));
            var video = imported.Select(item => item.Media.Metadata).FirstOrDefault(metadata => metadata.Kind == MediaKind.Video);
            _videoWidth = video?.Width ?? 0;
            _videoHeight = video?.Height ?? 0;
            if (imported.Select(item => item.Media.ThumbnailPpm).FirstOrDefault(thumbnail => thumbnail is not null) is { } thumbnail)
            {
                ColorInspector.SetSource(thumbnail);
            }

            LastLocalAnalysisReport = null;
            OnPropertyChanged(nameof(LastLocalAnalysisReport));
            TimelinePreviewPpm = null;
            TimelinePreviewStatusText = "Project reopened; preview a current timeline frame to decode it.";
            MediaImportStatusText = $"Reopened {imported.Count} project media file(s).";
            ProjectTitleText = Path.GetFileNameWithoutExtension(fullPath);
            TimelineProjectStatusText = $"Opened timeline project {Path.GetFileName(fullPath)}.";
            _pendingTimelineProjectPath = null;
            _pendingTimelineSnapshot = null;
            SetMissingTimelineMedia([]);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TimelineProjectStatusText = "Timeline project open canceled; the current project remains loaded.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or MediaImportException or MediaProbeException or MediaDecodeException or UnauthorizedAccessException)
        {
            _diagnosticsLog.Record("timeline-project-open-failed", exception);
            TimelineProjectStatusText = $"Timeline project could not be opened: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> RelinkSelectedTimelineMediaAsync(string replacementPath, CancellationToken cancellationToken)
    {
        if (_pendingTimelineProjectPath is null || _pendingTimelineSnapshot is null || SelectedMissingTimelineMedia is null)
        {
            TimelineProjectStatusText = "Open a timeline project with missing media and select a file to relink.";
            return false;
        }

        try
        {
            var relinked = await _timelineProjectRecovery.RelinkMediaAsync(
                _pendingTimelineProjectPath,
                _pendingTimelineSnapshot,
                SelectedMissingTimelineMedia.Id,
                replacementPath,
                cancellationToken);
            await _timelineProjectRepository.SaveAtomicAsync(_pendingTimelineProjectPath, relinked, cancellationToken);
            _pendingTimelineSnapshot = relinked;
            var missing = _timelineProjectRecovery.FindMissingMedia(_pendingTimelineProjectPath, relinked);
            if (missing.Count > 0)
            {
                SetMissingTimelineMedia(missing);
                TimelineProjectStatusText = $"Relinked media; {missing.Count} missing media file(s) remain.";
                return false;
            }

            var projectPath = _pendingTimelineProjectPath;
            _pendingTimelineProjectPath = null;
            _pendingTimelineSnapshot = null;
            SetMissingTimelineMedia([]);
            return await OpenTimelineProjectAsync(projectPath, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TimelineProjectStatusText = "Timeline media relink canceled; the current project remains loaded.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            _diagnosticsLog.Record("timeline-media-relink-failed", exception);
            TimelineProjectStatusText = $"Timeline media could not be relinked: {exception.Message}";
            return false;
        }
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

    public async Task<bool> RequestAiAnalysisAsync(CancellationToken cancellationToken)
    {
        if (LastLocalAnalysisReport is null)
        {
            AnalysisStatusText = "Prepare local analysis samples before sending anything to Gemini.";
            return false;
        }

        if (!LastLocalAnalysisReport.UploadConsent)
        {
            AnalysisStatusText = "Gemini analysis stopped: explicitly approve this exact sample scope first.";
            return false;
        }

        try
        {
            var apiKey = await GeminiSettings.ReadApiKeyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                AnalysisStatusText = "Gemini analysis requires a stored key; manual editing remains available offline.";
                return false;
            }

            var client = new GeminiCreativeDirectorClient(new GeminiInteractionsClient(
                _transcriptionHttpClient,
                new GeminiClientOptions(apiKey, GeminiSettings.SelectedModelId, TimeSpan.FromMinutes(2), 3, TimeSpan.FromSeconds(1)),
                _geminiInteractionHistory,
                _diagnosticsLog));
            AnalysisStatusText = "Uploading the approved visual sample scope to Gemini...";
            var result = await client.CreatePlanAsync(
                LastLocalAnalysisReport,
                BuildAiTargetIds(),
                Math.Max(1, TimelineEditor.TotalFrames),
                cancellationToken);
            GeminiUsageStatusText = FormatGeminiUsage(
                result.Usage,
                result.Attempts,
                result.UploadedSampleCount,
                result.UploadedBytes);
            RefreshGeminiActivityLog();
            ValidateAiPlan(result.PlanJson);
            if (AiProposal.ValidatedJson is null)
            {
                AnalysisStatusText = "Gemini returned a plan rejected by local validation; no mutation occurred.";
                return false;
            }

            _lastGeminiInteractionId = result.InteractionId;
            AnalysisStatusText = $"Gemini analyzed {result.UploadedSampleCount} approved visual sample(s); review {AiProposal.Actions.Count} editable action(s) before applying.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RefreshGeminiActivityLog();
            AnalysisStatusText = "Gemini analysis canceled; no project mutation occurred.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or HttpRequestException)
        {
            _diagnosticsLog.Record("gemini-analysis-failed", exception);
            RefreshGeminiActivityLog();
            AnalysisStatusText = $"Gemini analysis failed: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> AnalyzeColorReferenceAsync(
        ImportedMediaViewModel? sourceItem,
        ImportedMediaViewModel? referenceItem,
        bool uploadConsent,
        CancellationToken cancellationToken)
    {
        if (!TryCreateVisualSample(sourceItem, out var source) || !TryCreateVisualSample(referenceItem, out var reference))
        {
            ReferenceAnalysisStatusText = "Select two imported media previews before requesting a reference grade.";
            return false;
        }

        if (!uploadConsent)
        {
            ReferenceAnalysisStatusText = "Reference matching stopped: explicitly approve uploading the selected source and reference previews first.";
            return false;
        }

        ColorInspector.SetSource(source.Payload);
        try
        {
            var apiKey = await GeminiSettings.ReadApiKeyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ReferenceAnalysisStatusText = "Reference matching requires a stored key; local grading remains available offline.";
                return false;
            }

            var client = new GeminiReferenceAnalysisClient(
                _transcriptionHttpClient,
                new GeminiClientOptions(apiKey, GeminiSettings.SelectedModelId, TimeSpan.FromMinutes(2), 3, TimeSpan.FromSeconds(1)),
                _geminiInteractionHistory,
                _diagnosticsLog);
            _pendingReferenceGrade = null;
            ReferenceAnalysisStatusText = $"Uploading only {sourceItem!.Name} and {referenceItem!.Name} to Gemini after consent...";
            var result = await client.AnalyzeAsync(
                new GeminiReferenceAnalysisRequest([source], [reference], uploadConsent: true),
                new Progress<LocalAnalysisProgress>(update => ReferenceAnalysisStatusText = $"{update.Stage} ({update.Fraction:P0})"),
                cancellationToken);
            _pendingReferenceGrade = result.Grade;
            GeminiUsageStatusText = FormatGeminiUsage(result.Usage, result.Attempts, 2);
            RefreshGeminiActivityLog();
            ReferenceAnalysisStatusText = "Reference grade ready for review; apply it only after checking the before/after preview.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RefreshGeminiActivityLog();
            ReferenceAnalysisStatusText = "Gemini reference matching canceled; no grade was applied.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or HttpRequestException)
        {
            _diagnosticsLog.Record("gemini-reference-analysis-failed", exception);
            RefreshGeminiActivityLog();
            ReferenceAnalysisStatusText = $"Gemini reference matching failed: {exception.Message}";
            return false;
        }
    }

    public bool ApplyPendingReferenceGrade()
    {
        if (_pendingReferenceGrade is not { } grade)
        {
            ReferenceAnalysisStatusText = "Analyze an approved source/reference pair before applying a grade.";
            return false;
        }

        ColorInspector.ApplyGrade(grade);
        _pendingReferenceGrade = null;
        ReferenceAnalysisStatusText = "Applied the reviewed Gemini reference grade; the change remains editable through the color controls.";
        return true;
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
            _lastTimelineExportRequest = new TimelineExportRequest(plan, destinationPath, preset);
            return await RunTimelineExportAsync(_lastTimelineExportRequest, retryOf: null, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or MediaExportException or MediaProbeException)
        {
            _diagnosticsLog.Record("timeline-export-failed", exception);
            TimelineExportStatusText = $"Timeline export failed: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> ExportTimelineBatchAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        if (_timelineExporter is null)
        {
            TimelineExportStatusText = "Timeline export is unavailable.";
            return false;
        }

        try
        {
            outputDirectory = Path.GetFullPath(outputDirectory);
            if (!Directory.Exists(outputDirectory))
            {
                throw new DirectoryNotFoundException(outputDirectory);
            }

            var targets = Enum.GetValues<SocialAspectRatio>().Select(aspectRatio =>
            {
                var socialPreset = SocialVideoExportPreset.Create(aspectRatio);
                var plan = TimelineEditor.BuildRenderPlan(socialPreset.Width, socialPreset.Height, _mediaSources);
                var name = aspectRatio switch
                {
                    SocialAspectRatio.Landscape16By9 => "16x9-landscape",
                    SocialAspectRatio.Square1By1 => "1x1-square",
                    SocialAspectRatio.Portrait9By16 => "9x16-portrait",
                    _ => throw new ArgumentOutOfRangeException(nameof(aspectRatio))
                };
                return new TimelineBatchExportTarget(
                    name,
                    plan,
                    Path.Combine(outputDirectory, $"frame-theory-{name}.mp4"),
                    socialPreset.ToExportPreset(plan.FrameRate, plan.AudioLayers.Count > 0));
            }).ToArray();

            using var batchCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeBatchExport = batchCancellation;
            var receipts = await new TimelineBatchExporter(_timelineExporter).ExportAsync(
                targets,
                new Progress<TimelineBatchExportProgress>(update =>
                    TimelineExportStatusText = $"{update.TargetName}: {update.Progress.Stage} ({update.Progress.Fraction:P0}); {update.CompletedTargets}/{update.TotalTargets} targets complete."),
                batchCancellation.Token);
            TimelineExportStatusText = $"Exported {receipts.Count} social targets to {outputDirectory}.";
            return true;
        }
        catch (OperationCanceledException)
        {
            TimelineExportStatusText = "Batch timeline export canceled.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or DirectoryNotFoundException or IOException or InvalidOperationException or MediaExportException or MediaProbeException)
        {
            _diagnosticsLog.Record("timeline-batch-export-failed", exception);
            TimelineExportStatusText = $"Batch timeline export failed: {exception.Message}";
            return false;
        }
        finally
        {
            _activeBatchExport = null;
        }
    }

    public async Task<bool> RetryLastTimelineExportAsync(CancellationToken cancellationToken)
    {
        if (_timelineExporter is null || _lastTimelineExportRequest is null || _lastTimelineExport is null)
        {
            TimelineExportStatusText = "No failed or canceled timeline export is available to retry.";
            return false;
        }

        if (_lastTimelineExport.State is not (ExportQueueJobState.Failed or ExportQueueJobState.Canceled))
        {
            TimelineExportStatusText = "Only a failed or canceled timeline export can be retried.";
            return false;
        }

        return await RunTimelineExportAsync(_lastTimelineExportRequest, _lastTimelineExport, cancellationToken);
    }

    private async Task<bool> RunTimelineExportAsync(
        TimelineExportRequest request,
        ExportQueueJob? retryOf,
        CancellationToken cancellationToken)
    {
        try
        {
            var job = retryOf is null
                ? _exportQueue.Enqueue(queueCancellation => _timelineExporter!.ExportAsync(
                    request.Plan,
                    request.DestinationPath,
                    request.Preset,
                    new Progress<ExportProgress>(update => TimelineExportStatusText = $"{update.Stage} ({update.Fraction:P0})"),
                    queueCancellation))
                : _exportQueue.Retry(retryOf);
            _lastTimelineExport = job;
            _activeExport = job;
            TimelineExportStatusText = $"Queued export {job.Id:N}.";
            using var cancellationRegistration = cancellationToken.Register(job.Cancel);
            var receipt = await job.Completion.WaitAsync(cancellationToken);
            TimelineExportStatusText = $"Exported {Path.GetFileName(receipt.OutputPath)} ({receipt.OutputBytes} bytes).";
            return true;
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
        if (_activeExport is null && _activeBatchExport is null)
        {
            TimelineExportStatusText = "No timeline export is active.";
            return;
        }

        _activeExport?.Cancel();
        _activeBatchExport?.Cancel();
        TimelineExportStatusText = "Timeline export cancellation requested.";
    }

    public async Task<bool> ImportCaptionsAsync(string sourcePath, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(sourcePath);
            if (LocalPathSecurity.ContainsReparsePoint(fullPath))
            {
                throw new InvalidDataException("Symlinked caption files are not accepted.");
            }

            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
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

    public async Task<bool> ImportTranscriptAsync(string sourcePath, string transcriptPath, CancellationToken cancellationToken)
    {
        try
        {
            var fullSourcePath = Path.GetFullPath(sourcePath);
            var fullTranscriptPath = Path.GetFullPath(transcriptPath);
            if (LocalPathSecurity.ContainsReparsePoint(fullSourcePath) || LocalPathSecurity.ContainsReparsePoint(fullTranscriptPath))
            {
                throw new InvalidDataException("Symlinked transcript sources are not accepted.");
            }

            CaptionStatusText = $"Verifying transcript source: {Path.GetFileName(fullSourcePath)}.";
            var provider = new JsonTranscriptProvider(fullTranscriptPath);
            var progressActive = true;
            TranscriptResult result;
            try
            {
                result = await provider.TranscribeAsync(
                    new Uri(fullSourcePath),
                    new Progress<TranscriptionProgress>(update =>
                    {
                        if (progressActive && update.Fraction < 1)
                        {
                            CaptionStatusText = $"{update.Stage} ({update.Fraction:P0})";
                        }
                    }),
                    cancellationToken);
            }
            finally
            {
                progressActive = false;
            }
            TimelineEditor.ImportTranscript(result);
            CaptionStatusText = $"Imported {TimelineEditor.Captions.Count} verified transcript caption cue(s) from {Path.GetFileName(fullTranscriptPath)}.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CaptionStatusText = "Verified transcript import canceled; no caption changes were applied.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
        {
            _diagnosticsLog.Record("verified-transcript-import-failed", exception);
            CaptionStatusText = $"Verified transcript import failed: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> TranscribeWithLocalWhisperAsync(
        string sourcePath,
        string executablePath,
        string modelPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var fullSourcePath = Path.GetFullPath(sourcePath);
            if (LocalPathSecurity.ContainsReparsePoint(fullSourcePath))
            {
                throw new InvalidDataException("Symlinked local Whisper sources are not accepted.");
            }

            CaptionStatusText = $"Preparing local Whisper: {Path.GetFileName(fullSourcePath)}.";
            var provider = new WhisperCliTranscriptionProvider(new LocalWhisperOptions(executablePath, modelPath));
            var progressActive = true;
            TranscriptResult result;
            try
            {
                result = await provider.TranscribeAsync(
                    new Uri(fullSourcePath),
                    new Progress<TranscriptionProgress>(update =>
                    {
                        if (progressActive && update.Fraction < 1)
                        {
                            CaptionStatusText = $"{update.Stage} ({update.Fraction:P0})";
                        }
                    }),
                    cancellationToken);
            }
            finally
            {
                progressActive = false;
            }
            TimelineEditor.ImportTranscript(result);
            CaptionStatusText = $"Imported {TimelineEditor.Captions.Count} verified local Whisper caption cue(s).";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CaptionStatusText = "Local Whisper transcription canceled; no caption changes were applied.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            _diagnosticsLog.Record("local-whisper-transcription-failed", exception);
            CaptionStatusText = $"Local Whisper transcription failed: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> ExportCaptionsAsync(string destinationPath, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(destinationPath);
            if (LocalPathSecurity.ContainsReparsePoint(fullPath))
            {
                throw new InvalidDataException("Symlinked caption export destinations are not accepted.");
            }

            var content = TimelineEditor.ExportCaptions();
            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
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

    public async Task<bool> TranscribeAudioAsync(string sourcePath, bool uploadConsent, CancellationToken cancellationToken)
    {
        if (!uploadConsent)
        {
            CaptionStatusText = "Transcription stopped: explicitly approve uploading this exact local audio file first.";
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(sourcePath);
            if (LocalPathSecurity.ContainsReparsePoint(fullPath))
            {
                throw new InvalidDataException("Symlinked audio sources are not accepted.");
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The selected audio file was not found.", fullPath);
            }

            var apiKey = await GeminiSettings.ReadApiKeyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                CaptionStatusText = "Transcription requires a stored Gemini key; manual captions remain available offline.";
                return false;
            }

            CaptionStatusText = $"Preparing approved media: {Path.GetFileName(fullPath)}.";
            var originalSource = new Uri(fullPath);
            var originalFingerprint = await TranscriptSourceFingerprint.ComputeAsync(originalSource, cancellationToken);
            await using var preparedSource = await _audioSourcePreparer.PrepareAsync(new Uri(fullPath), cancellationToken);
            CaptionStatusText = $"Uploading approved audio: {Path.GetFileName(fullPath)}.";
            var provider = new GeminiTranscriptionProvider(
                _transcriptionHttpClient,
                new GeminiTranscriptionOptions(apiKey, uploadConsent: true));
            var progressActive = true;
            TranscriptResult result;
            try
            {
                result = await provider.TranscribeAsync(
                    preparedSource.Source,
                    new Progress<TranscriptionProgress>(update =>
                    {
                        if (progressActive)
                        {
                            CaptionStatusText = $"{update.Stage} ({update.Fraction:P0})";
                        }
                    }),
                    cancellationToken);
            }
            finally
            {
                progressActive = false;
            }
            var currentFingerprint = await TranscriptSourceFingerprint.ComputeAsync(originalSource, cancellationToken);
            if (!string.Equals(originalFingerprint, currentFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The selected media changed while transcription was in progress.");
            }

            TimelineEditor.ImportTranscript(new TranscriptResult(
                originalSource,
                originalFingerprint,
                result.Words));
            CaptionStatusText = $"Imported {TimelineEditor.Captions.Count} verified transcript caption cue(s) from {Path.GetFileName(fullPath)}.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CaptionStatusText = "Gemini transcription canceled; no caption changes were applied.";
            return false;
        }
        catch (OperationCanceledException)
        {
            CaptionStatusText = "Gemini transcription timed out; no caption changes were applied.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or InvalidOperationException or HttpRequestException)
        {
            _diagnosticsLog.Record("gemini-transcription-failed", exception);
            CaptionStatusText = $"Gemini transcription failed: {exception.Message}";
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

    private void SetMissingTimelineMedia(IReadOnlyList<MediaReference> references)
    {
        MissingTimelineMedia = references
            .Select(reference => new MissingMediaViewModel(reference.Id, reference.RelativePath))
            .ToArray();
        SelectedMissingTimelineMedia = MissingTimelineMedia.FirstOrDefault();
        OnPropertyChanged(nameof(MissingTimelineMedia));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatGeminiUsage(
        GeminiUsage? usage,
        int attempts,
        int uploadedSampleCount = 0,
        long? uploadedBytes = null)
    {
        var tokenSummary = usage is null
            ? "provider usage unavailable"
            : $"input={usage.InputTokens?.ToString() ?? "n/a"}, output={usage.OutputTokens?.ToString() ?? "n/a"}, total={usage.TotalTokens?.ToString() ?? "n/a"}";
        var uploadSummary = uploadedSampleCount > 0
            ? $", approved samples={uploadedSampleCount}{(uploadedBytes is { } bytes ? $", uploaded bytes={bytes}" : string.Empty)}"
            : string.Empty;
        return $"Gemini usage: {tokenSummary}; attempts={attempts}{uploadSummary}.";
    }

    private void RefreshGeminiActivityLog()
    {
        if (_geminiInteractionHistory is not IGeminiInteractionHistoryReader reader)
        {
            return;
        }

        var records = reader.ReadRecent();
        GeminiActivityLogText = records.Count == 0
            ? "No Gemini activity recorded."
            : string.Join(
                Environment.NewLine,
                records.Select(record =>
                    $"{record.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss} {record.OperationName ?? "interaction"} " +
                    $"{(record.Succeeded ? "succeeded" : "failed")}; scope={record.ScopeSummary ?? "not recorded"}; " +
                    $"attempts={record.Attempts}; total_tokens={record.Usage?.TotalTokens?.ToString() ?? "n/a"}"));
    }

    private bool TryCreateVisualSample(ImportedMediaViewModel? item, out LocalAnalysisSample sample)
    {
        if (item?.Id is { } id &&
            _analysisAssets.TryGetValue(id, out var media) &&
            media.ThumbnailPpm is { } thumbnail)
        {
            sample = new LocalAnalysisSample(
                media.Metadata.SourceUri,
                media.Metadata.Kind,
                media.Metadata.Kind == MediaKind.Video ? "video-frame-ppm" : "photo-preview-ppm",
                0,
                TimeSpan.Zero,
                thumbnail);
            return true;
        }

        sample = null!;
        return false;
    }
}

internal sealed record TimelineExportRequest(
    TimelineVideoRenderPlan Plan,
    string DestinationPath,
    ExportPreset Preset);

public sealed class DesktopServices
{
    private readonly IDiagnosticsLog _diagnosticsLog;

    public DesktopServices(IDiagnosticsLog? diagnosticsLog = null) => _diagnosticsLog = diagnosticsLog ?? new FileDiagnosticsLog();

    public MainWindowViewModel CreateMainWindowViewModel()
    {
        var processRunner = new FfmpegProcessRunner();
        var ffmpegExecutable = FfmpegExecutableLocator.Resolve("ffmpeg");
        var ffprobeExecutable = FfmpegExecutableLocator.Resolve("ffprobe");
        var decoder = new FfmpegFrameDecoder(processRunner);
        var importer = new FfmpegMediaImporter(
            new FfprobeMediaProbe(processRunner),
            decoder,
            processRunner);
        var probe = new FfprobeMediaProbe(processRunner);
        var repository = new JsonProjectRepository();
        var exportQueue = new ExportQueueService();
        var previewProxy = new FfmpegPreviewProxyService(
            processRunner,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FrameTheoryStudio", "preview-cache"));
        return new(
            new WorkspaceCatalog(),
            new FrameTheory.Platform.SystemCapabilityDetector(ffmpegExecutable, ffprobeExecutable),
            importer,
            new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), repository),
            new PhotoEditorViewModel(new PhotoImporter(probe, decoder), new PhotoExporter(processRunner, probe), exportQueue, repository),
            timelineExporter: new FfmpegTimelineVideoExporter(processRunner, new FfprobeMediaProbe(processRunner)),
            analysisSampler: new LocalAnalysisSampler(decoder),
            exportQueue: exportQueue,
            diagnosticsLog: _diagnosticsLog,
            timelinePreviewDecoder: decoder,
            timelinePreviewProxy: previewProxy,
            timelinePreviewRenderer: new FfmpegTimelinePreviewRenderer(processRunner),
            workspaceStateStore: new FileWorkspaceStateStore());
    }
}
