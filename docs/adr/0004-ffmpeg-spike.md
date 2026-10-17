# ADR-0004 — FFmpeg process spike and MLT decision boundary

**Date:** 2026-10-17
**Status:** Provisional; FT-006 remains incomplete

## Context

The media engine needs safe process invocation, deterministic probe/export receipts, frame seeking and a path to multitrack composition. FFmpeg is a process tool, not an interactive timeline engine, so MLT must not be adopted without measured cross-platform evidence.

## Evidence

On Windows 11 x64, using the development-only Gyan FFmpeg 9.0.2 package and the repository-generated PPM/WAV fixtures:

- A 1-second H.264/AAC MP4 encoded at 30/1 fps and probed at 1.000000 seconds, 30 video frames, 8x4 video and one AAC stream.
- Seeking to 0.5 seconds produced a valid PPM frame without changing the source.
- Two video inputs were scaled and stacked into a 640x160 H.264/AAC MP4 at 30/1 fps with 30 video frames and 1.000000 seconds.
- One low-resolution run measured approximately 113 ms for single-track encode, 87 ms for seek and 142 ms for the two-input graph. These are smoke measurements, not performance claims.
- MLT (`melt`) was not available on the host. macOS and Linux measurements were not run.

## Decision

Keep FFmpeg behind `IMediaProcessRunner` and pass every argument through `ProcessStartInfo.ArgumentList`. The adapter is the only media process boundary currently committed. Do not add MLT or bundle a native FFmpeg build until playback, seek, multitrack behavior, crash handling and licensing are measured on at least two operating systems.

## Consequences

The repository can exercise real FFmpeg work when the executable is installed, while offline/manual startup remains usable when it is missing. The current Gyan build reports GPL-enabled configuration and is development-only; release redistribution requires a separate FFmpeg build, codec, patent and license review. FT-006 is blocked on a second OS and MLT feasibility evidence.
