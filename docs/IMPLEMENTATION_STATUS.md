# Implementation Status — Resume Checkpoint

**Current implementation note:** FT-003 now probes the OS, storage, FFmpeg/ffprobe and platform GPU adapters without claiming acceleration when a probe is unavailable; CPU fallback remains explicit. FT-006 now has a hosted three-OS FFmpeg deterministic timeline spike covering seek, two video layers, two audio layers, transitions, AI-applied edits, queue retry and Reel export; MLT was not adopted and interactive GUI parity remains a separate gate. FT-602 now has consented desktop transcription wiring with source-timed caption import, reparse-point rejection, FFmpeg extraction of the first audio track from local video and a resumable Gemini Files API path for approved audio over 20 MiB; live user-key/spoken-fixture verification and local ASR remain pending. FT-706 now has a consented multimodal initial-plan path from sampled video/photo frames, including inline PNG and larger-scope Files API inputs, plus a desktop natural-language revision flow; both validate provider output before proposal display, chain interaction IDs, record redacted local history and preserve transactional undo; live provider-to-plan verification remains pending. FT-902 now exposes real current-frame preview plus cancellable frame-accurate Play/Stop playback through the shared FFmpeg preview decoder; interactive workspace acceptance remains pending. FT-903 now has bounded repeatable seek traces with p50/p95 latency and decode-throughput output plus an optional content-fingerprint-keyed disk proxy with atomic creation, budgeted eviction and hosted native-media validation on Windows, macOS and Linux; interactive GUI acceptance remains pending. FT-904 now distinguishes missing executables from nonzero probe failures, captures version output from either stdout or stderr, exposes bounded actionable unknown-state details and lets users rerun the capability check after fixing dependencies; hosted validation passes while cross-platform interactive failure UX remains pending. FT-1003 now has a reproducible CI/local NuGet advisory scan plus per-run NuGet and FFmpeg/ffprobe evidence artifacts; licensing, codec and native redistribution clearance remain pending. FT-1005 now supports opt-in self-contained publishes in addition to framework-dependent validation packages; installers, native media bundling and signing remain pending. FT-1101 now provides non-destructive imported photo overlays with blending, opacity, clipping, persisted brush masks and clone-style retouch strokes, with UI controls and hosted native export/reopen evidence. FT-1102 now includes mask-aware non-destructive photo grading, deterministic scopes, translation tracking, decoded-frame motion reporting and persisted FFmpeg stabilization; visual-quality acceptance remains open. FT-1103 now has editable speed-ramp points with synchronized audio timing, persistence, Video workspace controls and real FFmpeg export coverage. FT-1105 now provides a real desktop multi-target batch export action for landscape, square and portrait MP4s, validated atomic AI recipe save/load through the existing allowlist and approval flow, cancellation, per-target progress, duplicate-path validation and social-canvas fit before FFmpeg padding; approved brand kits remain pending. The desktop Video workspace now saves and reopens versioned `.ftstudio` projects with atomic JSON, editable timeline operations, relative media references and media-ID remapping.

**Local media checkpoint (2026-10-17):** With .NET SDK 10.0.402 and FFmpeg/ffprobe 9.0.2 installed outside the repository, the Windows native smoke passed synthetic H.264/AAC import, 128-point waveform extraction, first-audio-track WAV preparation, an 8x4 preview proxy with cache reuse, local analysis without upload consent, two-video/two-audio timeline export, queue retry, AI grade/transition export and before/after Reel export. The final timeline output was a real 16x8 BT.709 MP4 with video and audio streams and a 2-second duration. The native smoke also saved and reopened a `.ftstudio` project using real FFmpeg imports and verified two reimported media items.

**FT-003 GPU capability checkpoint (2026-10-18):** `SystemCapabilityDetector` now uses fixed, platform-specific adapter probes: Windows PowerShell CIM output with an explicit GPU marker, macOS `system_profiler`, and Linux `lspci`. Successful adapter descriptions are reported as available; missing tools, nonzero probes and no-adapter results remain actionable `Unknown` states with explicit CPU fallback rather than a “not configured” placeholder. Focused foundation coverage passes 13/13, including deterministic probe injection and nonzero/no-adapter behavior.

**Timeline project checkpoint (2026-10-17):** The Video workspace now exposes Save/Open timeline project actions. Save serializes validated editable clips, audio settings, keyframes, transitions, grades, titles, captions, watermark settings, beat markers, playhead and view state into the existing atomic project JSON; open reimports relative local media, maps saved media IDs to current probe IDs, restores the editable state and leaves the current project unchanged on cancellation or import failure. Focused round-trip and malformed-state tests pass.

**Timeline restore safety checkpoint (2026-10-17):** Restore now validates and constructs all derived clip settings, media mappings and view state before swapping them into the active editor. An invalid saved grade is covered by a regression test proving the current timeline remains unchanged. Focused persistence tests pass 4/4.

**FT-1002 timeline relink checkpoint (2026-10-18):** Video project open now detects missing relative media before mutating the active timeline, exposes the missing references in the Video workspace and atomically relinks selected replacements within the project folder before reopening. Focused persistence/relink coverage passes 5/5; failures and cancellation preserve the current project. Hosted run `39068478308` passed the full three-OS quality and native-media matrix.

**FT-1002 moved-project checkpoint (2026-10-18):** Timeline project save/open now has regression coverage for moving the complete project directory, resolving its relative media after the move and restoring the editable timeline without a relink. Focused persistence coverage passes 7/7; the remaining FT-1002 gate is manual Avalonia UI and platform acceptance.

**Timeline playback checkpoint (2026-10-17):** The Video workspace now exposes cancellable frame-accurate Play/Stop controls. Playback temporarily disables timeline snapping so boundary clamping cannot stall frame advancement, decodes every frame through the shared preview path, restores the prior snapping setting and reports cancellation or completion. Focused playback coverage passes 4/4.

**FT-903 preview proxy checkpoint (2026-10-17):** The Video workspace now exposes an opt-in bounded preview-proxy toggle. When enabled, current-frame preview and playback create or reuse an atomic FFmpeg proxy in the local cache, decode from that proxy, propagate cancellation and report cache usage without replacing the original media. Focused proxy/UI wiring coverage passes 5/5.

**FT-402 photo project checkpoint (2026-10-18):** The Photo workspace now saves and reopens `.ftstudio` projects through the existing atomic repository. The persisted state includes crop/rotate/resize operations and adjustment layers, uses relative local media references with SHA-256 provenance, restores the editable state before replacing the current photo and preserves the current photo when open fails. Focused photo project coverage passes 3/3.

**FT-402 native smoke checkpoint (2026-10-18):** The real `FrameTheory.PhotoExportSmoke` now imports a generated PNG through FFmpeg, applies a non-destructive rotation, saves and reopens a `.ftstudio` project through `PhotoEditorViewModel`, compares the restored rendered bytes and exports a verified PNG. Hosted run `39065964020` passed this smoke on Windows, macOS and Linux.

**FT-801 checkpoint:** Before/after now composes an editable multi-clip base layer twice—unaltered and graded—and the split/wipe/timed-reveal output is covered by graph tests plus a real two-clip FFmpeg smoke.

**FT-402 source-integrity checkpoint (2026-10-18):** Photo project open now verifies a saved `sha256:` source fingerprint before importing; changed source bytes are rejected and the current edit remains loaded. Focused photo persistence coverage passes 4/4.

**FT-904 capability retry checkpoint (2026-10-18):** The desktop status bar now exposes an accessible “Refresh capability check” action. It reruns the existing local OS, storage, FFmpeg and ffprobe probes without restarting the application, so dependency fixes and probe failures are visible through the same user-facing status path.

**FT-802 checkpoint:** Video and Photo workspaces expose the same 16:9, 1:1 and 9:16 presets. Photo exports cover-crop to exact dimensions, preserve metadata/source bytes, and use the shared cancellable export queue when hosted by the desktop view model.

**FT-803 checkpoint:** Timeline editing now offers amplitude-peak and energy-onset beat marker modes plus an explicit local PNG/JPEG/WebP watermark selection path. Watermarks are validated, looped for the full render, opacity-adjusted and overlaid non-destructively; no approved Frame Theory logo asset is present in the repository, so branded output remains unverified.

**FT-602 checkpoint:** The Video workspace now offers a consent-gated “Transcribe approved audio” action. It reads the OS-stored Gemini key, sends only approved local audio or a bounded FFmpeg-extracted first audio track from an approved local video through the real transcription provider, uses the resumable Files API for audio over 20 MiB, deletes the temporary remote file, rejects ungrounded responses, groups source-timed words into editable verified caption cues, and preserves the source SHA-256 in editor state. The live API/key/spoken fixture and local ASR remain unverified.

**FT-602 long-audio checkpoint:** The provider uses the official Gemini upload host, validates both the resumable session URL and the returned file URI, sends the uploaded URI to `gemini-3.5-transcribe`, and deletes the returned `files/*` resource in a bounded cleanup path. Fake-HTTP coverage verifies the upload, URI input, delete request and rejection/cleanup of an untrusted URI; live provider/key and local ASR acceptance remain open.

**FT-706 checkpoint:** The AI workspace now accepts a natural-language revision request, sends only text to the bounded Gemini follow-up client, validates the returned schema/semantics locally, updates the proposal JSON for continued revisions, and leaves the timeline untouched until explicit approval. The previous interaction ID and redacted usage metadata are retained through the existing local history path.

**FT-804 checkpoint:** The shared export queue now exposes queued/running/succeeded/failed/canceled state, bounded in-memory job history, immediate cancellation completion and retry of failed/canceled jobs. Video export exposes a real “Retry last export” action; exporter temp-file cleanup and atomic finalization remain in the existing exporter contracts.

**FT-1103 kinetic-caption checkpoint (2026-10-18):** Verified transcript word timings now survive caption modeling, `.ftstudio` timeline persistence and restore. The Video workspace exposes a Kinetic word captions toggle; when enabled, the FFmpeg graph emits one timed `drawtext` stage per source-grounded word. Focused caption/editor/persistence coverage passes 24/24, the full unit/golden/build/format gate passes 148/2 with 0 build warnings/errors, and the native timeline smoke produced a verified kinetic-caption MP4. Hosted run `39068043241` passed the same quality and native-media matrix on Windows, macOS and Linux. Speed-ramp curves remain open under FT-1103.

**FT-1103 speed-ramp checkpoint (2026-10-18):** Selected video clips now accept validated 0.25x–4x speed-ramp points, persist them in `.ftstudio` state, expose apply/clear controls in the Video workspace and render synchronized piecewise video/audio segments through FFmpeg. Graph coverage verifies timebase normalization for transition compatibility; the native smoke produced and probed real ramped video/audio, caption, retry, AI and Reel MP4 outputs at 16x8 BT.709. Full local Release tests pass 152/2, build has 0 warnings/errors, formatting verification passes, and hosted run `39069354787` passed the complete Windows/macOS/Linux quality and native-media matrix for `aee3088`.

**FT-1104 reference-match checkpoint (2026-10-18):** The offline `ColorReferenceMatcher` now derives a bounded exposure, saturation, temperature and tint grade from two approved RGB samples without network or media upload, providing a deterministic headless seed for the existing reusable color-preset store. The Color workspace now saves and loads atomic style-library JSON files, applies selected grade/LUT styles to the real before/after preview and exposes accessible controls. The headless `GeminiReferenceAnalysisClient` accepts only bounded visual samples, requires explicit consent, converts local PPM previews to inline PNG input for small requests, switches oversized visual samples to the official Files API with URI inputs and bounded cleanup, redacts the API key from payloads, and locally validates the returned editable grade before any caller can apply it. The `RoughCutPlanner` orders imported clips by measured waveform energy/onsets; the Video workspace exposes review/apply controls, and transactional apply preserves linked audio, beat markers, editable settings and undo. The native smoke now applies the reviewed plan, saves/reopens the editable timeline and exports a real rough-cut MP4. Focused matcher/inspector/reference/rough-cut coverage passes 16/16 plus the large-reference Files API test; full local Release tests pass 162/2, build has 0 warnings/errors and formatting verification passes. Hosted runs `39069905388`, `39071362366` and `39071860694` passed the style-library, rough-cut export/persistence and large-reference code matrices, including documentation, native-media, format, test, publish and checksum gates on Windows, macOS and Linux. Live-key evidence and reference-grade semantic acceptance remain open; FT-1104 stays open until those limits and the full acceptance evidence are resolved.

**FT-1105 batch-export checkpoint (2026-10-18):** `TimelineBatchExporter` now runs validated landscape, square and portrait timeline targets sequentially with per-target progress, cancellation and duplicate destination rejection. The Video workspace exposes “Export all social targets” through a folder picker; each target is rendered at its exact social dimensions, and the compositor fits oversized source clips before padding so square/portrait exports do not fail on larger media. The AI workspace now saves and loads named `.ftrecipe.json` files atomically only after allowlist/schema validation; loading revalidates against the active project and never applies actions without explicit approval. Focused batch/desktop/recipe coverage passes 32/32; the full Release solution passes 189 unit and 2 golden tests, build has 0 warnings/errors and formatting verification passes. Hosted runs `39080099225`, `39080435271` and `39080954139` passed native batch MP4, recipe, documentation, advisory, build, format, tests, publish and checksum jobs on Windows, macOS and Linux; the latest run also verifies deterministic cross-platform progress completion. Approved brand-kit assets remain open.

**FT-1005 portable archive checkpoint (2026-10-18):** CI now packages each per-OS desktop publish tree as `frame-theory-<platform>-portable.zip` through `scripts/archive_package.ps1` before generating and verifying the SHA-256 manifest. The helper rejects directory output paths and archives placed inside the source tree, while CI opens each archive and asserts that `FrameTheory.Desktop.dll` is present. A local archive smoke verified the generated zip contents; hosted runs `39081424352` and `39082055065` passed archive creation, payload assertion, native media, tests, publish, manifest verification and artifact upload on Windows, macOS and Linux. These are portable validation archives, not installers; native FFmpeg bundling, signing/notarization and clean-host relaunch remain open.

**CI FFmpeg install checkpoint (2026-10-18):** Windows run `39082333029` correctly exposed a Chocolatey feed `404` while installing FFmpeg, so the workflow now falls back to the pinned WinGet `Gyan.FFmpeg` 9.0.2 package, locates its static executable, and preserves the existing FFmpeg/ffprobe evidence capture. Hosted run `39082588976` passed the repaired Windows path plus the full macOS/Linux matrix; no application behavior changed.

**FT-1003 runtime provenance checkpoint (2026-10-18):** `docs/SBOM.md` now records the pinned Windows validation package source, GPL status and installer SHA-256 without treating it as redistribution approval. Hosted run `39082884398` passed the documentation, restore/build, advisory, native-media, format, test, publish, portable-archive and checksum matrix on Windows, macOS and Linux.

**FT-901 accessibility checkpoint (2026-10-18):** The desktop window, workspace tab strip and individual Video/Photo/Color/AI tabs now expose stable Avalonia automation names while preserving the existing keyboard shortcuts and 1280x800 defaults. Local Foundation tests passed 13/13, the desktop Release build passed with 0 warnings/errors, formatting and diff checks passed, and hosted run `39085046211` passed the full Windows/macOS/Linux matrix. Interactive 1280x800/HiDPI layout and keyboard acceptance remain manual gates.

**Manual UI gate checkpoint (2026-10-18):** The Release desktop process launches from the managed `FrameTheory.Desktop.dll` with the expected `Frame Theory Studio AI` window title on this Windows host, but the available CUA inventory still returns zero native Windows applications and only browser tabs. FT-901..FT-904 and FT-1002 remain unverified; no interactive acceptance claim is made.

**Desktop shell smoke checkpoint (2026-10-18):** `scripts/smoke_desktop.ps1` now launches the real Release `FrameTheory.Desktop.dll`, waits for the `Frame Theory Studio AI` window title, fails on early exit/timeout and terminates the smoke process. Local Windows execution passed; hosted run `39083391933` passed the same Windows smoke plus the full macOS/Linux quality, native-media, publish, archive and checksum matrix. This proves startup reachability only, not interactive workspace acceptance.

**Portable relaunch checkpoint (2026-10-18):** CI now expands the generated Windows portable ZIP and runs the same real desktop-shell smoke against the extracted `FrameTheory.Desktop.dll`. Hosted run `39083944147` passed extraction and relaunch on Windows, alongside the complete macOS/Linux quality, native-media, archive and checksum matrix. Clean-host installation/relaunch on each target OS and interactive workflow acceptance remain open.

**Cross-platform portable smoke checkpoint (2026-10-18):** The first Unix extension run `39084169917` correctly exposed that `Process.MainWindowTitle` is not populated under macOS or Xvfb Linux; no application failure occurred. Commit `4228d48` now requires the title only on Windows and requires extracted Unix processes to remain alive for the startup window. Hosted run `39084466811` passed extracted-package startup on Windows, macOS and Linux plus the full native-media, test, publish, archive and checksum matrix. This is process/startup evidence, not interactive UI acceptance.

**FT-204 batch-import checkpoint (2026-10-18):** The desktop media browser now retains previously imported media when files arrive in later drag/drop or picker batches, preserves stable media IDs for the visible entries and avoids duplicate browser rows when the same source is imported again. The timeline and analysis dictionaries retain their existing idempotent behavior. Focused desktop import coverage passes 6/6; full local Release tests pass 163/2, build has 0 warnings/errors and formatting verification passes.

**FT-602 Files API trust-boundary checkpoint (2026-10-18):** Audio and visual Gemini Files API adapters now reject returned file URIs unless they are absolute HTTPS URIs on the official Gemini host; a valid returned file name is cleaned up before the typed failure is surfaced. Focused transcription coverage passes 12/12 and reference-analysis coverage passes 3/3; full local Release tests pass 164/2, build has 0 warnings/errors and formatting verification passes.

**FT-706 multimodal plan checkpoint (2026-10-18):** The AI workspace now separates local sample preparation from an explicit “Analyze approved samples with Gemini” action. The new client sends only consented bounded visual samples as inline PNG inputs, requests the machine-readable allowlisted plan schema, and passes the returned plan through the existing parser/semantic validator before proposal display; no timeline mutation occurs until per-action approval. Focused provider/desktop coverage passes 8/8; live key/provider-to-plan and larger-than-inline initial-plan acceptance remain open.

**FT-706 larger-scope checkpoint (2026-10-18):** Initial creative-direction plans now keep visual PNG inputs inline within the 20 MiB budget and switch larger approved scopes to the shared Gemini Files API transport. Returned upload sessions, file names and file URIs are constrained to the official HTTPS Gemini host; temporary `files/*` resources are deleted after success or failure. The regression fixture covers a sparse synthetic visual payload over 20 MiB, URI input, secret redaction and cleanup. Live provider/key acceptance remains open.

**CI checkpoint:** GitHub Actions run `39050810748` passed documentation/schema validation and .NET restore/build/format/test/publish/checksum jobs on Windows, macOS and Linux for `71b9bca`. Run `39051936860` also passed host-FFmpeg installation plus generated import/extracted-audio/repeated-seek assertions on Windows, macOS and Linux for `c37d112`. Run `39052895799` passed the same three-OS matrix with the NuGet advisory scan and published-artifact checksum verifier for `882c1c3`. Run `39053781388` passed the same three-OS matrix for `2858e59` after the FT-602 Files API implementation. Run `39054338193` passed the self-contained packaging/status checkpoint for `a4affa2`. Run `39054513784` passed the same three-OS matrix plus the new shell packaging syntax gate for `3808988`. Run `39055332326` passed the three-OS native-media matrix with the real multitrack timeline smoke for `b06a406`. Run `39056049489` passed the three-OS matrix with the generated NuGet dependency graph and FFmpeg/ffprobe version/build-configuration artifacts for `c2a67e2`. Run `39056614730` passed the three-OS native-media matrix with generated still-image conversion, photo import and PNG export for `dea377f`. Run `39057464353` passed the three-OS native-media matrix with preview proxy creation, ffprobe dimension checks and cache reuse for `d330d2e`; run `39057754819` repeated that evidence with a content SHA-256 fingerprint for `cc64553`. Run `39058607525` passed the hosted three-OS capability-probe test for `d4eb09f` after a first Linux-only attempt exposed the existing timing-sensitive export-queue test; no queue code changed. Run `39059471917` passed the three-OS build/test/publish/checksum matrix for `54dae94`, including the real timeline preview view-model test. Run `39059745492` passed the documentation/status checkpoint for `f5a05ac`. Run `39061301468` passed the timeline save/open implementation matrix for `9dff007`; run `39061566544` passed the associated status checkpoint for `16a1e3d`; run `39061896032` passed the final status correction for `c711903`; run `39062316915` passed the native-media matrix with real FFmpeg project save/reopen, media reimport and the existing export assertions for `2f1e16b`; and run `39062963831` passed the atomic timeline-restore safety fix for `964426f`. The Windows line-ending failure is resolved by `.gitattributes`; hosted packaging is still framework-dependent and unsigned. The local FT-903 trace tool passed on Windows at `f92c57f`, and the local native timeline/media smoke passed on 2026-10-17 as recorded above.

**Playback CI checkpoint:** Run `39063757381` passed the cancellable timeline playback implementation on Windows, macOS and Linux for `70805c9`; hosted packaging remains framework-dependent and unsigned.

**Photo persistence CI checkpoint:** Run `39065309000` passed photo project persistence, repository compatibility and the full quality matrix on Windows, macOS and Linux for `7d35174`; hosted packaging remains framework-dependent and unsigned.

**Native photo persistence CI checkpoint:** Run `39065964020` passed the generated-image FFmpeg photo project save/open smoke and its output assertion on Windows, macOS and Linux for `ad9cbd8`; hosted packaging remains framework-dependent and unsigned.

**Photo source-integrity CI checkpoint:** Run `39066595061` passed the fingerprint guard and full quality matrix on Windows, macOS and Linux for `1f0d285`; hosted packaging remains framework-dependent and unsigned.

**FT-904 capability retry validation:** Local Release unit tests pass 144/144 and the desktop Release build passes with 0 warnings and 0 errors for the capability-refresh UI increment. Hosted run `39067251130` passed documentation, restore, advisory, native-media, formatting, test, publish and checksum jobs on Windows, macOS and Linux for `3e003f4`; hosted packaging remains framework-dependent and unsigned.

**E09 test checkpoint:** Added focused evidence for every keyboard workspace shortcut, validated-plan JSON synchronization/clearing, repeatable seek traces, cancellable frame playback and credential-assignment redaction. Hosted native media smoke now covers all three OS runners; these tests strengthen FT-901/FT-902/FT-903/FT-904 but do not replace the required interactive 1280x800/HiDPI manual acceptance.

**Current status: PACKAGING MATRIX AND CI ARTIFACTS PARTIAL (2026-10-17).** FT-001 through FT-005, FT-006, FT-101 through FT-105, FT-201 through FT-205, FT-301 through FT-306, FT-401 through FT-405, FT-501 through FT-506, FT-601, FT-603 through FT-605, and FT-701 through FT-704 are implemented and verified on Windows x64 with .NET SDK 10.0.100 plus hosted native-media evidence for FT-006. FT-705 executes approved grade, clip, transition, title, caption, keyframe, audio, trusted-preset and Reel operations transactionally; FT-801 provides split, wipe and timed-reveal output across multiple video clips. FT-802 provides real 16:9, 1:1 and 9:16 MP4 presets plus Photo workspace PNG/JPEG import, non-destructive rotate/undo, preview and quality-controlled export; FT-803 adds imported user audio, amplitude/energy waveform-derived editable beat markers and explicit validated watermark selection; FT-804 adds a single-worker cancellable export queue and Video workspace cancel control; FT-706 exposes undo for the last AI transaction. FT-901 adds keyboard workspace/frame shortcuts, scroll-safe dense workspace layout, automation names for key controls and bounded dense/HiDPI ruler evidence. FT-902 surfaces synchronized selected-clip/playhead context, a real FFmpeg-decoded current-frame preview action and AI approval counts/status in the Video and AI panels; manual workspace acceptance remains open. FT-903 records real cold/warm frame-seek timing, working-set bytes, bounded cache metrics, repeatable p50/p95 seek and decode-throughput output, cancellation/cache policy and an optional content-fingerprint-keyed disk proxy; interactive proxy UI acceptance remains open. FT-904 adds local secret-redacting diagnostics, unhandled-failure capture, stdout/stderr dependency probing, nonzero-exit unknown-state reporting, actionable missing FFmpeg/ffprobe guidance and visible log location. FT-1001 adds an explicit source SHA-256 preservation assertion to the core unit/integration/golden gate. FT-1002 maps all UC-01..UC-06 to executable Windows smoke/test evidence and records the interactive/platform limits. FT-1003 records the actual restored NuGet/native dependency inventory and external FFmpeg redistribution blockers in `docs/SBOM.md`. FT-1004 extends the Windows/macOS/Linux CI matrix with desktop publish/test artifacts and SHA-256 manifests. FT-1005 provides reproducible framework-dependent and opt-in self-contained publish scripts plus a platform/release matrix; installers, native media bundling, signing and notarization remain open. FT-602 has a real consent-gated Gemini Interactions adapter, SHA-256 source provenance, strict word-timing rejection, reparse-source rejection, FFmpeg video-audio extraction, desktop caption wiring, larger-audio Files API upload/delete and real synthetic/fake-HTTP coverage; live key/media verification, local ASR and spoken-fixture acceptance remain open. Approved watermark licensing, provider retries and usage/redaction logs remain open. FT-006 uses the tested deterministic FFmpeg timeline path; MLT was not adopted, and interactive GUI parity is a separate gate. This is not a release.

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
- [x] FT-006 FFmpeg process adapter and deterministic timeline compositor; hosted Windows/macOS/Linux seek, multitrack, audio-mix, AI-edit and Reel smoke passed; MLT was not adopted
- [x] FT-101 normalized rational frame rates, frame ranges, stable media IDs and validated source metadata
- [x] FT-102 versioned project JSON, relative media references, legacy migration, atomic save/load and editable timeline-state persistence
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
- [~] FT-602 source-bound transcription contract, validated JSON fixture adapter, consent-gated Gemini REST provider, reparse-source rejection, FFmpeg video-audio extraction, desktop caption wiring and larger-audio Files API upload/delete are implemented; live provider/key/spoken-fixture verification and local ASR remain open
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
- [x] Timeline project save/open preserves editable timeline state, reimports relative media and remaps media IDs without mutating the current project on failed open
- [~] Timeline project missing-media detection, transactional relink and moved-project recovery are implemented and tested; manual cross-platform UI acceptance remains open
- [~] FT-706 consented multimodal initial-plan and natural-language revision flows, transactional AI undo, bounded Gemini transport, typed retry/timeout/cancellation failures, usage accounting and prompt/secret-redacted local history are implemented; live provider-to-plan and larger initial-plan acceptance remain open
- [~] FT-901 keyboard workspace/frame shortcuts, scroll-safe layout and automation labels are implemented; interactive 1280x800/HiDPI acceptance remains unverified
- [~] FT-902 synchronized selected-clip/playhead context, real current-frame FFmpeg preview, cancellable frame-accurate Play/Stop playback and AI approval summary are implemented; full manual workspace acceptance remains open
- [~] FT-903 bounded preview cache, cancellable rendering, an optional content-fingerprint-keyed disk proxy and a Video workspace proxy toggle are implemented; hosted native-media smoke validates proxy creation, dimensions and reuse on Windows, macOS and Linux, while interactive GUI acceptance remains open
- [~] FT-904 local redacted diagnostics, unhandled-failure capture, stdout/stderr dependency probing, nonzero-exit unknown-state reporting and actionable dependency guidance are implemented; cross-platform interactive failure UX acceptance remains open
- [x] FT-1001 core unit/golden/adversarial suite, caption provenance coverage and source SHA-256 preservation assertion pass on the hosted Windows/macOS/Linux matrix; broader manual/media journeys remain tracked by FT-1002
- [~] FT-1002 UC-01..UC-06 executable smoke/test matrix is documented with Windows evidence and hosted photo/video/native-media evidence; interactive and macOS/Linux acceptance remains open
- [~] FT-1003 dependency inventory and external FFmpeg/native asset assessment are documented; license/CVE clearance and redistribution evidence remain open
- [x] FT-1004 GitHub Actions verifies Windows/macOS/Linux restore/build/format/test/publish jobs and uploads per-OS SHA-256 manifests; optional signing/notarization is not part of this task claim
- [~] FT-1005 framework-dependent and opt-in self-contained Windows/macOS/Linux publishes plus checksummed portable validation archives are available; installers, native media bundling and signing remain open
- [~] FT-1006 V1 acceptance audit is recorded with 49 closed implementation issues and explicit remaining P0 blockers; no V1 tag is authorized
- [x] FT-1103 audio beat detection, source-timed kinetic word captions, proxy pipeline and editable speed-ramp curves are implemented with persistence, UI and native-export smoke evidence; hosted run `39069354787` passed on Windows, macOS and Linux
- [x] FT-1101 imported photo overlay composition with normal/multiply/screen blending, opacity, clipping, non-destructive undo, persisted brush masks and clone-style retouch, project persistence and hosted export/reopen smoke
- [~] FT-1102 mask-aware non-destructive photo grading, deterministic color scopes, translation tracking, decoded-frame motion reporting and persisted FFmpeg stabilization are implemented with Photo/Video workspace controls and hosted graph/build/native evidence; visual-quality acceptance remains open
- [~] FT-1104 offline reference matching, reusable style-library UI, consented visual Gemini analyzer with bounded inline/Files API inputs and waveform-grounded reviewed rough-cut apply/export are implemented with local and hosted evidence; live-provider and reference-grade semantic acceptance remain open
- [~] FT-1105 multi-target social timeline batch export with cancellation, progress, exact-canvas fitting and validated AI recipe persistence is implemented with hosted evidence; approved brand-kit assets remain open
- [x] FT-401 FFmpeg-backed PNG/JPEG photo import, PPM preview and bounded canvas fit/100%/zoom/pan state
- [x] FT-402 non-destructive PPM crop, 90° rotation, nearest-neighbor resize and undo-last operation stack
- [x] FT-403 immutable photo adjustment layers with ordering, inspector state and undo/redo history
- [x] FT-404 RGBA-preserving PNG and JPEG photo export with source metadata mapping, quality controls and atomic verification
- [x] FT-405 photo project autosave recovery, missing-media relink and corrupt-input UI status paths

## Next tasks
1. Exercise timeline playback/save/open/relink through the Avalonia UI on Windows, macOS and Linux, including moved-project recovery. The 2026-10-18 CUA inventory still exposes browsers but no native Windows app surface, so this manual gate remains unverified rather than claimed complete.
2. Obtain approved live Gemini spoken-fixture/provider evidence and decide whether a local ASR adapter is required for FT-602.
3. Finish manual/cross-platform FT-901..FT-904 acceptance and hosted FT-1001..FT-1005 gates.
4. Resolve approved logo, licensing/native redistribution, installer/signing, and live Gemini prerequisites before closing their GitHub tasks.
5. Keep MLT deferred unless interactive playback parity or a release licensing decision requires a new native engine spike.
6. Keep FT-1104 open for live-provider and reference-grade semantic acceptance; do not close it on offline or fake-HTTP evidence alone.
7. Continue FT-1105 only when the approved official asset source is available; do not invent branding assets.
8. Keep FT-1106 deferred until an approved segmentation/enhancement model, license terms and target hardware baseline are supplied; do not add a fake or unlicensed model.

## Latest execution record
```text
Date/time: 2026-10-18
Task / Epic: FT-1005 / E10 cross-platform portable package relaunch
Git SHA: 4228d48 (Unix-aware extracted archive smoke)
Files changed: scripts/smoke_desktop.ps1, .github/workflows/ci.yml
Actual behavior and UI path: After CI publishes and archives the desktop tree, each runner expands its exact portable ZIP and launches its extracted `FrameTheory.Desktop.dll` through the installed .NET host. Windows requires the `Frame Theory Studio AI` title; macOS and Linux require the process to remain alive because their process metadata does not expose the native title consistently. All smoke paths reject early exit and clean up the process.
Commands executed + OS: Windows 11 x64 local strict-title smoke, then hosted Windows/macOS/Linux validation with Xvfb on Linux; `git diff --check` passed.
Test outputs / exit codes: Local desktop smoke passed with `Desktop shell window detected: Frame Theory Studio AI`; hosted run `39084466811` passed documentation, desktop shell smoke, extracted portable relaunch on all three OSes, native batch MP4, recipe, advisory, build, format, tests, publish, archive payload assertion, checksum and upload validation.
Media fixtures and verification: The relaunch smoke uses no media, FFmpeg, Gemini key or network upload. The same hosted run exercised the existing synthetic H.264/AAC native-media fixtures; no user media, API key or proprietary binary entered Git.
Known limitations / blockers: This verifies framework-dependent portable startup only. Clean-host installation/uninstallation, installers, native redistribution, signing, approved branding, live Gemini evidence and interactive workspace acceptance remain open.
Next unblocked task: Continue clean-host/package acceptance on target hardware when available; do not close FT-1005 or claim V1 from portable startup alone.
```

**FT-1101 composition checkpoint (2026-10-18):** The Photo workspace exposes Import overlay, Clear overlays, brush-mask and clone-retouch controls with blend-mode and opacity selection. Overlay source files remain local, are never mutated, render through the same `PpmRaster` export path as the base photo, and are saved/reopened as bounded relative project media references with SHA-256 fingerprints. Focused composition/mask/retouch/persistence coverage passes 10/10; hosted native FFmpeg export/reopen reports one overlay, one mask and one retouch stroke. FT-1101 implementation acceptance is complete; manual UI, branding, licensing and installer gates remain separate.

**FT-1102 stabilization checkpoint (2026-10-18):** Deterministic scopes and bounded translation tracking are present in the media/color foundations. Timeline stabilization validates a 1–64 logical search radius, persists through project JSON and AI rollback state, exposes Apply/Clear controls in the Video workspace and emits valid FFmpeg `deshake` filters for standard and speed-ramp segments by normalizing the logical radius to FFmpeg's required 16-pixel increments. The Video workspace also decodes up to 12 local frames, reports measured translation/compensation and restores the playhead after analysis. Photo adjustment layers now accept persisted brush masks and apply exposure/grade changes only inside the painted coverage. Local focused/full tests and hosted Windows/macOS/Linux quality gates, including native stabilization export and reopen smoke, pass; visual-quality acceptance remains open.

**FT-1102 execution record (2026-10-18):** Git SHAs `bfe29b9`, `519208d`, `5891f36`, `53175ff` and `b9add3b`. Focused timeline tests passed 16/16, motion UI passed 7/7 and photo grading/persistence passed 11/11; the full solution passed 183 unit and 2 golden tests; the smoke tool built with zero warnings and formatting verification passed. Hosted run `39079254370` passed documentation, restore, advisory, build, native-media, format, tests, publish and checksum jobs on Windows, macOS and Linux. The hosted native smoke saved/reopened a stabilized project and produced a real exported MP4 through the normalized FFmpeg filter path. No user media, API key or binary entered Git. Next: complete manual visual-quality review or continue remaining P0 manual/external release gates without claiming FT-1102 complete.

## Previous execution record
```text
Date/time: 2026-10-18
Task / Epic: FT-602 / FT-1104 / Gemini Files API trust boundary
Git SHA: b1725a7
Files changed: src/FrameTheory.Captions/Transcription.cs, src/FrameTheory.AI/GeminiReferenceAnalysis.cs, tests/FrameTheory.UnitTests/TranscriptionTests.cs
Actual behavior and UI/headless path: Audio and visual Files API responses are accepted only when the returned URI is absolute HTTPS on `generativelanguage.googleapis.com`. If the provider returns a valid `files/*` name with an untrusted URI, the adapter attempts cleanup and returns a typed failure without sending that URI to an interaction.
Commands executed + OS: Windows 11 x64 with .NET SDK 10.0.402; focused transcription/reference tests, full Release tests, Release build and formatting verification.
Test outputs / exit codes: Focused transcription tests passed 12/12; focused reference tests passed 3/3; full tests passed 164 unit and 2 golden tests; Release build passed with 0 warnings/errors; formatting verification passed. Hosted quality gate run `39072895456` passed documentation/schema validation, advisory scan, restore/build, native media smoke, formatting, tests, publish and checksum verification on Windows, macOS and Linux.
Media fixtures and verification: Sparse synthetic long-audio and generated reference fixtures exercised fake HTTP upload, rejection, cleanup and no-key-in-body invariants; no user media, API key or binary entered Git.
Known limitations / blockers: Live Gemini provider/key and spoken/reference fixture acceptance remain open; manual Avalonia UI, branding, licensing and installer evidence remain open.
Next unblocked task: Continue remaining manual/platform and external-input gates; live Gemini, approved branding, licensing/native redistribution, installers/signing and native UI evidence remain open.
```

## Previous execution record
```text
Date/time: 2026-10-18
Task / Epic: FT-402 / photo source integrity
Git SHA: 1f0d285
Files changed: src/FrameTheory.Desktop/PhotoEditorViewModel.cs, tests/FrameTheory.UnitTests/PhotoProjectPersistenceTests.cs
Actual behavior and UI path: Photo project open verifies the saved SHA-256 source fingerprint before importing and preserves the current editor state when source bytes change.
Commands executed + OS: Focused photo persistence tests, full unit/golden tests, Release build and formatting verification on Windows 11 x64; hosted quality gate run 39066595061 passed on Windows, macOS and Linux.
Test outputs / exit codes: Focused photo persistence tests passed 4/4; full unit tests passed 144/144; golden tests passed 2/2; Release build passed with 0 warnings/errors; hosted run 39066595061 passed all jobs.
Media fixtures and verification: Changed-source coverage uses temporary local bytes; no user media, API key, generated fixture or binary entered Git.
Known limitations / blockers: Interactive Photo workspace close/reopen and cross-platform GUI acceptance remain open.
Next unblocked task: Continue the native-media journey matrix while manual UI and external-input gates remain visible.
```

## Previous execution record
```text
Date/time: 2026-10-18
Task / Epic: FT-402 / native photo project smoke
Git SHA: ad9cbd8
Files changed: tools/FrameTheory.PhotoExportSmoke/Program.cs, tools/FrameTheory.PhotoExportSmoke/FrameTheory.PhotoExportSmoke.csproj, .github/workflows/ci.yml
Actual behavior and UI path: The native photo export smoke imports a generated PNG through FFmpeg, applies a non-destructive rotation through PhotoEditorViewModel, saves and reopens a real `.ftstudio` project, compares restored rendered bytes and exports a verified PNG. CI asserts the `photo-project-roundtrip=` receipt.
Commands executed + OS: Local Release tool build, generated fixture/FFmpeg PNG smoke and hosted quality gate run 39065964020 on Windows, macOS and Linux.
Test outputs / exit codes: Local smoke exited 0 and reported `format=Png; size=8x4` plus `photo-project-roundtrip=...; operations=1`; hosted run 39065964020 passed all jobs.
Media fixtures and verification: The local smoke used a generated deterministic chart and FFmpeg/ffprobe 9.0.2; outputs remained under `%TEMP%` and no user media, API key or binary entered Git.
Known limitations / blockers: Interactive Photo workspace close/reopen and cross-platform GUI acceptance remain open; the project still requires the source media to remain within its project directory.
Next unblocked task: Continue the native-media journey matrix while manual UI and external-input gates remain visible.
```

## Previous execution record
```text
Date/time: 2026-10-18
Task / Epic: FT-402 / editable photo project persistence
Git SHA: 7d35174
Files changed: src/FrameTheory.Desktop/MainWindow.cs, src/FrameTheory.Desktop/MainWindowViewModel.cs, src/FrameTheory.Desktop/PhotoEditorViewModel.cs, src/FrameTheory.Desktop/PhotoProjectStateJson.cs, src/FrameTheory.Domain/Project.cs, src/FrameTheory.Storage/JsonProjectRepository.cs, tests/FrameTheory.UnitTests/PhotoProjectPersistenceTests.cs
Actual behavior and UI path: The Photo workspace exposes Open photo project and Save photo project. Save stores non-destructive operations and adjustment layers in the existing atomic project JSON with a relative photo reference and SHA-256 source fingerprint; open reimports the photo and restores the editable state without mutating the current photo on failure.
Commands executed + OS: Focused photo project tests, full unit tests, golden tests, Release build and formatting verification on Windows 11 x64; hosted quality gate run 39065309000 passed on Windows, macOS and Linux.
Test outputs / exit codes: Focused photo project tests passed 3/3; full unit tests passed 143/143; golden tests passed 2/2; Release build passed with 0 warnings/errors; hosted run 39065309000 passed all jobs.
Media fixtures and verification: The tests use temporary local photo bytes and deterministic import/process seams; no user media, API key, generated fixture or binary entered Git.
Known limitations / blockers: Interactive Photo workspace close/reopen and cross-platform GUI acceptance remain open; sources must remain within the project directory under the existing portable project contract.
Next unblocked task: Exercise photo and timeline project recovery through the native Avalonia UI when a usable native app surface is available.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-903 / Video workspace preview proxy wiring
Git SHA: 389e504
Files changed: src/FrameTheory.Desktop/MainWindow.cs, src/FrameTheory.Desktop/MainWindowViewModel.cs, tests/FrameTheory.UnitTests/DesktopImportTests.cs
Actual behavior and UI path: The Video workspace exposes Use bounded preview proxy. When enabled, current-frame preview and playback use the existing bounded FFmpeg proxy service, reuse a cache entry on subsequent frames, preserve the original source and report unavailable or canceled proxy work without pretending preview succeeded.
Commands executed + OS: Focused DesktopImportTests on Windows 11 x64 after the clip-dimension edge-case correction.
Test outputs / exit codes: Focused desktop tests passed 5/5; proxy creation ran once for two previews and the decoder received the same cached proxy URI.
Media fixtures and verification: The test uses a temporary local media file, a deterministic process runner and a bounded temporary cache; no user media, API key, generated fixture or binary entered Git.
Known limitations / blockers: Hosted three-OS validation and interactive Avalonia acceptance are pending; the proxy remains opt-in and does not replace the source media.
Next unblocked task: Push and verify the proxy UI wiring on Windows, macOS and Linux through the hosted quality matrix.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-902 / cancellable timeline playback
Git SHA: 70805c9
Files changed: src/FrameTheory.Desktop/MainWindow.cs, src/FrameTheory.Desktop/MainWindowViewModel.cs, tests/FrameTheory.UnitTests/DesktopImportTests.cs
Actual behavior and UI path: The Video workspace exposes Play timeline and Stop playback actions. Playback decodes each exact timeline frame through the shared FFmpeg preview path, temporarily disables snapping to avoid boundary stalls, restores the previous snapping setting and reports completion or cancellation.
Commands executed + OS: Focused DesktopImportTests, full unit tests, golden tests and Release build on Windows 11 x64; hosted quality gate run 39063757381 passed on Windows, macOS and Linux.
Test outputs / exit codes: Focused playback tests passed 4/4; full unit tests passed 139/139; golden tests passed 2/2; Release build passed with 0 warnings/errors; hosted run 39063757381 passed all jobs.
Media fixtures and verification: The focused playback tests use a deterministic three-frame importer and a cancellation-blocking decoder; no user media, API key, generated fixture or binary entered Git.
Known limitations / blockers: This is executable headless/view-model and hosted build evidence; interactive GUI acceptance, 1280x800/HiDPI layout, moved-project/missing-media recovery and external P0 release prerequisites remain open.
Next unblocked task: Exercise playback and project recovery through the native Avalonia UI when a usable native app surface is available.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-102 / project timeline persistence
Git SHA: 9dff007
Files changed: src/FrameTheory.Desktop/MainWindow.cs, src/FrameTheory.Desktop/MainWindowViewModel.cs, src/FrameTheory.Desktop/TimelineEditorViewModel.cs, src/FrameTheory.Desktop/TimelineProjectState.cs, src/FrameTheory.Domain/Project.cs, src/FrameTheory.Storage/JsonProjectRepository.cs, tests/FrameTheory.UnitTests/TimelineProjectPersistenceTests.cs
Actual behavior and UI path: The Video workspace now provides Save timeline project and Open timeline project actions. Saved state includes editable clips, audio settings, keyframes, transitions, grades, titles, captions, watermark, beat markers, playhead and view state. Open validates bounded JSON, reimports relative local media and remaps media IDs before restoring the timeline; failed or canceled imports leave the current project loaded.
Commands executed + OS: .NET SDK 10.0.402 Release build, focused persistence tests, 136 unit tests, 2 golden tests and diff checks on Windows 11 x64; hosted quality gate run 39061301468 passed on Windows, macOS and Linux.
Test outputs / exit codes: Focused persistence tests passed 3/3; full unit tests passed 136/136; golden tests passed 2/2; Release build passed with 0 warnings/errors.
Media fixtures and verification: The round-trip test uses a temporary local media file and a deterministic importer; no user media, API key, generated fixture or binary entered Git.
Known limitations / blockers: Project media references must remain inside the project directory because the existing portable project contract rejects path traversal; font and watermark paths are preserved as local paths and still require relinking if moved. Interactive cross-platform GUI acceptance remains open.
Next unblocked task: Exercise manual save/open and missing-media recovery when a native UI surface is available.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-102 / atomic timeline restore safety
Git SHA: 964426f
Files changed: src/FrameTheory.Desktop/TimelineEditorViewModel.cs, tests/FrameTheory.UnitTests/TimelineProjectPersistenceTests.cs
Actual behavior and UI path: Saved timeline state is fully validated and converted to local domain objects before replacing the active editor state. A malformed grade now reports an error without partially clearing clips or settings.
Commands executed + OS: Focused persistence tests, full unit/golden tests, Release build and native smoke on Windows 11 x64; hosted quality gate run 39062963831 on Windows, macOS and Linux.
Test outputs / exit codes: Focused persistence tests passed 4/4; full unit tests passed 137/137; golden tests passed 2/2; Release build passed with 0 warnings/errors; hosted run 39062963831 passed all jobs.
Media fixtures and verification: Existing temporary synthetic native smoke clips only; no user media, API key, generated fixture or binary entered Git.
Known limitations / blockers: Interactive GUI acceptance, moved-project/missing-media recovery and external P0 release prerequisites remain open.
Next unblocked task: Continue manual/platform and external-input gates without claiming V1 completion.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-1002 / native project save-open journey
Git SHA: 2f1e16b
Files changed: tools/FrameTheory.TimelineExportSmoke/Program.cs, .github/workflows/ci.yml
Actual behavior and UI path: The existing native-media smoke now constructs the desktop view model with the real FFmpeg importer, imports two temporary H.264/AAC clips, saves a `.ftstudio` project beside them, opens it through the real project path, verifies two reimported media items and a video track, then continues composite, queue-retry, AI and Reel exports.
Commands executed + OS: Local Release build, full unit/golden suite and native smoke on Windows 11 x64 with FFmpeg 9.0.2; hosted quality gate run 39062316915 on Windows, macOS and Linux.
Test outputs / exit codes: Local unit tests 136/136 passed; golden tests 2/2 passed; build exit 0 with 0 warnings/errors; smoke exit 0 and reported `project-roundtrip=...; media=2`; hosted run 39062316915 passed all jobs.
Media fixtures and verification: Synthetic H.264/AAC clips and all project/MP4 outputs were written under `%TEMP%` locally or CI artifacts; no user media, API key or binary entered Git.
Known limitations / blockers: This remains headless native-media evidence; interactive save/open, moved-project recovery, missing-media relink and GUI acceptance on all platforms remain open.
Next unblocked task: Exercise the same project journey through the native Avalonia UI when a usable native app surface is available.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-006 / FT-1002 native Windows media smoke
Git SHA: f5a05ac (evidence-only checkpoint)
Files changed: none; all media and outputs were written under %TEMP% and excluded from Git
Actual behavior and UI path: The built application libraries and existing smoke tools were exercised against temporary synthetic H.264/AAC clips. Import produced a thumbnail and 128 waveform peaks, transcription preparation produced a one-second audio WAV, the preview proxy created and reused an 8x4 file, local analysis honored uploadConsent=false, and the real timeline exporter produced composite, retry, AI and before/after Reel MP4 outputs.
Commands executed + OS: .NET SDK 10.0.402 Release build, 133 unit tests, 2 golden tests, FFmpeg 9.0.2 synthetic fixture generation, MediaImportSmoke, and TimelineExportSmoke on Windows 11 x64.
Test outputs / exit codes: Build exit 0; unit tests 133/133 passed; golden tests 2/2 passed; media import exit 0; timeline export exit 0; ffprobe verified video/audio streams, 16x8 dimensions, BT.709 color and 2-second duration.
Media fixtures and verification: Synthetic testsrc/color video and sine audio only; no user media, API key, native binary or generated fixture entered Git.
Known limitations / blockers: This is headless native-media evidence, not interactive GUI acceptance. The Avalonia DLL remained running locally, but the CUA inventory exposed no native app surface; manual 1280x800/HiDPI, playback, close/reopen, and all macOS/Linux GUI gates remain open.
Next unblocked task: Continue external-input and manual/platform release gates without closing tasks whose acceptance evidence is still missing.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-902 / E09 timeline preview
Git SHA: 54dae94
Files changed: src/FrameTheory.Desktop/MainWindow.cs, src/FrameTheory.Desktop/MainWindowViewModel.cs, tests/FrameTheory.UnitTests/DesktopImportTests.cs
Actual behavior and UI path: The Video workspace now exposes “Preview current frame”. It resolves the playhead’s active video sample, maps it to the imported source and source frame, decodes a real PPM frame through the shared FFmpeg decoder, and displays it with status/error text. The existing bounded decoder cache and cancellation path are reused.
Commands executed + OS: Local Release build, focused preview test, full unit/golden tests, format and diff checks on Windows 11 x64; hosted GitHub Actions run `39059471917` on Windows, macOS and Linux.
Test outputs / exit codes: Local build passed with 0 warnings/errors; focused DesktopImportTests passed 2 tests; full suite passed 133 unit tests and 2 golden tests. Hosted run `39059471917` passed documentation, restore/build, native-media, formatting, tests, publish and checksum jobs on all three OS runners.
Media fixtures and verification: The focused test uses a deterministic synthetic video metadata/import path and a decoder stub; hosted CI compiles and publishes the actual Avalonia application but does not claim interactive GUI behavior or user media upload.
Known limitations / blockers: Preview is manually triggered and does not yet provide continuous playback, composited overlays or platform GUI acceptance; FT-901/FT-902/FT-1002 manual gates remain open.
Next unblocked task: Continue manual/cross-platform UX evidence and release gates, or obtain the external live Gemini/logo/legal inputs needed by FT-602, FT-706 and FT-803.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-904 / E09 failure and dependency UX
Git SHA: d4eb09f
Files changed: src/FrameTheory.Application/Capabilities.cs, src/FrameTheory.Platform/SystemCapabilityDetector.cs, tests/FrameTheory.UnitTests/FoundationTests.cs
Actual behavior and UI path: Capability detection now preserves logical FFmpeg/ffprobe labels when custom executables are used, reads version/error text from both output streams, reports nonzero probes as Unknown instead of Available, and includes bounded actionable detail in the capability summary. Missing executables remain Missing with install guidance.
Commands executed + OS: Local Release build, focused foundation tests, full unit/golden tests, format and diff checks on Windows 11 x64; hosted GitHub Actions run `39058607525` on Windows, macOS and Linux.
Test outputs / exit codes: Local build passed with 0 warnings/errors; 12 focused foundation tests, 132 unit tests and 2 golden tests passed. The first Linux-only hosted attempt failed in the pre-existing timing-sensitive `ExportQueueTests.QueueRunsJobsInOrderAndSupportsCancellation`; the failed job rerun passed and all three OS jobs are green.
Media fixtures and verification: No user media or generated binary changed; hosted native-media and packaging jobs remained green on Windows, macOS and Linux.
Known limitations / blockers: Interactive failure-message acceptance and full cross-platform manual UX remain unverified; this increment does not close FT-904.
Next unblocked task: Continue manual/cross-platform FT-901..FT-904 and release gates, or obtain the external live Gemini/logo/legal inputs needed by FT-602, FT-706 and FT-803.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-903 / E09 preview proxy path
Git SHA: cc64553
Files changed: tools/FrameTheory.TimelineExportSmoke/Program.cs, src/FrameTheory.Media/PreviewProxy.cs, tests/FrameTheory.UnitTests/PreviewProxyTests.cs, .github/workflows/ci.yml
Actual behavior and UI path: The timeline smoke computes a source SHA-256, creates a bounded FFmpeg H.264 preview proxy at 8x8 maximum dimensions, verifies the proxy with ffprobe, and requests the same key again to prove reuse. The service rejects remote/reparse-point inputs, writes atomically, honors cancellation and evicts old entries within its cache budget.
Commands executed + OS: Local Release build, focused proxy tests and format/diff checks on Windows 11 x64; hosted GitHub Actions run `39057754819` on Windows, macOS and Linux.
Test outputs / exit codes: Local build passed with 0 warnings/errors; 3 focused proxy tests passed; hosted run `39057754819` passed all documentation, build, native-media, formatting, test, publish and checksum jobs on all three OS runners.
Media fixtures and verification: Hosted native smoke generated synthetic H.264/AAC media, created the proxy with FFmpeg and reported the proxy line plus verified dimensions/reuse; no user media or generated binary entered Git.
Known limitations / blockers: Proxy service is headless and not yet wired into interactive preview UI; manual 1280x800/HiDPI, cross-platform UI and release packaging gates remain open.
Next unblocked task: Continue FT-602 live provider evidence and FT-901..FT-904 / FT-1002..FT-1006 release acceptance.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-1002 / E10 photo journey smoke
Git SHA: dea377f
Files changed: .github/workflows/ci.yml
Actual behavior and UI path: The native-media CI job converts the deterministic generated color chart to a real PNG, imports it through `PhotoImporter`, renders a preview, applies the existing exposure layer and exports a real PNG through `PhotoExporter` with ffprobe verification.
Commands executed + OS: Hosted GitHub Actions run `39056614730` on Windows, macOS and Linux; FFmpeg conversion plus `FrameTheory.PhotoImportSmoke` and `FrameTheory.PhotoExportSmoke`.
Test outputs / exit codes: Run `39056614730` passed all three OS jobs. Each runner reported `kind=Image; size=8x4` and `format=Png; size=8x4` with a verified output path. Prior run `39056387134` correctly failed because the raw PPM fixture was identified as non-still media; `dea377f` converted it to PNG before import.
Media fixtures and verification: The chart, PNG source and PNG export were generated outside Git; no user media was used and source-preserving photo tests remain unchanged.
Known limitations / blockers: This is native headless media evidence, not Photo workspace close/reopen or interactive GUI evidence on any OS.
Next unblocked task: Continue FT-602 live provider evidence and FT-901..FT-904 / FT-1002..FT-1006 release acceptance.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-1003 / E10 dependency evidence
Git SHA: c2a67e2
Files changed: scripts/generate_sbom.ps1, .github/workflows/ci.yml, docs/SBOM.md
Actual behavior and UI path: The release workflow now emits a complete restored NuGet dependency graph and captures the installed FFmpeg/ffprobe version and build configuration for every OS runner. The inventory script validates that JSON contains restored projects and fails on a failed `dotnet list` command.
Commands executed + OS: Windows 11 x64 local inventory generation using .NET SDK 10.0.100; hosted GitHub Actions run `39056049489` on Windows, macOS and Linux.
Test outputs / exit codes: Local script exited 0 and parsed 20 projects. Hosted run `39056049489` passed docs/schema, advisory scan, dependency inventory, native FFmpeg media smoke, formatting, tests, publish and checksums on all three runners.
Media fixtures and verification: Hosted native artifacts include generated synthetic H.264/AAC media and per-run FFmpeg/ffprobe evidence; no user media or proprietary binary entered Git.
Known limitations / blockers: The artifacts identify validation dependencies but do not clear package licenses, FFmpeg GPL/LGPL or codec patents, native redistribution, application licensing or signing.
Next unblocked task: Continue FT-602 live provider evidence and FT-901..FT-904 / FT-1002..FT-1006 release acceptance.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-006 / E00 timeline engine spike
Git SHA: 2f8042b
Files changed: .github/workflows/ci.yml, docs/adr/0004-ffmpeg-spike.md, docs/DECISIONS.md, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: CI now runs the existing real FFmpeg timeline smoke after installing host FFmpeg. It imports two distinct local source paths, exercises the seek/transport path, builds two video layers and two audio layers with transition, animation, gain and fades, verifies a real timeline MP4, retries a failed export, executes approved AI edits and exports an editable before/after Reel. The selected path remains the existing FFmpeg decoder/compositor; no MLT dependency was added.
Commands executed + OS: Hosted GitHub Actions run `39055332326` on Windows, macOS and Linux; native FFmpeg/ffprobe installation, generated H.264/AAC fixture creation and `FrameTheory.TimelineExportSmoke`.
Test outputs / exit codes: Run `39055332326` passed documentation/schema, restore/build, native import/extraction/seek smoke, timeline export, queue retry, AI export, Reel export, formatting, tests, publish and checksum verification on all three OS runners. Earlier run `39055092338` failed because both smoke arguments used the same URI and the editor correctly deduplicated the media; `b06a406` changed the second fixture path and the rerun passed.
Media fixtures and verification: Only generated 16x8 H.264/AAC media was used. Timeline, queue-retry, AI and Reel outputs were real 16x8 BT.709 MP4s and were ffprobe-verified; no user media or network service was used.
Known limitations / blockers: MLT was not installed or adopted; interactive GUI/playback parity, native FFmpeg redistribution, licensing, installers and signing remain separate release gates.
Next unblocked task: Continue FT-602 live provider evidence and FT-901..FT-904 / FT-1002..FT-1006 release acceptance.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-1005 / E10 packaging validation
Git SHA: a4affa2 (packaging), 3808988 (CI syntax gate)
Files changed: scripts/package.ps1, scripts/package.sh, docs/RELEASE.md, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: The PowerShell packaging script now accepts `-SelfContained`; the shell packaging script accepts a validated `true` or `false` third argument. Both pass the value to `dotnet publish` and include the existing SHA-256 manifest. This produces portable runtime-bundled validation output, not an installer and not a native FFmpeg bundle.
Commands executed + OS: Windows 11 x64; self-contained `win-x64` package script, package checksum verifier, Release build, Release tests, format verification and diff check using .NET SDK 10.0.100.
Test outputs / exit codes: Self-contained publish exited 0 and produced `FrameTheory.Desktop.exe`; checksum verification reported 241 files verified; build passed with 0 warnings/errors; unit tests passed 128; golden tests passed 2; formatting and diff checks passed. Bash syntax check could not run locally because this Windows host exposes a WSL shim without `/bin/bash`; hosted run `39054513784` passed it on all three OS runners.
Media fixtures and verification: No media or user data entered the package; the package smoke was limited to publish output and checksums.
Known limitations / blockers: Installers, native FFmpeg bundling, legal/codec clearance, signing/notarization and matching-host relaunch evidence remain open. Hosted packaging validation is run `39054338193`; the follow-up syntax-gate run is `39054513784`.
Next unblocked task: Continue FT-901..FT-904 manual acceptance and FT-1002..FT-1006 release gates.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-602 / E06 transcription provider
Git SHA: bef8d46
Files changed: src/FrameTheory.Captions/Transcription.cs, src/FrameTheory.Media/AudioSourcePreparer.cs, tests/FrameTheory.UnitTests/TranscriptionTests.cs, tests/FrameTheory.UnitTests/AudioSourcePreparerTests.cs, docs/GEMINI_PROVIDER.md, docs/AI_EDIT_PLAN.md, docs/SECURITY_PRIVACY.md, docs/V1_ACCEPTANCE_AUDIT.md, docs/adr/0005-gemini-transcription-audio-preparation.md, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: The consent-gated Video workspace transcription path now keeps audio at or below 20 MiB inline and uses the official resumable Gemini Files API for larger approved local audio. It validates the returned upload host, sends only the returned audio URI to the transcription interaction, deletes the temporary `files/*` resource after success or failure, preserves the source SHA-256 and imports only source-timed verified words. Video extraction is bounded to the Gemini 2 GiB Files API per-file limit and remains owned temporary media.
Commands executed + OS: Windows 11 x64; focused transcription/audio-preparation tests, full `dotnet build FrameTheory.sln --no-restore --configuration Release --warnaserror`, full `dotnet test FrameTheory.sln --no-build --configuration Release`, `dotnet format FrameTheory.sln --no-restore --verify-no-changes --verbosity minimal`, and `git diff --check` using .NET SDK 10.0.100.
Test outputs / exit codes: Focused provider/preparer suite passed 17 tests. Full local gate passed 128 unit tests, 2 golden tests, 0 build warnings/errors, clean formatting and clean diff. Fake-HTTP coverage verified resumable upload initialization, URI-only interaction input, remote deletion and the 2 GiB extraction boundary. Hosted run `39053781388` passed documentation/schema validation, advisory scan, restore/build, native media smoke, formatting, tests, publish and checksum verification on Windows, macOS and Linux.
Media fixtures and verification: The long-audio test used a sparse 20 MiB-plus-one-byte WAV source; no user media or API key entered Git. Existing synthetic H.264/AAC extraction coverage remains unchanged.
Known limitations / blockers: No live Gemini key or spoken fixture was available, so live provider/key acceptance remains unverified. Local ASR fallback, MLT/second-OS playback, approved watermark bytes, interactive GUI acceptance, installers/signing and legal/native redistribution review remain open.
Next unblocked task: Obtain approved live Gemini spoken-fixture evidence when available, otherwise continue FT-901..FT-904 manual acceptance and FT-1002..FT-1006 release gates.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-1003 / E10 supply-chain evidence
Git SHA: 754312c
Files changed: scripts/check_nuget_vulnerabilities.ps1, .github/workflows/ci.yml, docs/SBOM.md, docs/IMPLEMENTATION_STATUS.md
Actual behavior and UI path: The release workflow now runs a reproducible transitive NuGet advisory scan after restore, stores the JSON report as a validation artifact, and fails when vulnerability records are present. This is release automation rather than a desktop UI path.
Commands executed + OS: Windows 11 x64; `pwsh ./scripts/check_nuget_vulnerabilities.ps1 -OutputPath <temp-file>` and `git diff --check`.
Test outputs / exit codes: The scan reported no NuGet vulnerabilities and exited 0; the report was written and removed from the temporary directory; diff check exited 0. Hosted run `39052895799` passed documentation/schema validation, the advisory scan, restore/build, native media smoke, formatting, tests, publish, manifest creation and manifest verification on Windows, macOS and Linux.
Media fixtures and verification: Not applicable; no media or application binaries changed.
Known limitations / blockers: A clean NuGet advisory result does not clear package licenses, FFmpeg codec patents, native redistribution, application licensing, signing or installer requirements. The hosted workflow remains framework-dependent and unsigned.
Next unblocked task: Continue FT-1003 dependency/license evidence and FT-1005 packaging validation without claiming legal or installer completion.
```

## Previous execution record
```text
Date/time: 2026-10-17
Task / Epic: FT-602 / FT-903 / FT-904
Git SHA: 0f5c7f6
Files changed: src/FrameTheory.Media/AudioSourcePreparer.cs, src/FrameTheory.Desktop/MainWindowViewModel.cs, src/FrameTheory.Desktop/MainWindow.cs, src/FrameTheory.Application/Diagnostics.cs, tests/FrameTheory.UnitTests/AudioSourcePreparerTests.cs, tests/FrameTheory.UnitTests/DesktopTranscriptionTests.cs, tests/FrameTheory.UnitTests/DiagnosticsTests.cs, tests/FrameTheory.UnitTests/FoundationTests.cs, tools/FrameTheory.MediaImportSmoke/Program.cs, .github/workflows/ci.yml, docs/DEVELOPMENT.md, docs/TEST_STRATEGY.md, docs/SECURITY_PRIVACY.md, docs/AI_EDIT_PLAN.md, docs/adr/0005-gemini-transcription-audio-preparation.md
Actual behavior and UI path: The Video workspace transcription action now prepares approved local audio or extracts the first audio track from an approved local video with FFmpeg, uploads only the prepared audio bytes, restores the original media URI on the verified transcript, rejects unsupported/reparse/oversized sources, and deletes owned temporary WAV output. The picker exposes every supported audio/video container. Diagnostics redact Gemini tokens plus API-key/authorization assignments. CI installs host FFmpeg on each OS, generates a synthetic H.264/AAC clip, verifies import and extracted audio, and asserts repeated seek output.
Commands executed + OS: Windows 11 x64; full `dotnet format`, Release build with warnings as errors, full solution tests, `git diff --check`, focused extraction/diagnostics tests, the media-import smoke and the frame-decode smoke using .NET SDK 10.0.100 and FFmpeg/ffprobe 9.0.2.
Test outputs / exit codes: Full local gate passed with 127 unit tests, 2 golden tests, 0 build warnings/errors and clean diff. Focused extraction tests passed (6) and diagnostics/capability failure tests passed (3). The real smoke reported `kind=Video; waveform_peaks=128`, then `kind=Audio; duration=00:00:01; has_audio=True`, and a four-sample seek trace reported p50/p95 latency and decode throughput (all exit 0).
Media fixtures and verification: The source MP4 remained unchanged, the temporary WAV was removed on scope disposal, and the request-path test proved the original video bytes were not uploaded. No API key, user media or generated asset entered Git.
Known limitations / blockers: The three-OS native-media workflow passed hosted run `39051936860`, but interactive GUI acceptance and optional disk-proxy evidence remain open. Live Gemini key/provider verification, local ASR, long-audio/Files API uploads, approved watermark bytes, MLT/second-OS playback proof and installer/signing evidence also remain open. Python is not installed, so `scripts/validate_repository.py` was not run locally.
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
Windows: FT-001/FT-002/FT-003/FT-004/FT-005/FT-006/FT-101/FT-102/FT-103/FT-104/FT-105/FT-201/FT-202/FT-203/FT-204/FT-205/FT-301/FT-302/FT-303/FT-304/FT-305/FT-306/FT-401/FT-402/FT-403/FT-404/FT-405/FT-501/FT-502/FT-503/FT-504/FT-505/FT-506/FT-601/FT-603/FT-604/FT-605/FT-701/FT-702/FT-703/FT-704/FT-705/FT-801/FT-802, FT-803, FT-804 and FT-706 undo paths built/tested; FT-1001 core suite and FT-1004 hosted CI artifacts verified; FT-803 watermark/spectral-marker, FT-602 live provider fixture and FT-706 live provider scope remain partial; packaged UI unverified. macOS: hosted CI restore/build/test/publish, native FFmpeg/media smoke and deterministic timeline export verified by run 39055332326; interactive desktop acceptance remains unverified. Linux: hosted CI restore/build/test/publish, native FFmpeg/media smoke and deterministic timeline export verified by run 39055332326; interactive desktop acceptance remains unverified. Gemini provider: headless adapter contract tested; no live key/media upload run. Logo: source provided outside GitHub repository, not yet committed. MLT: not adopted. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
