# Release Roadmap and Quality Gates

Scope is product milestones, not a fabricated delivery deadline. Measure progress by real acceptance evidence, not number of files or UI screenshots.

## R0 — Foundation proof (E00-E01)

**Gate:** source builds/tests with native desktop shell; project schema round-trip; deterministic frame/time types; crash-safe atomic writes; FFmpeg/MLT feasibility spike and license ADR; green CI on at least supported reference OS.

## R1 — Color/photo usable vertical slice (E02-E04)

**Gate:** import image, inspect profile, apply curves/HSL/LUT, save, close/reopen, preview before/after, export photo with golden difference tolerances and original checksum unchanged.

## R2 — Real video editor vertical slice (E02/E05/E06)

**Gate:** import two clips with audio, seek to frame, split/trim/move, compose transition, animate title, edit captions, save/reopen, export H.264/AAC with verified A/V sync, duration and color. No mocked preview.

## R3 — AI executes real edits (E07)

**Gate:** Gemini with authorized user key, prompt against consented assets, schema-valid operation plan, approval/diff, apply and undo group, follow-up revise, no-key offline, injection and partial-failure rollback tested. Show that exported pixels/audio materially change according to plan.

## R4 — Creator-ready V1 (E08-E10)

**Gate:** before/after Reel, social presets, robust export and cancel, helpful errors, autosave recovery, UI keyboard path, reference performance, security review, all UC-01..UC-06, Windows/macOS/Linux installer or published binaries tested with platform-specific limitations documented, licensing review complete.

## R5 — Competitive V2 (E11)

Masking, tracking, proxy, scopes, photo layers, kinetic captions, reference-grade matching, local AI modules. Each capability has own acceptance and availability flags; no P1 feature may destabilize V1.

## R6 — Advanced V3 (E12)

HDR/RAW, multicam, advanced compositing, model plugins and optional collaborative services after explicit architectural ADR and cost/security review.

## Milestone reporting

For every milestone log:

- feature checklist pass/fail with links;
- exact Git SHA and build artifacts;
- test commands plus observed exit codes, OS/architecture;
- export fixture report incl. quality and A/V metrics;
- scope deferred or blocked with reason;
- residual risks, license audit and release decision.

**Release policy:** No “market-ready” or “complete” claim without V1 R4 gate. Agent execution cannot continue across session/time limits by promise; preserve resume context in docs/IMPLEMENTATION_STATUS.md.
