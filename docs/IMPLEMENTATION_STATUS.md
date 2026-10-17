# Implementation Status — Resume Checkpoint

**Current status: MEDIA METADATA PROBE COMPLETE (2026-10-17).** FT-001 through FT-005, FT-101 through FT-105 and FT-201 are implemented and verified on Windows x64 with .NET SDK 10.0.100. FT-006 has a provisional Windows FFmpeg result and safe process adapter, but its two-OS/MLT acceptance gate is not complete. This is not a release.

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

## Next tasks
1. FT-202 cancellable frame decode/seek worker with bounded cache.
2. Return to FT-006 when macOS/Linux native prerequisites and an MLT test host are available.
3. Iterate per docs/EPICS_AND_TASKS.md; update this checkpoint **after every tested commit**.

## Latest execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-201 / E02
Git SHA: 8dd5153
Files changed: src/FrameTheory.Domain/TimeAndMedia.cs, src/FrameTheory.Media/MediaProbe.cs, tests/FrameTheory.UnitTests/MediaProbeTests.cs, tools/FrameTheory.MediaProbeSmoke/, FrameTheory.sln, docs/DEVELOPMENT.md, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: `FfprobeMediaProbe` accepts only local file URIs, rejects missing/direct-reparse inputs, invokes ffprobe with explicit input arguments, and parses stream/container duration, dimensions, rational FPS, frame count, audio presence, orientation and color tags. The smoke CLI exposes the headless contract.
Commands executed + OS: .NET 10.0.100 on Windows 11 x64; dotnet restore FrameTheory.sln; dotnet build FrameTheory.sln --no-restore --configuration Release --warnaserror; dotnet format FrameTheory.sln --no-restore --verify-no-changes --verbosity minimal; dotnet test FrameTheory.sln --no-build --configuration Release; real `FrameTheory.MediaProbeSmoke` against temporary H.264/AAC MP4; git diff --check
Test outputs / exit codes: restore 0; build 0 with 0 warnings/0 errors; format verification 0; 18 unit tests and 1 golden test passed; real probe reported Video, 8x4, 30/1, 00:00:01, 30 frames and audio=true.
Media fixtures and verification: Temporary FFmpeg-generated MP4 under `%TEMP%\frame-theory-ffmpeg-spike`; no media binary committed.
Known limitations / blockers: FT-006 remains partial pending MLT and second-OS evidence. Probe uses ffprobe’s reported metadata and has no decoder sandbox yet. Python is not installed, so scripts/validate_repository.py was not run locally. macOS/Linux probe runs are unverified.
Next unblocked task: FT-202
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
Windows: FT-001/FT-002/FT-003/FT-004/FT-005/FT-101/FT-102/FT-103/FT-104/FT-105/FT-201 built/tested; FT-006 provisional FFmpeg smoke tested on Windows x64; packaged UI unverified. macOS: not built or tested locally. Linux: not built or tested locally. Gemini: not connected. Logo: source provided outside GitHub repository, not yet committed. MLT: not tested. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
