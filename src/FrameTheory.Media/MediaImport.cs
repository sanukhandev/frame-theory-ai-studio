using System.Buffers.Binary;
using FrameTheory.Domain;

namespace FrameTheory.Media;

public sealed record ImportedMedia
{
    public ImportedMedia(MediaSourceMetadata metadata, byte[]? thumbnailPpm, IReadOnlyList<float> waveformPeaks)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(waveformPeaks);
        Metadata = metadata;
        _thumbnailPpm = thumbnailPpm?.ToArray();
        WaveformPeaks = Array.AsReadOnly(waveformPeaks.ToArray());
    }

    private readonly byte[]? _thumbnailPpm;

    public MediaSourceMetadata Metadata { get; }

    public byte[]? ThumbnailPpm => _thumbnailPpm?.ToArray();

    public IReadOnlyList<float> WaveformPeaks { get; }
}

public interface IMediaImporter
{
    Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken);
}

public sealed class MediaImportException : Exception
{
    public MediaImportException(string message) : base(message)
    {
    }
}

public sealed class FfmpegMediaImporter : IMediaImporter
{
    private const int WaveformSampleRate = 48000;
    private const int DefaultPeakCount = 128;
    private const int MaximumWaveformBytes = 32 * 1024 * 1024;
    private readonly IMediaProbe _probe;
    private readonly IPreviewFrameDecoder _decoder;
    private readonly IMediaProcessRunner _processRunner;

    public FfmpegMediaImporter(IMediaProbe probe, IPreviewFrameDecoder decoder, IMediaProcessRunner processRunner)
    {
        _probe = probe;
        _decoder = decoder;
        _processRunner = processRunner;
    }

    public async Task<ImportedMedia> ImportAsync(Uri file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var metadata = await _probe.ProbeAsync(file, cancellationToken);
        byte[]? thumbnail = metadata.Kind == MediaKind.Audio
            ? null
            : await _decoder.DecodeFrameAsync(
                file,
                metadata.FrameRate ?? new RationalFrameRate(1, 1),
                0,
                cancellationToken);
        var waveform = metadata.HasAudio || metadata.Kind == MediaKind.Audio
            ? await ExtractWaveformAsync(file, DefaultPeakCount, cancellationToken)
            : Array.Empty<float>();
        return new ImportedMedia(metadata, thumbnail, waveform);
    }

    private async Task<IReadOnlyList<float>> ExtractWaveformAsync(Uri file, int peakCount, CancellationToken cancellationToken)
    {
        if (!file.IsFile)
        {
            throw new MediaImportException("Only local file URIs can be imported.");
        }

        var path = Path.GetFullPath(file.LocalPath);
        if (LocalPathSecurity.ContainsReparsePoint(path))
        {
            throw new MediaImportException("Symlinked media paths are not accepted for waveform extraction.");
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"frame-theory-waveform-{Guid.NewGuid():N}.pcm");
        if (LocalPathSecurity.ContainsReparsePoint(outputPath))
        {
            throw new MediaImportException("The temporary waveform output must not be a reparse point.");
        }

        try
        {
            var result = await _processRunner.RunAsync(
                "ffmpeg",
                [
                    "-hide_banner", "-loglevel", "error", "-y",
                    "-i", path,
                    "-vn", "-ac", "1", "-ar", WaveformSampleRate.ToString(),
                    "-f", "s16le", outputPath
                ],
                cancellationToken);
            if (result.ExitCode != 0 || LocalPathSecurity.ContainsReparsePoint(outputPath) || !File.Exists(outputPath))
            {
                throw new MediaImportException("FFmpeg could not extract the audio waveform.");
            }

            var bytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
            if (bytes.Length == 0 || bytes.Length > MaximumWaveformBytes)
            {
                throw new MediaImportException("The extracted waveform is empty or exceeds the supported size.");
            }

            return BuildPeaks(bytes, peakCount);
        }
        finally
        {
            TryDelete(outputPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static IReadOnlyList<float> BuildPeaks(byte[] bytes, int peakCount)
    {
        if (peakCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(peakCount));
        }

        var sampleCount = bytes.Length / sizeof(short);
        var peaks = new float[peakCount];
        for (var peakIndex = 0; peakIndex < peakCount; peakIndex++)
        {
            var start = (long)peakIndex * sampleCount / peakCount;
            var end = Math.Max(start + 1, (long)(peakIndex + 1) * sampleCount / peakCount);
            end = Math.Min(end, sampleCount);
            for (var sampleIndex = start; sampleIndex < end; sampleIndex++)
            {
                var sample = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(checked((int)(sampleIndex * sizeof(short))), sizeof(short)));
                peaks[peakIndex] = Math.Max(peaks[peakIndex], Math.Abs((int)sample) / (float)short.MaxValue);
            }
        }

        return peaks;
    }
}
