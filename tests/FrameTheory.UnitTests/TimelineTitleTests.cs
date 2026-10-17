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
}
