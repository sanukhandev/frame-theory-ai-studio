using System.Net;
using System.Text;
using System.Text.Json;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Desktop;

namespace FrameTheory.UnitTests;

public sealed class DesktopAiRevisionTests
{
    [Xunit.Fact]
    public async Task DesktopRevisionValidatesResponseWithoutMutatingTimelineAndChainsInteraction()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-ai-revision-");
        try
        {
            var plan = """
                {"schemaVersion":"1.0","planId":"revision-1","projectId":"active-project","intent":"caption","actions":[{"id":"caption-1","type":"caption.add","targetId":"caption-track","startFrame":0,"durationFrames":1,"params":{"text":"Hello","provenance":"authored-caption"}}]}
                """;
            var secretStore = new InMemorySecretStore();
            var settings = new GeminiSettingsViewModel(new GeminiConfigurationService(secretStore));
            await settings.SaveAsync("revision-secret", CancellationToken.None);
            var requestBodies = new List<string>();
            var secondRequest = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callCount = 0;
            using var client = new HttpClient(new StubHandler(request =>
            {
                var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                requestBodies.Add(body);
                if (++callCount == 2)
                {
                    secondRequest.SetResult(body);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $"{{\"id\":\"interactions/{callCount}\",\"status\":\"completed\",\"output_text\":{JsonSerializer.Serialize(plan)}}}",
                        Encoding.UTF8,
                        "application/json")
                };
            }));
            var historyPath = Path.Combine(directory.FullName, "usage.jsonl");
            var viewModel = new MainWindowViewModel(
                new WorkspaceCatalog(),
                new TestCapabilityDetector(),
                geminiSettings: settings,
                transcriptionHttpClient: client,
                geminiInteractionHistory: new FileGeminiInteractionHistory(historyPath));

            var first = await viewModel.RequestAiRevisionAsync("add a caption", string.Empty, CancellationToken.None);
            var second = await viewModel.RequestAiRevisionAsync("keep the caption", viewModel.AiProposal.ValidatedJson!, CancellationToken.None);

            Xunit.Assert.True(first);
            Xunit.Assert.True(second);
            Xunit.Assert.Equal(2, requestBodies.Count);
            Xunit.Assert.Contains("previous_interaction_id", await secondRequest.Task, StringComparison.Ordinal);
            Xunit.Assert.Contains("interactions/1", await secondRequest.Task, StringComparison.Ordinal);
            Xunit.Assert.Single(viewModel.AiProposal.Actions);
            Xunit.Assert.Empty(viewModel.TimelineEditor.Captions);
            Xunit.Assert.Equal(plan, viewModel.AiProposal.ValidatedJson);
            Xunit.Assert.DoesNotContain("revision-secret", requestBodies[0]);
            var history = await File.ReadAllTextAsync(historyPath);
            Xunit.Assert.DoesNotContain("add a caption", history);
            Xunit.Assert.Contains("PromptCharacterCount", history);
        }
        finally
        {
            directory.Delete(true);
        }
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
