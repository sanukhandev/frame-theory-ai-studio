# Dependency Inventory and Redistribution Assessment

Generated from `dotnet list FrameTheory.sln package --include-transitive` on 2026-10-17 with .NET SDK 10.0.100. This is an inventory checkpoint, not a completed legal clearance.

The reproducible CI/local vulnerability check is `pwsh ./scripts/check_nuget_vulnerabilities.ps1`. The NuGet advisory feed returned no vulnerable package records on 2026-10-17; the generated JSON report is retained as a CI artifact. A clean advisory result does not replace license, codec-patent or native redistribution review.

Each CI runner also generates `artifacts/sbom/nuget-packages.json` with the complete restored direct/transitive package graph, plus `ffmpeg-version.txt`, `ffmpeg-buildconf.txt` and `ffprobe-version.txt` from the installed host tools. These artifacts identify the exact validation environment; they do not authorize redistribution of those binaries or codecs.

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

The Windows CI fallback pins the WinGet package `Gyan.FFmpeg` version `9.0.2` from the `winget` source. Its portable installer URL is `https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-full_build.zip` with installer SHA-256 `759d0a9831c436a0eb331ad36f236c06cb04aaa0005da46571f0e9d3d9206f6b`. This is GPL validation evidence only; it is not an approval to redistribute FFmpeg, codecs or the package in a product installer.

## Release blockers

- No installer currently bundles native media binaries or fonts.
- The repository license is undecided; do not label binaries as open source.
- Transitive package notices, CVE scan, FFmpeg configure/license evidence and platform-native checksums remain required for FT-1003/FT-1005.
- Gemini API use remains an optional external service with provider terms and user-account charges.
