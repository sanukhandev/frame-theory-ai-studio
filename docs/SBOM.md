# Dependency Inventory and Redistribution Assessment

Generated from `dotnet list FrameTheory.sln package --include-transitive` on 2026-10-17 with .NET SDK 10.0.100. This is an inventory checkpoint, not a completed legal clearance.

The reproducible CI/local vulnerability check is `pwsh ./scripts/check_nuget_vulnerabilities.ps1`. The NuGet advisory feed returned no vulnerable package records on 2026-10-17; the generated JSON report is retained as a CI artifact. A clean advisory result does not replace license, codec-patent or native redistribution review.

## Direct packages

| Package | Resolved version | Use | Release status |
|---|---:|---|---|
| Avalonia | 12.1.2 | Cross-platform desktop controls | MIT metadata recorded; attribution still required |
| Avalonia.Desktop | 12.1.2 | Desktop platform integration | Include platform-specific native assets only after package review |
| Avalonia.Themes.Fluent | 12.1.2 | UI theme | MIT metadata recorded; attribution still required |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.402 | Build analyzers | Development-only; not redistributed |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | Test-only; not redistributed |
| xunit | 2.9.3 | Unit/golden tests | Test-only; not redistributed |
| xunit.runner.visualstudio | 2.8.2 | Test runner adapter | Test-only; not redistributed |

## Important transitive/native inventory

The current restore resolves Avalonia native/UI dependencies including SkiaSharp 3.119.4, HarfBuzzSharp 8.3.1.3, Avalonia.Native/Win32/X11/FreeDesktop 12.1.2, Avalonia.Angle.Windows.Natives 2.1.27548.20260419, MicroCom.Runtime 0.11.6 and Tmds.DBus.Protocol 0.94.1. Test graphs additionally resolve Newtonsoft.Json 13.0.1 and Microsoft test-platform packages.

## External media runtime

FFmpeg/ffprobe are discovered from the host and are not committed or bundled by this repository. The Windows evidence host used FFmpeg 9.0.2 full build. A release must pin the exact binary build, configure flags, source offer/notice path, LGPL/GPL choice, codec patent review and per-OS redistribution method before packaging.

## Release blockers

- No installer currently bundles native media binaries or fonts.
- The repository license is undecided; do not label binaries as open source.
- Transitive package notices, CVE scan, FFmpeg configure/license evidence and platform-native checksums remain required for FT-1003/FT-1005.
- Gemini API use remains an optional external service with provider terms and user-account charges.
