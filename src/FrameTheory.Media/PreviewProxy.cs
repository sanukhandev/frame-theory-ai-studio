using System.Security.Cryptography;
using System.Text;
using FrameTheory.Domain;

namespace FrameTheory.Media;

public sealed record PreviewProxyReceipt(string OutputPath, string CacheKey, long OutputBytes, bool Reused);

public sealed class PreviewProxyException : Exception
{
    public PreviewProxyException(string message) : base(message)
    {
    }
}

public sealed class FfmpegPreviewProxyService
{
    private readonly IMediaProcessRunner _processRunner;
    private readonly string _cacheDirectory;
    private readonly long _maximumCacheBytes;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FfmpegPreviewProxyService(
        IMediaProcessRunner processRunner,
        string cacheDirectory,
        long maximumCacheBytes = 512L * 1024 * 1024)
    {
        _processRunner = processRunner;
        _cacheDirectory = Path.GetFullPath(cacheDirectory);
        _maximumCacheBytes = maximumCacheBytes;
        if (maximumCacheBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCacheBytes));
        }
    }

    public async Task<PreviewProxyReceipt> CreateAsync(
        Uri source,
        string sourceFingerprint,
        int maximumWidth,
        int maximumHeight,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsFile)
        {
            throw new PreviewProxyException("Only local file URIs can be proxied.");
        }

        if (string.IsNullOrWhiteSpace(sourceFingerprint))
        {
            throw new ArgumentException("A source fingerprint is required.", nameof(sourceFingerprint));
        }

        if (maximumWidth < 2 || maximumHeight < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumWidth), "Preview dimensions must be at least 2 pixels.");
        }

        var sourcePath = Path.GetFullPath(source.LocalPath);
        if (LocalPathSecurity.ContainsReparsePoint(sourcePath))
        {
            throw new PreviewProxyException("Symlinked preview sources are not accepted.");
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The preview source was not found.", sourcePath);
        }

        Directory.CreateDirectory(_cacheDirectory);
        if (LocalPathSecurity.ContainsReparsePoint(_cacheDirectory))
        {
            throw new PreviewProxyException("Symlinked preview cache directories are not accepted.");
        }

        var cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{source.AbsoluteUri}\n{sourceFingerprint.Trim()}\n{maximumWidth}x{maximumHeight}\npreview-proxy-v1")));
        var outputPath = Path.Combine(_cacheDirectory, $"{cacheKey}.mp4");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (LocalPathSecurity.ContainsReparsePoint(outputPath))
            {
                throw new PreviewProxyException("Symlinked preview cache entries are not accepted.");
            }

            if (File.Exists(outputPath))
            {
                var existingBytes = new FileInfo(outputPath).Length;
                if (existingBytes > 0 && existingBytes <= _maximumCacheBytes)
                {
                    File.SetLastAccessTimeUtc(outputPath, DateTime.UtcNow);
                    return new PreviewProxyReceipt(outputPath, cacheKey, existingBytes, Reused: true);
                }

                File.Delete(outputPath);
            }

            var temporaryPath = Path.Combine(_cacheDirectory, $".{cacheKey}.{Guid.NewGuid():N}.tmp.mp4");
            try
            {
                var filter = $"scale=trunc(min(iw\\,{maximumWidth})/2)*2:trunc(min(ih\\,{maximumHeight})/2)*2:force_original_aspect_ratio=decrease";
                var result = await _processRunner.RunAsync(
                    "ffmpeg",
                    [
                        "-hide_banner", "-loglevel", "error", "-y", "-i", sourcePath,
                        "-map", "0:v:0", "-vf", filter, "-an", "-c:v", "libx264",
                        "-preset", "ultrafast", "-crf", "28", "-pix_fmt", "yuv420p", temporaryPath
                    ],
                    cancellationToken);
                if (result.ExitCode != 0 || !File.Exists(temporaryPath))
                {
                    throw new PreviewProxyException("FFmpeg could not create the preview proxy.");
                }

                var outputBytes = new FileInfo(temporaryPath).Length;
                if (outputBytes == 0 || outputBytes > _maximumCacheBytes)
                {
                    throw new PreviewProxyException("The preview proxy exceeded the configured cache budget.");
                }

                File.Move(temporaryPath, outputPath, overwrite: true);
                EvictCacheFiles(outputPath);
                File.SetLastAccessTimeUtc(outputPath, DateTime.UtcNow);
                return new PreviewProxyReceipt(outputPath, cacheKey, outputBytes, Reused: false);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EvictCacheFiles(string currentPath)
    {
        var files = Directory.EnumerateFiles(_cacheDirectory, "*.mp4", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .Where(file => !LocalPathSecurity.ContainsReparsePoint(file.FullName))
            .OrderBy(file => file.LastAccessTimeUtc)
            .ToArray();
        var totalBytes = files.Sum(file => file.Length) + new FileInfo(currentPath).Length;
        foreach (var file in files)
        {
            if (totalBytes <= _maximumCacheBytes)
            {
                break;
            }

            var fileBytes = file.Length;
            file.Delete();
            totalBytes -= fileBytes;
        }
    }
}
