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

Release owners may provide a separately reviewed native FFmpeg directory as the final argument. It must contain both `ffmpeg` and `ffprobe` for Unix packages, or `ffmpeg.exe` and `ffprobe.exe` for Windows; the scripts copy the pair and adjacent support files beside the application and fail closed when either binary is missing:

```text
pwsh ./scripts/package.ps1 -RuntimeIdentifier win-x64 -OutputDirectory artifacts/package -SelfContained -FfmpegDirectory C:\reviewed\ffmpeg
pwsh ./scripts/package_windows.ps1 -FfmpegDirectory C:\reviewed\ffmpeg
sh ./scripts/package.sh linux-x64 artifacts/package true /reviewed/ffmpeg
sh ./scripts/package_macos_dmg.sh osx-arm64 artifacts/installer 0.1.1 /reviewed/ffmpeg
sh ./scripts/package_linux_deb.sh linux-x64 artifacts/installer 0.1.1 /reviewed/ffmpeg
```

The application resolves bundled binaries from its install directory before falling back to `PATH`. Supplying a directory is not license approval: release owners must record the exact build, checksums, configure flags, notices, codec obligations and redistribution permission in the release evidence before distributing it.

The output is a validation publish, not an installer. Windows installer packaging is currently x64-only (`win-x64`); it must be tested on the matching host before distribution.

CI writes `SHA256SUMS.txt` for the complete validation artifact set and verifies every listed file before upload with `pwsh ./scripts/verify_package.ps1`. The verifier rejects malformed, duplicate, escaping, missing, modified or unlisted nested files.

CI also creates `frame-theory-<platform>-portable.zip` from the published desktop tree with `pwsh ./scripts/archive_package.ps1`. This is a portable validation archive, not a native FFmpeg bundle. The same matrix now creates unsigned Windows Inno Setup, macOS DMG and Linux DEB validation installers.

## Current matrix

| Platform | Build/CI compile | Local GUI/media evidence | Installer/signing |
|---|---|---|---|
| Windows 11 x64 | Local and hosted CI publish verified; extracted portable archive and unsigned Inno installer install/relaunch/uninstall passed in `39099847174` | Hosted bundled FFmpeg/ffprobe import/audio probing, project persistence, batch social MP4, AI recipe and native smoke verified; hosted UI automation verifies 1280x800 workspace/control reachability; manual interactive and clean-host relaunch remain open | Unsigned validation installer available; no signing, notarization or approved production native-media redistribution |
| macOS Apple Silicon/Intel | Hosted restore/build/test/publish, native media, extracted portable process-liveness and mounted unsigned DMG relaunch plus support-file/version validation passed in `39099847174` | Hosted bundled FFmpeg/ffprobe import, project persistence, batch social MP4, AI recipe and native smoke verified; local GUI/manual acceptance not run here | Unsigned validation DMG available; no notarization or approved production native-media redistribution |
| Linux x64 | Hosted restore/build/test/publish, native media, extracted portable process-liveness, and installed/removed unsigned DEB relaunch plus support-file validation passed in `39099847174` | Hosted bundled FFmpeg/ffprobe import, project persistence, batch social MP4, AI recipe and native smoke verified under Xvfb; local GUI/manual acceptance not run here | Unsigned validation DEB available; no signing or approved production native-media redistribution |

The subsequent native-bundle run `39099250089` reached successful macOS/Linux package validation and proved the Windows bundled importer could read video and audio, but failed on a multiline PowerShell assertion. Run `39099847174` passed the corrected assertion and support-file package checks on all three OS jobs; legal, signing, clean-host and production redistribution gates remain open.

## Release blockers

- Approved Frame Theory logo bytes are absent from the repository; no replacement asset is allowed.
- An opt-in reviewed native bundle mechanism is validated, but the exact production FFmpeg/ffprobe build, license mode, codec notices and redistribution path are not approved.
- The application license is undecided.
- Clean-host install/relaunch/recover, package checksums on each target host, native media redistribution, and code-signing/notarization evidence are still required before V1.
