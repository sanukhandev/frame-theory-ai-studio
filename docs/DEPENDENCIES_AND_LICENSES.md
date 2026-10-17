# Dependency, Codec, Model and Asset Compliance

**Repository/application license is undecided.** Never guess that this project is MIT or legally open source. Owner must select a license before public binary distribution.

## Planned components and review requirements
| Component | Purpose | Required investigation before shipping |
|---|---|---|
| .NET SDK/runtime | Cross-platform core | Current servicing release, redistributable runtime terms |
| Avalonia | Cross-platform desktop UI | Core vs paid add-ons; license and attribution |
| FFmpeg/ffprobe | Decode/filter/encode | Exact compile flags, LGPL/GPL build, codecs, patent concerns, distribution method |
| MLT (candidate) | Multitrack/filters | LGPL/GPL module split, C# interop, binary packaging |
| SkiaSharp / ImageSharp candidate | Photo and display | Version-specific license (ImageSharp may require commercial license), color handling |
| JSON Schema validator | Gemini plan contract | License and performance/size handling |
| SQLite | Cache/index | Managed wrapper and platform package licenses |
| whisper.cpp / models (optional) | Local captions | Code and model-specific rights, downloadable sizes |
| SAM/segmentation models (later) | Masks/tracking | Model weights and redistribution rules |
| Fonts/icons/presets | UX | Original designs or documented licenses |
| Original Frame Theory logo | Official brand | Owner approval/source integrity; never synthesize replacement |

## Required SBOM process
Lock application packages; track exact native codec builds; record upstream URLs and versions; create SBOM for each release; scan CVEs/license notices; verify third-party attribution and model weights independently.

## Implemented dependency baseline
FT-002 uses `Avalonia`, `Avalonia.Desktop` and `Avalonia.Themes.Fluent` version `12.1.2` from NuGet. The package metadata identifies Avalonia as MIT-licensed; transitive packages and native assets remain part of the release SBOM and must be rechecked before packaging. FT-004 uses `Microsoft.CodeAnalysis.NetAnalyzers` version `10.0.402`, also identified as MIT-licensed.

## Packaging choices
Separate per-OS native dependencies, with architecture-specific smoke checks. Some codec usage may carry patent/licensing obligations even if FFmpeg itself is open source. Signing/notarization/Apple accounts can cost money, although development stack itself may be free.

## Blockers
Do not ship branded UI without verified logo original; do not ship commercial assets or third-party media without rights; do not claim “fully free” for Gemini paid API usage, signing, proprietary codecs or third-party services.
