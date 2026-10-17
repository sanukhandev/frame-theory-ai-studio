# Implementation Status — Resume Checkpoint

**Current status: DOMAIN TIME MODEL COMPLETE (2026-10-17).** FT-001 through FT-005 and FT-101 are implemented and verified on Windows x64 with .NET SDK 10.0.100. FT-006 has a provisional Windows FFmpeg result and safe process adapter, but its two-OS/MLT acceptance gate is not complete. This is not a release.

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

## Next tasks
1. FT-102 versioned native project JSON, relative asset references and atomic save/open.
2. Return to FT-006 when macOS/Linux native prerequisites and an MLT test host are available.
3. Iterate per docs/EPICS_AND_TASKS.md; update this checkpoint **after every tested commit**.

## Latest execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-101 / E01
Git SHA: b0b94cb
Files changed: src/FrameTheory.Domain/TimeAndMedia.cs, tests/FrameTheory.UnitTests/FoundationTests.cs, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: Domain code now represents normalized rational frame rates, deterministic frame/time mapping, non-negative frame ranges, stable SHA-256 media IDs from canonical URI plus fingerprint, and validated source metadata without UI/native dependencies.
Commands executed + OS: .NET 10.0.100 on Windows 11 x64; dotnet build FrameTheory.sln --no-restore --configuration Release --warnaserror; dotnet format FrameTheory.sln --no-restore --verify-no-changes --verbosity minimal; dotnet test FrameTheory.sln --no-build --configuration Release; git diff --check
Test outputs / exit codes: build 0 with 0 warnings/0 errors; format verification 0; 7 unit tests and 1 golden test passed.
Media fixtures and verification: Domain-only task; existing synthetic fixtures remain reproducible. FT-006 temporary MP4 smoke outputs were not committed.
Known limitations / blockers: FT-006 remains partial pending MLT and second-OS evidence. Python is not installed, so scripts/validate_repository.py was not run locally. The apphost launch remains denied by this host.
Next unblocked task: FT-102
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
Windows: FT-001/FT-002/FT-003/FT-004/FT-005/FT-101 built/tested; FT-006 provisional FFmpeg smoke tested on Windows x64; packaged UI unverified. macOS: not built or tested locally. Linux: not built or tested locally. Gemini: not connected. Logo: source provided outside GitHub repository, not yet committed. MLT: not tested. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
