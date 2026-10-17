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
    private const long MaximumFilesApiBytes = 2L * 1024 * 1024 * 1024;
    private static readonly Uri InteractionsEndpoint = new("https://generativelanguage.googleapis.com/v1beta/interactions");
    private static readonly Uri FilesUploadEndpoint = new("https://generativelanguage.googleapis.com/upload/v1beta/files");
    private static readonly Uri FilesApiEndpoint = new("https://generativelanguage.googleapis.com/v1beta/");
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
        var inlineBytes = 0L;
        var uploadedFiles = new List<GeminiUploadedFile>();
        try
        {
            foreach (var sample in request.SourceSamples.Concat(request.ReferenceSamples))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var png = PpmPngEncoder.Encode(sample.Payload);
                if (png.LongLength > MaximumFilesApiBytes)
                {
                    throw new GeminiReferenceAnalysisException("A reference sample exceeds the Gemini Files API limit.");
                }

                if (inlineBytes <= MaximumInlineBytes - png.LongLength)
                {
                    inputs.Add(GeminiImageInput.CreateImage(Convert.ToBase64String(png)));
                    inlineBytes += png.LongLength;
                }
                else
                {
                    progress?.Report(new LocalAnalysisProgress("Uploading approved reference sample", 0));
                    var uploadedFile = await UploadFileAsync(
                        png,
                        "image/png",
                        $"frame-theory-reference-{completed + 1}.png",
                        cancellationToken);
                    uploadedFiles.Add(uploadedFile);
                    inputs.Add(GeminiImageInput.CreateImageUri(uploadedFile.Uri, uploadedFile.MimeType));
                }

                completed++;
                progress?.Report(new LocalAnalysisProgress("Preparing consented reference samples", completed / (double)total));
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
        finally
        {
            foreach (var uploadedFile in uploadedFiles)
            {
                using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await DeleteFileAsync(uploadedFile.Name, cleanupTimeout.Token);
            }
        }
    }

    private async Task<GeminiUploadedFile> UploadFileAsync(
        byte[] payload,
        string contentType,
        string displayName,
        CancellationToken cancellationToken)
    {
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, FilesUploadEndpoint)
        {
            Content = JsonContent.Create(new GeminiFileUploadStartRequest(
                new GeminiFileMetadata(displayName)))
        };
        startRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Protocol", "resumable");
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Command", "start");
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Length", payload.LongLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Header-Content-Type", contentType);

        using var startResponse = await _httpClient.SendAsync(startRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!startResponse.IsSuccessStatusCode)
        {
            throw new GeminiReferenceAnalysisException(
                $"Gemini Files API upload initialization failed with HTTP {(int)startResponse.StatusCode} ({startResponse.StatusCode}).",
                startResponse.StatusCode);
        }

        var uploadUrl = GetHeader(startResponse, "x-goog-upload-url");
        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri) ||
            uploadUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uploadUri.Host, FilesUploadEndpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new GeminiReferenceAnalysisException("Gemini returned an invalid Files API upload URL.");
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
            throw new GeminiReferenceAnalysisException(
                $"Gemini Files API upload failed with HTTP {(int)uploadResponse.StatusCode} ({uploadResponse.StatusCode}).",
                uploadResponse.StatusCode);
        }

        var upload = await uploadResponse.Content.ReadFromJsonAsync<GeminiFileUploadResponse>(cancellationToken: cancellationToken)
            ?? throw new GeminiReferenceAnalysisException("Gemini Files API returned an empty upload response.");
        var file = upload.File;
        if (file is null || string.IsNullOrWhiteSpace(file.Name) || string.IsNullOrWhiteSpace(file.Uri))
        {
            throw new GeminiReferenceAnalysisException("Gemini Files API returned an incomplete upload response.");
        }

        if (!Uri.TryCreate(file.Uri, UriKind.Absolute, out var fileUri) ||
            fileUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(fileUri.Host, FilesUploadEndpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            await DeleteFileAsync(file.Name, cancellationToken);
            throw new GeminiReferenceAnalysisException("Gemini returned an invalid Files API file URI.");
        }

        var mimeType = string.IsNullOrWhiteSpace(file.MimeType) ? contentType : file.MimeType;
        return new GeminiUploadedFile(file.Name, fileUri.AbsoluteUri, mimeType);
    }

    private async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken)
    {
        if (!fileName.StartsWith("files/", StringComparison.Ordinal) || fileName.Contains("..", StringComparison.Ordinal) || fileName.Contains('?') || fileName.Contains('#'))
        {
            throw new GeminiReferenceAnalysisException("Gemini returned an invalid uploaded file name; cleanup was refused.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(FilesApiEndpoint, fileName));
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GeminiReferenceAnalysisException(
                $"Gemini temporary reference cleanup failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
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
        [property: JsonPropertyName("uri")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Uri,
        [property: JsonPropertyName("mime_type")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MimeType)
    {
        public static GeminiImageInput CreateText(string text) => new("text", text, null, null, null);

        public static GeminiImageInput CreateImage(string data) => new("image", null, data, null, "image/png");

        public static GeminiImageInput CreateImageUri(string uri, string mimeType) => new("image", null, null, uri, mimeType);
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

        [JsonPropertyName("mime_type")]
        public string? MimeType { get; set; }
    }

    private sealed record GeminiUploadedFile(string Name, string Uri, string MimeType);

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
