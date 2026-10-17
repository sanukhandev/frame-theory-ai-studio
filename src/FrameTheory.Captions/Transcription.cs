using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

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

public sealed record GeminiTranscriptionOptions
{
    public GeminiTranscriptionOptions(string apiKey, bool uploadConsent, string modelId = "gemini-3.5-transcribe")
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > 4096)
        {
            throw new ArgumentException("A user-provided Gemini API key is required.", nameof(apiKey));
        }

        if (!uploadConsent)
        {
            throw new InvalidOperationException("Explicit upload consent is required before Gemini transcription.");
        }

        if (string.IsNullOrWhiteSpace(modelId) || modelId.Length > 128 || modelId.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid Gemini transcription model is required.", nameof(modelId));
        }

        ApiKey = apiKey.Trim();
        UploadConsent = uploadConsent;
        ModelId = modelId.Trim();
    }

    public string ApiKey { get; }

    public bool UploadConsent { get; }

    public string ModelId { get; }
}

public sealed class GeminiTranscriptionException : InvalidOperationException
{
    public GeminiTranscriptionException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}

public sealed class GeminiTranscriptionProvider : IProgressiveTranscriptionProvider
{
    private const int MaximumInlineBytes = 20 * 1024 * 1024;
    private static readonly Uri InteractionsEndpoint = new("https://generativelanguage.googleapis.com/v1beta/interactions");
    private readonly HttpClient _httpClient;
    private readonly GeminiTranscriptionOptions _options;

    public GeminiTranscriptionProvider(HttpClient httpClient, GeminiTranscriptionOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<TranscriptResult> TranscribeAsync(Uri source, CancellationToken cancellationToken)
        => TranscribeAsync(source, progress: null, cancellationToken);

    public async Task<TranscriptResult> TranscribeAsync(
        Uri source,
        IProgress<TranscriptionProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        if (!source.IsFile)
        {
            throw new InvalidDataException("Gemini transcription sources must be local files.");
        }

        var sourcePath = source.LocalPath;
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The transcription source was not found.", sourcePath);
        }

        var contentType = GetAudioContentType(sourcePath);
        var fileInfo = new FileInfo(sourcePath);
        if (fileInfo.Length == 0)
        {
            throw new InvalidDataException("The transcription source is empty.");
        }

        if (fileInfo.Length > MaximumInlineBytes)
        {
            throw new InvalidDataException("Gemini transcription currently supports inline audio up to 20 MiB; use a shorter audio source.");
        }

        progress?.Report(new TranscriptionProgress("Reading approved audio", 0));
        await using var stream = File.OpenRead(sourcePath);
        await using var payload = new MemoryStream((int)fileInfo.Length);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        long bytesRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            await payload.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            hash.AppendData(buffer, 0, read);
            bytesRead += read;
            progress?.Report(new TranscriptionProgress("Reading approved audio", bytesRead / (double)fileInfo.Length));
        }

        var sourceFingerprint = $"sha256:{Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()}";
        progress?.Report(new TranscriptionProgress("Uploading approved audio", 0));
        using var request = new HttpRequestMessage(HttpMethod.Post, InteractionsEndpoint)
        {
            Content = JsonContent.Create(new GeminiInteractionRequest(
                _options.ModelId,
                [new GeminiAudioInput(Convert.ToBase64String(payload.ToArray()), contentType)],
                new GeminiGenerationConfig(new GeminiTranscriptionConfig(new GeminiTranscriptionMode("verbatim", ["word"])))))
        };
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GeminiTranscriptionException(
                $"Gemini transcription failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode);
        }

        progress?.Report(new TranscriptionProgress("Parsing word timings", 0.9));
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var interaction = await JsonSerializer.DeserializeAsync<GeminiInteractionResponse>(responseStream, cancellationToken: cancellationToken)
            ?? throw new GeminiTranscriptionException("Gemini returned an empty transcription response.");
        if (!string.Equals(interaction.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new GeminiTranscriptionException($"Gemini transcription did not complete (status: {interaction.Status ?? "unknown"}).");
        }

        var words = interaction.Steps?
            .SelectMany(step => step.Content ?? [])
            .SelectMany(content => content.Annotations ?? [])
            .Where(annotation => string.Equals(annotation.Type, "word_info", StringComparison.OrdinalIgnoreCase))
            .Select(ToTranscriptWord)
            .ToArray() ?? [];
        if (words.Length == 0)
        {
            throw new GeminiTranscriptionException("Gemini returned no source-timed word annotations; the result was rejected as ungrounded.");
        }

        progress?.Report(new TranscriptionProgress("Completed", 1));
        return new TranscriptResult(source, sourceFingerprint, words);
    }

    private static TranscriptWord ToTranscriptWord(GeminiWordAnnotation annotation)
    {
        if (string.IsNullOrWhiteSpace(annotation.Text) ||
            !TryParseOffset(annotation.StartOffset, out var start) ||
            !TryParseOffset(annotation.EndOffset, out var end))
        {
            throw new GeminiTranscriptionException("Gemini returned an invalid source-timed word annotation.");
        }

        try
        {
            return new TranscriptWord(annotation.Text, start, end);
        }
        catch (ArgumentException exception)
        {
            throw new GeminiTranscriptionException("Gemini returned invalid word timing.", innerException: exception);
        }
    }

    private static bool TryParseOffset(string? value, out TimeSpan offset)
    {
        offset = default;
        if (string.IsNullOrWhiteSpace(value) || !value.EndsWith('s'))
        {
            return false;
        }

        if (!double.TryParse(value[..^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds) ||
            seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return false;
        }

        offset = TimeSpan.FromSeconds(seconds);
        return true;
    }

    private static string GetAudioContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".aac" => "audio/aac",
        ".aiff" or ".aif" => "audio/aiff",
        ".flac" => "audio/flac",
        ".m4a" => "audio/m4a",
        ".mp3" => "audio/mp3",
        ".ogg" => "audio/ogg",
        ".opus" => "audio/opus",
        ".wav" => "audio/wav",
        ".webm" => "audio/webm",
        _ => throw new InvalidDataException("Gemini transcription requires a supported audio file (WAV, MP3, M4A, FLAC, OGG, OPUS, AAC, AIFF or WebM).")
    };

    private sealed record GeminiInteractionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<GeminiAudioInput> Input,
        [property: JsonPropertyName("generation_config")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiAudioInput(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("data")] string Data,
        [property: JsonPropertyName("mime_type")] string MimeType)
    {
        public GeminiAudioInput(string data, string mimeType) : this("audio", data, mimeType) { }
    }

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("transcription_config")] GeminiTranscriptionConfig TranscriptionConfig);

    private sealed record GeminiTranscriptionConfig(
        [property: JsonPropertyName("mode")] GeminiTranscriptionMode Mode);

    private sealed record GeminiTranscriptionMode(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("timestamp_granularities")] IReadOnlyList<string> TimestampGranularities);

    private sealed class GeminiInteractionResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("steps")]
        public GeminiStep[]? Steps { get; set; }
    }

    private sealed class GeminiStep
    {
        [JsonPropertyName("content")]
        public GeminiContent[]? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("annotations")]
        public GeminiWordAnnotation[]? Annotations { get; set; }
    }

    private sealed class GeminiWordAnnotation
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("start_offset")]
        public string? StartOffset { get; set; }

        [JsonPropertyName("end_offset")]
        public string? EndOffset { get; set; }
    }
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
