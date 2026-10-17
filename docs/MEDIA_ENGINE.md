# Timeline, Playback, Audio and Export Specification

## Foundational model
- Source clip metadata: URI, hash, dimensions, pixel format/color, audio stream properties, frame rate rational, time base, variable-frame-rate flags and duration.
- Project frame rate is a rational `numerator/denominator`; timeline time is integer frames at project rate or integer media ticks at source rate. Conversion must be deterministic with documented rounding.
- Track types: video, audio, title, captions, effect/adjustment (where supported). Clips hold source in/out and destination start/duration; explicit compositing order.
- Transform and effect keyframes are time-addressed, interpolated, and serializable. Resolve identical-time conflicts deterministically.

## P0 editing semantics
- Selection, play/pause, jog/scrub, seek, in/out range and snapping.
- Insert/overwrite, trim handles, split at playhead, move, ripple delete, basic two-clip transition, copy/paste and undo.
- Multiple video/audio tracks, volume, mute, fades, audio peak waveform, text overlays, opacity/position/scale animation.
- Source decoding respects orientation, sample aspect, interlacing and source color metadata; unsupported content fails visibly.

## Playback architecture choice
Prototype MLT/native binding and independent FFmpeg preview sample. Record in an ADR:
1. Seek latency (cold/warm), transport latency and frame stepping accuracy.
2. Audio/video sync and gap/overlap behavior.
3. Memory/CPU/GPU impact for 1080p 30fps and 4K proxy workflows.
4. Multi-platform native installation and licensing compatibility.
5. Comparison between actual preview frame and exported same-frame pixel sample.

Never confuse a rendered bitmap slideshow or pre-rendered MP4 with interactive multitrack playback.

## Color and effects
Both preview and export must consume a canonical effect graph and color pipeline. When CPU preview differs from export, golden tests enforce tolerances and UI discloses hardware-quality tradeoffs.

## Export contracts
- Use FFmpeg/ffprobe integration with arguments passed via `ArgumentList`, never concatenated untrusted shell command.
- V1 video: 1080x1920/1920x1080/1080x1080, H.264 and AAC, constant frame rate export, metadata and audio stream selection, optional source framerate where supported.
- V1 photo: PNG/JPEG, declared ICC output profile, alpha only where format supports.
- Render job has ID, progress with frame/time, logs scrubbed of secrets, cancellation, predictable retries and final `ExportReceipt`.
- Write temp output in destination filesystem; verify decode and stream metadata; atomic move to target on success. Never overwrite input assets.

## Media fixtures
Generate/supply *licensed synthetic* video color bars, motion counter, sine/beep timing, two-track overlay and silent track. Fixtures must not be private user footage. Golden expectations cover frame-accurate cuts, dissolves, titles, clip speed, sample-accurate audio within defined tolerance, orientation, output duration and color.

## Performance
- Prioritize UI responsiveness and correct exports before GPU claims.
- Cancellable background worker, bounded queue, in-memory frame cache, disk proxy cache with invalidation by source hash.
- Record reference-machine benchmarks, stale cache resolution, memory quotas and CPU fallback policies.
- Cross-platform feature detection rather than compile-time guesses.
