# Implementation Status — Resume Checkpoint

**Current status: PHOTO EXPORT COMPLETE (2026-10-17).** FT-001 through FT-005, FT-101 through FT-105, FT-201 through FT-205, FT-301 through FT-304 and FT-401 through FT-404 are implemented and verified on Windows x64 with .NET SDK 10.0.100. FT-006 has a provisional Windows FFmpeg result and safe process adapter, but its two-OS/MLT acceptance gate is not complete. This is not a release.

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
- [x] FT-401 FFmpeg-backed PNG/JPEG photo import, PPM preview and bounded canvas fit/100%/zoom/pan state
- [x] FT-402 non-destructive PPM crop, 90° rotation, nearest-neighbor resize and undo-last operation stack
- [x] FT-403 immutable photo adjustment layers with ordering, inspector state and undo/redo history
- [x] FT-404 RGBA-preserving PNG and JPEG photo export with source metadata mapping, quality controls and atomic verification

## Next tasks
1. FT-405 photo recovery/relink, missing asset and corrupt input UI tests.
2. Return to FT-006 when macOS/Linux native prerequisites and an MLT test host are available.
3. Iterate per docs/EPICS_AND_TASKS.md; update this checkpoint **after every tested commit**.

## Latest execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-404 / E04
Git SHA: 383a9ff
Files changed: src/FrameTheory.Media/FrameDecoder.cs, src/FrameTheory.Photo/PhotoCanvas.cs, src/FrameTheory.Photo/PhotoExport.cs, tests/FrameTheory.UnitTests/FrameDecoderTests.cs, tests/FrameTheory.UnitTests/PhotoAlphaTests.cs, tests/FrameTheory.UnitTests/PhotoExportTests.cs, tools/FrameTheory.PhotoExportSmoke, docs/DEVELOPMENT.md, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: `FfmpegFrameDecoder` provides a cached RGBA PAM path for photos. `PpmRaster` preserves alpha through crop, rotate, resize and shared color adjustments. `PhotoExporter` encodes the editable render to PNG or JPEG with bounded quality, source metadata mapping, ffprobe dimension/kind verification and same-directory atomic finalization; PNG preserves alpha and JPEG intentionally flattens it.
Commands executed + OS: .NET 10.0.100 on Windows 11 x64; dotnet build FrameTheory.sln --no-restore --configuration Release --warnaserror; dotnet format FrameTheory.sln --no-restore --verify-no-changes --verbosity minimal; dotnet test FrameTheory.sln --no-build --configuration Release; git diff --check
Test outputs / exit codes: build 0 with 0 warnings/0 errors; format verification 0; 45 unit tests and 1 golden test passed; RGBA parse/transform, layer output, export bounds, failure atomicity and undo/redo passed.
Media fixtures and verification: Temporary FFmpeg-generated transparent PNG exported to verified 16x8 RGBA PNG, plus 16x8 PNG/JPEG export smoke outputs; no media binary committed.
Known limitations / blockers: FT-006 remains partial pending MLT and second-OS evidence. Photo project persistence, visible canvas rendering and UI export wiring remain later tasks. Python is not installed, so scripts/validate_repository.py was not run locally. macOS/Linux photo runs are unverified.
Next unblocked task: FT-405
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
Windows: FT-001/FT-002/FT-003/FT-004/FT-005/FT-101/FT-102/FT-103/FT-104/FT-105/FT-201/FT-202/FT-203/FT-204/FT-205/FT-301/FT-302/FT-303/FT-304/FT-401/FT-402/FT-403/FT-404 built/tested; FT-006 provisional FFmpeg smoke tested on Windows x64; packaged UI unverified. macOS: not built or tested locally. Linux: not built or tested locally. Gemini: not connected. Logo: source provided outside GitHub repository, not yet committed. MLT: not tested. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
