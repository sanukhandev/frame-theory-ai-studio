using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.Domain;

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

public sealed class TimelineEditorViewModel : INotifyPropertyChanged
{
    private const int AudioSampleRate = 48000;
    private readonly RationalFrameRate _projectFrameRate;
    private readonly List<TimelineClip> _videoClips = [];
    private readonly List<TimelineClip> _audioClips = [];
    private readonly HashSet<MediaId> _mediaIds = [];
    private TimelineGraph _graph;
    private TimelineTransport _transport;
    private long? _selectedClipStart;
    private string? _selectedTrackId;
    private double _framesPerPixel = 1;
    private long _scrollFrame;
    private bool _snapEnabled = true;

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
}
