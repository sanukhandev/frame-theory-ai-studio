using FrameTheory.Domain;
using FrameTheory.Media;
using System.Diagnostics;

if (args.Length is < 2 or > 3 || !long.TryParse(args[1], out var frameIndex) || frameIndex < 0 ||
    (args.Length == 3 && (!int.TryParse(args[2], out var sampleCount) || sampleCount is < 1 or > 120)))
{
    throw new ArgumentException("Usage: FrameTheory.FrameDecodeSmoke <media-path> <frame-index> [sample-count 1..120]");
}

var path = Path.GetFullPath(args[0]);
var decoder = new FfmpegFrameDecoder(new FfmpegProcessRunner());
var frameRate = new RationalFrameRate(30, 1);
var requestedSampleCount = args.Length == 3 ? int.Parse(args[2]) : 1;
var stopwatch = Stopwatch.StartNew();
var bytes = await decoder.DecodeFrameAsync(new Uri(path), frameRate, frameIndex, CancellationToken.None);
var coldMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
stopwatch.Restart();
var cachedBytes = await decoder.DecodeFrameAsync(new Uri(path), frameRate, frameIndex, CancellationToken.None);
var warmMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
var sampleMilliseconds = new List<double>(requestedSampleCount);
for (var sample = 0; sample < requestedSampleCount; sample++)
{
    stopwatch.Restart();
    await decoder.DecodeFrameAsync(new Uri(path), frameRate, checked(frameIndex + sample), CancellationToken.None);
    sampleMilliseconds.Add(stopwatch.Elapsed.TotalMilliseconds);
}

var orderedMilliseconds = sampleMilliseconds.OrderBy(value => value).ToArray();
var p50Milliseconds = Percentile(orderedMilliseconds, 0.50);
var p95Milliseconds = Percentile(orderedMilliseconds, 0.95);
var decodeFps = 1000 / sampleMilliseconds.Average();
var workingSetBytes = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine($"decoded_frame={frameIndex}; bytes={bytes.Length}; cached_bytes={cachedBytes.Length}; signature={System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(2, bytes.Length))}; cold_ms={coldMilliseconds:0.###}; warm_ms={warmMilliseconds:0.###}; sample_count={requestedSampleCount}; seek_p50_ms={p50Milliseconds:0.###}; seek_p95_ms={p95Milliseconds:0.###}; decode_fps={decodeFps:0.###}; cache_entries={decoder.CacheEntryCount}; cache_limit={decoder.MaximumCacheEntries}; working_set_bytes={workingSetBytes}");

static double Percentile(IReadOnlyList<double> orderedValues, double percentile)
{
    var index = (orderedValues.Count - 1) * percentile;
    var lower = (int)Math.Floor(index);
    var upper = (int)Math.Ceiling(index);
    if (lower == upper)
    {
        return orderedValues[lower];
    }

    var weight = index - lower;
    return orderedValues[lower] + ((orderedValues[upper] - orderedValues[lower]) * weight);
}
