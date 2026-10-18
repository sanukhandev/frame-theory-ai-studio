using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Domain;

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
    private const int MaximumWords = 100_000;

    public TranscriptResult(Uri source, string sourceFingerprint, IEnumerable<TranscriptWord> words)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsFile || string.IsNullOrWhiteSpace(sourceFingerprint))
        {
            throw new ArgumentException("A local source URI and source fingerprint are required.", nameof(source));
        }

        ArgumentNullException.ThrowIfNull(words);
        var orderedWords = words.Take(MaximumWords + 1).OrderBy(word => word.Start).ToArray();
        if (orderedWords.Length == 0 || orderedWords.Length > MaximumWords || orderedWords.Zip(orderedWords.Skip(1), (previous, current) => previous.End <= current.Start).Any(isValid => !isValid))
        {
            throw new ArgumentException("Transcript words must be non-empty, non-overlapping and within the supported limit.", nameof(words));
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
    private const int MaximumResponseBytes = 4 * 1024 * 1024;
    private const long MaximumFilesApiBytes = 2L * 1024 * 1024 * 1024;
    private static readonly Uri InteractionsEndpoint = new("https://generativelanguage.googleapis.com/v1beta/interactions");
    private static readonly Uri FilesUploadEndpoint = new("https://generativelanguage.googleapis.com/upload/v1beta/files");
    private static readonly Uri FilesApiEndpoint = new("https://generativelanguage.googleapis.com/v1beta/");
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

        if (LocalPathSecurity.ContainsReparsePoint(sourcePath))
        {
            throw new InvalidDataException("Symlinked transcription sources are not accepted.");
        }

        var contentType = GetAudioContentType(sourcePath);
        var fileInfo = new FileInfo(sourcePath);
        if (fileInfo.Length == 0)
        {
            throw new InvalidDataException("The transcription source is empty.");
        }

        if (fileInfo.Length > MaximumFilesApiBytes)
        {
            throw new InvalidDataException("Gemini transcription supports audio files up to 2 GiB through the Files API.");
        }

        progress?.Report(new TranscriptionProgress("Reading approved audio", 0));
        await using var stream = File.OpenRead(sourcePath);
        using var payload = fileInfo.Length <= MaximumInlineBytes ? new MemoryStream((int)fileInfo.Length) : null;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        long bytesRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            if (payload is not null)
            {
                await payload.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            hash.AppendData(buffer, 0, read);
            bytesRead += read;
            progress?.Report(new TranscriptionProgress("Reading approved audio", bytesRead / (double)fileInfo.Length));
        }

        var sourceFingerprint = $"sha256:{Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()}";
        GeminiUploadedFile? uploadedFile = null;
        try
        {
            GeminiAudioInput input;
            if (payload is not null)
            {
                progress?.Report(new TranscriptionProgress("Uploading approved audio", 0));
                input = GeminiAudioInput.FromInline(Convert.ToBase64String(payload.ToArray()), contentType);
            }
            else
            {
                progress?.Report(new TranscriptionProgress("Uploading approved audio via Gemini Files API", 0));
                uploadedFile = await UploadFileAsync(sourcePath, contentType, fileInfo.Length, cancellationToken);
                progress?.Report(new TranscriptionProgress("Uploading approved audio via Gemini Files API", 0.8));
                input = GeminiAudioInput.FromUri(uploadedFile.Uri, uploadedFile.MimeType);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, InteractionsEndpoint)
            {
                Content = JsonContent.Create(new GeminiInteractionRequest(
                    _options.ModelId,
                    [input],
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
            var responseBytes = await ReadBoundedAsync(responseStream, cancellationToken);
            GeminiInteractionResponse interaction;
            try
            {
                interaction = JsonSerializer.Deserialize<GeminiInteractionResponse>(responseBytes)
                    ?? throw new GeminiTranscriptionException("Gemini returned an empty transcription response.");
            }
            catch (JsonException exception)
            {
                throw new GeminiTranscriptionException("Gemini returned malformed transcription JSON.", innerException: exception);
            }
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
        finally
        {
            if (uploadedFile is not null)
            {
                using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await DeleteFileAsync(uploadedFile.Name, cleanupTimeout.Token);
            }
        }
    }

    private async Task<GeminiUploadedFile> UploadFileAsync(
        string sourcePath,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken)
    {
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, FilesUploadEndpoint)
        {
            Content = JsonContent.Create(new GeminiFileUploadStartRequest(
                new GeminiFileMetadata(Path.GetFileName(sourcePath))))
        };
        startRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Protocol", "resumable");
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "start");
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Length", contentLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Type", contentType);

        using var startResponse = await _httpClient.SendAsync(startRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!startResponse.IsSuccessStatusCode)
        {
            throw new GeminiTranscriptionException(
                $"Gemini Files API upload initialization failed with HTTP {(int)startResponse.StatusCode} ({startResponse.StatusCode}).",
                startResponse.StatusCode);
        }

        var uploadUrl = GetHeader(startResponse, "x-goog-upload-url");
        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri) ||
            uploadUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uploadUri.Host, FilesUploadEndpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new GeminiTranscriptionException("Gemini returned an invalid Files API upload URL.");
        }

        await using var sourceStream = File.OpenRead(sourcePath);
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUri)
        {
            Content = new StreamContent(sourceStream)
        };
        uploadRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Offset", "0");
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "upload, finalize");
        uploadRequest.Content.Headers.ContentLength = contentLength;
        uploadRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

        using var uploadResponse = await _httpClient.SendAsync(uploadRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new GeminiTranscriptionException(
                $"Gemini Files API upload failed with HTTP {(int)uploadResponse.StatusCode} ({uploadResponse.StatusCode}).",
                uploadResponse.StatusCode);
        }

        var upload = await uploadResponse.Content.ReadFromJsonAsync<GeminiFileUploadResponse>(cancellationToken: cancellationToken)
            ?? throw new GeminiTranscriptionException("Gemini Files API returned an empty upload response.");
        var file = upload.File;
        if (file is null || string.IsNullOrWhiteSpace(file.Name) || string.IsNullOrWhiteSpace(file.Uri))
        {
            throw new GeminiTranscriptionException("Gemini Files API returned an incomplete upload response.");
        }

        if (!Uri.TryCreate(file.Uri, UriKind.Absolute, out var fileUri) ||
            fileUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(fileUri.Host, FilesUploadEndpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            await DeleteFileAsync(file.Name, cancellationToken);
            throw new GeminiTranscriptionException("Gemini returned an invalid Files API file URI.");
        }

        var mimeType = string.IsNullOrWhiteSpace(file.MimeType) ? file.LegacyMimeType : file.MimeType;
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            mimeType = contentType;
        }

        return new GeminiUploadedFile(file.Name, fileUri.AbsoluteUri, mimeType);
    }

    private async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
    {
        if (!fileName.StartsWith("files/", StringComparison.Ordinal) || fileName.Contains("..", StringComparison.Ordinal) || fileName.Contains('?') || fileName.Contains('#'))
        {
            throw new GeminiTranscriptionException("Gemini returned an invalid uploaded file name; cleanup was refused.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(FilesApiEndpoint, fileName));
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GeminiTranscriptionException(
                $"Gemini temporary file cleanup failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode);
        }
    }

    private static string? GetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
        {
            return values.SingleOrDefault();
        }

        return response.Content.Headers.TryGetValues(name, out values) ? values.SingleOrDefault() : null;
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

    private static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var payload = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            if (payload.Length > MaximumResponseBytes - bytesRead)
            {
                throw new GeminiTranscriptionException("Gemini returned a transcription response larger than the supported limit.");
            }

            payload.Write(buffer, 0, bytesRead);
        }

        return payload.ToArray();
    }

    private sealed record GeminiInteractionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<GeminiAudioInput> Input,
        [property: JsonPropertyName("generation_config")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiAudioInput(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("data")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Data,
        [property: JsonPropertyName("uri")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Uri,
        [property: JsonPropertyName("mime_type")] string MimeType)
    {
        public static GeminiAudioInput FromInline(string data, string mimeType) => new("audio", data, null, mimeType);

        public static GeminiAudioInput FromUri(string uri, string mimeType) => new("audio", null, uri, mimeType);
    }

    private sealed record GeminiFileUploadStartRequest(
        [property: JsonPropertyName("file")] GeminiFileMetadata File);

    private sealed record GeminiFileMetadata(
        [property: JsonPropertyName("display_name")] string DisplayName);

    private sealed class GeminiFileUploadResponse
    {
        [JsonPropertyName("file")]
        public GeminiFile? File { get; set; }
    }

    private sealed class GeminiFile
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("mime_type")]
        public string? LegacyMimeType { get; set; }
    }

    private sealed record GeminiUploadedFile(string Name, string Uri, string MimeType);

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

        var sourcePath = source.LocalPath;
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The transcript source was not found.", sourcePath);
        }

        if (LocalPathSecurity.ContainsReparsePoint(sourcePath))
        {
            throw new InvalidDataException("Symlinked transcript sources are not accepted.");
        }

        if (!File.Exists(_transcriptPath))
        {
            throw new FileNotFoundException("The transcript JSON file was not found.", _transcriptPath);
        }

        if (LocalPathSecurity.ContainsReparsePoint(_transcriptPath))
        {
            throw new InvalidDataException("Symlinked transcript files are not accepted.");
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

        var sourceFingerprint = await TranscriptSourceFingerprint.ComputeAsync(source, cancellationToken);
        if (!string.Equals(document.SourceFingerprint, sourceFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The transcript source fingerprint does not match the selected media.");
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

public static class TranscriptSourceFingerprint
{
    public static async Task<string> ComputeAsync(Uri source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        if (!source.IsFile)
        {
            throw new InvalidDataException("Transcript sources must be local files.");
        }

        var sourcePath = source.LocalPath;
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The transcript source was not found.", sourcePath);
        }

        if (LocalPathSecurity.ContainsReparsePoint(sourcePath))
        {
            throw new InvalidDataException("Symlinked transcript sources are not accepted.");
        }

        await using var stream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            hash.AppendData(buffer.AsSpan(0, bytesRead));
        }

        return $"sha256:{Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()}";
    }
}
