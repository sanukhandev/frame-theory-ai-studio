using System.Text.Json;

namespace FrameTheory.Captions;

public sealed record TranscriptWord
{
    public TranscriptWord(string text, TimeSpan start, TimeSpan end)
    {
        if (string.IsNullOrWhiteSpace(text) || start < TimeSpan.Zero || end <= start)
        {
            throw new ArgumentException("Transcript words require non-empty text and positive timing.", nameof(text));
        }

        Text = text.Trim();
        Start = start;
        End = end;
    }

    public string Text { get; }

    public TimeSpan Start { get; }

    public TimeSpan End { get; }
}

public sealed record TranscriptResult
{
    public TranscriptResult(Uri source, string sourceFingerprint, IEnumerable<TranscriptWord> words)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsFile || string.IsNullOrWhiteSpace(sourceFingerprint))
        {
            throw new ArgumentException("A local source URI and source fingerprint are required.", nameof(source));
        }

        ArgumentNullException.ThrowIfNull(words);
        var orderedWords = words.OrderBy(word => word.Start).ToArray();
        if (orderedWords.Length == 0 || orderedWords.Zip(orderedWords.Skip(1), (previous, current) => previous.End <= current.Start).Any(isValid => !isValid))
        {
            throw new ArgumentException("Transcript words must be non-empty and non-overlapping.", nameof(words));
        }

        Source = source;
        SourceFingerprint = sourceFingerprint.Trim();
        Words = orderedWords;
    }

    public Uri Source { get; }

    public string SourceFingerprint { get; }

    public IReadOnlyList<TranscriptWord> Words { get; }

    public CaptionProvenance Provenance => CaptionProvenance.VerifiedTranscript;
}

public interface ITranscriptionProvider
{
    Task<TranscriptResult> TranscribeAsync(Uri source, CancellationToken cancellationToken);
}

public sealed class JsonTranscriptProvider : ITranscriptionProvider
{
    private const int MaximumBytes = 10 * 1024 * 1024;
    private readonly string _transcriptPath;

    public JsonTranscriptProvider(string transcriptPath)
    {
        if (string.IsNullOrWhiteSpace(transcriptPath))
        {
            throw new ArgumentException("A transcript path is required.", nameof(transcriptPath));
        }

        _transcriptPath = Path.GetFullPath(transcriptPath);
    }

    public async Task<TranscriptResult> TranscribeAsync(Uri source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsFile)
        {
            throw new InvalidDataException("Transcript sources must be local files.");
        }

        if (!File.Exists(_transcriptPath))
        {
            throw new FileNotFoundException("The transcript JSON file was not found.", _transcriptPath);
        }

        if (new FileInfo(_transcriptPath).Length > MaximumBytes)
        {
            throw new InvalidDataException("The transcript JSON file exceeds the supported size.");
        }

        await using var stream = File.OpenRead(_transcriptPath);
        var document = await JsonSerializer.DeserializeAsync<TranscriptDocument>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("The transcript JSON document is empty.");
        if (!string.Equals(document.Source, source.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The transcript source does not match the selected media.");
        }

        if (string.IsNullOrWhiteSpace(document.SourceFingerprint) || document.Words is null)
        {
            throw new InvalidDataException("The transcript requires a source fingerprint and timed words.");
        }

        try
        {
            return new TranscriptResult(
                source,
                document.SourceFingerprint,
                document.Words.Select(word => new TranscriptWord(word.Text ?? string.Empty, TimeSpan.FromMilliseconds(word.StartMs), TimeSpan.FromMilliseconds(word.EndMs))));
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The transcript contains invalid word timing.", exception);
        }
    }

    private sealed class TranscriptDocument
    {
        public string? Source { get; set; }

        public string? SourceFingerprint { get; set; }

        public TranscriptWordDocument[]? Words { get; set; }
    }

    private sealed class TranscriptWordDocument
    {
        public string? Text { get; set; }

        public long StartMs { get; set; }

        public long EndMs { get; set; }
    }
}
