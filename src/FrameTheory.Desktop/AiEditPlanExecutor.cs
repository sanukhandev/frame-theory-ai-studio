using System.Globalization;
using System.Text.Json;
using FrameTheory.AI;
using FrameTheory.Captions;
using FrameTheory.Color;
using FrameTheory.Domain;

namespace FrameTheory.Desktop;

public sealed record AiActionExecutionOutcome(string ActionId, bool Applied, string Message);

public sealed record AiPlanExecutionResult(
    bool IsApplied,
    string? ChangeGroupId,
    IReadOnlyList<AiActionExecutionOutcome> Outcomes,
    string Message);

public static class AiEditPlanExecutor
{
    public static AiPlanExecutionResult Execute(
        IReadOnlyList<AiEditAction> actions,
        TimelineEditorViewModel timeline,
        IReadOnlyDictionary<string, string> approvedFontPaths,
        IReadOnlyDictionary<string, ColorGrade> trustedPresets)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(approvedFontPaths);
        ArgumentNullException.ThrowIfNull(trustedPresets);
        if (actions.Count == 0)
        {
            return new(false, null, [], "No approved AI actions were selected.");
        }

        var before = timeline.CaptureAiState();
        var outcomes = new List<AiActionExecutionOutcome>(actions.Count);
        try
        {
            foreach (var action in actions)
            {
                Apply(action, timeline, approvedFontPaths, trustedPresets);
                outcomes.Add(new(action.Id, true, $"Applied {action.Type}."));
            }

            timeline.CommitAiState(before);
            return new(true, Guid.NewGuid().ToString("N"), outcomes, $"Applied {actions.Count} approved AI action(s) as one undoable change group.");
        }
        catch (Exception exception)
        {
            timeline.RestoreAiStateForAi(before);
            var failedAction = outcomes.Count < actions.Count ? actions[outcomes.Count] : null;
            if (failedAction is not null)
            {
                outcomes.Add(new(failedAction.Id, false, exception.Message));
            }

            return new(false, null, outcomes, $"AI edit group rolled back: {exception.Message}");
        }
    }

    private static void Apply(
        AiEditAction action,
        TimelineEditorViewModel timeline,
        IReadOnlyDictionary<string, string> approvedFontPaths,
        IReadOnlyDictionary<string, ColorGrade> trustedPresets)
    {
        switch (action.Type)
        {
            case "color.adjust":
                timeline.ApplyColorGradeToMedia(action.TargetId, new ColorGrade(
                    Number(action, "exposureEV"),
                    Number(action, "contrast"),
                    Number(action, "saturation"),
                    Number(action, "temperature"),
                    Number(action, "tint")));
                break;
            case "clip.trim":
                SelectClip(timeline, action.TargetId, TimelineTrackKind.Video);
                timeline.TrimSelected(action.StartFrame, checked(action.StartFrame + action.DurationFrames));
                break;
            case "clip.split":
                SelectClip(timeline, action.TargetId, TimelineTrackKind.Video);
                timeline.SplitSelectedAt(action.StartFrame);
                break;
            case "clip.move":
                SelectClip(timeline, action.TargetId, TimelineTrackKind.Video, TimelineTrackKind.Audio);
                timeline.MoveSelectedBy(Integer(action, "offsetFrames"));
                break;
            case "clip.transform":
                SelectClip(timeline, action.TargetId, TimelineTrackKind.Video);
                timeline.ApplyStaticTransformToSelected(Number(action, "scale"), Number(action, "offsetX"), Number(action, "offsetY"), Number(action, "rotation"));
                break;
            case "transition.add":
                SelectClip(timeline, action.TargetId, TimelineTrackKind.Video);
                timeline.ApplyTransitionToSelected(action.Params["kind"].GetString() switch
                {
                    "cross-dissolve" => TimelineTransitionKind.CrossDissolve,
                    "wipe" => TimelineTransitionKind.WipeLeft,
                    _ => throw new InvalidOperationException("The transition kind is not supported.")
                }, action.DurationFrames);
                break;
            case "title.add":
                timeline.AddTitleAt(
                    action.Params["text"].GetString() ?? throw new InvalidOperationException("Title text is required."),
                    ApprovedFont(approvedFontPaths, action.Params["fontId"].GetString()),
                    action.StartFrame,
                    action.DurationFrames,
                    ParsePreset(action.Params["preset"].GetString()));
                break;
            case "caption.add":
                timeline.AddCaptionAt(
                    action.Params["text"].GetString() ?? throw new InvalidOperationException("Caption text is required."),
                    action.Params["provenance"].GetString() switch
                    {
                        "verified-transcript" => CaptionProvenance.VerifiedTranscript,
                        "authored-caption" => CaptionProvenance.AuthoredCaption,
                        _ => throw new InvalidOperationException("Caption provenance is not supported.")
                    },
                    action.StartFrame,
                    action.DurationFrames,
                    ApprovedFont(approvedFontPaths, approvedFontPaths.Keys.FirstOrDefault()));
                break;
            case "animation.keyframes":
                SelectClip(timeline, action.TargetId, TimelineTrackKind.Video);
                timeline.ApplyKeyframesToSelected(ParseKeyframes(action));
                break;
            case "audio.gain":
                SelectClip(timeline, action.TargetId, TimelineTrackKind.Audio);
                timeline.ApplyAudioSettingsToSelected(
                    Number(action, "gainDb"),
                    muted: false,
                    Integer(action, "fadeInFrames"),
                    Integer(action, "fadeOutFrames"),
                    Number(action, "duckingDb"));
                break;
            case "preset.apply":
                if (!trustedPresets.TryGetValue(action.Params["presetId"].GetString() ?? string.Empty, out var grade))
                {
                    throw new KeyNotFoundException("The requested preset is not in the trusted local preset catalog.");
                }

                timeline.ApplyColorGradeToMedia(action.TargetId, grade);
                break;
            case "reel.before_after":
                timeline.ApplyBeforeAfterStyle(action.Params["style"].GetString() switch
                {
                    "split" => TimelineRevealStyle.Split,
                    "wipe" => TimelineRevealStyle.Wipe,
                    "timed-reveal" => TimelineRevealStyle.TimedReveal,
                    _ => throw new InvalidOperationException("The before/after reveal style is not supported.")
                });
                break;
            default:
                throw new InvalidOperationException($"The operation '{action.Type}' is not executable.");
        }
    }

    private static void SelectClip(TimelineEditorViewModel timeline, string mediaId, params TimelineTrackKind[] kinds)
    {
        foreach (var kind in kinds)
        {
            if (timeline.SelectMedia(mediaId, kind))
            {
                return;
            }
        }

        throw new InvalidOperationException($"The target clip '{mediaId}' is not on the timeline.");
    }

    private static string ApprovedFont(IReadOnlyDictionary<string, string> approvedFontPaths, string? fontId)
    {
        if (fontId is null || !approvedFontPaths.TryGetValue(fontId, out var path) ||
            LocalPathSecurity.ContainsReparsePoint(path) || !File.Exists(path))
        {
            throw new InvalidOperationException($"The font ID '{fontId ?? ""}' is not mapped to an existing approved local font.");
        }

        return path;
    }

    private static TimelineAnimationPreset ParsePreset(string? value) => value switch
    {
        "fade-in" => TimelineAnimationPreset.FadeIn,
        "fade-out" => TimelineAnimationPreset.FadeOut,
        "push-in" => TimelineAnimationPreset.PushIn,
        "pull-out" => TimelineAnimationPreset.PullOut,
        _ => throw new InvalidOperationException("The title animation preset is not supported.")
    };

    private static TimelineTransformKeyframes ParseKeyframes(AiEditAction action)
    {
        var keyframes = action.Params["keyframes"].EnumerateArray().Select(frame =>
        {
            if (frame.ValueKind != JsonValueKind.Object ||
                !frame.TryGetProperty("frame", out var frameValue) || !frameValue.TryGetInt64(out var position) ||
                !frame.TryGetProperty("opacity", out var opacityValue) || !opacityValue.TryGetDouble(out var opacity) ||
                !frame.TryGetProperty("scale", out var scaleValue) || !scaleValue.TryGetDouble(out var scale) ||
                !frame.TryGetProperty("offsetX", out var offsetXValue) || !offsetXValue.TryGetInt32(out var offsetX) ||
                !frame.TryGetProperty("offsetY", out var offsetYValue) || !offsetYValue.TryGetInt32(out var offsetY))
            {
                throw new InvalidOperationException("Each animation keyframe requires frame, opacity, scale, offsetX and offsetY.");
            }

            var easing = frame.TryGetProperty("easing", out var easingValue) && easingValue.ValueKind == JsonValueKind.String
                ? Enum.Parse<TimelineEasing>(easingValue.GetString()!, ignoreCase: true)
                : TimelineEasing.Linear;
            return new TimelineTransformKeyframe(position, new TimelineVideoTransform(opacity, scale, offsetX, offsetY), easing);
        });
        return new TimelineTransformKeyframes(keyframes);
    }

    private static float Number(AiEditAction action, string name) => action.Params[name].GetSingle();

    private static long Integer(AiEditAction action, string name) => action.Params[name].GetInt64();
}
