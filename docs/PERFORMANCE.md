# Performance Evidence and Preview Policy

## Reference smoke

Run the existing decoder smoke against a local synthetic clip:

```text
dotnet run --project tools/FrameTheory.FrameDecodeSmoke -- <clip.mp4> <frame-index> [sample-count]
```

The optional sample count is bounded to 120. The output records cold and warm exact-frame seek milliseconds, p50/p95 sequential seek latency, decoded throughput, cache entries, cache limit and process working-set bytes. The warm read must be served from the bounded in-memory cache and must not start another FFmpeg process.

## Current policy

- Preview frames use an LRU cache of eight entries by default; callers can lower the entry limit for constrained hosts.
- A decoded frame is rejected above 20 MiB and all decoder/export work accepts cancellation.
- No disk proxy is created implicitly. If proxy mode is added, its files must be keyed by source fingerprint plus decode profile, stored under the user cache root, evicted by the existing cache budget, and deleted when the source fingerprint changes.
- The optional proxy design is intentionally deferred until measured cold seeks show a need; the current smoke evidence is the baseline for that decision.

## Windows reference capture

On Windows 11 x64 with .NET 10.0.100, FFmpeg 9.0.2 and a generated 16x8, 30 fps, three-second H.264 clip, the smoke reported:

```text
decoded_frame=15; bytes=396; cached_bytes=396; signature=P6; cold_ms=113.424; warm_ms=0.237; sample_count=1; seek_p50_ms=0.237; seek_p95_ms=0.237; decode_fps=4219.409; cache_entries=1; cache_limit=8; working_set_bytes=29003776
codec_name=h264
width=16
height=8
r_frame_rate=30/1
```

Values are machine-specific and are evidence, not release performance guarantees.
