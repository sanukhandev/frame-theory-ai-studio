# Architecture and Module Contracts

## Target topology

```text
FrameTheory.Desktop (Avalonia/MVVM)
  ├─ FrameTheory.Application (use cases, DI, orchestration)
  ├─ FrameTheory.Domain (project, timeline, clip, layer, edit commands, undo)
  ├─ FrameTheory.Media (probe, decode, render adapters, export workers)
  ├─ FrameTheory.Photo (image operators, layer compositor)
  ├─ FrameTheory.Color (universal grade graph, LUT parser/generator, profiles)
  ├─ FrameTheory.AI (Gemini adapter, sampler, plan schema, validator/executor)
  ├─ FrameTheory.Captions (timed text, transcript providers, SRT)
  ├─ FrameTheory.Storage (file bundle, autosave, migration, relink)
  └─ FrameTheory.Platform (keychain, dialogs, OS/native dependencies)
tests/ ... unit / integration / golden / UI smoke
tools/ ... fixture creators and QA scripts
```

## Architectural constraints

- `Domain` is pure and headless: no Avalonia, FFmpeg process creation, HTTP, filesystem or native libraries.
- `Application` coordinates injected interfaces; UI never calls FFmpeg/HTTP directly.
- `Media` owns frame/time conventions, process invocation, audio routing, cancellation, probe metadata and output receipts.
- `Photo` and `Media` consume common `Color` parameter graph, but each adapter handles correct pixel format and color space.
- `AI` produces domain commands and validation results only. Gemini does not write arbitrary disk files or execute code.
- `Storage` writes versioned project atomically; keep references and content checksums; assets may live externally, relink via UI.
- `Platform` abstracts secure secrets, FFmpeg executable discovery, available GPU backend and OS-specific packaging.

## Ownership and interfaces (illustrative C#)

```csharp
public interface IMediaProbe { Task<MediaInfo> ProbeAsync(Uri file, CancellationToken ct); }
public interface IPreviewRenderer { Task<FrameBuffer> RenderFrameAsync(ProjectSnapshot project, FrameIndex frame, CancellationToken ct); }
public interface IExportService { Task<ExportReceipt> ExportAsync(ProjectSnapshot project, ExportPreset preset, IProgress<ExportProgress> progress, CancellationToken ct); }
public interface IProjectRepository { Task<ProjectSnapshot> LoadAsync(string path, CancellationToken ct); Task SaveAtomicAsync(string path, ProjectSnapshot snapshot, CancellationToken ct); }
public interface IEditCommand { CommandResult Apply(ProjectSnapshot source); IEditCommand Inverse(ProjectSnapshot before); }
public interface IAiPlanner { Task<AiPlanResult> PlanAsync(AiRequest request, CancellationToken ct); }
public interface IAiPlanExecutor { PlanValidation Validate(AiEditPlan plan, ProjectSnapshot project); CommandResult ApplyTransactional(AiEditPlan plan, ProjectSnapshot project); }
public interface ISecretStore { Task StoreAsync(string name, string secret); Task<string?> ReadAsync(string name); Task DeleteAsync(string name); }
```

Types are contract sketches; update documentation when finalized, not secretly diverge.

## Primary data model

`ProjectSnapshot`: schemaVersion, id, frameRate rational, canvas dimensions, color working space, media refs (stable id, fingerprint, relative/absolute relink hint), ordered tracks, clips (in/out integer frames, start frame, layer/z-order), effects, grade graph, titles/captions, keyframes, audio mix, undo checkpoint pointer.

Photos share the project container but use `PhotoDocument`: canvas, asset refs, layer tree, adjustments, masks, transform, export profile.

## Rendering strategy

Select/verify a real playback integration early. FFmpeg is not itself a ready interactive NLE timeline; investigate MLT for graph orchestration and native hosting. Build a spike measuring frame seek, track mix, 1080p frame latency, color LUT, crossfade, crash handling and ARM64 packaging. If MLT bindings fail, implement a tested adapter with FFmpeg preview/proxy + a deterministic compositor; do not promise full parity without proof.

**One canonical command/effect representation** drives preview and export. Avoid separate divergent effect pipelines; where different engines are required, maintain conformance tests.

## Failure containment

- Render/export in cancellable workers; supervise native subprocess crashes and timeouts.
- Atomic output writes; never replace originals; checksum source media after destructive-looking operations.
- Bounded caches with disk quota and eviction; project references remain valid if cache is deleted.
- Feature detection drives UI; unsupported features disabled with explanation, not silently skipped.

## Dependency graph / critical path

Foundation -> project+time model -> probe/playback spike -> color graph -> photo+video edit command execution -> saving/export -> AI plan validation+transaction -> UX refinement -> packaging/test. Parallelize captions, docs, platform wrappers only after stable contracts.

## Architectural decision process

Write an ADR under docs/adr/ before replacing major libraries, changing schema, adding a cloud dependency or introducing license-restricted artifacts. Capture alternatives, benchmarks and rollback strategy.
