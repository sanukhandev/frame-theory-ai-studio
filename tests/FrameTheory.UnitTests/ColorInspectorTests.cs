using FrameTheory.Desktop;
using FrameTheory.Color;

namespace FrameTheory.UnitTests;

public sealed class ColorInspectorTests
{
    [Xunit.Fact]
    public void InspectorUsesTheSharedPipelineForBeforeAfterAndReset()
    {
        byte[] source = [.. "P6\n2 1\n255\n"u8, 32, 64, 128, 200, 100, 40];
        var inspector = new ColorGradeInspectorViewModel();
        inspector.SetSource(source);
        var before = inspector.BeforePreviewPpm;

        inspector.ExposureEV = 1;
        var after = inspector.AfterPreviewPpm;

        Xunit.Assert.NotNull(before);
        Xunit.Assert.NotNull(after);
        Xunit.Assert.NotEqual(before, after);
        Xunit.Assert.Contains("2x1", inspector.BeforeSummary);
        Xunit.Assert.Contains("mean RGB", inspector.AfterSummary);
        inspector.ResetGrade();
        Xunit.Assert.Equal(before, inspector.AfterPreviewPpm);
    }

    [Xunit.Fact]
    public void InspectorReportsInvalidPreviewInsteadOfFabricatingAResult()
    {
        var inspector = new ColorGradeInspectorViewModel();
        inspector.SetSource("not-a-ppm"u8.ToArray());

        Xunit.Assert.Contains("unavailable", inspector.StatusText);
        Xunit.Assert.Null(inspector.AfterPreviewPpm);
    }

    [Xunit.Fact]
    public void InspectorCanCreateAndApplyReusableStylePresets()
    {
        byte[] source = [.. "P6\n1 1\n255\n"u8, 32, 64, 128];
        var inspector = new ColorGradeInspectorViewModel();
        inspector.SetSource(source);
        inspector.ExposureEV = 1;
        var preset = inspector.CreatePreset("Warm");
        inspector.ResetGrade();
        inspector.ApplyPreset(preset);

        Xunit.Assert.Equal("Warm", preset.Name);
        Xunit.Assert.Equal(1, inspector.ExposureEV);
        Xunit.Assert.NotEqual(inspector.BeforePreviewPpm, inspector.AfterPreviewPpm);
        Xunit.Assert.Contains("Applied style", inspector.StatusText);
    }
}
