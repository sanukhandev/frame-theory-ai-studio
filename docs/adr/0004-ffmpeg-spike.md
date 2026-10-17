# ADR-0004 — FFmpeg process spike and MLT decision boundary

**Date:** 2026-10-17
**Status:** Accepted for the current deterministic timeline path; MLT not adopted

## Context

The media engine needs safe process invocation, deterministic probe/export receipts, frame seeking and a path to multitrack composition. FFmpeg is a process tool rather than a full NLE, so the repository keeps playback transport and media boundaries behind interfaces and uses a deterministic FFmpeg compositor for verified preview/export behavior. MLT was considered but must not be adopted without measured cross-platform and licensing evidence.

## Evidence

The original Windows 11 x64 spike used the development-only Gyan FFmpeg 9.0.2 package and repository-generated PPM/WAV fixtures:

- A 1-second H.264/AAC MP4 encoded at 30/1 fps and probed at 1.000000 seconds, 30 video frames, 8x4 video and one AAC stream.
- Seeking to 0.5 seconds produced a valid PPM frame without changing the source.
- Two video inputs were scaled and stacked into a 640x160 H.264/AAC MP4 at 30/1 fps with 30 video frames and 1.000000 seconds.
- One low-resolution run measured approximately 113 ms for single-track encode, 87 ms for seek and 142 ms for the two-input graph. These are smoke measurements, not performance claims.
- MLT (`melt`) was not available on the original host, so no native MLT dependency was added.

The follow-up hosted native-media run `39055332326` exercised the selected FFmpeg path on Windows, macOS and Linux with generated H.264/AAC media:

- Each runner imported the clip, extracted its audio and recorded repeated frame seeks with p50/p95 latency and decode throughput.
- `FrameTheory.TimelineExportSmoke` used distinct source paths to build two video layers and two audio layers, including a cross-dissolve, animated overlay and audio gains/fades, then produced and ffprobe-verified a real 16x8 BT.709 MP4.
- The same run produced a real queue-retry export, an approved-AI edit export and an editable before/after Reel export on all three runners.
- The smoke uses no user footage, network service or proprietary native dependency; FFmpeg remains a host capability.

## Decision

Keep FFmpeg behind `IMediaProcessRunner` and pass every argument through `ProcessStartInfo.ArgumentList`. Use `FfmpegFrameDecoder`, `TimelineTransport` and `FfmpegTimelineVideoExporter` as the current tested deterministic timeline path; the desktop transport remains separate from export and does not claim full NLE playback parity. Do not add MLT or bundle a native FFmpeg build until a separate requirement supplies measured cross-platform playback, crash handling and licensing evidence.

## Consequences

The repository exercises real FFmpeg work when the executable is installed, while offline/manual startup remains usable when it is missing. The current Gyan build reports GPL-enabled configuration and is development-only; release redistribution still requires a separate FFmpeg build, codec, patent and license review. FT-006 is complete for the selected deterministic compositor spike; interactive GUI acceptance, native redistribution and installer work remain separate release gates.
