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
                            [new GeminiTextInput("text", prompt)],
                            previousInteractionId))
                    };
                    request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    if (IsRetryable(response.StatusCode) && attempts < _options.MaximumAttempts)
                    {
                        _diagnostics?.Record("gemini.follow-up.retry");
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
                        prompt.Length,
                        result.Usage));
                    return result;
                }
                catch (HttpRequestException exception) when (attempts < _options.MaximumAttempts)
                {
                    _diagnostics?.Record("gemini.follow-up.retry", exception);
                    await Task.Delay(TimeSpan.FromTicks(_options.RetryDelay.Ticks * attempts), cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempts < _options.MaximumAttempts)
                {
                    _diagnostics?.Record("gemini.follow-up.retry");
                    await Task.Delay(TimeSpan.FromTicks(_options.RetryDelay.Ticks * attempts), cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new GeminiProviderException("Gemini follow-up timed out.", GeminiProviderFailureKind.Timeout);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var exception = new GeminiProviderException("Gemini follow-up was canceled.", GeminiProviderFailureKind.Cancellation);
            RecordFailure(exception, attempts, prompt.Length);
            throw exception;
        }
        catch (GeminiProviderException exception)
        {
            RecordFailure(exception, attempts, prompt.Length);
            throw;
        }
        catch (HttpRequestException exception)
        {
            var wrapped = new GeminiProviderException("Gemini follow-up could not reach the provider.", GeminiProviderFailureKind.Transport, innerException: exception);
            RecordFailure(wrapped, attempts, prompt.Length);
            throw wrapped;
        }

        var exhausted = new GeminiProviderException("Gemini follow-up exhausted its retry budget.", GeminiProviderFailureKind.Transport);
        RecordFailure(exhausted, attempts, prompt.Length);
        throw exhausted;
    }

    private void RecordFailure(GeminiProviderException exception, int attempts, int promptCharacterCount)
    {
        _diagnostics?.Record("gemini.follow-up.failed", exception);
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
        [property: JsonPropertyName("input")] IReadOnlyList<GeminiTextInput> Input,
        [property: JsonPropertyName("previous_interaction_id")] string? PreviousInteractionId);

    private sealed record GeminiTextInput(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text);

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
