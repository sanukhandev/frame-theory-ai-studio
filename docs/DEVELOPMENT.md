# Local Development and Contributor Setup

## Prerequisites
- Git and a supported .NET SDK (select current supported SDK/target in first implementation commit and pin via `global.json`).
- C# IDE: free VS Code + C# extension or other compatible IDE.
- FFmpeg/ffprobe available on PATH for development; bundle only after legal/native packaging review.
- Per-OS supported Avalonia/native dependencies, build tools and optional OS secure-store integration.

## Start
```bash
git clone https://github.com/sanukhandev/frame-theory-ai-studio.git
cd frame-theory-ai-studio
dotnet --info
ffmpeg -version
ffprobe -version
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet run --project src/FrameTheory.Desktop
dotnet run --project tools/FrameTheory.Fixtures -- tools/fixtures/generated
dotnet tools/FrameTheory.MediaProbeSmoke/bin/Release/net10.0/FrameTheory.MediaProbeSmoke.dll <media-path>
dotnet tools/FrameTheory.MediaImportSmoke/bin/Release/net10.0/FrameTheory.MediaImportSmoke.dll <media-path>
dotnet tools/FrameTheory.PhotoImportSmoke/bin/Release/net10.0/FrameTheory.PhotoImportSmoke.dll <image-path>
dotnet tools/FrameTheory.PhotoExportSmoke/bin/Release/net10.0/FrameTheory.PhotoExportSmoke.dll <image-path> <output-path> [png|jpeg]
```
For a video fixture, the media-import smoke also extracts its first audio track through the transcription preparer, probes the owned temporary WAV, and deletes it on completion.
The solution targets .NET 10. The desktop shell uses Avalonia 12.1.2 with code-only controls and the Fluent theme; media tasks beginning with FT-003/FT-006 still require FFmpeg/ffprobe.

## Offline verified transcript import
The Video workspace's **Import verified transcript JSON** action accepts a local audio/video source followed by a JSON sidecar with this shape:

```json
{
  "Source": "file:///absolute/path/to/source.wav",
  "SourceFingerprint": "sha256:<lowercase-source-sha256>",
  "Words": [
    { "Text": "hello", "StartMs": 100, "EndMs": 400 }
  ]
}
```

The source URI must match the selected file and the provider recomputes the source SHA-256 before importing editable verified caption cues. Linked source or sidecar files, mismatched fingerprints, invalid timing and cancellation leave the current timeline unchanged. This is an offline interchange path for a separately licensed local ASR tool; no ASR executable or model is bundled by this repository. Remote transcription also fingerprints the original selected media before and after processing, rejecting returned words if that source changes while derived audio is being sent.

The Video workspace also exposes **Transcribe with local Whisper**. It requires the user to select a local Whisper CLI executable, a local model file and the source media. The adapter invokes only that selected executable through fixed argument-list parameters (`--model`, `--output_format json`, `--word_timestamps True`, `--output_dir`, `--task transcribe` and the selected source), parses segment word timings, verifies the original source SHA-256 before and after processing, deletes temporary JSON output and never downloads or bundles a model.

The real local-engine acceptance smoke is reproducible with a separately installed Whisper-compatible CLI and model:

```powershell
dotnet run --project tools/FrameTheory.LocalWhisperSmoke/FrameTheory.LocalWhisperSmoke.csproj --configuration Release -- <whisper-executable> <model-path> <spoken-audio-path>
```

On 2026-10-18 Windows x64, this command ran `openai-whisper` 20250625 with the `tiny.en` model against a locally synthesized spoken fixture and produced seven source-timed words with a preserved `sha256:` fingerprint. The executable, model, FFmpeg helper and audio fixture remained outside the repository; this is validation evidence, not a bundled runtime or redistribution approval.

## Repository target layout
```text
src/
  FrameTheory.Desktop/
  FrameTheory.Application/
  FrameTheory.Domain/
  FrameTheory.Media/
  FrameTheory.Photo/
  FrameTheory.Color/
  FrameTheory.AI/
  FrameTheory.Captions/
  FrameTheory.Storage/
  FrameTheory.Platform/
tests/
  FrameTheory.UnitTests/
  FrameTheory.IntegrationTests/
  FrameTheory.GoldenTests/
  FrameTheory.UISmokeTests/
schemas/ai-edit-plan.v1.schema.json
tools/fixtures/
assets/brand/     # approved owner-supplied logo only
docs/adr/
packaging/windows|macos|linux/
.github/workflows/
```

## Secrets
Never put Gemini keys in environment sample files with real values, source code, git history or log output. Runtime app uses secure OS credential storage. For isolated CI, use a fake HTTP adapter to test schema without live API. Real integration testing requires explicit user key and costs acknowledgement.

## Sample tests to build
```bash
dotnet test --filter FullyQualifiedName~Domain
dotnet test --filter FullyQualifiedName~Color
dotnet test --filter FullyQualifiedName~AI
# tools/fixtures/generated/ is populated by the deterministic fixture CLI
# tools/fixtures/verify-export.* to probe A/V and compare frames
```

## Change control
Update docs/IMPLEMENTATION_STATUS.md after each work slice; use issue IDs FT-xxx in commit messages; add ADR for module/library changes. Do not assert cross-platform success without testing OS-specific native functionality.
