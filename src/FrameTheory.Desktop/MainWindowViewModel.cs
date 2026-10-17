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
    private string _aiRevisionStatusText = "No Gemini revision requested.";
    private readonly Dictionary<MediaId, Uri> _mediaSources = [];
    private readonly Dictionary<MediaId, ImportedMedia> _analysisAssets = [];
    private ITimelineVideoExporter? _timelineExporter;
    private readonly ExportQueueService _exportQueue;
    private readonly HttpClient _transcriptionHttpClient;
    private readonly IAudioSourcePreparer _audioSourcePreparer;
    private readonly IGeminiInteractionHistory _geminiInteractionHistory;
    private string? _lastGeminiInteractionId;
    private ExportQueueJob? _activeExport;
    private ExportQueueJob? _lastTimelineExport;
    private TimelineExportRequest? _lastTimelineExportRequest;
    private readonly ILocalAnalysisSampler? _analysisSampler;
    private readonly IPreviewFrameDecoder _timelinePreviewDecoder;
    private int _videoWidth;
    private int _videoHeight;
    private byte[]? _timelinePreviewPpm;
    private string _timelinePreviewStatusText = "No timeline frame preview requested.";
    private string _timelineProjectStatusText = "No timeline project opened.";

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
        IPreviewFrameDecoder? timelinePreviewDecoder = null)
    {
        Workspaces = workspaceCatalog.GetWorkspaces();
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
        _timelineExporter = timelineExporter;
        _analysisSampler = analysisSampler;
        _timelinePreviewDecoder = timelinePreviewDecoder ?? new FfmpegFrameDecoder(new FfmpegProcessRunner());
        if (Workspaces.Count == 0)
        {
            throw new InvalidOperationException("At least one workspace is required.");
        }
    }

    public IReadOnlyList<WorkspaceViewModel> Workspaces { get; }

    public IReadOnlyList<ImportedMediaViewModel> ImportedMedia { get; private set; } = [];

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
        }
    }

    public PhotoRecoveryViewModel PhotoRecovery { get; }

    public PhotoEditorViewModel? PhotoEditor { get; }

    public ColorGradeInspectorViewModel ColorInspector { get; }

    public TimelineEditorViewModel TimelineEditor { get; }

    public GeminiSettingsViewModel GeminiSettings { get; }

    public AiProposalViewModel AiProposal { get; } = new();

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
            ValidateAiPlan(result.OutputText);
            AiRevisionStatusText = AiProposal.Actions.Count == 0
                ? "Gemini returned a response that failed local plan validation; no mutation occurred."
                : $"Validated Gemini revision {result.InteractionId}; review and approve actions before applying.";
            return AiProposal.Actions.Count > 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AiRevisionStatusText = "Gemini revision canceled; no mutation occurred.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or HttpRequestException)
        {
            _diagnosticsLog.Record("gemini-revision-failed", exception);
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
            TimelinePreviewStatusText = $"Decoding frame {sourceFrame}...";
            TimelinePreviewPpm = await _timelinePreviewDecoder.DecodeFrameAsync(
                source,
                media.Metadata.FrameRate ?? new RationalFrameRate(30, 1),
                sourceFrame,
                cancellationToken);
            TimelinePreviewStatusText = $"Previewed frame {sourceFrame} from {Path.GetFileName(source.LocalPath)}.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TimelinePreviewStatusText = "Timeline preview canceled.";
            return false;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or MediaDecodeException)
        {
            _diagnosticsLog.Record("timeline-preview-failed", exception);
            TimelinePreviewStatusText = $"Timeline preview failed: {exception.Message}";
            TimelinePreviewPpm = null;
            return false;
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
            var snapshot = await new JsonProjectRepository().LoadAsync(fullPath, cancellationToken);
            if (snapshot.TimelineStateJson is null)
            {
                throw new InvalidDataException("The project does not contain a saved timeline.");
            }

            var timelineState = TimelineProjectStateJson.Deserialize(snapshot.TimelineStateJson);
            var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A project directory is required.", nameof(projectPath));
            var imported = new List<(MediaReference Reference, ImportedMedia Media, Uri Source)>();
            foreach (var reference in snapshot.MediaReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = Path.GetFullPath(Path.Combine(directory, reference.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
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
                    item.Media.ThumbnailPpm));
            }

            ImportedMedia = importedViewModels;
            OnPropertyChanged(nameof(ImportedMedia));
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
            TimelineProjectStatusText = $"Opened timeline project {Path.GetFileName(fullPath)}.";
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
            await using var preparedSource = await _audioSourcePreparer.PrepareAsync(new Uri(fullPath), cancellationToken);
            CaptionStatusText = $"Uploading approved audio: {Path.GetFileName(fullPath)}.";
            var provider = new GeminiTranscriptionProvider(
                _transcriptionHttpClient,
                new GeminiTranscriptionOptions(apiKey, uploadConsent: true));
            var result = await provider.TranscribeAsync(
                preparedSource.Source,
                new Progress<TranscriptionProgress>(update => CaptionStatusText = $"{update.Stage} ({update.Fraction:P0})"),
                cancellationToken);
            TimelineEditor.ImportTranscript(new TranscriptResult(
                new Uri(fullPath),
                result.SourceFingerprint,
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        var decoder = new FfmpegFrameDecoder(processRunner);
        var importer = new FfmpegMediaImporter(
            new FfprobeMediaProbe(processRunner),
            decoder,
            processRunner);
        var probe = new FfprobeMediaProbe(processRunner);
        var repository = new JsonProjectRepository();
        var exportQueue = new ExportQueueService();
        return new(
            new WorkspaceCatalog(),
            new FrameTheory.Platform.SystemCapabilityDetector(),
            importer,
            new PhotoRecoveryViewModel(new ProjectRecoveryService(repository), repository),
            new PhotoEditorViewModel(new PhotoImporter(probe, decoder), new PhotoExporter(processRunner, probe), exportQueue),
            timelineExporter: new FfmpegTimelineVideoExporter(processRunner, new FfprobeMediaProbe(processRunner)),
            analysisSampler: new LocalAnalysisSampler(decoder),
            exportQueue: exportQueue,
            diagnosticsLog: _diagnosticsLog,
            timelinePreviewDecoder: decoder);
    }
}
