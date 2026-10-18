# Frame Theory Studio AI

**Imagine it. Direct it. Edit it.**

An open-development, offline-first **desktop creative studio** for **video editing, image editing, and universal cinematic color grading**. A user-supplied **Google Gemini API key** enables an AI Creative Director that proposes *structured, reversible edits* and, when authorized, applies them to the real timeline and image layers.

> **Status: working implementation, not a V1 release.** The offline editor, non-destructive project persistence, FFmpeg media path, color/photo workflows, AI approval executor, social batch export and validated AI recipes have hosted Windows/macOS/Linux evidence. Manual UI acceptance, live Gemini validation, approved branding, licensing/native redistribution, installers/signing and final V1 gates remain open. See [implementation status](docs/IMPLEMENTATION_STATUS.md) and [the acceptance audit](docs/V1_ACCEPTANCE_AUDIT.md); do not present planned or unverified features as shipped.

## Product differentiation

- **One application, three workspaces:** Video, Photo and Color; shared assets, project system, and grading presets.
- **AI that actually edits:** Gemini analyses consented media, returns a versioned edit plan, and a deterministic local executor applies supported cuts, effects, keyframes, captions, and color operations.
- **Editable results:** every AI-generated change remains a timeline operation, effect, adjustment, or layer; reversible with undo/redo.
- **Offline-first:** import, manual editing, preview, save, and export work without a network. Gemini requires explicit network access and may incur provider charges.
- **Cross-platform target:** Windows, macOS (Intel/Apple Silicon), and Linux. Native dependencies, acceleration, installers, and QA require per-OS verification.
- **Creator-first:** LUT import/export, reference-grade comparisons, before/after reveal Reels, and social export formats.

## Planned technology

| Layer | Proposed implementation |
|---|---|
| Desktop shell | C# / .NET + Avalonia UI; MVVM |
| Project and command model | Shared .NET libraries; versioned JSON, SQLite indexes/cache when useful |
| Multitrack media | FFmpeg/ffprobe; evaluate MLT through a versioned adapter before adoption |
| Photo rendering | SkiaSharp or ImageSharp after licensing/performance review |
| Common grading | Color-managed normalized parameter graph; image/video adapters; .cube LUTs |
| AI planning | Gemini via user-owned key; schema-constrained JSON; explicit upload consent |
| Optional local AI | Whisper-compatible captions; separately installable inference modules |
| Verification | Unit, integration, export golden tests, UI smoke tests, cross-platform CI |

These are design choices to validate in working spikes, not promises about completed integrations. Select supported dependency versions at implementation time and document platform licensing.

## Workspaces

**Video** — media bin, multi-track timeline, trim/split, ripple editing, text, audio, keyframes, transitions, captions, AI preview, export.

**Photo** — layered composition, crop, resize, transform, masks, adjustment stack, brush/retouch roadmap, image export.

**Color** — shared exposure/contrast/white-balance, HSL, curves, LUTs, scopes roadmap, split before/after, reusable presets across photo and video.

**AI** — Gemini setup, local sampling and upload preview, edit-plan proposal and diff, user approval, deterministic execution, subsequent natural-language refinement.

## Start developing

1. Install a supported .NET SDK, Git, FFmpeg/ffprobe, and an IDE with C# support.
2. Read [setup](docs/DEVELOPMENT.md), [architecture](docs/ARCHITECTURE.md), [AI contract](docs/AI_EDIT_PLAN.md), and [agent instructions](AGENTS.md).
3. Implement in the dependency order defined by [epics and tasks](docs/EPICS_AND_TASKS.md).
4. Build and test with commands documented in the repository **after the solution exists**. Never report a test as passed unless it ran.
5. Set your Gemini key via the application's OS-secure settings screen; never commit keys, tokens, or private user media.

## Documentation

| Document | Purpose |
|---|---|
| [AGENTS.md](AGENTS.md) | Non-negotiable Codex execution rules |
| [Product spec](docs/PRODUCT_REQUIREMENTS.md) | Scope, user journeys, P0/P1/P2 and acceptance |
| [Architecture](docs/ARCHITECTURE.md) | Modules, dependencies, boundaries, data flow |
| [AI edit contract](docs/AI_EDIT_PLAN.md) | Schema, approvals, executor, undo and safety |
| [Color pipeline](docs/COLOR_PIPELINE.md) | Color correctness and LUT interoperability |
| [Timeline and media](docs/MEDIA_ENGINE.md) | Deterministic timeline, preview and export |
| [UX](docs/UX_DESIGN.md) | Desktop workspaces and interaction conventions |
| [Security](docs/SECURITY_PRIVACY.md) | API keys, consent, isolation, threat model |
| [Epics and tasks](docs/EPICS_AND_TASKS.md) | Dependency-aware backlog and definitions of done |
| [Roadmap](docs/ROADMAP.md) | Incremental releases and acceptance gates |
| [Test strategy](docs/TEST_STRATEGY.md) | Quality gates and reproducible fixtures |
| [Codex prompt](docs/CODEX_MASTER_PROMPT.md) | Copy-ready implementation directive |
| [Licenses](docs/DEPENDENCIES_AND_LICENSES.md) | Dependency reviews and distribution restrictions |
| [Brand](docs/BRAND_ASSETS.md) | Source-only logo policy and official Instagram handle |

## Brand

**Frame Theory** · Instagram **[@frametheoryvisuals.ai](https://www.instagram.com/frametheoryvisuals.ai/)**. Use only the approved source logo, unchanged. If the approved logo is not present in the repository, do not invent one; see [brand rules](docs/BRAND_ASSETS.md).

## License

**Not yet selected.** Do not assume this repository or bundled assets are open-licensed. Choose and add an appropriate project license only with owner approval; audit all redistributable native dependencies, codecs, models and assets first.
