using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class SocialExportTests
{
    [Xunit.Fact]
    public void SocialPresetsProduceStableLandscapeSquareAndPortraitDimensions()
    {
        Xunit.Assert.Equal((1920, 1080), (SocialVideoExportPreset.Create(SocialAspectRatio.Landscape16By9, 1080).Width, SocialVideoExportPreset.Create(SocialAspectRatio.Landscape16By9, 1080).Height));
        Xunit.Assert.Equal((1080, 1080), (SocialVideoExportPreset.Create(SocialAspectRatio.Square1By1, 1080).Width, SocialVideoExportPreset.Create(SocialAspectRatio.Square1By1, 1080).Height));
        Xunit.Assert.Equal((1080, 1920), (SocialVideoExportPreset.Create(SocialAspectRatio.Portrait9By16, 1080).Width, SocialVideoExportPreset.Create(SocialAspectRatio.Portrait9By16, 1080).Height));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => SocialVideoExportPreset.Create(SocialAspectRatio.Square1By1, 0));
    }
}
