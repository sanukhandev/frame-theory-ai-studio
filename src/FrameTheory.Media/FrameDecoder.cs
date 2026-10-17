namespace FrameTheory.Media;

using FrameTheory.Domain;

public sealed class MediaDecodeException : Exception
{
    public MediaDecodeException(string message) : base(message)
    {
    }
}

public interface IPreviewFrameDecoder
{
    Task<byte[]> DecodeFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken);
}

public interface IRgbaPreviewFrameDecoder
{
    Task<byte[]> DecodeRgbaFrameAsync(Uri file, RationalFrameRate frameRate, long frameIndex, CancellationToken cancellationToken);
}

public sealed class FfmpegFrameDecoder : IPreviewFrameDecoder, IRgbaPreviewFrameDecoder
{
    private const int MaximumFrameBytes = 20 * 1024 * 1024;
    private readonly IMediaProcessRunner _processRunner;
    private readonly int _maximumCacheEntries;
    private readonly Dictionary<string, byte[]> _cache = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _cacheOrder = [];
    private readonly object _cacheLock = new();

    public FfmpegFrameDecoder(IMediaProcessRunner processRunner, int maximumCacheEntries = 8)
    {
        _processRunner = processRunner;
        if (maximumCacheEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCacheEntries));
        }

        _maximumCacheEntries = maximumCacheEntries;
    }

    public int MaximumCacheEntries => _maximumCacheEntries;

    public int CacheEntryCount
    {
        get
        {
            lock (_cacheLock)
            {
                return _cache.Count;
            }
        }
    }

    public async Task<byte[]> DecodeFrameAsync(
        Uri file,
        RationalFrameRate frameRate,
        long frameIndex,
        CancellationToken cancellationToken)
        => await DecodeFrameInternalAsync(file, frameRate, frameIndex, rgba: false, cancellationToken);

    public async Task<byte[]> DecodeRgbaFrameAsync(
        Uri file,
        RationalFrameRate frameRate,
        long frameIndex,
        CancellationToken cancellationToken)
        => await DecodeFrameInternalAsync(file, frameRate, frameIndex, rgba: true, cancellationToken);

    private async Task<byte[]> DecodeFrameInternalAsync(
        Uri file,
        RationalFrameRate frameRate,
        long frameIndex,
        bool rgba,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.IsFile)
        {
            throw new MediaDecodeException("Only local file URIs can be decoded.");
        }

        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        var path = Path.GetFullPath(file.LocalPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The media file was not found.", path);
        }

        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new MediaDecodeException("Symlinked media paths are not accepted by the decoder.");
        }

        var cacheKey = $"{file.AbsoluteUri}|{frameRate}|{frameIndex}|rgba={rgba}";
        var cached = TryGetCached(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var outputExtension = rgba ? "pam" : "ppm";
        var outputPath = Path.Combine(Path.GetTempPath(), $"frame-theory-preview-{Guid.NewGuid():N}.{outputExtension}");
        try
        {
            var arguments = new List<string>
            {
                "-hide_banner", "-loglevel", "error", "-y",
                "-i", path,
                "-vf", $"select=eq(n\\,{frameIndex})",
                "-frames:v", "1",
                "-f", "image2",
                "-c:v", rgba ? "pam" : "ppm"
            };
            if (rgba)
            {
                arguments.AddRange(["-pix_fmt", "rgba"]);
            }

            arguments.Add(outputPath);
            var result = await _processRunner.RunAsync("ffmpeg", arguments, cancellationToken);
            if (result.ExitCode != 0 || !File.Exists(outputPath))
            {
                throw new MediaDecodeException("FFmpeg could not decode the requested frame.");
            }

            var bytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
            if (bytes.Length == 0 || bytes.Length > MaximumFrameBytes)
            {
                throw new MediaDecodeException("The decoded frame is empty or exceeds the supported size.");
            }

            AddToCache(cacheKey, bytes);
            return bytes.ToArray();
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    private byte[]? TryGetCached(string cacheKey)
    {
        lock (_cacheLock)
        {
            if (!_cache.TryGetValue(cacheKey, out var bytes))
            {
                return null;
            }

            _cacheOrder.Remove(cacheKey);
            _cacheOrder.AddLast(cacheKey);
            return bytes.ToArray();
        }
    }

    private void AddToCache(string cacheKey, byte[] bytes)
    {
        lock (_cacheLock)
        {
            _cache[cacheKey] = bytes.ToArray();
            _cacheOrder.Remove(cacheKey);
            _cacheOrder.AddLast(cacheKey);
            while (_cacheOrder.Count > _maximumCacheEntries)
            {
                var oldest = _cacheOrder.First!.Value;
                _cacheOrder.RemoveFirst();
                _cache.Remove(oldest);
            }
        }
    }
}
