using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using FrameTheory.AI;
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

public enum BeatDetectionMode
{
    AmplitudePeaks,
    EnergyOnsets
}

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
    private readonly Dictionary<TimelineClip, TimelineSpeedRamp> _speedRamps = [];
    private readonly Dictionary<TimelineClip, TimelineStabilizationSettings> _stabilization = [];
    private readonly Dictionary<MediaId, ColorGrade> _colorGrades = [];
    private readonly List<TimelineTitle> _titles = [];
    private IReadOnlyList<TimedCaption> _captions = [];
    private string? _transcriptSourceFingerprint;
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
    private readonly List<long> _beatMarkers = [];
    private MediaId? _beatMarkerMediaId;
    private IReadOnlyList<float> _beatMarkerWaveform = [];
    private BeatDetectionMode _beatDetectionMode;
    private bool _kineticCaptions;
    private readonly Stack<TimelineEditorState> _aiUndo = [];
    private TimelineWatermark? _watermark;

    public TimelineEditorViewModel(RationalFrameRate? projectFrameRate = null)
    {
        _projectFrameRate = projectFrameRate ?? new RationalFrameRate(30, 1);
        _graph = new TimelineGraph(_projectFrameRate, []);
        _transport = new TimelineTransport(_graph);
        Refresh();
    }

    public long CurrentFrame => _transport.CurrentFrame;

    public RationalFrameRate ProjectFrameRate => _projectFrameRate;

    public TimeSpan CurrentPosition => _projectFrameRate.TimeAtFrame(CurrentFrame);

    public string CurrentTimecode => $"{CurrentPosition.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture)} · F{CurrentFrame}";

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
            OnPropertyChanged(nameof(TrackRows));
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
            OnPropertyChanged(nameof(TrackRows));
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

    public IReadOnlyList<long> BeatMarkers => _beatMarkers;

    public BeatDetectionMode BeatDetectionMode
    {
        get => _beatDetectionMode;
        set
        {
            if (!Enum.IsDefined(value) || value == _beatDetectionMode)
            {
                return;
            }

            _beatDetectionMode = value;
            OnPropertyChanged();
            if (_beatMarkerMediaId is not null && _beatMarkerWaveform.Count > 0)
            {
                AddBeatMarkers(_beatMarkerMediaId.Value, _beatMarkerWaveform);
            }
        }
    }

    public string BeatMarkersText => _beatMarkers.Count == 0
        ? "No beat markers detected. Import audio or video with audio."
        : $"Beat markers: {string.Join(", ", _beatMarkers)} frames";

    public bool KineticCaptions
    {
        get => _kineticCaptions;
        set
        {
            if (value == _kineticCaptions)
            {
                return;
            }

            _kineticCaptions = value;
            OnPropertyChanged();
        }
    }

    public TimelineWatermark? Watermark => _watermark;

    public string WatermarkStatusText => _watermark is null
        ? "No watermark selected; approved brand assets are opt-in."
        : $"Watermark: {Path.GetFileName(_watermark.SourcePath)}";

    public string SelectionContextText
    {
        get
        {
            var selected = FindSelected();
            return selected is null
                ? $"No timeline clip selected; playhead frame {CurrentFrame}."
                : $"Selected {selected.Value.Kind} clip {selected.Value.Clip.MediaId.Value[..Math.Min(8, selected.Value.Clip.MediaId.Value.Length)]}; frames {selected.Value.Clip.TimelineRange.StartFrame}–{selected.Value.Clip.TimelineRange.EndFrameExclusive - 1}; playhead {CurrentFrame}.";
        }
    }

    public string? SelectedMediaId => FindSelected()?.Clip.MediaId.Value;

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

    public void AddBeatMarkers(MediaId mediaId, IReadOnlyList<float> waveformPeaks)
    {
        ArgumentNullException.ThrowIfNull(waveformPeaks);
        _beatMarkerMediaId = mediaId;
        _beatMarkerWaveform = waveformPeaks.ToArray();
        var clip = _audioClips.FirstOrDefault(candidate => candidate.MediaId == mediaId);
        if (clip is null || waveformPeaks.Count == 0)
        {
            return;
        }

        _beatMarkers.Clear();
        for (var index = 0; index < waveformPeaks.Count; index++)
        {
            var peak = waveformPeaks[index];
            var previous = index == 0 ? peak : waveformPeaks[index - 1];
            var next = index == waveformPeaks.Count - 1 ? peak : waveformPeaks[index + 1];
            if (float.IsNaN(peak) || float.IsInfinity(peak) || !IsBeatCandidate(peak, previous, next, index))
            {
                continue;
            }

            var frame = clip.TimelineRange.StartFrame + (long)index * clip.TimelineRange.Length / waveformPeaks.Count;
            if (_beatMarkers.LastOrDefault(-4) + 3 <= frame)
            {
                _beatMarkers.Add(frame);
            }
        }

        OnPropertyChanged(nameof(BeatMarkers));
        OnPropertyChanged(nameof(BeatMarkersText));
    }

    private bool IsBeatCandidate(float peak, float previous, float next, int index) => BeatDetectionMode switch
    {
        BeatDetectionMode.AmplitudePeaks => peak >= 0.6f && peak >= previous && peak >= next,
        BeatDetectionMode.EnergyOnsets => index > 0 && peak >= 0.35f && peak - previous >= 0.2f && peak >= next,
        _ => false
    };

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
                animation: _animations.TryGetValue(clip, out var animation) ? animation : null,
                speedRamp: _speedRamps.TryGetValue(clip, out var speedRamp) ? speedRamp : null,
                stabilization: _stabilization.TryGetValue(clip, out var stabilization) ? stabilization : null)))],
            audioLayers,
            _titles,
            _captions,
            _captionFontPath is null ? null : new TimelineCaptionStyle(_captionFontPath, kineticWords: KineticCaptions),
            _colorGrades,
            _beforeAfterStyle,
            TotalFrames / 2,
            _watermark);
    }

    public TimelineProjectState CaptureProjectState() => new()
    {
        FrameRateNumerator = _projectFrameRate.Numerator,
        FrameRateDenominator = _projectFrameRate.Denominator,
        VideoClips = _videoClips.Select(ToProjectState).ToList(),
        AudioClips = _audioClips.Select(ToProjectState).ToList(),
        VideoSettings = _animations.Keys
            .Concat(_transitions.Keys)
            .Concat(_staticTransforms.Keys)
            .Concat(_speedRamps.Keys)
            .Concat(_stabilization.Keys)
            .Distinct()
            .Select(clip => new TimelineVideoSettingsProjectState
            {
                Clip = ToProjectState(clip),
                Transform = _staticTransforms.TryGetValue(clip, out var transform) ? ToProjectState(transform) : null,
                Transition = _transitions.TryGetValue(clip, out var transition) ? transition.Kind : null,
                TransitionDurationFrames = _transitions.TryGetValue(clip, out transition) ? transition.DurationFrames : 0,
                Animation = _animations.TryGetValue(clip, out var animation) ? ToProjectState(animation) : null,
                SpeedRamp = _speedRamps.TryGetValue(clip, out var speedRamp) ? ToProjectState(speedRamp) : null,
                Stabilization = _stabilization.TryGetValue(clip, out var stabilization) ? ToProjectState(stabilization) : null
            })
            .ToList(),
        AudioSettings = _audioSettings.Values.Select(settings => new TimelineAudioSettingsProjectState
        {
            Clip = ToProjectState(settings.Clip),
            GainDb = settings.GainDb,
            Muted = settings.Muted,
            FadeInFrames = settings.FadeInFrames,
            FadeOutFrames = settings.FadeOutFrames,
            DuckingDb = settings.DuckingDb
        }).ToList(),
        ColorGrades = _colorGrades.Select(item => new TimelineColorGradeProjectState
        {
            MediaId = item.Key.Value,
            ExposureEV = item.Value.ExposureEV,
            Contrast = item.Value.Contrast,
            Saturation = item.Value.Saturation,
            Temperature = item.Value.Temperature,
            Tint = item.Value.Tint
        }).ToList(),
        Titles = _titles.Select(ToProjectState).ToList(),
        Captions = _captions.Select(caption => new TimelineCaptionProjectState
        {
            Sequence = caption.Sequence,
            StartTicks = caption.Start.Ticks,
            EndTicks = caption.End.Ticks,
            Text = caption.Text,
            Provenance = caption.Provenance,
            Words = caption.Words.Select(word => new TimelineCaptionWordProjectState
            {
                StartTicks = word.Start.Ticks,
                EndTicks = word.End.Ticks,
                Text = word.Text
            }).ToList()
        }).ToList(),
        KineticCaptions = KineticCaptions,
        BeforeAfterStyle = _beforeAfterStyle,
        Watermark = _watermark is null ? null : new TimelineWatermarkProjectState
        {
            SourcePath = BrandAssetCatalog.IsApprovedLogoPath(_watermark.SourcePath)
                ? BrandAssetCatalog.ApprovedLogoRelativePath
                : _watermark.SourcePath,
            IsApprovedAsset = BrandAssetCatalog.IsApprovedLogoPath(_watermark.SourcePath),
            Opacity = _watermark.Opacity,
            Margin = _watermark.Margin
        },
        TranscriptSourceFingerprint = _transcriptSourceFingerprint,
        CaptionFontPath = _captionFontPath,
        CurrentFrame = CurrentFrame,
        ScrollFrame = ScrollFrame,
        FramesPerPixel = FramesPerPixel,
        SnapEnabled = SnapEnabled,
        BeatMarkers = _beatMarkers.ToList(),
        BeatMarkerMediaId = _beatMarkerMediaId?.Value,
        BeatMarkerWaveform = _beatMarkerWaveform.ToList(),
        BeatDetectionMode = BeatDetectionMode,
        SelectedTrackId = _selectedTrackId,
        SelectedClipStart = _selectedClipStart
    };

    public void RestoreProjectState(
        TimelineProjectState state,
        IReadOnlyDictionary<string, string>? mediaIdMap = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.SchemaVersion != 1 || state.FrameRateNumerator != _projectFrameRate.Numerator ||
            state.FrameRateDenominator != _projectFrameRate.Denominator)
        {
            throw new InvalidDataException("The timeline state schema or frame rate is unsupported.");
        }

        var videoClips = state.VideoClips.Select(clip => FromProjectState(clip, mediaIdMap)).ToArray();
        var audioClips = state.AudioClips.Select(clip => FromProjectState(clip, mediaIdMap)).ToArray();
        if (videoClips.Any(clip => !clip.IsVideo) || audioClips.Any(clip => !clip.IsAudio))
        {
            throw new InvalidDataException("The timeline contains a clip on the wrong track.");
        }

        var videoSettings = new Dictionary<TimelineClip, TimelineVideoSettingsProjectState>();
        foreach (var settings in state.VideoSettings)
        {
            var clip = FromProjectState(settings.Clip, mediaIdMap);
            var actual = videoClips.SingleOrDefault(candidate => candidate.Equals(clip));
            if (actual is null || !actual.IsVideo)
            {
                throw new InvalidDataException("Timeline video settings reference an unknown clip.");
            }

            videoSettings[actual] = settings;
        }

        var audioSettings = new Dictionary<TimelineClip, TimelineAudioSettingsProjectState>();
        foreach (var settings in state.AudioSettings)
        {
            var clip = FromProjectState(settings.Clip, mediaIdMap);
            var actual = audioClips.SingleOrDefault(candidate => candidate.Equals(clip));
            if (actual is null || !actual.IsAudio)
            {
                throw new InvalidDataException("Timeline audio settings reference an unknown clip.");
            }

            audioSettings[actual] = settings;
        }

        var titles = state.Titles.Select(FromProjectState).ToArray();
        var captions = state.Captions.Select(caption => new TimedCaption(
            caption.Sequence,
            TimeSpan.FromTicks(caption.StartTicks),
            TimeSpan.FromTicks(caption.EndTicks),
            caption.Text,
            caption.Provenance,
            caption.Words.Select(word => new TimedCaptionWord(
                word.Text,
                TimeSpan.FromTicks(word.StartTicks),
                TimeSpan.FromTicks(word.EndTicks))))).ToArray();
        var watermarkPath = state.Watermark?.IsApprovedAsset == true && BrandAssetCatalog.TryGetApprovedLogoPath(out var approvedPath)
            ? approvedPath
            : state.Watermark?.SourcePath;
        var watermark = state.Watermark is null
            ? null
            : new TimelineWatermark(watermarkPath!, state.Watermark.Opacity, state.Watermark.Margin);
        if (!Enum.IsDefined(state.BeatDetectionMode) ||
            (state.BeforeAfterStyle is { } beforeAfterStyle && !Enum.IsDefined(beforeAfterStyle)) ||
            double.IsNaN(state.FramesPerPixel) || double.IsInfinity(state.FramesPerPixel) || state.FramesPerPixel <= 0)
        {
            throw new InvalidDataException("The timeline view or composition state is invalid.");
        }

        var restoredAnimations = new Dictionary<TimelineClip, TimelineTransformKeyframes>();
        var restoredTransitions = new Dictionary<TimelineClip, TimelineTransitionSettings>();
        var restoredStaticTransforms = new Dictionary<TimelineClip, TimelineVideoTransform>();
        var restoredSpeedRamps = new Dictionary<TimelineClip, TimelineSpeedRamp>();
        var restoredStabilization = new Dictionary<TimelineClip, TimelineStabilizationSettings>();
        foreach (var settings in videoSettings)
        {
            if (settings.Value.Animation is not null)
            {
                restoredAnimations[settings.Key] = FromProjectState(settings.Value.Animation);
            }

            if (settings.Value.Transition is not null)
            {
                if (!Enum.IsDefined(settings.Value.Transition.Value))
                {
                    throw new InvalidDataException("The timeline transition state is invalid.");
                }

                restoredTransitions[settings.Key] = new TimelineTransitionSettings(
                    settings.Value.Transition.Value,
                    settings.Value.TransitionDurationFrames);
            }

            if (settings.Value.Transform is not null)
            {
                restoredStaticTransforms[settings.Key] = FromProjectState(settings.Value.Transform);
            }

            if (settings.Value.SpeedRamp is not null)
            {
                restoredSpeedRamps[settings.Key] = FromProjectState(settings.Value.SpeedRamp, settings.Key);
            }

            if (settings.Value.Stabilization is not null)
            {
                restoredStabilization[settings.Key] = FromProjectState(settings.Value.Stabilization);
            }
        }

        var restoredAudioSettings = new Dictionary<TimelineClip, TimelineAudioSegment>();
        foreach (var settings in audioSettings)
        {
            restoredAudioSettings[settings.Key] = new TimelineAudioSegment(
                settings.Key,
                settings.Value.GainDb,
                settings.Value.Muted,
                settings.Value.FadeInFrames,
                settings.Value.FadeOutFrames,
                settings.Value.DuckingDb);
        }

        var restoredColorGrades = new Dictionary<MediaId, ColorGrade>();
        foreach (var grade in state.ColorGrades)
        {
            restoredColorGrades[MapMediaId(grade.MediaId, mediaIdMap)] = new ColorGrade(
                grade.ExposureEV,
                grade.Contrast,
                grade.Saturation,
                grade.Temperature,
                grade.Tint);
        }

        _ = new TimelineTrack("Video 1", TimelineTrackKind.Video, videoClips);
        _ = new TimelineTrack("Audio 1", TimelineTrackKind.Audio, audioClips);
        MediaId? restoredBeatMarkerMediaId = state.BeatMarkerMediaId is null
            ? null
            : MapMediaId(state.BeatMarkerMediaId, mediaIdMap);

        _videoClips.Clear();
        _videoClips.AddRange(videoClips);
        _audioClips.Clear();
        _audioClips.AddRange(audioClips);
        _animations.Clear();
        foreach (var settings in restoredAnimations)
        {
            _animations[settings.Key] = settings.Value;
        }

        _transitions.Clear();
        foreach (var settings in restoredTransitions)
        {
            _transitions[settings.Key] = settings.Value;
        }

        _staticTransforms.Clear();
        foreach (var settings in restoredStaticTransforms)
        {
            _staticTransforms[settings.Key] = settings.Value;
        }

        _speedRamps.Clear();
        foreach (var settings in restoredSpeedRamps)
        {
            _speedRamps[settings.Key] = settings.Value;
        }

        _stabilization.Clear();
        foreach (var settings in restoredStabilization)
        {
            _stabilization[settings.Key] = settings.Value;
        }

        _audioSettings.Clear();
        foreach (var settings in restoredAudioSettings)
        {
            _audioSettings[settings.Key] = settings.Value;
        }

        _colorGrades.Clear();
        foreach (var grade in restoredColorGrades)
        {
            _colorGrades[grade.Key] = grade.Value;
        }

        _titles.Clear();
        _titles.AddRange(titles);
        _captions = captions;
        _kineticCaptions = state.KineticCaptions;
        _beforeAfterStyle = state.BeforeAfterStyle;
        _watermark = watermark;
        _transcriptSourceFingerprint = state.TranscriptSourceFingerprint;
        _captionFontPath = state.CaptionFontPath;
        _selectedTrackId = state.SelectedTrackId;
        _selectedClipStart = state.SelectedClipStart;
        _mediaIds.Clear();
        _mediaIds.UnionWith(videoClips.Concat(audioClips).Select(clip => clip.MediaId));
        _beatMarkers.Clear();
        _beatMarkers.AddRange(state.BeatMarkers);
        _beatMarkerMediaId = restoredBeatMarkerMediaId;
        _beatMarkerWaveform = state.BeatMarkerWaveform.ToArray();
        _beatDetectionMode = state.BeatDetectionMode;
        SnapEnabled = state.SnapEnabled;
        SetZoom(state.FramesPerPixel);
        _aiUndo.Clear();
        Refresh();
        _scrollFrame = Math.Clamp(state.ScrollFrame, 0, Math.Max(0, TotalFrames - 1));
        SeekFrame(state.CurrentFrame);
        OnPropertyChanged(nameof(BeatMarkers));
        OnPropertyChanged(nameof(BeatMarkersText));
        OnPropertyChanged(nameof(Watermark));
        OnPropertyChanged(nameof(WatermarkStatusText));
        OnPropertyChanged(nameof(Captions));
        OnPropertyChanged(nameof(TranscriptSourceFingerprint));
        OnPropertyChanged(nameof(KineticCaptions));
    }

    public void ApplyBeforeAfterStyle(TimelineRevealStyle style)
    {
        if (_videoClips.Count == 0)
        {
            throw new InvalidOperationException("Before/after composition requires at least one video clip.");
        }

        _beforeAfterStyle = style;
    }

    public void SetWatermark(string sourcePath)
    {
        _watermark = new TimelineWatermark(sourcePath);
        OnPropertyChanged(nameof(Watermark));
        OnPropertyChanged(nameof(WatermarkStatusText));
    }

    public void SetApprovedWatermark()
    {
        if (!BrandAssetCatalog.TryGetApprovedLogoPath(out var path))
        {
            throw new InvalidOperationException("The approved Frame Theory watermark is unavailable in this build.");
        }

        SetWatermark(path);
    }

    public void ClearWatermark()
    {
        _watermark = null;
        OnPropertyChanged(nameof(Watermark));
        OnPropertyChanged(nameof(WatermarkStatusText));
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

    public void ApplySpeedRampToSelected(double startSpeed, double endSpeed)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsVideo)
        {
            throw new InvalidOperationException("Speed ramps require a video clip.");
        }

        _speedRamps[selected.Clip] = new TimelineSpeedRamp([
            new TimelineSpeedPoint(selected.Clip.TimelineRange.StartFrame, startSpeed),
            new TimelineSpeedPoint(selected.Clip.TimelineRange.EndFrameExclusive, endSpeed)
        ]);
    }

    public void ApplyStabilizationToSelected(int searchRadius = 8)
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        if (!selected.Clip.IsVideo)
        {
            throw new InvalidOperationException("Stabilization requires a video clip.");
        }

        _stabilization[selected.Clip] = new TimelineStabilizationSettings(searchRadius);
    }

    public void ClearSelectedStabilization()
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        _stabilization.Remove(selected.Clip);
    }

    public void ApplyRoughCut(RoughCutPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var before = CaptureAiState();
        var beforeBeatMarkers = _beatMarkers.ToArray();
        try
        {
            var oldVideo = _videoClips.ToDictionary(clip => clip.MediaId.Value, StringComparer.Ordinal);
            var oldAudio = _audioClips.ToDictionary(clip => clip.MediaId.Value, StringComparer.Ordinal);
            if (plan.Segments.Count != oldVideo.Count || plan.Segments.Any(segment => !oldVideo.ContainsKey(segment.MediaId)))
            {
                throw new InvalidOperationException("The rough-cut plan does not match the current video timeline.");
            }

            var newVideo = new Dictionary<string, TimelineClip>(StringComparer.Ordinal);
            var newAudio = new Dictionary<string, TimelineClip>(StringComparer.Ordinal);
            foreach (var segment in plan.Segments)
            {
                var video = RemapClip(oldVideo[segment.MediaId], segment.StartFrame, segment.DurationFrames);
                newVideo.Add(segment.MediaId, video);
                if (oldAudio.TryGetValue(segment.MediaId, out var audio))
                {
                    newAudio.Add(segment.MediaId, RemapClip(audio, segment.StartFrame, segment.DurationFrames));
                }
            }

            if (newAudio.Count != oldAudio.Count)
            {
                throw new InvalidOperationException("The rough cut requires every audio clip to be linked to a planned video clip.");
            }

            _animations.Clear();
            _staticTransforms.Clear();
            _speedRamps.Clear();
            _stabilization.Clear();
            _transitions.Clear();
            _audioSettings.Clear();
            for (var segmentIndex = 0; segmentIndex < plan.Segments.Count; segmentIndex++)
            {
                var segment = plan.Segments[segmentIndex];
                var oldVideoClip = oldVideo[segment.MediaId];
                var videoClip = newVideo[segment.MediaId];
                var frameOffset = checked(videoClip.TimelineRange.StartFrame - oldVideoClip.TimelineRange.StartFrame);
                if (before.Animations.TryGetValue(oldVideoClip, out var animation))
                {
                    _animations[videoClip] = TranslateAnimation(animation, frameOffset);
                }

                if (before.StaticTransforms.TryGetValue(oldVideoClip, out var transform))
                {
                    _staticTransforms[videoClip] = transform;
                }

                if (before.SpeedRamps.TryGetValue(oldVideoClip, out var speedRamp))
                {
                    _speedRamps[videoClip] = TranslateSpeedRamp(speedRamp, frameOffset);
                }

                if (before.Stabilization.TryGetValue(oldVideoClip, out var stabilization))
                {
                    _stabilization[videoClip] = stabilization;
                }

                if (segmentIndex > 0 && before.Transitions.TryGetValue(oldVideoClip, out var transition) && transition.DurationFrames < videoClip.TimelineRange.Length)
                {
                    _transitions[videoClip] = transition;
                }

                if (newAudio.TryGetValue(segment.MediaId, out var audioClip) && before.AudioSettings.TryGetValue(oldAudio[segment.MediaId], out var audioSettings))
                {
                    _audioSettings[audioClip] = new TimelineAudioSegment(
                        audioClip,
                        audioSettings.GainDb,
                        audioSettings.Muted,
                        Math.Min(audioSettings.FadeInFrames, audioClip.TimelineRange.Length - 1),
                        Math.Min(audioSettings.FadeOutFrames, audioClip.TimelineRange.Length - 1),
                        audioSettings.DuckingDb);
                }
            }

            ApplyClips(
                plan.Segments.Select(segment => newVideo[segment.MediaId]),
                plan.Segments.Where(segment => newAudio.ContainsKey(segment.MediaId)).Select(segment => newAudio[segment.MediaId]));
            if (_beatMarkerMediaId is { } beatMediaId && oldVideo.TryGetValue(beatMediaId.Value, out var oldBeatClip))
            {
                var newBeatClip = newVideo[beatMediaId.Value];
                var offset = newBeatClip.TimelineRange.StartFrame - oldBeatClip.TimelineRange.StartFrame;
                _beatMarkers.Clear();
                _beatMarkers.AddRange(beforeBeatMarkers.Select(marker => checked(marker + offset)));
            }

            _selectedTrackId = null;
            _selectedClipStart = null;
            CommitAiState(before);
        }
        catch
        {
            _beatMarkers.Clear();
            _beatMarkers.AddRange(beforeBeatMarkers);
            RestoreAiStateForAi(before);
            throw;
        }
    }

    public void ClearSelectedSpeedRamp()
    {
        var selected = FindSelected() ?? throw new InvalidOperationException("A timeline clip must be selected.");
        _speedRamps.Remove(selected.Clip);
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
        new Dictionary<TimelineClip, TimelineSpeedRamp>(_speedRamps),
        new Dictionary<TimelineClip, TimelineStabilizationSettings>(_stabilization),
        new Dictionary<MediaId, ColorGrade>(_colorGrades),
        _beforeAfterStyle,
        _watermark,
        _titles.ToArray(),
        _captions.ToArray(),
        _transcriptSourceFingerprint,
        _captionFontPath,
        _mediaIds.ToHashSet(),
        _beatMarkers.ToArray(),
        _beatMarkerMediaId,
        _beatMarkerWaveform.ToArray(),
        _beatDetectionMode,
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

        _speedRamps.Clear();
        foreach (var item in state.SpeedRamps)
        {
            _speedRamps[item.Key] = item.Value;
        }

        _stabilization.Clear();
        foreach (var item in state.Stabilization)
        {
            _stabilization[item.Key] = item.Value;
        }

        _colorGrades.Clear();
        foreach (var item in state.ColorGrades)
        {
            _colorGrades[item.Key] = item.Value;
        }

        _beforeAfterStyle = state.BeforeAfterStyle;
        _watermark = state.Watermark;

        _titles.Clear();
        _titles.AddRange(state.Titles);
        _captions = state.Captions;
        _transcriptSourceFingerprint = state.TranscriptSourceFingerprint;
        _captionFontPath = state.CaptionFontPath;
        _mediaIds.Clear();
        _mediaIds.UnionWith(state.MediaIds);
        _beatMarkers.Clear();
        _beatMarkers.AddRange(state.BeatMarkers);
        _beatMarkerMediaId = state.BeatMarkerMediaId;
        _beatMarkerWaveform = state.BeatMarkerWaveform.ToArray();
        _beatDetectionMode = state.BeatDetectionMode;
        _selectedTrackId = state.SelectedTrackId;
        _selectedClipStart = state.SelectedClipStart;
        Refresh();
        SeekFrame(state.CurrentFrame);
        OnPropertyChanged(nameof(Watermark));
        OnPropertyChanged(nameof(WatermarkStatusText));
        OnPropertyChanged(nameof(Captions));
        OnPropertyChanged(nameof(TranscriptSourceFingerprint));
        OnPropertyChanged(nameof(BeatMarkers));
        OnPropertyChanged(nameof(BeatMarkersText));
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

    public string? TranscriptSourceFingerprint => _transcriptSourceFingerprint;

    public void ImportCaptions(string content)
    {
        _captions = SrtCaptionFile.Import(content);
        _transcriptSourceFingerprint = null;
        KineticCaptions = false;
        OnPropertyChanged(nameof(Captions));
        OnPropertyChanged(nameof(TranscriptSourceFingerprint));
    }

    public void ImportTranscript(TranscriptResult transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        _captions = transcript.Words
            .Chunk(7)
            .Select((words, index) => new TimedCaption(
                index + 1,
                words[0].Start,
                words[^1].End,
                string.Join(' ', words.Select(word => word.Text)),
                CaptionProvenance.VerifiedTranscript,
                words.Select(word => new TimedCaptionWord(word.Text, word.Start, word.End))))
            .ToArray();
        _transcriptSourceFingerprint = transcript.SourceFingerprint;
        OnPropertyChanged(nameof(Captions));
        OnPropertyChanged(nameof(TranscriptSourceFingerprint));
    }

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
        OnPropertyChanged(nameof(CurrentTimecode));
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
        OnPropertyChanged(nameof(SelectionContextText));
        OnPropertyChanged(nameof(SelectedMediaId));
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

    private static TimelineClipProjectState ToProjectState(TimelineClip clip) => new()
    {
        MediaId = clip.MediaId.Value,
        SourceStartUnit = clip.SourceStartUnit,
        SourceUnitCount = clip.SourceUnitCount,
        TimelineStartFrame = clip.TimelineRange.StartFrame,
        TimelineDurationFrames = clip.TimelineRange.Length,
        SourceFrameRateNumerator = clip.SourceFrameRate?.Numerator,
        SourceFrameRateDenominator = clip.SourceFrameRate?.Denominator,
        AudioSampleRate = clip.AudioSampleRate,
        SourceStartSample = clip.SourceStartSample
    };

    private static TimelineTransformProjectState ToProjectState(TimelineVideoTransform transform) => new()
    {
        Opacity = transform.Opacity,
        Scale = transform.Scale,
        OffsetX = transform.OffsetX,
        OffsetY = transform.OffsetY
    };

    private static TimelineAnimationProjectState ToProjectState(TimelineTransformKeyframes animation) => new()
    {
        Keyframes = animation.Keyframes.Select(keyframe => new TimelineKeyframeProjectState
        {
            Frame = keyframe.Frame,
            Value = ToProjectState(keyframe.Value),
            Easing = keyframe.Easing
        }).ToList()
    };

    private static TimelineSpeedRampProjectState ToProjectState(TimelineSpeedRamp speedRamp) => new()
    {
        Points = speedRamp.Points.Select(point => new TimelineSpeedPointProjectState
        {
            Frame = point.Frame,
            Speed = point.Speed
        }).ToList()
    };

    private static TimelineStabilizationProjectState ToProjectState(TimelineStabilizationSettings stabilization) => new()
    {
        SearchRadius = stabilization.SearchRadius
    };

    private static TimelineTitleProjectState ToProjectState(TimelineTitle title) => new()
    {
        Text = title.Text,
        FontPath = title.FontPath,
        StartFrame = title.TimelineRange.StartFrame,
        DurationFrames = title.TimelineRange.Length,
        FontSize = title.FontSize,
        ColorRgb = title.ColorRgb,
        Placement = title.Placement,
        Order = title.Order,
        Animation = title.Animation is null ? null : ToProjectState(title.Animation)
    };

    private static TimelineClip FromProjectState(
        TimelineClipProjectState state,
        IReadOnlyDictionary<string, string>? mediaIdMap)
    {
        var mediaId = MapMediaId(state.MediaId, mediaIdMap);
        if (state.SourceFrameRateNumerator is int numerator && state.SourceFrameRateDenominator is int denominator)
        {
            return TimelineClip.Video(
                mediaId,
                state.SourceStartUnit,
                state.SourceUnitCount,
                state.TimelineStartFrame,
                state.TimelineDurationFrames,
                new RationalFrameRate(numerator, denominator));
        }

        return TimelineClip.Audio(
            mediaId,
            state.SourceStartSample,
            state.SourceUnitCount,
            state.TimelineStartFrame,
            state.TimelineDurationFrames,
            state.AudioSampleRate);
    }

    private static TimelineVideoTransform FromProjectState(TimelineTransformProjectState state) =>
        new(state.Opacity, state.Scale, state.OffsetX, state.OffsetY);

    private static TimelineTransformKeyframes FromProjectState(TimelineAnimationProjectState state) =>
        new(state.Keyframes.Select(keyframe => new TimelineTransformKeyframe(
            keyframe.Frame,
            FromProjectState(keyframe.Value),
            keyframe.Easing)));

    private static TimelineSpeedRamp FromProjectState(TimelineSpeedRampProjectState state, TimelineClip clip)
    {
        var ramp = new TimelineSpeedRamp(state.Points.Select(point => new TimelineSpeedPoint(point.Frame, point.Speed)));
        if (ramp.Points[0].Frame != clip.TimelineRange.StartFrame || ramp.Points[^1].Frame != clip.TimelineRange.EndFrameExclusive)
        {
            throw new InvalidDataException("The timeline speed ramp does not fit its clip.");
        }

        return ramp;
    }

    private static TimelineStabilizationSettings FromProjectState(TimelineStabilizationProjectState state) =>
        new(state.SearchRadius);

    private static TimelineTitle FromProjectState(TimelineTitleProjectState state) =>
        new(
            state.Text,
            state.FontPath,
            state.StartFrame,
            state.DurationFrames,
            state.FontSize,
            state.ColorRgb,
            state.Placement,
            state.Order,
            state.Animation is null ? null : FromProjectState(state.Animation));

    private static MediaId MapMediaId(string mediaId, IReadOnlyDictionary<string, string>? mediaIdMap)
    {
        if (mediaIdMap is not null && mediaIdMap.TryGetValue(mediaId, out var mappedMediaId))
        {
            mediaId = mappedMediaId;
        }

        return MediaId.Parse(mediaId);
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

    private TimelineClip RemapClip(TimelineClip clip, long timelineStartFrame, long timelineDuration)
    {
        if (timelineDuration <= 0 || timelineDuration > clip.TimelineRange.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(timelineDuration));
        }

        var sourceCount = clip.SourceUnitCount;
        if (timelineDuration < clip.TimelineRange.Length)
        {
            var sourceEnd = SourceUnitAt(clip, clip.TimelineRange.StartFrame + timelineDuration - 1);
            sourceCount = Math.Max(1, sourceEnd - clip.SourceStartUnit + 1);
        }

        return CreateClip(clip, timelineStartFrame, timelineDuration, clip.SourceStartUnit, sourceCount);
    }

    private static TimelineTransformKeyframes TranslateAnimation(TimelineTransformKeyframes animation, long frameOffset) =>
        new(animation.Keyframes.Select(keyframe => new TimelineTransformKeyframe(
            checked(keyframe.Frame + frameOffset),
            keyframe.Value,
            keyframe.Easing)));

    private static TimelineSpeedRamp TranslateSpeedRamp(TimelineSpeedRamp speedRamp, long frameOffset) =>
        new(speedRamp.Points.Select(point => new TimelineSpeedPoint(checked(point.Frame + frameOffset), point.Speed)));

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
    IReadOnlyDictionary<TimelineClip, TimelineSpeedRamp> SpeedRamps,
    IReadOnlyDictionary<TimelineClip, TimelineStabilizationSettings> Stabilization,
    IReadOnlyDictionary<MediaId, ColorGrade> ColorGrades,
    TimelineRevealStyle? BeforeAfterStyle,
    TimelineWatermark? Watermark,
    IReadOnlyList<TimelineTitle> Titles,
    IReadOnlyList<TimedCaption> Captions,
    string? TranscriptSourceFingerprint,
    string? CaptionFontPath,
    IReadOnlySet<MediaId> MediaIds,
    IReadOnlyList<long> BeatMarkers,
    MediaId? BeatMarkerMediaId,
    IReadOnlyList<float> BeatMarkerWaveform,
    BeatDetectionMode BeatDetectionMode,
    string? SelectedTrackId,
    long? SelectedClipStart,
    long CurrentFrame);
