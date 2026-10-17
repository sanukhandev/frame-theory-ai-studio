using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using FrameTheory.Captions;
using FrameTheory.Color;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.Desktop;

public sealed record TimelineClipRowViewModel(
    string MediaId,
    long StartFrame,
    long DurationFrames,
    bool IsSelected)
{
    public override string ToString() => $"{MediaId[..Math.Min(8, MediaId.Length)]}  {StartFrame}–{StartFrame + DurationFrames - 1}";
}

public sealed record TimelineTrackRowViewModel(
    string Id,
    TimelineTrackKind Kind,
    IReadOnlyList<TimelineClipRowViewModel> Clips)
{
    public override string ToString() => $"{Kind}: {Id} — {string.Join(", ", Clips)}";
}

public sealed record TimelineRulerTick(long Frame, TimeSpan Position)
{
    public override string ToString() => $"{Frame} ({Position.TotalSeconds:0.###}s)";
}

public sealed record TimelineClipboard(TimelineTrackKind Kind, TimelineClip Clip);

public sealed class TimelineEditorViewModel : INotifyPropertyChanged
{
    private const int AudioSampleRate = 48000;
    private readonly RationalFrameRate _projectFrameRate;
    private readonly List<TimelineClip> _videoClips = [];
    private readonly List<TimelineClip> _audioClips = [];
    private readonly Dictionary<TimelineClip, TimelineTransformKeyframes> _animations = [];
    private readonly Dictionary<TimelineClip, TimelineAudioSegment> _audioSettings = [];
    private readonly Dictionary<TimelineClip, TimelineTransitionSettings> _transitions = [];
    private readonly Dictionary<TimelineClip, TimelineVideoTransform> _staticTransforms = [];
    private readonly Dictionary<MediaId, ColorGrade> _colorGrades = [];
    private readonly List<TimelineTitle> _titles = [];
    private IReadOnlyList<TimedCaption> _captions = [];
    private string? _captionFontPath;
    private readonly HashSet<MediaId> _mediaIds = [];
    private TimelineGraph _graph;
    private TimelineTransport _transport;
    private long? _selectedClipStart;
    private string? _selectedTrackId;
    private double _framesPerPixel = 1;
    private long _scrollFrame;
    private bool _snapEnabled = true;
    private TimelineRevealStyle? _beforeAfterStyle;
    private readonly Stack<TimelineEditorState> _aiUndo = [];

    public TimelineEditorViewModel(RationalFrameRate? projectFrameRate = null)
    {
        _projectFrameRate = projectFrameRate ?? new RationalFrameRate(30, 1);
        _graph = new TimelineGraph(_projectFrameRate, []);
        _transport = new TimelineTransport(_graph);
        Refresh();
    }

    public long CurrentFrame => _transport.CurrentFrame;

    public TimeSpan CurrentPosition => _projectFrameRate.TimeAtFrame(CurrentFrame);

    public long TotalFrames { get; private set; } = 1;

    public double FramesPerPixel
    {
        get => _framesPerPixel;
        private set
        {
            if (value.Equals(_framesPerPixel))
            {
                return;
            }

            _framesPerPixel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RulerText));
        }
    }

    public long ScrollFrame
    {
        get => _scrollFrame;
        private set
        {
            if (value == _scrollFrame)
            {
                return;
            }

            _scrollFrame = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RulerText));
        }
    }

    public bool SnapEnabled
    {
        get => _snapEnabled;
        set
        {
            if (value == _snapEnabled)
            {
                return;
            }

            _snapEnabled = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<TimelineTrackRowViewModel> TrackRows { get; private set; } = [];

    public string RulerText => string.Join("  |  ", BuildRulerTicks(640).Select(tick => tick.ToString()));

    public void AddMedia(MediaSourceMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (!_mediaIds.Add(metadata.Id) || metadata.DurationFrames <= 0 && metadata.Duration == TimeSpan.Zero)
        {
            return;
        }

        var startFrame = TotalFrames == 1 && _videoClips.Count == 0 && _audioClips.Count == 0 ? 0 : TotalFrames;
        if (metadata.Kind == MediaKind.Video)
        {
            _videoClips.Add(TimelineClip.Video(
                metadata.Id,
                0,
                metadata.DurationFrames,
                startFrame,
                metadata.DurationFrames,
                metadata.FrameRate ?? _projectFrameRate));
            if (metadata.HasAudio)
            {
                _audioClips.Add(TimelineClip.Audio(
                    metadata.Id,
                    0,
                    SampleCount(metadata.Duration),
                    startFrame,
                    metadata.DurationFrames,
                    AudioSampleRate));
            }
        }
        else if (metadata.Kind == MediaKind.Audio)
        {
            var durationFrames = Math.Max(1, metadata.FrameRate?.FrameIndexAt(metadata.Duration) ??
                _projectFrameRate.FrameIndexAt(metadata.Duration));
            _audioClips.Add(TimelineClip.Audio(
                metadata.Id,
                0,
                SampleCount(metadata.Duration),
                startFrame,
                durationFrames,
                AudioSampleRate));
        }

        Refresh();
    }

    public TimelineVideoRenderPlan BuildRenderPlan(
        int width,
        int height,
        IReadOnlyDictionary<MediaId, Uri> sources)
    {
        if (_videoClips.Count == 0)
        {
            throw new InvalidOperationException("Import at least one video before exporting the timeline.");
        }

        IReadOnlyList<TimelineAudioLayer> audioLayers = _audioClips.Count == 0
            ? []
            : [new TimelineAudioLayer("Audio 1", 0, _audioClips.Select(clip => _audioSettings.TryGetValue(clip, out var settings) ? settings : new TimelineAudioSegment(clip)))];
        return new TimelineVideoRenderPlan(
            _projectFrameRate,
            width,
            height,
            TotalFrames,
            sources,
            [new TimelineVideoLayer("Video 1", 0, _videoClips.Select(clip => new TimelineVideoSegment(
                clip,
                transform: _staticTransforms.TryGetValue(clip, out var transform) ? transform : null,
                transition: _transitions.TryGetValue(clip, out var transition) ? transition.Kind : null,
                transitionDurationFrames: _transitions.TryGetValue(clip, out transition) ? transition.DurationFrames : 0,
                animation: _animations.TryGetValue(clip, out var animation) ? animation : null)))],
            audioLayers,
            _titles,
            _captions,
            _captionFontPath is null ? null : new TimelineCaptionStyle(_captionFontPath),
            _colorGrades,
            _beforeAfterStyle,
            TotalFrames / 2);
    }

    public void ApplyBeforeAfterStyle(TimelineRevealStyle style)
    {
        if (_videoClips.Count != 1)
        {
            throw new InvalidOperationException("Before/after composition currently requires exactly one video clip.");
        }

        _beforeAfterStyle = style;
    }

    public bool SelectMedia(string mediaId, TimelineTrackKind kind)
    {
        var trackId = TrackId(kind);
        var track = _graph.Tracks.SingleOrDefault(candidate => string.Equals(candidate.Id, trackId, StringComparison.OrdinalIgnoreCase));
        var clip = track?.Clips.FirstOrDefault(candidate => string.Equals(candidate.MediaId.Value, mediaId, StringComparison.Ordinal));
        if (clip is null)
        {
            return false;
        }

        _selectedTrackId = trackId;
        _selectedClipStart = clip.TimelineRange.StartFrame;
        RefreshRows();
        return true;
    }

    public bool SelectFirst(TimelineTrackKind kind)
    {
        var trackId = TrackId(kind);
        var clip = _graph.Tracks.SingleOrDefault(candidate => string.Equals(candidate.Id, trackId, StringComparison.OrdinalIgnoreCase))?.Clips.FirstOrDefault();
        return clip is not null && SelectMedia(clip.MediaId.Value, kind);
    }

    public void AddTitleAt(string text, string fontPath, long startFrame, long durationFrames, TimelineAnimationPreset? preset = null)
    {
        if (_videoClips.Count == 0 || startFrame < 0 || durationFrames <= 0 || checked(startFrame + durationFrames) > TotalFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(startFrame), "The title range must fit within the video timeline.");
        }

        _titles.Add(new TimelineTitle(text, fontPath, startFrame, durationFrames, order: _titles.Count));
        if (preset is not null)
        {
            ApplyAnimationPresetToLastTitle(preset.Value);
        }

        OnPropertyChanged(nameof(TitleAnimationStatus));
    }

    public void AddCaptionAt(string text, CaptionProvenance provenance, long startFrame, long durationFrames, string fontPath)
    {
        if (startFrame < 0 || durationFrames <= 0 || startFrame + durationFrames > TotalFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(startFrame));
        }

        SetCaptionFontPath(fontPath);
        var caption = new TimedCaption(
            _captions.Count + 1,
            _projectFrameRate.TimeAtFrame(startFrame),
            _projectFrameRate.TimeAtFrame(startFrame + durationFrames),
            text,
            provenance);
        if (_captions.Any(existing => existing.End > caption.Start && caption.End > existing.Start))
        {
            throw new InvalidOperationException("AI caption actions cannot overlap existing captions.");
        }

        _captions = _captions.Append(caption).OrderBy(existing => existing.Start).ToArray();
        OnPropertyChanged(nameof(Captions));
    }

    public void ApplyStaticTransformToSelected(double scale, double offsetX, double offsetY, double rotation)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsVideo || rotation != 0)
        {
            throw new InvalidOperationException("Only video scale and non-negative pixel offsets are supported by this executor.");
        }

        var transform = new TimelineVideoTransform(1, scale, checked((int)Math.Round(offsetX)), checked((int)Math.Round(offsetY)));
        _animations.Remove(selected.Clip);
        _transitions.Remove(selected.Clip);
        _staticTransforms[selected.Clip] = transform;
    }

    public void ApplyKeyframesToSelected(TimelineTransformKeyframes keyframes)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsVideo)
        {
            throw new InvalidOperationException("Keyframes require a video clip.");
        }

        if (keyframes.Keyframes[0].Frame < selected.Clip.TimelineRange.StartFrame ||
            keyframes.Keyframes[^1].Frame >= selected.Clip.TimelineRange.EndFrameExclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(keyframes), "Keyframes must fit inside the selected clip.");
        }

        _animations[selected.Clip] = keyframes;
    }

    public void ApplyTransitionToSelected(TimelineTransitionKind kind, long durationFrames)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsVideo || selected.Clip.TimelineRange.StartFrame == 0)
        {
            throw new InvalidOperationException("Transitions require a non-first video clip.");
        }

        if (durationFrames <= 0 || durationFrames >= selected.Clip.TimelineRange.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(durationFrames), "The transition duration must be shorter than the selected clip.");
        }

        _transitions[selected.Clip] = new TimelineTransitionSettings(kind, durationFrames);
    }

    public void MoveSelectedBy(long offsetFrames)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        MoveSelected(checked(selected.Clip.TimelineRange.StartFrame + offsetFrames));
    }

    public void ApplyColorGradeToMedia(string mediaId, ColorGrade grade)
    {
        ArgumentNullException.ThrowIfNull(grade);
        if (!_videoClips.Any(clip => clip.MediaId.Value == mediaId))
        {
            throw new InvalidOperationException($"The video media '{mediaId}' is not on the timeline.");
        }

        _colorGrades[MediaId.Parse(mediaId)] = grade;
    }

    internal TimelineEditorState CaptureAiState() => new(
        _videoClips.ToArray(),
        _audioClips.ToArray(),
        new Dictionary<TimelineClip, TimelineTransformKeyframes>(_animations),
        new Dictionary<TimelineClip, TimelineAudioSegment>(_audioSettings),
        new Dictionary<TimelineClip, TimelineTransitionSettings>(_transitions),
        new Dictionary<TimelineClip, TimelineVideoTransform>(_staticTransforms),
        new Dictionary<MediaId, ColorGrade>(_colorGrades),
        _beforeAfterStyle,
        _titles.ToArray(),
        _captions.ToArray(),
        _captionFontPath,
        _mediaIds.ToHashSet(),
        _selectedTrackId,
        _selectedClipStart,
        CurrentFrame);

    internal void CommitAiState(TimelineEditorState state) => _aiUndo.Push(state);

    internal void RestoreAiStateForAi(TimelineEditorState state) => RestoreAiState(state);

    private void RestoreAiState(TimelineEditorState state)
    {
        _videoClips.Clear();
        _videoClips.AddRange(state.VideoClips);
        _audioClips.Clear();
        _audioClips.AddRange(state.AudioClips);
        _animations.Clear();
        foreach (var item in state.Animations)
        {
            _animations[item.Key] = item.Value;
        }

        _audioSettings.Clear();
        foreach (var item in state.AudioSettings)
        {
            _audioSettings[item.Key] = item.Value;
        }

        _transitions.Clear();
        foreach (var item in state.Transitions)
        {
            _transitions[item.Key] = item.Value;
        }

        _staticTransforms.Clear();
        foreach (var item in state.StaticTransforms)
        {
            _staticTransforms[item.Key] = item.Value;
        }

        _colorGrades.Clear();
        foreach (var item in state.ColorGrades)
        {
            _colorGrades[item.Key] = item.Value;
        }

        _beforeAfterStyle = state.BeforeAfterStyle;

        _titles.Clear();
        _titles.AddRange(state.Titles);
        _captions = state.Captions;
        _captionFontPath = state.CaptionFontPath;
        _mediaIds.Clear();
        _mediaIds.UnionWith(state.MediaIds);
        _selectedTrackId = state.SelectedTrackId;
        _selectedClipStart = state.SelectedClipStart;
        Refresh();
        SeekFrame(state.CurrentFrame);
    }

    public bool UndoAiEdit()
    {
        if (_aiUndo.Count == 0)
        {
            return false;
        }

        RestoreAiState(_aiUndo.Pop());
        return true;
    }

    public void AddTitle(string text, string fontPath)
    {
        if (_videoClips.Count == 0)
        {
            throw new InvalidOperationException("Import at least one video before adding a title.");
        }

        var duration = Math.Min(90, TotalFrames - CurrentFrame);
        if (duration <= 0)
        {
            throw new InvalidOperationException("The playhead is outside the title range.");
        }

        _titles.Add(new TimelineTitle(text, fontPath, CurrentFrame, duration, order: _titles.Count));
        OnPropertyChanged(nameof(TitleAnimationStatus));
    }

    public void ApplyAnimationPresetToLastTitle(TimelineAnimationPreset preset)
    {
        if (_titles.Count == 0)
        {
            throw new InvalidOperationException("Add a title before applying an animation.");
        }

        var title = _titles[^1];
        _titles[^1] = new TimelineTitle(
            title.Text,
            title.FontPath,
            title.TimelineRange.StartFrame,
            title.TimelineRange.Length,
            title.FontSize,
            title.ColorRgb,
            title.Placement,
            title.Order,
            TimelineTransformKeyframes.FromPreset(
                preset,
                title.TimelineRange.StartFrame,
                title.TimelineRange.EndFrameExclusive,
                new TimelineVideoTransform()));
        OnPropertyChanged(nameof(TitleAnimationStatus));
    }

    public string TitleAnimationStatus
    {
        get
        {
            var animation = _titles.LastOrDefault()?.Animation;
            return animation is null
                ? "No animation on the last title."
                : string.Join(
                    " | ",
                    animation.Keyframes.Select(keyframe =>
                        $"frame {keyframe.Frame}: opacity {keyframe.Value.Opacity.ToString("0.###", CultureInfo.InvariantCulture)}, scale {keyframe.Value.Scale.ToString("0.###", CultureInfo.InvariantCulture)}, offset {keyframe.Value.OffsetX},{keyframe.Value.OffsetY}"));
        }
    }

    public IReadOnlyList<TimedCaption> Captions => _captions;

    public void ImportCaptions(string content) => _captions = SrtCaptionFile.Import(content);

    public string ExportCaptions() => SrtCaptionFile.Export(_captions);

    public void SetCaptionFontPath(string fontPath)
    {
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw new ArgumentException("A caption font path is required.", nameof(fontPath));
        }

        _captionFontPath = Path.GetFullPath(fontPath);
    }

    public void ApplyAnimationPresetToSelected(TimelineAnimationPreset preset)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsVideo)
        {
            throw new InvalidOperationException("Animation presets require a video clip.");
        }

        _animations[selected.Clip] = TimelineTransformKeyframes.FromPreset(
            preset,
            selected.Clip.TimelineRange.StartFrame,
            selected.Clip.TimelineRange.EndFrameExclusive,
            new TimelineVideoTransform());
    }

    public void ClearSelectedAnimation()
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        _animations.Remove(selected.Clip);
    }

    public void ApplyAudioSettingsToSelected(double gainDb, bool muted, long fadeInFrames = 0, long fadeOutFrames = 0, double duckingDb = 0)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsAudio)
        {
            throw new InvalidOperationException("Audio settings require an audio clip.");
        }

        _audioSettings[selected.Clip] = new TimelineAudioSegment(selected.Clip, gainDb, muted, fadeInFrames, fadeOutFrames, duckingDb);
    }

    public void ApplyAudioDuckingToSelected(double duckingDb = 12)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsAudio)
        {
            throw new InvalidOperationException("Audio ducking requires an audio clip.");
        }

        var current = _audioSettings.TryGetValue(selected.Clip, out var settings)
            ? settings
            : new TimelineAudioSegment(selected.Clip);
        _audioSettings[selected.Clip] = new TimelineAudioSegment(
            selected.Clip,
            current.GainDb,
            current.Muted,
            current.FadeInFrames,
            current.FadeOutFrames,
            duckingDb);
    }

    public void ClearAudioDuckingFromSelected()
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsAudio)
        {
            throw new InvalidOperationException("Audio ducking requires an audio clip.");
        }

        var current = _audioSettings.TryGetValue(selected.Clip, out var settings)
            ? settings
            : new TimelineAudioSegment(selected.Clip);
        _audioSettings[selected.Clip] = new TimelineAudioSegment(
            selected.Clip,
            current.GainDb,
            current.Muted,
            current.FadeInFrames,
            current.FadeOutFrames);
    }

    public void ClearSelectedAudioSettings()
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        _audioSettings.Remove(selected.Clip);
    }

    public void InsertClip(TimelineTrackKind kind, TimelineClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        var video = _videoClips.ToArray();
        var audio = _audioClips.ToArray();
        if (kind == TimelineTrackKind.Video)
        {
            video = [.. video, clip];
        }
        else
        {
            audio = [.. audio, clip];
        }

        ApplyClips(video, audio);
    }

    public TimelineClipboard? CopySelected()
    {
        var selected = FindSelected();
        return selected is null ? null : new TimelineClipboard(selected.Value.Kind, selected.Value.Clip);
    }

    public void Paste(TimelineClipboard clipboard, long timelineStartFrame)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        if (timelineStartFrame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineStartFrame));
        }

        InsertClip(clipboard.Kind, MoveClip(clipboard.Clip, timelineStartFrame));
        _selectedTrackId = TrackId(clipboard.Kind);
        _selectedClipStart = timelineStartFrame;
        RefreshRows();
    }

    public void MoveSelected(long timelineStartFrame)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (timelineStartFrame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineStartFrame));
        }

        var replacement = MoveClip(selected.Clip, timelineStartFrame);
        ReplaceSelected(selected.Kind, replacement);
        _selectedClipStart = timelineStartFrame;
        RefreshRows();
    }

    public void SplitSelectedAt(long timelineFrame)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.TimelineRange.Contains(timelineFrame) || timelineFrame == selected.Clip.TimelineRange.StartFrame)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineFrame));
        }

        var leftDuration = timelineFrame - selected.Clip.TimelineRange.StartFrame;
        if (leftDuration == selected.Clip.TimelineRange.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineFrame));
        }

        var rightStart = SourceUnitAt(selected.Clip, timelineFrame);
        var leftSourceCount = Math.Max(1, rightStart - selected.Clip.SourceStartUnit);
        var rightSourceCount = Math.Max(1, selected.Clip.SourceUnitCount - leftSourceCount);
        var left = CreateClip(selected.Clip, selected.Clip.TimelineRange.StartFrame, leftDuration, selected.Clip.SourceStartUnit, leftSourceCount);
        var right = CreateClip(selected.Clip, timelineFrame, selected.Clip.TimelineRange.Length - leftDuration, rightStart, rightSourceCount);
        var clips = Clips(selected.Kind).Where(clip => !ReferenceEquals(clip, selected.Clip)).Append(left).Append(right);
        ApplyClips(selected.Kind == TimelineTrackKind.Video ? clips : _videoClips, selected.Kind == TimelineTrackKind.Audio ? clips : _audioClips);
        _selectedClipStart = timelineFrame;
        RefreshRows();
    }

    public void TrimSelected(long newStartFrame, long newEndFrameExclusive)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        var range = selected.Clip.TimelineRange;
        if (newStartFrame < range.StartFrame || newEndFrameExclusive > range.EndFrameExclusive || newEndFrameExclusive <= newStartFrame)
        {
            throw new ArgumentOutOfRangeException(nameof(newStartFrame));
        }

        var sourceStart = SourceUnitAt(selected.Clip, newStartFrame);
        var sourceEnd = SourceUnitAt(selected.Clip, newEndFrameExclusive - 1);
        var replacement = CreateClip(
            selected.Clip,
            newStartFrame,
            newEndFrameExclusive - newStartFrame,
            sourceStart,
            Math.Max(1, sourceEnd - sourceStart + 1));
        ReplaceSelected(selected.Kind, replacement);
        _selectedClipStart = newStartFrame;
        RefreshRows();
    }

    public void TrimSelectedEndAt(long timelineFrame)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        TrimSelected(selected.Clip.TimelineRange.StartFrame, timelineFrame);
    }

    public void RippleDeleteSelected()
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        var deletedRange = selected.Clip.TimelineRange;
        var video = _videoClips.Where(clip => !ReferenceEquals(clip, selected.Clip)).Select(clip =>
            clip.TimelineRange.StartFrame >= deletedRange.EndFrameExclusive
                ? MoveClip(clip, clip.TimelineRange.StartFrame - deletedRange.Length)
                : clip);
        var audio = _audioClips.Where(clip => !ReferenceEquals(clip, selected.Clip)).Select(clip =>
            clip.TimelineRange.StartFrame >= deletedRange.EndFrameExclusive
                ? MoveClip(clip, clip.TimelineRange.StartFrame - deletedRange.Length)
                : clip);
        ApplyClips(video, audio);
        _selectedTrackId = null;
        _selectedClipStart = null;
        RefreshRows();
    }

    public TimelineSeekResult SeekFrame(long frame)
    {
        var target = SnapFrame(Math.Clamp(frame, 0, Math.Max(0, TotalFrames - 1)));
        var result = _transport.Seek(target);
        OnPropertyChanged(nameof(CurrentFrame));
        OnPropertyChanged(nameof(CurrentPosition));
        RefreshRows();
        return result;
    }

    public TimelineSeekResult Step(int frameDelta) =>
        SeekFrame(checked(CurrentFrame + frameDelta));

    public void ScrubPixels(double pixels)
    {
        if (double.IsNaN(pixels) || double.IsInfinity(pixels) || pixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixels));
        }

        SeekFrame(ScrollFrame + (long)Math.Round(pixels * FramesPerPixel, MidpointRounding.AwayFromZero));
    }

    public void SelectAt(long frame, string trackId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackId);
        var track = _graph.Tracks.SingleOrDefault(candidate => string.Equals(candidate.Id, trackId, StringComparison.OrdinalIgnoreCase));
        if (track?.ClipAt(frame) is null)
        {
            _selectedTrackId = null;
            _selectedClipStart = null;
        }
        else
        {
            _selectedTrackId = track.Id;
            _selectedClipStart = track.ClipAt(frame)!.TimelineRange.StartFrame;
        }

        RefreshRows();
    }

    public void SetZoom(double framesPerPixel)
    {
        if (double.IsNaN(framesPerPixel) || double.IsInfinity(framesPerPixel))
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerPixel));
        }

        FramesPerPixel = Math.Clamp(framesPerPixel, 0.05, 100);
    }

    public void ScrollBy(long frameDelta) => ScrollFrame = Math.Clamp(ScrollFrame + frameDelta, 0, Math.Max(0, TotalFrames - 1));

    public IReadOnlyList<TimelineRulerTick> BuildRulerTicks(double viewportPixels)
    {
        if (double.IsNaN(viewportPixels) || double.IsInfinity(viewportPixels) || viewportPixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportPixels));
        }

        var interval = Math.Max(1, (long)Math.Ceiling(100 * FramesPerPixel));
        var end = Math.Min(TotalFrames, ScrollFrame + (long)Math.Ceiling(viewportPixels * FramesPerPixel));
        return Enumerable.Range(0, checked((int)Math.Max(0, (end - ScrollFrame + interval - 1) / interval)))
            .Select(index => ScrollFrame + index * interval)
            .Select(frame => new TimelineRulerTick(frame, _projectFrameRate.TimeAtFrame(frame)))
            .ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private long SnapFrame(long frame)
    {
        if (!SnapEnabled)
        {
            return frame;
        }

        var boundaries = _graph.Tracks
            .SelectMany(track => track.Clips)
            .SelectMany(clip => new[] { clip.TimelineRange.StartFrame, clip.TimelineRange.EndFrameExclusive })
            .OrderBy(boundary => Math.Abs(boundary - frame))
            .FirstOrDefault(frame);
        return Math.Abs(boundaries - frame) <= 2 ? boundaries : frame;
    }

    private void Refresh()
    {
        var currentFrame = _transport.CurrentFrame;
        var tracks = new List<TimelineTrack>();
        if (_videoClips.Count > 0)
        {
            tracks.Add(new TimelineTrack("Video 1", TimelineTrackKind.Video, _videoClips));
        }

        if (_audioClips.Count > 0)
        {
            tracks.Add(new TimelineTrack("Audio 1", TimelineTrackKind.Audio, _audioClips));
        }

        _graph = new TimelineGraph(_projectFrameRate, tracks);
        _transport = new TimelineTransport(_graph);
        TotalFrames = Math.Max(1, tracks.SelectMany(track => track.Clips).Select(clip => clip.TimelineRange.EndFrameExclusive).DefaultIfEmpty(1).Max());
        _transport.Seek(Math.Clamp(currentFrame, 0, Math.Max(0, TotalFrames - 1)));
        ScrollFrame = Math.Min(ScrollFrame, TotalFrames - 1);
        RefreshRows();
        OnPropertyChanged(nameof(TotalFrames));
        OnPropertyChanged(nameof(RulerText));
    }

    private void RefreshRows()
    {
        TrackRows = _graph.Tracks.Select(track => new TimelineTrackRowViewModel(
            track.Id,
            track.Kind,
            track.Clips.Select(clip => new TimelineClipRowViewModel(
                clip.MediaId.Value,
                clip.TimelineRange.StartFrame,
                clip.TimelineRange.Length,
                string.Equals(_selectedTrackId, track.Id, StringComparison.OrdinalIgnoreCase) &&
                _selectedClipStart == clip.TimelineRange.StartFrame)).ToArray())).ToArray();
        OnPropertyChanged(nameof(TrackRows));
        OnPropertyChanged(nameof(RulerText));
    }

    private static long SampleCount(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return 1;
        }

        return Math.Max(1, checked((long)(((Int128)duration.Ticks * AudioSampleRate + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond)));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private (TimelineTrackKind Kind, TimelineClip Clip)? FindSelected()
    {
        if (_selectedTrackId is null || _selectedClipStart is null)
        {
            return null;
        }

        var track = _graph.Tracks.SingleOrDefault(candidate => string.Equals(candidate.Id, _selectedTrackId, StringComparison.OrdinalIgnoreCase));
        var clip = track?.Clips.SingleOrDefault(candidate => candidate.TimelineRange.StartFrame == _selectedClipStart);
        return clip is null ? null : (track!.Kind, clip);
    }

    private IReadOnlyList<TimelineClip> Clips(TimelineTrackKind kind) => kind == TimelineTrackKind.Video ? _videoClips : _audioClips;

    private void ReplaceSelected(TimelineTrackKind kind, TimelineClip replacement)
    {
        var clips = Clips(kind).Select(clip => clip.TimelineRange.StartFrame == _selectedClipStart ? replacement : clip);
        ApplyClips(kind == TimelineTrackKind.Video ? clips : _videoClips, kind == TimelineTrackKind.Audio ? clips : _audioClips);
    }

    private void ApplyClips(IEnumerable<TimelineClip> video, IEnumerable<TimelineClip> audio)
    {
        var videoArray = video.ToArray();
        var audioArray = audio.ToArray();
        _ = new TimelineTrack("Video 1", TimelineTrackKind.Video, videoArray);
        _ = new TimelineTrack("Audio 1", TimelineTrackKind.Audio, audioArray);
        _videoClips.Clear();
        _videoClips.AddRange(videoArray);
        _audioClips.Clear();
        _audioClips.AddRange(audioArray);
        Refresh();
    }

    private long SourceUnitAt(TimelineClip clip, long timelineFrame) =>
        clip.IsVideo
            ? clip.SourceFrameAt(timelineFrame, _projectFrameRate)
            : clip.SourceSampleAt(timelineFrame, _projectFrameRate);

    private static TimelineClip MoveClip(TimelineClip clip, long timelineStartFrame) =>
        CreateClip(clip, timelineStartFrame, clip.TimelineRange.Length, clip.SourceStartUnit, clip.SourceUnitCount);

    private static TimelineClip CreateClip(TimelineClip clip, long timelineStartFrame, long timelineDuration, long sourceStart, long sourceCount) =>
        clip.IsVideo
            ? TimelineClip.Video(clip.MediaId, sourceStart, sourceCount, timelineStartFrame, timelineDuration, clip.SourceFrameRate!.Value)
            : TimelineClip.Audio(clip.MediaId, sourceStart, sourceCount, timelineStartFrame, timelineDuration, clip.AudioSampleRate);

    private static string TrackId(TimelineTrackKind kind) => kind == TimelineTrackKind.Video ? "Video 1" : "Audio 1";
}

internal sealed record TimelineTransitionSettings(TimelineTransitionKind Kind, long DurationFrames);

internal sealed record TimelineEditorState(
    IReadOnlyList<TimelineClip> VideoClips,
    IReadOnlyList<TimelineClip> AudioClips,
    IReadOnlyDictionary<TimelineClip, TimelineTransformKeyframes> Animations,
    IReadOnlyDictionary<TimelineClip, TimelineAudioSegment> AudioSettings,
    IReadOnlyDictionary<TimelineClip, TimelineTransitionSettings> Transitions,
    IReadOnlyDictionary<TimelineClip, TimelineVideoTransform> StaticTransforms,
    IReadOnlyDictionary<MediaId, ColorGrade> ColorGrades,
    TimelineRevealStyle? BeforeAfterStyle,
    IReadOnlyList<TimelineTitle> Titles,
    IReadOnlyList<TimedCaption> Captions,
    string? CaptionFontPath,
    IReadOnlySet<MediaId> MediaIds,
    string? SelectedTrackId,
    long? SelectedClipStart,
    long CurrentFrame);
