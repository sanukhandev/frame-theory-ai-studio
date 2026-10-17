using FrameTheory.Color;

namespace FrameTheory.UnitTests;

public sealed class ColorScopeTests
{
    [Xunit.Fact]
    public void AnalyzerBuildsBoundedHistogramWaveformAndVectorscopeBins()
    {
        var scopes = ColorScopeAnalyzer.Analyze(2, 2, [255, 0, 0, 0, 255, 0, 0, 0, 255, 128, 128, 128]);

        Xunit.Assert.Equal(4, scopes.PixelCount);
        Xunit.Assert.Equal(1, scopes.RedHistogram[255]);
        Xunit.Assert.Equal(1, scopes.GreenHistogram[255]);
        Xunit.Assert.Equal(1, scopes.BlueHistogram[255]);
        Xunit.Assert.Equal(1, scopes.RedHistogram[128]);
        Xunit.Assert.Equal(4, scopes.Waveform.Sum());
        Xunit.Assert.Equal(4, scopes.Vectorscope.Sum());
        Xunit.Assert.Equal(64 * 64, scopes.Waveform.Count);
        Xunit.Assert.Equal(64 * 64, scopes.Vectorscope.Count);
    }

    [Xunit.Fact]
    public void AnalyzerRejectsMismatchedOrOversizedInput()
    {
        Xunit.Assert.Throws<ArgumentException>(() => ColorScopeAnalyzer.Analyze(2, 2, [0, 0, 0]));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => ColorScopeAnalyzer.Analyze(0, 2, []));
    }
}
