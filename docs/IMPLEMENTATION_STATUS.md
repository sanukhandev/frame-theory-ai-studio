# Implementation Status — Resume Checkpoint

**Current status: END-TO-END MATRIX AND QA PARTIAL (2026-10-17).** FT-001 through FT-005, FT-101 through FT-105, FT-201 through FT-205, FT-301 through FT-306, FT-401 through FT-405, FT-501 through FT-506, FT-601, FT-603 through FT-605, and FT-701 through FT-704 are implemented and verified on Windows x64 with .NET SDK 10.0.100. FT-705 now executes approved grade, clip, transition, title, caption, keyframe, audio, trusted-preset and Reel operations transactionally; FT-801 provides split, wipe and timed-reveal output for exactly one video clip. FT-802 provides real 16:9, 1:1 and 9:16 MP4 presets plus Photo workspace PNG/JPEG import, non-destructive rotate/undo, preview and quality-controlled export; FT-803 adds imported user audio and waveform-derived editable beat markers; FT-804 adds a single-worker cancellable export queue and Video workspace cancel control; FT-706 exposes undo for the last AI transaction. FT-901 adds keyboard workspace/frame shortcuts, scroll-safe dense workspace layout, automation names for key controls and bounded dense/HiDPI ruler evidence. FT-902 surfaces synchronized selected-clip/playhead context in Video and approved-action counts plus current AI status in the AI panel. FT-903 records real cold/warm frame-seek timing, working-set bytes, bounded cache metrics and cancellation/cache policy; optional disk proxies remain deferred. FT-904 adds local secret-redacting diagnostics, unhandled-failure capture, actionable missing FFmpeg/ffprobe guidance and visible log location. FT-1001 adds an explicit source SHA-256 preservation assertion to the core unit/integration/golden gate. FT-1002 now maps all UC-01..UC-06 to executable Windows smoke/test evidence and records the interactive/platform limits. Cross-platform workflow, packaging and licensing gates remain open. Watermark licensing, provider retries and usage/redaction logs remain open. FT-602 has a strict source-bound provider contract and JSON fixture adapter, but real local ASR/Gemini transcription is not connected. FT-006 has a provisional Windows FFmpeg result and safe process adapter, but its two-OS/MLT acceptance gate is not complete. This is not a release.

## Completed
- [x] Product README and Codex agent operating contract
- [x] PRD, architecture and color/media/AI boundaries
- [x] Epics, tasks, test plan, release and privacy documentation
- [x] Machine-readable AI edit plan schema and documentation-only CI
- [x] FT-001 solution scaffold with ten production projects, desktop entry point and unit-test project
- [x] FT-001 Release restore/build/test gate on Windows x64
- [x] FT-002 Avalonia shell with Video, Photo, Color and AI workspace navigation
- [x] FT-002 headless view-model tests and managed desktop launch smoke
- [x] FT-003 local OS, storage, FFmpeg, ffprobe and GPU capability report with non-fatal failures
- [x] FT-004 deterministic build settings, analyzer package, formatting check and three-OS CI matrix
- [x] FT-005 deterministic PPM/WAV/frame-counter fixture CLI and golden-byte comparison tests
- [~] FT-006 Windows FFmpeg encode/seek/multitrack smoke and `ArgumentList` process adapter; MLT and second-OS proof remain open
- [x] FT-101 normalized rational frame rates, frame ranges, stable media IDs and validated source metadata
- [x] FT-102 versioned project JSON, relative media references, legacy migration and atomic save/load
- [x] FT-103 immutable commands, inverse operations, named transactions and bounded undo/redo
- [x] FT-104 autosave/recovery, missing-media inspection/relink and persistent bounded cache index
- [x] FT-105 corruption/fuzz handling, oversize guards, typed upgrade errors and symlink containment checks
- [x] FT-201 safe local URI probe via ffprobe with duration, FPS, dimensions, audio, orientation and color metadata
- [x] FT-202 cancellable exact-frame FFmpeg decoder with bounded in-memory cache
- [x] FT-203 synchronized video/audio timeline graph with deterministic seek, scrub, step and sample mapping
- [x] FT-204 real local media import, thumbnail/waveform extraction and Avalonia drag/drop browser
- [x] FT-205 cancellable FFmpeg H.264/AAC export with progress, atomic finalization and ffprobe verification
- [x] FT-301 canonical color graph with explicit sRGB, Rec.709 and linear working profiles
- [x] FT-302 bounded exposure, contrast, saturation and temperature/tint grading operators
- [x] FT-303 bounded `.cube` 3D LUT parser, writer and trilinear interpolator
- [x] FT-304 atomic named preset store, shared pixel application and reversible project preset command
- [x] FT-305 real thumbnail before/after viewer, grade inspector controls and deterministic preview/export parity golden coverage
- [x] FT-306 explicit sRGB PNG/JPEG and Rec.709 SDR video conversion metadata with ffprobe verification
- [x] FT-501 timeline tracks, playhead, frame ruler, snapping, zoom/scroll and exact-frame selection UI
- [x] FT-502 frame-level clip insert, move, split, trim, ripple delete and copy/paste operations with Video workspace controls
- [x] FT-503 deterministic FFmpeg multi-layer compositing, transform contracts, cross-dissolve/wipe graph stages, atomic export and Video workspace composite MP4 export
- [x] FT-504 opacity/position/scale keyframe interpolation, easing, fade/push presets, animated FFmpeg export and selected-clip Video workspace controls
- [x] FT-505 synchronized audio segments with gain, mute, fades, waveform-backed import and AAC timeline mix/export controls
- [x] FT-506 validated title overlays with user-supplied font paths, safe-area placement, z-order and real FFmpeg drawtext export
- [x] FT-601 bounded timed-caption model with imported-subtitle provenance, timezone-independent SRT import/export and Video workspace actions
- [~] FT-602 source-bound transcription provider contract and validated timed-word JSON fixture adapter; real local/Gemini provider remains open
- [x] FT-603 timed caption overlays with deterministic word wrapping, safe-area placement, font validation and real FFmpeg drawtext preview/export
- [x] FT-604 animated title presets with immutable per-property keyframes, Video workspace inspection and real FFmpeg drawtext export
- [x] FT-605 audio ducking, corrected mute attenuation, and cancellable progressive transcript fixture reads
- [x] FT-701 offline-safe Gemini settings, built-in supported model catalog, OS credential storage and no-key manual editing path
- [x] FT-702 explicit video-frame/audio-waveform/photo-preview scope selector, local bounded sampler, upload-consent disclosure and cancellation
- [x] FT-703 bounded JSON edit-plan parser, semantic project validation and allowlisted operation catalog
- [x] FT-704 AI proposal review panel with action summaries, per-action approval state and no-mutation-before-approval behavior
- [~] FT-705 transactional executor for approved grades, clip edits, transitions, titles, captions, keyframes, audio, trusted presets and bounded Reel actions; multi-clip/scope completion remains open
- [~] FT-801 editable one-clip before/after split, wipe and timed-reveal composition with real FFmpeg output; multi-clip wizard remains open
- [~] FT-802 real MP4 social aspect presets for 16:9, 1:1 and 9:16 plus Photo workspace PNG/JPEG quality export; social photo dimensions and queue parity remain open
- [~] FT-803 imported user audio, waveform-derived beat markers and existing creative title/caption slots; approved watermark and richer beat options remain open
- [~] FT-804 single-worker cancellable background export queue with Video workspace cancellation; retry policy and multi-job progress remain open
- [~] FT-706 AI transaction undo control; Gemini follow-up transport, retries, usage and redaction logs remain open
- [~] FT-901 keyboard workspace/frame shortcuts, scroll-safe layout and automation labels are implemented; interactive 1280x800/HiDPI acceptance remains unverified
- [~] FT-902 synchronized selected-clip/playhead context and AI approval summary are implemented; full manual workspace acceptance remains open
- [~] FT-903 bounded preview cache and cancellable rendering are implemented; real Windows seek evidence is recorded, while optional disk proxy implementation and cross-platform traces remain open
- [~] FT-904 local redacted diagnostics, unhandled-failure capture and actionable dependency guidance are implemented; cross-platform failure UX acceptance remains open
- [~] FT-1001 core unit/golden/adversarial suite and source SHA-256 preservation assertion are implemented; full cross-platform integration matrix remains open
- [~] FT-1002 UC-01..UC-06 executable smoke/test matrix is documented with Windows evidence; interactive and macOS/Linux acceptance remains open
- [x] FT-401 FFmpeg-backed PNG/JPEG photo import, PPM preview and bounded canvas fit/100%/zoom/pan state
- [x] FT-402 non-destructive PPM crop, 90° rotation, nearest-neighbor resize and undo-last operation stack
- [x] FT-403 immutable photo adjustment layers with ordering, inspector state and undo/redo history
- [x] FT-404 RGBA-preserving PNG and JPEG photo export with source metadata mapping, quality controls and atomic verification
- [x] FT-405 photo project autosave recovery, missing-media relink and corrupt-input UI status paths

## Next tasks
1. FT-1003 dependency SBOM and codec/license redistribution assessment.
2. FT-1004 cross-platform CI artifact/checksum gates.
3. FT-1005 packaging/platform matrix and release recovery docs.
4. FT-1006 final acceptance audit; return to FT-006 when macOS/Linux native prerequisites and an MLT test host are available.

## Latest execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-1002 / E10
Git SHA: pending commit
Files changed: docs/E2E_JOURNEYS.md
Actual behavior and UI path: The UC-01..UC-06 matrix now points to the real photo, timeline, AI, Reel, color, diagnostics and export smoke/test surfaces, while explicitly distinguishing headless evidence from unverified interactive and cross-platform acceptance.
Commands executed + OS: Evidence references the existing Windows smoke commands and full Release gate; no new code path was introduced in this checkpoint.
Test outputs / exit codes: The mapped smoke/test evidence is recorded in the matrix; interactive Windows and macOS/Linux UI runs remain open.
Media fixtures and verification: Photo export continues to use real FFmpeg/ffprobe and atomic temporary files; no source media or API keys entered Git. Approved logo/watermark asset is still absent from the repository, so no watermark was invented.
Known limitations / blockers: FT-706 still lacks Gemini transport, follow-up request history, provider retries/timeouts, usage accounting and redaction logs. FT-804 has one active worker and no persisted queue/retry history; FT-802 social photo dimensions and queue parity remain open. FT-803 uses amplitude peaks as a basic marker aid. FT-602 remains a contract/fixture adapter pending a real local ASR or consented Gemini provider. FT-006 remains partial pending MLT and second-OS evidence. Python is not installed, so scripts/validate_repository.py was not run locally. macOS/Linux analysis and desktop runs are unverified.
Next unblocked task: FT-1002
```

## Task execution record template
```text
Date/time:
Task / Epic:
Git SHA:
Files changed:
Actual behavior and UI path:
Commands executed + OS:
Test outputs / exit codes:
Media fixtures and verification:
Known limitations / blockers:
Next unblocked task:
```

## Release verification status
Windows: FT-001/FT-002/FT-003/FT-004/FT-005/FT-101/FT-102/FT-103/FT-104/FT-105/FT-201/FT-202/FT-203/FT-204/FT-205/FT-301/FT-302/FT-303/FT-304/FT-305/FT-306/FT-401/FT-402/FT-403/FT-404/FT-405/FT-501/FT-502/FT-503/FT-504/FT-505/FT-506/FT-601/FT-603/FT-604/FT-605/FT-701/FT-702/FT-703/FT-704, FT-705, bounded FT-801/FT-802/FT-803, FT-804 and FT-706 undo paths built/tested; FT-705/FT-801 multi-clip, FT-802 social-photo/queue parity, FT-803 watermark/spectral-marker, FT-804 retry/history and FT-706 provider scope remains partial; FT-602 contract/fixture tested only; FT-006 provisional FFmpeg smoke tested on Windows x64; packaged UI unverified. macOS: not built or tested locally. Linux: not built or tested locally. Gemini provider: not connected. Logo: source provided outside GitHub repository, not yet committed. MLT: not tested. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
