# Continuous Integration, Builds and Releases

## Continuous checks
- Markdown/doc-reference consistency and JSON schema syntax on all pushes.
- After `*.sln`/`*.slnx` lands: `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release` across Windows, macOS and Linux; add platform-specific native tests.
- Generate media fixtures and run deterministic golden export tests on CI where licenses permit; keep optional GPU/hardware tests separately marked.
- Dependency and secret scan; license/SBOM review before release.
- Report actual test coverage, failures and skipped checks; green docs CI is **not** a completed application build.

## Packaging targets
- Windows: portable `.zip` plus `.exe` installer or MSI when build tooling and signing policy confirmed.
- macOS: architecture-specific `.app` bundle and `.dmg`; assess Apple signing/notarization.
- Linux: AppImage and/or distro packages with portable native dependency policies.
- Never assume a single .NET binary contains FFmpeg, MLT or runtime native plugins.

## Version policy
`0.1.x` technical previews, `0.5.x` feature-complete betas, `1.0.0` only after V1 gate. Use release notes with tested OS/architecture, codecs, known limitations, SBOM, checksums and regressions.

Validation packaging defaults to `0.1.0` and accepts an explicit version override: Windows uses `scripts/package_windows.ps1 -Version`, macOS uses the third `scripts/package_macos_dmg.sh` argument, and Linux uses the third `scripts/package_linux_deb.sh` argument. Each script rejects versions outside the documented numeric preview format before writing package metadata.

## Release checklist
- All UC-01..UC-06 smoke paths pass on packaged binaries.
- Clean host install/uninstall, project save/reopen, third-party library presence/rights.
- Offline no-key photo/video edit success; Gemini key secure-store and upload consent tested.
- Input source preserved across cancellation, crash and failures.
- Branded asset verified from project source and properly packaged, or mark build intentionally unbranded.
- No high-severity security/data-loss issues; no unstated unavailable features.
