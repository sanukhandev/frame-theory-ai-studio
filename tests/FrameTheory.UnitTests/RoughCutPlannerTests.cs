using FrameTheory.AI;

namespace FrameTheory.UnitTests;

public sealed class RoughCutPlannerTests
{
    [Xunit.Fact]
    public void PlannerOrdersRealSourcesByMeasuredEnergyAndKeepsFrameDurations()
    {
        var plan = RoughCutPlanner.Create([
            new RoughCutSource("quiet", 0, 30, [0.1f, 0.1f, 0.1f]),
            new RoughCutSource("active", 30, 45, [0.2f, 0.9f, 0.8f]),
            new RoughCutSource("middle", 75, 20, [0.4f, 0.4f, 0.4f])
        ]);

        Xunit.Assert.Equal(["active", "middle", "quiet"], plan.Segments.Select(segment => segment.MediaId));
        Xunit.Assert.Equal([0L, 45L, 65L], plan.Segments.Select(segment => segment.StartFrame));
        Xunit.Assert.Equal(95, plan.TotalFrames);
        Xunit.Assert.Contains("measured waveform activity", plan.Summary, StringComparison.Ordinal);
    }

    [Xunit.Fact]
    public void PlannerRejectsDuplicateOrInvalidSources()
    {
        Xunit.Assert.Throws<ArgumentException>(() => RoughCutPlanner.Create([
            new RoughCutSource("same", 0, 10, []),
            new RoughCutSource("same", 10, 10, [])
        ]));
        Xunit.Assert.Throws<ArgumentException>(() => new RoughCutSource("bad", 0, 10, [1.1f]));
        Xunit.Assert.Throws<ArgumentException>(() => RoughCutPlanner.Create([new RoughCutSource("one", 0, 10, [])]));
    }
}
