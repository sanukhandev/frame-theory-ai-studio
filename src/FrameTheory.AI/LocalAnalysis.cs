using System.Text;
using FrameTheory.Domain;
using FrameTheory.Media;

namespace FrameTheory.AI;

[Flags]
public enum GeminiAnalysisScope
{
    None = 0,
    VideoFrames = 1,
    AudioWaveform = 2,
    PhotoPreview = 4
}

public sealed record GeminiAnalysisRequest
{
    public GeminiAnalysisRequest(IReadOnlyList<ImportedMedia> assets, GeminiAnalysisScope scopes, bool uploadConsent)
    {
        ArgumentNullException.ThrowIfNull(assets);
        const GeminiAnalysisScope supportedScopes = GeminiAnalysisScope.VideoFrames | GeminiAnalysisScope.AudioWaveform | GeminiAnalysisScope.PhotoPreview;
        if (assets.Count == 0 || assets.Any(asset => asset is null) || scopes == GeminiAnalysisScope.None || (scopes & ~supportedScopes) != 0)
        {
            throw new ArgumentException("At least one local asset and one supported analysis scope are required.");
        }

        Assets = Array.AsReadOnly(assets.ToArray());
        Scopes = scopes;
        UploadConsent = uploadConsent;
    }

    public IReadOnlyList<ImportedMedia> Assets { get; }

    public GeminiAnalysisScope Scopes { get; }

    public bool UploadConsent { get; }
}

public sealed record LocalAnalysisProgress(string Stage, double Fraction);

public sealed record LocalAnalysisSample
{
    private readonly byte[] _payload;

    public LocalAnalysisSample(
        Uri source,
        MediaKind kind,
        string sampleKind,
        long? frameIndex,
        TimeSpan? timestamp,
        byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(payload);
        if (!source.IsFile)
        {
            throw new ArgumentException("Analysis samples must use local file URIs.", nameof(source));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(sampleKind) || sampleKind.Length > 64)
        {
            throw new ArgumentException("A bounded analysis sample kind is required.", nameof(sampleKind));
        }

        if (frameIndex < 0 || timestamp < TimeSpan.Zero || payload.Length == 0)
        {
            throw new ArgumentException("Analysis sample timing and payload must be non-negative and non-empty.");
        }

        Source = source;
        Kind = kind;
        SampleKind = sampleKind.Trim();
        FrameIndex = frameIndex;
        Timestamp = timestamp;
        _payload = payload.ToArray();
    }

    public Uri Source { get; }

    public MediaKind Kind { get; }

    public string SampleKind { get; }

    public long? FrameIndex { get; }

    public TimeSpan? Timestamp { get; }

    public byte[] Payload => _payload.ToArray();
}

public sealed record LocalMediaAnalysisReport
{
    public LocalMediaAnalysisReport(
        IReadOnlyList<LocalAnalysisSample> Samples,
        GeminiAnalysisScope Scopes,
        bool UploadConsent,
        long EstimatedUploadBytes,
        string Disclosure)
    {
        ArgumentNullException.ThrowIfNull(Samples);
        var sampleSnapshot = Samples.ToArray();
        if (sampleSnapshot.Any(sample => sample is null))
        {
            throw new ArgumentException("Analysis reports cannot contain null samples.", nameof(Samples));
        }

        const GeminiAnalysisScope supportedScopes = GeminiAnalysisScope.VideoFrames | GeminiAnalysisScope.AudioWaveform | GeminiAnalysisScope.PhotoPreview;
        if (Scopes == GeminiAnalysisScope.None || (Scopes & ~supportedScopes) != 0 || EstimatedUploadBytes < 0 || string.IsNullOrWhiteSpace(Disclosure))
        {
            throw new ArgumentException("Analysis reports require a valid scope, non-negative byte estimate and disclosure.");
        }

        this.Samples = Array.AsReadOnly(sampleSnapshot);
        this.Scopes = Scopes;
        this.UploadConsent = UploadConsent;
        this.EstimatedUploadBytes = EstimatedUploadBytes;
        this.Disclosure = Disclosure.Trim();
    }

    public IReadOnlyList<LocalAnalysisSample> Samples { get; }

    public GeminiAnalysisScope Scopes { get; }

    public bool UploadConsent { get; }

    public long EstimatedUploadBytes { get; }

    public string Disclosure { get; }
}

public interface ILocalAnalysisSampler
{
    Task<LocalMediaAnalysisReport> PrepareAsync(
        GeminiAnalysisRequest request,
        IProgress<LocalAnalysisProgress>? progress,
        CancellationToken cancellationToken);
}

public sealed class LocalAnalysisSampler : ILocalAnalysisSampler
{
    private const int VideoSampleCount = 3;
    private readonly IPreviewFrameDecoder _decoder;

    public LocalAnalysisSampler(IPreviewFrameDecoder decoder)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
    }

    public async Task<LocalMediaAnalysisReport> PrepareAsync(
        GeminiAnalysisRequest request,
        IProgress<LocalAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var samples = new List<LocalAnalysisSample>();
        progress?.Report(new LocalAnalysisProgress("Preparing local analysis scope", 0));
        for (var assetIndex = 0; assetIndex < request.Assets.Count; assetIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var asset = request.Assets[assetIndex];
            if (!asset.Metadata.SourceUri.IsFile)
            {
                throw new InvalidDataException("Only local media can be sampled for Gemini analysis.");
            }

            if (asset.Metadata.Kind == MediaKind.Video && request.Scopes.HasFlag(GeminiAnalysisScope.VideoFrames))
            {
                var frameCount = Math.Min(VideoSampleCount, checked((int)Math.Max(1, Math.Min(asset.Metadata.DurationFrames, VideoSampleCount))));
                for (var sampleIndex = 0; sampleIndex < frameCount; sampleIndex++)
                {
                    var frameIndex = frameCount == 1
                        ? 0
                        : (long)sampleIndex * Math.Max(1, asset.Metadata.DurationFrames - 1) / (frameCount - 1);
                    var frame = await _decoder.DecodeFrameAsync(
                        asset.Metadata.SourceUri,
                        asset.Metadata.FrameRate ?? new RationalFrameRate(1, 1),
                        frameIndex,
                        cancellationToken);
                    samples.Add(new LocalAnalysisSample(
                        asset.Metadata.SourceUri,
                        asset.Metadata.Kind,
                        "video-frame-ppm",
                        frameIndex,
                        asset.Metadata.FrameRate?.TimeAtFrame(frameIndex),
                        frame));
                }
            }

            if (asset.Metadata.Kind == MediaKind.Image && request.Scopes.HasFlag(GeminiAnalysisScope.PhotoPreview))
            {
                var preview = asset.ThumbnailPpm ?? await _decoder.DecodeFrameAsync(asset.Metadata.SourceUri, new RationalFrameRate(1, 1), 0, cancellationToken);
                samples.Add(new LocalAnalysisSample(asset.Metadata.SourceUri, asset.Metadata.Kind, "photo-preview-ppm", 0, TimeSpan.Zero, preview));
            }

            if (asset.Metadata.Kind == MediaKind.Audio && request.Scopes.HasFlag(GeminiAnalysisScope.AudioWaveform))
            {
                var waveform = asset.WaveformPeaks.Count == 0
                    ? throw new InvalidDataException($"No imported waveform is available for '{asset.Metadata.SourceUri.LocalPath}'.")
                    : Encoding.UTF8.GetBytes(string.Join(',', asset.WaveformPeaks.Select(peak => peak.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture))));
                samples.Add(new LocalAnalysisSample(asset.Metadata.SourceUri, asset.Metadata.Kind, "audio-waveform", null, null, waveform));
            }

            progress?.Report(new LocalAnalysisProgress("Preparing local analysis scope", (assetIndex + 1d) / request.Assets.Count));
        }

        if (samples.Count == 0)
        {
            throw new InvalidOperationException("The selected scopes do not match any imported media.");
        }

        var bytes = samples.Sum(sample => (long)sample.Payload.Length);
        var uploadText = request.UploadConsent ? "Upload consent recorded for this exact sample scope; no upload was performed." : "No upload consent was given; samples remain local.";
        progress?.Report(new LocalAnalysisProgress("Local analysis samples ready", 1));
        return new LocalMediaAnalysisReport(
            samples,
            request.Scopes,
            request.UploadConsent,
            bytes,
            $"{samples.Count} bounded sample(s), {bytes} byte(s). {uploadText}");
    }
}
