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

    [Xunit.Fact]
    public void ScopeDimensionsReportTheInvalidParameter()
    {
        Xunit.Assert.Equal("height", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => ColorScopeAnalyzer.Analyze(2, 0, [])).ParamName);
        var bins = new int[256];
        var scopeBins = new int[64 * 64];
        Xunit.Assert.Equal("height", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new ColorScopeData(1, 0, bins, bins, bins, scopeBins, scopeBins)).ParamName);
    }

    [Xunit.Fact]
    public void ScopeDataSnapshotsValidatedBins()
    {
        var red = new int[256];
        var green = new int[256];
        var blue = new int[256];
        var waveform = new int[64 * 64];
        var vectorscope = new int[64 * 64];
        var scopes = new ColorScopeData(1, 1, red, green, blue, waveform, vectorscope);

        red[0] = 1;
        waveform[0] = 1;

        Xunit.Assert.Equal(0, scopes.RedHistogram[0]);
        Xunit.Assert.Equal(0, scopes.Waveform[0]);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<int>>(scopes.RedHistogram).IsReadOnly);
    }
}
