using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class TimelineTitleTests
{
    [Xunit.Fact]
    public void TitleContractRejectsUnsafeBoundsAndRequiresFontPath()
    {
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineTitle(string.Empty, "font.ttf", 0, 30));
        Xunit.Assert.Throws<ArgumentException>(() => new TimelineTitle("Title", string.Empty, 0, 30));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineTitle("Title", "font.ttf", 0, 30, fontSize: 5000));
        Xunit.Assert.Equal("durationFrames", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineTitle("Title", "font.ttf", 0, 0)).ParamName);
        Xunit.Assert.Equal("placement", Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineTitle(
            "Title",
            "font.ttf",
            0,
            30,
            placement: (TimelineTitlePlacement)99)).ParamName);
    }

    [Xunit.Fact]
    public void FilterGraphUsesEscapedTextSafeAreaAndZOrderedTitleStage()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-title-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var fontPath = Path.Combine(directory.FullName, "licensed.ttf");
            File.WriteAllText(sourcePath, "source");
            File.WriteAllText(fontPath, "font");
            var mediaId = MediaId.Create(new Uri(sourcePath), "title-test");
            var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video", 0, [new TimelineVideoSegment(clip)])],
                titles: [new TimelineTitle("A: title, safely", fontPath, 0, 30, placement: TimelineTitlePlacement.TopCenter)]);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Contains("drawtext=fontfile=", graph.FilterComplex);
            Xunit.Assert.Contains("text='A\\: title\\, safely'", graph.FilterComplex);
            Xunit.Assert.Contains("x=(w-text_w)/2:y=h*0.1", graph.FilterComplex);
            Xunit.Assert.Contains("between(t,0,1)", graph.FilterComplex);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void AnimatedTitleCarriesInspectableKeyframesIntoDrawtextExpressions()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-animated-title-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var fontPath = Path.Combine(directory.FullName, "licensed.ttf");
            File.WriteAllText(sourcePath, "source");
            File.WriteAllText(fontPath, "font");
            var mediaId = MediaId.Create(new Uri(sourcePath), "animated-title-test");
            var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var animation = TimelineTransformKeyframes.FromPreset(
                TimelineAnimationPreset.PushIn,
                0,
                30,
                new TimelineVideoTransform());
            var title = new TimelineTitle("Animated", fontPath, 0, 30, animation: animation);
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video", 0, [new TimelineVideoSegment(clip)])],
                titles: [title]);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Equal(2, title.Animation!.Keyframes.Count);
            Xunit.Assert.Contains("fontsize=48*(if(lt(n", graph.FilterComplex);
            Xunit.Assert.Contains("alpha=if(lt(n", graph.FilterComplex);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
