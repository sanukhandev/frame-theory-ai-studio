using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Color;

namespace FrameTheory.AI;

public sealed class GeminiReferenceAnalysisRequest
{
    public GeminiReferenceAnalysisRequest(
        IReadOnlyList<LocalAnalysisSample> sourceSamples,
        IReadOnlyList<LocalAnalysisSample> referenceSamples,
        bool uploadConsent)
    {
        ArgumentNullException.ThrowIfNull(sourceSamples);
        ArgumentNullException.ThrowIfNull(referenceSamples);
        SourceSamples = VisualSamples(sourceSamples, nameof(sourceSamples));
        ReferenceSamples = VisualSamples(referenceSamples, nameof(referenceSamples));
        UploadConsent = uploadConsent;
    }

    public IReadOnlyList<LocalAnalysisSample> SourceSamples { get; }

    public IReadOnlyList<LocalAnalysisSample> ReferenceSamples { get; }

    public bool UploadConsent { get; }

    private static IReadOnlyList<LocalAnalysisSample> VisualSamples(
        IReadOnlyList<LocalAnalysisSample> samples,
        string parameterName)
    {
        var visual = samples
            .Where(sample => string.Equals(sample.SampleKind, "video-frame-ppm", StringComparison.Ordinal) ||
                string.Equals(sample.SampleKind, "photo-preview-ppm", StringComparison.Ordinal))
            .Take(3)
            .ToArray();
        if (visual.Length == 0)
        {
            throw new ArgumentException("At least one bounded visual PPM sample is required.", parameterName);
        }

        return visual;
    }
}

public sealed record GeminiReferenceAnalysisResult(ColorGrade Grade, string ProviderResponse, int Attempts);

public sealed class GeminiReferenceAnalysisException : InvalidOperationException
{
    public GeminiReferenceAnalysisException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}

public sealed class GeminiReferenceAnalysisClient
{
    private const int MaximumInlineBytes = 20 * 1024 * 1024;
    private static readonly Uri InteractionsEndpoint = new("https://generativelanguage.googleapis.com/v1beta/interactions");
    private readonly HttpClient _httpClient;
    private readonly GeminiClientOptions _options;

    public GeminiReferenceAnalysisClient(HttpClient httpClient, GeminiClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<GeminiReferenceAnalysisResult> AnalyzeAsync(
        GeminiReferenceAnalysisRequest request,
        IProgress<LocalAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.UploadConsent)
        {
            throw new InvalidOperationException("Visual reference analysis requires explicit upload consent for the selected samples.");
        }

        var inputs = new List<GeminiImageInput>(1 + request.SourceSamples.Count + request.ReferenceSamples.Count)
        {
            GeminiImageInput.CreateText("Return only JSON with exactly these numeric fields: exposureEV, contrast, saturation, temperature, tint. Compare the source images to the reference images and recommend a bounded editable color grade. Do not describe the images or return commands. Each value must be finite and within exposureEV -4..4 and all other fields -1..1.")
        };
        var total = request.SourceSamples.Count + request.ReferenceSamples.Count;
        var completed = 0;
        foreach (var sample in request.SourceSamples.Concat(request.ReferenceSamples))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var png = PpmPngEncoder.Encode(sample.Payload);
            if (png.Length > MaximumInlineBytes)
            {
                throw new GeminiReferenceAnalysisException("A reference sample exceeds the inline Gemini request limit.");
            }

            inputs.Add(GeminiImageInput.CreateImage(Convert.ToBase64String(png)));
            completed++;
            progress?.Report(new LocalAnalysisProgress("Preparing consented reference samples", completed / (double)total));
        }

        var promptBytes = inputs.Sum(input => input.Data?.Length ?? 0);
        if (promptBytes > MaximumInlineBytes)
        {
            throw new GeminiReferenceAnalysisException("The selected reference samples exceed the inline Gemini request limit.");
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, InteractionsEndpoint)
        {
            Content = JsonContent.Create(new GeminiInteractionRequest(
                _options.ModelId,
                inputs,
                new GeminiResponseFormat("text", "application/json")))
        };
        requestMessage.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        progress?.Report(new LocalAnalysisProgress("Uploading approved reference samples", 0));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);
        using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new GeminiReferenceAnalysisException(
                $"Gemini reference analysis failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                response.StatusCode);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(timeout.Token);
        var interaction = await JsonSerializer.DeserializeAsync<GeminiInteractionResponse>(responseStream, cancellationToken: timeout.Token)
            ?? throw new GeminiReferenceAnalysisException("Gemini returned an empty reference-analysis response.");
        if (!string.Equals(interaction.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new GeminiReferenceAnalysisException($"Gemini reference analysis did not complete (status: {interaction.Status ?? "unknown"}).");
        }

        var output = interaction.OutputText ?? interaction.Steps?
            .SelectMany(step => step.Content ?? [])
            .Where(content => string.Equals(content.Type, "text", StringComparison.OrdinalIgnoreCase))
            .Select(content => content.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new GeminiReferenceAnalysisException("Gemini returned no reference grade recommendation.");
        }

        progress?.Report(new LocalAnalysisProgress("Reference grade ready for review", 1));
        return new GeminiReferenceAnalysisResult(ParseGrade(output), output, 1);
    }

    private static ColorGrade ParseGrade(string output)
    {
        try
        {
            using var document = JsonDocument.Parse(output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new GeminiReferenceAnalysisException("Gemini reference grade must be a JSON object.");
            }

            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                "exposureEV", "contrast", "saturation", "temperature", "tint"
            };
            foreach (var property in root.EnumerateObject())
            {
                if (!allowed.Contains(property.Name))
                {
                    throw new GeminiReferenceAnalysisException($"Gemini returned unsupported reference grade field '{property.Name}'.");
                }
            }

            return new ColorGrade(
                ReadNumber(root, "exposureEV"),
                ReadNumber(root, "contrast"),
                ReadNumber(root, "saturation"),
                ReadNumber(root, "temperature"),
                ReadNumber(root, "tint"));
        }
        catch (JsonException exception)
        {
            throw new GeminiReferenceAnalysisException("Gemini returned malformed reference grade JSON.", innerException: exception);
        }
        catch (KeyNotFoundException exception)
        {
            throw new GeminiReferenceAnalysisException("Gemini reference grade omitted a required field.", innerException: exception);
        }
        catch (InvalidOperationException exception) when (exception is not GeminiReferenceAnalysisException)
        {
            throw new GeminiReferenceAnalysisException("Gemini reference grade contains a non-numeric field.", innerException: exception);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new GeminiReferenceAnalysisException("Gemini reference grade exceeds the supported bounds.", innerException: exception);
        }
    }

    private static float ReadNumber(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out var number))
        {
            throw new KeyNotFoundException(name);
        }

        return number;
    }

    private sealed record GeminiInteractionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<GeminiImageInput> Input,
        [property: JsonPropertyName("response_format")] GeminiResponseFormat ResponseFormat);

    private sealed record GeminiResponseFormat(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("mime_type")] string MimeType);

    private sealed record GeminiImageInput(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Text,
        [property: JsonPropertyName("data")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Data,
        [property: JsonPropertyName("mime_type")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MimeType)
    {
        public static GeminiImageInput CreateText(string text) => new("text", text, null, null);

        public static GeminiImageInput CreateImage(string data) => new("image", null, data, "image/png");
    }

    private sealed class GeminiInteractionResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("output_text")]
        public string? OutputText { get; set; }

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
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

internal static class PpmPngEncoder
{
    public static byte[] Encode(byte[] ppm)
    {
        ArgumentNullException.ThrowIfNull(ppm);
        var index = 0;
        var magic = ReadToken(ppm, ref index);
        if (!string.Equals(magic, "P6", StringComparison.Ordinal))
        {
            throw new GeminiReferenceAnalysisException("Only binary PPM reference samples are supported.");
        }

        if (!int.TryParse(ReadToken(ppm, ref index), out var width) || !int.TryParse(ReadToken(ppm, ref index), out var height) ||
            !int.TryParse(ReadToken(ppm, ref index), out var maximum) || width <= 0 || height <= 0 || maximum != 255)
        {
            throw new GeminiReferenceAnalysisException("The reference sample has an invalid PPM header.");
        }

        var pixelBytes = checked(width * height * 3);
        SkipHeaderDelimiter(ppm, ref index);
        if (ppm.Length - index != pixelBytes)
        {
            throw new GeminiReferenceAnalysisException("The reference sample pixel data is incomplete.");
        }

        using var raw = new MemoryStream(checked(height * (width * 3 + 1)));
        for (var row = 0; row < height; row++)
        {
            raw.WriteByte(0);
            raw.Write(ppm, index + row * width * 3, width * 3);
        }

        using var compressed = new MemoryStream();
        using (var deflate = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            raw.Position = 0;
            raw.CopyTo(deflate);
        }

        using var png = new MemoryStream();
        png.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        WriteChunk(png, "IHDR"u8.ToArray(),
        [
            (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
            (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
            8, 2, 0, 0, 0
        ]);
        WriteChunk(png, "IDAT"u8.ToArray(), compressed.ToArray());
        WriteChunk(png, "IEND"u8.ToArray(), []);
        return png.ToArray();
    }

    private static string ReadToken(byte[] bytes, ref int index)
    {
        SkipWhitespaceAndComments(bytes, ref index);
        var start = index;
        while (index < bytes.Length && bytes[index] > 32)
        {
            index++;
        }

        return Encoding.ASCII.GetString(bytes, start, index - start);
    }

    private static void SkipWhitespace(byte[] bytes, ref int index)
    {
        while (index < bytes.Length && bytes[index] <= 32)
        {
            index++;
        }
    }

    private static void SkipHeaderDelimiter(byte[] bytes, ref int index)
    {
        if (index >= bytes.Length || bytes[index] > 32)
        {
            throw new GeminiReferenceAnalysisException("The reference sample is missing its PPM header delimiter.");
        }

        if (bytes[index] == 13)
        {
            index++;
            if (index < bytes.Length && bytes[index] == 10)
            {
                index++;
            }
        }
        else
        {
            index++;
        }
    }

    private static void SkipWhitespaceAndComments(byte[] bytes, ref int index)
    {
        while (index < bytes.Length)
        {
            SkipWhitespace(bytes, ref index);
            if (index >= bytes.Length || bytes[index] != '#')
            {
                return;
            }

            while (index < bytes.Length && bytes[index] is not (10 or 13))
            {
                index++;
            }
        }
    }

    private static void WriteChunk(Stream output, byte[] type, byte[] data)
    {
        WriteUInt32(output, checked((uint)data.Length));
        output.Write(type);
        output.Write(data);
        WriteUInt32(output, Crc32(type, data));
    }

    private static void WriteUInt32(Stream output, uint value) => output.Write(
    [
        (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value
    ]);

    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xffffffffu;
        foreach (var value in type.Concat(data))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : (crc >> 1) ^ 0xedb88320u;
            }
        }

        return ~crc;
    }
}
