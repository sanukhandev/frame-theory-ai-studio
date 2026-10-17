using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class TimelineAudioTests
{
    [Xunit.Fact]
    public void AudioSegmentsValidateGainFadesAndSourceTiming()
    {
        var mediaId = MediaId.Parse(new string('a', 64));
        var clip = TimelineClip.Audio(mediaId, 0, 48000, 0, 30, 48000);
        var segment = new TimelineAudioSegment(clip, gainDb: 3, muted: false, fadeInFrames: 5, fadeOutFrames: 6);

        Xunit.Assert.Equal(3, segment.GainDb);
        Xunit.Assert.Equal(48000, segment.Clip.SourceUnitCount);
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineAudioSegment(clip, gainDb: 20));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineAudioSegment(clip, fadeInFrames: 30));
    }

    [Xunit.Fact]
    public void FilterGraphBuildsSynchronizedAudioMixWithFadesAndMute()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-audio-graph-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "source.mp4");
            File.WriteAllText(sourcePath, "source");
            var mediaId = MediaId.Create(new Uri(sourcePath), "audio-graph-test");
            var videoClip = TimelineClip.Video(mediaId, 0, 30, 0, 30, new RationalFrameRate(30, 1));
            var audioClip = TimelineClip.Audio(mediaId, 0, 48000, 0, 30, 48000);
            var plan = new TimelineVideoRenderPlan(
                new RationalFrameRate(30, 1),
                16,
                8,
                30,
                new Dictionary<MediaId, Uri> { [mediaId] = new Uri(sourcePath) },
                [new TimelineVideoLayer("video", 0, [new TimelineVideoSegment(videoClip)])],
                [new TimelineAudioLayer("audio", 0, [new TimelineAudioSegment(audioClip, gainDb: 3, muted: false, fadeInFrames: 2, fadeOutFrames: 3)])]);

            var graph = new TimelineFilterGraphBuilder().Build(plan);

            Xunit.Assert.Equal("aout", graph.AudioOutputLabel);
            Xunit.Assert.Contains("atrim=start_sample=0:end_sample=48000", graph.FilterComplex);
            Xunit.Assert.Contains("volume=3dB", graph.FilterComplex);
            Xunit.Assert.Contains("afade=t=in", graph.FilterComplex);
            Xunit.Assert.Contains("afade=t=out", graph.FilterComplex);
            Xunit.Assert.Contains("amix=inputs=2", graph.FilterComplex);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
