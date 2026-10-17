using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Application;

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

        if (timeout is null || timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Gemini request timeout must be between one millisecond and ten minutes.");
        }

        if (maximumAttempts is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Gemini retries are bounded to one through three attempts.");
        }

        if (retryDelay is null || retryDelay < TimeSpan.Zero || retryDelay > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "Gemini retry delay must be between zero and thirty seconds.");
        }

        ApiKey = apiKey.Trim();
        ModelId = modelId.Trim();
        Timeout = timeout.Value;
        MaximumAttempts = maximumAttempts;
        RetryDelay = retryDelay.Value;
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
}

public sealed record GeminiInteractionRecord(
    DateTimeOffset Timestamp,
    string ModelId,
    string? InteractionId,
    bool Succeeded,
    GeminiProviderFailureKind? FailureKind,
    HttpStatusCode? StatusCode,
    int Attempts,
    int PromptCharacterCount,
    GeminiUsage? Usage);

public interface IGeminiInteractionHistory
{
    string LogPath { get; }

    void Record(GeminiInteractionRecord record);
}

public sealed class FileGeminiInteractionHistory : IGeminiInteractionHistory
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
        if (inputs.Count is < 2 or > 64 || inputs[0].Type != "text" || string.IsNullOrWhiteSpace(inputs[0].Text))
        {
            throw new ArgumentException("A Gemini plan requires one prompt and one to sixty-three bounded media inputs.", nameof(inputs));
        }

        if (inputs.Skip(1).Any(input =>
            input.Type == "image"
                ? string.IsNullOrWhiteSpace(input.Data) || input.MimeType != "image/png" || input.Uri is not null
                : input.Type != "text" || string.IsNullOrWhiteSpace(input.Text) || input.Data is not null || input.Uri is not null))
        {
            throw new ArgumentException("A Gemini plan accepts only inline PNG samples and bounded text metadata.", nameof(inputs));
        }

        if (responseSchema.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("The Gemini plan response schema must be a JSON object.", nameof(responseSchema));
        }

        return await CreateInteractionAsync(inputs, null, responseSchema, "plan", cancellationToken);
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
                        await Task.Delay(TimeSpan.FromTicks(_options.RetryDelay.Ticks * attempts), cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw CreateProviderException(response.StatusCode);
                    }

                    await using var responseStream = await response.Content.ReadAsStreamAsync(timeout.Token);
                    var interaction = await JsonSerializer.DeserializeAsync<GeminiInteractionResponse>(responseStream, cancellationToken: timeout.Token)
                        ?? throw new GeminiProviderException("Gemini returned an empty response.", GeminiProviderFailureKind.MalformedResponse);
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
                        result.Usage));
                    return result;
                }
                catch (HttpRequestException exception) when (attempts < _options.MaximumAttempts)
                {
                    _diagnostics?.Record($"gemini.{operationName}.retry", exception);
                    await Task.Delay(TimeSpan.FromTicks(_options.RetryDelay.Ticks * attempts), cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempts < _options.MaximumAttempts)
                {
                    _diagnostics?.Record($"gemini.{operationName}.retry");
                    await Task.Delay(TimeSpan.FromTicks(_options.RetryDelay.Ticks * attempts), cancellationToken);
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
            RecordFailure(operationName, exception, attempts, promptCharacterCount);
            throw exception;
        }
        catch (GeminiProviderException exception)
        {
            RecordFailure(operationName, exception, attempts, promptCharacterCount);
            throw;
        }
        catch (HttpRequestException exception)
        {
            var wrapped = new GeminiProviderException($"Gemini {operationName} could not reach the provider.", GeminiProviderFailureKind.Transport, innerException: exception);
            RecordFailure(operationName, wrapped, attempts, promptCharacterCount);
            throw wrapped;
        }

        var exhausted = new GeminiProviderException($"Gemini {operationName} exhausted its retry budget.", GeminiProviderFailureKind.Transport);
        RecordFailure(operationName, exhausted, attempts, promptCharacterCount);
        throw exhausted;
    }

    private void RecordFailure(string operationName, GeminiProviderException exception, int attempts, int promptCharacterCount)
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
            null));
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == (HttpStatusCode)429 ||
        (int)statusCode >= 500;

    private static GeminiProviderException CreateProviderException(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new GeminiProviderException("Gemini rejected the configured API key.", GeminiProviderFailureKind.InvalidKey, statusCode),
        (HttpStatusCode)429 => new GeminiProviderException("Gemini rate limit reached after the bounded retry budget.", GeminiProviderFailureKind.RateLimit, statusCode),
        HttpStatusCode.PaymentRequired => new GeminiProviderException("Gemini reported a quota or billing limit.", GeminiProviderFailureKind.Quota, statusCode),
        _ => new GeminiProviderException($"Gemini follow-up failed with HTTP {(int)statusCode} ({statusCode}).", GeminiProviderFailureKind.Transport, statusCode)
    };

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
