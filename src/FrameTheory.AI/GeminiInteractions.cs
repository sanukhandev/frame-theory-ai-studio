using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Application;
using FrameTheory.Domain;

namespace FrameTheory.AI;

public enum GeminiProviderFailureKind
{
    InvalidKey,
    RateLimit,
    Quota,
    Timeout,
    Cancellation,
    Transport,
    MalformedResponse
}

public sealed class GeminiProviderException : InvalidOperationException
{
    public GeminiProviderException(
        string message,
        GeminiProviderFailureKind failureKind,
        HttpStatusCode? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public GeminiProviderFailureKind FailureKind { get; }

    public HttpStatusCode? StatusCode { get; }
}

public sealed record GeminiClientOptions
{
    public GeminiClientOptions(
        string apiKey,
        string modelId,
        TimeSpan? timeout = null,
        int maximumAttempts = 3,
        TimeSpan? retryDelay = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > 4096)
        {
            throw new ArgumentException("A user-provided Gemini API key is required.", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(modelId) || modelId.Length > 128 || modelId.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid Gemini model is required.", nameof(modelId));
        }

        var resolvedTimeout = timeout ?? TimeSpan.FromMinutes(2);
        var resolvedRetryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
        if (resolvedTimeout <= TimeSpan.Zero || resolvedTimeout > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Gemini request timeout must be between one millisecond and ten minutes.");
        }

        if (maximumAttempts is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Gemini retries are bounded to one through three attempts.");
        }

        if (resolvedRetryDelay < TimeSpan.Zero || resolvedRetryDelay > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "Gemini retry delay must be between zero and thirty seconds.");
        }

        ApiKey = apiKey.Trim();
        ModelId = modelId.Trim();
        Timeout = resolvedTimeout;
        MaximumAttempts = maximumAttempts;
        RetryDelay = resolvedRetryDelay;
    }

    public string ApiKey { get; }

    public string ModelId { get; }

    public TimeSpan Timeout { get; }

    public int MaximumAttempts { get; }

    public TimeSpan RetryDelay { get; }
}

public sealed record GeminiUsage(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens);

public sealed record GeminiInteractionResult(
    string InteractionId,
    string OutputText,
    GeminiUsage? Usage,
    int Attempts);

public sealed record GeminiInteractionInput(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Text,
    [property: JsonPropertyName("data")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Data,
    [property: JsonPropertyName("uri")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Uri,
    [property: JsonPropertyName("mime_type")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MimeType)
{
    public static GeminiInteractionInput CreateText(string text) => new("text", text, null, null, null);

    public static GeminiInteractionInput CreateImage(byte[] png) => new("image", null, Convert.ToBase64String(png), null, "image/png");

    public static GeminiInteractionInput CreateImageUri(string uri, string mimeType) => new("image", null, null, uri, mimeType);
}

internal sealed record GeminiUploadedFile(string Name, string Uri, string MimeType);

public sealed record GeminiInteractionRecord(
    DateTimeOffset Timestamp,
    string ModelId,
    string? InteractionId,
    bool Succeeded,
    GeminiProviderFailureKind? FailureKind,
    HttpStatusCode? StatusCode,
    int Attempts,
    int PromptCharacterCount,
    GeminiUsage? Usage,
    string? OperationName = null,
    string? ScopeSummary = null);

public interface IGeminiInteractionHistory
{
    string LogPath { get; }

    void Record(GeminiInteractionRecord record);
}

public interface IGeminiInteractionHistoryReader
{
    IReadOnlyList<GeminiInteractionRecord> ReadRecent(int maximumEntries = 20);
}

public sealed class FileGeminiInteractionHistory : IGeminiInteractionHistory, IGeminiInteractionHistoryReader
{
    private readonly object _sync = new();

    public FileGeminiInteractionHistory(string? logPath = null)
    {
        LogPath = string.IsNullOrWhiteSpace(logPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FrameTheory", "gemini-usage.jsonl")
            : Path.GetFullPath(logPath);
    }

    public string LogPath { get; }

    public void Record(GeminiInteractionRecord record)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            if (LocalPathSecurity.ContainsReparsePoint(LogPath))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            var line = JsonSerializer.Serialize(record) + Environment.NewLine;
            lock (_sync)
            {
                File.AppendAllText(LogPath, line);
            }
        }
        catch
        {
        }
    }

    public IReadOnlyList<GeminiInteractionRecord> ReadRecent(int maximumEntries = 20)
    {
        if (maximumEntries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        }

        try
        {
            if (LocalPathSecurity.ContainsReparsePoint(LogPath))
            {
                return [];
            }

            lock (_sync)
            {
                if (!File.Exists(LogPath))
                {
                    return [];
                }

                var records = new Queue<GeminiInteractionRecord>(maximumEntries);
                foreach (var line in File.ReadLines(LogPath))
                {
                    try
                    {
                        if (JsonSerializer.Deserialize<GeminiInteractionRecord>(line) is { } record)
                        {
                            if (records.Count == maximumEntries)
                            {
                                records.Dequeue();
                            }

                            records.Enqueue(record);
                        }
                    }
                    catch (JsonException)
                    {
                    }
                }

                return records.ToArray();
            }
        }
        catch
        {
            return [];
        }
    }
}

public interface IGeminiClient
{
    Task<GeminiInteractionResult> CreateFollowUpAsync(
        string prompt,
        string? previousInteractionId,
        CancellationToken cancellationToken);

    Task<GeminiInteractionResult> CreatePlanAsync(
        IReadOnlyList<GeminiInteractionInput> inputs,
        JsonElement responseSchema,
        CancellationToken cancellationToken);
}

public sealed class GeminiInteractionsClient : IGeminiClient
{
    private const int MaximumInlineImageBytes = 20 * 1024 * 1024;
    private const int MaximumResponseBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan MaximumRetryWait = TimeSpan.FromSeconds(30);
    private static readonly Uri InteractionsEndpoint = new("https://generativelanguage.googleapis.com/v1beta/interactions");
    private readonly HttpClient _httpClient;
    private readonly GeminiClientOptions _options;
    private readonly IGeminiInteractionHistory? _history;
    private readonly IDiagnosticsLog? _diagnostics;

    public GeminiInteractionsClient(
        HttpClient httpClient,
        GeminiClientOptions options,
        IGeminiInteractionHistory? history = null,
        IDiagnosticsLog? diagnostics = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _history = history;
        _diagnostics = diagnostics;
    }

    internal async Task<GeminiUploadedFile> UploadFileAsync(
        byte[] payload,
        string contentType,
        string displayName,
        CancellationToken cancellationToken)
    {
        var filesEndpoint = new Uri("https://generativelanguage.googleapis.com/upload/v1beta/files");
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, filesEndpoint)
        {
            Content = JsonContent.Create(new GeminiFileUploadStartRequest(new GeminiFileMetadata(displayName)))
        };
        startRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Protocol", "resumable");
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "start");
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Length", payload.LongLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Type", contentType);

        using var startResponse = await _httpClient.SendAsync(startRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!startResponse.IsSuccessStatusCode)
        {
            throw new GeminiProviderException($"Gemini Files API upload initialization failed with HTTP {(int)startResponse.StatusCode} ({startResponse.StatusCode}).", GeminiProviderFailureKind.Transport, startResponse.StatusCode);
        }

        var uploadUrl = GetHeader(startResponse, "x-goog-upload-url");
        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri) ||
            uploadUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uploadUri.Host, filesEndpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new GeminiProviderException("Gemini returned an invalid Files API upload URL.", GeminiProviderFailureKind.MalformedResponse);
        }

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUri)
        {
            Content = new ByteArrayContent(payload)
        };
        uploadRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Offset", "0");
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "upload, finalize");
        uploadRequest.Content.Headers.ContentLength = payload.LongLength;
        uploadRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

        using var uploadResponse = await _httpClient.SendAsync(uploadRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            throw new GeminiProviderException($"Gemini Files API upload failed with HTTP {(int)uploadResponse.StatusCode} ({uploadResponse.StatusCode}).", GeminiProviderFailureKind.Transport, uploadResponse.StatusCode);
        }

        var upload = await uploadResponse.Content.ReadFromJsonAsync<GeminiFileUploadResponse>(cancellationToken: cancellationToken)
            ?? throw new GeminiProviderException("Gemini Files API returned an empty upload response.", GeminiProviderFailureKind.MalformedResponse);
        var file = upload.File;
        if (file is null || string.IsNullOrWhiteSpace(file.Name) || string.IsNullOrWhiteSpace(file.Uri))
        {
            throw new GeminiProviderException("Gemini Files API returned an incomplete upload response.", GeminiProviderFailureKind.MalformedResponse);
        }

        if (!file.Name.StartsWith("files/", StringComparison.Ordinal) || file.Name.Contains("..", StringComparison.Ordinal) || file.Name.Contains('?') || file.Name.Contains('#'))
        {
            throw new GeminiProviderException("Gemini returned an invalid Files API file name.", GeminiProviderFailureKind.MalformedResponse);
        }

        if (!Uri.TryCreate(file.Uri, UriKind.Absolute, out var fileUri) ||
            fileUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(fileUri.Host, filesEndpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await DeleteFileAsync(file.Name, cleanupTimeout.Token);
            throw new GeminiProviderException("Gemini returned an invalid Files API file URI.", GeminiProviderFailureKind.MalformedResponse);
        }

        return new GeminiUploadedFile(file.Name, fileUri.AbsoluteUri, string.IsNullOrWhiteSpace(file.MimeType) ? contentType : file.MimeType);
    }

    internal async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
    {
        if (!fileName.StartsWith("files/", StringComparison.Ordinal) || fileName.Contains("..", StringComparison.Ordinal) || fileName.Contains('?') || fileName.Contains('#'))
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri($"https://generativelanguage.googleapis.com/v1beta/{fileName}"));
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GeminiProviderException($"Gemini temporary file cleanup failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).", GeminiProviderFailureKind.Transport, response.StatusCode);
        }
    }

    public async Task<GeminiInteractionResult> CreateFollowUpAsync(
        string prompt,
        string? previousInteractionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 32_000)
        {
            throw new ArgumentException("A follow-up prompt between one and 32,000 characters is required.", nameof(prompt));
        }

        if (previousInteractionId is not null && (string.IsNullOrWhiteSpace(previousInteractionId) || previousInteractionId.Length > 256))
        {
            throw new ArgumentException("The previous Gemini interaction ID is invalid.", nameof(previousInteractionId));
        }

        return await CreateInteractionAsync(
            [GeminiInteractionInput.CreateText(prompt)],
            previousInteractionId,
            responseSchema: null,
            "follow-up",
            cancellationToken);
    }

    public async Task<GeminiInteractionResult> CreatePlanAsync(
        IReadOnlyList<GeminiInteractionInput> inputs,
        JsonElement responseSchema,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var prompt = inputs.Count > 0 ? inputs[0].Text : null;
        if (inputs.Count is < 2 or > 64 || inputs[0].Type != "text" || string.IsNullOrWhiteSpace(prompt) || prompt?.Length > 32_000)
        {
            throw new ArgumentException("A Gemini plan requires one prompt of at most 32,000 characters and one to sixty-three bounded media inputs.", nameof(inputs));
        }

        if (inputs.Skip(1).Any(input =>
            input.Type == "image"
                ? !IsValidImageInput(input)
                : input.Type != "text" || string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 1_048_576 || input.Data is not null || input.Uri is not null))
        {
            throw new ArgumentException("A Gemini plan accepts only inline PNGs, validated Gemini-hosted PNG URIs and bounded text metadata.", nameof(inputs));
        }

        if (responseSchema.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("The Gemini plan response schema must be a JSON object.", nameof(responseSchema));
        }

        return await CreateInteractionAsync(inputs, null, responseSchema, "plan", cancellationToken);
    }

    private static bool IsValidImageInput(GeminiInteractionInput input)
    {
        if (input.MimeType != "image/png")
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(input.Data) && input.Uri is null)
        {
            if (input.Data.Length > ((MaximumInlineImageBytes + 2) / 3) * 4)
            {
                return false;
            }

            try
            {
                return Convert.FromBase64String(input.Data).LongLength <= MaximumInlineImageBytes;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        return input.Data is null &&
            Uri.TryCreate(input.Uri, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            string.Equals(uri.Host, "generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<GeminiInteractionResult> CreateInteractionAsync(
        IReadOnlyList<GeminiInteractionInput> inputs,
        string? previousInteractionId,
        JsonElement? responseSchema,
        string operationName,
        CancellationToken cancellationToken)
    {
        var promptCharacterCount = inputs
            .Where(input => input.Text is not null)
            .Sum(input => input.Text!.Length);
        var scopeSummary = DescribeInputScope(inputs);
        var attempts = 0;
        try
        {
            for (attempts = 1; attempts <= _options.MaximumAttempts; attempts++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(_options.Timeout);
                    using var request = new HttpRequestMessage(HttpMethod.Post, InteractionsEndpoint)
                    {
                        Content = JsonContent.Create(new GeminiInteractionRequest(
                            _options.ModelId,
                            inputs,
                            previousInteractionId,
                            responseSchema is { } schema
                                ? new GeminiResponseFormat("text", "application/json", schema)
                                : null))
                    };
                    request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    if (IsRetryable(response.StatusCode) && attempts < _options.MaximumAttempts)
                    {
                        _diagnostics?.Record($"gemini.{operationName}.retry");
                        await Task.Delay(CalculateRetryDelay(_options.RetryDelay, attempts, GetRetryAfter(response)), cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw CreateProviderException(response.StatusCode);
                    }

                    await using var responseStream = await response.Content.ReadAsStreamAsync(timeout.Token);
                    var responseBytes = await ReadBoundedAsync(responseStream, timeout.Token);
                    GeminiInteractionResponse interaction;
                    try
                    {
                        interaction = JsonSerializer.Deserialize<GeminiInteractionResponse>(responseBytes)
                            ?? throw new GeminiProviderException("Gemini returned an empty response.", GeminiProviderFailureKind.MalformedResponse);
                    }
                    catch (JsonException exception)
                    {
                        throw new GeminiProviderException("Gemini returned malformed JSON.", GeminiProviderFailureKind.MalformedResponse, innerException: exception);
                    }
                    if (!string.Equals(interaction.Status, "completed", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(interaction.Id))
                    {
                        throw new GeminiProviderException("Gemini returned an incomplete interaction.", GeminiProviderFailureKind.MalformedResponse);
                    }

                    var outputText = interaction.OutputText ?? interaction.Steps?
                        .SelectMany(step => step.Content ?? [])
                        .Where(content => string.Equals(content.Type, "text", StringComparison.OrdinalIgnoreCase))
                        .Select(content => content.Text)
                        .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
                    if (string.IsNullOrWhiteSpace(outputText))
                    {
                        throw new GeminiProviderException("Gemini returned no text output.", GeminiProviderFailureKind.MalformedResponse);
                    }

                    var result = new GeminiInteractionResult(interaction.Id, outputText, interaction.Usage?.ToUsage(), attempts);
                    _history?.Record(new GeminiInteractionRecord(
                        DateTimeOffset.UtcNow,
                        _options.ModelId,
                        result.InteractionId,
                        Succeeded: true,
                        FailureKind: null,
                        StatusCode: response.StatusCode,
                        attempts,
                        promptCharacterCount,
                        result.Usage,
                        OperationName: operationName,
                        ScopeSummary: scopeSummary));
                    return result;
                }
                catch (HttpRequestException exception) when (attempts < _options.MaximumAttempts)
                {
                    _diagnostics?.Record($"gemini.{operationName}.retry", exception);
                    await Task.Delay(CalculateRetryDelay(_options.RetryDelay, attempts, null), cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempts < _options.MaximumAttempts)
                {
                    _diagnostics?.Record($"gemini.{operationName}.retry");
                    await Task.Delay(CalculateRetryDelay(_options.RetryDelay, attempts, null), cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new GeminiProviderException($"Gemini {operationName} timed out.", GeminiProviderFailureKind.Timeout);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var exception = new GeminiProviderException($"Gemini {operationName} was canceled.", GeminiProviderFailureKind.Cancellation);
            RecordFailure(operationName, exception, attempts, promptCharacterCount, scopeSummary);
            throw exception;
        }
        catch (GeminiProviderException exception)
        {
            RecordFailure(operationName, exception, attempts, promptCharacterCount, scopeSummary);
            throw;
        }
        catch (HttpRequestException exception)
        {
            var wrapped = new GeminiProviderException($"Gemini {operationName} could not reach the provider.", GeminiProviderFailureKind.Transport, innerException: exception);
            RecordFailure(operationName, wrapped, attempts, promptCharacterCount, scopeSummary);
            throw wrapped;
        }

        var exhausted = new GeminiProviderException($"Gemini {operationName} exhausted its retry budget.", GeminiProviderFailureKind.Transport);
        RecordFailure(operationName, exhausted, attempts, promptCharacterCount, scopeSummary);
        throw exhausted;
    }

    private static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var payload = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            if (payload.Length > MaximumResponseBytes - bytesRead)
            {
                throw new GeminiProviderException("Gemini returned a response larger than the supported limit.", GeminiProviderFailureKind.MalformedResponse);
            }

            payload.Write(buffer, 0, bytesRead);
        }

        return payload.ToArray();
    }

    private void RecordFailure(string operationName, GeminiProviderException exception, int attempts, int promptCharacterCount, string scopeSummary)
    {
        _diagnostics?.Record($"gemini.{operationName}.failed", exception);
        _history?.Record(new GeminiInteractionRecord(
            DateTimeOffset.UtcNow,
            _options.ModelId,
            null,
            Succeeded: false,
            exception.FailureKind,
            exception.StatusCode,
            attempts,
            promptCharacterCount,
            null,
            OperationName: operationName,
            ScopeSummary: scopeSummary));
    }

    private static string DescribeInputScope(IReadOnlyList<GeminiInteractionInput> inputs)
    {
        var textCount = inputs.Count(input => input.Type == "text");
        var inlineImageCount = inputs.Count(input => input.Type == "image" && input.Data is not null);
        var hostedImageCount = inputs.Count(input => input.Type == "image" && input.Uri is not null);
        return $"text={textCount}; inline_images={inlineImageCount}; hosted_images={hostedImageCount}";
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == (HttpStatusCode)429 ||
        (int)statusCode >= 500;

    internal static TimeSpan CalculateRetryDelay(TimeSpan baseDelay, int attempt, TimeSpan? serverDelay)
    {
        if (attempt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt));
        }

        var multiplier = 1L << Math.Min(attempt - 1, 5);
        var maximumTicks = MaximumRetryWait.Ticks;
        var exponentialTicks = baseDelay.Ticks > maximumTicks / multiplier
            ? maximumTicks
            : baseDelay.Ticks * multiplier;
        var exponentialDelay = TimeSpan.FromTicks(exponentialTicks);
        if (serverDelay is not { } requestedDelay || requestedDelay <= TimeSpan.Zero)
        {
            return exponentialDelay;
        }

        return requestedDelay > MaximumRetryWait ? MaximumRetryWait : requestedDelay;
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        return retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null;
    }

    private static string? GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values.SingleOrDefault()
            : response.Content.Headers.TryGetValues(name, out values) ? values.SingleOrDefault() : null;

    private static GeminiProviderException CreateProviderException(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new GeminiProviderException("Gemini rejected the configured API key.", GeminiProviderFailureKind.InvalidKey, statusCode),
        (HttpStatusCode)429 => new GeminiProviderException("Gemini rate limit reached after the bounded retry budget.", GeminiProviderFailureKind.RateLimit, statusCode),
        HttpStatusCode.PaymentRequired => new GeminiProviderException("Gemini reported a quota or billing limit.", GeminiProviderFailureKind.Quota, statusCode),
        _ => new GeminiProviderException($"Gemini follow-up failed with HTTP {(int)statusCode} ({statusCode}).", GeminiProviderFailureKind.Transport, statusCode)
    };

    private sealed record GeminiFileMetadata([property: JsonPropertyName("display_name")] string DisplayName);

    private sealed record GeminiFileUploadStartRequest([property: JsonPropertyName("file")] GeminiFileMetadata File);

    private sealed class GeminiFileUploadResponse
    {
        [JsonPropertyName("file")]
        public GeminiFileUpload? File { get; set; }
    }

    private sealed class GeminiFileUpload
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("mime_type")]
        public string? MimeType { get; set; }
    }

    private sealed record GeminiInteractionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<GeminiInteractionInput> Input,
        [property: JsonPropertyName("previous_interaction_id")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PreviousInteractionId,
        [property: JsonPropertyName("response_format")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] GeminiResponseFormat? ResponseFormat);

    private sealed record GeminiResponseFormat(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("mime_type")] string MimeType,
        [property: JsonPropertyName("schema")] JsonElement Schema);

    private sealed class GeminiInteractionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("output_text")]
        public string? OutputText { get; set; }

        [JsonPropertyName("steps")]
        public GeminiStep[]? Steps { get; set; }

        [JsonPropertyName("usage")]
        public GeminiUsageDocument? Usage { get; set; }
    }

    private sealed class GeminiStep
    {
        [JsonPropertyName("content")]
        public GeminiContent[]? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class GeminiUsageDocument
    {
        [JsonPropertyName("total_input_tokens")]
        public int? TotalInputTokens { get; set; }

        [JsonPropertyName("total_output_tokens")]
        public int? TotalOutputTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }

        public GeminiUsage ToUsage() => new(TotalInputTokens, TotalOutputTokens, TotalTokens);
    }
}
