using System.Net;
using System.Text;
using System.Text.Json;
using FrameTheory.AI;
using FrameTheory.Application;

namespace FrameTheory.UnitTests;

public sealed class GeminiInteractionsTests
{
    [Xunit.Fact]
    public void InteractionInputFactoriesRejectMissingPayloads()
    {
        Xunit.Assert.Throws<ArgumentNullException>(() => GeminiInteractionInput.CreateText(null!));
        Xunit.Assert.Throws<ArgumentException>(() => GeminiInteractionInput.CreateText(" "));
        Xunit.Assert.Throws<ArgumentNullException>(() => GeminiInteractionInput.CreateImage(null!));
        Xunit.Assert.Throws<ArgumentException>(() => GeminiInteractionInput.CreateImage([]));
        Xunit.Assert.Throws<ArgumentException>(() => GeminiInteractionInput.CreateImageUri(" ", "image/png"));
        Xunit.Assert.Throws<ArgumentException>(() => GeminiInteractionInput.CreateImageUri("https://example.test/image.png", " "));
    }

    [Xunit.Fact]
    public void ClientOptionsUseSafeDefaultsWhenOptionalValuesAreOmitted()
    {
        var options = new GeminiClientOptions("test-secret", "gemini-test");

        Xunit.Assert.Equal(TimeSpan.FromMinutes(2), options.Timeout);
        Xunit.Assert.Equal(3, options.MaximumAttempts);
        Xunit.Assert.Equal(TimeSpan.FromSeconds(1), options.RetryDelay);
    }

    [Xunit.Fact]
    public void RetryDelayUsesBoundedExponentialBackoffAndServerHint()
    {
        Xunit.Assert.Equal(
            TimeSpan.FromMilliseconds(25),
            GeminiInteractionsClient.CalculateRetryDelay(TimeSpan.FromMilliseconds(25), 1, null));
        Xunit.Assert.Equal(
            TimeSpan.FromMilliseconds(50),
            GeminiInteractionsClient.CalculateRetryDelay(TimeSpan.FromMilliseconds(25), 2, null));
        Xunit.Assert.Equal(
            TimeSpan.FromSeconds(30),
            GeminiInteractionsClient.CalculateRetryDelay(TimeSpan.FromSeconds(30), 3, null));
        Xunit.Assert.Equal(
            TimeSpan.FromSeconds(30),
            GeminiInteractionsClient.CalculateRetryDelay(TimeSpan.FromMilliseconds(25), 1, TimeSpan.FromMinutes(1)));
    }

    [Xunit.Fact]
    public void FileHistoryReadKeepsOnlyRequestedTail()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-history-tail-");
        try
        {
            var path = Path.Combine(directory.FullName, "usage.jsonl");
            var history = new FileGeminiInteractionHistory(path);
            for (var index = 0; index < 4; index++)
            {
                history.Record(new GeminiInteractionRecord(
                    DateTimeOffset.UtcNow.AddSeconds(index),
                    $"model-{index}",
                    null,
                    true,
                    null,
                    null,
                    1,
                    index,
                    null));
            }

            var recent = history.ReadRecent(2);

            Xunit.Assert.Equal(["model-2", "model-3"], recent.Select(record => record.ModelId));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void FileHistoryBoundsReadRequests()
    {
        var history = new FileGeminiInteractionHistory(Path.Combine(Path.GetTempPath(), "frame-theory-history-bounds.jsonl"));

        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => history.ReadRecent(0));
        Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => history.ReadRecent(1001));
    }

    [Xunit.Fact]
    public void FileHistoryTrimsOversizedLogsToRecentRecords()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-history-limit-");
        try
        {
            var path = Path.Combine(directory.FullName, "usage.jsonl");
            var history = new FileGeminiInteractionHistory(path);
            for (var index = 0; index < 220; index++)
            {
                history.Record(new GeminiInteractionRecord(
                    DateTimeOffset.UtcNow.AddSeconds(index),
                    $"model-{index}",
                    null,
                    true,
                    null,
                    null,
                    1,
                    1,
                    null,
                    ScopeSummary: new string('x', 6000)));
            }

            Xunit.Assert.True(new FileInfo(path).Length <= 1024 * 1024);
            Xunit.Assert.Equal("model-219", history.ReadRecent(1).Single().ModelId);
            Xunit.Assert.True(history.ReadRecent(500).Count < 220);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void FileHistoryDoesNotFollowReparsePointLogPath()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-history-boundary-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-gemini-history-external-");
        try
        {
            var externalPath = Path.Combine(externalDirectory.FullName, "usage.jsonl");
            File.WriteAllText(externalPath, "original");
            var linkedPath = Path.Combine(directory.FullName, "usage.jsonl");
            File.CreateSymbolicLink(linkedPath, externalPath);
            var history = new FileGeminiInteractionHistory(linkedPath);

            history.Record(new GeminiInteractionRecord(
                DateTimeOffset.UtcNow,
                "gemini-2.5-flash",
                null,
                true,
                null,
                null,
                1,
                1,
                null));

            Xunit.Assert.Equal("original", File.ReadAllText(externalPath));
            Xunit.Assert.Empty(history.ReadRecent());
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
            externalDirectory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void FileHistoryDoesNotFollowReparsePointLogParent()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-history-parent-");
        var externalDirectory = Directory.CreateTempSubdirectory("frame-theory-gemini-history-parent-external-");
        try
        {
            var linkedDirectory = Path.Combine(directory.FullName, "linked");
            Directory.CreateSymbolicLink(linkedDirectory, externalDirectory.FullName);
            var history = new FileGeminiInteractionHistory(Path.Combine(linkedDirectory, "usage.jsonl"));

            history.Record(new GeminiInteractionRecord(
                DateTimeOffset.UtcNow,
                "gemini-2.5-flash",
                null,
                true,
                null,
                null,
                1,
                1,
                null));

            Xunit.Assert.False(File.Exists(Path.Combine(externalDirectory.FullName, "usage.jsonl")));
            Xunit.Assert.Empty(history.ReadRecent());
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            directory.Delete(true);
            externalDirectory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task FollowUpParsesUsageAndKeepsPromptAndSecretOutOfLocalHistory()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-history-");
        try
        {
            var historyPath = Path.Combine(directory.FullName, "usage.jsonl");
            var diagnosticsPath = Path.Combine(directory.FullName, "diagnostics.log");
            string? requestBody = null;
            string? apiKey = null;
            using var client = new HttpClient(new StubHandler(request =>
            {
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                apiKey = request.Headers.GetValues("x-goog-api-key").Single();
                return Response("""
                    {
                      "id": "interactions/next",
                      "status": "completed",
                      "output_text": "structured plan",
                      "usage": { "total_input_tokens": 12, "total_output_tokens": 8, "total_tokens": 20 }
                    }
                    """);
            }));
            var result = await new GeminiInteractionsClient(
                client,
                new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero),
                new FileGeminiInteractionHistory(historyPath),
                new FileDiagnosticsLog(diagnosticsPath))
                .CreateFollowUpAsync("revise the approved plan", "interactions/previous", CancellationToken.None);

            Xunit.Assert.Equal("interactions/next", result.InteractionId);
            Xunit.Assert.Equal("structured plan", result.OutputText);
            Xunit.Assert.Equal(20, result.Usage?.TotalTokens);
            Xunit.Assert.Equal(1, result.Attempts);
            Xunit.Assert.Contains("previous_interaction_id", requestBody);
            Xunit.Assert.Equal("test-secret", apiKey);
            Xunit.Assert.DoesNotContain("test-secret", requestBody);
            var history = await File.ReadAllTextAsync(historyPath);
            Xunit.Assert.DoesNotContain("revise the approved plan", history);
            Xunit.Assert.DoesNotContain("test-secret", history);
            Xunit.Assert.Contains("PromptCharacterCount", history);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task RateLimitRetriesAreBoundedAndReportAttemptCount()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-retry-");
        try
        {
            var callCount = 0;
            using var client = new HttpClient(new StubHandler(_ =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage((HttpStatusCode)429)
                    : Response("{\"id\":\"interactions/recovered\",\"status\":\"completed\",\"output_text\":\"ok\"}");
            }));
            var result = await new GeminiInteractionsClient(
                client,
                new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 3, TimeSpan.Zero))
                .CreateFollowUpAsync("retry safely", null, CancellationToken.None);

            Xunit.Assert.Equal(2, callCount);
            Xunit.Assert.Equal(2, result.Attempts);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task OfflineTransportFailureIsTypedAndDoesNotRetryForever()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-offline-");
        try
        {
            var callCount = 0;
            var historyPath = Path.Combine(directory.FullName, "usage.jsonl");
            using var client = new HttpClient(new StubHandler(_ =>
            {
                callCount++;
                throw new HttpRequestException("network unavailable");
            }));

            var exception = await Xunit.Assert.ThrowsAsync<GeminiProviderException>(() => new GeminiInteractionsClient(
                client,
                new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 2, TimeSpan.Zero),
                new FileGeminiInteractionHistory(historyPath))
                .CreateFollowUpAsync("offline request", null, CancellationToken.None));

            Xunit.Assert.Equal(GeminiProviderFailureKind.Transport, exception.FailureKind);
            Xunit.Assert.Equal(2, callCount);
            var history = await File.ReadAllTextAsync(historyPath);
            Xunit.Assert.Contains("Succeeded", history);
            Xunit.Assert.DoesNotContain("offline request", history);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task CallerCancellationStopsBeforeSendingARequest()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(_ =>
        {
            calls++;
            return Response("{\"id\":\"unused\",\"status\":\"completed\",\"output_text\":\"unused\"}");
        }));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = await Xunit.Assert.ThrowsAsync<GeminiProviderException>(() => new GeminiInteractionsClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 3, TimeSpan.Zero))
            .CreateFollowUpAsync("cancel", null, cancellation.Token));
        Xunit.Assert.Equal(GeminiProviderFailureKind.Cancellation, exception.FailureKind);
        Xunit.Assert.Equal(0, calls);
    }

    [Xunit.Fact]
    public async Task PlanInputRejectsMalformedAndOversizedInlineImagesBeforeNetworkUse()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(_ =>
        {
            calls++;
            return Response("{\"id\":\"unused\",\"status\":\"completed\",\"output_text\":\"unused\"}");
        }));
        var service = new GeminiInteractionsClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero));
        var schema = JsonDocument.Parse("{}").RootElement.Clone();

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => service.CreatePlanAsync(
            [
                GeminiInteractionInput.CreateText("plan"),
                new GeminiInteractionInput("image", null, "not-base64", null, "image/png")
            ],
            schema,
            CancellationToken.None));

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => service.CreatePlanAsync(
            [
                GeminiInteractionInput.CreateText("plan"),
                new GeminiInteractionInput("image", null, new string('A', 28_000_000), null, "image/png")
            ],
            schema,
            CancellationToken.None));

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => service.CreatePlanAsync(
            [
                new GeminiInteractionInput("text", "plan", "unexpected", null, "image/png"),
                GeminiInteractionInput.CreateText("metadata")
            ],
            schema,
            CancellationToken.None));

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => service.CreatePlanAsync(
            [
                GeminiInteractionInput.CreateText("plan"),
                new GeminiInteractionInput("image", "unexpected", Convert.ToBase64String([1]), null, "image/png")
            ],
            schema,
            CancellationToken.None));

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => service.CreatePlanAsync(
            [null!], schema, CancellationToken.None));

        Xunit.Assert.Equal(0, calls);
    }

    [Xunit.Fact]
    public async Task ProviderRejectsAnOversizedResponseBeforeDeserialization()
    {
        using var client = new HttpClient(new StubHandler(_ => Response(new string('x', 4 * 1024 * 1024 + 1))));
        var exception = await Xunit.Assert.ThrowsAsync<GeminiProviderException>(() => new GeminiInteractionsClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero))
            .CreateFollowUpAsync("bounded response", null, CancellationToken.None));

        Xunit.Assert.Equal(GeminiProviderFailureKind.MalformedResponse, exception.FailureKind);
    }

    [Xunit.Fact]
    public async Task ProviderWrapsMalformedJsonAsTypedFailure()
    {
        using var client = new HttpClient(new StubHandler(_ => Response("{")));
        var exception = await Xunit.Assert.ThrowsAsync<GeminiProviderException>(() => new GeminiInteractionsClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero))
            .CreateFollowUpAsync("malformed response", null, CancellationToken.None));

        Xunit.Assert.Equal(GeminiProviderFailureKind.MalformedResponse, exception.FailureKind);
    }

    [Xunit.Fact]
    public async Task PlanInputRejectsAnOversizedPromptBeforeNetworkUse()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler(_ =>
        {
            calls++;
            return Response("{\"id\":\"unused\",\"status\":\"completed\",\"output_text\":\"unused\"}");
        }));
        var service = new GeminiInteractionsClient(
            client,
            new GeminiClientOptions("test-secret", "gemini-2.5-flash", TimeSpan.FromSeconds(5), 1, TimeSpan.Zero));
        var schema = JsonDocument.Parse("{}").RootElement.Clone();

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => service.CreatePlanAsync(
            [
                GeminiInteractionInput.CreateText(new string('p', 32_001)),
                GeminiInteractionInput.CreateText("metadata")
            ],
            schema,
            CancellationToken.None));

        Xunit.Assert.Equal(0, calls);
    }

    private static HttpResponseMessage Response(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responder(request));
        }
    }
}
