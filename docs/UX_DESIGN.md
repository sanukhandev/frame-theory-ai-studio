# Interface and Interaction Specification

## Visual direction

Professional, fast, editorial and minimal: neutral charcoal/black/white interface, deliberate spacing, legible compact typography, restrained accent color for selection and active states, subtle short-duration animations. Never imitate trademarked CapCut/Adobe layouts or bundle proprietary fonts/icons.

## Shell

Top chrome: project title, save state, undo/redo, workspace selector `Video | Photo | Color | AI`, export status, preferences. Left: project/media library and tools. Center: preview canvas and playback. Right: context-dependent inspector and AI panel. Bottom: video multitrack timeline, or photo layers/history; all panels resizable and persistable.

### Video workspace

- File/media explorer, preview with overlays/safe zones, timecode transport, frame-step, multi-track timeline with clear track headers.
- Context inspector for transform, timing, transitions, effect stack, keyframes, audio, typography.
- Keep selected clip, frame range, playhead and inspector context visible together; selection changes must update the track rows and context text from the same timeline model.
- Accessible drag/drop and keyboard alternatives; snap indicators and waveform placeholders must be backed by real data.

### Photo workspace

- Canvas pan/zoom fit/100%, layer list, adjustment stack, brush/mask toolbar, property inspector.
- Histogram deferred to P1; initial compare uses split/hold/bypass.
- Preserve editability of layers/adjustments on reopen.

### Color workspace

- Same UI and grade model for a selected photo or video clip: sliders, curve editor, HSL, LUT chooser, presets, before/after, scopes P1.
- Surface input and output profile; warn on unknown/camera log data and mixed spaces.

### AI workspace/panel

- Conversational input, analyze selection/project toggle, media-scope preview with opt-in consent.
- Planning view: action list with type, target, exact parameters, timing, rationale, individual accept toggles.
- Apply button only after validation; show progress and partial failures; one-click undo AI group.
- Show the approved-action count and the latest validation/apply/undo status beside the proposal actions.
- Follow-up chat targets current selection; clarify only genuinely ambiguous targets; preview and manual overrides remain possible.

## Accessibility and resilience

Keyboard navigation for every essential control; accessible labels/focus ring; high-DPI, macOS Retina and Linux scales; reduced-motion preference; resizable viewport; 1280x800 minimum useful layout; user-friendly error details and logs copy.

The desktop shell provides `Ctrl/⌘+1..4` workspace switching, `Left/Right` frame stepping in Video, and `Ctrl/⌘+Z` undo for the last approved AI transaction. Dense workspace content is scrollable rather than clipped, and timeline/playhead, track, zoom, export-ratio and grade-slider controls expose automation names.

## Important screens / flows

1. Welcome/project: new/open/recent, sample project, project settings.
2. Media import and relink: metadata errors, unsupported formats, permission handling.
3. Video timeline editing: source monitor/program monitor may initially be one combined preview; functional not ornamental.
4. Photo editing: layer adjustment, crop and save.
5. Color compare: shared preset apply and .cube export.
6. AI: key setup, upload scope, proposal, apply, results, revision.
7. Render queue: format, destination, progress, cancel, validation receipt.
8. Settings: hardware/backend, cache location/quota, secret store and diagnostics.

## Brand

Follow docs/BRAND_ASSETS.md. Do not construct a text-only “FT” substitute for the official source logo in shipping UI. Use generic unbranded placeholder in development until original logo is provided as an approved asset.
