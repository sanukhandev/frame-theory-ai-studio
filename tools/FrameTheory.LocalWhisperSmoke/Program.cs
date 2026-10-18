using FrameTheory.Captions;

if (args.Length != 3)
{
    throw new ArgumentException("Usage: FrameTheory.LocalWhisperSmoke <whisper-executable> <model-path> <audio-path>");
}

var executablePath = Path.GetFullPath(args[0]);
var modelPath = Path.GetFullPath(args[1]);
var sourcePath = Path.GetFullPath(args[2]);
var provider = new WhisperCliTranscriptionProvider(new LocalWhisperOptions(executablePath, modelPath));
var result = await provider.TranscribeAsync(
    new Uri(sourcePath),
    new Progress<TranscriptionProgress>(progress => Console.WriteLine($"progress={progress.Stage}; fraction={progress.Fraction:0.00}")),
    CancellationToken.None);

if (result.Words.Count == 0)
{
    throw new InvalidDataException("The local Whisper smoke produced no source-timed words.");
}

Console.WriteLine($"local-whisper=ok; words={result.Words.Count}; first={result.Words[0].Text}; last={result.Words[^1].Text}; fingerprint={result.SourceFingerprint}");
