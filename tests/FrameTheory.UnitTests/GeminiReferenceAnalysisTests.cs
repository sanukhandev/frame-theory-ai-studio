using System.Net;
using System.Text;
using System.Text.Json;
using FrameTheory.AI;

namespace FrameTheory.UnitTests;

public sealed class GeminiReferenceAnalysisTests
{
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

    private static LocalAnalysisSample Sample() => new(
        new Uri("file:///sample.png"),
        FrameTheory.Domain.MediaKind.Image,
        "photo-preview-ppm",
        0,
        TimeSpan.Zero,
        [.. "P6\n1 1\n255\n"u8, 255, 0, 0]);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responder(request));
        }
    }
}
