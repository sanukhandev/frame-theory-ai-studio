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
- Optional disk proxies are created only through `FfmpegPreviewProxyService`; the UI does not create them implicitly. Each cache key includes the canonical source URI, caller-provided content SHA-256, maximum dimensions and proxy schema version.
- Proxy files are stored under the caller-selected cache root, generated to a unique temporary file, atomically moved into place, capped at 512 MiB by default, and evicted oldest-first. A changed source fingerprint selects a new entry and cannot reuse the old proxy.
- Proxies are video-only preview artifacts, not final exports; FFmpeg work is cancellable and the service rejects remote or reparse-point paths. Hosted native-media smoke validates creation, ffprobe dimensions and second-request reuse on Windows, macOS and Linux.
- Interactive UI proxy wiring and manual cold-seek acceptance remain open; the existing in-memory decoder remains the default path.

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
