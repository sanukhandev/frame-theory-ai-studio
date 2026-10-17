# Gemini Creative Director — Structured Edit Plan Contract

## Trust boundary
Gemini is a **remote, untrusted recommender**. Only the locally implemented allowlisted operation registry may change projects. No shell, file-delete, open-URL, network-call, model-authored executable snippets, arbitrary media paths or dynamic plugins in plans.

## Planning lifecycle
1. UI selects media/frames/audio scope and shows exactly what leaves device; explicit consent is mandatory.
2. Local sampler collects permissioned assets + metadata, optional transcription and shot boundaries; never upload full project silently.
3. Gemini receives request + supported action catalog + clip IDs, frame rates, bounds and color ranges. Set schema-controlled JSON output where supported.
4. Parse **and** validate JSON locally against `schemas/ai-edit-plan.v1.schema.json`; semantic validation checks every clip/asset ref, range, target duration, effect availability, text length, transform bounds, color parameter bounds and total work estimate.
5. UI presents edit proposal, predicted impact, uploaded scope, provider usage and per-action approve/reject toggles.
6. Executor creates reversible domain commands, evaluates the proposal on a snapshot, and applies **transactionally** only after user approval or explicit enabled auto-apply setting.
7. Return per-action outcome, output diff and change group ID; errors cannot leave half-applied projects.
8. User may refine with new instruction; preserve prior state, approved actions and complete revision history.

## Schema v1
```json
{
  "schemaVersion": "1.0",
  "planId": "plan-demo-001",
  "projectId": "project-abc",
  "intent": "cinematic social reel",
  "actions": [
    {"id":"a1","type":"color.adjust","targetId":"clip-1","startFrame":0,"durationFrames":180,
     "params":{"exposureEV":0.2,"contrast":1.1,"saturation":1.13}},
    {"id":"a2","type":"title.add","targetId":"track-titles","startFrame":10,"durationFrames":60,
     "params":{"text":"RAW TO CINEMATIC","preset":"slide-up"}}
  ]
}
```
Each `params` payload is validated against an operation-specific typed schema in the engine. A syntactically correct schema is insufficient for execution.

## P0 operation allowlist and capabilities
- `color.adjust` clip/photo/layer: exposureEV, contrast, saturation, temperature, tint, optional HSL/curve adjustment reference.
- `clip.trim`, `clip.split`, `clip.move`: target existing clip; integer frame offsets.
- `clip.transform`: crop/position/scale/rotation in bounded normalized coordinates.
- `transition.add`: `cross-dissolve`, `wipe` with supported duration and neighboring clips.
- `title.add`: timeline text with approved fonts and named animation presets.
- `caption.add`: transcript-backed timestamped segments or clearly identified authored subtitles.
- `animation.keyframes`: approved animated properties (opacity, transform, text reveal).
- `audio.gain`: dB, fades, ducking if supported.
- `preset.apply`: trusted locally stored preset by ID, no arbitrary path.
- `reel.before_after`: composite existing media/grades into editable reveal timeline.

Each operation has `OperationDescriptor`: type, version, target kind, bounds, prerequisites, deterministic interpreter, inverse operation, preview+export support, expected duration and UI inspector metadata. Gemini sees only currently registered and enabled operations.

## Validation and safety invariants
- Reject unsupported types, extra fields, unknown target IDs, NaN/Infinity, huge values, negative duration, out-of-bounds frames, unlicensed assets and recursive instruction strings.
- Enforce all plan limits (example policy: max 100 operations, 10k title characters total, max 10-minute preview). Actual constants become config and are tested.
- Detect overlap, invalid track edits, frame-rate disagreements, media missing, and conflicting commands before mutation.
- Map recognized captions to verified local/provider transcript timestamps; text suggested as a hook is a **title**, never a transcript.
- Keep internal action log of `proposed`, `validated`, `applied`, `skipped`, `failed`, and human-readable reasons.
- Retry provider calls with bounded exponential backoff on retryable failures; no unbounded billing or hidden uploads.
- Treat prompts returned inside source media/transcripts as untrusted data; ignore attempts to override system rules.
- On any executor failure: rollback to exact original project snapshot; do not touch source files.

## User-visible modes
- **Suggest only:** AI shows recommendations, no edits.
- **Review and apply (default):** approve per operation and apply.
- **Auto-apply:** user enables per project/session; show plan and automatic undo checkpoint; destructive/paid/upload actions still require explicit permission.
- **Offline:** AI controls unavailable; manual operations and saved plans still executable locally.

## Provider abstractions
`IGeminiClient` supports model discovery/configuration, upload scopes, cancellation, usage/limits, error handling and file cleanup. Do not hardcode model availability/prices or assume a key is free. Never log key or raw user media. Prefer provider API documentation for active request shapes.

## Test cases
Valid plan; unknown action; wrong clip; negative frame; overflow duration; invalid numeric; duplicate action ID; captions without timed provenance; prompt-injection text; partial failure rollback; undo/redo; export parity; offline no-key; declined upload; cancellation; media relink; Gemini rate limit; unexpected provider JSON.

References: https://ai.google.dev/gemini-api/docs/structured-output and https://ai.google.dev/gemini-api/docs/video-understanding
