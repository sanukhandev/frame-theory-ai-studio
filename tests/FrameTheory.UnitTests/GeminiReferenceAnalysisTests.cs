using System.Net;
using System.Text;
using System.Text.Json;
using FrameTheory.AI;

namespace FrameTheory.UnitTests;

public sealed class GeminiReferenceAnalysisTests
{
    [Xunit.Fact]
    public async Task ReferenceAnalysisRetriesRateLimitAndRecordsUsage()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-reference-retry-");
        try
        {
            var calls = 0;
            var historyPath = Path.Combine(directory.FullName, "usage.jsonl");
            using var client = new HttpClient(new StubHandler(_ =>
            {
                calls++;
                if (calls == 1)
                {
                    return new HttpResponseMessage((HttpStatusCode)429)
                    {
                        Headers = { RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero) }
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"interactions/reference-1\",\"status\":\"completed\",\"output_text\":\"{\\\"exposureEV\\\":0,\\\"contrast\\\":0,\\\"saturation\\\":0,\\\"temperature\\\":0,\\\"tint\\\":0}\",\"usage\":{\"total_input_tokens\":4,\"total_output_tokens\":3,\"total_tokens\":7}}", Encoding.UTF8, "application/json")
                };
            }));

            var result = await new GeminiReferenceAnalysisClient(
                client,
                new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(5), 2, TimeSpan.Zero),
                new FileGeminiInteractionHistory(historyPath))
                .AnalyzeAsync(new GeminiReferenceAnalysisRequest([Sample()], [Sample()], uploadConsent: true), null, CancellationToken.None);

            Xunit.Assert.Equal(2, calls);
            Xunit.Assert.Equal(2, result.Attempts);
            Xunit.Assert.Equal(7, result.Usage?.TotalTokens);
            var history = await File.ReadAllTextAsync(historyPath);
            Xunit.Assert.Contains("Succeeded", history);
            Xunit.Assert.Contains("TotalTokens", history);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task ReferenceAnalysisUploadsOnlyWithConsentAndReturnsEditableGrade()
    {
        var requests = new List<HttpRequestMessage>();
        string? capturedBody = null;
        using var client = new HttpClient(new StubHandler(request =>
        {
            requests.Add(request);
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"completed\",\"output_text\":\"{\\\"exposureEV\\\":0.5,\\\"contrast\\\":0.2,\\\"saturation\\\":0.1,\\\"temperature\\\":0.3,\\\"tint\\\":-0.1}\"}", Encoding.UTF8, "application/json")
            };
        }));

        var result = await new GeminiReferenceAnalysisClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero))
            .AnalyzeAsync(new GeminiReferenceAnalysisRequest([Sample()], [Sample()], uploadConsent: true), null, CancellationToken.None);

        Xunit.Assert.Equal(0.5f, result.Grade.ExposureEV);
        Xunit.Assert.Equal(0.3f, result.Grade.Temperature);
        Xunit.Assert.Single(requests);
        Xunit.Assert.Contains("\"type\":\"image\"", capturedBody);
        Xunit.Assert.Contains("\"mime_type\":\"image/png\"", capturedBody);
        Xunit.Assert.DoesNotContain("test-secret", capturedBody);
        using var document = JsonDocument.Parse(capturedBody!);
        var encoded = document.RootElement.GetProperty("input")[1].GetProperty("data").GetString();
        Xunit.Assert.StartsWith("iVBORw0KGgo", encoded);
    }

    [Xunit.Fact]
    public async Task ReferenceAnalysisRejectsMissingConsentAndUnsafeProviderGrade()
    {
        var request = new GeminiReferenceAnalysisRequest([Sample()], [Sample()], uploadConsent: false);
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":\"completed\",\"output_text\":\"{\\\"exposureEV\\\":99,\\\"contrast\\\":0,\\\"saturation\\\":0,\\\"temperature\\\":0,\\\"tint\\\":0}\"}", Encoding.UTF8, "application/json")
        }));

        await Xunit.Assert.ThrowsAsync<InvalidOperationException>(() => new GeminiReferenceAnalysisClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero))
            .AnalyzeAsync(request, null, CancellationToken.None));

        await Xunit.Assert.ThrowsAsync<GeminiReferenceAnalysisException>(() => new GeminiReferenceAnalysisClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero))
            .AnalyzeAsync(new GeminiReferenceAnalysisRequest([Sample()], [Sample()], uploadConsent: true), null, CancellationToken.None));
    }

    [Xunit.Fact]
    public async Task LargeReferenceSampleUsesFilesApiAndCleansUpBeforeReturning()
    {
        var requests = new List<HttpRequestMessage>();
        string? interactionBody = null;
        using var client = new HttpClient(new StubHandler(request =>
        {
            requests.Add(request);
            if (request.Method == HttpMethod.Post && request.Headers.TryGetValues("X-Goog-Upload-Command", out var uploadValues))
            {
                var uploadCommand = uploadValues.Single();
                if (string.Equals(uploadCommand, "start", StringComparison.Ordinal))
                {
                    var startResponse = new HttpResponseMessage(HttpStatusCode.OK);
                    startResponse.Headers.TryAddWithoutValidation("X-Goog-Upload-URL", "https://generativelanguage.googleapis.com/upload/v1beta/files/reference-1");
                    return startResponse;
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"file\":{\"name\":\"files/reference-1\",\"uri\":\"https://generativelanguage.googleapis.com/v1beta/files/reference-1\",\"mime_type\":\"image/png\"}}", Encoding.UTF8, "application/json")
                };
                return response;
            }

            if (request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.EndsWith("/interactions", StringComparison.Ordinal) == true)
            {
                interactionBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"completed\",\"output_text\":\"{\\\"exposureEV\\\":0,\\\"contrast\\\":0,\\\"saturation\\\":0,\\\"temperature\\\":0,\\\"tint\\\":0}\"}", Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var result = await new GeminiReferenceAnalysisClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(30), 1, TimeSpan.Zero))
            .AnalyzeAsync(new GeminiReferenceAnalysisRequest([LargeSample()], [Sample()], uploadConsent: true), null, CancellationToken.None);

        Xunit.Assert.Equal(0, result.Grade.ExposureEV);
        Xunit.Assert.Equal(4, requests.Count);
        Xunit.Assert.Contains("\"uri\":\"https://generativelanguage.googleapis.com/v1beta/files/reference-1\"", interactionBody);
        Xunit.Assert.DoesNotContain("test-secret", interactionBody);
        Xunit.Assert.Contains(requests, request => request.Method == HttpMethod.Delete);
    }

    private static LocalAnalysisSample Sample() => new(
        new Uri("file:///sample.png"),
        FrameTheory.Domain.MediaKind.Image,
        "photo-preview-ppm",
        0,
        TimeSpan.Zero,
        [.. "P6\n1 1\n255\n"u8, 255, 0, 0]);

    private static LocalAnalysisSample LargeSample()
    {
        const int width = 3000;
        const int height = 2500;
        var header = Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        var payload = new byte[checked(header.Length + width * height * 3)];
        header.CopyTo(payload, 0);
        var random = new Random(17);
        random.NextBytes(payload.AsSpan(header.Length));
        return new LocalAnalysisSample(
            new Uri("file:///large-reference.png"),
            FrameTheory.Domain.MediaKind.Image,
            "photo-preview-ppm",
            0,
            TimeSpan.Zero,
            payload);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responder(request));
        }
    }
}
