using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Domain;

namespace FrameTheory.Storage;

public sealed record CacheIndexEntry(string Key, string RelativePath, long SizeBytes, DateTimeOffset LastAccessUtc);

public sealed class CacheIndex
{
    private readonly Dictionary<string, CacheIndexEntry> _entries = new(StringComparer.Ordinal);

    public IReadOnlyCollection<CacheIndexEntry> Entries => Array.AsReadOnly(_entries.Values.ToArray());

    public long TotalSizeBytes => _entries.Values.Sum(entry => entry.SizeBytes);

    public void Register(string key, string relativePath, long sizeBytes, DateTimeOffset lastAccessUtc)
    {
        var normalizedPath = relativePath?.Trim().Replace('\\', '/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(normalizedPath) ||
            normalizedPath == ".." || normalizedPath.StartsWith("../", StringComparison.Ordinal) || normalizedPath.Contains("/../", StringComparison.Ordinal))
        {
            throw new ArgumentException("Cache keys and paths must be non-empty and relative.");
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        _entries[key] = new CacheIndexEntry(key, normalizedPath, sizeBytes, lastAccessUtc);
    }

    public bool TryGet(string key, out CacheIndexEntry? entry) => _entries.TryGetValue(key, out entry);

    public IReadOnlyList<CacheIndexEntry> EvictToSize(long maximumBytes)
    {
        if (maximumBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        var evicted = new List<CacheIndexEntry>();
        foreach (var entry in _entries.Values.OrderBy(entry => entry.LastAccessUtc).ThenBy(entry => entry.Key).ToArray())
        {
            if (TotalSizeBytes <= maximumBytes)
            {
                break;
            }

            _entries.Remove(entry.Key);
            evicted.Add(entry);
        }

        return Array.AsReadOnly(evicted.ToArray());
    }
}

public sealed class JsonCacheIndexStore
{
    private const long MaximumIndexBytes = 5 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task SaveAtomicAsync(string path, CacheIndex index, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(index);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A cache directory is required.", nameof(path));
        Directory.CreateDirectory(directory);
        EnsureRegularPath(fullPath, directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            }))
            {
                await JsonSerializer.SerializeAsync(stream, index.Entries, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public async Task<CacheIndex> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("A cache directory is required.", nameof(path));
        EnsureRegularPath(fullPath, directory);
        if (!File.Exists(fullPath))
        {
            return new CacheIndex();
        }

        if (new FileInfo(fullPath).Length > MaximumIndexBytes)
        {
            throw new InvalidDataException("The cache index exceeds the supported size limit.");
        }

        await using var stream = new FileStream(fullPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        try
        {
            var entries = await JsonSerializer.DeserializeAsync<CacheIndexEntry[]>(stream, JsonOptions, cancellationToken) ?? [];
            var index = new CacheIndex();
            foreach (var entry in entries)
            {
                if (entry is null)
                {
                    throw new InvalidDataException("The cache index contains a null entry.");
                }

                try
                {
                    index.Register(entry.Key, entry.RelativePath, entry.SizeBytes, entry.LastAccessUtc);
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidDataException("The cache index contains an invalid entry.", exception);
                }
            }

            return index;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The cache index JSON is malformed.", exception);
        }
    }

    private static void EnsureRegularPath(string fullPath, string directory)
    {
        if (LocalPathSecurity.ContainsReparsePoint(directory))
        {
            throw new InvalidDataException("The cache index directory must not be a reparse point.");
        }

        if (LocalPathSecurity.ContainsReparsePoint(fullPath))
        {
            throw new InvalidDataException("The cache index file must not be a reparse point.");
        }
    }
}
