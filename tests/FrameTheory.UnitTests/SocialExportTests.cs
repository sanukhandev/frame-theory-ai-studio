using FrameTheory.Media;
using FrameTheory.Photo;

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

    [Xunit.Fact]
    public void PhotoPresetsReuseStableSocialDimensions()
    {
        var landscape = SocialPhotoExportPreset.Create(SocialAspectRatio.Landscape16By9, 180);
        var square = SocialPhotoExportPreset.Create(SocialAspectRatio.Square1By1, 180);
        var portrait = SocialPhotoExportPreset.Create(SocialAspectRatio.Portrait9By16, 180);

        Xunit.Assert.Equal((320, 180), (landscape.Width, landscape.Height));
        Xunit.Assert.Equal((180, 180), (square.Width, square.Height));
        Xunit.Assert.Equal((180, 320), (portrait.Width, portrait.Height));
    }
}
