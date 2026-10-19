# Product Requirements Document — Frame Theory Studio AI

**Stage:** Proposed specification | **As of:** 2026-10-17 | **Audience:** development and QA

## Vision and value

One desktop creative environment to import, edit, color-grade and export **images and videos**, combining the precision of local non-destructive media editing with Gemini-assisted creative direction. Target beginner-to-intermediate social creators first; evolve into professional workflows with measured parity.

## Personas & jobs-to-be-done

1. Solo social creator: turn a landscape clip into an Instagram-ready 9:16 Reel with captivating title, captions, music, before/after reveal and watermark.
2. Photographer: import an image, crop/mask, grade it, compare, store look, export as JPG/PNG.
3. Colorist: grade several video clips and photos with one normalized preset and control color spaces.
4. Editor: trim multiple clips on frame boundaries, adjust audio, add keyframes/transitions, recover autosaves.
5. AI-assisted editor: ask Gemini to plan grade, cuts, captions and animations; inspect proposed changes; apply/refine or undo them.

## V1 P0 product acceptance (must work, not stubs)

- Desktop shell on Windows/macOS/Linux: Photo / Video / Color / AI workspaces, tabs, dockable or resizable panels, keyboard navigation, persistent layout and accessible messages.
- Video/image/audio import with metadata, safe paths, thumbnails/waveforms, missing-file relink UI, no modification of source.
- Frame-accurate timeline with tracks, seek/play/scrub, split/trim/ripple, clip transforms, basic transitions, text, basic animation keyframes and audio levels.
- Photo canvas with pan/zoom, crop/rotate/resize, adjustment stack and exported image; minimum single editable raster layer plus additional adjustment layers.
- Color settings exposure (EV), contrast, saturation, temperature/tint, HSL + curves, .cube import/export, before/after, film presets; sRGB for images, Rec.709 SDR for video as baseline.
- Save/reopen versioned native project, relative media-link portability, autosave/recovery, bounded undo/redo with deterministic serialization.
- Gemini settings: selectable supported model, secret storage, cost/usage visibility where provider returns usage, media-upload consent, analysis job cancellation.
- Gemini returns validated, versioned structured edit plan: color, trims, transforms, title/caption segments, keyframe animations, supported transitions; preview diff and approval; apply as one undoable transaction.
- Caption transcription baseline via local optional engine or approved Gemini transcription; editable text/timing and SRT export. No synthetic captions misrepresented as transcripts.
- Before/after Reel wizard: split/wipe reveal, title, timeline operations, music or audio option, flexible durations, 9:16 output.
- Exports: H.264/AAC MP4 and PNG/JPG; presets 9:16, 1:1, 16:9; progress, cancel, failure details; preserve audio sync and color.
- Complete implementation tests, Golden master preview/export parity, normal failure paths, GitHub Actions checks, installers/platform smoke evidence.

## P1 after V1

Proxy caching, speed curves, chroma key, stabilized playback, scopes, selective grading, reference shot matching, trackable AI masks, word-by-word captions, beat detection and sync, image layers/blend modes/masks/retouch, batch exports, saved AI recipes, brand kits.

## P2 / later

RAW/HDR end-to-end, multicam, advanced composites, plugin API, video inpainting, dubbing/lip-sync, AI generation, cloud collaboration/publishing integration.

## Key use cases & acceptance scenarios

### UC-01: Local photo edit

Import `test_chart.png` -> adjustment + crop -> save -> close/reopen -> edit adjustment -> export PNG. Export matches expected pixel/color tolerance, original checksum unchanged.

### UC-02: Local video edit

Import two 10s 30/1 fps clips with audio -> split at frame 90 -> add 12-frame dissolve and animated title -> export 1080x1920 H.264/AAC -> verify duration, transitions, text, A/V sync and frame count.

### UC-03: Universal look

Create a named preset on photo; apply to a video frame from the same known color chart; compare normalized pixel values after profile conversion; .cube import/export roundtrip under declared tolerances.

### UC-04: Gemini loop

User opts in to upload sampled frames -> Gemini returns versioned edit plan -> invalid operations refused with actionable errors -> user approves -> timeline updates -> save/reopen -> refine via Gemini -> undo restores exact previous state.

### UC-05: Offline and failures

No network/key -> UI launches, manual edit+export work; Gemini shows disconnected state. Network loss mid-job does not corrupt projects.

### UC-06: Before/after Reel

Video/photo input -> choose look and reveal style -> title/audio -> generate editable timeline -> export 9:16 Reel and verify no destructive source edits.

## Product boundaries

- Not a CapCut, Photoshop or Adobe source-code clone; do not replicate copyrighted UI/assets.
- No guaranteed “viral” performance, no unlimited free Gemini usage.
- Signed installers and stores may incur independent fees. Users own the media and have responsibility for rights.
- GPU acceleration may vary by OS/driver; preserve documented CPU fallback for supported file sizes.

## Experience targets (initial, to validate on reference machines)

- Avoid blocking UI over 100 ms on media operations; show responsive cancellation and progress.
- 1080p proxy preview should be interactive on a documented mid-range system; record measured FPS, dropped frames, CPU/GPU and memory.
- Crash recovery should preserve last confirmed autosave; exports to temp path and atomically replace on success.
- No critical data-loss, secret disclosure, unconsented upload or preview/export mismatch at release.

## Definition of product completeness

See docs/ROADMAP.md and docs/TEST_STRATEGY.md. P0 is not complete until real binaries are built and validated, not merely wired UIs or mocked services.
