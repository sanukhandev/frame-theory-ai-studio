using FrameTheory.AI;
using FrameTheory.Desktop;

namespace FrameTheory.UnitTests;

public sealed class AiProposalViewModelTests
{
    [Xunit.Fact]
    public void ValidPlanCreatesUnapprovedReviewActionsWithoutMutation()
    {
        var viewModel = new AiProposalViewModel();

        viewModel.Validate(
            """
            {"schemaVersion":"1.0","planId":"plan-1","projectId":"project-1","intent":"grade","actions":[{"id":"grade-1","type":"color.adjust","targetId":"clip-1","startFrame":0,"durationFrames":10,"params":{"exposureEV":0.25,"contrast":0,"saturation":0,"temperature":0,"tint":0}}]}
            """,
            new AiPlanValidationContext("project-1", new HashSet<string>(["clip-1"]), 20));

        Xunit.Assert.Single(viewModel.Actions);
        Xunit.Assert.False(viewModel.Actions[0].IsApproved);
        Xunit.Assert.Equal(0, viewModel.ApprovedCount);
        Xunit.Assert.Contains("No project mutation occurred", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public void ApprovalIsExplicitAndOnlyRecordedUntilExecutorExists()
    {
        var viewModel = new AiProposalViewModel();
        viewModel.Validate(
            """
            {"schemaVersion":"1.0","planId":"plan-1","projectId":"project-1","intent":"grade","actions":[{"id":"grade-1","type":"color.adjust","targetId":"clip-1","startFrame":0,"durationFrames":10,"params":{"exposureEV":0.25,"contrast":0,"saturation":0,"temperature":0,"tint":0}}]}
            """,
            new AiPlanValidationContext("project-1", new HashSet<string>(["clip-1"]), 20));

        viewModel.Actions[0].IsApproved = true;
        Xunit.Assert.Equal(1, viewModel.ApprovedCount);

        viewModel.RecordApprovals();

        Xunit.Assert.Contains("No project mutation occurred", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public void InvalidPlanClearsAnyPreviousProposal()
    {
        var viewModel = new AiProposalViewModel();
        viewModel.Validate(
            """
            {"schemaVersion":"1.0","planId":"plan-1","projectId":"project-1","intent":"grade","actions":[{"id":"grade-1","type":"color.adjust","targetId":"clip-1","startFrame":0,"durationFrames":10,"params":{"exposureEV":0.25,"contrast":0,"saturation":0,"temperature":0,"tint":0}}]}
            """,
            new AiPlanValidationContext("project-1", new HashSet<string>(["clip-1"]), 20));

        viewModel.Validate("{not-json", new AiPlanValidationContext("project-1", new HashSet<string>(["clip-1"]), 20));

        Xunit.Assert.Empty(viewModel.Actions);
        Xunit.Assert.Contains("Plan rejected", viewModel.StatusText, StringComparison.Ordinal);
    }
}
