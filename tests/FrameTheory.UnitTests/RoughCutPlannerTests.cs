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

    [Xunit.Fact]
    public void SourceReportsTheInvalidFrameBound()
    {
        var startException = Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new RoughCutSource("source", -1, 10, []));
        var durationException = Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new RoughCutSource("source", 0, 0, []));

        Xunit.Assert.Equal("timelineStartFrame", startException.ParamName);
        Xunit.Assert.Equal("durationFrames", durationException.ParamName);
    }

    [Xunit.Fact]
    public void RoughCutModelsExposeImmutableSnapshots()
    {
        var peaks = new List<float> { 0.5f };
        var source = new RoughCutSource("source", 0, 10, peaks);
        var segments = new List<RoughCutSegment> { new("source", 0, 0, 10, 0.5) };
        var plan = new RoughCutPlan(segments);

        peaks[0] = 0;
        segments.Clear();

        Xunit.Assert.Equal(0.5f, source.WaveformPeaks[0]);
        Xunit.Assert.Single(plan.Segments);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<float>>(source.WaveformPeaks).IsReadOnly);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<RoughCutSegment>>(plan.Segments).IsReadOnly);
    }
}
