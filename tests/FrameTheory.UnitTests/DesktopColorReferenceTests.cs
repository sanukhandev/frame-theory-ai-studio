using System.Net;
using System.Text;
using System.Text.Json;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Desktop;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.UnitTests;

public sealed class DesktopColorReferenceTests
{
    [Xunit.Fact]
    public async Task ReferenceMatchRequiresConsentAndAppliesReviewedGrade()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-color-reference-");
        try
        {
            using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        id = "interactions/reference-1",
                        status = "completed",
                        output_text = "{\"exposureEV\":0.5,\"contrast\":0.2,\"saturation\":0.1,\"temperature\":0.3,\"tint\":-0.1}",
                        usage = new { total_input_tokens = 12, total_output_tokens = 8, total_tokens = 20 }
                    }),
                    Encoding.UTF8,
                    "application/json")
            }));
            var settings = new GeminiSettingsViewModel(new GeminiConfigurationService(new InMemorySecretStore()));
            await settings.SaveAsync("reference-secret", CancellationToken.None);
            var historyPath = Path.Combine(directory.FullName, "usage.jsonl");
            var viewModel = new MainWindowViewModel(
                new WorkspaceCatalog(),
                new TestCapabilityDetector(),
                new ImageImporter(),
                geminiSettings: settings,
                transcriptionHttpClient: client,
                geminiInteractionHistory: new FileGeminiInteractionHistory(historyPath));

            await viewModel.ImportFilesAsync(["source.png", "reference.png"], CancellationToken.None);
            Xunit.Assert.Equal(2, viewModel.ImportedMedia.Count);
            Xunit.Assert.False(await viewModel.AnalyzeColorReferenceAsync(
                viewModel.ImportedMedia[0],
                viewModel.ImportedMedia[1],
                uploadConsent: false,
                CancellationToken.None));
            Xunit.Assert.Contains("approve", viewModel.ReferenceAnalysisStatusText, StringComparison.OrdinalIgnoreCase);

            Xunit.Assert.True(await viewModel.AnalyzeColorReferenceAsync(
                viewModel.ImportedMedia[0],
                viewModel.ImportedMedia[1],
                uploadConsent: true,
                CancellationToken.None));
            Xunit.Assert.Contains("ready for review", viewModel.ReferenceAnalysisStatusText, StringComparison.OrdinalIgnoreCase);
            Xunit.Assert.Contains("input=12, output=8, total=20", viewModel.GeminiUsageStatusText, StringComparison.Ordinal);
            Xunit.Assert.Equal(0, viewModel.ColorInspector.Grade.ExposureEV);

            Xunit.Assert.True(viewModel.ApplyPendingReferenceGrade());
            Xunit.Assert.Equal(0.5f, viewModel.ColorInspector.Grade.ExposureEV);
            Xunit.Assert.Contains("Applied", viewModel.ReferenceAnalysisStatusText, StringComparison.Ordinal);
            Xunit.Assert.DoesNotContain("reference-secret", await File.ReadAllTextAsync(historyPath));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private sealed class ImageImporter : IMediaImporter
    {
        public Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
        {
            var metadata = new MediaSourceMetadata(
                MediaId.Create(file, "desktop-color-reference"),
                file,
                MediaKind.Image,
                1,
                1,
                null,
                0,
                TimeSpan.Zero);
            return Task.FromResult(new ImportedMedia(metadata, Sample(), []));
        }
    }

    private static byte[] Sample() => [.. "P6\n1 1\n255\n"u8, 255, 0, 0];

    private sealed class InMemorySecretStore : ISecretStore
    {
        private string? _secret;

        public Task StoreAsync(string name, string secret, CancellationToken cancellationToken)
        {
            _secret = secret;
            return Task.CompletedTask;
        }

        public Task<string?> ReadAsync(string name, CancellationToken cancellationToken) => Task.FromResult(_secret);

        public Task DeleteAsync(string name, CancellationToken cancellationToken)
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
