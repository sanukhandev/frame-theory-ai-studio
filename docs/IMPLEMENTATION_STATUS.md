# Implementation Status — Resume Checkpoint

**Color-preset save checkpoint (2026-10-19):** Color-preset serialization now enforces the 5 MB UTF-8 bound before creating a temporary file and cleans up interrupted writes without masking the original error; focused preset persistence coverage passes. Native preset workflow acceptance remains open.

**AI-recipe save checkpoint (2026-10-19):** AI recipe serialization now enforces the 1 MB UTF-8 bound before creating a temporary file, preventing saves that cannot be reopened; focused recipe persistence coverage passes. Native AI recipe acceptance remains open.

**Photo-state save checkpoint (2026-10-19):** Photo edit-state serialization now applies the same 1 MB UTF-8 bound as deserialization, preventing saves that cannot be reopened; focused photo persistence coverage passes. Native photo save/reopen acceptance remains open.

**Photo-state input checkpoint (2026-10-19):** Portable photo edit-state loading now enforces its 1 MB limit against UTF-8 encoded bytes, preventing Unicode payloads from bypassing persistence bounds; focused photo persistence coverage passes. Native photo reopen acceptance remains open.

**LUT-input checkpoint (2026-10-19):** `.cube` parsing now enforces the 64 MB limit against UTF-8 encoded bytes rather than UTF-16 character count, preventing oversized Unicode payloads from bypassing the parser guard; focused LUT coverage passes. Native LUT workflow acceptance remains open.

**Render-duration checkpoint (2026-10-19):** Timeline render plans now reject clips, titles or captions that extend beyond the requested duration, preventing out-of-range filter stages and export drift; focused compositor coverage passes. Native export acceptance remains open.

**Render-source checkpoint (2026-10-19):** Timeline render plans now reject video/audio clips whose media IDs are absent from the source map, converting corrupted plans into clear validation failures before filter-graph generation; focused compositor coverage passes. Native export acceptance remains open.

**Transition-boundary checkpoint (2026-10-19):** Timeline layers now reject incoming transitions longer than the preceding clip, preventing negative compositor `xfade` offsets; focused compositor coverage passes. Native transition export acceptance remains open.

**Motion-frame bound checkpoint (2026-10-19):** Motion-analysis frames now enforce the 64M-pixel bound before byte-length validation or tracking, preventing oversized CPU/memory work; focused motion coverage passes. Manual stabilization quality acceptance remains open.

**Photo-resize checkpoint (2026-10-19):** Raster resizing now uses 64-bit coordinate products before division, preserving valid tall/wide images beyond 32-bit intermediate limits; focused photo coverage passes. Native large-image export acceptance remains open.

**AI action-identity checkpoint (2026-10-19):** Programmatic AI actions now reject blank IDs, operation types and target IDs before semantic validation or execution; focused plan coverage passes. Gemini execution acceptance remains open.

**Batch-progress checkpoint (2026-10-19):** Batch-export progress now rejects blank targets, null nested progress and impossible completed/total counts; focused batch-export coverage passes. Native queue/progress acceptance remains open.

**AI input-shape checkpoint (2026-10-19):** Gemini plan requests now require a pure text prompt, reject text/image field smuggling, and continue to block malformed media before network use; focused interaction coverage passes. Live provider acceptance remains open.

**AI text-input checkpoint (2026-10-19):** Gemini interaction text inputs now reject whitespace-only content before provider request construction; focused interaction coverage passes. Live provider acceptance remains open.

**Progress-contract checkpoint (2026-10-19):** Local-analysis and export progress updates now reject blank stages and non-finite or out-of-range fractions, preventing invalid status values from reaching the UI; focused AI/export coverage passes. Native progress rendering remains open.

**Cache-size checkpoint (2026-10-19):** Cache-index accounting now rejects total-size overflow before mutating entries and keeps eviction totals exact; focused recovery coverage passes. Native cache-recovery acceptance remains open.

**Timeline track-kind checkpoint (2026-10-19):** Timeline selection and insertion now reject undefined track kinds instead of silently treating them as audio; focused timeline coverage passes. Native timeline acceptance remains open.

**AI-key accessibility checkpoint (2026-10-19):** The secure Gemini API-key textbox now exposes a stable automation name and matching secret-store tooltip in the production panel; focused headless shell coverage passes. Native screen-reader acceptance remains open.

**Scope-size checkpoint (2026-10-19):** Color-scope construction now enforces the same 64M-pixel bound as analysis, preventing `PixelCount` overflow and unbounded inspector allocations; focused scope coverage passes. Native scope acceptance remains open.

**Waveform-input checkpoint (2026-10-19):** Imported media now rejects non-finite or out-of-range waveform peaks before beat detection, rough-cut scoring or AI metadata serialization; focused media-import coverage passes. Native import acceptance remains open.

**Motion-plan checkpoint (2026-10-19):** Stabilization plans now reject null transforms before motion data reaches compositor generation; focused motion coverage passes. Manual stabilization quality acceptance remains open.

**Recovery containment checkpoint (2026-10-19):** Contained recovery paths now reject blank project-directory and relative-media inputs consistently on Unix-like platforms; hosted Linux/macOS recovery-test failure is fixed locally and focused coverage is pending rerun.

**AI history checkpoint (2026-10-19):** Gemini usage-history reads now cap requested entries at 1000 to keep local diagnostics memory-bounded; focused history coverage passes. Live provider acceptance remains open.

**AI input checkpoint (2026-10-19):** Gemini interaction factories now reject missing text, empty inline images, and incomplete image URI metadata before request construction; focused AI coverage passes. Live provider acceptance remains open.

**Clip-sampling checkpoint (2026-10-19):** Timeline video and audio clip sampling now rejects an uninitialized project frame rate before source arithmetic; focused timeline coverage passes. Native playback acceptance remains open.

**Title-placement checkpoint (2026-10-19):** Timeline titles now reject undefined placement values before drawtext coordinate generation; focused title-contract coverage passes. Native caption/title acceptance remains open.

**Reveal-style checkpoint (2026-10-19):** Timeline render plans now reject undefined before/after reveal styles before compositor dispatch; focused render-plan coverage passes. Native reel acceptance remains open.

**Decoder dependency checkpoint (2026-10-19):** `FfmpegFrameDecoder` now rejects a missing media process runner at construction; focused decoder coverage passes. Native preview acceptance remains open.

**Preview-proxy dependency checkpoint (2026-10-19):** `FfmpegPreviewProxyService` now rejects a missing media process runner at construction; focused proxy coverage passes. Native proxy acceptance remains open.

**Probe executable checkpoint (2026-10-19):** `FfprobeMediaProbe` now rejects a blank executable name at construction instead of failing later in process launch; focused probe coverage passes. Native probe acceptance remains open.

**Proxy cache-path checkpoint (2026-10-19):** Preview proxy construction now rejects a blank cache directory instead of resolving it to the process working directory; focused proxy coverage passes. Native proxy acceptance remains open.

**Video export destination checkpoint (2026-10-19):** Media export now rejects a blank destination before path normalization or FFmpeg work; focused export coverage passes. Native export acceptance remains open.

**Photo export destination checkpoint (2026-10-19):** Photo export now rejects a blank destination before source/path normalization or FFmpeg work; focused photo-export coverage passes. Native photo-export acceptance remains open.

**Preset path checkpoint (2026-10-19):** Color preset save/load now rejects blank paths before normalizing them to the working directory; focused preset persistence coverage passes. Native preset workflow acceptance remains open.

**Cache-index path checkpoint (2026-10-19):** Cache-index save/load now rejects blank paths before creating or reading a working-directory file; focused recovery persistence coverage passes. Native cache recovery acceptance remains open.

**Recovery path checkpoint (2026-10-19):** Project recovery now rejects blank project and replacement paths before autosave, relink or containment normalization; focused recovery coverage passes. Native recovery workflow acceptance remains open.

**Project frame-rate checkpoint (2026-10-19):** Project snapshots now reject an uninitialized frame rate at the shared persistence boundary; focused domain coverage passes. Native project reopen acceptance remains open.

**Timeline transform checkpoint (2026-10-19):** Video transforms now permit signed X/Y offsets so pan-up and pan-left keyframes are representable; focused timeline contract coverage passes. Native transform preview acceptance remains open.

**Edit-command checkpoint (2026-10-19):** Media-reference add/remove commands now reject null references at construction instead of failing during transactional apply/undo; focused command-history coverage passes. Native command workflow acceptance remains open.

**Command-group checkpoint (2026-10-19):** Edit command groups now reject null members before transactional execution; focused command-history coverage passes. Native command workflow acceptance remains open.

**Canvas-command checkpoint (2026-10-19):** Canvas edit commands now reject non-positive dimensions before entering undo history; focused command-history coverage passes. Native canvas workflow acceptance remains open.

**Timeline enum checkpoint (2026-10-19):** Timeline tracks now reject undefined track kinds before clip classification; focused timeline contract coverage passes. Native timeline acceptance remains open.

**Transition enum checkpoint (2026-10-19):** Video segments now reject undefined transition kinds before filter-graph construction; focused timeline contract coverage passes. Native transition acceptance remains open.

**Keyframe easing checkpoint (2026-10-19):** Transform keyframes now reject undefined easing values before interpolation or FFmpeg expression generation; focused keyframe coverage passes. Native animation acceptance remains open.

**Media-kind checkpoint (2026-10-19):** Media metadata now rejects undefined media kinds before dimension and timing interpretation; focused domain coverage passes. Native import acceptance remains open.

**Social preset checkpoint (2026-10-19):** Video and photo social presets now reject undefined aspect ratios and non-positive dimensions at construction; focused export-preset coverage passes. Native social export acceptance remains open.

**Color-profile checkpoint (2026-10-19):** Color profiles now reject undefined primaries/transfers and normalize names before entering the shared color graph; focused color coverage passes. Native color-export acceptance remains open.

**Photo blend checkpoint (2026-10-19):** Composite photo layers now reject undefined blend modes before raster composition; focused photo-layer coverage passes. Native photo compositing acceptance remains open.

**Photo-operation checkpoint (2026-10-19):** Photo edit operations now reject undefined operation kinds before state replay or persistence restore; focused photo-layer coverage passes. Native photo editing acceptance remains open.

**Photo-mask checkpoint (2026-10-19):** Adjustment layers now reject null mask strokes before rendering or persistence; focused photo-layer coverage passes. Native photo editing acceptance remains open.

**Composite-mask checkpoint (2026-10-19):** Composite layers now reject null mask strokes before raster composition or persistence; focused photo-layer coverage passes. Native photo editing acceptance remains open.

**Photo viewport checkpoint (2026-10-19):** Canvas fit, 100% and zoom operations now reject default zero-size viewports before calculating invalid layout state; focused photo-canvas coverage passes. Native photo canvas acceptance remains open.

**Process-runner checkpoint (2026-10-19):** FFmpeg process execution now rejects blank commands and null argument collections before process creation; focused runner coverage passes. Native codec execution acceptance remains open.

**AI analysis contract checkpoint (2026-10-19):** Local Gemini-analysis samples and reports now validate local sources, bounded metadata, timing, payloads, scopes, byte estimates and disclosure before any provider call; focused analysis coverage passes. Gemini acceptance remains open.

**AI request checkpoint (2026-10-19):** Gemini analysis requests now reject null assets and unsupported scope flags before local sampling; focused analysis coverage passes. Gemini acceptance remains open.

**Reference-analysis checkpoint (2026-10-19):** Gemini reference-grade requests now reject null visual samples before provider payload construction; focused reference-analysis coverage passes. Gemini acceptance remains open.

**Gemini input checkpoint (2026-10-19):** Gemini plan requests now reject null interaction inputs before prompt inspection or network use; focused interaction coverage passes. Gemini acceptance remains open.

**Caption-word checkpoint (2026-10-19):** Timed captions now reject null word entries before sorting or timing validation; focused caption coverage passes. Native caption acceptance remains open.

**Caption-export checkpoint (2026-10-19):** SRT export now rejects null caption entries before ordering or serialization; focused caption coverage passes. Native caption acceptance remains open.

**Transcript-word checkpoint (2026-10-19):** Imported transcript results now reject null word entries before ordering or overlap validation; focused transcription coverage passes. Native transcription acceptance remains open.

**Photo-layer collection checkpoint (2026-10-19):** Photo adjustment-layer stacks now reject null layers before duplicate-name checks or render traversal; focused photo-layer coverage passes. Native photo editing acceptance remains open.

**Photo-state checkpoint (2026-10-19):** Photo edit-state restoration now rejects null operations, composite layers and retouch strokes before render traversal; focused photo-layer coverage passes. Native photo editing acceptance remains open.

**Timeline-seek checkpoint (2026-10-19):** Timeline seek results now reject null samples before transport consumers read the immutable snapshot; focused timeline coverage passes. Native timeline preview acceptance remains open.

**Audio-fade checkpoint (2026-10-19):** Audio segments now reject combined fade-in/fade-out ranges longer than the segment before filter-graph construction; focused audio-timeline coverage passes. Native audio acceptance remains open.

**AI catalog checkpoint (2026-10-19):** AI operation descriptors now reject null, blank or oversized parameter names before allowlist publication; focused AI-plan coverage passes. Gemini acceptance remains open.

**AI context checkpoint (2026-10-19):** AI plan validation contexts now reject blank or oversized target IDs and normalize the project ID before allowlist validation; focused AI-plan coverage passes. Gemini acceptance remains open.

**Preview frame-rate checkpoint (2026-10-19):** Frame decoding now rejects an uninitialized preview frame rate before filesystem or FFmpeg work; focused decoder coverage passes. Native preview acceptance remains open.

**Process-argument checkpoint (2026-10-19):** FFmpeg process execution now rejects null argument entries before `ProcessStartInfo` construction; focused runner coverage passes. Native codec execution acceptance remains open.

**Audio-preparation dependency checkpoint (2026-10-19):** Audio source preparation now rejects a missing media process runner at construction instead of failing during extraction; focused audio coverage passes. Native audio acceptance remains open.

**Media-import dependency checkpoint (2026-10-19):** FFmpeg media importers now reject missing probe, preview-decoder and process-runner services at construction; focused import coverage passes. Native import acceptance remains open.

**Windows CI manifest checkpoint (2026-10-19):** Native-bundle tamper and traversal smoke assertions now validate PowerShell rejection text through `try/catch` rather than relying on `$LASTEXITCODE`, which is not updated consistently for script `throw` failures. Hosted Windows rerun is required.

**Hosted Linux fixture checkpoint (2026-10-19):** Case-variant output tests now specify the metadata they require—video export disables audio when the stub has no audio, and photo export probes the document's `1×1` dimensions. Local focused coverage passes; hosted Linux rerun is required.

**50-commit release checkpoint (2026-10-19):** Full local gate passes after aligning the photo-document dimension test with the stricter visual metadata guard: Release build has 0 warnings/0 errors, 383 unit tests pass, 2 golden tests pass, `dotnet format --verify-no-changes` passes, repository validation passes, and `git diff --check` is clean. Hosted/native UI, packaging and real-media export evidence remain tracked separately; six P0 gates remain partial.

**AI plan integrity checkpoint (2026-10-19):** Programmatically constructed edit plans now reject null actions before schema validation, approval or execution; focused AI-plan coverage passes. Gemini/network acceptance remains open.

**Render-grade integrity checkpoint (2026-10-19):** Timeline render plans now reject null color grades and default media IDs before storing the immutable grade map; focused compositor coverage passes. Native color-export acceptance remains open.

**Filter-graph boundary checkpoint (2026-10-19):** Timeline filter graphs now require nonblank video expressions/labels, local file inputs and nonblank optional audio labels before FFmpeg argument construction; focused compositor coverage passes. Native preview/export acceptance remains open.

**Timeline exporter dependency checkpoint (2026-10-19):** Timeline video exporters now reject missing process-runner and media-probe services at construction instead of failing during export; focused compositor coverage passes. Native timeline export acceptance remains open.

**Recovery input checkpoint (2026-10-19):** Missing-media discovery and relink now reject null project snapshots explicitly before path or media traversal; focused recovery coverage passes. Native recovery workflow acceptance remains open.

**Cache-index integrity checkpoint (2026-10-19):** Cache-index loads now reject duplicate keys instead of silently overwriting earlier entries; focused recovery persistence coverage passes. Native cache-recovery acceptance remains open.

**Preset persistence checkpoint (2026-10-19):** Preset saves now reject null collection entries before name grouping or serialization; focused color-preset persistence coverage passes. Native preset workflow acceptance remains open.

**Frame-rate validity checkpoint (2026-10-19):** Default `0/0` frame rates now fail at shared metadata, timeline graph/clip, render-plan and export-preset boundaries instead of surfacing as late divide-by-zero timing failures; focused domain/media coverage passes. Native timing/export acceptance remains open.

**Render-plan boundary checkpoint (2026-10-19):** Timeline render plans now reject null layers and invalid media-ID/URI source entries before filter-graph construction; focused compositor coverage passes. Native export acceptance remains open.

**Visual metadata dimension checkpoint (2026-10-19):** Image and video metadata now require positive dimensions while audio metadata may remain dimensionless; focused domain coverage and the full local gate pass. Native media-probe acceptance remains open.

**Media identity boundary checkpoint (2026-10-19):** Media references and source metadata now reject the default empty `MediaId` value, preventing invalid identity records from entering project persistence or timeline state; focused repository/domain coverage passes. Native relink acceptance remains open.

**Timeline domain collection checkpoint (2026-10-19):** Timeline tracks, graphs and speed ramps now reject null entries before ordering or dereferencing; focused domain validation coverage passes. Native timeline persistence acceptance remains open.

**Timeline layer integrity checkpoint (2026-10-19):** Video and audio layer constructors now reject null segment entries before ordering them, preventing corrupted state from surfacing as null-reference failures; focused compositor/audio coverage passes. Native timeline persistence acceptance remains open.

**Timeline keyframe integrity checkpoint (2026-10-19):** Keyframe collections now reject null entries before sorting, converting corrupted input into a clear argument failure instead of a null-reference crash; focused keyframe coverage passes. Native timeline persistence acceptance remains open.

**AI keyframe schema checkpoint (2026-10-19):** AI validation now checks keyframe fields, finite transform bounds, easing values and duplicate frame positions before approval; focused malformed-animation coverage passes. Gemini execution remains allowlisted and six P0 gates remain partial.

**AI action range checkpoint (2026-10-19):** AI validation now rejects negative starts and non-positive durations even when a plan is constructed in memory without the JSON parser; focused validator coverage passes. Gemini execution remains allowlisted and six P0 gates remain partial.

**Timeline ruler bound checkpoint (2026-10-19):** Ruler generation now clamps extreme viewport math and caps tick materialization at 4096 entries, preventing pathological zoom/viewport requests from overflowing or allocating unbounded UI state; focused timeline coverage passes. Native HiDPI acceptance remains open.

**Timeline numeric-boundary checkpoint (2026-10-19):** Timeline scroll deltas now clamp instead of wrapping, and snap-distance ordering uses `Int128` so extreme frame positions cannot overflow; focused timeline coverage passes. Native transport acceptance remains open.

**Timeline caption bounds checkpoint (2026-10-19):** Caption range validation now uses checked frame arithmetic, preventing large frame inputs from wrapping into an apparently valid range; focused timeline coverage passes. Native caption acceptance remains open.

**LUT parser boundary checkpoint (2026-10-19):** 3D LUT parsing now rejects duplicate size and domain directives instead of silently redefining structural state; focused LUT coverage passes. LUT legal/format acceptance remains open.

**Photo raster parser checkpoint (2026-10-19):** PPM numeric parsing is now culture-invariant and duplicate PAM header fields fail as `InvalidDataException` instead of leaking dictionary exceptions; focused alpha/parser coverage passes. Native photo export acceptance remains open.

**Media adapter dependency checkpoint (2026-10-19):** Photo import/export, FFmpeg export, and ffprobe adapters now reject missing injected services during composition; focused constructor coverage passes. Native media-engine acceptance remains open.

**Photo dimension boundary checkpoint (2026-10-19):** Photo documents now reject zero-dimension image metadata before canvas fit/zoom calculations can produce invalid coordinates; focused canvas coverage passes. Native photo rendering acceptance remains open.

**Photo recovery dependency checkpoint (2026-10-19):** Photo recovery now rejects missing repository or recovery services at construction instead of failing during open/relink; focused view-model coverage passes. Photo recovery acceptance remains open.

**Media metadata boundary checkpoint (2026-10-19):** Unsupported `ffprobe` frame rates now produce a stable media-probe error, while negative or out-of-range durations normalize safely instead of leaking overflow exceptions; synthetic malformed-metadata coverage passes. Native codec acceptance remains open.

**Recovery dependency validation checkpoint (2026-10-19):** Project recovery now rejects a missing JSON repository at construction instead of deferring the failure to the first autosave operation; focused recovery coverage passes. Storage relink acceptance remains open.

**Workspace dependency validation checkpoint (2026-10-19):** Desktop shell construction now rejects a missing capability detector at the composition boundary instead of failing later during an asynchronous refresh; focused constructor coverage passes. Native workspace acceptance remains open.

**Workspace startup resilience checkpoint (2026-10-19):** A workspace-state read failure now falls back to Video and records diagnostics instead of preventing shell construction; focused startup coverage complements the existing write-failure regression. Native workspace acceptance remains open.

**Case-sensitive export checkpoint (2026-10-19):** Media and photo overwrite guards now use Windows-only case folding, allowing distinct case-variant output names on Linux without weakening same-path protection; focused exporter coverage includes both paths. Native export acceptance remains open.

**Batch export path checkpoint (2026-10-19):** Social batch-export destination uniqueness now follows platform path semantics, allowing case-variant filenames on Linux while retaining Windows case-insensitive protection; a Linux regression covers the distinction. Native batch export acceptance remains open.

**Credential cancellation checkpoint (2026-10-19):** Unix credential-store process cleanup now ignores unsupported kill failures so canceled key operations preserve their cancellation result; secret names and OS-backed storage behavior remain unchanged. Existing platform-secret coverage remains green.

**Process cancellation checkpoint (2026-10-19):** FFmpeg and local Whisper cancellation cleanup now ignores platform kill failures, preserving the original cancellation result instead of leaking a secondary termination exception. Existing media/caption cancellation coverage remains green; native interruption acceptance remains open.

**Timeline beat-mode persistence checkpoint (2026-10-19):** Project and transactional timeline restores now notify the bound `BeatDetectionMode` property after loading its persisted value, keeping the waveform-analysis selector synchronized; round-trip coverage passes. Native timeline UI acceptance remains open.

**Timeline scroll persistence checkpoint (2026-10-19):** Reopened timeline projects now restore `ScrollFrame` through its notifying property setter, keeping the bound timeline viewport synchronized with the persisted horizontal position; round-trip coverage passes. Native reopen/visual acceptance remains open.

**Native bundle packaging checkpoint (2026-10-19):** Windows, macOS and Linux package scripts now fail closed on nested native-bundle directories instead of silently omitting them; existing linked-entry and required-tool guards remain active. Shell syntax and focused packaging guard validation remain required before hosted release evidence.

**Cross-platform containment checkpoint (2026-10-19):** Project media containment now uses Windows-only case folding and ordinal comparison elsewhere, preventing a case-variant sibling escape on Linux; the Linux regression is covered. Storage/relink acceptance remains open on native platforms.

**Capability probe cleanup checkpoint (2026-10-19):** Temporary storage-probe cleanup now ignores filesystem delete failures so a capability report is not replaced by a secondary cleanup exception. Existing detector coverage remains green; native capability acceptance remains open.

**Timeline caption-history checkpoint (2026-10-19):** Manual undo/redo snapshots now preserve the persisted `KineticCaptions` setting when caption or transcript imports toggle it; a regression confirms undo restores both caption data and the setting. Timeline-focused coverage remains green; native caption acceptance remains open.

**Native manifest generation checkpoint (2026-10-19):** Manifest generation now rejects blank or path-bearing FFmpeg/ffprobe names before resolving them, matching verifier containment rules. Local valid generation and invalid-path rejection pass; FT-1003 legal/license, CVE, codec and redistribution approval remains open.

**Native manifest path checkpoint (2026-10-19):** Native-media manifest verification now requires `ffmpeg` and `ffprobe` names to match exact staged-file records, preventing a manifest path from resolving outside the bundle; Windows hosted validation adds a traversal regression beside version-tampering coverage. FT-1003 remains open for legal/license, CVE, codec and redistribution approval.

**Editor shortcut polish checkpoint (2026-10-19):** The production shell now advertises both `Ctrl+Z` undo and CapCut-style `Ctrl+Shift+Z` redo alongside the existing `Ctrl+Y` route; the existing text-input protection remains unchanged. Foundation shortcut/UI coverage remains green. Native keyboard acceptance remains open.

**Native manifest provenance checkpoint (2026-10-19):** Native FFmpeg/ffprobe manifest verification now re-runs each staged tool and rejects tampered version output in addition to hash and size mismatches; the Windows hosted validation path includes the regression. Local fake-tool generation, verification and tampered-version rejection pass. FT-1003 remains open for legal/license, CVE, codec and redistribution approval.

**Timeline seek-boundary checkpoint (2026-10-19):** Video Home/End shortcuts now seek through the existing transport path, and post-snap clamping prevents a clip’s exclusive end boundary from becoming an invalid current frame. Foundation coverage passes 29 tests and timeline coverage passes 21 tests; native keyboard/HiDPI acceptance remains open.

**Timeline undo/redo checkpoint (2026-10-19):** Video workspace manual edits now run through the existing full timeline snapshot transaction boundary, with visible Undo/Redo controls, `Ctrl+Z`/`Ctrl+Y` routing, rollback on failed operations, and coverage for captions, watermark changes and inspector actions; reviewed rough cuts retain their existing transactional AI-undo path without duplicate history entries. Timeline-focused coverage passes 20 tests and Foundation UI/shortcut coverage passes 28 tests; the full local Release gate passes 355 unit tests, 2 golden tests, a warning-free build, format verification and repository validation. Native manual acceptance remains open.

**Timeline transport shortcut checkpoint (2026-10-19):** Video workspace Space-key handling now starts or stops the existing local timeline preview path, while text inputs remain protected; the shell advertises the shortcut beside existing workspace and undo keys. Focused Foundation coverage passes 27 tests; native keyboard and HiDPI acceptance remains open.

**Workspace-state resilience checkpoint (2026-10-19):** A local workspace-state write failure no longer interrupts workspace navigation; the failure is recorded through diagnostics while the selected workspace and active status continue to update. Focused Foundation coverage passes 26 tests; native UX acceptance remains open.

**AI-recipe permission checkpoint (2026-10-19):** AI recipe save/load now report permission-denied paths through the AI workspace status boundary instead of escaping local recipe commands. Focused revision/recipe coverage passes.

**Caption-permission checkpoint (2026-10-19):** Caption import, verified transcript import, local Whisper and optional Gemini transcription now report permission failures through the Caption workspace status boundary. Focused transcription coverage passes; no caption mutation occurs on failure.

**Gemini-analysis storage checkpoint (2026-10-19):** Credential-store access failures during optional Gemini analysis and reference-grade requests now remain status results without escaping the AI command. Focused analysis coverage passes; offline/manual editing remains available.

**Gemini-revision storage checkpoint (2026-10-19):** Credential-store access failures during optional Gemini revision requests now remain ordinary status results, preserving offline/manual editing instead of escaping the AI workspace command. Focused revision coverage passes.

**Path-security checkpoint (2026-10-19):** Reparse-point checks now treat attribute IO and access failures as unsafe paths instead of leaking an OS exception through the security boundary. Existing path-security and storage coverage remains green.

**Autosave cleanup checkpoint (2026-10-19):** Autosave removal is now best effort, so recovery and project lifecycle callers are not interrupted by a secondary filesystem cleanup failure. Existing autosave recovery and relink coverage remains green.

**Timeline-compositor cleanup checkpoint (2026-10-19):** Timeline compositor temporary output cleanup is now best effort, preserving composition, verification and cancellation failures when an intermediate file cannot be deleted. Existing compositor coverage remains green.

**Media-export cleanup checkpoint (2026-10-19):** Direct video export temporary-file cleanup is now best effort, preserving encoder, verification and cancellation failures instead of replacing them with cleanup exceptions. Existing media-export coverage remains green.

**Preview-cache maintenance checkpoint (2026-10-19):** Proxy temp cleanup and optional cache eviction no longer mask a successfully generated preview when the operating system refuses a maintenance delete. Existing proxy reuse, failure and budget coverage remains green.

**Timeline-preview cleanup checkpoint (2026-10-19):** Rendered timeline-frame cleanup is now best effort, preserving the underlying FFmpeg, decode or cancellation failure when the temporary PPM cannot be removed. Existing timeline preview coverage remains green.

**Frame-decoder cleanup checkpoint (2026-10-19):** Single-frame preview cleanup is now best effort, preserving the decoder or cancellation error when the temporary output cannot be removed. Existing decoder failure and cancellation coverage remains green.

**Photo-export cleanup checkpoint (2026-10-19):** Photo export temporary-file cleanup is now best effort, so an encoder, verification or cancellation error remains the user-visible failure even when the operating system denies cleanup. Existing photo export failure coverage remains green.

**Cache-index cancellation checkpoint (2026-10-19):** Cache-index temporary-file cleanup no longer masks the original save or cancellation failure, and a canceled replacement save preserves the existing index. Focused recovery coverage passes; the next full release gate remains required before the 50-commit push.

**Release batching checkpoint (2026-10-19):** Per owner instruction, push only after 50 verified local commits are accumulated to reduce hosted-minute usage. The exact 50-commit batch was pushed at `acd4e83`; the next local status checkpoint will remain unpushed until another 50 verified commits accumulate.

**Hosted Release gate checkpoint (2026-10-19):** Quality Gates run `39172309590` passed for `acd4e83` across documentation/schema validation, Ubuntu, Windows and macOS restore/build/test, native media, headless shell/UI automation, formatting, portable archives, installer/DMG/DEB validation, relaunch and checksums. The six tracked partial P0 gates remain open because manual visual/HiDPI/screen-reader, legal redistribution, signing/clean-host and final-audit evidence are still required.

**GitHub epic-closure checkpoint (2026-10-19):** A live audit confirmed every implementation issue under E00-E07 is closed, so completed umbrella epics E00-E07 were closed with the local Release evidence above. GitHub now reports 63 closed issues and 19 open issues. The six partial P0 gates remain `FT-901`, `FT-902`, `FT-1002`, `FT-1003`, `FT-1005` and `FT-1006`; E11/E12 work and external/manual release evidence remain open. This is not a V1 completion claim.

**FT-901 compact-header checkpoint (2026-10-19):** Workspace headers now stack their status and local/optional-AI mode metadata below the title at the compact 960px shell width, then restore the two-column layout at standard width. Avalonia headless coverage verifies both layouts; the full local gate passes 314 unit tests and 2 golden tests with format and diff checks clean. Native visual, HiDPI and screen-reader acceptance remains open.

**Media import cancellation checkpoint (2026-10-19):** A canceled multi-file import now publishes the successfully imported prefix to the media bin and timeline instead of leaving view-model state stale after partial work. A regression covers the cancellation boundary; the full local gate passes 315 unit tests and 2 golden tests with format and diff checks clean.

**Media probe duration checkpoint (2026-10-19):** ffprobe fallback frame-count parsing now clamps a positive-duration video to at least one frame when `nb_frames` is unavailable, preventing valid sub-frame-duration metadata from producing an invalid timeline clip. Focused and full validation pass at 316 unit tests and 2 golden tests; zero-duration unknown media remains rejected by downstream timeline validation.

**Video metadata boundary checkpoint (2026-10-19):** The domain now rejects zero-frame video metadata before it can enter a timeline, and the desktop import boundary reports argument-validation failures as ordinary per-file import failures. The full Release build is warning-free and the 316-unit/2-golden validation remains green.

**Timeline recovery cancellation checkpoint (2026-10-19):** Opening a replacement timeline project now keeps the current missing-media/relink state intact until every replacement asset imports and the new project is committed. A canceled replacement-open regression passes; the full local gate is 317 unit tests and 2 golden tests with a warning-free build.

**Photo recovery failure checkpoint (2026-10-19):** A failed attempt to open a different photo recovery project no longer clears the active missing-media list or snapshot, so the user can continue the original relink flow. The full Release gate remains green at 317 unit tests and 2 golden tests with zero build warnings.

**Timeline relink handoff checkpoint (2026-10-19):** Successful timeline relinks now retain the repaired pending snapshot until the follow-up project open completes, preventing cancellation or import failure from discarding recovery state after the on-disk project has already been updated. Full validation remains 317 unit tests and 2 golden tests with zero build warnings.

**Export queue shutdown checkpoint (2026-10-19):** Queue disposal now cancels active and queued export jobs before stopping the worker, preventing application shutdown from hanging on an in-flight export or leaving queued completions unresolved. Focused queue coverage includes the shutdown regression; full release validation is required before the next 50-commit push.

**Preview cache invalidation checkpoint (2026-10-19):** Decoded frame cache keys now include source file size and last-write time, so replacing media at the same path invalidates stale preview pixels without hashing the source on every seek. Focused decoder coverage includes the replacement regression; full release validation is required before the next 50-commit push.

**Proxy cache invalidation checkpoint (2026-10-19):** Optional disk-proxy cache keys now include source file size and last-write time in addition to the caller identity, so same-path source replacement cannot reuse an old proxy. Focused proxy coverage includes the replacement regression; full release validation is required before the next 50-commit push.

**Import consistency checkpoint (2026-10-19):** Local media import now verifies the source file remains the same size and last-write version across probe, thumbnail and waveform work, rejecting a changed source instead of publishing mixed metadata and analysis. Focused import coverage includes the mutation regression; full release validation is required before the next 50-commit push.

**Timeline media fingerprint checkpoint (2026-10-19):** Timeline projects now save SHA-256 fingerprints for local media and validate them during reopen, treating same-path source replacements as missing media for the existing relink workflow. Legacy `probe:` references remain readable. Focused persistence/recovery coverage passes 20/20; the full release gate is required before the next 50-commit push.

**Media identity checkpoint (2026-10-19):** ffprobe metadata now derives `MediaId` from the actual local media SHA-256 instead of a path-only marker, so same-path replacement imports cannot silently reuse the prior asset identity. Focused probe coverage includes the replacement regression; full release validation remains required before the next 50-commit push.

**Caption transaction checkpoint (2026-10-19):** Rejected overlapping caption additions now leave the previously selected caption font unchanged because validation completes before editor state mutation. Focused timeline-editor coverage includes the rollback regression; full release validation remains required before the next 50-commit push.

**Export queue lifecycle checkpoint (2026-10-19):** Export queue disposal is now idempotent, so repeated application teardown paths await the completed worker without touching disposed cancellation state. Focused queue coverage includes double-dispose behavior; full release validation remains required before the next 50-commit push.

**AI target-boundary checkpoint (2026-10-19):** Desktop AI validation now allowlists only media IDs represented by timeline clips for timeline edit operations; imported photo-only assets can still be analyzed locally but cannot be proposed as unsupported timeline targets. Focused desktop AI coverage includes the rejection path; full release validation remains required before the next 50-commit push.

**Photo export snapshot checkpoint (2026-10-19):** Queued photo exports now capture the immutable edit state when enqueued, preventing later edits from changing the requested export while it waits behind another job. Focused desktop export coverage includes the blocked-queue regression; full release validation remains required before the next 50-commit push.

**Gemini history memory checkpoint (2026-10-19):** Local Gemini usage history now retains only the requested recent tail while reading JSONL records, avoiding an unbounded in-memory list for long-lived logs. Focused history coverage includes the tail-order contract; full release validation remains required before the next 50-commit push.

**AI approval path checkpoint (2026-10-19):** Optional `FRAME_THEORY_BRAND_FONT_PATH` resolution now ignores malformed, inaccessible or symlinked paths instead of allowing an invalid environment value to escape from the approval UI action. Focused desktop AI coverage includes the rejected-path contract; full release validation remains required before the next 50-commit push.

**Media permission checkpoint (2026-10-19):** Permission-denied imports now remain ordinary per-file failures in the desktop media-bin workflow instead of escaping the import command and interrupting the UI. Focused import coverage includes the access-denied contract; full release validation remains required before the next 50-commit push.

**Preview-proxy error checkpoint (2026-10-19):** Preview-proxy failures now flow through the existing timeline preview status boundary instead of escaping the Video workspace command. Focused desktop preview coverage includes a real failed-proxy path; full release validation remains required before the next 50-commit push.

**Live media replacement checkpoint (2026-10-19):** The open desktop project now captures local media size/write stamps at import and refuses preview, motion analysis, AI sampling, save or export after a source path changes, preventing stale metadata from rendering replacement bytes. A focused live-replacement preview regression covers the guard; full release validation remains required before the next 50-commit push.

**Export retry integrity checkpoint (2026-10-19):** Timeline export retries now revalidate the stored plan sources before requeueing, so a failed export cannot later render a changed same-path source. Focused desktop coverage includes fail-then-replace-then-retry behavior; full release validation remains required before the next 50-commit push.

**Gemini history retention checkpoint (2026-10-19):** The local Gemini usage JSONL now trims itself to the newest records after crossing a 1 MiB limit, preventing long-lived optional-AI use from growing the local history file without bound. Focused history coverage verifies byte and tail limits; full release validation remains required before the next 50-commit push.

**Photo export error checkpoint (2026-10-19):** Invalid, inaccessible or malformed photo export destinations now become visible Photo workspace status failures instead of escaping the queued export command. Focused photo export coverage exercises the real exporter with an invalid destination; full release validation remains required before the next 50-commit push.

**Caption export error checkpoint (2026-10-19):** Invalid or inaccessible caption destinations now become visible Caption workspace status failures instead of escaping local SRT export. Focused transcription coverage includes the invalid-destination contract; full release validation remains required before the next 50-commit push.

**Photo source integrity checkpoint (2026-10-19):** Photo base and overlay sources now capture local size/write stamps at import and reject preview export, queued export or project save after replacement, preventing non-destructive edit state from being rendered against changed path bytes. Focused photo export coverage includes replacement detection; full release validation remains required before the next 50-commit push.

**Local AI error checkpoint (2026-10-19):** Ordinary IO and permission failures during offline sample preparation now become visible AI workspace status failures instead of escaping the optional-AI command. Focused desktop AI coverage includes the IO failure contract; full release validation remains required before the next 50-commit push.

**Timeline export error checkpoint (2026-10-19):** Argument and permission failures raised by a queued timeline exporter now flow through the Video workspace status boundary. Focused desktop coverage includes queue-propagated invalid-destination failure; full release validation remains required before the next 50-commit push.

**Export history retention checkpoint (2026-10-19):** The shared export queue now retains only the newest 100 terminal/history entries, preventing long-running editor sessions from accumulating an unbounded in-memory job list while preserving recent status and retry records. Focused queue coverage verifies eviction; full release validation remains required before the next 50-commit push.

**Preview permission checkpoint (2026-10-19):** Preview-proxy permission failures now use the same visible Video workspace error status as decode and FFmpeg failures. Focused desktop coverage exercises a permission-denied proxy runner; full release validation remains required before the next 50-commit push.

**Gemini upload cleanup checkpoint (2026-10-19):** Temporary Files API cleanup after an invalid provider URI now uses an independent bounded timeout instead of the caller's potentially canceled token, so cancellation cannot skip deletion of an already-uploaded sample. Existing provider cleanup coverage remains green; full release validation is required before the next 50-commit push.

**AI caption execution checkpoint (2026-10-19):** Approved `caption.add` actions now use the caption font explicitly selected in the Video workspace instead of incorrectly requiring the optional title-brand font map. Plans fail clearly without a selected caption font, and success remains one undoable transaction; focused executor coverage includes both paths.

**AI caption provenance checkpoint (2026-10-19):** `verified-transcript` AI captions now require imported source-timed transcript evidence in both plan validation and transactional execution; ungrounded generated copy must use `authored-caption` instead. Focused validator/executor coverage includes the rejection paths.

**Caption model boundary checkpoint (2026-10-19):** `TimedCaption` now rejects undefined provenance enum values before they can enter the editable timeline or be persisted again. Focused caption-model coverage includes the malformed provenance regression.

**Core boundary hardening checkpoint (2026-10-19):** `GeminiClientOptions` now honors its optional timeout/retry-delay parameters with bounded two-minute/one-second defaults, and `MediaSourceMetadata` rejects negative durations. Focused Gemini coverage passes 12/12 and focused Foundation coverage passes 25/25; the next full Release gate remains required before the next 50-commit push.

**Export queue integrity checkpoint (2026-10-19):** Export job cancellation now preserves terminal success/failure state under repeated or late cancellation, with synchronized state and completion transitions. The full local Release gate passes 310 unit tests and 2 golden tests, a warning-free build, format verification, repository validation and `git diff --check`; this evidence remains local until the exact 50-commit push boundary.

**Persistence boundary checkpoint (2026-10-19):** Cache indexes now reject traversal and normalize corrupt entries as invalid data; project snapshots reject duplicate media IDs; photo edit state restores only against its verified source media ID. Focused Recovery coverage passes 7/7, ProjectRepository coverage 7/7 and PhotoProjectPersistence coverage 10/10. Full Release validation remains required after this batch.

**Timeline/recipe persistence checkpoint (2026-10-19):** Timeline JSON now rejects unsupported schemas, oversized collections and incomplete title animations at deserialization; AI recipe loading normalizes invalid names/plans to `InvalidDataException`; color preset loading rejects null entries. Focused TimelineProjectPersistence coverage passes 11/11, AiRecipeStore 4/4 and ColorPreset 3/3. Full Release validation remains required before pushing.

**Timeline nested-bound checkpoint (2026-10-19):** Timeline persistence now also bounds nested caption words, speed points and animation keyframes, and rejects invalid beat-marker values. The focused TimelineProjectPersistence suite remains green at 11/11 after the regression correction; the next exact 50-commit gate is still the push boundary.

**Gemini settings persistence checkpoint (2026-10-19):** The selected supported Gemini model now round-trips through the existing OS-backed credential store alongside the key and is removed on clear; whitespace-normalized model IDs remain allowlisted. Focused GeminiConfiguration coverage passes 3/3; live provider acceptance remains separate.

**Normalized-path boundary checkpoint (2026-10-19):** Cache and project media references now validate rooted paths after trimming and separator normalization, closing whitespace-prefixed absolute-path bypasses. Focused Recovery coverage passes 7/7; full Release validation is reserved for the exact 50-commit boundary.

**50-commit boundary preparation checkpoint (2026-10-19):** Timeline beat-marker ingestion now rejects non-finite or out-of-range waveform peaks before mutating editor state; focused TimelineEditor coverage passes 17/17. The next commit reaches the owner-requested exact 50-commit local batch; full Release validation and hosted revalidation remain required before any completion claim.

**Latest hosted 50-commit gate (2026-10-19):** Quality Gates run `39168203239` passed for `742cd38` across documentation/schema validation, Ubuntu, Windows and macOS restore/build/test, native media, headless shell/UI automation, formatting, portable archives, installer/DMG/DEB validation, packaged relaunch and checksums. This confirms the pushed batch; the six tracked partial P0 gates remain open because manual visual/HiDPI/screen-reader, legal redistribution, signing/clean-host and final-audit evidence are still required.

**Exact-50 integrity Release gate (2026-10-19):** The pushed 50-commit batch after `742cd38` passes 314 unit tests and 2 golden tests, a warning-free Release build, format verification, repository validation and `git diff --check`; hosted run `39172309590` confirms the same batch on Windows, macOS and Linux. The six tracked partial P0 gates remain open.

**FT-1102 motion-frame integrity checkpoint (2026-10-19):** `MotionFrame` now returns a defensive pixel snapshot while the tracker reads an internal non-allocating memory view, preventing caller mutation from changing motion-analysis input without adding per-pixel allocations. Focused motion coverage includes the mutation regression; manual visual-quality acceptance remains open.

**Foundation capability-report integrity checkpoint (2026-10-19):** `CapabilityReport` now snapshots capability entries at construction, preventing downstream mutation from changing dependency status or summary text. The focused Foundation suite covers the regression; release and manual platform gates remain distinct.

**Timeline seek-result integrity checkpoint (2026-10-19):** `TimelineSeekResult` now snapshots synchronized track samples at construction, protecting seek/selection state from caller-owned collection mutation. Focused timeline coverage includes the regression; manual view-synchronization acceptance remains open.

**Timeline filter-graph integrity checkpoint (2026-10-19):** `TimelineFilterGraph` now snapshots its media inputs at construction, preserving the exact FFmpeg input set after caller-owned collection mutation. Focused compositor coverage includes the regression; native export and manual acceptance remain separate gates.

**AI validation-result integrity checkpoint (2026-10-19):** `AiPlanValidationResult` now snapshots validation errors at construction, preserving the exact review/rejection explanation after caller-owned collection mutation. Focused AI-plan coverage includes the regression; provider/live-key acceptance remains open.

**AI validation-context integrity checkpoint (2026-10-19):** `AiPlanValidationContext` now freezes its target allowlist at construction, preventing later caller mutation from changing which project objects a reviewed plan may address. Focused AI-plan coverage includes the regression; live-provider acceptance remains open.

**AI execution-result integrity checkpoint (2026-10-19):** `AiPlanExecutionResult` now snapshots action outcomes at construction, preserving the transactional apply/rollback audit shown to reviewers. Focused executor coverage includes the regression; live-provider acceptance remains open.

**Timeline UI-state integrity checkpoint (2026-10-19):** Timeline rows, beat markers and restored/imported captions now expose read-only snapshots from the desktop view model, preventing binding consumers from mutating editable state outside commands. Focused timeline-editor coverage includes the regression; native UI acceptance remains open.

**AI review-state integrity checkpoint (2026-10-19):** Validated proposal actions now expose a read-only snapshot from the desktop view model, preserving review/approval state until the explicit approval or clear commands run. Focused AI-review coverage includes the regression; native UI acceptance remains open.

**Desktop media-state integrity checkpoint (2026-10-19):** Imported media and missing-timeline-media collections now expose read-only snapshots, preventing binding consumers from mutating selection/relink state outside the import and recovery commands. Focused desktop import coverage includes the regression; native UI acceptance remains open.

**Desktop workspace-state integrity checkpoint (2026-10-19):** Workspace catalog results are now snapshotted at the shell boundary, preserving the four-workspace navigation model from caller-owned collection mutation. Focused Foundation coverage includes the regression; native UI acceptance remains open.

**Photo recovery-state integrity checkpoint (2026-10-19):** Missing-media recovery results now expose a read-only snapshot, preserving relink selection state until the explicit recovery commands run. Focused photo-recovery coverage includes the regression; native UI acceptance remains open.

**Imported-media view integrity checkpoint (2026-10-19):** Imported media view models now defensively copy thumbnail bytes and waveform peaks, preventing downstream preview or analysis consumers from mutating the media-bin snapshot. Focused desktop import coverage includes the regression; native UI acceptance remains open.

**Timeline row integrity checkpoint (2026-10-19):** Nested timeline clip rows now expose read-only snapshots alongside the protected outer row collection, preventing binding mutation of the visible timeline rail. Focused timeline-editor coverage includes the regression; native UI acceptance remains open.

**Batch-export integrity checkpoint (2026-10-19):** Timeline batch export now snapshots validated targets before asynchronous work, preserving target ordering and completion accounting if a caller mutates its source collection during export. Focused batch-export coverage includes the regression; native interruption acceptance remains open.

**Timeline row-contract integrity checkpoint (2026-10-19):** `TimelineTrackRowViewModel` now snapshots caller-provided clip rows while preserving its public deconstructor and string representation. Focused timeline-editor coverage includes the regression; native UI acceptance remains open.

**Gemini settings-state integrity checkpoint (2026-10-19):** The optional Gemini model selector now exposes a read-only catalog snapshot, preventing UI consumers from mutating supported-model state. Focused desktop AI coverage includes the regression; live-provider acceptance remains open.

**Timeline preview-state integrity checkpoint (2026-10-19):** Timeline preview PPM bytes are now copied on assignment and retrieval, preventing UI consumers from mutating cached preview data or decoder-owned buffers. Focused desktop preview coverage includes the regression; native preview acceptance remains open.

**Cache-index integrity checkpoint (2026-10-19):** Cache index entries now expose an actual read-only collection view instead of a mutable array behind `IReadOnlyCollection`, preserving recovery metadata from caller mutation. Focused recovery coverage includes the regression; clean-host release acceptance remains open.

**Cache-eviction integrity checkpoint (2026-10-19):** Cache eviction now returns an actual read-only result snapshot, preserving the recorded eviction set from caller mutation. Focused recovery coverage includes the regression; clean-host release acceptance remains open.

**50-commit push boundary checkpoint (2026-10-19):** The requested local batch is complete at exactly 50 commits ahead of `origin/main`. The final local Release gate passes 271 unit tests and 2 golden tests, a warning-free Release build, format verification, repository validation and `git diff --check`; hosted revalidation is the next required evidence.

**GitHub issue inventory checkpoint (2026-10-19):** Before the completed E00-E06 epic closures, a live `gh issue list` audit reported 55 closed issues and 27 open issues. The current inventory is recorded in the GitHub epic-closure checkpoint above; no partial release issue is closed from local headless evidence alone.

**FT-1102 small-frame tracker checkpoint (2026-10-19):** Translation tracking now bounds its search radius to each decoded frame and compares valid overlap using normalized error, so small media no longer silently produces zero samples. The regression suite passes 4/4 motion tests with format and diff checks clean; the local fix is unpushed until the next exact 50-commit boundary, and FT-1102 remains open for manual visual-quality acceptance.

**FT-1102 confidence-status checkpoint (2026-10-19):** Timeline motion analysis now reports the measured mean confidence alongside translation and compensation, making low-confidence tracking visible without changing the editable stabilization path. Focused desktop/motion coverage passes 5/5 with format and diff checks clean; the local change remains unpushed until the next exact 50-commit boundary.

**FT-1102 motion-validation checkpoint (2026-10-19):** Motion-vector, tracker and stabilization validation now identify the exact invalid argument at their trust boundaries. Focused motion coverage passes 5/5; FT-1102 remains open for manual visual-quality acceptance and the local change remains unpushed until the next exact 50-commit boundary.

**Post-motion Release gate checkpoint (2026-10-19):** The local post-boundary changes pass 273 unit tests and 2 golden tests, build with 0 warnings/errors, pass `dotnet format --verify-no-changes`, repository validation and `git diff --check`. This evidence remains local until the next exact 50-commit push; the six partial P0 gates remain open.

**FT-1102 stabilization-boundary checkpoint (2026-10-19):** FFmpeg search-radius normalization now has explicit coverage for the logical 1, 16, 17 and 64 boundaries plus invalid 0/65 inputs. Focused compositor coverage passes 9/9; the local test change remains unpushed until the next exact 50-commit boundary.

**FT-1102 stabilization-snapshot checkpoint (2026-10-19):** Computed stabilization transforms are now copied into the plan, preventing later caller mutation from changing an already-created edit result. Focused motion coverage passes 6/6 with format and diff checks clean; the local change remains unpushed until the next exact 50-commit boundary.

**FT-1102 scope-snapshot checkpoint (2026-10-19):** Validated histogram, waveform and vectorscope bins are now copied into `ColorScopeData`, preventing caller mutation from changing an inspector scope after analysis. Focused scope coverage passes 3/3 with format and diff checks clean; the local change remains unpushed until the next exact 50-commit boundary.

**FT-1102 immutable-view checkpoint (2026-10-19):** Scope bins and stabilization transforms now expose actual read-only collection views, not mutable arrays behind `IReadOnlyList` interfaces. Focused motion/scope coverage passes 9/9 with format and diff checks clean; the local change remains unpushed until the next exact 50-commit boundary.

**FT-1104 rough-cut-snapshot checkpoint (2026-10-19):** Rough-cut waveform samples and planned segments now expose actual read-only snapshots, preventing caller mutation from changing a reviewed AI rough-cut. Focused rough-cut coverage passes 3/3 with format and diff checks clean; the local change remains unpushed until the next exact 50-commit boundary.

**FT-1104 rough-cut-validation checkpoint (2026-10-19):** Rough-cut source validation now reports `durationFrames` for non-positive durations instead of misidentifying `timelineStartFrame`. Focused rough-cut coverage passes 4/4 with format and diff checks clean; this local fix remains unpushed until the next exact 50-commit boundary.

**Core validation checkpoint (2026-10-19):** Color scopes, motion frames, export presets and timeline render plans now report the specific invalid width, height or duration parameter at their trust boundaries. The UnitTests project passes 281/281 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**FT-602 caption-validation checkpoint (2026-10-19):** Caption style validation now reports `marginBottom` and `maximumCharactersPerLine` independently from `fontSize`. Focused caption coverage passes 4/4 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**FT-401 photo-validation checkpoint (2026-10-19):** Social photo export presets and photo viewport validation now report the specific invalid width or height parameter. Focused photo coverage passes 7/7 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**Domain-validation checkpoint (2026-10-19):** Rational frame rates, media metadata and project snapshots now report the exact invalid denominator, height or canvas-height field at their trust boundaries. Focused domain coverage passes 1/1 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**Timeline-validation checkpoint (2026-10-19):** Timeline clips, audio fades and titles now report the exact invalid source/timeline range, fade-out or duration field. Focused timeline coverage passes 9/9 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**Core snapshot checkpoint (2026-10-19):** Project media references, timeline tracks/clips/layers and render-plan sources/collections now expose immutable snapshots, preventing caller mutation from changing persisted or rendered edit state. Focused snapshot coverage passes 19/19 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**FT-401 photo-source checkpoint (2026-10-19):** Photo documents now defensively copy imported preview bytes and return a fresh preview snapshot, preventing caller mutation from altering non-destructive source edits. Focused photo edit coverage passes 9/9 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**Media import snapshot checkpoint (2026-10-19):** Imported thumbnails and waveform peaks now use defensive snapshots, preventing downstream mutation from changing imported analysis. Focused media-import coverage passes 5/5 with format and diff checks clean; the local fix remains unpushed until the next exact 50-commit boundary.

**AI/photo state-integrity checkpoint (2026-10-19):** Approved AI actions, local analysis payloads, reference samples, caption words, transcript words, photo edit history, layer masks and raster pixels now expose defensive snapshots at mutation boundaries. Focused AI/reference coverage passes 13/7/6 and focused photo coverage passes 21; format and diff checks are clean. The local changes remain unpushed until the next exact 50-commit boundary.

**50-commit batch integrity checkpoint (2026-10-19):** Imported captions, color presets, AI operation catalogs and export-queue history now also expose read-only snapshots. The final focused queue coverage passes with format and diff checks clean; this commit completes exactly 50 verified local commits ahead of `origin/main`. Full Release validation and the push are the next actions.

**Post-rough-cut Release gate checkpoint (2026-10-19):** The local changes pass 277 unit tests and 2 golden tests, build with 0 warnings/errors, pass `dotnet format --verify-no-changes`, repository validation through `py scripts/validate_repository.py` and `git diff --check`. The validator correctly remains blocked by six tracked partial P0 tasks; this is not a V1 completion claim. The local commits remain unpushed until the next exact 50-commit boundary.

**Workspace navigation affordance checkpoint (2026-10-19):** The adaptive shell's Ctrl+1 through Ctrl+4 workspace shortcut hint now has an explicit accessible tooltip explaining each destination. Focused production-shell coverage and format/diff checks pass; native keyboard acceptance remains unverified.

**Media import affordance checkpoint (2026-10-19):** The shared media-bin drop surface now explains supported local media, the explicit Import action and the local-only default. Focused production-shell coverage and format/diff checks pass; actual native drag/drop acceptance remains unverified.

**Video/export affordance checkpoint (2026-10-19):** Local motion analysis, export retry, approved/custom watermark selection, watermark clearing and timeline viewport scrolling now explain their scope and side effects. Focused production-shell coverage and format/diff checks pass; native media and hosted release gates remain separate.

**Full Release gate checkpoint (2026-10-19):** The 40-commit local batch passes 271 unit tests and 2 golden tests, builds with 0 warnings/errors, and passes format verification, repository validation and `git diff --check`. The validator remains correctly blocked by six tracked partial P0 tasks; this is not a V1 completion claim.

**AI recipe affordance checkpoint (2026-10-19):** AI proposal clearing now states that the project is unchanged, while recipe naming, save and load actions explain local reviewability and that loading never auto-applies a plan. Focused production-shell coverage and format/diff checks pass; optional Gemini remains a bounded planning layer.

**Photo affordance checkpoint (2026-10-19):** Photo overlay import/clear, circular masking, clone retouch and exposure-grade actions now expose concise effect guidance in the inspector. The focused production-shell test and format/diff checks pass; the source remains non-destructive and native visual acceptance remains open.

**Full Release gate checkpoint (2026-10-19):** The 35-commit local batch passes 271 unit tests and 2 golden tests, builds with 0 warnings/errors, passes `dotnet format --verify-no-changes`, repository validation and `git diff --check`. The validator correctly retains six tracked partial P0 gates; application, native-media, manual and hosted release acceptance remain distinct.

**Audio and caption affordance checkpoint (2026-10-19):** Audio mute, gain, fades, ducking, title animation, SRT import/export, verified transcript import, Gemini transcription consent and local Whisper actions now expose concise side-effect guidance. The focused production-shell test and format/diff checks pass; native and hosted acceptance remain separate.

**Timeline affordance checkpoint (2026-10-19):** Timeline analysis, navigation, clip editing, animation, speed-ramp and stabilization actions now expose concise guidance tooltips alongside their automation names. The focused production-shell test and format/diff checks pass; the full Release gate remains scheduled at the next batch checkpoint.

**Release gate checkpoint (2026-10-19):** After the latest photo recovery/relink inspector refinement, the full local Release gate passes 271 unit tests and 2 golden tests, builds with 0 warnings/errors, and passes `dotnet format --verify-no-changes`, repository validation and `git diff --check`. The six tracked partial P0 gates remain open; no hosted revalidation is claimed before the requested 50-commit push.

**FT-901 photo-inspector grouping checkpoint (2026-10-19):** The Photo mask-and-retouch inspector now presents separate `OVERLAY MASK` and `CLONE RETOUCH` groups, reducing control density without changing edit behavior. Headless production-shell coverage verifies both groups are reachable and the full Release gate passes 271 unit tests and 2 golden tests with a warning-free build, format verification, repository validation and `git diff --check`. Native visual and screen-reader acceptance remains open.

**FT-901 photo-field checkpoint (2026-10-19):** Mask and clone-retouch numeric inputs now keep compact persistent labels beside their default values, so Center X/Y, Radius, Strength and source offsets remain understandable without relying on placeholder text. Focused production-shell coverage and formatting verification pass; edit parsing and persistence are unchanged.

**FT-901 color-guidance checkpoint (2026-10-19):** Color style selection and visual-reference matching controls now expose concise tooltips for loaded-style choice, source/reference semantics and the exact Gemini upload-consent boundary. The color reference consent also has a stable automation name; focused production-shell coverage and formatting verification pass.

**FT-901 motion-field checkpoint (2026-10-19):** Timeline speed-ramp and stabilization inputs now keep persistent compact labels and range guidance beside their default values, improving scanability in the Motion and speed inspector without changing the underlying timeline commands. Focused production-shell coverage and formatting verification pass.

**Release gate checkpoint (2026-10-19):** After the photo, color-reference and timeline motion UI refinements, the full local Release gate passes 271 unit tests and 2 golden tests, the solution builds with 0 warnings/errors, `dotnet format --verify-no-changes`, repository validation and `git diff --check` pass. No hosted revalidation is claimed before the requested 50-commit push.

**FT-901 action-hierarchy checkpoint (2026-10-19):** The reviewed waveform rough-cut action now uses the production primary-action treatment and explains that it applies editable timeline operations, matching the established export/apply hierarchy. Focused production-shell coverage and formatting verification pass.

**FT-901 photo-grade checkpoint (2026-10-19):** The Photo Grade inspector now labels its default exposure value explicitly as `Exposure EV` and explains the non-destructive layer semantics through the field tooltip. Focused production-shell coverage and formatting verification pass.

**FT-901 photo-export checkpoint (2026-10-19):** The Photo Export inspector now labels format, JPEG quality and output dimensions beside their live controls, keeping the compact export surface understandable without relying on placeholder or option text. Focused production-shell coverage and formatting verification pass; export behavior is unchanged.

**FT-901 color-style checkpoint (2026-10-19):** The Color Style library inspector now labels the style-name and loaded-style selectors even when the local library is empty, preserving discoverability in the compact shell without changing preset persistence or application behavior. Focused production-shell coverage and formatting verification pass.

**FT-901 AI-guidance checkpoint (2026-10-19):** The optional Gemini workspace now labels provider, API-key, and recipe fields, and exposes scope-specific consent guidance for video frames, audio waveform and photo preview samples. Stable automation names and focused production-shell coverage pass; secure storage and upload-consent semantics are unchanged.

**Release gate checkpoint (2026-10-19):** The full Release gate after the latest inspector refinements passes 271 unit tests and 2 golden tests, builds with 0 warnings/errors, and passes format verification, repository validation and `git diff --check`. These 18 local commits remain unpushed pending the owner-requested 50-commit boundary.

**FT-901 launch-smoke checkpoint (2026-10-19):** The managed Windows desktop shell smoke launches the current production app and detects the real title `Frame Theory Studio AI` after the latest UI refinements. This verifies startup only; native visual, HiDPI and screen-reader acceptance remain open.

**FT-901 timeline-guidance checkpoint (2026-10-19):** Timeline project relink and output-framing controls now keep persistent labels and trust-boundary guidance beside their live inputs, making missing-media recovery and social framing discoverable in the compact inspector. Focused production-shell coverage and formatting verification pass; relink/export behavior is unchanged.

**FT-901 caption-guidance checkpoint (2026-10-19):** The Titles and captions inspector now separates the title field from animation actions and the optional Gemini transcription consent row, with kinetic-caption and licensed-font guidance exposed through tooltips. Focused production-shell coverage and formatting verification pass; caption provenance and upload consent behavior are unchanged.

**FT-901 reference-guidance checkpoint (2026-10-19):** Color reference matching now keeps visible `Source` and `Reference` labels beside its thumbnail selectors, removing ambiguity from the review-before-apply flow while preserving explicit upload consent. Focused production-shell coverage and formatting verification pass.

**FT-901 AI-review checkpoint (2026-10-19):** The AI proposal inspector now labels Plan JSON and natural-language Revision inputs explicitly, preserving the schema-validation and review-before-apply boundary while making the workflow readable when fields are empty. Focused production-shell coverage and formatting verification pass.

**FT-901 photo-layer checkpoint (2026-10-19):** Photo overlay blend mode and opacity now use persistent labels alongside their live controls and opacity readout, making the non-destructive layer inspector scannable at compact widths. Focused production-shell coverage and formatting verification pass; overlay rendering and persistence are unchanged.

**Release gate checkpoint (2026-10-19):** The full Release gate after the latest AI, Color, Photo and Timeline inspector refinements passes 271 unit tests and 2 golden tests, builds with 0 warnings/errors, and passes format verification, repository validation and `git diff --check`. No hosted revalidation is claimed before the requested 50-commit push.

**FT-901 timeline-analysis checkpoint (2026-10-19):** The waveform analysis selector now has a persistent `Detection` label beside its local mode choices, reducing ambiguity in the compact rough-cut inspector without changing beat-marker behavior. Focused production-shell coverage and formatting verification pass.

**FT-901 status-guidance checkpoint (2026-10-19):** Timeline quick-save and quick-export readouts now expose concise tooltips for their live local status values, matching the editor’s existing accessible status treatment. Focused production-shell coverage and formatting verification pass.

**FT-901 playback-guidance checkpoint (2026-10-19):** The timeline READY/PLAYING badge now explains its local preview state through the same accessible tooltip path as transport controls. Focused production-shell coverage and formatting verification pass.

**FT-901 relink-guidance checkpoint (2026-10-19):** The timeline relink action now explains its contained-project validation boundary beside the labeled missing-media recovery controls. Focused production-shell coverage and formatting verification pass; source-integrity enforcement is unchanged.

**FT-901 photo-recovery checkpoint (2026-10-19):** Photo project recovery now labels project/replacement paths and explains missing-media selection, safe open/recover and contained relink actions in the inspector. Focused production-shell coverage and formatting verification pass; recovery and source-integrity behavior are unchanged.

**FT-901 workspace-render checkpoint (2026-10-19):** The production Skia-backed headless shell now captures all four Video, Photo, Color and AI workspaces at the compact 960×640 surface after real tab selection. Focused render coverage passes 2/2; the full Release suite passes 271 unit tests and 2 golden tests with format verification, repository validation and `git diff --check` green. This strengthens responsive-render evidence but does not replace native visual or screen-reader review.

**FT-901 rendered-frame checkpoint (2026-10-19):** The test-only Avalonia shell harness now uses the production Skia renderer to capture the real `MainWindow` at 1280×800 standard scale, 960×640 compact scale and 1920×1200 at 1.5× HiDPI, proving the rendered surface remains available at the supported responsive bounds and scale. The managed Release desktop launch smoke also detected `Frame Theory Studio AI`. The full Release suite passes 270 unit tests and 2 golden tests with a warning-free build, format verification, repository validation and `git diff --check`; this strengthens evidence but does not replace native visual or screen-reader review.

**Hosted AI-undo validation checkpoint (2026-10-18):** Quality Gates run `39160407760` for pushed commit `f08c80c` passed documentation/schema validation, Windows/macOS/Linux restore/build/test, native media smoke, formatting, headless shell, Windows UI automation, packaging, relaunch and checksum verification. The AI undo-affordance implementation is hosted-green; manual AI workspace acceptance remains open.

**FT-902 AI-undo-affordance checkpoint (2026-10-18):** The production AI review panel now disables `Undo last AI apply` until a successful transactional AI edit group exists, exposes timeline AI-history availability through `CanUndoAiEdit`, and clears the affordance after undo. Executor/model and headless shell coverage pass; the full Release suite passes 269 unit tests and 2 golden tests with a warning-free build, format verification, repository validation and `git diff --check`. Manual AI workspace acceptance remains open.

**FT-902 AI-approval affordance checkpoint (2026-10-18):** The production AI review panel now disables `Apply approved actions` until a validated proposal contains at least one explicitly checked action, then updates enablement through the existing observable approval state without bypassing validation. Model and headless UI coverage pass; the full Release suite passes 269 unit tests and 2 golden tests with a warning-free build, format verification, repository validation and `git diff --check`. Manual AI workspace acceptance remains open.

**FT-902 photo undo-affordance checkpoint (2026-10-18):** Photo history now exposes real `CanUndo`/`CanRedo` state through the desktop view model, and the production Photo workspace disables unavailable Undo/Redo actions while preserving updates after execute, undo, redo and redo-branch invalidation. Focused history and shell tests pass; the full Release suite passes 269 unit tests and 2 golden tests with a warning-free build, format verification, repository validation and `git diff --check`. This strengthens undo affordance evidence but does not replace manual native workspace acceptance.

**FT-902 selection-synchronization checkpoint (2026-10-18):** Headless production-shell coverage now clicks a real timeline video clip and verifies that the imported-media list selects the same media, the timeline view model exposes the selected media ID, the selection inspector preserves the current playhead on selection, and explicit seek/step operations update the visible playhead text and refreshed selected-clip automation affordance. The focused selection/timeline tests pass; the full Release suite passes 268 unit tests and 2 golden tests with a warning-free build, format verification, repository validation and `git diff --check`. This strengthens implementation evidence but does not close FT-902 because manual view synchronization and cross-platform acceptance remain open.

**FT-902 color-preview synchronization checkpoint (2026-10-18):** A second production-shell test imports distinct synthetic thumbnails, selects each timeline clip and verifies that the shared Color inspector before-preview follows the selected source pixels. The full Release suite passes 268 unit tests and 2 golden tests with a warning-free build, format verification, repository validation and `git diff --check`; manual cross-platform view acceptance remains open.

**Compact workspace-rail checkpoint (2026-10-18):** Below the 1120px window breakpoint, workspace navigation now keeps stable tab geometry while hiding secondary descriptors and shortcut numerals and centering the approved workspace badges, reducing compact-shell noise without destabilizing Avalonia timeline realization or removing automation names/tooltips. The focused adaptive-shell and timeline-layout tests pass; the full Release suite passes 266 unit tests and 2 golden tests, the managed desktop shell smoke reports `Frame Theory Studio AI`, and the warning-free build, format verification, repository validation and `git diff --check` pass. This strengthens FT-901/FT-902 implementation evidence but does not replace native visual, HiDPI, screen-reader or cross-platform manual acceptance.

**Hosted compact-inspector validation checkpoint (2026-10-18):** Quality Gates run `39157738274` for pushed commit `88aae80` passed documentation/schema validation plus Windows, macOS and Linux restore/build/test, native FFmpeg smoke, formatting, headless shell, Windows UI automation, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksum verification. It validates the responsive Photo overlay controls and stacked preview cards, bounded Gemini model selector and adaptive activity log, plus the restored hosted-green Windows shortcut smoke contract. Manual native visual, 1280x800/HiDPI and screen-reader acceptance remains open; no partial GitHub task is closed from automated evidence alone.

**Responsive photo-preview checkpoint (2026-10-18):** Photo original/edited comparison cards now stack below 620px of measured card width and return to a two-up layout above it, preserving readable previews in narrow inspectors without changing source or edit behavior. Foundation coverage verifies both arrangements.

**Responsive Gemini-provider checkpoint (2026-10-18):** The optional Gemini model selector now uses a 150–300px bounded stretch with explicit model-selection guidance, keeping provider setup usable in compact inspectors without changing secure-key or consent behavior. Foundation coverage verifies the geometry and accessible tooltip.

**Responsive photo-overlay checkpoint (2026-10-18):** Photo Layers and blend controls now give overlay opacity and blend-mode inputs bounded responsive widths, preventing fixed controls from crowding the compact inspector. Foundation coverage verifies stretch alignment, bounds and blend-mode guidance; the change is local and non-destructive.

**Hosted shortcut-smoke regression checkpoint (2026-10-18):** Quality Gates run `39156858822` passed documentation/schema validation and the macOS/Linux restore, build, test, media-smoke, packaging and checksum gates, but Windows UI automation failed at `Ctrl+2` during the custom Win32 `SendInput` helper. The application build and shell smoke passed; commit `65f1d0d` restores the previously hosted-green `System.Windows.Forms.SendKeys` path. The local host also rejects `SendKeys` at `Ctrl+1` with `The operation completed successfully`, so the next exact-ten push must revalidate the full Windows gate before this checkpoint can be considered green. No task is closed from the failed run.

**Responsive recovery-path checkpoint (2026-10-18):** Timeline missing-media recovery now bounds the replacement-path input to 220–360px and lets it stretch within the project inspector, preventing long path placeholders from forcing compact layouts wider. Foundation coverage verifies the geometry and editable recovery control.

**Responsive export-framing selector checkpoint (2026-10-18):** Video export aspect-ratio selection now uses a readable 150–220px bounded stretch, preserving label clarity without imposing a fixed-width inspector control. Foundation coverage verifies the geometry and existing left-aligned content contract.

**Responsive beat-analysis selector checkpoint (2026-10-18):** The timeline’s waveform beat-detection selector now uses a 120–180px bounded stretch instead of a fixed width, keeping audio-analysis controls usable in compact inspector flows. Foundation coverage verifies its geometry and accessible name.

**Local UI batch gate checkpoint (2026-10-18):** The current responsive-shell and Windows UI-smoke batch passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. The validator now accepts either the legacy SendKeys or the hardened SendInput shortcut path while still requiring the all-workspace loop. Hosted revalidation is deferred until the next exact-ten push.

**Responsive color-style selector checkpoint (2026-10-18):** The loaded Color style-library selector now uses a 140–220px bounded stretch instead of a fixed 220px width, keeping reusable-style controls aligned in compact inspectors. Foundation coverage verifies the geometry and accessible name.

**Responsive media-header checkpoint (2026-10-18):** The media-bin header now keeps title, selected asset and asset count aligned in one row at standard width, then moves the selected-media chip to a full-width second row below 1120px. Foundation coverage verifies both arrangements and preserves selection semantics; native visual and HiDPI acceptance remains open.

**Media-search spacing checkpoint (2026-10-18):** The responsive media search row now keeps a six-pixel gap between search, type-filter and clear controls in both its standard and compact arrangements. Foundation coverage verifies the spacing contract alongside the 1280px/960px row layouts.

**Responsive media-search checkpoint (2026-10-18):** The media-bin search controls now use a single compact row at standard width and switch to full-width search plus a second filter/clear row below the 1120px breakpoint, preventing cramped controls in the 960px shell. Foundation coverage verifies both arrangements; native visual and HiDPI acceptance remains open.

**Windows UI smoke input checkpoint (2026-10-18):** `scripts/smoke_desktop_ui.ps1` uses the `System.Windows.Forms.SendKeys` path that passed hosted Windows UI automation in run `39155435600`. The script parses cleanly and the desktop shell title smoke passes locally; this host rejects `SendKeys` at `Ctrl+1`, so the full local UI smoke remains unverified while hosted Windows UI automation is authoritative. FT-901 remains a manual visual, HiDPI and screen-reader gate.

**Hosted compact-shell validation checkpoint (2026-10-18):** Quality Gates run `39155435600` for pushed commit `3208e9d` passed documentation/schema validation plus Windows, macOS and Linux restore/build/test, native FFmpeg smoke, formatting, headless shell, Windows UI automation, portable archive, installers/DMG/DEB, packaged relaunch and checksums. It validates the responsive media rail, compact workspace frame/footer, offline mode metadata, accessible action discovery and social-export hierarchy. Manual native visual, 1280x800/HiDPI and screen-reader acceptance remains open; no partial GitHub task is closed from automated evidence alone.

**Responsive width-lifecycle checkpoint (2026-10-18):** The media rail now uses the attached top-level window width when available, with a measured-layout fallback, preventing an early startup pass from applying the compact width to the standard shell. Foundation coverage remains green at 960px and 1280px.

**Responsive workspace-frame checkpoint (2026-10-18):** Each editor workspace now reduces outer margin and padding below the 1120px window breakpoint, preserving the dense media/inspector composition at the 960px minimum while retaining the spacious standard shell. Foundation coverage verifies standard and compact frame geometry across the production workspaces; native visual and HiDPI acceptance remains open.

**Responsive footer checkpoint (2026-10-18):** The bottom capability bar now hides the secondary diagnostics readout below the 1120px window breakpoint while retaining its tooltip/log path, keeping the primary capability status and refresh action readable at the 960px minimum. Attached-window initialization is covered by Foundation geometry tests; native visual and HiDPI acceptance remains open.

**Mode-discovery checkpoint (2026-10-18):** The global `LOCAL MODE · GEMINI OPTIONAL` badge and each workspace mode badge now expose accessible names and explicit offline/consent guidance, making the offline-first boundary visible without relying on color alone. Foundation coverage verifies the shell metadata; live screen-reader acceptance remains open.

**Local responsive-shell gate checkpoint (2026-10-18):** The UI follow-up batch passes `dotnet build FrameTheory.sln --configuration Release --no-restore --warnaserror` with 0 warnings/errors, 266 unit tests, 2 golden tests, repository/schema validation, `dotnet format --verify-no-changes` and `git diff --check`. Hosted revalidation is intentionally deferred until the next exact-ten push; native visual, HiDPI and screen-reader acceptance remains open.

**Accessible input-discovery checkpoint (2026-10-18):** Editable fields that receive an accessible name now also receive a fallback tooltip when no control-specific guidance is configured. Existing secure-key, consent and workflow tooltips remain authoritative; the focused Foundation shell suite remains green.

**Export action hierarchy checkpoint (2026-10-18):** The multi-target social export action now uses the same production primary-action treatment as single-target timeline export, photo export and approved-edit actions. Foundation coverage verifies the reachable action class; export behavior and cancellation remain unchanged.

**Accessible action-discovery checkpoint (2026-10-18):** The production shell now gives every named button a fallback tooltip when no feature-specific guidance exists, while preserving explicit tooltips for media, export, AI and editing actions. Foundation coverage verifies the fallback on timeline editing controls; native screen-reader acceptance remains open.

**Responsive media-rail checkpoint (2026-10-18):** The shared Video/Photo/Color/AI workspace layout now reduces its local media rail from 300px to 240px below the 1120px window-width breakpoint, preserving inspector space at the 960px minimum while retaining the full rail on the standard 1280px shell. The layout exposes an accessible tooltip and Foundation coverage verifies both widths; native visual and HiDPI acceptance remains open.

**Hosted responsive editor follow-up checkpoint (2026-10-18):** Quality Gates run `39154109601` for pushed commit `53f9b73` passed documentation/schema validation plus Windows, macOS and Linux restore/build/test, native FFmpeg smoke, formatting, headless shell, Windows UI automation, portable archive, installers/DMG/DEB, packaged relaunch and checksums. It validates responsive Color scopes, Photo previews/export controls, AI/Color inspector inputs, monitor stacking, title input and photo undo/redo shortcuts. Manual native visual, 1280x800/HiDPI and screen-reader acceptance remains open; local native SendKeys smoke remains unverified on this host.

**Responsive photo-export checkpoint (2026-10-18):** Photo format, JPEG quality and social-dimension controls now use bounded responsive widths instead of fixed children, keeping export actions readable when the inspector wraps. Foundation coverage verifies each control’s compact geometry; native visual and HiDPI acceptance remains open.

**Responsive style-input checkpoint (2026-10-18):** Color style-library naming now uses a bounded 140–220px input that stretches with the inspector flow and explains its local-save purpose through a tooltip. Foundation coverage verifies the compact geometry and accessible name.

**Responsive title-input checkpoint (2026-10-18):** Timeline title entry now stretches within a bounded 160–360px inspector width and exposes a concise editable-title tooltip, preventing the compact captions panel from being dominated by a fixed input. Foundation coverage verifies its bounds and discoverability.

**Responsive monitor-stack checkpoint (2026-10-18):** The Video program monitor now stacks its action rail below the measured 520px compact-card threshold and restores the two-column monitor/action composition above it. Headless adaptive-shell coverage verifies both arrangements and preserves the live controls; native visual and HiDPI acceptance remains open.

**Keyboard edit-history checkpoint (2026-10-18):** Photo undo/redo now routes through standard `Ctrl/Cmd+Z`, `Ctrl/Cmd+Shift+Z` and `Ctrl/Cmd+Y` shortcuts according to the active workspace, while Video/AI preserve the existing approved-AI undo path. Empty-history calls return explicit status without mutation; Foundation coverage exercises the routing contract.

**Photo redo checkpoint (2026-10-18):** The existing non-destructive photo history now exposes a real `Redo photo edit` action beside undo, preserves the operation stack and reports an explicit empty-redo state. Persistence coverage exercises undo followed by redo before save/reopen, and the adaptive shell exposes the accessible control.

**Responsive inspector-input checkpoint (2026-10-18):** AI revision and recipe inputs now use bounded minimum/maximum widths, and Color reference selectors stretch within a 160–240px bound. The controls retain their existing commands while wrapping cleanly in compact inspector flows; the adaptive-shell contract verifies their geometry and automation names.

**Responsive photo-preview checkpoint (2026-10-18):** Photo original/edited canvases now stretch within their two-up comparison cards while preserving aspect ratio and a 640px bound, avoiding centered oversized children at compact widths. The adaptive-shell contract verifies both images; native visual and HiDPI acceptance remains open.

**Responsive scopes checkpoint (2026-10-18):** Color waveform and vectorscope previews now keep a 160–240px bounded width, preserve their generated aspect ratio and stretch within the inspector flow instead of forcing fixed 240px children. The adaptive-shell contract verifies both scope images; native visual and HiDPI acceptance remains open.

**Hosted responsive-shell validation checkpoint (2026-10-18):** Quality Gates run `39152462667` for pushed commit `afe2490` passed documentation/schema validation plus Windows, macOS and Linux restore/build/test, native FFmpeg smoke, formatting, headless shell, Windows UI automation, portable archive, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksum gates. The run validates the responsive project chrome, monitor action rail, fluid monitor/color previews, fluid grade controls, compact header behavior and workspace status density. Manual native visual, 1280x800/HiDPI and screen-reader acceptance remains open; local native SendKeys smoke remains unverified on this host.

**Workspace status-density checkpoint (2026-10-18):** The duplicated active-workspace status readout now caps at 240px beside the workspace title and mode badge, keeping narrow editor headers aligned while retaining the full status in its tooltip and global capability/status bar. The adaptive-shell contract verifies the compact cap; native visual and HiDPI acceptance remains open.

**P0 parser self-check checkpoint (2026-10-18):** Release validation now exercises a synthetic heading/status fixture before checking the repository, covering both `[P0]` and `[P0, depends ...]` epic forms and the partial-blocker rule. The real validator still passes and reports six partial P0 tasks; this protects the release audit from silently undercounting blockers.

**Responsive grade-control checkpoint (2026-10-18):** Color grade controls now use a fixed label column, a stretchable slider column and a fixed value column instead of a 180px slider row. The compact inspector can therefore shrink or wrap without clipping grade input; the adaptive-shell contract verifies unset slider width and stretch alignment.

**Responsive Color preview checkpoint (2026-10-18):** Color before/after preview cards now use fluid image widths with bounded 320px maximums and 160px card minimums, so the comparison wraps cleanly instead of imposing a 240px fixed child at compact widths. The adaptive-shell contract verifies both previews stretch without fixed widths; native visual and HiDPI acceptance remains open.

**Responsive monitor checkpoint (2026-10-18):** The program-monitor image no longer forces a 320×180 child into the responsive editor column. It now preserves aspect ratio while stretching to the available monitor surface with bounded 640×360 maximums, preventing minimum-window clipping; the adaptive-shell contract verifies unset fixed dimensions, stretch alignment and uniform scaling.

**Monitor action-rail checkpoint (2026-10-18):** The Video program monitor now keeps frame preview/playback transport in one row and places the output framing, quick-save and MP4 export controls under a separate `OUTPUT` label. This reduces mixed-action density without changing command wiring; the adaptive-shell contract verifies the live output label and existing export controls. Native visual acceptance remains open.

**Project chrome checkpoint (2026-10-18):** The top editor chrome now exposes a real `Current project title` bound to the timeline project lifecycle. It starts honestly as `Untitled project` and changes only after a successful local save or reopen, keeping the compact CapCut-inspired shell informative without presenting fabricated save state. Focused adaptive-shell coverage includes the title and tooltip; native visual acceptance remains open.

**Compact header checkpoint (2026-10-18):** The production shell now hides the non-essential workspace shortcut legend and tightens header padding below 1120 logical pixels, preventing the minimum 960x640 window from presenting a cramped top bar. The headless adaptive-shell test covers 1280-to-960-to-1280 transitions; focused Release test passes 1/1. Native visual and HiDPI review remains open.

**P0 audit guard checkpoint (2026-10-18):** `py scripts/validate_repository.py` now parses every `[P0]` epic section, requires each P0 task to have a status entry, requires every partial P0 task to appear in `docs/V1_ACCEPTANCE_AUDIT.md`, and rejects an unsupported V1-complete claim. The validator passes and reports six partial P0 tasks: FT-901, FT-902, FT-1002, FT-1003, FT-1005 and FT-1006. This is a release-audit safeguard, not task closure; manual, legal, clean-host and final-audit evidence remains open.

**Hosted production UI validation checkpoint (2026-10-18):** Quality Gates run `39150767355` for pushed commit `a4c8756` is green across documentation/schema validation, Windows/macOS/Linux restore/build/test, native FFmpeg smoke, Windows UI automation, formatting, headless shell, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksums. The run validates the production read-only feedback/readout controls and normalized editor input geometry from the preceding exact-ten batch. Manual native visual, 1280x800/HiDPI and screen-reader acceptance remains open; local native SendKeys smoke remains unverified on this host.

**Exact-ten production UI consistency checkpoint (2026-10-18):** The batch since pushed `60b8ff3` records hosted green evidence (`a7745a3`), unifies Gemini feedback readouts (`b64736f`), unifies Color feedback readouts (`beb6527`), unifies timeline feedback readouts (`b7be17e`), exposes timeline frame/position readouts (`5f65abb`), polishes timeline analysis feedback (`1e284f7`), normalizes editor input geometry (`c0d88c7`), polishes Color summary readouts (`1b226e8`), and exposes monitor timecode as a read-only readout (`b7db26f`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. Hosted revalidation is required after the mandated push; manual visual, 1280x800/HiDPI and screen-reader acceptance remains open.

**Hosted cross-workspace status validation checkpoint (2026-10-18):** Quality Gates run `39149567663` for pushed commit `60b8ff3` is green across documentation/schema validation, Windows/macOS/Linux restore/build/test, native FFmpeg smoke, Windows UI automation, formatting, headless shell, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksums. The run validates the UIA-safe capability, media import, photo, color, Gemini, AI workflow and recovery status controls from the preceding exact-ten batch. Manual native visual, 1280x800/HiDPI and screen-reader acceptance remains open; local native SendKeys smoke remains unverified on this host.

**Exact-ten cross-workspace status consistency checkpoint (2026-10-18):** The batch since pushed `7082802` records the prior hosted validation (`79da1a2`), exposes the capability strip as UIA-safe read-only controls (`93c5e4a`), clarifies media-import feedback (`9d88589`), polishes Photo editor status (`6ba5820`), polishes Color inspector status (`5d80473`), exposes Gemini provider and analysis statuses (`a94774a`), unifies AI revision and recipe statuses (`f7ad27c`), and polishes photo recovery/relink status (`4153d47`). Each live readout keeps its existing binding, uses muted borderless typography, and has Foundation coverage for read-only state, sizing and tooltips. The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. Hosted cross-platform/UIA revalidation is required after this mandated push; manual visual, 1280x800/HiDPI and screen-reader acceptance remains open.

**Hosted UI consistency validation checkpoint (2026-10-18):** Quality Gates run `39148469536` for pushed commit `7082802` is green across documentation/schema validation, Windows/macOS/Linux restore/build/test, native FFmpeg smoke, Windows UI automation, formatting, headless shell, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksums. The run validates the UIA-safe workspace/preview/status controls and the compact editor polish in the preceding exact-ten batch. Manual native visual, HiDPI and screen-reader acceptance remains open; local native SendKeys smoke remains unverified on this host.

**Exact-ten UI consistency and hosted UIA correction checkpoint (2026-10-18):** The batch since pushed `1e4623d` converts the active workspace status to a discoverable read-only control (`5af69b7`), exposes preview and motion status controls through the same UIA-safe path (`3b7a2d2`), adds selected-media context guidance (`ab72b50`), guards status automation names (`ce10539`), applies muted typography to live status readouts (`4d0f70c`), unifies timeline status styling (`5380fd7`), adds timeline status tooltips (`19f78ca`), makes the export selector responsive (`3c98e02`), and unifies the expanded project/export status cards (`59e49cb`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. Hosted run `39147581896` failed before this correction because Windows UIA could not find `Active workspace status`; macOS and Ubuntu jobs passed. A new hosted run after this exact-ten push is required, and native visual/HiDPI/screen-reader acceptance remains unverified. Local native SendKeys smoke remains unclaimed because this host's input surface fails before interaction.

**Exact-ten hosted-smoke correction and UI polish checkpoint (2026-10-18):** The batch since pushed `f123ca7` corrects the Windows UIA contract after hosted run `39146883732` exposed that the composite `Timeline action status` border was not surfaced as a native control (`5430b09`, `a1a0acc`), styles selected media as a compact chip (`e06e2d9`), surfaces export framing beside preview actions (`17db4e3`), covers visible output framing (`bb321f0`), validates the media chip (`f9aca55`), validates filtered selection context (`0b69e19`), exposes save/export status as accessible read-only controls (`3ea51ff`), and prioritizes those controls in hosted smoke (`054e958`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. The tenth commit is this checkpoint; the hosted run remains failed only on the now-corrected UIA lookup and must be revalidated by the next mandated push. Manual visual/HiDPI/screen-reader acceptance remains unverified.

**Exact-ten production UI polish checkpoint (2026-10-18):** The batch since pushed `c682cb1` expands the hosted smoke contract for the collapsed Project and export section (`807ba96`), covers media-type filtering (`234bb3b`), verifies visible quick export (`f8b6c36`), surfaces quick project save (`e28a437`), covers quick-save smoke (`b8795f4`), exposes live save status (`b17e835`), groups save/export feedback into a labeled local-actions panel (`666d368`, `cdcceea`), and adds selected-media context to the media-bin header (`2cf2ac1`, `4967a97`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. Local native UI smoke remains unclaimed because this host's Windows `SendKeys` surface fails; hosted Windows/macOS/Linux validation is pending this mandated push, and manual visual/HiDPI/screen-reader review remains unverified.

**Media rail and export affordance checkpoint (2026-10-18):** The exact ten-commit batch since pushed `395dc20` adds media type filtering (`b447ba3`), explicit selected-media badges (`adb0359`), responsive media search sizing (`2e9ccb1`), Photo/Video/Audio label normalization (`9853fcb`), a live playback status badge (`c0e389d`), a compact offline workspace strip (`22f9fdc`), a visible timeline reset action (`32c262c`), quick MP4 export in the monitor controls (`265c9c9`), quick-export availability coverage (`1265d5d`), and live quick-export status (`3d832f4`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. This checkpoint is ready for the mandated push; hosted validation and native visual/HiDPI/screen-reader review remain pending/unverified.

**Production dark-editor UI checkpoint (2026-10-18):** The exact ten-commit batch since pushed `5bd50b5` adds synchronized lane scrolling (`36ecdf5`, `59b85f5`), a decluttered timeline inspector default (`ae2b011`), larger inspector hit targets (`e0da72d`), text-based workspace status (`eb8610c`), shared dark control geometry (`d5c1422`), ruler/lane scroll synchronization (`3077689`), simplified track labels (`4f5a411`), playback-state transport feedback (`ffbe665`), and media-aware video action gating (`e4f4abb`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. This checkpoint is ready for the mandated push; hosted validation and native visual/HiDPI/screen-reader review remain pending/unverified.

**Selection, review and preview semantics checkpoint (2026-10-18):** The ten-commit batch since pushed `2da3df5` adds timeline-to-media selection synchronization (`3c10ae0`), unified frame-accurate position readouts (`d161bd8`), Color range tooltips (`75c4961`), live AI approved-action card styling (`81e28bc`), a ruler playhead marker (`eda4dae`), Photo opacity/quality values (`6cf3ab1`), synced media-highlight coverage (`0b5c146`), semantic Photo/Color comparison previews (`5e8cb14`), and a semantic Color scopes surface (`8fe36b4`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. This checkpoint is the tenth commit and is ready for the mandated push; hosted validation for this batch and native visual/HiDPI/screen-reader review remain pending/unverified.

**Cross-workspace production UI refinement checkpoint (2026-10-18):** The ten-commit batch since pushed `00bad13` includes shared media/editor panes (`592cca3`), a responsive stacked media rail (`7007a85`), pane separation (`8ff6c55`), selected-clip semantics (`9aaf13d`), compact media thumbnails (`87b953a`), filename-based timeline labels (`396d023`), dense-control tooltips (`5e27358`), collapsed Photo recovery (`f98f832`), and a focused Photo core inspector (`301fcfd`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. This checkpoint is the tenth commit and is ready for the mandated push; hosted validation for this batch and native visual/HiDPI/screen-reader review remain pending/unverified.

**Production NLE UI refinement checkpoint (2026-10-18):** The ten-commit batch since pushed `6f95a30` adds imported waveform semantics (`22f6179`), an explicit active workspace rail (`a921a66`), selected media-card styling (`d221e80`), a two-pane Video editor composition (`c0ed756`), a monitor timecode overlay (`db85e67`), and an explicit frame-accurate `timecode · frame` contract (`7eb368f`), alongside the batch's visual ruler, seekable ruler and waveform rendering commits (`4bf6333`, `78661e2`, `b855490`). The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, repository/schema validation, formatting verification and `git diff --check`. This checkpoint is the tenth commit and is ready for the mandated push; hosted validation and native visual/HiDPI/screen-reader review remain pending/unverified.

**Timeline and UI quality refinement checkpoint (2026-10-18):** Commits `ce2a3b2`, `21a207b`, `d51929b`, `b53b90e`, `4167005`, `26bfa41`, `90332f4`, `8a06e0d` and `1c0647c` add time-scaled timeline clip lanes, frame-position geometry coverage, a live playhead, imported-media thumbnails, framed Color comparison/scope panes, a timeline zoom readout, synchronized Color control state and named AI approval-card coverage. The full local gate passes a warning-free Release build, 266 unit tests, 2 golden tests, documentation/schema validation, formatting verification and `git diff --check`. The branch is nine commits ahead of pushed `063aa77`; this checkpoint is the tenth commit for the mandated push. Hosted quality run `39140772259` for the previous push is still in progress; native visual, HiDPI and screen-reader review remains unverified.

**Production UI refinement checkpoint (2026-10-18):** Commits `425bbfd`, `c7066f2`, `53f0d51`, `df0d999`, `d4db84f`, `533e8e0`, `d221f88` and `1e85949` add a real Photo original/edited comparison using the source and rendered edit buffers, a structured Video program-monitor/transport surface with frame/timecode readouts, a clearer empty Media Bin state, readable AI approval cards, live Color grade values and duplicate-preview-render avoidance. The full local gate passes a warning-free Release build, 264 unit tests, 2 golden tests, documentation/schema validation, formatting verification and `git diff --check`. The branch is nine commits ahead of pushed `620f682`; this checkpoint is the tenth commit for the mandated push. Hosted/native visual and HiDPI review remains unverified.

**Hosted Color/AI UI validation checkpoint (2026-10-18):** Quality Gates run `39139456212` for pushed commit `620f682` is green across documentation/schema, Windows/macOS/Linux restore/build/test, native FFmpeg smoke, Windows UI Automation, headless shell, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksums. The new Color `Scopes` inspector contract passed hosted UI automation; manual native visual, HiDPI and screen-reader acceptance remains open for FT-901/FT-902/FT-1002, and FT-1102 remains open for its broader mask/tracking scope.

**Exact-ten Color/AI UI batch checkpoint (2026-10-18):** Commits `0250240`, `2f5cd8f`, `040ca52`, `71d3f6b`, `3235349`, `0049671`, `efb6e64`, `0452ced` and `6db673f` add hosted evidence for the previous shell batch, real post-grade Color waveform/vectorscope previews, stable grade controls, a guarded Scopes inspector, Photo project/export guidance, and AI consent/apply/undo guidance. The full local gate passes a warning-free Release build, 264 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`. This is exactly ten commits ahead of pushed `98dd603` after this checkpoint; hosted Windows UIA, macOS DMG and Linux packaging revalidation is required after the push.

**Color scope UI checkpoint (2026-10-18):** Commits `2f5cd8f`, `040ca52` and `71d3f6b` add real waveform and vectorscope previews to the Color inspector, keep them empty until a real local preview exists, calculate them from the post-grade pixels, and stabilize grade-slider sizing for compact layouts. The full local gate passes a warning-free Release build, 264 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`. Hosted validation is deferred to the next exact-ten push; FT-1102 remains open for the broader mask/tracking scope and native acceptance.

**Hosted production-shell validation checkpoint (2026-10-18):** Quality Gates run `39138271695` for pushed commit `98dd603` is green across documentation/schema validation, Windows, macOS and Linux restore/build/test, native FFmpeg media smoke, formatting, headless desktop shell, Windows UI Automation, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksums. The run confirms the ten-commit UI batch, including the timeline selection/operation surfaces, media search controls, and preview/project/export action guidance. Manual native visual, HiDPI and screen-reader acceptance remains open for FT-901/FT-902/FT-1002; no release issue is closed from automated evidence alone.

**UI action guidance and exact-ten local gate checkpoint (2026-10-18):** Commit `f50f064` adds concise tooltips to the real timeline preview, playback, project save/open and MP4 export actions so the dense editor surface remains discoverable without changing behavior. The focused adaptive shell test passes; the full local gate passes a warning-free Release build, 262 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`. The ten commits since pushed baseline `0a473d7` are ready for the mandated push; hosted Windows UI Automation, macOS DMG retry and Linux packaging revalidation remain pending, and manual native visual/HiDPI/screen-reader acceptance remains open.

**Hosted correction local gate checkpoint (2026-10-18):** Commit `6e5fc73` passes the Git Bash macOS packaging syntax check, PowerShell UI-smoke parsing, repository validation, warning-free Release build, 262 unit tests, 2 golden tests, formatting and `git diff --check`. Hosted revalidation remains pending the next exact-ten-commit push after the failed run `39137069246`.

**Hosted regression and correction checkpoint (2026-10-18):** Quality Gates run `39137069246` correctly exposed two independent issues in the ten-commit UI batch: Windows UI Automation could not surface the semantic `Border` named `Timeline selection inspector`, and macOS `hdiutil create` transiently failed with `Resource busy`. Commit `6e5fc73` adds the existing UIA context-name alias, bounded five-attempt DMG creation with stale-output cleanup, and a repository guard for that retry contract. The failed run remains non-release evidence; a new exact-ten-commit push is required.

**UI automation and workspace affordance batch checkpoint (2026-10-18):** The next ten-commit batch adds hosted smoke coverage for media search/clear, timeline tracks/selection context/operation status, named video/audio timeline lanes, media-card metadata tooltips, timeline navigation/status tooltips and a primary local-image entry action. The full local Release gate passes with 0 warnings/errors, 262 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`; push is ready for hosted revalidation.

**Hosted UI contract extension checkpoint (2026-10-18):** The Windows UI smoke now requires the production media search/clear controls, timeline track surface, selection inspector and operation status in addition to the existing transport/export actions. PowerShell parsing, repository validation and `git diff --check` pass locally; hosted confirmation is deferred to the next exact-ten-commit push.

**Hosted accessible editor validation checkpoint (2026-10-18):** Quality Gates run `39135856203` for pushed commit `4353ad8` is green across documentation/schema, Windows, macOS and Linux restore/build/test, native FFmpeg media smoke, formatting, headless shell, Windows UI Automation, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksums. The run validates the semantic timeline lane accents and keyboard-focused action styling in the production shell. Manual native visual, HiDPI and screen-reader acceptance remains open for FT-901/FT-902/FT-1002.

**UI accessibility and timeline batch checkpoint (2026-10-18):** The production editor surface now provides semantic video/audio lane accents and an explicit keyboard-focus treatment for buttons, while preserving existing timeline selection and edit commands. The exact ten local commits since pushed baseline `d63f0f8` pass a warning-free Release solution build, 262 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`; the batch is ready for the mandated hosted push. Manual native visual, HiDPI and screen-reader acceptance remains unverified.

**Timeline chip density checkpoint (2026-10-18):** Timeline clip controls now use a stable minimum width, left-aligned labels and ellipsis trimming so dense edit lanes remain readable without changing the underlying selection or edit commands. The focused selection test, full Release suite (262 unit tests and 2 golden tests), repository validation, formatting and `git diff --check` pass locally. Hosted revalidation remains pending the next exact-ten-commit push; native visual and HiDPI acceptance remains unverified.

**Media selection continuity checkpoint (2026-10-18):** Filtering the populated media browser now preserves the selected asset when it remains in scope and restores that selection after Clear, keeping Color/Timeline edit context stable. The focused interaction test and full local gate pass; current totals are 262 unit tests and 2 golden tests with repository validation, formatting and `git diff --check` green. Hosted revalidation is pending the next exact-ten-commit push.

**Populated media-browser test checkpoint (2026-10-18):** The existing synthetic importer now drives a headless production `MainWindow` test through filename filtering, zero-result messaging and the real Clear button click. The full Release suite passes 262 unit tests and 2 golden tests; repository validation, formatting and `git diff --check` also pass. Next executable work remains manual/cross-platform FT-901/FT-902/FT-1002 and external FT-1003/FT-1005/FT-1006 release evidence; no implementation issue is being closed from this indirect UI evidence.

**Hosted media-browser validation checkpoint (2026-10-18):** Run `39134452039` for pushed commit `d63f0f8` is green across documentation/schema, Windows, macOS and Linux restore/build/test, native FFmpeg media smoke, formatting, headless desktop shell, Windows UI Automation, portable archives, installers, packaged relaunch and checksums. The run validates the real media filename filter, no-match state, disabled-until-needed Clear action, primary action hierarchy and prior timeline/media surfaces. Manual native visual/HiDPI/screen-reader acceptance remains open.

**Media search completion checkpoint (2026-10-18):** The asset browser now supports local filename filtering, an accessible no-match state and a disabled-until-needed Clear action. Full local validation passes 261 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`; this is the tenth commit since the last push and is ready for hosted revalidation.

**Media search empty-state checkpoint (2026-10-18):** The local media filter now presents an accessible `No matching media.` state when a query returns zero assets and hides it for an empty query or populated results. The full local gate passes 261 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`; hosted revalidation is pending the next exact-ten-commit push.

**Media search checkpoint (2026-10-18):** The media browser now has a real local filename filter that refreshes when imports or reopened projects change the source collection. The shell test covers the search control's layout, tooltip and automation contract; the full local gate passes 261 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`. Hosted revalidation is pending the next exact-ten-commit push.

**Primary action hierarchy checkpoint (2026-10-18):** Real import, timeline playback, photo export, color/reference grading, Gemini analysis/approval and video export actions now use the shared accent primary-button treatment. The shell test verifies the class in each selected workspace; the full local gate passes 261 unit tests, 2 golden tests, repository validation, formatting and `git diff --check`. Hosted revalidation is pending the next exact-ten-commit push.

**Hosted editor surface validation checkpoint (2026-10-18):** Run `39133151567` for pushed commit `4ff8410` is green across documentation/schema, Windows, macOS and Linux restore/build/test, native FFmpeg media smoke, formatting, headless desktop shell, Windows UI Automation, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksums. The batch verifies the thumbnail-card media browser, compact list spacing, structured timeline lanes and live status UIA path. Manual native visual/HiDPI/screen-reader acceptance remains open.

**Editor surface regression checkpoint (2026-10-18):** The headless shell contract now guards the thumbnail-card media bin's wrapping template and height plus the structured timeline lane surface. The focused adaptive shell test passes, formatting and `git diff --check` pass; this completes the current ten-commit local UI batch and requires the mandated hosted push before citing cross-platform evidence.

**Timeline lane checkpoint (2026-10-18):** Video tracks now render as labeled, bordered lanes with real selectable clip chips and frame ranges instead of an unstructured button stack. The existing timeline model, selection actions and accessibility names are unchanged. Release build with warnings-as-errors, the adaptive shell test, formatting and `git diff --check` pass locally; hosted/native visual validation remains pending.

**Editor spacing checkpoint (2026-10-18):** Shared `ListBoxItem` padding is now compact enough for thumbnail-card media browsing while keeping timeline track rows readable. Release build with warnings-as-errors, the adaptive shell test, formatting and `git diff --check` pass locally. Hosted/native visual review remains pending.

**Media bin validation checkpoint (2026-10-18):** The thumbnail-card media bin correction passes the full local Release gate: 261 unit tests, 2 golden tests, repository documentation/schema validation, formatting and `git diff --check`. The first validator invocation hit the host's missing `python` alias; the same script passed through the available `py` launcher. Hosted Windows UIA and native visual review remain pending.

**Media bin presentation checkpoint (2026-10-18):** The shared media bin now uses real thumbnail cards with kind badges, filename/metadata hierarchy and compact wrapping suitable for an editor asset browser. Selection still drives the existing Color and Timeline models; no import or media-processing behavior changed. Release build is warning-free, the adaptive shell test passes, formatting and `git diff --check` pass locally. Hosted/native visual validation remains pending.

**Native status smoke checkpoint (2026-10-18):** Windows UI Automation now requires the live `Active workspace status` region in addition to the existing Video/Photo/Color/AI control checks. Run `39132281248` exposed that Avalonia `TextBlock` was not discoverable through the hosted UIA tree even though the headless binding test passed. The status presenter is now a borderless read-only `TextBox`, which keeps the same live binding while using the already verified UIA text-control path. Unit tests pass 261/261, golden tests pass 2/2, formatting and `git diff --check` pass locally; the correction is unpushed and requires hosted revalidation.

**Status affordance regression checkpoint (2026-10-18):** The shell test now verifies the live workspace status tooltip and its text after switching Video, Photo, Color and AI tabs. Focused and full local gates pass: 261 unit tests, 2 golden tests, warning-free Release build, formatting and `git diff --check`.

**Live workspace status checkpoint (2026-10-18):** The top of every workspace now exposes a real bound status line for project/export state, photo edits, color grading, or AI proposal review. It updates when the active workspace changes and when the underlying view model reports a new state; it is automation-named and ellipsis-safe for compact layouts. Release build, 261 unit tests, 2 golden tests, formatting and `git diff --check` pass locally; hosted validation is pending for the next exact-ten-commit push.

**GitHub backlog audit checkpoint (2026-10-18):** `gh issue list` reports 55 closed issues and 27 open issues. The closed implementation set covers FT-001 through FT-804, FT-903/FT-904, FT-1001/FT-1004, and FT-1101/FT-1103; the remaining open implementation/release items are manual UI/platform acceptance, legal/native redistribution, signing, live-provider or approved-asset gates, plus deferred E11/E12 scope. No open issue is being closed from indirect evidence.

**Latest hosted inspector validation checkpoint (2026-10-18):** Run `39131287232` for pushed commit `53c0f0b` passed documentation/schema validation and Windows, macOS and Linux quality jobs. It includes the corrected Windows UI Automation smoke, which successfully navigated collapsed Video/Color/AI inspector sections before finding nested controls, plus native FFmpeg media smoke, 261 unit tests, 2 golden tests, formatting, portable archives, Windows installer, macOS DMG, Linux DEB, packaged relaunch and checksum artifacts. Manual native visual/HiDPI/screen-reader acceptance remains open.

**UIA guard checkpoint (2026-10-18):** Repository validation now requires the native UI smoke to include explicit inspector-section expansion and coverage for Video titles/captions, Color reference matching and AI activity before checking nested controls. Documentation validation, PowerShell parsing and diff checks pass; hosted confirmation is green in run `39131287232`.

**Explicit label regression checkpoint (2026-10-18):** The adaptive shell contract now asserts stable names for Photo edit/reset/mask/retouch actions, AI proposal clearing, and timeline motion/audio/title actions. Focused shell coverage and the full local Release gate pass: 261 unit tests, 2 golden tests, formatting and `git diff --check`; hosted confirmation is green in run `39131287232`.

**Explicit action-label checkpoint (2026-10-18):** Photo rotate/undo/overlay-mask/retouch/grade actions, AI proposal clearing, timeline motion/audio/title actions now carry stable action-specific automation names instead of relying on generic content fallback. Release build is warning-free; 261 unit tests, 2 golden tests, formatting and `git diff --check` pass locally. The hosted UI smoke including these labels is green in run `39131287232`.

**Local UIA limitation checkpoint (2026-10-18):** Running `scripts/smoke_desktop_ui.ps1` on this host reaches the native process but Windows `SendKeys` fails with the host message `The operation completed successfully`, indicating no interactive desktop input surface. No local shortcut pass is claimed; hosted Windows remains the authoritative UIA environment for this gate.

**Hosted UI smoke correction checkpoint (2026-10-18):** Run `39130580530` passed documentation, Ubuntu and macOS jobs but Windows UI automation failed because the smoke searched for caption/reference/activity controls while their real inspector sections were collapsed. `scripts/smoke_desktop_ui.ps1` now locates and expands the named Avalonia sections through UI Automation before checking their live controls; the corrected hosted result is green in run `39131287232`.

**Inspector interaction checkpoint (2026-10-18):** The adaptive shell test now expands a real collapsed timeline section, verifies its live motion-analysis action becomes available, collapses it again, and checks all inspector sections' stretch alignment. The exact-ten local UI batch is green: Release build with 0 warnings/errors, 261 unit tests, 2 golden tests, formatting, repository validation and `git diff --check`. This batch is ready for the required hosted push; native visual/HiDPI/screen-reader acceptance remains unverified.

**Unified workspace hierarchy checkpoint (2026-10-18):** Photo project/layers/mask/grade/export, Color reference/before-after/grade/style, and AI provider/consent/activity plus proposal plan/review/recipe surfaces now use the same real accessible inspector sections as Video. Primary editing and review sections open by default; secondary operations remain available without overwhelming the initial viewport. Headless shell coverage expands each workspace before checking automation names. Release build, 261 unit tests, 2 golden tests, formatting and `git diff --check` pass locally; native visual/HiDPI review remains unverified.

**Inspector theme checkpoint (2026-10-18):** Timeline expanders now use the shared dark editor palette with graphite surfaces, separator borders, hover contrast and blue keyboard focus instead of host-default control surfaces. The adaptive shell test asserts the sections expose tooltips and styled backgrounds/borders; Release build, 261 unit tests, 2 golden tests, formatting and `git diff --check` pass locally. Native visual/HiDPI review remains unverified.

**Production editor density checkpoint (2026-10-18):** The Video workspace now keeps preview, transport, frame selection, tracks, clip editing and project/export actions immediately discoverable while grouping secondary analysis, motion, audio and title/caption controls into real accessible Avalonia expanders. Expanding a section preserves the existing live controls and now triggers a deferred accessibility pass for generated template buttons. The shell contract covers seven sections, default open/closed states and tooltips. Release build, 261 unit tests, 2 golden tests, formatting and `git diff --check` pass locally; native visual/HiDPI review remains unverified.

**Latest hosted UI validation checkpoint (2026-10-18):** Run `39129216639` for pushed commit `f261d37` passed documentation/schema validation plus the Windows, macOS and Linux build/test jobs. The matrix passed native FFmpeg media smoke, formatting, 261 unit tests, 2 golden tests, headless desktop shell checks, portable archive verification, Windows installer validation, macOS DMG validation, Linux DEB validation, packaged relaunch and checksum artifacts. This verifies the third UI batch cross-platform; manual native visual/HiDPI/screen-reader acceptance remains open.

**Hosted editor presentation validation checkpoint (2026-10-18):** Quality gates run `39127041107` passed documentation/schema validation and the Windows, macOS and Linux build/test jobs after the editor presentation batch. The run passed native FFmpeg media smoke, formatting, unit/golden tests, headless shell validation, portable archive checks, and platform package validation. The current desktop shell presents a dark, keyboard-labeled workspace rail, real media thumbnails, timeline clip rows, centered preview canvases, and readable color selectors. Manual native visual/HiDPI acceptance remains open for FT-901/FT-902/FT-1002.

**Interactive timeline and media surface checkpoint (2026-10-18):** Timeline clip chips are now real accessible buttons that select their existing editable clip model directly; the workspace rail uses a right-edge selected-state indicator appropriate for vertical navigation; and the media bin reports its live asset count, including a clear empty-project state. Release build, focused shell/import coverage and diff checks pass locally. Manual native visual/keyboard acceptance remains open.

**Full local UI batch gate (2026-10-18):** The ten-commit editor polish batch passes `dotnet build FrameTheory.sln --configuration Release --no-restore --warnaserror` with 0 warnings/errors, 260/260 unit tests, 2/2 golden tests, repository validation, `dotnet format --verify-no-changes` and `git diff --check`. Hosted cross-platform evidence is green at run `39127041107`; the next push will revalidate the final clickable-clip and media-summary changes.

**Workspace empty-state checkpoint (2026-10-18):** Photo, color before/after, timeline and AI proposal surfaces now expose explicit empty states until real media, decoded frames or validated actions are available. These states are bound to the existing live models and are covered by the headless workspace-shell test; no preview or AI result is fabricated.

**Second full local UI batch gate (2026-10-18):** The follow-up ten-commit UI batch passes Release build with 0 warnings/errors, 260/260 unit tests, 2/2 golden tests, repository validation, formatting verification and `git diff --check`. It adds direct timeline clip selection, selection-inspector hierarchy, truthful photo/color/timeline preview empty states and AI proposal empty-state feedback. Hosted revalidation is pending after the required exact-ten-commit push.

**HiDPI shell checkpoint (2026-10-18):** The desktop window now explicitly enables Avalonia layout rounding so rail edges, borders and control bounds avoid fractional-pixel blur on scaled displays. The adaptive shell test asserts the setting; native HiDPI visual review remains open.

**Media-bin interaction checkpoint (2026-10-18):** Imported assets retain their media kind through import and project reopen. Selecting an asset updates the local color source and selects its existing video/audio timeline clip when applicable; the compact rail also exposes workspace descriptions through native tooltips. Focused import/shell tests pass; native pointer/keyboard review remains open.

**Headless UI isolation checkpoint (2026-10-18):** The media-bin interaction test now exercises the real imported `ListBox` and serializes shared Avalonia headless sessions to prevent dispatcher ownership races. The full local unit suite passes 261/261 after this coverage addition; native pointer/keyboard review remains open.

**Color selector presentation checkpoint (2026-10-18):** Color reference source and target selectors now show decoded thumbnails beside concise filenames while retaining the existing consent, analysis and apply flow. Focused shell build/test and diff checks pass; native visual review remains open.

**Asset context checkpoint (2026-10-18):** Media-bin thumbnails now include a compact kind badge, and timeline clip chips expose exact frame ranges through native tooltips. The headless media interaction test verifies the tooltip and direct selection path; native visual review remains open.

**Third full local UI batch gate (2026-10-18):** The ten-commit batch passes Release build with 0 warnings/errors, 261/261 unit tests, 2/2 golden tests, repository validation, formatting verification and `git diff --check`. It includes media-summary reopen synchronization, color selector thumbnails, asset-kind badges, timeline frame guidance and shell action tooltips. Hosted validation remains pending for the pushed revision.

**Editor presentation batch checkpoint (2026-10-18):** The production Avalonia shell now separates dense editor sections with dividers, renders imported media with real decoded thumbnails and metadata, renders timeline tracks with readable clip chips and frame ranges, centers photo/color/video preview canvases, uses concise color-reference asset selectors, and gives dropdown options consistent selected/hover contrast. The headless shell contract covers all new templates and layout surfaces. The corrected local Release gate passes with 0 warnings/errors, 260/260 unit tests, 2/2 golden tests, repository validation, formatting and `git diff --check`; hosted revalidation is required after the symlink-status assertion correction, and native visual/HiDPI/screen-reader acceptance remains unverified.

**Hosted batch correction checkpoint (2026-10-18):** Quality gates run `39126185641` passed documentation/schema validation, build, formatting, golden tests and Windows UI automation, but macOS/Linux/Windows test jobs failed only because `TimelineProjectImportRejectsSymlinkedMediaDirectory` asserted the platform-specific term `reparse` while the stable cross-platform import status says `Symlinked media imports are not accepted`. The assertion now checks `symlink`; a new exact-ten-commit push is required before citing hosted green evidence.

**Compact editor UI checkpoint (2026-10-18):** The Avalonia shell now presents a compact shared media bin with asset list and local import/drop surface side-by-side, consistent spacing for action rows, full-width selected media rows, and deliberate hover/focus feedback for buttons, fields, selectors and checkboxes. The Foundation shell contract covers the media-bin geometry and accessibility names. The full local Release gate passes with 0 build warnings/errors, 260/260 unit tests, 2/2 golden tests, repository validation, formatting and `git diff --check`; native visual, HiDPI and screen-reader acceptance remains unverified.

**Import-surface accessibility checkpoint (2026-10-18):** Each workspace’s real local drag/drop surface now has an explicit automation name in addition to its supported file-picker action and import status. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native drag/drop and assistive-technology review remains unverified.

**Full local correction-batch gate (2026-10-18):** Media/timeline list focus now retains a blue keyboard edge. After the hosted regression fixes, the local Release solution build passes with 0 warnings/errors, unit tests pass 260/260, golden tests pass 2/2, repository validation and formatting pass, and `git diff --check` is clean. The next hosted run must revalidate the failed macOS/Linux symlink tests and Windows UI smoke before any task closure.

**Workspace-focus checkpoint (2026-10-18):** Keyboard focus on the workspace rail now retains a visible blue edge even before selection changes, completing the selected/hover/focus interaction states. Desktop Release build, focused shell test, formatting and `git diff --check` pass; native keyboard and HiDPI visual review remains unverified.

**AI-status accessibility checkpoint (2026-10-18):** Gemini provider/consent scope, analysis, proposal, approval, revision and recipe feedback regions now have stable automation names, preserving the optional-provider boundary while making review states discoverable. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native assistive-technology review remains unverified.

**Color-status accessibility checkpoint (2026-10-18):** Color before/after summaries, deterministic scope output, inspector state, style-library state and reference-analysis feedback now expose stable automation names; the adaptive-shell contract asserts them. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native assistive-technology review remains unverified.

**Photo-status accessibility checkpoint (2026-10-18):** Photo recovery now names its missing-media list and recovery status, while the editor exposes a distinct live edit status alongside its real preview. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native assistive-technology review remains unverified.

**Video-status accessibility checkpoint (2026-10-18):** Media import, compositor preview, motion analysis, rough cut, title animation, captions, timeline project/export and watermark feedback regions now expose stable automation names; the adaptive-shell contract asserts them. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native assistive-technology review remains unverified.

**Timeline inspector accessibility checkpoint (2026-10-18):** Live timeline frame, position, ruler, selection, beat-marker and operation-status regions now have explicit automation names, so the current edit context can be consumed independently of the visual layout. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native assistive-technology review remains unverified.

**Shortcut-discovery checkpoint (2026-10-18):** The production header now exposes the working `Ctrl+1`–`Ctrl+4` workspace shortcuts in a compact, ellipsis-safe hint, with a named accessibility region and headless contract coverage. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native visual/HiDPI review remains unverified.

**Cross-platform link-boundary checkpoint (2026-10-18):** `LocalPathSecurity` now detects both `FileAttributes.ReparsePoint` and .NET link targets, and timeline media import rejects Unix/Windows symlinked directories before they reach the importer or project state. The corrected trust-boundary test passes locally, with formatting and `git diff --check` green; the previous hosted failure exposed the old save-stage expectation and must be revalidated with this corrected acceptance.

**UI smoke compatibility checkpoint (2026-10-18):** Restored the established `Open image` automation name after hosted Windows UI smoke correctly detected that the accessibility wording change had broken the existing control contract. Desktop build, rebuilt Foundation shell test, formatting and `git diff --check` pass; hosted smoke revalidation is pending.

**Full local UI batch gate (2026-10-18):** The adaptive shell regression now verifies all four workspace scroll surfaces are named as well as stretched and top-aligned. The Release solution build passes with 0 warnings/errors, unit tests pass 260/260, golden tests pass 2/2, repository validation and formatting pass, and `git diff --check` is clean. Hosted revalidation is required after the current ten-commit batch; native visual/HiDPI and manual screen-reader acceptance remain unverified.

**Selector accessibility checkpoint (2026-10-18):** Photo export format/quality and the Gemini model selector now have explicit automation names and are included in the adaptive-shell contract. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native assistive-technology review remains unverified.

**Input accessibility checkpoint (2026-10-18):** Placeholder-driven text inputs now receive stable automation names after the window is opened, and the adaptive-shell test rejects any placeholder-bearing text box without a name. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native screen-reader review remains unverified.

**Media-selection styling checkpoint (2026-10-18):** Imported-media and timeline-track list rows now share dark hover and selected states, keeping the active clip or asset readable without changing selection behavior. The rebuilt Foundation shell test, Release test-project build, formatting and `git diff --check` pass; native visual selection review remains unverified.

**Status-region accessibility checkpoint (2026-10-18):** The live capability and diagnostics footer regions now expose explicit automation names alongside the existing refresh action, and the adaptive-shell test asserts both names. Rebuilt Release test project, focused shell test, formatting and `git diff --check` pass; native screen-reader review remains unverified.

**Compact status-bar checkpoint (2026-10-18):** The footer diagnostics message now uses bounded width and ellipsis trimming, preserving the capability status and refresh action when the editor is resized toward its supported minimum. Desktop Release build, focused shell test, formatting and `git diff --check` pass; native resize/HiDPI review remains unverified.

**Workspace-rail checkpoint (2026-10-18):** The left workspace rail now has deterministic graphite, hover and selected states with a blue active edge and readable focus contrast, keeping Video/Photo/Color/AI navigation visually legible at compact widths. The rebuilt Foundation shell test, Release test-project build, formatting and `git diff --check` pass; native visual/HiDPI inspection remains unverified.

**Editor interaction-state checkpoint (2026-10-18):** Shared Avalonia styles now give buttons deterministic graphite hover/pressed/disabled states and give text/combo inputs a blue focus border, preserving the dark Frame Theory palette across host themes. The rebuilt Foundation shell test, Release test-project build, formatting and `git diff --check` pass; native pointer/keyboard visual review remains unverified.

**Deterministic dark-theme checkpoint (2026-10-18):** The Avalonia Fluent theme is now explicitly requested as dark before the shared editor styles are installed, preventing host OS theme defaults from producing inconsistent light controls. The rebuilt Foundation shell test passes, the Release test-project build is warning-free, and formatting/diff checks are green. Native visual inspection remains unverified.

**Hosted UI correction checkpoint (2026-10-18):** Hosted run `39123624550` correctly failed the new adaptive-shell test on macOS/Linux because `FoundationTests.cs` lacked its `Avalonia.Layout` import; the local `--no-build` smoke had not compiled that changed test source. The test now rebuilds from source and passes, unnamed string-content buttons receive content-derived automation names after window open, and formatting/diff checks are green. The corrective commit is local and awaits the next exact-ten-commit push.

**Responsive workspace checkpoint (2026-10-18):** Each workspace content surface now explicitly stretches to the available editor width and anchors at the top of its scroll viewport, with automation coverage for all four workspace containers. The Foundation adaptive-shell test, Desktop Release build, formatting and `git diff --check` pass; native resize/HiDPI review remains unverified.

**UI accessibility checkpoint (2026-10-18):** Every visible editor button with an action now has an explicit automation name, including Photo recovery/editor, Color, Gemini settings, timeline editing/audio/caption controls and timeline navigation. The Foundation shell test fails if a content-bearing button is unnamed; Desktop Release build, focused shell test, formatting and `git diff --check` pass. Native visual, keyboard and HiDPI review remains unverified.

**Explicit import control checkpoint (2026-10-18):** Every workspace now exposes a real multi-select `Import media` file-picker action alongside drag/drop, limited to supported local media patterns and routed through the existing path-safe importer. The new control is automation-named and covered by the headless shell test; Desktop Release build, formatting and diff checks pass.

**Desktop drag/drop boundary checkpoint (2026-10-18):** Main-window media import now rejects reparse-point paths before invoking the importer and reports a user-facing failure. The regression proves a redirected file never reaches a custom importer; focused desktop import coverage passes 11/11, repository validation, formatting, diff checks and the Release Desktop build are green.

**AI workspace hierarchy checkpoint (2026-10-18):** The optional Gemini surface now separates Provider, Consent and analysis scope, Activity, Plan input and revision, Review and apply, and Reusable recipe sections. Existing secure key storage, explicit upload consent, schema validation and transactional actions remain functional; desktop build, headless shell and formatting checks pass. Hosted visual revalidation remains pending.

**Color workspace hierarchy checkpoint (2026-10-18):** The Color workspace now groups reference matching, before/after preview, grade controls and reusable style-library actions, with the real image previews and consent-gated Gemini actions preserved. Desktop build, headless shell coverage and formatting checks pass; hosted visual revalidation remains pending.

**Photo workspace hierarchy checkpoint (2026-10-18):** The Photo workspace now leads with a contained canvas surface and labels Project, Layers and blend, Mask and retouch, Grade and Export tool groups. The real import, non-destructive edit, recovery and export handlers remain unchanged; the desktop Release build, headless shell test and formatting checks pass. Hosted visual revalidation remains pending.

**Workspace panel surface checkpoint (2026-10-18):** Photo, Color and AI panels now share the production dark surface, border, radius and spacing language used by the Video workspace, without changing their existing commands, previews, consent controls or automation names. Local Release build, formatting and diff checks are green; hosted UI revalidation is pending the next exact-ten-commit push.

**Hosted security-batch revalidation checkpoint (2026-10-18):** Run `39121797572` for pushed commit `464bec7` passed documentation/schema validation and the Windows, macOS and Linux matrix, including restore, advisories, dependency inventory, build, native media, headless/native UI smoke, formatting, tests, packaging, relaunch and checksums. The batch improves path-boundary safety; manual visual UI, legal, signing, clean-host and final-audit gates remain open.

**Timeline workspace hierarchy checkpoint (2026-10-18):** The Video workspace now presents a real preview/action card followed by labeled timeline navigation, analysis, clip editing, motion, audio, captions, project/export and view sections. Existing controls and handlers are unchanged; the Release build and accessible headless shell test pass, with native visual inspection still unverified.

**Production UI shell checkpoint (2026-10-18):** The Avalonia shell now uses the approved logo when available, a compact local-mode brand bar, dark graphite surfaces, a left workspace rail, clearer import drop-zone guidance, stronger card hierarchy and consistent control styling while preserving the functional Video/Photo/Color/AI controls and automation names. The full local Release suite passes 259 unit tests and 2 golden tests; native visual/HiDPI review remains unverified because no native application surface is available in the current computer-use host.

**FFmpeg executable-boundary checkpoint (2026-10-18):** Bundled and environment-selected FFmpeg/ffprobe candidates now ignore reparse-point paths before they can be returned to the process runner. The locator regression covers redirected bundled tools; focused locator coverage and repository policy validation are pending this commit's local gate and hosted revalidation.

**Photo input-boundary checkpoint (2026-10-18):** The Photo workspace now rejects reparse-point image and overlay paths before handing them to the importer, and reports the rejection through the existing status surface instead of escaping the UI. Focused photo persistence coverage passes 8/8, with formatting, repository validation and `git diff --check` green; hosted cross-platform revalidation remains pending at the next exact-ten-commit push.

**Native GUI availability checkpoint (2026-10-18):** The Windows computer-use inventory returned no native applications (`apps=[]`), only browser tabs. Native UI interaction, HiDPI inspection and cross-platform manual journeys therefore remain unverified; no GUI task is closed from headless or CI evidence alone.

**Capability-probe checkpoint (2026-10-18):** The storage capability probe now rejects a reparse-point temporary path before writing its marker, and repository validation requires the shared guard in that platform entry point. Focused Foundation coverage passes 20/20, with formatting, repository validation and `git diff --check` green. This is local evidence pending the next exact-ten-commit hosted revalidation.

**UI evidence wording checkpoint (2026-10-18):** `docs/TEST_STRATEGY.md` now distinguishes logical 1280x800/960x640 headless bounds from the DPI-scaled-safe native Windows minimum and records `Ctrl+1..4` coverage accurately. Documentation validation and `git diff --check` pass; native HiDPI/manual cross-platform evidence remains open.

**Trust-boundary policy checkpoint (2026-10-18):** Repository validation now requires the shared reparse-point guard in timeline preview, diagnostics and desktop file-I/O entry points in addition to media, storage, caption and AI consumers. The validator and `git diff --check` pass; this policy evidence is local pending the next exact-ten-commit hosted revalidation.

**Full local post-hardening checkpoint (2026-10-18):** The Release solution passes 256 unit tests and 2 golden tests with 0 failures after temporary-output and diagnostics path hardening; repository validation, formatting and `git diff --check` also pass. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Diagnostics privacy checkpoint (2026-10-18):** Local diagnostics now uses the shared reparse-point guard for both the log directory and file, matching Gemini history behavior; linked diagnostics destinations are ignored without writing through to the external target. Focused diagnostics/history coverage passes 14/14, with formatting, repository validation and `git diff --check` green. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Temporary-output trust-boundary checkpoint (2026-10-18):** FFmpeg frame/timeline previews, waveform/audio extraction, proxy creation, media/photo/timeline exports and local Whisper now reject reparse-point temporary outputs before reading, probing or moving them. Decoder coverage includes a redirected-output regression; focused media/caption/export coverage passes 66/66, with formatting, repository validation and `git diff --check` green. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Hosted revalidation checkpoint (2026-10-18):** Run `39120508948` for pushed `d3bb1dc` passed documentation/schema validation and the Windows, macOS and Linux matrix: restore, vulnerability/dependency checks, build, native FFmpeg media, Windows UI automation, formatting, full tests, headless desktop shell, publish/archive, installer/DMG/DEB, packaged relaunch and SHA-256 verification. Manual HiDPI/view journeys, per-OS manual UC evidence, legal redistribution, signing/clean-host review and final audit remain open; no task was closed from CI alone.

**Exact-ten batch checkpoint (2026-10-18):** The ten local commits after pushed `7c81dd6` are now validated through focused trust-boundary tests; the final desktop caption-I/O slice passes 11/11, with formatting, repository validation and `git diff --check` green. Push is now authorized by the batch policy; hosted cross-platform revalidation remains required.

**Desktop caption-I/O checkpoint (2026-10-18):** Caption import, verified transcript import, local Whisper input and caption export destinations now reject reparse-point paths before reading or writing. A dangling caption-link regression passes; focused desktop caption coverage passes 10/10, with formatting, repository validation and `git diff --check` green. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Recovery dangling-path checkpoint (2026-10-18):** Autosave load/clear and media relink now reject reparse-point paths before missing-file fallbacks; a dangling replacement link is rejected as unsafe rather than reported as an ordinary missing file. Focused recovery coverage passes 6/6; formatting, repository validation and `git diff --check` pass. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Full local security-gate checkpoint (2026-10-18):** After the media, audio, AI and desktop path-boundary fixes, the Release solution passes 251 unit tests and 2 golden tests with 0 failures; repository validation, formatting and `git diff --check` also pass. This is local evidence only; the next hosted cross-platform matrix is held until the exact-ten-commit push.

**Desktop input-boundary checkpoint (2026-10-18):** Timeline project reopen, photo-project source capture, brand-font discovery and Gemini transcription now guard resolved paths before existence fallbacks. The desktop transcription regression confirms a dangling source is rejected before upload or caption mutation. Focused desktop/photo/foundation coverage passes 35/35; formatting, repository validation and `git diff --check` pass. This is local evidence pending the next exact-ten-commit hosted revalidation.

**AI font trust-boundary checkpoint (2026-10-18):** AI title and caption actions now validate approved font paths through the shared reparse-point guard before applying an edit; a dangling approved font rolls back the transactional action group. Focused executor coverage passes 5/5; formatting, repository validation and `git diff --check` pass. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Audio/brand dangling-path checkpoint (2026-10-18):** Audio preparation and approved-logo verification now inspect the shared reparse-point guard before existence or hashing fallbacks. A dangling audio-link regression and brand/audio focused coverage pass 8/8; formatting, repository validation and `git diff --check` pass. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Media/caption dangling-path checkpoint (2026-10-18):** Probe, decode, preview, export, timeline, photo and transcription consumers now run the shared reparse-point guard before missing-file fallbacks; export destinations are guarded even when the final entry does not exist. A dangling media-link regression passes, and the focused media/caption/export suite passes 61/61; formatting, repository validation and `git diff --check` pass. This is local evidence pending the next exact-ten-commit hosted revalidation.

**Dangling-loader checkpoint (2026-10-18):** Atomic project, cache, recipe, preset and workspace loaders now run the shared path guard before missing-file fallbacks, and final-path checks no longer depend on `File.Exists`; a dangling cache link is rejected instead of being treated as an empty cache. Focused storage/color/AI/foundation/project coverage passes 38/38; formatting and `git diff --check` pass.

**Current batch correction checkpoint (2026-10-18):** Hosted run `39119313079` correctly exposed that a physical `1280x800` UI Automation rectangle is invalid under runner DPI scaling (`1028x749`). The native smoke now retains its scaled-safe `960x640` minimum; exact `1280x800` and `960x640` logical bounds remain covered by the production headless tests. The failed hosted run is not release evidence and a new exact-ten-commit revalidation batch is required.

**Gemini parent-link regression checkpoint (2026-10-18):** Interaction-history coverage now proves record/read refuses a log beneath a linked parent directory and never creates the external target. Focused Gemini interaction coverage passes 11/11; formatting and `git diff --check` pass.

**Regular-destination regression checkpoint (2026-10-18):** Foundation coverage verifies that a missing file beneath a normal directory remains accepted for atomic first-time saves while linked and dangling paths are rejected. Focused Foundation coverage passes 20/20; formatting and `git diff --check` pass.

**Native UI smoke contract checkpoint (2026-10-18):** Repository validation now fails if the Windows UI smoke loses its `1280x800` launch assertion or its loop over all four workspace shortcuts. Documentation validation and PowerShell parsing pass; the actual local SendKeys limitation remains recorded separately.

**Native UI attempt checkpoint (2026-10-18):** The local Windows UI smoke launched the production desktop window but the non-interactive session failed at the first `SendKeys("^1")` call with the host exception `The operation completed successfully.` The script cleaned up the process; native shortcut success is not claimed locally, and hosted Windows UI validation is required for the new all-shortcut smoke.

**Native launch-size checkpoint (2026-10-18):** Windows UI automation fails closed below a scaled-safe `960x640` minimum and exercises the supported resize path; exact logical `1280x800` and `960x640` bounds are validated by the production headless test. PowerShell parsing and diff checks pass.

**Native shortcut smoke checkpoint (2026-10-18):** `scripts/smoke_desktop_ui.ps1` now drives and verifies real `Ctrl+1..4` workspace selection through Windows UI Automation instead of checking only `Ctrl+3`. PowerShell parsing passes and focused Foundation coverage passes 19/19; hosted confirmation is pending the next exact-ten-commit push.

**Latest hosted revalidation checkpoint (2026-10-18):** Run `39118310628` for pushed commit `807230f` passed documentation/schema validation plus Windows, macOS and Linux restore, build, native FFmpeg media smoke, formatting, full tests, headless desktop shell, publish/archive, installer/DMG/DEB, packaged relaunch and SHA-256 verification. The run validates the ten-commit path-security batch; manual GUI, live-provider, legal, signing, clean-host and final-audit gates remain open.

**Gemini history path checkpoint (2026-10-18):** Redacted Gemini interaction history now refuses linked log files and parent directories on record/read, preserving the existing best-effort offline failure behavior without following redirected paths. Focused Gemini interaction coverage passes 10/10, repository validation, formatting and `git diff --check` pass. This increment was included in pushed commit `807230f` and hosted run `39118310628`.

**Latest full local gate checkpoint (2026-10-18):** After the Gemini interaction-history guard, the full Release solution gate passes 245 unit tests and 2 golden tests with 0 failures; repository validation, formatting and `git diff --check` also pass. Hosted run `39118310628` covers the pushed predecessor; the current local docs-only corrections are not pushed until the next exact-ten-commit boundary.

**Path-guard regression contract checkpoint (2026-10-18):** Documentation-stage validation now rejects any production C# reintroduction of `FileAttributes.ReparsePoint` outside `LocalPathSecurity`, while requiring every protected entry point to call the shared guard. Validator, formatting and `git diff --check` pass.

**Remaining trust-boundary consolidation checkpoint (2026-10-18):** Photo project source capture, approved-brand loading and preview-cache eviction now use the shared ancestor-aware path guard; static repository validation requires all three entry points to retain it. Focused photo/preview/brand coverage passes 10/10, repository validation, formatting and `git diff --check` pass.

**Dangling-link checkpoint (2026-10-18):** `LocalPathSecurity` now inspects the requested path before walking ancestors and treats missing-entry attribute errors as a normal walk step, so dangling file links cannot bypass the reparse-point guard used by local stores and media boundaries. Focused Foundation coverage passes 19/19, formatting and `git diff --check` pass.

**Shared ancestor regression checkpoint (2026-10-18):** Foundation coverage now verifies that a future file beneath a linked directory is rejected by `LocalPathSecurity`, covering the non-existent destination case used by atomic stores. Focused Foundation coverage passes 18/18; formatting and `git diff --check` pass.

**Shared-store guard checkpoint (2026-10-18):** Cache-index, AI-recipe, color-preset and workspace-state stores now use the shared `LocalPathSecurity.ContainsReparsePoint` walk, including existing files and parent ancestors, instead of independent final-entry checks. Focused recovery/recipe/color/foundation coverage passes 29/29; formatting, repository validation and `git diff --check` pass. This is local evidence pending the next exact-ten-commit push and hosted revalidation.

**Current hardening batch checkpoint (2026-10-18):** Full local Release validation passes 235 unit tests and 2 golden tests with 0 failures; repository validation, formatting and `git diff --check` also pass. Hosted run `39116168651` passed the documentation/schema, dependency, Windows/macOS/Linux build/test, native-media, packaging and headless desktop-shell matrix for the pushed ten-commit storage hardening batch. Manual GUI, legal, signing, clean-host and final-audit gates remain open.

**GitHub visibility checkpoint (2026-10-18):** `main` is pushed through commit `5f91036`; GitHub reports 55 closed issues (54 implementation tasks plus aggregate E08) and 27 open issues (15 implementation/deferred tasks plus 12 epics). No additional task is closed from the latest hosted matrix because the remaining acceptance gates are manual, legal, signing, clean-host or final-audit requirements.

**Headless UI coverage checkpoint (2026-10-18):** The UnitTests project now uses the test-only `Avalonia.Headless` 12.1.2 platform to instantiate the production `MainWindow`, verify 1280x800 and 960x640 bounds, minimum dimensions, two-way workspace synchronization, all four workspace automation names and required Video/Color/AI control names, then exercise the real Avalonia `Ctrl+1..4` input paths. Focused Foundation coverage passes 17/17 and the full Release suite passes 235 unit tests plus 2 golden tests. This strengthens FT-901/FT-902 implementation evidence but does not replace native HiDPI, visual-quality or macOS/Linux manual acceptance.

**Headless dependency evidence checkpoint (2026-10-18):** Local SBOM generation resolves 41 package versions with no missing license metadata and records `Avalonia.Headless` 12.1.2 with `MIT` metadata and `metadataFound=true`; CI now fails closed on missing metadata and explicitly checks the headless test package. This is dependency metadata evidence only and does not clear application licensing or redistribution review.

**Headless CI visibility checkpoint (2026-10-18):** The quality workflow now runs a named `FoundationTests` headless desktop-shell step on every Windows, macOS and Linux build/test job in addition to the full solution test. Local execution passes 17/17, and hosted run `39116168651` confirms the step passed on all three OS jobs.

**Hosted hardening checkpoint (2026-10-18):** Run `39116168651` passed documentation/schema validation plus Windows, macOS and Linux restore, advisory, dependency inventory, build, native FFmpeg media smoke, formatting, full tests, headless shell, publish, archive, installer/DMG/DEB, packaged relaunch and SHA-256 verification. This confirms the ten-commit path-hardening batch; it does not close manual UI, live-provider, legal, signing, clean-host or final-audit tasks.

**Photo-project path boundary checkpoint (2026-10-18):** Photo-project save and reopen now share `ProjectRecoveryService.ResolveContainedPath`, rejecting reparse-point project directories, parent media directories and final media entries before a source is fingerprinted or imported. Focused photo persistence coverage passes 6/6; the full Release suite passes 229 unit tests and 2 golden tests, and the new regression proves a symlinked media directory cannot redirect a valid project source outside its project root.

**Project repository boundary checkpoint (2026-10-18):** `JsonProjectRepository` now rejects reparse-point project directories and existing linked `.ftstudio` files before atomic save or load. Focused repository coverage passes 5/5, including save and load attempts through symlinked paths; this is local evidence pending the next hosted matrix.

**Timeline project path boundary checkpoint (2026-10-18):** Timeline project save now resolves each relative media reference through the shared containment validator before persisting it, so linked media directories cannot create projects that only fail later at reopen. Focused timeline persistence coverage passes 11/11, including a symlinked media-directory save regression; hosted confirmation remains pending.

**AI recipe path boundary checkpoint (2026-10-18):** `AiRecipeStore` now rejects reparse-point recipe directories and existing linked recipe files before atomic save or load. Focused recipe coverage passes 4/4, including a valid-schema linked-file regression; the recipe remains schema-validated and approval-gated after loading.

**Color preset path boundary checkpoint (2026-10-18):** `JsonColorPresetStore` now rejects reparse-point preset directories and existing linked preset files before atomic save or load. Focused color-preset coverage passes 3/3, preserving the offline shared color pipeline without following redirected style-library paths.

**Workspace state path boundary checkpoint (2026-10-18):** The local workspace-selection store now ignores reparse-point state files and directories on both load and atomic save, preserving the existing safe-default startup behavior without following redirected user settings. Focused Foundation coverage passes 17/17; this is local evidence pending hosted revalidation.

**Cache index path boundary checkpoint (2026-10-18):** `JsonCacheIndexStore` now rejects reparse-point cache directories and existing linked index files before atomic save or load. Focused recovery coverage passes 5/5; the full local Release gate passes 235 unit tests and 2 golden tests, while hosted confirmation is pending.

**Export destination boundary checkpoint (2026-10-18):** Video and photo exporters now reject reparse-point destination directories before creating temporary FFmpeg outputs, preserving the atomic destination contract and preventing redirected writes. Focused media/photo export coverage passes 7/7; hosted confirmation remains pending.

**Timeline export destination checkpoint (2026-10-18):** The shared timeline compositor now rejects reparse-point destination directories before building or running the FFmpeg graph. Focused timeline compositor coverage passes 8/8; hosted confirmation remains pending.

**Current unpushed exporter hardening checkpoint (2026-10-18):** The video/photo destination-directory and timeline-compositor path guards pass the full local Release gate at 238 unit tests and 2 golden tests with 0 failures; formatting, repository validation and `git diff --check` also pass. This is local evidence only until the next ten-commit push and hosted revalidation.

**Media path security checkpoint (2026-10-18):** `LocalPathSecurity` now walks existing media files, destination directories and their parents, rejecting reparse-point escapes consistently across probe, decode, waveform import, audio preparation, preview proxy, video export, timeline compositor and photo export. Focused media/path coverage passes 37/37; hosted confirmation remains pending.

**Unified local path security checkpoint (2026-10-18):** The reparse-point walk now lives in `FrameTheory.Domain.LocalPathSecurity` and is shared by media/photo processing plus Gemini transcription and local Whisper executable/model/source validation. Focused transcription coverage passes 27/27, including linked-parent source rejection; hosted confirmation remains pending.

**Shared path guard validation checkpoint (2026-10-18):** Repository validation now fails if the shared `LocalPathSecurity.ContainsReparsePoint` guard or any protected media/photo/transcription entry point is removed. The validator, formatting and diff checks pass; the full local Release gate is 241 unit tests and 2 golden tests.

**Recovery guard consolidation checkpoint (2026-10-18):** `ProjectRecoveryService` now delegates project/media reparse validation to the shared Domain path guard instead of maintaining a second directory walk. Focused recovery coverage passes 5/5 and formatting/diff checks pass; hosted confirmation remains pending.

**Current ten-commit batch checkpoint (2026-10-18):** The unpushed exporter, shared parent-path, transcription privacy, validator and recovery-consolidation increments pass the full local Release gate at 241 unit tests and 2 golden tests with 0 failures; repository validation, formatting and `git diff --check` also pass. Push is held until this tenth commit, then hosted revalidation is required.

**Hosted compatibility correction checkpoint (2026-10-18):** Run `39117126857` is not green: the macOS native-media smoke failed after the initial shared ancestor walk treated macOS’s OS-managed `/Users` firmlink as an unsafe reparse point; Ubuntu and Windows completed successfully. `LocalPathSecurity` now allows only macOS’s documented system aliases (`/var`, `/tmp`, `/etc` and `/Users`) while continuing to reject linked user media/cache paths below those boundaries. A new ten-commit hosted revalidation is required.

**Project-root ancestor checkpoint (2026-10-18):** `JsonProjectRepository` now uses the shared local path guard for project directories, existing files and ancestors, preventing a nested `.ftstudio` path from writing through a linked root. Focused repository coverage passes 6/6; hosted revalidation remains pending after the macOS correction.

**Media import boundary checkpoint (2026-10-18):** Waveform extraction now independently rejects reparse-point media paths before invoking FFmpeg, even when a composed or test probe adapter does not enforce the same rule, and best-effort cleanup cannot mask the original import failure. Focused media-import coverage passes 4/4, the Release build is warning-free through the focused compile, formatting and diff checks pass, and original source bytes remain untouched.

**Package traversal checkpoint (2026-10-18):** `scripts/verify_package.ps1` now rejects a reparse-point package root or directory entry before recursive discovery, then hashes only entries from the fail-closed walk. Native bundle generation and verification also reject linked manifests, linked entries and unexpected subdirectories. PowerShell parsing, a real one-file package checksum smoke, repository validation and `git diff --check` pass; the hosted package-manifest gate now includes a real directory-symlink rejection regression, pending the next hosted run.

**Timeline status binding checkpoint (2026-10-18):** The read-only timeline preview-performance control now binds to `TextBox.TextProperty`, so measured seek/decode/cache metrics are displayed by the actual control instead of being assigned to an unrelated `TextBlock` property. The Release solution build passes with 0 warnings/errors, the full suite passes 225 unit tests and 2 golden tests, desktop shell smoke passes, and formatting/diff checks are clean. This is implementation hardening for FT-902; manual workspace acceptance remains open.

**Latest hosted checkpoint (2026-10-18):** Run `39114967796` passed the complete documentation, Windows, macOS and Linux restore, advisory, build, native-media, formatting, full-test, dependency-inventory, named headless-shell, publish, portable archive, installer, packaged relaunch and checksum matrix; Windows also passed desktop UI automation. No affected issue is closed from this run because FT-901/FT-902/FT-1002 still require manual GUI acceptance and FT-1003/FT-1005/FT-1006 still require legal, signing, clean-host or final-audit evidence.

**Current batch checkpoint (2026-10-18):** Local Release validation passes 225 unit tests and 2 golden tests with 0 failures; repository documentation/schema validation, formatting and `git diff --check` also pass. This checkpoint covers the recovery, storage, workspace-state, color-golden, capability-cancellation, credential-cancellation, package-integrity and relink-fingerprint increments in the current nine-commit batch. The batch is being pushed only after its tenth commit; hosted revalidation is required before treating these changes as cross-platform evidence.

**Storage recovery boundary checkpoint (2026-10-18):** `ProjectRecoveryService` now rejects reparse-point project directories, parent media directories and final media entries before missing-media detection or relink resolution. Domain-level relative-path validation remains enforced. Focused recovery coverage passes 3/3 and the Release solution build passes with 0 warnings/errors; unsupported local symlink creation is skipped by the test on hosts without the required privilege.

**Workspace-state persistence checkpoint (2026-10-18):** `FileWorkspaceStateStore` now writes a unique temporary file with write-through flushing, atomically replaces the selected-workspace state and cleans temporary artifacts on success or failure. Focused Foundation coverage passes 15/15; the Release solution build and formatting verification pass with 0 warnings/errors. Cross-platform interactive GUI acceptance remains separately open under FT-901/FT-902.

**Color golden-test checkpoint (2026-10-18):** The shared color preview golden test now compares the rendered PPM bytes against a fixed SHA-256 fingerprint instead of generating expected and actual outputs from the same implementation. Golden coverage passes 2/2; formatting and diff checks pass. A changed color pipeline now causes a deterministic test failure rather than silently updating both sides of a tautological comparison.

**Capability cancellation checkpoint (2026-10-18):** FFmpeg, ffprobe and platform GPU capability probes now terminate their process trees when a refresh is canceled, preventing orphaned native processes while preserving the existing missing/unknown diagnostics. Focused Foundation coverage passes 15/15; the Release build passes with 0 warnings/errors and formatting is clean.

**Credential-store cancellation checkpoint (2026-10-18):** macOS `security` and Linux `secret-tool` operations now terminate their child process trees when secret-store I/O is canceled, preventing a canceled key operation from lingering outside the application. Focused Gemini configuration coverage passes 3/3; the Release build passes with 0 warnings/errors and formatting is clean.

**Relink fingerprint checkpoint (2026-10-18):** Photo and timeline media relink now hashes the selected replacement through an asynchronous, cancellation-aware SHA-256 stream, updates the persisted content fingerprint and preserves the existing `MediaId` so editable timeline references remain stable. Focused recovery coverage passes 5/5; the Release build passes with 0 warnings/errors and formatting is clean.

**Latest hosted checkpoint (2026-10-18):** Run `39110323787` passed documentation/schema validation plus Windows, macOS and Linux restore, build, advisory, native-media, formatting, test, publish, portable archive, packaged relaunch and checksum gates. The corrected shared-compositor timeline preview assertion passed on all three runners; Windows also passed the desktop shell and UI automation smoke, and the per-OS installer validation paths passed. The local Release gate for this pushed batch passed 222 unit tests and 2 golden tests with 0 build warnings/errors. FT-602 is now closed with separate real Windows local-engine evidence; FT-902 and FT-1002 remain open for manual/platform acceptance, while legal, signing and optional live-Gemini gates remain separate.

**Latest hosted checkpoint (2026-10-18):** Run `39108819166` passed documentation/schema validation plus Windows, macOS and Linux restore, build, advisory, native-media, formatting, test, publish, portable archive, packaged relaunch and checksum gates. The native-media smoke now includes the corrected shared-compositor timeline preview assertion (`timeline-preview=ok`). Windows also passed the desktop shell and UI automation smoke; Windows installer, macOS DMG and Linux DEB validation all passed on their respective runners. The local Release gate passes 222 unit tests and 2 golden tests with 0 build warnings/errors; formatting and `git diff --check` pass. The optional local Whisper path now has real Windows spoken-fixture evidence; FT-902 and FT-1002 remain open for explicit manual/platform acceptance, while live Gemini, legal and release gates remain separate.

**FT-602 local engine acceptance checkpoint (2026-10-18):** `tools/FrameTheory.LocalWhisperSmoke` ran the production `WhisperCliTranscriptionProvider` against a locally synthesized spoken WAV using `openai-whisper` 20250625 and the official `tiny.en` model. The run produced seven source-timed words, preserved the source fingerprint `sha256:7577d26c6d8445c884a6478b869c0104a23573891c0a1a8f422e7ceb1dd4f700`, reported progress through completion and cleaned its temporary JSON output. The external executable, model, FFmpeg helper and fixture stayed outside Git; live Gemini provider verification remains optional and unverified.

**Previous hosted checkpoint (2026-10-18):** Run `39107146281` passed documentation/schema validation plus Windows, macOS and Linux restore/build, advisory, native-media, formatting, test, publish, portable archive, installer/DMG/DEB, relaunch and checksum jobs. Windows also passed the desktop shell, UI automation and local-Whisper control discovery smoke. The local Release gate at that checkpoint passed 216 unit tests and 2 golden tests with 0 build warnings/errors; formatting and `git diff --check` passed. FT-602 remained open because live approved Gemini/key/spoken-fixture verification and an actual local Whisper executable/model smoke were not available in that environment.

**FT-902 edited-preview checkpoint (2026-10-18):** The production desktop host now renders the selected timeline frame through the same FFmpeg compositor graph used for export, including timeline transforms, transitions, grades, titles, captions and approved watermark layers, instead of showing only the raw source frame. A bounded single-frame PPM renderer has success, bounds/cancellation, failure-cleanup and UI-error coverage, the native timeline smoke asserts a real compositor preview, and the local Release gate passes 222 unit tests and 2 golden tests with 0 build warnings/errors. Local native-media execution is unverified because this host has no `ffmpeg` executable; hosted verification is required before claiming cross-platform evidence. FT-902 remains open for manual workspace and non-Windows UI acceptance.

**FT-602 UI progress checkpoint (2026-10-18):** Offline transcript import, local Whisper and Gemini transcription now gate queued `Progress<T>` callbacks after provider completion, preventing a stale 90% status update from overwriting the final verified-caption success message. The desktop transcription suite passes 8/8 in five consecutive runs; the Release build remains warning-free.

**FT-902 hosted correction checkpoint (2026-10-18):** Hosted run `39108170113` correctly rejected the first compositor-preview implementation on Windows, macOS and Linux because the shared timeline graph exposed an audio output while the preview mapped video only. Commit `0a8acbe` routes that preview-only audio branch to `anullsink` and adds an audio-bearing regression assertion; the run remains failed evidence and must not be cited as green. A new ten-commit batch is required before hosted revalidation.

**FT-902 preview error checkpoint (2026-10-18):** Compositor preview now reports missing FFmpeg diagnostics explicitly, handles invalid renderer arguments through the desktop failure status path, and keeps temporary output cleanup covered. Focused desktop preview coverage passes 10/10; the corrected native smoke remains pending hosted revalidation.

**FT-902 preview UX checkpoint (2026-10-18):** The Video workspace now distinguishes `Rendering timeline frame ... with compositor` from raw/proxy decoding in its accessible status text. The nullable production renderer path is warning-free and the focused desktop preview suite remains 10/10.

**FT-602 source-integrity checkpoint (2026-10-18):** `JsonTranscriptProvider` now requires the selected local source to exist as a regular file and verifies the transcript's declared `sha256:` fingerprint against the source bytes before exposing any word timings. Focused transcription coverage passes 14/14; the full Release gate passes 209 unit tests and 2 golden tests, the build has 0 warnings/errors, formatting and `git diff --check` pass. Live Gemini/key/spoken-fixture verification and a bundled local ASR engine remain open.

**AI and transcription trust-boundary checkpoint (2026-10-18):** Remote transcription fingerprints the original selected media before and after derived-audio processing and rejects returned words if the source changes. Gemini plan requests reject malformed or oversized inline images and oversized text inputs before network use; plan and transcription responses are capped at 4 MiB before deserialization. Focused Gemini interaction coverage passes 7/7 and focused transcription coverage passes 19/19; no user media or key was used.

**Hosted trust-boundary verification (2026-10-18):** Run `39106385749` passed documentation/schema validation and the Windows, macOS and Linux build, native-media, formatting, test, portable archive, installer/DMG/DEB and checksum jobs after the source-provenance and Gemini payload-bound changes. Windows UI automation and the new verified-transcript control remain green; this does not clear live Gemini, local-ASR, manual GUI, legal, signing or clean-host release gates.

**FT-602 local Whisper checkpoint (2026-10-18):** The Video workspace now exposes `Transcribe with local Whisper`, which accepts a user-selected executable, model and local source, invokes fixed non-shell arguments, parses source-timed JSON words, verifies original-media SHA-256 before and after processing, cancels safely and cleans temporary output. Focused transcription coverage passes 23/23 and focused desktop transcription coverage passes 8/8; no executable, model, user media or API key entered Git. Live Gemini/key/spoken-fixture evidence and a hosted run for this batch remain pending.

**FT-602 offline UI checkpoint (2026-10-18):** The Video workspace now exposes `Import verified transcript JSON`, which selects a local audio/video source and source-bound transcript sidecar, verifies the source fingerprint before mutation, imports editable verified caption cues offline and reports cancellation/failure without changing the current timeline. Focused desktop transcription coverage passes 6/6; hosted run `39105543584` passed the Windows UI smoke with the new control, plus the Windows/macOS/Linux build, native-media, packaging and artifact checks.

**Local Windows verification (2026-10-18):** `scripts/smoke_desktop.ps1 -RequireWindowTitle` passed against the Release desktop assembly, and `dotnet test FrameTheory.sln --configuration Release --no-restore --no-build` passed 209 unit tests plus 2 golden tests. `scripts/smoke_desktop_ui.ps1` reached the real window but could not complete the `Ctrl+3` assertion because this non-interactive session cannot deliver `SendKeys` to the foreground desktop; hosted UI evidence remains the authoritative result and FT-901/FT-902/FT-1002 stay open.

**Current implementation note:** FT-003 now probes the OS, storage, FFmpeg/ffprobe and platform GPU adapters without claiming acceleration when a probe is unavailable; CPU fallback remains explicit. FT-006 now has a hosted three-OS FFmpeg deterministic timeline spike covering seek, two video layers, two audio layers, transitions, AI-applied edits, queue retry and Reel export; MLT was not adopted and interactive GUI parity remains a separate gate. FT-602 now has consented desktop transcription wiring with source-timed caption import, reparse-point rejection, FFmpeg extraction of the first audio track from local video and a resumable Gemini Files API path for approved audio over 20 MiB; live user-key/spoken-fixture verification and local ASR remain pending. FT-706 now has a consented multimodal initial-plan path from sampled video/photo frames, including inline PNG and larger-scope Files API inputs, plus a desktop natural-language revision flow; both validate provider output before proposal display, chain interaction IDs, record redacted local history and preserve transactional undo; live provider-to-plan verification remains pending. FT-902 now exposes real current-frame preview plus cancellable frame-accurate Play/Stop playback through the shared FFmpeg preview decoder; interactive workspace acceptance remains pending. FT-903 now has bounded repeatable seek traces with p50/p95 latency and decode-throughput output plus an optional content-fingerprint-keyed disk proxy with atomic creation, budgeted eviction and hosted native-media validation on Windows, macOS and Linux; interactive GUI acceptance remains pending. FT-904 now distinguishes missing executables from nonzero probe failures, captures version output from either stdout or stderr, exposes bounded actionable unknown-state details and lets users rerun the capability check after fixing dependencies; hosted validation passes while cross-platform interactive failure UX remains pending. FT-1003 now has a reproducible CI/local NuGet advisory scan plus per-run NuGet and FFmpeg/ffprobe evidence artifacts; licensing, codec and native redistribution clearance remain pending. FT-1005 now supports opt-in self-contained publishes in addition to framework-dependent validation packages and has a hosted-verified reviewed native bundle mechanism; clean-host acceptance, legal redistribution, signing and notarization remain pending. FT-1101 now provides non-destructive imported photo overlays with blending, opacity, clipping, persisted brush masks and clone-style retouch strokes, with UI controls and hosted native export/reopen evidence. FT-1102 now includes mask-aware non-destructive photo grading, deterministic scopes, translation tracking, decoded-frame motion reporting and persisted FFmpeg stabilization; visual-quality acceptance remains open. FT-1103 now has editable speed-ramp points with synchronized audio timing, persistence, Video workspace controls and real FFmpeg export coverage. FT-1105 now provides a real desktop multi-target batch export action for landscape, square and portrait MP4s, validated atomic AI recipe save/load through the existing allowlist and approval flow, cancellation, per-target progress, duplicate-path validation and social-canvas fit before FFmpeg padding; approved brand kits remain pending. The desktop Video workspace now saves and reopens versioned `.ftstudio` projects with atomic JSON, editable timeline operations, relative media references and media-ID remapping. E08 is aggregate-complete; FT-803 has hosted branded-output evidence in run `39102849779`: the native timeline smoke asserted `watermark=approved` and each portable archive contains the byte-verified approved logo asset; release-owner rights/visual review remains a separate release gate.

**FT-803 runtime integrity checkpoint (2026-10-18):** Commits `78c16f1` and `c63659a` make the approved-watermark resolver verify the packaged logo's exact SHA-256 at runtime, reject a missing, reparse-point or modified asset before use, and reject arbitrary watermark paths in the regression suite. Focused branding tests pass 1/1; the full Release build passes with 0 warnings/errors, 200 unit tests and 2 golden tests pass, formatting and `git diff --check` pass. The hosted branded-output evidence remains run `39102849779`; release-owner rights/visual review is unchanged.

**FT-803 runtime smoke checkpoint (2026-10-18):** With the validated FFmpeg/ffprobe 9.0.2 pair on PATH, `FrameTheory.TimelineExportSmoke` generated synthetic H.264/AAC clips, reopened the portable approved-watermark project, exported the real branded timeline, retried the queue, applied the AI fixture, produced all three social targets, and emitted the Reel. The final MP4 contains video/audio at `16x8` with `bt709`; the smoke reported `watermark=approved`. The initial attempt without the FFmpeg directory on PATH failed clearly at `ffprobe` resolution and was rerun with the documented dependency path.

**FT-1005 macOS cleanup follow-up (2026-10-18):** Hosted run `39103650378` passed the macOS build, DMG creation, bundled media import, clean-`PATH` smoke, metadata checks and desktop launch, then failed only because the first `hdiutil detach` returned `Resource busy`. Commits `7a4c394` and `f69db0f` add five bounded detach retries plus process teardown draining; the corrected hosted run `39104297945` is green across Windows, macOS and Linux.

**FT-1005 smoke teardown hardening (2026-10-18):** Commit `f69db0f` waits for the desktop process exit result, drains a short filesystem-release interval and disposes the process handle before package cleanup. The repository validator in `2f34e1f` guards the macOS retry contract; hosted run `39104297945` passed the corrected DMG cleanup path.

**Documentation validator hardening (2026-10-18):** Commit `eaf293b` adds exact approved-logo hash/regular-file checks to `scripts/validate_repository.py`. The local Windows host lacks a real Python interpreter (only the Microsoft Store alias), so the Python validator remains hosted-only; hosted run `39104297945` passed it, and equivalent local PowerShell hash and CI-guard checks passed.

**FT-1005 hosted bundle follow-up (2026-10-18):** Run `39098739893` passed documentation, macOS and Linux validation but failed only in the Windows installer media smoke because the validation harness copied the Chocolatey-resolved executable names without the adjacent native runtime files. The failed run is not release evidence. Commits `4e5ccfb`, `ab65fff`, `f4606d5` and `30fe095` now select an actual executable pair when available, copy adjacent reviewed support files, expose bounded ffprobe diagnostics and guard the bundle contract; the next exact 10-commit push must re-run the hosted matrix before this checkpoint is upgraded.

**FT-1005 hosted bundle assertion follow-up (2026-10-18):** Run `39099250089` proved the repaired Windows package could import video and extract audio through the bundled runtime (`kind=Video` and `has_audio=True`) but failed its PowerShell assertion because the two-line output array made `-notmatch` treat the nonmatching second line as a failure. Commit `c5dc702` now tests the array for any matching video line; the run remains failed and is not release evidence.

**FT-1005 bundle trust local checkpoint (2026-10-18):** Commits `0d5aa6f`, `4297c64` and `139908a` reject linked bundle directories, require explicit directory-level CI guards and assert that an adjacent support file survives each Windows, macOS and Linux installer path. Local PowerShell/shell syntax and diff checks pass; hosted rerun is required before updating release evidence.

**FT-1005 hosted bundle checkpoint (2026-10-18):** Run `39099847174` passed documentation/schema validation and all Windows, macOS and Linux jobs. Windows installer validation proves the actual bundled FFmpeg/ffprobe pair, adjacent support-file packaging, video import, audio probing, UI launch and uninstall; macOS DMG and Linux DEB paths prove the same support-file packaging plus native media smoke. The mechanism is verified, but FT-1005 remains partial for clean-host production acceptance, legal/codec redistribution approval, signing and notarization.

**FT-1005 clean-PATH local checkpoint (2026-10-18):** The next hosted installer validation adds a managed-media smoke with an empty `PATH` on Windows, macOS and Linux while setting only the packaged bundle directory. Local workflow syntax and static guards pass; no clean-PATH hosted result is claimed until the next pushed matrix completes.

**FT-1005 clean-PATH hosted checkpoint (2026-10-18):** Run `39100413937` passed the complete documentation, Windows, macOS and Linux matrix, including bundled video/audio media import with host FFmpeg removed from `PATH`, support-file package assertions, installer relaunch, uninstall and checksum verification. This proves packaged runtime resolution independent of host `PATH`; clean-host production recovery, legal/codec redistribution, signing and notarization remain open.

**FT-1003 native provenance local checkpoint (2026-10-18):** `scripts/generate_native_media_manifest.ps1` now records bounded native tool version output, per-file SHA-256 and byte length for each staged bundle while rejecting linked directories/files. CI emits one manifest per OS installer validation; hosted confirmation is pending, and the artifact remains provenance evidence rather than legal approval.

**FT-1003 native provenance hosted checkpoint (2026-10-18):** Run `39100943885` passed documentation/schema validation and all Windows, macOS and Linux jobs while emitting `native-bundle-windows.json`, `native-bundle-macos.json` and `native-bundle-linux.json`. Each records the staged files and actual tool version output; this improves reviewability but does not clear license, codec or redistribution approval.

**FT-1003 native provenance verification checkpoint (2026-10-18):** `scripts/verify_native_media_manifest.ps1` now re-hashes every staged file, checks exact file-set/size agreement and validates both tool records before each installer package is built. The next hosted matrix must pass this verifier on all three OS jobs before the checkpoint is upgraded.

**FT-1003 native provenance verification hosted checkpoint (2026-10-18):** Run `39101406586` passed documentation/schema validation and the Windows, macOS and Linux jobs with pre-package native manifest hash verification. The verifier checked exact file sets, byte lengths, SHA-256 values and both native tool records for each staged bundle; this is reproducible provenance evidence, not legal, codec or redistribution approval.

**FT-803 approved watermark asset checkpoint (2026-10-18):** The policy-named source attachment `Elegant Frame Theory Monogram.png` is imported byte-for-byte at `assets/brand/frame-theory-original.png` with SHA-256 `0BD29D37FBCC4C753B8870A65E3A02E23460C1A5C305E53E6CD2D9D2F151F388`. The desktop Video workspace exposes an explicit approved-watermark action and publishes the asset beside the application. Hosted run `39102849779` asserted the real watermark path and portable archive contents; release-owner rights/visual review remains a separate release gate.

**FT-803 approved watermark local media checkpoint (2026-10-18):** Local `FrameTheory.TimelineExportSmoke` ran with the documented FFmpeg 9.0.2 validation pair and generated synthetic H.264/AAC media. The real timeline export reported `watermark=approved`; ffprobe verified the final MP4 contains video and audio, while project round-trip, retry, AI, batch, rough-cut and Reel outputs also completed. Hosted run `39102849779` repeated the branded assertion on the Windows, macOS and Linux jobs; release-owner rights/visual review remains open separately.

**FT-803 watermark canvas-fit checkpoint (2026-10-18):** The compositor now scales watermarks proportionally to fit within 25% of each target canvas before applying opacity and margin. Local branded batch exports produced verified landscape `28x16`, square `16x16` and portrait `16x28` video outputs; focused compositor coverage passes 7/7 and the Release build has 0 warnings/errors.

**FT-803 watermark persistence checkpoint (2026-10-18):** Approved watermark state now serializes as the portable bundled reference `assets/brand/frame-theory-original.png` instead of an install-specific absolute path. The timeline smoke publishes the asset, saves/reopens the project with `watermark=approved`, and exports the real branded timeline; this preserves custom watermark paths as local user assets. Hosted run `39102849779` passed the same project-roundtrip assertion on all three OS jobs; release-owner rights/visual review remains open separately.

**FT-1005 native media bundle checkpoint (2026-10-18):** Optional release-owner-supplied FFmpeg/ffprobe directories are now validated as a complete pair and copied into portable, Windows, macOS and Linux package payloads; the runtime resolves adjacent binaries before falling back to `PATH`. Local Release validation passes 195 unit tests and 2 golden tests, the desktop build has 0 warnings/errors, formatting/diff checks pass, and all three shell packagers pass syntax checks. Hosted confirmation is required; this does not clear native license, codec, redistribution, signing or clean-host gates.

**FT-1005 bundle trust-boundary checkpoint (2026-10-18):** Windows package scripts reject reparse-point native tools and Unix package scripts reject symlinked tools; CI now statically guards those checks and runs the packaged media-import smoke through `FRAME_THEORY_FFMPEG_DIR` on each installer path. The locator regression suite passes 2/2; hosted confirmation remains pending for this hardening batch.

**FT-1005 bundle hardening local gate (2026-10-18):** Full local Release validation passes 196 unit tests and 2 golden tests, the desktop build has 0 warnings/errors, formatting and diff checks are clean, all three shell packagers pass syntax validation, and both PowerShell packagers parse successfully. Push is held until this exact 10-commit boundary; hosted confirmation is the next step.

**FT-903 desktop telemetry checkpoint (2026-10-18):** The Video workspace now records measured decode latency, rolling decode FPS, process working-set bytes and sample count for real frame previews and playback. The metrics are exposed in a read-only accessible performance field beside the preview controls; focused desktop coverage passes 7/7, the full Release gate passes 194 unit and 2 golden tests, the desktop build has 0 warnings/errors, Windows UI automation reaches 17 controls at 1280x800, and hosted run `39092893198` passes the Windows/macOS/Linux matrix. HiDPI, manual cross-platform GUI acceptance and long-session visual-performance sign-off remain open.

**FT-902 proposal-accessibility checkpoint (2026-10-18):** AI workspace validation, approved-apply, undo and natural-language revision controls now expose stable automation names alongside the existing analysis action. Windows UI automation reaches 21 controls at 1280x800, and hosted run `39093635415` passes the Windows/macOS/Linux matrix; manual view synchronization, HiDPI and macOS/Linux GUI acceptance remain open.

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

**FT-803 checkpoint:** Timeline editing now offers amplitude-peak and energy-onset beat marker modes plus explicit approved and custom PNG/JPEG/WebP watermark paths. Watermarks are validated, looped for the full render, opacity-adjusted and overlaid non-destructively; the approved Frame Theory source is now packaged and byte-verified, while release-owner rights/visual review and hosted branded-output confirmation remain open.

**FT-602 checkpoint:** The Video workspace now offers a consent-gated “Transcribe approved audio” action. It reads the OS-stored Gemini key, sends only approved local audio or a bounded FFmpeg-extracted first audio track from an approved local video through the real transcription provider, uses the resumable Files API for audio over 20 MiB, deletes the temporary remote file, rejects ungrounded responses, groups source-timed words into editable verified caption cues, and preserves the source SHA-256 in editor state. The live API/key/spoken fixture and local ASR remain unverified.

**FT-602 long-audio checkpoint:** The provider uses the official Gemini upload host, validates both the resumable session URL and the returned file URI, sends the uploaded URI to `gemini-3.5-transcribe`, and deletes the returned `files/*` resource in a bounded cleanup path. Fake-HTTP coverage verifies the upload, URI input, delete request and rejection/cleanup of an untrusted URI; live provider/key and local ASR acceptance remain open.

**FT-706 checkpoint:** The AI workspace now accepts a natural-language revision request, sends only text to the bounded Gemini follow-up client, validates the returned schema/semantics locally, updates the proposal JSON for continued revisions, and leaves the timeline untouched until explicit approval. The previous interaction ID and redacted usage metadata are retained through the existing local history path.

**FT-706 provider resilience checkpoint (2026-10-18):** Gemini interaction retries now use bounded exponential backoff, honor a provider `Retry-After` hint without exceeding the 30-second cap, and retain typed timeout, cancellation, rate-limit and transport failures. Local JSONL history records only model, interaction outcome, attempt count, prompt length and returned usage; diagnostics remain secret-redacted. Focused Gemini interaction coverage passes 5/5 and the Release AI build passes with 0 warnings/errors. Live user-key/provider-to-plan execution remains unverified.

**FT-706 usage disclosure checkpoint (2026-10-18):** The AI workspace now displays returned input/output/total token usage, bounded attempt count, and approved-sample/upload-byte scope after initial creative-direction analysis or a follow-up revision. The view-model tests cover both paths, while unavailable provider usage is displayed explicitly rather than fabricated.

**FT-706/702 activity-log checkpoint (2026-10-18):** The local Gemini JSONL history now records a safe operation name and aggregate input-scope summary alongside timestamp, outcome, attempts and usage; a bounded reader exposes recent entries in the AI workspace as a read-only activity log. Raw prompts, keys, media bytes and provider responses remain excluded. Focused AI coverage passes 11/11, the full Release gate passes 194 unit and 2 golden tests, the desktop build has 0 warnings/errors, Windows UI automation reaches 22 controls, and hosted run `39094586204` passes the Windows/macOS/Linux matrix; live provider acceptance remains open.

**FT-804 checkpoint:** The shared export queue now exposes queued/running/succeeded/failed/canceled state, bounded in-memory job history, immediate cancellation completion and retry of failed/canceled jobs. Video export exposes a real “Retry last export” action; exporter temp-file cleanup and atomic finalization remain in the existing exporter contracts.

**FT-1103 kinetic-caption checkpoint (2026-10-18):** Verified transcript word timings now survive caption modeling, `.ftstudio` timeline persistence and restore. The Video workspace exposes a Kinetic word captions toggle; when enabled, the FFmpeg graph emits one timed `drawtext` stage per source-grounded word. Focused caption/editor/persistence coverage passes 24/24, the full unit/golden/build/format gate passes 148/2 with 0 build warnings/errors, and the native timeline smoke produced a verified kinetic-caption MP4. Hosted run `39068043241` passed the same quality and native-media matrix on Windows, macOS and Linux. Speed-ramp curves remain open under FT-1103.

**FT-1103 speed-ramp checkpoint (2026-10-18):** Selected video clips now accept validated 0.25x–4x speed-ramp points, persist them in `.ftstudio` state, expose apply/clear controls in the Video workspace and render synchronized piecewise video/audio segments through FFmpeg. Graph coverage verifies timebase normalization for transition compatibility; the native smoke produced and probed real ramped video/audio, caption, retry, AI and Reel MP4 outputs at 16x8 BT.709. Full local Release tests pass 152/2, build has 0 warnings/errors, formatting verification passes, and hosted run `39069354787` passed the complete Windows/macOS/Linux quality and native-media matrix for `aee3088`.

**FT-1104 reference-match checkpoint (2026-10-18):** The offline `ColorReferenceMatcher` now derives a bounded exposure, saturation, temperature and tint grade from two approved RGB samples without network or media upload, providing a deterministic headless seed for the existing reusable color-preset store. The Color workspace now saves and loads atomic style-library JSON files, applies selected grade/LUT styles to the real before/after preview and exposes accessible controls. The headless `GeminiReferenceAnalysisClient` accepts only bounded visual samples, requires explicit consent, converts local PPM previews to inline PNG input for small requests, switches oversized visual samples to the official Files API with URI inputs and bounded cleanup, redacts the API key from payloads, and locally validates the returned editable grade before any caller can apply it. The `RoughCutPlanner` orders imported clips by measured waveform energy/onsets; the Video workspace exposes review/apply controls, and transactional apply preserves linked audio, beat markers, editable settings and undo. The native smoke now applies the reviewed plan, saves/reopens the editable timeline and exports a real rough-cut MP4. Focused matcher/inspector/reference/rough-cut coverage passes 16/16 plus the large-reference Files API test; full local Release tests pass 162/2, build has 0 warnings/errors and formatting verification passes. Hosted runs `39069905388`, `39071362366` and `39071860694` passed the style-library, rough-cut export/persistence and large-reference code matrices, including documentation, native-media, format, test, publish and checksum gates on Windows, macOS and Linux. Live-key evidence and reference-grade semantic acceptance remain open; FT-1104 stays open until those limits and the full acceptance evidence are resolved.

**FT-1104 reference UI checkpoint (2026-10-18):** The Color workspace now lets users select two imported visual previews, see the exact source/reference upload scope, require explicit consent, request a Gemini reference grade, review the bounded recommendation and apply it only through a separate editable color action. The reference provider now uses bounded exponential retries, capped `Retry-After`, typed timeout/rate-limit/cancellation failures, returned usage accounting and redacted local history. Focused desktop/reference coverage passes, local UI automation verifies both Color actions, and live-key/reference-semantic acceptance remains open.

**FT-1105 batch-export checkpoint (2026-10-18):** `TimelineBatchExporter` now runs validated landscape, square and portrait timeline targets sequentially with per-target progress, cancellation and duplicate destination rejection. The Video workspace exposes “Export all social targets” through a folder picker; each target is rendered at its exact social dimensions, and the compositor fits oversized source clips before padding so square/portrait exports do not fail on larger media. The AI workspace now saves and loads named `.ftrecipe.json` files atomically only after allowlist/schema validation; loading revalidates against the active project and never applies actions without explicit approval. Focused batch/desktop/recipe coverage passes 32/32; the full Release solution passes 189 unit and 2 golden tests, build has 0 warnings/errors and formatting verification passes. Hosted runs `39080099225`, `39080435271` and `39080954139` passed native batch MP4, recipe, documentation, advisory, build, format, tests, publish and checksum jobs on Windows, macOS and Linux; the latest run also verifies deterministic cross-platform progress completion. Approved brand-kit assets remain open.

**FT-1005 portable archive checkpoint (2026-10-18):** CI now packages each per-OS desktop publish tree as `frame-theory-<platform>-portable.zip` through `scripts/archive_package.ps1` before generating and verifying the SHA-256 manifest. The helper rejects directory output paths and archives placed inside the source tree, while CI opens each archive and asserts that `FrameTheory.Desktop.dll` is present. A local archive smoke verified the generated zip contents; hosted runs `39081424352` and `39082055065` passed archive creation, payload assertion, native media, tests, publish, manifest verification and artifact upload on Windows, macOS and Linux. These are portable validation archives, not installers; native FFmpeg bundling, signing/notarization and clean-host relaunch remain open.

**CI FFmpeg install checkpoint (2026-10-18):** Windows run `39082333029` correctly exposed a Chocolatey feed `404` while installing FFmpeg, so the workflow now falls back to the pinned WinGet `Gyan.FFmpeg` 9.0.2 package, locates its static executable, and preserves the existing FFmpeg/ffprobe evidence capture. Hosted run `39082588976` passed the repaired Windows path plus the full macOS/Linux matrix; no application behavior changed.

**FT-1003 runtime provenance checkpoint (2026-10-18):** `docs/SBOM.md` now records the pinned Windows validation package source, GPL status and installer SHA-256 without treating it as redistribution approval. Hosted run `39082884398` passed the documentation, restore/build, advisory, native-media, format, test, publish, portable-archive and checksum matrix on Windows, macOS and Linux.

**FT-1003 package-license metadata checkpoint (2026-10-18):** `scripts/generate_sbom.ps1` now emits a separate `nuget-licenses.json` report from each restored package `.nuspec`, preserving package ID, version, license expression/URL and whether metadata was found. Local generation produced 39 package-version records and captured Avalonia `MIT`; hosted run `39087349669` passed the full Windows/macOS/Linux matrix with the new report. This is reproducible evidence, not legal or redistribution approval.

**FT-1003 SBOM portability fix (2026-10-18):** The first hosted attempt `39087079726` correctly failed on Unix because `USERPROFILE` is absent. The follow-up now selects `NUGET_PACKAGES`, `USERPROFILE` or `HOME` in that order and validates the cache directory before parsing metadata; a simulated Windows run with `USERPROFILE` unset and `HOME` set produced 39 records. The failed run is not release evidence.

**FT-1003 SBOM assertion checkpoint (2026-10-18):** CI now rejects an empty `nuget-licenses.json` and requires the restored Avalonia package to retain its `MIT` metadata record. The local generated report passes the same assertions, and hosted run `39087705477` passed the new gate on Windows, macOS and Linux. This remains metadata evidence rather than legal clearance.

**FT-901 accessibility checkpoint (2026-10-18):** The desktop window, workspace tab strip and individual Video/Photo/Color/AI tabs now expose stable Avalonia automation names while preserving the existing keyboard shortcuts and 1280x800 defaults. Local Foundation tests passed 13/13, the desktop Release build passed with 0 warnings/errors, formatting and diff checks passed, and hosted run `39085046211` passed the full Windows/macOS/Linux matrix. Interactive 1280x800/HiDPI layout and keyboard acceptance remain manual gates.

**FT-901 workspace-state checkpoint (2026-10-18):** The real desktop service now persists the selected workspace index in an atomic, local-only JSON file under the OS local application-data directory and restores only valid workspace indices. Unit coverage verifies invalid saved state falls back to Video, selection survives view-model recreation, and a file-backed state round trip. The default test view model uses a no-op store so tests never touch user settings. Focused Foundation tests passed 15/15 and the desktop Release build passed with 0 warnings/errors. Interactive 1280x800/HiDPI and keyboard acceptance remain manual gates.

**FT-902 accessibility checkpoint (2026-10-18):** The Video workspace now exposes stable automation names for the current-frame preview, play/stop transport, frame stepping, selection, clip edits, caption import/export, approved transcription, project save/open, timeline export/cancel/retry, and the Photo/Color before/after previews. Local Foundation tests passed 15/15, the desktop Release build passed with 0 warnings/errors, formatting and diff checks passed. Manual view synchronization and cross-platform interactive acceptance remain open.

**FT-901/FT-902 Windows UI automation checkpoint (2026-10-18):** `scripts/smoke_desktop_ui.ps1` now launches the real managed desktop process, finds its native UI Automation window, verifies 1280x800 bounds, selects all four workspace tabs, and checks 14 reachable workspace controls. Local Windows execution passed, and hosted run `39088633198` passed the documentation plus Windows, macOS and Linux build/test/package matrix, including the Windows UI automation smoke. The smoke accepts stable Avalonia automation names with visible-label fallback; earlier hosted timing/name failures were caught and corrected in `2e3b58c` and `50ac268`. This provides Windows UI evidence but does not replace macOS/Linux GUI acceptance or HiDPI hardware testing.

**FT-901 adaptive-flow checkpoint (2026-10-18):** Dense photo, color, AI and timeline control rows now use Avalonia wrapping panels instead of fixed-width horizontal stacks, allowing controls to flow onto additional lines at narrow or scaled widths without removing functionality. Release desktop build passed with 0 warnings/errors, the full Release suite passed 194 unit and 2 golden tests, formatting and diff checks passed, and the Windows UI smoke passed with 1280x800 bounds, a 960x640 minimum resize, four workspaces, 22 reachable controls and the `Ctrl+3` Color workspace shortcut. HiDPI hardware and non-Windows interactive acceptance remain manual gates.

**FT-1005 installer checkpoint (2026-10-18):** Added reproducible unsigned validation packaging for Windows Inno Setup, macOS DMG and Linux DEB, with CI checks for package contents plus Windows install/relaunch/uninstall. The desktop smoke helper now launches either a managed DLL through the .NET host or a self-contained executable. Local Git Bash shell syntax, PowerShell parsing, focused Foundation tests (15/15), Release build (0 warnings/errors) and diff checks pass. Direct self-contained EXE launch is denied by the current Windows host policy, so the Windows executable/install smoke remains hosted-CI evidence; native FFmpeg bundling, signing/notarization and legal redistribution remain open.

**FT-1005 hosted installer checkpoint (2026-10-18):** Hosted run `39086158425` passed the full Windows/macOS/Linux matrix. Windows built an unsigned Inno Setup installer, installed it into a temporary directory, launched the installed self-contained executable with the real window-title smoke, and removed it through the uninstaller. macOS built and mounted an unsigned DMG and verified the executable inside the `.app` bundle. Linux built a DEB and verified its metadata and launcher entry. These are validation installers, not signed production releases; FFmpeg/ffprobe remain host dependencies and clean-host, native redistribution, signing/notarization and legal review remain open.

**FT-1005 package-version checkpoint (2026-10-18):** Windows, macOS and Linux validation packaging now accept an explicit version override while retaining the `0.1.0` preview default; Windows rejects invalid versions before invoking Inno Setup, macOS and Linux validate before writing package metadata, macOS rejects unsupported RIDs, and Linux maps `linux-x64`/`linux-arm64` to `amd64`/`arm64` while rejecting unsupported RIDs. CI now asserts the generated macOS plist version and Linux DEB version/architecture. Local PowerShell parsing, Git Bash syntax checks and invalid-version/RID rejection pass; hosted confirmation is pending for the next batch.

**CI correction checkpoint (2026-10-18):** Hosted run `39095947457` stopped in the documentation job because the Windows invalid-version guard asserted provider-specific error text on a Linux PowerShell runner; the guard had already rejected the invalid input. Commit `4c3d718` now asserts the stable nonzero exit contract only. The current batch also rejects unknown macOS host/RIDs and names Windows installers by requested RID; corrected hosted verification remains pending.

**Batch checkpoint (2026-10-18):** The corrected packaging batch now contains 10 local commits since pushed commit `851e4ac`, covering Windows/macOS/Linux version and RID guards, CI metadata assertions and the hosted-failure correction. Push only at this boundary after the final local diff check.

**Manifest security checkpoint (2026-10-18):** `scripts/verify_package.ps1` now uses path-relative containment rather than a vulnerable string-prefix comparison. A normal temporary package verifies successfully, while a `package-sibling` escape is rejected before hashing. The fix is locally validated and awaits hosted confirmation in the next batch.

**FT-1005 packaging guard checkpoint (2026-10-18):** Hosted run `39096221492` passed documentation, Windows and Linux packaging validation plus the complete build/test/native-media matrix, but exposed a macOS-only CI assertion defect: raw XML text matching did not validate the mounted plist even though the DMG was valid. Commit `8b1f3d5` now reads `CFBundleShortVersionString` through macOS `plutil`; commit `efc3a20` adds hosted valid-manifest and sibling-prefix escape regression coverage. The corrected run is pending; unsigned installers, native FFmpeg redistribution, signing and clean-host acceptance remain open.

**FT-1005 macOS metadata correction checkpoint (2026-10-18):** Follow-up run `39096771337` confirmed the DMG executable and all surrounding macOS build/test steps, but `plutil` returned a non-matching value because the packaging script's single-quoted plist heredoc preserved the literal `$version` token. Commit `3b45080` expands the heredoc so the requested version reaches `Info.plist`; commit `4ce9c25` reports the mounted bundle version before asserting it. Corrected hosted confirmation remains pending.

**FT-1005 correction local checkpoint (2026-10-18):** The corrected macOS packager passes Git Bash `sh -n`; all three release shell scripts pass syntax validation, the Windows packager passes PowerShell parsing, and `git diff --check` is clean. The new CI guards reject a quoted plist heredoc and require `plutil -lint`; no hosted success is claimed until the next pushed batch completes.

**FT-1005 corrected hosted checkpoint (2026-10-18):** Run `39097200292` passed documentation/schema validation, Windows/macOS/Linux restore, advisory, build, native-media, formatting, tests, publish, portable archive, installer and checksum jobs. macOS DMG validation now confirms the executable, generated plist version and mounted-bundle smoke; Windows installer and Linux DEB validations remain green. This verifies the implementation and CI gates, but FT-1005 remains partial for clean-host production acceptance, native FFmpeg redistribution, licensing, signing and notarization.

**FT-1005 installer lifecycle batch checkpoint (2026-10-18):** The next 10-commit batch extends Linux DEB validation from extraction to `dpkg` install/remove, installed-binary and installed-launcher smoke, plus launcher-target assertions and macOS bundle identity checks. Local shell syntax and `git diff --check` pass; hosted confirmation is required before updating the release evidence or task state.

**Packaging hardening batch checkpoint (2026-10-18):** The next 10-commit batch adds native macOS plist validation, Windows installer `win-x64` enforcement, valid/sibling/nested checksum-manifest regressions, and synchronized release/security/test documentation. Local validation passed with 194 unit tests, 2 golden tests, a 0-warning desktop Release build, formatting verification, Git Bash syntax checks for all three shell packagers and `git diff --check`. Push is intentionally held until this exact 10-commit boundary; corrected hosted CI confirmation remains pending.

**FT-1005 packaged relaunch checkpoint (2026-10-18):** Hosted run `39086502910` repeated the full matrix after adding real packaged-app startup checks. Windows install/relaunch/uninstall remained green; macOS launched the executable from the mounted DMG; Linux extracted the DEB and launched the packaged self-contained executable under Xvfb. This is stronger package reachability evidence, not clean-host or signed-release evidence.

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

**Current status: PACKAGING MATRIX AND CI ARTIFACTS PARTIAL (2026-10-17).** FT-001 through FT-005, FT-006, FT-101 through FT-105, FT-201 through FT-205, FT-301 through FT-306, FT-401 through FT-405, FT-501 through FT-506, FT-601, FT-603 through FT-605, and FT-701 through FT-704 are implemented and verified on Windows x64 with .NET SDK 10.0.100 plus hosted native-media evidence for FT-006. FT-602 now also has real local Whisper spoken-fixture evidence through the production adapter; the optional live Gemini transcription path remains unverified. FT-705 executes approved grade, clip, transition, title, caption, keyframe, audio, trusted-preset and Reel operations transactionally; FT-801 provides split, wipe and timed-reveal output across multiple video clips. FT-802 provides real 16:9, 1:1 and 9:16 MP4 presets plus Photo workspace PNG/JPEG import, non-destructive rotate/undo, preview and quality-controlled export; FT-803 adds imported user audio, amplitude/energy waveform-derived editable beat markers and explicit validated watermark selection; FT-804 adds a single-worker cancellable export queue and Video workspace cancel control; FT-706 exposes undo for the last AI transaction plus bounded exponential provider retries, timeout/rate-limit handling and redacted usage history. FT-901 adds keyboard workspace/frame shortcuts, scroll-safe dense workspace layout, automation names for key controls and bounded dense/HiDPI ruler evidence. FT-902 surfaces synchronized selected-clip/playhead context, a real FFmpeg-decoded current-frame preview action and AI approval counts/status in the Video and AI panels; manual workspace acceptance remains open. FT-903 records real cold/warm frame-seek timing, working-set bytes, bounded cache metrics, repeatable p50/p95 seek and decode-throughput output, cancellation/cache policy and an optional content-fingerprint-keyed disk proxy; interactive proxy UI acceptance remains open. FT-904 adds local secret-redacting diagnostics, unhandled-failure capture, stdout/stderr dependency probing, nonzero-exit unknown-state reporting, actionable missing FFmpeg/ffprobe guidance and visible log location. FT-1001 adds an explicit source SHA-256 preservation assertion to the core unit/integration/golden gate. FT-1002 maps all UC-01..UC-06 to executable Windows smoke/test evidence and records the interactive/platform limits. FT-1003 records the actual restored NuGet/native dependency inventory and external FFmpeg redistribution blockers in `docs/SBOM.md`. FT-1004 extends the Windows/macOS/Linux CI matrix with desktop publish/test artifacts and SHA-256 manifests. FT-1005 provides reproducible framework-dependent and opt-in self-contained publish scripts, checksummed archives, unsigned installers and a hosted reviewed native-bundle mechanism; clean-host production recovery, legal redistribution, signing and notarization remain open. Approved watermark release rights remain open. FT-006 uses the tested deterministic FFmpeg timeline path; MLT was not adopted, and interactive GUI parity is a separate gate. This is not a release.

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
- [x] FT-602 source-bound transcription contract, validated JSON fixture adapter, consent-gated Gemini REST provider, reparse-source rejection, FFmpeg video-audio extraction, desktop caption wiring, larger-audio Files API upload/delete and real local Whisper spoken-fixture acceptance are implemented; live Gemini provider verification remains an optional separate release gate
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
- [x] FT-803 imported user audio, waveform-derived amplitude/energy beat markers, creative title/caption slots and packaged approved-watermark selection; local and hosted branded-output evidence passed, while release-owner rights/visual review remains a separate release gate
- [x] FT-804 single-worker cancellable background export queue with Video workspace cancellation, retry, in-memory job history and real FFmpeg retry smoke; macOS/Linux interruption acceptance remains unverified
- [x] Timeline project save/open preserves editable timeline state, reimports relative media and remaps media IDs without mutating the current project on failed open
- [~] Timeline project missing-media detection, transactional relink and moved-project recovery are implemented and tested; manual cross-platform UI acceptance remains open
- [x] FT-706 consented multimodal initial-plan and natural-language revision flows, transactional AI undo, bounded Gemini transport, typed retry/timeout/cancellation failures, usage accounting and prompt/secret-redacted local history meet the issue's offline fallback and rollback acceptance; live provider-to-plan and larger initial-plan verification remain a separate V1 release gate
- [~] FT-901 keyboard workspace/frame shortcuts, scroll-safe layout and automation labels are implemented; interactive 1280x800/HiDPI acceptance remains unverified
- [~] FT-902 synchronized selected-clip/playhead context, real current-frame FFmpeg preview, cancellable frame-accurate Play/Stop playback and AI approval summary are implemented; full manual workspace acceptance remains open
- [x] FT-903 bounded preview cache, cancellable rendering, an optional content-fingerprint-keyed disk proxy, a Video workspace proxy toggle and accessible desktop preview-performance telemetry meet the recorded memory/FPS trace acceptance; hosted native-media smoke validates proxy creation, dimensions and reuse on Windows, macOS and Linux, while interactive long-session sign-off remains a separate V1 release gate
- [x] FT-904 local redacted diagnostics, unhandled-failure capture, stdout/stderr dependency probing, nonzero-exit unknown-state reporting and actionable dependency guidance meet the failure-UX test acceptance; cross-platform interactive failure-UX sign-off remains a separate V1 release gate
- [x] FT-1001 core unit/golden/adversarial suite, caption provenance coverage and source SHA-256 preservation assertion pass on the hosted Windows/macOS/Linux matrix; broader manual/media journeys remain tracked by FT-1002
- [~] FT-1002 UC-01..UC-06 executable smoke/test matrix is documented with Windows evidence and hosted photo/video/native-media evidence; interactive and macOS/Linux acceptance remains open
- [~] FT-1003 dependency inventory, advisory evidence, per-OS native bundle manifests and external FFmpeg/native asset assessment are documented; license/CVE clearance and redistribution approval remain open
- [x] FT-1004 GitHub Actions verifies Windows/macOS/Linux restore/build/format/test/publish jobs and uploads per-OS SHA-256 manifests; optional signing/notarization is not part of this task claim
- [~] FT-1005 framework-dependent and opt-in self-contained publishes, checksummed portable archives, unsigned Windows/macOS/Linux validation installers and the reviewed native-bundle mechanism are available; clean-host production acceptance, legal redistribution and signing remain open
- [~] FT-1006 V1 acceptance audit is recorded with 55 closed implementation issues and explicit remaining P0 blockers; no V1 tag is authorized
- [x] FT-1103 audio beat detection, source-timed kinetic word captions, proxy pipeline and editable speed-ramp curves are implemented with persistence, UI and native-export smoke evidence; hosted run `39069354787` passed on Windows, macOS and Linux
- [x] FT-1101 imported photo overlay composition with normal/multiply/screen blending, opacity, clipping, non-destructive undo, persisted brush masks and clone-style retouch, project persistence and hosted export/reopen smoke
- [~] FT-1102 mask-aware non-destructive photo grading, deterministic color scopes, translation tracking, decoded-frame motion reporting and persisted FFmpeg stabilization are implemented with Photo/Video workspace controls and hosted graph/build/native evidence; visual-quality acceptance remains open
- [~] FT-1104 offline reference matching, reusable style-library UI, consented Color-workspace visual Gemini analyzer with bounded inline/Files API inputs, review-before-apply grade flow and waveform-grounded reviewed rough-cut apply/export are implemented with local and hosted evidence; live-provider and reference-grade semantic acceptance remain open
- [~] FT-1105 multi-target social timeline batch export with cancellation, progress, exact-canvas fitting and validated AI recipe persistence is implemented with hosted evidence; approved brand-kit assets remain open
- [x] FT-401 FFmpeg-backed PNG/JPEG photo import, PPM preview and bounded canvas fit/100%/zoom/pan state
- [x] FT-402 non-destructive PPM crop, 90° rotation, nearest-neighbor resize and undo-last operation stack
- [x] FT-403 immutable photo adjustment layers with ordering, inspector state and undo/redo history
- [x] FT-404 RGBA-preserving PNG and JPEG photo export with source metadata mapping, quality controls and atomic verification
- [x] FT-405 photo project autosave recovery, missing-media relink and corrupt-input UI status paths

## Next tasks
1. Exercise timeline playback/save/open/relink through the Avalonia UI on Windows, macOS and Linux, including moved-project recovery. The 2026-10-18 CUA inventory still exposes browsers but no native Windows app surface, so this manual gate remains unverified rather than claimed complete.
2. Obtain approved live Gemini provider evidence only if the optional remote transcription path is intended for release; the offline local Whisper path is now accepted with a real Windows fixture.
3. Finish manual/cross-platform FT-901, FT-902 and FT-1002 acceptance; resolve the external FT-1003/FT-1005/FT-1006 release gates. FT-1001 and FT-1004 hosted quality evidence is green.
4. Resolve approved logo, licensing/native redistribution, installer/signing, and live Gemini prerequisites before closing their GitHub tasks.
5. Keep MLT deferred unless interactive playback parity or a release licensing decision requires a new native engine spike.
6. Keep FT-1104 open for live-provider and reference-grade semantic acceptance; do not close it on offline or fake-HTTP evidence alone.
7. Continue FT-1105 only when the approved official asset source is available; do not invent branding assets.
8. Keep FT-1106 deferred until an approved segmentation/enhancement model, license terms and target hardware baseline are supplied; do not add a fake or unlicensed model.

**FT-602 local engine execution record (2026-10-18):** Commits `6e9be76`, `24e929b`, `bbc4c22`, `b05ccd5`, `785af80`, `f1cd3a2` and `0ad1132` add the solution smoke tool, ADR, dependency/development/release documentation, repository guard and malformed-timing coverage. `dotnet build FrameTheory.sln --configuration Release --no-restore` passed with 0 warnings/errors; the full Release suite passed 222 unit tests and 2 golden tests; the focused transcription suite passed 25 tests; `dotnet format --verify-no-changes`, repository validation and `git diff --check` passed. The real Windows smoke used a separately installed `openai-whisper` 20250625 CLI, official `tiny.en` model and synthesized speech, returning seven timed words with source fingerprint preservation. No engine, model, fixture or user key entered Git. Next: push this ten-commit batch, verify hosted build, then close FT-602 if the remote evidence remains green; live Gemini transcription remains optional.

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
Next unblocked task: Continue FT-602 live provider evidence and FT-901..FT-904 / FT-1002..FT-1006 release acceptance.
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
Windows: FT-001/FT-002/FT-003/FT-004/FT-005/FT-006/FT-101/FT-102/FT-103/FT-104/FT-105/FT-201/FT-202/FT-203/FT-204/FT-205/FT-301/FT-302/FT-303/FT-304/FT-305/FT-306/FT-401/FT-402/FT-403/FT-404/FT-405/FT-501/FT-502/FT-503/FT-504/FT-505/FT-506/FT-601/FT-603/FT-604/FT-605/FT-701/FT-702/FT-703/FT-704/FT-705/FT-801/FT-802/FT-803/FT-804 and FT-706 undo paths built/tested; FT-1001 core suite and FT-1004 hosted CI artifacts verified; FT-602 live provider fixture and FT-706 live provider scope remain partial; packaged UI remains unverified for production. macOS: hosted CI restore/build/test/publish, native FFmpeg/media smoke, deterministic timeline export, approved branded watermark assertion and corrected DMG detach cleanup verified by run 39104297945; interactive desktop acceptance remains unverified. Linux: hosted CI restore/build/test/publish, native FFmpeg/media smoke, deterministic timeline export and approved branded watermark assertion verified by run 39104297945; interactive desktop acceptance remains unverified. Gemini provider: headless adapter contract tested; no live key/media upload run. Logo: approved source is committed at `assets/brand/frame-theory-original.png` and hash-verified; release-owner rights/visual review remains open. MLT: not adopted. Release native media: not approved or bundled.

## Honest reporting
Codex should not mark a task complete for a mock UI, TODO, downloaded asset, simulated exporter, docs-only passing CI, or speculative integration. If execution is interrupted, preserve status and resume from first unblocked task on next run.
