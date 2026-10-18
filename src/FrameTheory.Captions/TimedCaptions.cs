using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FrameTheory.Captions;

public enum CaptionProvenance
{
    ImportedSubtitle,
    VerifiedTranscript,
    AuthoredCaption
}

public sealed record TimedCaptionWord
{
    public TimedCaptionWord(string text, TimeSpan start, TimeSpan end)
    {
        if (string.IsNullOrWhiteSpace(text) || start < TimeSpan.Zero || end <= start)
        {
            throw new ArgumentException("Caption words require non-empty text and positive timing.", nameof(text));
        }

        Text = text.Trim();
        Start = start;
        End = end;
    }

    public string Text { get; }

    public TimeSpan Start { get; }

    public TimeSpan End { get; }
}

public sealed record TimedCaption
{
    public TimedCaption(
        int sequence,
        TimeSpan start,
        TimeSpan end,
        string text,
        CaptionProvenance provenance = CaptionProvenance.ImportedSubtitle,
        IEnumerable<TimedCaptionWord>? words = null)
    {
        if (sequence <= 0 || start < TimeSpan.Zero || end <= start)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        if (string.IsNullOrWhiteSpace(text) || text.Length > 10_000)
        {
            throw new ArgumentException("Caption text must contain between one and 10000 characters.", nameof(text));
        }

        Sequence = sequence;
        Start = start;
        End = end;
        Text = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        Provenance = provenance;
        Words = Array.AsReadOnly((words ?? [])
            .OrderBy(word => word.Start)
            .ToArray());
        if (Words.Any(word => word.Start < start || word.End > end) ||
            Words.Zip(Words.Skip(1), (previous, current) => previous.End <= current.Start).Any(isValid => !isValid))
        {
            throw new ArgumentException("Caption word timings must fit within the caption and must not overlap.", nameof(words));
        }
    }

    public int Sequence { get; }

    public TimeSpan Start { get; }

    public TimeSpan End { get; }

    public string Text { get; }

    public CaptionProvenance Provenance { get; }

    public IReadOnlyList<TimedCaptionWord> Words { get; }
}

public static partial class SrtCaptionFile
{
    private const int MaximumBytes = 10 * 1024 * 1024;
    private const int MaximumCues = 10_000;

    public static IReadOnlyList<TimedCaption> Import(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (Encoding.UTF8.GetByteCount(content) > MaximumBytes)
        {
            throw new InvalidDataException("The SRT file exceeds the supported size.");
        }

        var blocks = Regex.Split(content.TrimStart('\uFEFF', '\r', '\n'), "(?:\r?\n){2,}", RegexOptions.CultureInvariant)
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToArray();
        if (blocks.Length > MaximumCues)
        {
            throw new InvalidDataException("The SRT file contains too many cues.");
        }

        var captions = new List<TimedCaption>(blocks.Length);
        for (var blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
        {
            var lines = blocks[blockIndex].Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
            var timingIndex = Array.FindIndex(lines, line => line.Contains("-->", StringComparison.Ordinal));
            if (timingIndex < 0 || timingIndex == lines.Length - 1)
            {
                throw new InvalidDataException($"SRT cue {blockIndex + 1} has no timing or text.");
            }

            var timing = lines[timingIndex].Split("-->", 2, StringSplitOptions.TrimEntries);
            if (timing.Length != 2)
            {
                throw new InvalidDataException($"SRT cue {blockIndex + 1} has invalid timing.");
            }

            var start = ParseTimestamp(timing[0], blockIndex + 1);
            var endToken = timing[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            var end = ParseTimestamp(endToken, blockIndex + 1);
            if (end <= start)
            {
                throw new InvalidDataException($"SRT cue {blockIndex + 1} ends before it starts.");
            }

            var text = string.Join('\n', lines[(timingIndex + 1)..]).Trim();
            var caption = new TimedCaption(blockIndex + 1, start, end, text);
            if (captions.LastOrDefault()?.End > caption.Start)
            {
                throw new InvalidDataException($"SRT cue {blockIndex + 1} overlaps the preceding cue.");
            }

            captions.Add(caption);
        }

        return captions;
    }

    public static string Export(IEnumerable<TimedCaption> captions)
    {
        ArgumentNullException.ThrowIfNull(captions);
        var ordered = captions.OrderBy(caption => caption.Start).ToArray();
        if (ordered.Length > MaximumCues)
        {
            throw new InvalidDataException("The caption collection contains too many cues.");
        }

        var builder = new StringBuilder();
        for (var index = 0; index < ordered.Length; index++)
        {
            var caption = ordered[index];
            if (index > 0 && ordered[index - 1].End > caption.Start)
            {
                throw new InvalidDataException("Caption cues overlap.");
            }

            builder.Append(index + 1).Append("\r\n")
                .Append(FormatTimestamp(caption.Start)).Append(" --> ").Append(FormatTimestamp(caption.End)).Append("\r\n")
                .Append(caption.Text.Replace("\n", "\r\n", StringComparison.Ordinal)).Append("\r\n\r\n");
        }

        return builder.ToString();
    }

    private static TimeSpan ParseTimestamp(string value, int cueNumber)
    {
        var match = TimestampPattern().Match(value);
        if (!match.Success ||
            !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            !int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            !int.TryParse(match.Groups[4].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds) ||
            minutes >= 60 || seconds >= 60)
        {
            throw new InvalidDataException($"SRT cue {cueNumber} has an invalid timestamp.");
        }

        try
        {
            return new TimeSpan(0, hours, minutes, seconds, milliseconds);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException($"SRT cue {cueNumber} has an invalid timestamp.", exception);
        }
    }

    private static string FormatTimestamp(TimeSpan value)
    {
        var totalHours = (long)value.TotalHours;
        return $"{totalHours:00}:{value.Minutes:00}:{value.Seconds:00},{value.Milliseconds:000}";
    }

    [GeneratedRegex(@"^(\d{2,}):(\d{2}):(\d{2}),(\d{3})$")]
    private static partial Regex TimestampPattern();
}
