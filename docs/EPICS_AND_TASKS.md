# Delivery Epics and Executable Tasks

This file is the **canonical ordered backlog**. P0 is required for a usable V1; P1/P2 are subsequent releases. Task IDs are stable. Implement only after completing dependencies and proven vertical slices. Create/update GitHub Issues from these items; cross-reference progress in docs/IMPLEMENTATION_STATUS.md.

## Dependency order

`E00 -> E01 -> E02 -> E03 -> E04/E05 -> E06 -> E07 -> E08 -> E09 -> E10`. `E11` and `E12` follow V1. Parallelize behind validated domain contracts.

## E00 — Repository & cross-platform foundation [P0]

- **FT-001** Scaffold solution and projects: Desktop, Application, Domain, Media, Photo, Color, AI, Captions, Storage, Platform, Tests. DoD: `dotnet restore/build/test` succeeds.
- **FT-002** Avalonia desktop shell with navigation Video/Photo/Color/AI and MVVM/DI. DoD: app boots on reference OS, navigable by keyboard.
- **FT-003** Capability detection/diagnostics: OS, FFmpeg/ffprobe, storage, GPU, codecs. DoD: missing dependency visible; no startup crash.
- **FT-004** Configure analyzers, formatting, editorconfig, deterministic build, CI jobs for Windows/macOS/Linux. DoD: green matrix.
- **FT-005** Create synthetic fixture generators, media contract tests and golden comparison harness. DoD: reproducible artifacts without user media.
- **FT-006** Spike FFmpeg+MLT real playback/seek/multitrack/bindings; ADR documenting selection and licensing. DoD: measured proof on 2 OS minimum before committing to dependency.

## E01 — Project, domain state & commands [P0, depends E00]

- **FT-101** Rational timecode, frame mapping, stable media IDs, source metadata domain types. DoD: variable rates, negative/bounds tests.
- **FT-102** Versioned native project JSON, relative asset refs, project save/open/migrate and atomic writes. DoD: round-trip tests.
- **FT-103** Transactional commands, inverse operations, named command groups, undo/redo. DoD: full state equality after rollback.
- **FT-104** Autosave, crash recovery, missing-file relink and cache index. DoD: simulated crash preserves prior save.
- **FT-105** Storage fuzz, corruption handling, oversized input guards and explicit upgrade path. DoD: no source destruction.

## E02 — Media pipeline, playback & preview [P0, depends E00/E01]

- **FT-201** Probe actual video/audio/image metadata, orientation, frame rate and profiles; URI sandboxing. DoD: mixed media fixture suite.
- **FT-202** Decode and seek frames with cancellable preview worker and bounded cache. DoD: expected frame under tolerance.
- **FT-203** Video/audio multitrack graph with synchronized play/seek/scrub. DoD: 2 tracks and audio timing tested.
- **FT-204** Waveforms, thumbnails, import browser, media relink and drag/drop. DoD: rendered from real files.
- **FT-205** Export worker pipeline with progress/cancel/atomic file finalization and verified streams. DoD: decode+ffprobe receipt.

## E03 — Universal color engine [P0, depends E01/E02]

- **FT-301** Canonical color graph with explicit input/working/output profiles. DoD: synthetic sRGB/Rec.709 charts.
- **FT-302** Grading operators: EV, contrast, saturation, temperature/tint, curves and HSL. DoD: numerical image/video tests.
- **FT-303** `.cube` LUT parser/writer/interpolator with validation and roundtrip tests. DoD: identity and known transforms.
- **FT-304** Preset store and shared application to photo/video; reversible commands. DoD: save/use in both workspaces.
- **FT-305** Before/after viewer and grade inspector UI; export/preview parity. DoD: golden tests.
- **FT-306** Correct profile output for JPEG/PNG and video Rec.709. DoD: metadata + pixel tolerance tests.

## E04 — Photo workspace [P0, depends E01/E03]

- **FT-401** Photo canvas pan/zoom/fit/100% and file import. DoD: real PNG/JPEG renders.
- **FT-402** Crop, rotate, resize, transform; non-destructive adjustment layers. DoD: edit/save/reopen/export.
- **FT-403** Layer/adjustment order, inspector, histogram placeholder avoidance, undo. DoD: commands tested.
- **FT-404** Transparent PNG and JPEG output with proper conversion, metadata and quality options. DoD: golden exports.
- **FT-405** Photo project recovery/relink, missing asset and corrupt input UI tests. DoD: no source mutation.

## E05 — Video timeline & editing [P0, depends E01/E02/E03]

- **FT-501** Functional timeline UI: tracks, playhead, time ruler, snapping, zoom/scroll and selection. DoD: edit exact frame.
- **FT-502** Insert/move/split/trim/ripple delete and copy/paste. DoD: frame-level tests for edge cases.
- **FT-503** Compositing, clip transforms and basic cross-dissolve/wipe. DoD: multi-track preview/export.
- **FT-504** Opacity/position/scale keyframes with presets and easing. DoD: accurate intermediate frames.
- **FT-505** Audio tracks, gains, fades, mute, source sync and waveform. DoD: audible real output.
- **FT-506** Title overlays, safe areas, font selection with licensed fonts, z-order. DoD: exact render tests.

## E06 — Captions, animation and audio enhancements [P0, depends E02/E05]

- **FT-601** Timed caption model, text inspector and SRT import/export. DoD: timezone-independent timestamp tests.
- **FT-602** Transcription provider interface, real local engine or user-approved Gemini API, word timing provenance. DoD: known spoken fixture.
- **FT-603** Caption overlay rendering, line wrapping, title/caption distinct provenance. DoD: preview-export parity.
- **FT-604** Animated title presets, inspectable per-property keyframes; no fake UI toggles. DoD: real animation export.
- **FT-605** Audio fade/ducking and cancellation progress across large transcriptions. DoD: no UI hangs.

## E07 — Gemini AI Creative Director [P0, depends E01/E03/E04/E05/E06]

- **FT-701** Gemini configuration UI, model selection/discovery, secure OS-backed key storage and no-key offline behavior. DoD: platform tests.
- **FT-702** Opt-in media-scope selector, sampled image/audio/video analysis job and cost/privacy disclosure. DoD: uploads never run without consent.
- **FT-703** Schema-constrained provider response adapter, `schemas/ai-edit-plan.v1.schema.json`, semantic validator, operation catalog. DoD: malicious plans rejected.
- **FT-704** Proposal UI with diff and per-action review/approval, explanation and optional auto-apply. DoD: no mutation before approval.
- **FT-705** Transactional executor maps AI plan to real project commands including grade, trim, title, caption, keyframes, transitions, and before/after Reel. DoD: fixture plan changes real export.
- **FT-706** Follow-up refinement, undo AI group, provider retries/timeouts/rate-limits, usage and redaction logs. DoD: offline fallback and rollback tests.

## E08 — Reel wizard and creator exports [P0, depends E03/E05/E06/E07]

- **FT-801** Before/after split, wipe or timed reveal generator as editable project composition. DoD: render genuine reveal.
- **FT-802** Social export presets 9:16 / 1:1 / 16:9, MP4, PNG, JPG, resolution/frame rate/quality options. DoD: codec/metadata validation.
- **FT-803** User-supplied music + basic beat marker option, watermark using approved asset only, creative title/caption slots. DoD: output matches chosen options.
- **FT-804** Background export queue with progress/cancel/clean temp outputs. DoD: interrupt/retry smoke test.

## E09 — UX, polish & performance [P0, depends E04/E05/E07/E08]

- **FT-901** Compact professional adaptive desktop design, focused accessible keyboard controls, shortcuts and workspace state. DoD: tested 1280x800 and HiDPI.
- **FT-902** View synchronization, inspector context, selection, before/after, AI proposal statuses, undo affordances. DoD: manual acceptance.
- **FT-903** Cache limits, render cancellation, seek benchmarks, optional proxy design validated. DoD: memory/fps traces recorded.
- **FT-904** Errors, logs, crash reporting local-first; user-friendly dependencies and codec messaging. DoD: failure UX tests.

## E10 — Security, QA, licensing and releases [P0, depends all V1]

- **FT-1001** Core unit/integration/golden suite with schema adversarial tests, caption provenance, source checksum invariants.
- **FT-1002** End-to-end journeys UC-01..UC-06 in PRODUCT_REQUIREMENTS, manual smoke evidence on each OS.
- **FT-1003** Dependency SBOM, codec / GPL-LGPL / image library / fonts and native binary redistribution assessment.
- **FT-1004** CI gates for Windows/macOS/Linux, signed/notarized optional production release path, checksummed test artifacts.
- **FT-1005** Installer packaging and platform matrix; opt-in analytics only, privacy docs, release notes and recovery guide.
- **FT-1006** Acceptance audit: no P0 gaps, no fake functionality, no unsupported “complete” claims; tag V1 only on passing gates.

## E11 — Competitive enhancements [P1, only after V1]

- **FT-1101** Multilayer photo composition/blending/brush masking and retouch tools.
- **FT-1102** Mask-aware grading, scopes, video stabilization and motion tracking.
- **FT-1103** Audio beat detection, word-by-word kinetic captions, speed ramp curves, proxy pipeline.
- **FT-1104** AI reference matching and style library, smarter multi-clip rough cut.
- **FT-1105** Brand kits (official assets only), batch jobs, multiple export targets, AI recipes.
- **FT-1106** Optional local background segmentation and enhancement models with license checks.

## E12 — Advanced / research [P2, only after P1]

- **FT-1201** HDR/LOG/RAW, color-managed high bit depth, camera profiles.
- **FT-1202** Advanced composites, multicam, richer keyframe/motion graph.
- **FT-1203** Opt-in model plugins, inpainting, local AI generation subject to licensing/GPU.
- **FT-1204** Optional cloud collaboration and publishing connectors with user-approved data sharing.
- **FT-1205** Plugin SDK with sandbox and signed package support.

## Issue and task execution order

A Codex agent should first implement E00 foundation and E01 persistence, then deliver a **vertical slice**: photo import -> grade -> save/reopen -> export, followed by video import -> edit -> export, then Gemini plan -> approval -> execute -> export. Finish V1 before P1.

## Common acceptance checklist for EACH task

- A reproducible real behavior is reachable.
- Tests cover success, failure, cancellation and state persistence as relevant.
- Logs/metrics captured for engine or performance changes.
- Docs and progress tracking updated.
- Compatible with source license/brand policy.
- Build/test runs recorded with their exit codes.
- Code committed; task closed only with evidence and reference to commit/CI run.
