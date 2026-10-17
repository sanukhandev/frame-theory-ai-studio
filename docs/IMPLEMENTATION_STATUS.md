# Implementation Status — Resume Checkpoint

**Current implementation note:** FT-602 now has consented desktop transcription wiring with source-timed caption import, reparse-point rejection and FFmpeg extraction of the first audio track from local video before upload; live user-key verification, local ASR and long-audio handling remain pending. FT-706 now has a desktop natural-language revision flow that validates provider output before proposal display, chains interaction IDs, records redacted local history and preserves transactional undo; live provider-to-plan verification remains pending. FT-903 now has a bounded repeatable seek trace with p50/p95 latency and decode-throughput output; cross-platform captures remain pending.

**FT-801 checkpoint:** Before/after now composes an editable multi-clip base layer twice—unaltered and graded—and the split/wipe/timed-reveal output is covered by graph tests plus a real two-clip FFmpeg smoke.

**FT-802 checkpoint:** Video and Photo workspaces expose the same 16:9, 1:1 and 9:16 presets. Photo exports cover-crop to exact dimensions, preserve metadata/source bytes, and use the shared cancellable export queue when hosted by the desktop view model.

**FT-803 checkpoint:** Timeline editing now offers amplitude-peak and energy-onset beat marker modes plus an explicit local PNG/JPEG/WebP watermark selection path. Watermarks are validated, looped for the full render, opacity-adjusted and overlaid non-destructively; no approved Frame Theory logo asset is present in the repository, so branded output remains unverified.

**FT-602 checkpoint:** The Video workspace now offers a consent-gated “Transcribe approved audio” action. It reads the OS-stored Gemini key, sends only approved local audio or a bounded FFmpeg-extracted first audio track from an approved local video through the real transcription provider, rejects ungrounded responses, groups source-timed words into editable verified caption cues, and preserves the source SHA-256 in editor state. The live API/key, local ASR and long-audio paths remain unverified.

**FT-706 checkpoint:** The AI workspace now accepts a natural-language revision request, sends only text to the bounded Gemini follow-up client, validates the returned schema/semantics locally, updates the proposal JSON for continued revisions, and leaves the timeline untouched until explicit approval. The previous interaction ID and redacted usage metadata are retained through the existing local history path.

**FT-804 checkpoint:** The shared export queue now exposes queued/running/succeeded/failed/canceled state, bounded in-memory job history, immediate cancellation completion and retry of failed/canceled jobs. Video export exposes a real “Retry last export” action; exporter temp-file cleanup and atomic finalization remain in the existing exporter contracts.

**CI checkpoint:** GitHub Actions run `39050810748` passed documentation/schema validation and .NET restore/build/format/test/publish/checksum jobs on Windows, macOS and Linux for `71b9bca`. The Windows line-ending failure is resolved by `.gitattributes`; hosted packaging is still framework-dependent and unsigned. Commit `bc59879` adds host-FFmpeg installation plus generated import/extracted-audio/repeated-seek assertions to the three-OS job; that new hosted run is pending. The local FT-903 trace tool passed on Windows at `f92c57f`.

**E09 test checkpoint:** Added focused evidence for every keyboard workspace shortcut, validated-plan JSON synchronization/clearing, repeatable seek traces and credential-assignment redaction. These tests strengthen FT-901/FT-902/FT-903/FT-904 but do not replace the required interactive 1280x800/HiDPI manual acceptance.

**Current status: PACKAGING MATRIX AND CI ARTIFACTS PARTIAL (2026-10-17).** FT-001 through FT-005, FT-101 through FT-105, FT-201 through FT-205, FT-301 through FT-306, FT-401 through FT-405, FT-501 through FT-506, FT-601, FT-603 through FT-605, and FT-701 through FT-704 are implemented and verified on Windows x64 with .NET SDK 10.0.100. FT-705 executes approved grade, clip, transition, title, caption, keyframe, audio, trusted-preset and Reel operations transactionally; FT-801 provides split, wipe and timed-reveal output across multiple video clips. FT-802 provides real 16:9, 1:1 and 9:16 MP4 presets plus Photo workspace PNG/JPEG import, non-destructive rotate/undo, preview and quality-controlled export; FT-803 adds imported user audio, amplitude/energy waveform-derived editable beat markers and explicit validated watermark selection; FT-804 adds a single-worker cancellable export queue and Video workspace cancel control; FT-706 exposes undo for the last AI transaction. FT-901 adds keyboard workspace/frame shortcuts, scroll-safe dense workspace layout, automation names for key controls and bounded dense/HiDPI ruler evidence. FT-902 surfaces synchronized selected-clip/playhead context in Video and approved-action counts plus current AI status in the AI panel. FT-903 records real cold/warm frame-seek timing, working-set bytes, bounded cache metrics, repeatable p50/p95 seek and decode-throughput output, and cancellation/cache policy; optional disk proxies remain deferred. FT-904 adds local secret-redacting diagnostics, unhandled-failure capture, actionable missing FFmpeg/ffprobe guidance and visible log location. FT-1001 adds an explicit source SHA-256 preservation assertion to the core unit/integration/golden gate. FT-1002 maps all UC-01..UC-06 to executable Windows smoke/test evidence and records the interactive/platform limits. FT-1003 records the actual restored NuGet/native dependency inventory and external FFmpeg redistribution blockers in `docs/SBOM.md`. FT-1004 extends the Windows/macOS/Linux CI matrix with desktop publish/test artifacts and SHA-256 manifests. FT-1005 provides reproducible framework-dependent publish scripts and a platform/release matrix; installers, native media bundling, signing and notarization remain open. FT-602 has a real consent-gated Gemini Interactions adapter, SHA-256 source provenance, strict word-timing rejection, reparse-source rejection, FFmpeg video-audio extraction, desktop caption wiring and a real synthetic video smoke; live key/media verification, local ASR, long-audio and Files API uploads remain open. Approved watermark licensing, provider retries and usage/redaction logs remain open. FT-006 has a provisional Windows FFmpeg result and safe process adapter, but its two-OS/MLT acceptance gate is not complete. This is not a release.

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
- [~] FT-602 source-bound transcription contract, validated JSON fixture adapter, consent-gated Gemini REST provider, reparse-source rejection, FFmpeg video-audio extraction and desktop caption wiring are implemented; live provider/key verification, local ASR, long-audio and Files API uploads remain open
- [x] FT-603 timed caption overlays with deterministic word wrapping, safe-area placement, font validation and real FFmpeg drawtext preview/export
- [x] FT-604 animated title presets with immutable per-property keyframes, Video workspace inspection and real FFmpeg drawtext export
- [x] FT-605 audio ducking, corrected mute attenuation, and cancellable progressive transcript fixture reads
- [x] FT-701 offline-safe Gemini settings, built-in supported model catalog, OS credential storage and no-key manual editing path
- [x] FT-702 explicit video-frame/audio-waveform/photo-preview scope selector, local bounded sampler, upload-consent disclosure and cancellation
- [x] FT-703 bounded JSON edit-plan parser, semantic project validation and allowlisted operation catalog
- [x] FT-704 AI proposal review panel with action summaries, per-action approval state and no-mutation-before-approval behavior
- [x] FT-705 transactional executor for approved grades, clip edits, transitions, titles, captions, keyframes, audio, trusted presets and bounded Reel actions across multiple clips; real two-clip FFmpeg export smoke verified
- [x] FT-801 editable multi-clip before/after split, wipe and timed-reveal composition with real FFmpeg output; no watermark asset was invented
- [x] FT-802 real MP4 and Photo social aspect presets for 16:9, 1:1 and 9:16, PNG/JPEG quality export, exact-dimension verification and shared cancellable queue path
- [~] FT-803 imported user audio, waveform-derived amplitude/energy beat markers and existing creative title/caption slots; approved watermark asset verification remains open
- [x] FT-804 single-worker cancellable background export queue with Video workspace cancellation, retry, in-memory job history and real FFmpeg retry smoke; macOS/Linux interruption acceptance remains unverified
- [~] FT-706 AI transaction undo control, bounded Gemini follow-up transport, typed retry/timeout/cancellation failures, usage accounting, prompt/secret-redacted local history and desktop revision-to-validator wiring are implemented; live provider-to-plan verification remains open
- [~] FT-901 keyboard workspace/frame shortcuts, scroll-safe layout and automation labels are implemented; interactive 1280x800/HiDPI acceptance remains unverified
- [~] FT-902 synchronized selected-clip/playhead context and AI approval summary are implemented; full manual workspace acceptance remains open
- [~] FT-903 bounded preview cache and cancellable rendering are implemented; the Windows smoke now records cold/warm seeks, p50/p95 latency, decode throughput and working-set bytes, while optional disk proxy implementation and cross-platform traces remain open
- [~] FT-904 local redacted diagnostics, unhandled-failure capture and actionable dependency guidance are implemented; cross-platform failure UX acceptance remains open
- [x] FT-1001 core unit/golden/adversarial suite, caption provenance coverage and source SHA-256 preservation assertion pass on the hosted Windows/macOS/Linux matrix; broader manual/media journeys remain tracked by FT-1002
- [~] FT-1002 UC-01..UC-06 executable smoke/test matrix is documented with Windows evidence; interactive and macOS/Linux acceptance remains open
- [~] FT-1003 dependency inventory and external FFmpeg/native asset assessment are documented; license/CVE clearance and redistribution evidence remain open
- [x] FT-1004 GitHub Actions verifies Windows/macOS/Linux restore/build/format/test/publish jobs and uploads per-OS SHA-256 manifests; optional signing/notarization is not part of this task claim
- [~] FT-1005 framework-dependent Windows/macOS/Linux publish scripts and release matrix are documented; installers, native media bundling and signing remain open
- [~] FT-1006 V1 acceptance audit is recorded with 46 closed implementation issues and explicit remaining P0 blockers; no V1 tag is authorized
- [x] FT-401 FFmpeg-backed PNG/JPEG photo import, PPM preview and bounded canvas fit/100%/zoom/pan state
- [x] FT-402 non-destructive PPM crop, 90° rotation, nearest-neighbor resize and undo-last operation stack
- [x] FT-403 immutable photo adjustment layers with ordering, inspector state and undo/redo history
- [x] FT-404 RGBA-preserving PNG and JPEG photo export with source metadata mapping, quality controls and atomic verification
- [x] FT-405 photo project autosave recovery, missing-media relink and corrupt-input UI status paths

## Next tasks
1. Return to FT-006 when macOS/Linux native prerequisites and an MLT test host are available.
2. Wire FT-602 into the desktop caption workflow and add a known spoken-fixture/provider integration run, then complete FT-803..FT-804 remaining P0 scope.
3. Finish manual/cross-platform FT-901..FT-904 acceptance and hosted FT-1001..FT-1005 gates.

## Latest execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-602 / FT-903 / FT-904
Git SHA: bc59879
Files changed: src/FrameTheory.Media/AudioSourcePreparer.cs, src/FrameTheory.Desktop/MainWindowViewModel.cs, src/FrameTheory.Desktop/MainWindow.cs, src/FrameTheory.Application/Diagnostics.cs, tests/FrameTheory.UnitTests/AudioSourcePreparerTests.cs, tests/FrameTheory.UnitTests/DesktopTranscriptionTests.cs, tests/FrameTheory.UnitTests/DiagnosticsTests.cs, tools/FrameTheory.MediaImportSmoke/Program.cs, .github/workflows/ci.yml, docs/DEVELOPMENT.md, docs/TEST_STRATEGY.md, docs/SECURITY_PRIVACY.md
Actual behavior and UI path: The Video workspace transcription action now prepares approved local audio or extracts the first audio track from an approved local video with FFmpeg, uploads only the prepared audio bytes, restores the original media URI on the verified transcript, rejects oversized derived audio, and deletes owned temporary WAV output. Diagnostics redact Gemini tokens plus API-key/authorization assignments. CI now installs host FFmpeg on each OS, generates a synthetic H.264/AAC clip, verifies import and extracted audio, and asserts repeated seek output.
Commands executed + OS: Windows 11 x64; full `dotnet format`, Release build with warnings as errors, full solution tests, `git diff --check`, focused extraction/diagnostics tests, the media-import smoke and the frame-decode smoke using .NET SDK 10.0.100 and FFmpeg/ffprobe 9.0.2.
Test outputs / exit codes: Full local gate passed with 123 unit tests, 2 golden tests, 0 build warnings/errors and clean diff. Focused extraction tests passed (3) and diagnostics tests passed (2). The real smoke reported `kind=Video; waveform_peaks=128`, then `kind=Audio; duration=00:00:01; has_audio=True`, and a four-sample seek trace reported p50/p95 latency and decode throughput (all exit 0).
Media fixtures and verification: The source MP4 remained unchanged, the temporary WAV was removed on scope disposal, and the request-path test proved the original video bytes were not uploaded. No API key, user media or generated asset entered Git.
Known limitations / blockers: The new three-OS native-media workflow is not hosted-verified yet. Live Gemini key/provider verification, local ASR, long-audio/Files API uploads, approved watermark bytes, MLT/second-OS playback proof, interactive GUI acceptance and installer/signing evidence remain open. Python is not installed, so `scripts/validate_repository.py` was not run locally.
Next unblocked task: FT-901 manual acceptance / FT-902 view acceptance / FT-904 failure UX evidence
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
Windows: FT-001/FT-002/FT-003/FT-004/FT-005/FT-101/FT-102/FT-103/FT-104/FT-105/FT-201/FT-202/FT-203/FT-204/FT-205/FT-301/FT-302/FT-303/FT-304/FT-305/FT-306/FT-401/FT-402/FT-403/FT-404/FT-405/FT-501/FT-502/FT-503/FT-504/FT-505/FT-506/FT-601/FT-603/FT-604/FT-605/FT-701/FT-702/FT-703/FT-704/FT-705/FT-801/FT-802, FT-803, FT-804 and FT-706 undo paths built/tested; FT-1001 core suite and FT-1004 hosted CI artifacts verified; FT-803 watermark/spectral-marker, FT-602 live provider fixture and FT-706 live provider scope remain partial; FT-006 provisional FFmpeg smoke tested on Windows x64; packaged UI unverified. macOS: hosted CI restore/build/test/publish verified by run 39050810748; native FFmpeg/media smoke and interactive desktop acceptance unverified. Linux: hosted CI restore/build/test/publish verified by run 39050810748; native FFmpeg/media smoke and interactive desktop acceptance unverified. Gemini provider: headless adapter contract tested; no live key/media upload run. Logo: source provided outside GitHub repository, not yet committed. MLT: not tested. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
