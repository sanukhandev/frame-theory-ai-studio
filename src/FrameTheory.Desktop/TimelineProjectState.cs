using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Captions;
using FrameTheory.Domain;

namespace FrameTheory.Desktop;

public sealed class TimelineProjectState
{
    public int SchemaVersion { get; set; } = 1;

    public int FrameRateNumerator { get; set; }

    public int FrameRateDenominator { get; set; }

    public List<TimelineClipProjectState> VideoClips { get; set; } = [];

    public List<TimelineClipProjectState> AudioClips { get; set; } = [];

    public List<TimelineVideoSettingsProjectState> VideoSettings { get; set; } = [];

    public List<TimelineAudioSettingsProjectState> AudioSettings { get; set; } = [];

    public List<TimelineColorGradeProjectState> ColorGrades { get; set; } = [];

    public List<TimelineTitleProjectState> Titles { get; set; } = [];

    public List<TimelineCaptionProjectState> Captions { get; set; } = [];

    public TimelineRevealStyle? BeforeAfterStyle { get; set; }

    public TimelineWatermarkProjectState? Watermark { get; set; }

    public string? TranscriptSourceFingerprint { get; set; }

    public string? CaptionFontPath { get; set; }

    public long CurrentFrame { get; set; }

    public long ScrollFrame { get; set; }

    public double FramesPerPixel { get; set; } = 1;

    public bool SnapEnabled { get; set; } = true;

    public List<long> BeatMarkers { get; set; } = [];

    public string? BeatMarkerMediaId { get; set; }

    public List<float> BeatMarkerWaveform { get; set; } = [];

    public BeatDetectionMode BeatDetectionMode { get; set; }

    public string? SelectedTrackId { get; set; }

    public long? SelectedClipStart { get; set; }
}

public sealed class TimelineClipProjectState
{
    public string MediaId { get; set; } = string.Empty;

    public long SourceStartUnit { get; set; }

    public long SourceUnitCount { get; set; }

    public long TimelineStartFrame { get; set; }

    public long TimelineDurationFrames { get; set; }

    public int? SourceFrameRateNumerator { get; set; }

    public int? SourceFrameRateDenominator { get; set; }

    public int AudioSampleRate { get; set; }

    public long SourceStartSample { get; set; }
}

public sealed class TimelineTransformProjectState
{
    public double Opacity { get; set; }

    public double Scale { get; set; }

    public int OffsetX { get; set; }

    public int OffsetY { get; set; }
}

public sealed class TimelineKeyframeProjectState
{
    public long Frame { get; set; }

    public TimelineTransformProjectState Value { get; set; } = new();

    public TimelineEasing Easing { get; set; }
}

public sealed class TimelineAnimationProjectState
{
    public List<TimelineKeyframeProjectState> Keyframes { get; set; } = [];
}

public sealed class TimelineVideoSettingsProjectState
{
    public TimelineClipProjectState Clip { get; set; } = new();

    public TimelineTransformProjectState? Transform { get; set; }

    public TimelineTransitionKind? Transition { get; set; }

    public long TransitionDurationFrames { get; set; }

    public TimelineAnimationProjectState? Animation { get; set; }
}

public sealed class TimelineAudioSettingsProjectState
{
    public TimelineClipProjectState Clip { get; set; } = new();

    public double GainDb { get; set; }

    public bool Muted { get; set; }

    public long FadeInFrames { get; set; }

    public long FadeOutFrames { get; set; }

    public double DuckingDb { get; set; }
}

public sealed class TimelineColorGradeProjectState
{
    public string MediaId { get; set; } = string.Empty;

    public float ExposureEV { get; set; }

    public float Contrast { get; set; }

    public float Saturation { get; set; }

    public float Temperature { get; set; }

    public float Tint { get; set; }
}

public sealed class TimelineTitleProjectState
{
    public string Text { get; set; } = string.Empty;

    public string FontPath { get; set; } = string.Empty;

    public long StartFrame { get; set; }

    public long DurationFrames { get; set; }

    public int FontSize { get; set; }

    public uint ColorRgb { get; set; }

    public TimelineTitlePlacement Placement { get; set; }

    public int Order { get; set; }

    public TimelineAnimationProjectState? Animation { get; set; }
}

public sealed class TimelineCaptionProjectState
{
    public int Sequence { get; set; }

    public long StartTicks { get; set; }

    public long EndTicks { get; set; }

    public string Text { get; set; } = string.Empty;

    public CaptionProvenance Provenance { get; set; }
}

public sealed class TimelineWatermarkProjectState
{
    public string SourcePath { get; set; } = string.Empty;

    public float Opacity { get; set; }

    public int Margin { get; set; }
}

public static class TimelineProjectStateJson
{
    private const int MaximumBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 32
    };

    public static string Serialize(TimelineProjectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(state, JsonOptions);
    }

    public static TimelineProjectState Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaximumBytes)
        {
            throw new InvalidDataException("The timeline state is empty or exceeds the supported size limit.");
        }

        try
        {
            var state = JsonSerializer.Deserialize<TimelineProjectState>(json, JsonOptions)
                ?? throw new InvalidDataException("The timeline state is empty.");
            Validate(state);
            return state;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The timeline state JSON is malformed.", exception);
        }
    }

    private static void Validate(TimelineProjectState state)
    {
        if (state.VideoClips is null || state.AudioClips is null || state.VideoSettings is null ||
            state.AudioSettings is null || state.ColorGrades is null || state.Titles is null ||
            state.Captions is null || state.BeatMarkers is null || state.BeatMarkerWaveform is null ||
            state.VideoClips.Any(clip => clip is null) || state.AudioClips.Any(clip => clip is null) ||
            state.VideoSettings.Any(settings => settings is null || settings.Clip is null) ||
            state.AudioSettings.Any(settings => settings is null || settings.Clip is null) ||
            state.ColorGrades.Any(grade => grade is null) || state.Titles.Any(title => title is null) ||
            state.Captions.Any(caption => caption is null))
        {
            throw new InvalidDataException("The timeline state contains null collections or entries.");
        }

        if (state.VideoSettings.Any(settings => settings.Animation is not null &&
            (settings.Animation.Keyframes is null || settings.Animation.Keyframes.Any(keyframe => keyframe is null || keyframe.Value is null))))
        {
            throw new InvalidDataException("The timeline animation state is incomplete.");
        }
    }
}
