# Implementation Status — Resume Checkpoint

**Current status: FOUNDATION MEDIA SPIKE PARTIAL (2026-10-17).** FT-001 through FT-005 are implemented and verified on Windows x64 with .NET SDK 10.0.100. FT-006 has a provisional Windows FFmpeg result and safe process adapter, but its two-OS/MLT acceptance gate is not complete. This is not a release.

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

## Next tasks
1. FT-101 rational timecode and stable media metadata types (independent of the FT-006 platform blocker).
2. Return to FT-006 when macOS/Linux native prerequisites and an MLT test host are available.
3. Iterate per docs/EPICS_AND_TASKS.md; update this checkpoint **after every tested commit**.

## Latest execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-006 / E00 (partial; not complete)
Git SHA: pending commit
Files changed: src/FrameTheory.Media/MediaProcessRunner.cs, tests/FrameTheory.UnitTests/FoundationTests.cs, docs/adr/0004-ffmpeg-spike.md, docs/DECISIONS.md, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: Media subprocesses use `ArgumentList`, cancellation kills the process tree, and nonzero startup errors become typed `InvalidOperationException`s. A Windows FFmpeg smoke encoded/probed/seeked a real synthetic MP4 and composed two video inputs with audio.
Commands executed + OS: .NET 10.0.100 on Windows 11 x64; dotnet build FrameTheory.sln --no-restore --configuration Release --warnaserror; dotnet test FrameTheory.sln --no-build --configuration Release; FFmpeg 9.0.2 encode, ffprobe, seek and two-input graph commands; git diff --check
Test outputs / exit codes: build 0 with 0 warnings/0 errors; 5 unit tests and 1 golden test passed; FFmpeg/ffprobe commands exit 0; single 1.000000s/30-frame H.264/AAC output, seek PPM, and 640x160 1.000000s/30-frame two-input output verified.
Media fixtures and verification: Synthetic PPM/WAV/CSV inputs and temporary MP4/PPM outputs under `%TEMP%\frame-theory-ffmpeg-spike`; no repository binaries committed.
Known limitations / blockers: FT-006 is not complete: MLT was not available, macOS/Linux were not tested, and release FFmpeg/codec licensing is not approved. Python is not installed, so scripts/validate_repository.py was not run locally. The apphost launch remains denied by this host.
Next unblocked task: FT-101
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
Windows: FT-001/FT-002/FT-003/FT-004/FT-005 built/tested; FT-006 provisional FFmpeg smoke tested on Windows x64; packaged UI unverified. macOS: not built or tested locally. Linux: not built or tested locally. Gemini: not connected. Logo: source provided outside GitHub repository, not yet committed. MLT: not tested. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
