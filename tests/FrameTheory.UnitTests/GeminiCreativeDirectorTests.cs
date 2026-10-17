using System.Net;
using System.Text;
using System.Text.Json;
using FrameTheory.AI;
using FrameTheory.Domain;

namespace FrameTheory.UnitTests;

public sealed class GeminiCreativeDirectorTests
{
    [Xunit.Fact]
    public async Task CreativeDirectorSendsApprovedVisualsAndSchemaValidatedPlanRequest()
    {
        string? body = null;
        var plan = """
            {"schemaVersion":"1.0","planId":"plan-1","projectId":"active-project","intent":"grade","actions":[{"id":"grade-1","type":"color.adjust","targetId":"active-project","startFrame":0,"durationFrames":1,"params":{"exposureEV":0.5,"contrast":0,"saturation":0,"temperature":0,"tint":0}}]}
            """;
        using var client = new HttpClient(new StubHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"id\":\"interactions/plan-1\",\"status\":\"completed\",\"output_text\":{JsonSerializer.Serialize(plan)}}}",
                    Encoding.UTF8,
                    "application/json")
            };
        }));

        var report = new LocalMediaAnalysisReport(
            [Sample()],
            GeminiAnalysisScope.PhotoPreview,
            UploadConsent: true,
            EstimatedUploadBytes: 15,
            "approved");
        var result = await new GeminiCreativeDirectorClient(
            new GeminiInteractionsClient(
                client,
                new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero)))
            .CreatePlanAsync(report, new HashSet<string>(StringComparer.Ordinal) { "active-project" }, 1, CancellationToken.None);

        Xunit.Assert.Equal("interactions/plan-1", result.InteractionId);
        Xunit.Assert.Equal(1, result.UploadedSampleCount);
        Xunit.Assert.Contains("\"type\":\"image\"", body);
        Xunit.Assert.Contains("\"response_format\"", body);
        Xunit.Assert.Contains("\"schema\"", body);
        Xunit.Assert.DoesNotContain("test-secret", body);
        using var document = JsonDocument.Parse(body!);
        var encoded = document.RootElement.GetProperty("input")[1].GetProperty("data").GetString();
        Xunit.Assert.StartsWith("iVBORw0KGgo", encoded);
    }

    [Xunit.Fact]
    public async Task CreativeDirectorRejectsUnconsentedOrUnknownReportsBeforeNetworkAccess()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var creativeDirector = new GeminiCreativeDirectorClient(
            new GeminiInteractionsClient(
                client,
                new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero)));

        await Xunit.Assert.ThrowsAsync<InvalidOperationException>(() => creativeDirector.CreatePlanAsync(
            new LocalMediaAnalysisReport([Sample()], GeminiAnalysisScope.PhotoPreview, false, 1, "not approved"),
            new HashSet<string>(StringComparer.Ordinal) { "active-project" },
            1,
            CancellationToken.None));
        await Xunit.Assert.ThrowsAsync<InvalidOperationException>(() => creativeDirector.CreatePlanAsync(
            new LocalMediaAnalysisReport([new LocalAnalysisSample(new Uri("file:///unknown.bin"), MediaKind.Audio, "unknown", null, null, [1])], GeminiAnalysisScope.AudioWaveform, true, 1, "approved"),
            new HashSet<string>(StringComparer.Ordinal) { "active-project" },
            1,
            CancellationToken.None));

        Xunit.Assert.Equal(0, calls);
    }

    [Xunit.Fact]
    public async Task LargeVisualSampleUsesFilesApiAndCleansUpBeforeReturning()
    {
        var requests = new List<HttpRequestMessage>();
        string? interactionBody = null;
        var plan = "{\"schemaVersion\":\"1.0\",\"planId\":\"plan-large\",\"projectId\":\"active-project\",\"intent\":\"review\",\"actions\":[]}";
        using var client = new HttpClient(new StubHandler(request =>
        {
            requests.Add(request);
            if (request.Method == HttpMethod.Post && request.Headers.TryGetValues("X-Goog-Upload-Command", out var uploadValues))
            {
                var uploadCommand = uploadValues.Single();
                if (string.Equals(uploadCommand, "start", StringComparison.Ordinal))
                {
                    var startResponse = new HttpResponseMessage(HttpStatusCode.OK);
                    startResponse.Headers.TryAddWithoutValidation("X-Goog-Upload-URL", "https://generativelanguage.googleapis.com/upload/v1beta/files/creative-1");
                    return startResponse;
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"file\":{\"name\":\"files/creative-1\",\"uri\":\"https://generativelanguage.googleapis.com/v1beta/files/creative-1\",\"mime_type\":\"image/png\"}}", Encoding.UTF8, "application/json")
                };
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
                    Content = new StringContent($"{{\"id\":\"interactions/plan-large\",\"status\":\"completed\",\"output_text\":{JsonSerializer.Serialize(plan)}}}", Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var report = new LocalMediaAnalysisReport(
            [LargeSample(), Sample()],
            GeminiAnalysisScope.PhotoPreview,
            UploadConsent: true,
            EstimatedUploadBytes: 1,
            "approved");
        var result = await new GeminiCreativeDirectorClient(
            new GeminiInteractionsClient(
                client,
                new GeminiClientOptions("test-secret", "gemini-test", TimeSpan.FromSeconds(30), 1, TimeSpan.Zero)))
            .CreatePlanAsync(report, new HashSet<string>(StringComparer.Ordinal) { "active-project" }, 1, CancellationToken.None);

        Xunit.Assert.Equal("interactions/plan-large", result.InteractionId);
        Xunit.Assert.Equal(2, result.UploadedSampleCount);
        Xunit.Assert.True(result.UploadedBytes > 20 * 1024 * 1024);
        Xunit.Assert.Contains("\"uri\":\"https://generativelanguage.googleapis.com/v1beta/files/creative-1\"", interactionBody);
        Xunit.Assert.Contains(requests, request => request.Method == HttpMethod.Delete);
    }

    private static LocalAnalysisSample Sample() => new(
        new Uri("file:///sample.png"),
        MediaKind.Image,
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
        var random = new Random(31);
        random.NextBytes(payload.AsSpan(header.Length));
        return new LocalAnalysisSample(
            new Uri("file:///large-creative-direction.png"),
            MediaKind.Image,
            "photo-preview-ppm",
            0,
            TimeSpan.Zero,
            payload);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
