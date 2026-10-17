# Codex Agent Operating Contract — Frame Theory Studio AI

## Mission
Build a **real, tested, cross-platform desktop application** for non-destructive **video, photo and color editing**, with optional Gemini-authored edits that the local editor can execute. This document is executable project policy for Codex and other coding agents.

## Read first, in order
1. README.md and docs/PRODUCT_REQUIREMENTS.md (product contract)
2. docs/ARCHITECTURE.md and docs/DECISIONS.md (technical boundaries)
3. docs/AI_EDIT_PLAN.md, docs/COLOR_PIPELINE.md, docs/MEDIA_ENGINE.md (engine contracts)
4. docs/EPICS_AND_TASKS.md, docs/ROADMAP.md, docs/TEST_STRATEGY.md (ordered work + gates)
5. docs/SECURITY_PRIVACY.md, docs/BRAND_ASSETS.md, docs/DEPENDENCIES_AND_LICENSES.md

## Work loop (repeat within the active execution session)
1. Inspect repository and git status; identify the first incomplete, unblocked task in the ordered backlog. Preserve all existing user code.
2. Define the **smallest end-to-end executable slice** and its tests. Write or update a failing test where practical.
3. Implement real features, not empty interfaces, hard-coded screenshots, fake AI output, fabricated exports or TODO-only shells.
4. Run restore/build/test/lint/integration + an appropriate sample-media smoke test. For unrun tests, state exactly why.
5. Update task and acceptance state in docs/IMPLEMENTATION_STATUS.md (create it on first implementation); include commands, observed results, blockers, known limits, next task.
6. Commit meaningful, green increments with messages `feat(scope): ...`, `fix(scope): ...`, `test(scope): ...`, or `docs(scope): ...`. Keep migrations backward compatible.
7. Move to the next unblocked task **without waiting for routine approval**. If tool/time/context limits end the session, leave a reproducible resume checkpoint; do not claim autonomous infinite operation.

## Non-negotiable product rules
- One installable application, three workspaces; project files and adjustments must be portable and non-destructive.
- AI is an *optional planning layer*. Manual editor works entirely offline; no Gemini key is needed for import/edit/preview/export.
- Gemini is **never authorized to execute arbitrary shell commands, URLs, scripts, file paths or network requests**; it emits allowlisted, schema-validated edit operations only.
- AI preview, approval, diff, transactional apply, undo/redo, and partial-failure reporting are required. Preserve original media.
- Caption words require source-grounded transcripts and timings; generated copy must not be misrepresented as recognized speech.
- Shared photo/video grading must be color-managed, deterministic and verified with preview-export parity fixtures.
- API keys reside only in OS credential storage or equivalent secure mechanism; never log, commit, or transmit keys elsewhere.
- Before uploading frames, audio or video to Gemini, show exact upload scope and obtain opt-in consent; support cancel and cleanup.
- Use **Frame Theory** and official `@frametheoryvisuals.ai` only. Official logo comes **only from approved project source file**, unchanged. Do not invent an icon/monogram if asset is absent.
- Never include commercial CapCut/Adobe effects, premium fonts, stock media, or models without license approval.
- Native modules and codecs must have documented license, redistribution path and per-platform verification.

## Engineering principles
- Strict module boundaries; interfaces between UI, domain model, processing engine, AI adapter, and platform integration.
- .NET/Avalonia MVVM and dependency injection; async/cancellable background work; no blocking media work on UI thread.
- Prefer precise time representation: rational FPS and integer frame or tick-based positions, not floating-point editing clocks.
- Domain operations are idempotent where possible and validated before mutation. Keep event/command log supporting undo.
- Property-based, unit, integration, deterministic golden-image/video and end-to-end tests; no mock-only release claim.
- Use synthetic/cleared fixture media; keep user footage, API keys and binaries out of Git.
- Use correct failure semantics. Display missing FFmpeg/codecs or failed Gemini requests clearly and keep manual editor usable.
- Gate advanced features behind validated capabilities; defer if they compromise stability rather than pretending completion.
- Apply secure dependency and supply-chain hygiene; pin hashes/versions on release, verify advisories.

## Definition of done — every task
- Product behavior is reachable through the UI (or contract is genuinely headless), functional, keyboard accessible where relevant.
- Unit + integration tests cover success, bounds, errors, cancellation and persistence.
- Reopen/save and preview/export stay consistent; no regression in sample smoke tests.
- Public docs, acceptance table, status checkpoint and relevant architecture decision are updated.
- Tested and committed. If not verified on Windows/macOS/Linux, mark platform status **unverified**.
- No critical security, privacy or data-loss defects remain.

## Branch and commit policy
- Current repository initialization is on `main` as requested. For implementation prefer focused branches/PRs unless repository owner explicitly authorizes direct commits; obey branch protection.
- Never rewrite main history or force push. Do not merge failing or unreviewed risky changes.
- Do not commit Gemini secrets, local media, generated caches, proprietary binaries or invented brand assets.

## Blocker policy
If a blocker needs user input (e.g. actual approved logo asset in repository, app signing, production API quota), implement independent work, log the precise blocker, and continue the next unblocked task. For complete claims, require demonstrable artifacts and passing gates.

## Completion criteria
Only declare **V1 complete** when docs/ROADMAP.md V1 gates, docs/TEST_STRATEGY.md matrices, platform packaging artifacts and all P0 acceptance criteria pass with evidence. Do not claim P1/P2 shipped. Never say “finished” based on documents or unexecuted code alone.
