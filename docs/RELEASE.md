# Release and Platform Matrix

## Reproducible validation publish

The repository provides a framework-dependent desktop publish for each runtime and a SHA-256 manifest:

```text
pwsh ./scripts/package.ps1 -RuntimeIdentifier win-x64 -OutputDirectory artifacts/package
sh ./scripts/package.sh osx-x64 artifacts/package
sh ./scripts/package.sh linux-x64 artifacts/package
```

Use `-SelfContained` with the PowerShell script, or pass `true` as the third argument to the shell script, when the target host must not install the .NET runtime:

```text
pwsh ./scripts/package.ps1 -RuntimeIdentifier win-x64 -OutputDirectory artifacts/package -SelfContained
sh ./scripts/package.sh linux-x64 artifacts/package true
```

The output is a validation publish, not an installer. It must be tested on the matching host before distribution.

CI writes `SHA256SUMS.txt` for the complete validation artifact set and verifies every listed file before upload with `pwsh ./scripts/verify_package.ps1`. The verifier rejects malformed, duplicate, escaping, missing or modified entries.

## Current matrix

| Platform | Build/CI compile | Local GUI/media evidence | Installer/signing |
|---|---|---|---|
| Windows 11 x64 | Local and hosted CI publish verified in runs `39050810748` and `39051936860` | Local FFmpeg import/export/smoke plus hosted native media smoke verified; interactive shell and package relaunch remain open | Not packaged, signed or notarized |
| macOS Apple Silicon/Intel | Hosted restore/build/test/publish and native media smoke verified in run `39051936860` | Hosted FFmpeg import/extraction/seek smoke verified; local GUI and package relaunch not run here | Not packaged or notarized |
| Linux x64 | Hosted restore/build/test/publish and native media smoke verified in run `39051936860` | Hosted FFmpeg import/extraction/seek smoke verified; local GUI and package relaunch not run here | Not packaged or signed |

## Release blockers

- Approved Frame Theory logo bytes are absent from the repository; no replacement asset is allowed.
- FFmpeg/ffprobe are host dependencies and are not bundled; exact build, license mode, codec notices and redistribution path are not approved.
- The application license is undecided.
- Install/relaunch/recover, package checksums on each host, and code-signing/notarization evidence are still required before V1.
