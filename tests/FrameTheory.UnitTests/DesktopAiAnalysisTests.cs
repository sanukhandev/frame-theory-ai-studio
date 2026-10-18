using System.Net;
using System.Text;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class DesktopAiAnalysisTests
{
    [Xunit.Fact]
    public async Task DesktopAnalysisUploadsApprovedSamplesAndLeavesProposalUnapplied()
    {
        var plan = """
            {"schemaVersion":"1.0","planId":"plan-1","projectId":"active-project","intent":"grade","actions":[{"id":"grade-1","type":"color.adjust","targetId":"active-project","startFrame":0,"durationFrames":1,"params":{"exposureEV":0.5,"contrast":0,"saturation":0,"temperature":0,"tint":0}}]}
            """;
        var body = string.Empty;
        using var client = new HttpClient(new StubHandler(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"id\":\"interactions/plan-1\",\"status\":\"completed\",\"output_text\":{System.Text.Json.JsonSerializer.Serialize(plan)},\"usage\":{{\"total_input_tokens\":12,\"total_output_tokens\":8,\"total_tokens\":20}}}}",
                    Encoding.UTF8,
                    "application/json")
            };
        }));
        var settings = new GeminiSettingsViewModel(new GeminiConfigurationService(new InMemorySecretStore()));
        Xunit.Assert.True(Xunit.Assert.IsAssignableFrom<IList<string>>(settings.Models).IsReadOnly);
        await settings.SaveAsync("test-secret", CancellationToken.None);
        var viewModel = new MainWindowViewModel(
            new WorkspaceCatalog(),
            new TestCapabilityDetector(),
            new ImageImporter(),
            geminiSettings: settings,
            analysisSampler: new SampleSampler(),
            transcriptionHttpClient: client);

        await viewModel.ImportFilesAsync(["photo.png"], CancellationToken.None);
        Xunit.Assert.True(await viewModel.PrepareLocalAnalysisAsync(GeminiAnalysisScope.PhotoPreview, true, CancellationToken.None));
        Xunit.Assert.True(await viewModel.RequestAiAnalysisAsync(CancellationToken.None));

        Xunit.Assert.Single(viewModel.AiProposal.Actions);
        Xunit.Assert.NotNull(viewModel.AiProposal.ValidatedJson);
        Xunit.Assert.Empty(viewModel.TimelineEditor.Captions);
        Xunit.Assert.Contains("\"type\":\"image\"", body);
        Xunit.Assert.Contains("review 1 editable action", viewModel.AnalysisStatusText, StringComparison.Ordinal);
        Xunit.Assert.Contains("input=12, output=8, total=20", viewModel.GeminiUsageStatusText, StringComparison.Ordinal);
        Xunit.Assert.Contains("approved samples=1", viewModel.GeminiUsageStatusText, StringComparison.Ordinal);
        Xunit.Assert.Contains("plan succeeded", viewModel.GeminiActivityLogText, StringComparison.Ordinal);
        Xunit.Assert.Contains("inline_images=1", viewModel.GeminiActivityLogText, StringComparison.Ordinal);
    }

    private sealed class ImageImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            var metadata = new MediaSourceMetadata(
                MediaId.Create(file, "desktop-ai-analysis"),
                file,
                MediaKind.Image,
                1,
                1,
                null,
                0,
                TimeSpan.Zero);
            return Task.FromResult(new ImportedMedia(metadata, Sample().Payload, []));
        }
    }

    private sealed class SampleSampler : ILocalAnalysisSampler
    {
        public Task<LocalMediaAnalysisReport> PrepareAsync(
            GeminiAnalysisRequest request,
            IProgress<LocalAnalysisProgress>? progress,
            CancellationToken cancellationToken) => Task.FromResult(
                new LocalMediaAnalysisReport([Sample()], request.Scopes, request.UploadConsent, Sample().Payload.Length, "approved"));
    }

    private static LocalAnalysisSample Sample() => new(
        new Uri("file:///sample.png"),
        MediaKind.Image,
        "photo-preview-ppm",
        0,
        TimeSpan.Zero,
        [.. "P6\n1 1\n255\n"u8, 255, 0, 0]);

    private sealed class InMemorySecretStore : ISecretStore
    {
        private string? _secret;

        public Task StoreAsync(string name, string secret, CancellationToken cancellationToken = default)
        {
            _secret = secret;
            return Task.CompletedTask;
        }

        public Task<string?> ReadAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(_secret);

        public Task DeleteAsync(string name, CancellationToken cancellationToken = default)
        {
            _secret = null;
            return Task.CompletedTask;
        }
    }

    private sealed class TestCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
