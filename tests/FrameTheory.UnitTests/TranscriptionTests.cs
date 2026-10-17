using FrameTheory.Captions;

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
                  "SourceFingerprint": "sha256:fixture",
                  "Words": [
                    { "Text": "hello", "StartMs": 100, "EndMs": 400 },
                    { "Text": "world", "StartMs": 450, "EndMs": 800 }
                  ]
                }
                """);

            var result = await new JsonTranscriptProvider(transcriptPath).TranscribeAsync(source, CancellationToken.None);

            Xunit.Assert.Equal("sha256:fixture", result.SourceFingerprint);
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
                  "SourceFingerprint": "sha256:fixture",
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

    private sealed class InlineProgress(Action<TranscriptionProgress> callback) : IProgress<TranscriptionProgress>
    {
        public void Report(TranscriptionProgress value) => callback(value);
    }
}
