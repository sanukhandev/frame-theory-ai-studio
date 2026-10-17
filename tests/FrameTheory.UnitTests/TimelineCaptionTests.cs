using FrameTheory.Captions;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class TimelineCaptionTests
{
    [Xunit.Fact]
    public void CaptionGraphWrapsTextAndUsesTimedSafePlacement()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-caption-graph-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            var fontPath = Path.Combine(directory.FullName, "caption.ttf");
            File.WriteAllText(sourcePath, "source");
            File.WriteAllText(fontPath, "font");
            var mediaId = MediaId.Create(new Uri(sourcePath), "caption-graph-test");
            var clip = TimelineClip.Video(mediaId, 0, 90, 0, 90, new RationalFrameRate(30, 1));
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                90,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video", 0, [new TimelineVideoSegment(clip)])],
                captions: [new TimedCaption(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "one two three four")],
                captionStyle: new TimelineCaptionStyle(fontPath, maximumCharactersPerLine: 7));

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Contains("drawtext=fontfile=", graph.FilterComplex);
            Xunit.Assert.Contains("text='one two\\\\nthree\\\\nfour'", graph.FilterComplex);
            Xunit.Assert.Contains("x=(w-text_w)/2:y=h-text_h-32", graph.FilterComplex);
            Xunit.Assert.Contains("enable='between(t,1,2)'", graph.FilterComplex);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void CaptionExportRejectsMissingRenderFontInsteadOfFakingOutput()
    {
        var mediaId = MediaId.Parse(new string('b', 64));
        var clip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
        var plan = new TimelineVideoRenderPlan(
            new RationalFrameRate(30, 1),
            16,
            8,
            30,
            new Dictionary<MediaId, Uri>(),
            [new TimelineVideoLayer("video", 0, [new TimelineVideoSegment(clip)])],
            captions: [new TimedCaption(1, TimeSpan.Zero, TimeSpan.FromSeconds(1), "caption")]);

        Xunit.Assert.Throws<MediaExportException>(() => new TimelineFilterGraphBuilder().Build(plan));
    }
}
