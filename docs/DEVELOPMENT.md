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
```
The solution targets .NET 10. The desktop shell uses Avalonia 12.1.2 with code-only controls and the Fluent theme; media tasks beginning with FT-003/FT-006 still require FFmpeg/ffprobe.

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
