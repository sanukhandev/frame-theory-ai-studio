using FrameTheory.Captions;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FrameTheory.UnitTests;

public sealed class TranscriptionTests
{
    [Xunit.Fact]
    public async Task JsonProviderLoadsSourceBoundWordTimings()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-transcript-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.mp4");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "media");
            var source = new Uri(sourcePath);
            await File.WriteAllTextAsync(transcriptPath, $$"""
                {
                  "Source": "{{source.AbsoluteUri}}",
                  "SourceFingerprint": "{{Fingerprint(sourcePath)}}",
                  "Words": [
                    { "Text": "hello", "StartMs": 100, "EndMs": 400 },
                    { "Text": "world", "StartMs": 450, "EndMs": 800 }
                  ]
                }
                """);

            var result = await new JsonTranscriptProvider(transcriptPath).TranscribeAsync(source, CancellationToken.None);

            Xunit.Assert.Equal(Fingerprint(sourcePath), result.SourceFingerprint);
            Xunit.Assert.Equal(CaptionProvenance.VerifiedTranscript, result.Provenance);
            Xunit.Assert.Equal(TimeSpan.FromMilliseconds(450), result.Words[1].Start);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task JsonProviderRejectsMismatchedSourceAndOverlappingWords()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-transcript-invalid-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.mp4");
            var otherPath = Path.Combine(directory.FullName, "other.mp4");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "media");
            await File.WriteAllTextAsync(otherPath, "other");
            await File.WriteAllTextAsync(transcriptPath, "{\"Source\":\"file:///wrong.mp4\",\"SourceFingerprint\":\"x\",\"Words\":[{\"Text\":\"one\",\"StartMs\":0,\"EndMs\":500},{\"Text\":\"two\",\"StartMs\":400,\"EndMs\":700}]}");

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonTranscriptProvider(transcriptPath).TranscribeAsync(new Uri(sourcePath), CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task JsonProviderRejectsSourceFingerprintMismatch()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-transcript-fingerprint-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.mp4");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "media");
            var source = new Uri(sourcePath);
            await File.WriteAllTextAsync(transcriptPath, $$"""
                {
                  "Source": "{{source.AbsoluteUri}}",
                  "SourceFingerprint": "sha256:wrong",
                  "Words": [{ "Text": "hello", "StartMs": 100, "EndMs": 400 }]
                }
                """);

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonTranscriptProvider(transcriptPath).TranscribeAsync(source, CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task JsonProviderRejectsReparsePointTranscript()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-transcript-link-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.mp4");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            var linkedTranscriptPath = Path.Combine(directory.FullName, "linked.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "media");
            var source = new Uri(sourcePath);
            await File.WriteAllTextAsync(transcriptPath, $$"""
                {
                  "Source": "{{source.AbsoluteUri}}",
                  "SourceFingerprint": "{{Fingerprint(sourcePath)}}",
                  "Words": [{ "Text": "hello", "StartMs": 100, "EndMs": 400 }]
                }
                """);

            try
            {
                File.CreateSymbolicLink(linkedTranscriptPath, transcriptPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                return;
            }

            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new JsonTranscriptProvider(linkedTranscriptPath).TranscribeAsync(source, CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task JsonProviderReportsProgressAndHonorsCancellation()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-transcript-progress-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.mp4");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "media");
            var source = new Uri(sourcePath);
            await File.WriteAllTextAsync(transcriptPath, $$"""
                {
                  "Source": "{{source.AbsoluteUri}}",
                  "SourceFingerprint": "{{Fingerprint(sourcePath)}}",
                  "Words": [{ "Text": "hello", "StartMs": 100, "EndMs": 400 }]
                }
                """);

            var progress = new List<TranscriptionProgress>();
            var provider = new JsonTranscriptProvider(transcriptPath);
            await provider.TranscribeAsync(source, new InlineProgress(progress.Add), CancellationToken.None);

            Xunit.Assert.Equal("Reading", progress[0].Stage);
            Xunit.Assert.Equal("Completed", progress[^1].Stage);
            Xunit.Assert.Equal(1, progress[^1].Fraction);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Xunit.Assert.ThrowsAsync<OperationCanceledException>(() => provider.TranscribeAsync(source, new InlineProgress(_ => { }), cancellation.Token));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task GeminiProviderUploadsOnlyWithConsentAndPreservesTimedWordProvenance()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-transcript-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            var sourceBytes = Encoding.ASCII.GetBytes("synthetic-audio");
            await File.WriteAllBytesAsync(sourcePath, sourceBytes);
            string? capturedApiKey = null;
            string? capturedBody = null;
            using var client = new HttpClient(new StubHandler(request =>
            {
                capturedApiKey = request.Headers.GetValues("x-goog-api-key").Single();
                capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {
                          "status": "completed",
                          "steps": [{
                            "content": [{
                              "annotations": [
                                { "type": "word_info", "text": "hello", "start_offset": "0.100s", "end_offset": "0.400s" },
                                { "type": "word_info", "text": "world", "start_offset": "0.450s", "end_offset": "0.800s" }
                              ]
                            }]
                          }]
                        }
                        """, Encoding.UTF8, "application/json")
                };
            }))
            {
                BaseAddress = new Uri("https://example.test/")
            };
            var progress = new List<TranscriptionProgress>();

            var result = await new GeminiTranscriptionProvider(
                client,
                new GeminiTranscriptionOptions("test-secret", uploadConsent: true))
                .TranscribeAsync(new Uri(sourcePath), new InlineProgress(progress.Add), CancellationToken.None);

            Xunit.Assert.Equal("test-secret", capturedApiKey);
            Xunit.Assert.NotNull(capturedBody);
            Xunit.Assert.Contains("gemini-3.5-transcribe", capturedBody);
            Xunit.Assert.Contains("audio/wav", capturedBody);
            Xunit.Assert.DoesNotContain("test-secret", capturedBody);
            Xunit.Assert.Equal(2, result.Words.Count);
            Xunit.Assert.StartsWith("sha256:", result.SourceFingerprint);
            Xunit.Assert.Equal(TimeSpan.FromMilliseconds(450), result.Words[1].Start);
            Xunit.Assert.Equal("Completed", progress[^1].Stage);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public void GeminiProviderRequiresExplicitConsentAndRejectsUngroundedResults()
    {
        Xunit.Assert.Throws<InvalidOperationException>(() => new GeminiTranscriptionOptions("test-secret", uploadConsent: false));
    }

    [Xunit.Fact]
    public async Task GeminiProviderRejectsSuccessfulResponseWithoutWordAnnotations()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-transcript-invalid-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"completed\",\"steps\":[]}", Encoding.UTF8, "application/json")
            }))
            {
                BaseAddress = new Uri("https://example.test/")
            };

            await Xunit.Assert.ThrowsAsync<GeminiTranscriptionException>(() => new GeminiTranscriptionProvider(
                client,
                new GeminiTranscriptionOptions("test-secret", uploadConsent: true))
                .TranscribeAsync(new Uri(sourcePath), CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task GeminiProviderUsesFilesApiForLongAudioAndDeletesRemoteFile()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-transcript-files-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "long.wav");
            await using (var source = File.Create(sourcePath))
            {
                source.SetLength(20 * 1024 * 1024 + 1);
            }

            var requests = new List<HttpRequestMessage>();
            string? interactionBody = null;
            using (var client = new HttpClient(new StubHandler(request =>
            {
                requests.Add(request);
                if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/upload/v1beta/files", StringComparison.Ordinal))
                {
                    if (request.Headers.Contains("X-Goog-Upload-Protocol"))
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.OK);
                        response.Headers.TryAddWithoutValidation("x-goog-upload-url", "https://generativelanguage.googleapis.com/upload/session");
                        return response;
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"file\":{\"name\":\"files/test-long-audio\",\"uri\":\"https://generativelanguage.googleapis.com/v1beta/files/test-long-audio\",\"mimeType\":\"audio/wav\"}}", Encoding.UTF8, "application/json")
                    };
                }

                if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/upload/session", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"file\":{\"name\":\"files/test-long-audio\",\"uri\":\"https://generativelanguage.googleapis.com/v1beta/files/test-long-audio\",\"mimeType\":\"audio/wav\"}}", Encoding.UTF8, "application/json")
                    };
                }

                if (request.Method == HttpMethod.Delete)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                interactionBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"completed\",\"steps\":[{\"content\":[{\"annotations\":[{\"type\":\"word_info\",\"text\":\"hello\",\"start_offset\":\"0.100s\",\"end_offset\":\"0.400s\"}]}]}]}", Encoding.UTF8, "application/json")
                };
            })))
            {
                var result = await new GeminiTranscriptionProvider(
                    client,
                    new GeminiTranscriptionOptions("test-secret", uploadConsent: true))
                    .TranscribeAsync(new Uri(sourcePath), CancellationToken.None);

                Xunit.Assert.Single(result.Words);
                Xunit.Assert.Contains(requests, request => request.Method == HttpMethod.Delete && request.RequestUri!.AbsolutePath.EndsWith("/v1beta/files/test-long-audio", StringComparison.Ordinal));
                Xunit.Assert.NotNull(interactionBody);
                Xunit.Assert.Contains("\"uri\":\"https://generativelanguage.googleapis.com/v1beta/files/test-long-audio\"", interactionBody);
                Xunit.Assert.DoesNotContain("\"data\"", interactionBody);
            }
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task GeminiProviderRejectsAndCleansUpAnUntrustedFileUri()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-transcript-uri-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "long.wav");
            await using (var source = File.Create(sourcePath))
            {
                source.SetLength(20 * 1024 * 1024 + 1);
            }

            var deleted = false;
            using var client = new HttpClient(new StubHandler(request =>
            {
                if (request.Method == HttpMethod.Post && request.Headers.Contains("X-Goog-Upload-Protocol"))
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Headers.TryAddWithoutValidation("x-goog-upload-url", "https://generativelanguage.googleapis.com/upload/session");
                    return response;
                }

                if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/upload/session", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"file\":{\"name\":\"files/untrusted-uri\",\"uri\":\"https://example.test/file\",\"mimeType\":\"audio/wav\"}}", Encoding.UTF8, "application/json")
                    };
                }

                if (request.Method == HttpMethod.Delete)
                {
                    deleted = true;
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

            await Xunit.Assert.ThrowsAsync<GeminiTranscriptionException>(() => new GeminiTranscriptionProvider(
                client,
                new GeminiTranscriptionOptions("test-secret", uploadConsent: true))
                .TranscribeAsync(new Uri(sourcePath), CancellationToken.None));

            Xunit.Assert.True(deleted);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task GeminiProviderRejectsReparsePointSource()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-gemini-transcript-link-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            var linkPath = Path.Combine(directory.FullName, "linked.wav");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            try
            {
                File.CreateSymbolicLink(linkPath, sourcePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                return;
            }

            using var client = new HttpClient();
            await Xunit.Assert.ThrowsAsync<InvalidDataException>(() => new GeminiTranscriptionProvider(
                client,
                new GeminiTranscriptionOptions("test-secret", uploadConsent: true))
                .TranscribeAsync(new Uri(linkPath), CancellationToken.None));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static string Fingerprint(string path)
        => $"sha256:{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()}";

    private sealed class InlineProgress(Action<TranscriptionProgress> callback) : IProgress<TranscriptionProgress>
    {
        public void Report(TranscriptionProgress value) => callback(value);
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
