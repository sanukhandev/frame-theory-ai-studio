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

    private static LocalAnalysisSample Sample() => new(
        new Uri("file:///sample.png"),
        MediaKind.Image,
        "photo-preview-ppm",
        0,
        TimeSpan.Zero,
        [.. "P6\n1 1\n255\n"u8, 255, 0, 0]);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
