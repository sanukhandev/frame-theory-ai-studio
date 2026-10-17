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

public sealed record TranscriptionProgress
{
    public TranscriptionProgress(string stage, double fraction)
    {
        if (string.IsNullOrWhiteSpace(stage) || double.IsNaN(fraction) || double.IsInfinity(fraction) || fraction is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction));
        }

        Stage = stage.Trim();
        Fraction = fraction;
    }

    public string Stage { get; }

    public double Fraction { get; }
}

public interface ITranscriptionProvider
{
    Task<TranscriptResult> TranscribeAsync(Uri source, CancellationToken cancellationToken);
}

public interface IProgressiveTranscriptionProvider : ITranscriptionProvider
{
    Task<TranscriptResult> TranscribeAsync(Uri source, IProgress<TranscriptionProgress>? progress, CancellationToken cancellationToken);
}

public sealed class JsonTranscriptProvider : IProgressiveTranscriptionProvider
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
        => await TranscribeAsync(source, progress: null, cancellationToken);

    public async Task<TranscriptResult> TranscribeAsync(
        Uri source,
        IProgress<TranscriptionProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
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

        var fileLength = new FileInfo(_transcriptPath).Length;
        progress?.Report(new TranscriptionProgress("Reading", 0));
        await using var stream = File.OpenRead(_transcriptPath);
        await using var payload = new MemoryStream((int)fileLength);
        var buffer = new byte[64 * 1024];
        long bytesRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            if (bytesRead > MaximumBytes - read)
            {
                throw new InvalidDataException("The transcript JSON file exceeds the supported size.");
            }

            await payload.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            bytesRead += read;
            progress?.Report(new TranscriptionProgress("Reading", bytesRead / (double)Math.Max(1, fileLength)));
        }

        payload.Position = 0;
        progress?.Report(new TranscriptionProgress("Parsing", 0.9));
        var document = await JsonSerializer.DeserializeAsync<TranscriptDocument>(payload, cancellationToken: cancellationToken)
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
            var result = new TranscriptResult(
                source,
                document.SourceFingerprint,
                document.Words.Select(word => new TranscriptWord(word.Text ?? string.Empty, TimeSpan.FromMilliseconds(word.StartMs), TimeSpan.FromMilliseconds(word.EndMs))));
            progress?.Report(new TranscriptionProgress("Completed", 1));
            return result;
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
