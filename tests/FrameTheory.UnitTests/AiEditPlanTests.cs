using System.Text.Json;
using FrameTheory.AI;

namespace FrameTheory.UnitTests;

public sealed class AiEditPlanTests
{
    [Xunit.Fact]
    public void ParserAndValidatorAcceptAValidAllowlistedPlan()
    {
        var plan = AiEditPlanParser.Parse("""
            {
              "schemaVersion": "1.0",
              "planId": "plan-1",
              "projectId": "project-1",
              "intent": "cinematic title",
              "actions": [
                { "id": "grade-1", "type": "color.adjust", "targetId": "clip-1", "startFrame": 0, "durationFrames": 60,
                  "params": { "exposureEV": 0.25, "contrast": 0.2, "saturation": -0.1, "temperature": 0.1, "tint": 0 } },
                { "id": "title-1", "type": "title.add", "targetId": "title-track", "startFrame": 10, "durationFrames": 30,
                  "params": { "text": "FRAME THEORY", "fontId": "brand-sans", "preset": "fade-in" } },
                { "id": "caption-1", "type": "caption.add", "targetId": "caption-track", "startFrame": 10, "durationFrames": 20,
                  "params": { "text": "hello", "provenance": "verified-transcript" } }
              ]
            }
            """);

        var result = AiEditPlanValidator.Validate(
            plan,
            new AiPlanValidationContext("project-1", new HashSet<string>(["clip-1", "title-track", "caption-track"]), 120));

        Xunit.Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Xunit.Assert.Equal(3, plan.Actions.Count);
        Xunit.Assert.NotEmpty(AiOperationCatalog.Supported);
    }

    [Xunit.Fact]
    public void ParserRejectsUnknownFieldsUnknownOperationsAndDuplicateActionIds()
    {
        Xunit.Assert.Throws<AiPlanFormatException>(() => AiEditPlanParser.Parse("""
            { "schemaVersion":"1.0", "planId":"p", "projectId":"q", "intent":"x", "unexpected":true, "actions":[] }
            """));
        Xunit.Assert.Throws<AiPlanFormatException>(() => AiEditPlanParser.Parse("""
            { "schemaVersion":"1.0", "planId":"p", "projectId":"q", "intent":"x", "actions":[{"id":"a","type":"shell.exec","targetId":"x","startFrame":0,"durationFrames":1,"params":{}}] }
            """));
        Xunit.Assert.Throws<AiPlanFormatException>(() => AiEditPlanParser.Parse("""
            { "schemaVersion":"1.0", "planId":"p", "projectId":"q", "intent":"x", "actions":[
              {"id":"a","type":"clip.split","targetId":"x","startFrame":0,"durationFrames":1,"params":{}},
              {"id":"a","type":"clip.split","targetId":"x","startFrame":1,"durationFrames":1,"params":{}}] }
            """));
    }

    [Xunit.Fact]
    public void EditPlanSnapshotsActionsAndParameters()
    {
        var parameters = new Dictionary<string, JsonElement>
        {
            ["text"] = JsonDocument.Parse("\"hello\"").RootElement.Clone()
        };
        var actions = new List<AiEditAction>
        {
            new("caption", "caption.add", "caption-track", 0, 10, parameters)
        };
        var plan = new AiEditPlan("1.0", "plan-1", "project-1", "caption", actions);

        parameters.Clear();
        actions.Clear();

        Xunit.Assert.Single(plan.Actions);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<AiEditAction>>(plan.Actions).IsReadOnly);
        Xunit.Assert.Single(plan.Actions[0].Params);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IDictionary<string, JsonElement>>(plan.Actions[0].Params).IsReadOnly);
    }

    [Xunit.Fact]
    public void ValidatorRejectsUnknownTargetsRangesPathsAndUnverifiedCaptionText()
    {
        var plan = AiEditPlanParser.Parse("""
            {
              "schemaVersion":"1.0", "planId":"p", "projectId":"project-1", "intent":"unsafe",
              "actions":[
                {"id":"title","type":"title.add","targetId":"missing","startFrame":90,"durationFrames":40,
                 "params":{"text":"run rm -rf","fontPath":"C:/bad.ttf","preset":"fade-in"}},
                {"id":"caption","type":"caption.add","targetId":"caption-track","startFrame":0,"durationFrames":1,
                 "params":{"text":"synthetic speech"}}
              ]
            }
            """);

        var result = AiEditPlanValidator.Validate(
            plan,
            new AiPlanValidationContext("project-1", new HashSet<string>(["caption-track"]), 100));

        Xunit.Assert.False(result.IsValid);
        Xunit.Assert.Contains(result.Errors, error => error.Contains("unknown object", StringComparison.Ordinal));
        Xunit.Assert.Contains(result.Errors, error => error.Contains("frame range", StringComparison.Ordinal));
        Xunit.Assert.Contains(result.Errors, error => error.Contains("unsupported parameter 'fontPath'", StringComparison.Ordinal));
        Xunit.Assert.Contains(result.Errors, error => error.Contains("fontId", StringComparison.Ordinal));
        Xunit.Assert.Contains(result.Errors, error => error.Contains("provenance", StringComparison.Ordinal));
    }
}
