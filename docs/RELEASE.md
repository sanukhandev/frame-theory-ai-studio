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

The output is a validation publish, not an installer. Windows installer packaging is currently x64-only (`win-x64`); it must be tested on the matching host before distribution.

CI writes `SHA256SUMS.txt` for the complete validation artifact set and verifies every listed file before upload with `pwsh ./scripts/verify_package.ps1`. The verifier rejects malformed, duplicate, escaping, missing or modified entries.

CI also creates `frame-theory-<platform>-portable.zip` from the published desktop tree with `pwsh ./scripts/archive_package.ps1`. This is a portable validation archive, not a native FFmpeg bundle. The same matrix now creates unsigned Windows Inno Setup, macOS DMG and Linux DEB validation installers.

## Current matrix

| Platform | Build/CI compile | Local GUI/media evidence | Installer/signing |
|---|---|---|---|
| Windows 11 x64 | Local and hosted CI publish verified; extracted portable archive and unsigned Inno installer install/relaunch/uninstall passed in `39086502910` | Hosted FFmpeg import/export, project persistence, batch social MP4, AI recipe and native smoke verified; hosted UI automation now verifies 1280x800 workspace/control reachability in `39088633198`; manual interactive and clean-host relaunch remain open | Unsigned validation installer available; no signing, notarization or native FFmpeg bundle |
| macOS Apple Silicon/Intel | Hosted restore/build/test/publish, native media, extracted portable process-liveness and mounted unsigned DMG relaunch passed in `39086502910` | Hosted FFmpeg import/export, project persistence, batch social MP4, AI recipe and native smoke verified; local GUI/manual acceptance not run here | Unsigned validation DMG available; no notarization or native FFmpeg bundle |
| Linux x64 | Hosted restore/build/test/publish, native media, extracted portable process-liveness and extracted unsigned DEB relaunch passed in `39086502910` | Hosted FFmpeg import/export, project persistence, batch social MP4, AI recipe and native smoke verified under Xvfb; local GUI/manual acceptance not run here | Unsigned validation DEB available; no signing or native FFmpeg bundle |

## Release blockers

- Approved Frame Theory logo bytes are absent from the repository; no replacement asset is allowed.
- FFmpeg/ffprobe are host dependencies and are not bundled; exact build, license mode, codec notices and redistribution path are not approved.
- The application license is undecided.
- Clean-host install/relaunch/recover, package checksums on each target host, native media redistribution, and code-signing/notarization evidence are still required before V1.
