using System.Text.Json;
using FrameTheory.AI;
using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class AiEditPlanExecutorTests
{
    [Xunit.Fact]
    public void ApprovedColorActionChangesRenderPlanAndUndoRestoresIt()
    {
        var editor = CreateEditor(out var mediaId);
        var action = ColorAction(mediaId.Value);
        Xunit.Assert.False(editor.CanUndoAiEdit);

        var result = AiEditPlanExecutor.Execute([action], editor, new Dictionary<string, string>(), new Dictionary<string, FrameTheory.Color.ColorGrade>());

        Xunit.Assert.True(result.IsApplied, result.Message);
        Xunit.Assert.True(editor.CanUndoAiEdit);
        var source = new Uri(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "frame-theory-ai-source.mp4")));
        File.WriteAllBytes(source.LocalPath, [0]);
        try
        {
            var plan = editor.BuildRenderPlan(16, 8, new Dictionary<MediaId, Uri> { [mediaId] = source });
            var graph = new TimelineFilterGraphBuilder().Build(plan);
            Xunit.Assert.Contains("eq=brightness=", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.True(editor.UndoAiEdit());
            Xunit.Assert.False(editor.CanUndoAiEdit);
            Xunit.Assert.Empty(editor.BuildRenderPlan(16, 8, new Dictionary<MediaId, Uri> { [mediaId] = source }).ColorGrades);
        }
        finally
        {
            File.Delete(source.LocalPath);
        }
    }

    [Xunit.Fact]
    public void FailedLaterActionRollsBackEarlierApprovedChanges()
    {
        var editor = CreateEditor(out var mediaId);
        var actions = new[]
        {
            ColorAction(mediaId.Value),
            new AiEditAction("bad", "transition.add", mediaId.Value, 0, 10, new Dictionary<string, JsonElement>
            {
                ["kind"] = JsonDocument.Parse("\"cross-dissolve\"").RootElement.Clone()
            })
        };

        var result = AiEditPlanExecutor.Execute(actions, editor, new Dictionary<string, string>(), new Dictionary<string, FrameTheory.Color.ColorGrade>());

        Xunit.Assert.False(result.IsApplied);
        Xunit.Assert.Empty(editor.BuildRenderPlan(16, 8, new Dictionary<MediaId, Uri> { [mediaId] = new Uri("file:///unused.mp4") }).ColorGrades);
        Xunit.Assert.False(editor.UndoAiEdit());
    }

    [Xunit.Fact]
    public void DanglingApprovedFontRollsBackTitleAction()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-ai-font-link-");
        try
        {
            var linkedFont = Path.Combine(directory.FullName, "linked.ttf");
            File.CreateSymbolicLink(linkedFont, Path.Combine(directory.FullName, "missing.ttf"));
            var action = new AiEditAction("title", "title.add", "title-track", 0, 10, new Dictionary<string, JsonElement>
            {
                ["text"] = JsonDocument.Parse("\"Title\"").RootElement.Clone(),
                ["fontId"] = JsonDocument.Parse("\"brand-sans\"").RootElement.Clone(),
                ["preset"] = JsonDocument.Parse("\"fade-in\"").RootElement.Clone()
            });

            var result = AiEditPlanExecutor.Execute(
                [action],
                new TimelineEditorViewModel(),
                new Dictionary<string, string> { ["brand-sans"] = linkedFont },
                new Dictionary<string, FrameTheory.Color.ColorGrade>());

            Xunit.Assert.False(result.IsApplied);
            Xunit.Assert.Contains("approved local font", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void ApprovedReelActionProducesEditableRevealPlan()
    {
        var editor = CreateEditor(out var mediaId);
        var action = new AiEditAction("reel", "reel.before_after", "active-project", 0, 10, new Dictionary<string, JsonElement>
        {
            ["style"] = JsonDocument.Parse("\"split\"").RootElement.Clone()
        });
        var source = new Uri(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "frame-theory-ai-reel-source.mp4")));
        File.WriteAllBytes(source.LocalPath, [0]);
        try
        {
            var result = AiEditPlanExecutor.Execute([action], editor, new Dictionary<string, string>(), new Dictionary<string, FrameTheory.Color.ColorGrade>());
            var graph = new TimelineFilterGraphBuilder().Build(editor.BuildRenderPlan(16, 8, new Dictionary<MediaId, Uri> { [mediaId] = source }));

            Xunit.Assert.True(result.IsApplied, result.Message);
            Xunit.Assert.Contains("beforeafter_left", graph.FilterComplex, StringComparison.Ordinal);
            Xunit.Assert.True(editor.UndoAiEdit());
        }
        finally
        {
            File.Delete(source.LocalPath);
        }
    }

    [Xunit.Fact]
    public void ApprovedActionsCanTargetMultipleTimelineClipsInOneUndoableGroup()
    {
        var editor = CreateEditor(out var firstMediaId);
        var secondMediaId = MediaId.Create(new Uri("file:///second-fixture.mp4"), "second-fixture");
        editor.AddMedia(new MediaSourceMetadata(
            secondMediaId,
            new Uri("file:///second-fixture.mp4"),
            MediaKind.Video,
            16,
            8,
            new RationalFrameRate(30, 1),
            30,
            TimeSpan.FromSeconds(1)));
        var actions = new[]
        {
            ColorAction(firstMediaId.Value),
            ColorAction(secondMediaId.Value),
            new AiEditAction("transition", "transition.add", secondMediaId.Value, 30, 5, new Dictionary<string, JsonElement>
            {
                ["kind"] = JsonDocument.Parse("\"cross-dissolve\"").RootElement.Clone()
            })
        };

        var result = AiEditPlanExecutor.Execute(actions, editor, new Dictionary<string, string>(), new Dictionary<string, FrameTheory.Color.ColorGrade>());
        var plan = editor.BuildRenderPlan(16, 8, new Dictionary<MediaId, Uri>
        {
            [firstMediaId] = new Uri("file:///first-fixture.mp4"),
            [secondMediaId] = new Uri("file:///second-fixture.mp4")
        });

        Xunit.Assert.True(result.IsApplied, result.Message);
        Xunit.Assert.Equal(3, result.Outcomes.Count);
        Xunit.Assert.Equal(2, plan.ColorGrades.Count);
        Xunit.Assert.Contains(plan.Layers.Single().Segments, segment => segment.Transition == TimelineTransitionKind.CrossDissolve);
        Xunit.Assert.True(editor.UndoAiEdit());
        Xunit.Assert.Empty(editor.BuildRenderPlan(16, 8, new Dictionary<MediaId, Uri>
        {
            [firstMediaId] = new Uri("file:///first-fixture.mp4"),
            [secondMediaId] = new Uri("file:///second-fixture.mp4")
        }).ColorGrades);
    }

    [Xunit.Fact]
    public void ExecutionResultSnapshotsOutcomes()
    {
        var outcomes = new List<AiActionExecutionOutcome> { new("action", true, "Applied.") };
        var result = new AiPlanExecutionResult(true, "group", outcomes, "Applied.");

        outcomes.Clear();

        Xunit.Assert.Single(result.Outcomes);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<AiActionExecutionOutcome>>(result.Outcomes).IsReadOnly);
    }

    private static TimelineEditorViewModel CreateEditor(out MediaId mediaId)
    {
        mediaId = MediaId.Create(new Uri("file:///fixture.mp4"), "fixture");
        var editor = new TimelineEditorViewModel();
        editor.AddMedia(new MediaSourceMetadata(mediaId, new Uri("file:///fixture.mp4"), MediaKind.Video, 16, 8, new RationalFrameRate(30, 1), 30, TimeSpan.FromSeconds(1)));
        return editor;
    }

    private static AiEditAction ColorAction(string mediaId) => new(
        "grade",
        "color.adjust",
        mediaId,
        0,
        10,
        new Dictionary<string, JsonElement>
        {
            ["exposureEV"] = Number(0.5),
            ["contrast"] = Number(0.1),
            ["saturation"] = Number(0),
            ["temperature"] = Number(0),
            ["tint"] = Number(0)
        });

    private static JsonElement Number(double value) => JsonDocument.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture)).RootElement.Clone();
}
