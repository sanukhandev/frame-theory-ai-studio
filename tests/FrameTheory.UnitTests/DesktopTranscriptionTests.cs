using System.Net;
using System.Security.Cryptography;
using System.Text;
using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Captions;
using FrameTheory.Desktop;
using FrameTheory.Media;

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

    [Xunit.Fact]
    public async Task DesktopTranscriptionRejectsDanglingSourceBeforeUpload()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-transcription-link-");
        try
        {
            var linkedPath = Path.Combine(directory.FullName, "linked.wav");
            File.CreateSymbolicLink(linkedPath, Path.Combine(directory.FullName, "missing.wav"));
            var secretStore = new InMemorySecretStore();
            var settings = new GeminiSettingsViewModel(new GeminiConfigurationService(secretStore));
            await settings.SaveAsync("link-secret", CancellationToken.None);
            var viewModel = new MainWindowViewModel(
                new WorkspaceCatalog(),
                new TestCapabilityDetector(),
                geminiSettings: settings);

            var succeeded = await viewModel.TranscribeAudioAsync(linkedPath, uploadConsent: true, CancellationToken.None);

            Xunit.Assert.False(succeeded);
            Xunit.Assert.Contains("symlinked", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
            Xunit.Assert.Empty(viewModel.TimelineEditor.Captions);
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
        }
    }

    [Xunit.Fact]
    public async Task DesktopCaptionImportRejectsDanglingFileBeforeRead()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-caption-link-");
        try
        {
            var linkedPath = Path.Combine(directory.FullName, "linked.srt");
            File.CreateSymbolicLink(linkedPath, Path.Combine(directory.FullName, "missing.srt"));
            var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new TestCapabilityDetector());

            var succeeded = await viewModel.ImportCaptionsAsync(linkedPath, CancellationToken.None);

            Xunit.Assert.False(succeeded);
            Xunit.Assert.Contains("symlinked", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
            Xunit.Assert.Empty(viewModel.TimelineEditor.Captions);
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
        }
    }

    [Xunit.Fact]
    public async Task DesktopCaptionExportRejectsSymlinkedDestinationBeforeWrite()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-caption-export-link-");
        try
        {
            var targetPath = Path.Combine(directory.FullName, "target.srt");
            var linkedPath = Path.Combine(directory.FullName, "linked.srt");
            await File.WriteAllTextAsync(targetPath, "original");
            File.CreateSymbolicLink(linkedPath, targetPath);
            var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new TestCapabilityDetector());
            viewModel.TimelineEditor.ImportCaptions("1\n00:00:00,000 --> 00:00:01,000\nCaption\n");

            var succeeded = await viewModel.ExportCaptionsAsync(linkedPath, CancellationToken.None);

            Xunit.Assert.False(succeeded);
            Xunit.Assert.Contains("symlinked", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
            Xunit.Assert.Equal("original", await File.ReadAllTextAsync(targetPath));
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
        }
    }

    [Xunit.Fact]
    public async Task DesktopTranscriptionExtractsVideoAudioBeforeUpload()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-transcription-video-");
        try
        {
            var videoPath = Path.Combine(directory.FullName, "spoken.mp4");
            var audioPath = Path.Combine(directory.FullName, "spoken.wav");
            await File.WriteAllBytesAsync(videoPath, [9, 9, 9]);
            await File.WriteAllBytesAsync(audioPath, [7, 8, 9]);
            var secretStore = new InMemorySecretStore();
            var settings = new GeminiSettingsViewModel(new GeminiConfigurationService(secretStore));
            await settings.SaveAsync("video-secret", CancellationToken.None);
            string? requestBody = null;
            using var client = new HttpClient(new StubHandler(request =>
            {
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"status\":\"completed\",\"steps\":[{\"content\":[{\"annotations\":[{\"type\":\"word_info\",\"text\":\"video\",\"start_offset\":\"0.100s\",\"end_offset\":\"0.400s\"}]}]}]}",
                        Encoding.UTF8,
                        "application/json")
                };
            }));
            var preparer = new StubAudioSourcePreparer(new Uri(audioPath));
            var viewModel = new MainWindowViewModel(
                new WorkspaceCatalog(),
                new TestCapabilityDetector(),
                geminiSettings: settings,
                transcriptionHttpClient: client,
                audioSourcePreparer: preparer);

            var succeeded = await viewModel.TranscribeAudioAsync(videoPath, uploadConsent: true, CancellationToken.None);

            Xunit.Assert.True(succeeded);
            Xunit.Assert.Equal(new Uri(videoPath), preparer.RequestedSource);
            Xunit.Assert.Contains("audio/wav", requestBody, StringComparison.Ordinal);
            Xunit.Assert.Contains(Convert.ToBase64String([7, 8, 9]), requestBody, StringComparison.Ordinal);
            Xunit.Assert.DoesNotContain(Convert.ToBase64String([9, 9, 9]), requestBody, StringComparison.Ordinal);
            Xunit.Assert.Equal("video", viewModel.TimelineEditor.Captions.Single().Text);
            Xunit.Assert.Equal(Fingerprint(videoPath), viewModel.TimelineEditor.TranscriptSourceFingerprint);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DesktopTranscriptionRejectsSourceMutationDuringUpload()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-transcription-mutation-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            var preparedPath = Path.Combine(directory.FullName, "prepared.wav");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            await File.WriteAllBytesAsync(preparedPath, [7, 8, 9]);
            var secretStore = new InMemorySecretStore();
            var settings = new GeminiSettingsViewModel(new GeminiConfigurationService(secretStore));
            await settings.SaveAsync("mutation-secret", CancellationToken.None);
            using var client = new HttpClient(new StubHandler(_ =>
            {
                File.WriteAllBytes(sourcePath, [4, 5, 6]);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"status\":\"completed\",\"steps\":[{\"content\":[{\"annotations\":[{\"type\":\"word_info\",\"text\":\"changed\",\"start_offset\":\"0.100s\",\"end_offset\":\"0.400s\"}]}]}]}",
                        Encoding.UTF8,
                        "application/json")
                };
            }));
            var viewModel = new MainWindowViewModel(
                new WorkspaceCatalog(),
                new TestCapabilityDetector(),
                geminiSettings: settings,
                transcriptionHttpClient: client,
                audioSourcePreparer: new StubAudioSourcePreparer(new Uri(preparedPath)));

            var succeeded = await viewModel.TranscribeAudioAsync(sourcePath, uploadConsent: true, CancellationToken.None);

            Xunit.Assert.False(succeeded);
            Xunit.Assert.Empty(viewModel.TimelineEditor.Captions);
            Xunit.Assert.Contains("changed", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DesktopVerifiedTranscriptImportIsOfflineAndSourceBound()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-transcript-import-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "offline media");
            var source = new Uri(sourcePath);
            await File.WriteAllTextAsync(transcriptPath, $$"""
                {
                  "Source": "{{source.AbsoluteUri}}",
                  "SourceFingerprint": "{{Fingerprint(sourcePath)}}",
                  "Words": [
                    { "Text": "offline", "StartMs": 100, "EndMs": 400 },
                    { "Text": "caption", "StartMs": 450, "EndMs": 800 }
                  ]
                }
                """);
            var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new TestCapabilityDetector());

            var succeeded = await viewModel.ImportTranscriptAsync(sourcePath, transcriptPath, CancellationToken.None);

            Xunit.Assert.True(succeeded);
            Xunit.Assert.Single(viewModel.TimelineEditor.Captions);
            Xunit.Assert.Equal("offline caption", viewModel.TimelineEditor.Captions[0].Text);
            Xunit.Assert.Equal(CaptionProvenance.VerifiedTranscript, viewModel.TimelineEditor.Captions[0].Provenance);
            Xunit.Assert.Equal(Fingerprint(sourcePath), viewModel.TimelineEditor.TranscriptSourceFingerprint);
            Xunit.Assert.Contains("verified transcript", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DesktopVerifiedTranscriptImportRejectsMismatchWithoutMutation()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-transcript-mismatch-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "offline media");
            var source = new Uri(sourcePath);
            await File.WriteAllTextAsync(transcriptPath, $$"""
                {
                  "Source": "{{source.AbsoluteUri}}",
                  "SourceFingerprint": "sha256:wrong",
                  "Words": [{ "Text": "bad", "StartMs": 100, "EndMs": 400 }]
                }
                """);
            var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new TestCapabilityDetector());
            viewModel.TimelineEditor.ImportCaptions("1\n00:00:00,100 --> 00:00:00,400\nExisting\n");

            var succeeded = await viewModel.ImportTranscriptAsync(sourcePath, transcriptPath, CancellationToken.None);

            Xunit.Assert.False(succeeded);
            Xunit.Assert.Single(viewModel.TimelineEditor.Captions);
            Xunit.Assert.Equal("Existing", viewModel.TimelineEditor.Captions[0].Text);
            Xunit.Assert.Contains("fingerprint", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DesktopVerifiedTranscriptImportHonorsCancellationWithoutMutation()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-transcript-cancel-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            var transcriptPath = Path.Combine(directory.FullName, "spoken.transcript.json");
            await File.WriteAllTextAsync(sourcePath, "offline media");
            await File.WriteAllTextAsync(transcriptPath, "{}");
            var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new TestCapabilityDetector());
            viewModel.TimelineEditor.ImportCaptions("1\n00:00:00,100 --> 00:00:00,400\nExisting\n");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var succeeded = await viewModel.ImportTranscriptAsync(sourcePath, transcriptPath, cancellation.Token);

            Xunit.Assert.False(succeeded);
            Xunit.Assert.Single(viewModel.TimelineEditor.Captions);
            Xunit.Assert.Equal("Existing", viewModel.TimelineEditor.Captions[0].Text);
            Xunit.Assert.Contains("canceled", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Xunit.Fact]
    public async Task DesktopLocalWhisperReportsMissingEngineWithoutMutation()
    {
        var directory = Directory.CreateTempSubdirectory("frame-theory-desktop-local-whisper-");
        try
        {
            var sourcePath = Path.Combine(directory.FullName, "spoken.wav");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var viewModel = new MainWindowViewModel(new WorkspaceCatalog(), new TestCapabilityDetector());
            viewModel.TimelineEditor.ImportCaptions("1\n00:00:00,100 --> 00:00:00,400\nExisting\n");

            var succeeded = await viewModel.TranscribeWithLocalWhisperAsync(
                sourcePath,
                Path.Combine(directory.FullName, "missing-whisper"),
                Path.Combine(directory.FullName, "missing-model"),
                CancellationToken.None);

            Xunit.Assert.False(succeeded);
            Xunit.Assert.Single(viewModel.TimelineEditor.Captions);
            Xunit.Assert.Equal("Existing", viewModel.TimelineEditor.Captions[0].Text);
            Xunit.Assert.Contains("local Whisper", viewModel.CaptionStatusText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static string Fingerprint(string path)
        => $"sha256:{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()}";

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

    private sealed class StubAudioSourcePreparer(Uri preparedSource) : IAudioSourcePreparer
    {
        public Uri? RequestedSource { get; private set; }

        public Task<PreparedAudioSource> PrepareAsync(Uri source, CancellationToken cancellationToken)
        {
            RequestedSource = source;
            return Task.FromResult(new PreparedAudioSource(preparedSource, temporaryPath: null));
        }
    }

    private sealed class TestCapabilityDetector : ICapabilityDetector
    {
        public Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken) => Task.FromResult(
            new CapabilityReport("Test", "x64", [new("Storage", CapabilityState.Available, "ready")]));
    }
}
