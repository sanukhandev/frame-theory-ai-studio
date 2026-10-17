using System.Net;
using System.Text;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Desktop;

namespace FrameTheory.UnitTests;

public sealed class DesktopTranscriptionTests
{
    [Xunit.Fact]
    public async Task DesktopTranscriptionUsesStoredKeyAndConsentToCreateVerifiedCaptions()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-transcription-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3, 4]);
            var secretStore = new InMemorySecretStore();
            var settings = new GeminiSettingsViewModel(new GeminiConfigurationService(secretStore));
            await settings.SaveAsync("desktop-secret", CancellationToken.None);
            string? requestBody = null;
            string? requestKey = null;
            using var client = new HttpClient(new StubHandler(request =>
            {
                requestKey = request.Headers.GetValues("x-goog-api-key").Single();
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"status\":\"completed\",\"steps\":[{\"content\":[{\"annotations\":[{\"type\":\"word_info\",\"text\":\"hello\",\"start_offset\":\"0.100s\",\"end_offset\":\"0.400s\"}]}]}]}",
                        Encoding.UTF8,
                        "application/json")
                };
            }));
            var viewModel = new MainWindowViewModel(
                new WorkspaceCatalog(),
                new TestCapabilityDetector(),
                geminiSettings: settings,
                transcriptionHttpClient: client);

            var succeeded = await viewModel.TranscribeAudioAsync(sourcePath, uploadConsent: true, CancellationToken.None);

            Xunit.Assert.True(succeeded);
            Xunit.Assert.Single(viewModel.TimelineEditor.Captions);
            Xunit.Assert.Equal("hello", viewModel.TimelineEditor.Captions[0].Text);
            Xunit.Assert.Equal("sha256:9f64a747e1b97f131fabb6b447296c9b6f0201e79fb3c5356e6c77e89b6a806a", viewModel.TimelineEditor.TranscriptSourceFingerprint);
            Xunit.Assert.Equal("desktop-secret", requestKey);
            Xunit.Assert.DoesNotContain("desktop-secret", requestBody);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DesktopTranscriptionRejectsMissingConsentBeforeReadingKey()
    {
        var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new TestCapabilityDetector());

        var succeeded = await viewModel.TranscribeAudioAsync("does-not-exist.wav", uploadConsent: false, CancellationToken.None);

        Xunit.Assert.False(succeeded);
        Xunit.Assert.Contains("approve uploading", viewModel.CaptionStatusText, StringComparison.Ordinal);
    }

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

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class TestCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }
}
