using FrameTheory.Domain;
using FrameTheory.Media;
using System.Diagnostics;

if (args.Length != 2 || !long.TryParse(args[1], out var frameIndex) || frameIndex < 0)
{
    throw new ArgumentException("Usage: FrameTheory.FrameDecodeSmoke <media-path> <frame-index>");
}

var path = Path.GetFullPath(args[0]);
var decoder = new FfmpegFrameDecoder(new FfmpegProcessRunner());
var frameRate = new RationalFrameRate(30, 1);
var stopwatch = Stopwatch.StartNew();
var bytes = await decoder.DecodeFrameAsync(new Uri(path), frameRate, frameIndex, CancellationToken.None);
var coldMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
stopwatch.Restart();
var cachedBytes = await decoder.DecodeFrameAsync(new Uri(path), frameRate, frameIndex, CancellationToken.None);
var warmMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
var workingSetBytes = Process.GetCurrentProcess().WorkingSet64;
Console.WriteLine($"decoded_frame={frameIndex}; bytes={bytes.Length}; cached_bytes={cachedBytes.Length}; signature={System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(2, bytes.Length))}; cold_ms={coldMilliseconds:0.###}; warm_ms={warmMilliseconds:0.###}; cache_entries={decoder.CacheEntryCount}; cache_limit={decoder.MaximumCacheEntries}; working_set_bytes={workingSetBytes}");
