using FrameTheory.Captions;

namespace FrameTheory.UnitTests;

public sealed class TimedCaptionTests
{
    [Xunit.Fact]
    public void SrtRoundTripUsesExactTimezoneIndependentMillisecondsAndProvenance()
    {
        const string source = "1\r\n00:00:01,250 --> 00:00:02,500\r\nHello\r\nworld\r\n\r\n2\r\n00:01:00,000 --> 00:01:01,001\r\nLater\r\n";

        var captions = SrtCaptionFile.Import(source);
        var exported = SrtCaptionFile.Export(captions);

        Xunit.Assert.Equal(TimeSpan.FromMilliseconds(1250), captions[0].Start);
        Xunit.Assert.Equal(TimeSpan.FromMilliseconds(2500), captions[0].End);
        Xunit.Assert.Equal("Hello\nworld", captions[0].Text);
        Xunit.Assert.Equal(CaptionProvenance.ImportedSubtitle, captions[0].Provenance);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<TimedCaption>>(captions).IsReadOnly);
        Xunit.Assert.Contains("00:00:01,250 --> 00:00:02,500", exported);
        Xunit.Assert.Contains("00:01:00,000 --> 00:01:01,001", exported);
    }

    [Xunit.Fact]
    public void SrtParserRejectsMalformedOverlappingAndOversizedCues()
    {
        Xunit.Assert.Throws<InvalidDataException>(() => SrtCaptionFile.Import("1\n00:00:02,000 --> 00:00:01,000\nBad"));
        Xunit.Assert.Throws<InvalidDataException>(() => SrtCaptionFile.Import("1\n00:00:00,000 --> 00:00:01,000\nOne\n\n2\n00:00:00,500 --> 00:00:02,000\nTwo"));
        Xunit.Assert.Throws<InvalidDataException>(() => SrtCaptionFile.Import(string.Concat(Enumerable.Repeat("x", 10 * 1024 * 1024 + 1))));
    }

    [Xunit.Fact]
    public void TimedCaptionRetainsBoundedWordTimingsAndRejectsOverlaps()
    {
        var words = new List<TimedCaptionWord>
        {
            new TimedCaptionWord("one", TimeSpan.Zero, TimeSpan.FromMilliseconds(400)),
            new TimedCaptionWord("two", TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1))
        };

        var caption = new TimedCaption(1, TimeSpan.Zero, TimeSpan.FromSeconds(1), "one two", CaptionProvenance.VerifiedTranscript, words);

        Xunit.Assert.Equal(words, caption.Words);
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<TimedCaptionWord>>(caption.Words).IsReadOnly);
        words.Clear();
        Xunit.Assert.Equal(2, caption.Words.Count);
        Xunit.Assert.Throws<ArgumentException>(() => new TimedCaption(
            1,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            "one two",
            CaptionProvenance.VerifiedTranscript,
            [
                new TimedCaptionWord("one", TimeSpan.Zero, TimeSpan.FromMilliseconds(600)),
                new TimedCaptionWord("two", TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1))
            ]));
        Xunit.Assert.Throws<ArgumentException>(() => new TimedCaption(
            1, TimeSpan.Zero, TimeSpan.FromSeconds(1), "caption", words: [null!]));
    }

    [Xunit.Fact]
    public void UndefinedCaptionProvenanceIsRejected()
    {
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new TimedCaption(
            1,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            "caption",
            (CaptionProvenance)99));
    }
}
