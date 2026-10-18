# Release and Platform Matrix

## Reproducible validation publish

The repository provides framework-dependent desktop publishes, optional self-contained desktop publishes, and a SHA-256 manifest:

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

CI also creates `frame-theory-<platform>-portable.zip` from the published desktop tree with `pwsh ./scripts/archive_package.ps1`. This is a portable validation archive, not an installer and not a native FFmpeg bundle.

## Current matrix

| Platform | Build/CI compile | Local GUI/media evidence | Installer/signing |
|---|---|---|---|
| Windows 11 x64 | Local and hosted CI publish verified; extracted portable archive relaunch passed in `39083944147` | Hosted FFmpeg import/export, project persistence, batch social MP4, AI recipe and native smoke verified; interactive workspace and clean-host relaunch remain open | Portable validation package available; no installer, signing or notarization |
| macOS Apple Silicon/Intel | Hosted restore/build/test/publish, native media and extracted portable process-liveness smoke verified in `39084466811` | Hosted FFmpeg import/export, project persistence, batch social MP4, AI recipe and native smoke verified; local GUI/manual acceptance not run here | Portable validation package path available; no installer or notarization |
| Linux x64 | Hosted restore/build/test/publish, native media and extracted portable process-liveness smoke verified in `39084466811` | Hosted FFmpeg import/export, project persistence, batch social MP4, AI recipe and native smoke verified under Xvfb; local GUI/manual acceptance not run here | Portable validation package path available; no installer or signing |

## Release blockers

- Approved Frame Theory logo bytes are absent from the repository; no replacement asset is allowed.
- FFmpeg/ffprobe are host dependencies and are not bundled; exact build, license mode, codec notices and redistribution path are not approved.
- The application license is undecided.
- Install/relaunch/recover, package checksums on each host, and code-signing/notarization evidence are still required before V1.
