# Quality Engineering & Acceptance Strategy

## Test pyramid
1. **Unit:** pure Domain frame math, clip ranges, undo/redo, schema parse+semantic validation, color transformations, LUT parsing, subtitle timecodes, project migrations.
2. **Integration:** real ffprobe decode, media composite render, project save/reload, native host and subprocess failure/retry, Gemini mocked HTTP against real deserializer, OS secure-store adapters, and the CI native-media smoke that generates distinct H.264/AAC fixtures then verifies import, extracted audio, repeated exact-frame seeks, two-video/two-audio timeline export, queue retry, approved AI export and Reel export.
3. **Golden:** synthetic color wedges, known LUTs, moving timecode counters, mixed-rate source video, title/captions/transition composite and expected pixel/frame/audio results.
4. **End-to-end:** interactive workflows UC-01..UC-06 through the actual desktop UI and exported media; run with network blocked, then user-approved Gemini integration. The Windows UI smoke launches the real desktop process, verifies 1280x800 bounds, resizes to 960x640, selects all four workspaces, exercises `Ctrl+3`, and checks the reachable controls; HiDPI hardware and non-Windows GUI runs remain separate evidence requirements.
5. **Release:** dependency/license matrix, package/install/uninstall, explicit package-version and architecture metadata assertions, runtime-specific installer naming, macOS plist lint/identity/version validation, Linux DEB install/remove smoke, bundled FFmpeg/ffprobe presence with adjacent support files and clean-`PATH` runtime-resolution smoke, per-file native-bundle provenance manifests, checksum-manifest valid/escape coverage, keychain, OS/CPU compatibility, code signing where possible, smoke on real hardware.

## Required golden fixtures (generated locally, licensed)
- 3s 30fps color bars and 24fps second source; one variable-rate fixture; landscape+portrait rotations.
- 1 kHz tone/beeps and spoken permissively generated sample with known timestamp text; silence and music clip.
- PNG sRGB with alpha, ICC-tagged JPEG test chart, 17/33 LUT identity and warm look.
- 2x track overlays, wipes, fades, animated title/caption, before/after reveal source.
Use FFmpeg lavfi generators where useful; keep recorded scripts and expected checksums.

## Must-pass acceptance assertions
- `save(load(save(project)))` matches canonical snapshot; schema version migration tested.
- Undo(AI apply plan) exactly restores prior snapshot; failed 3rd action rolls back 1st and 2nd.
- Invalid AI targets/actions/values produce no changes and clear errors.
- Every approved AI operation is present in timeline project, and renderer honors it; no “advisory-only” mode reported as applied.
- Photo/video same-look sample values within declared color tolerance after profile conversions.
- 30fps clip split at frame 90 occurs exactly at 3s (not near 2.97s); rational NTSC rates tested.
- Export H.264/AAC contains right frames/aspect/duration, audible music/captions and bounded audio drift.
- Preview-vs-export selected frames under measured pixel tolerance; scope/limitations documented.
- Project survives blocked network, wrong API key, corrupt project, missing media, canceled render, FFmpeg crash, denied upload and 4K stress.
- Original source checksums remain unchanged; temp file removed on failed export.
- No secrets in git, project JSON, reports or logs.

## Security abuse tests
Prompt injection embedded in subtitles, unexpected JSON keys, path traversal in media references, package checksum sibling escapes and unlisted nested files, huge frames/LUTs, negative/overflow durations, symlink redirects, hostile file names, model retry storms, cache eviction and OS secret-store failure.

## Platform matrix
Windows 11 x64, macOS Apple Silicon + Intel if available, Linux Ubuntu x64 baseline; track exact OS versions / hardware / driver / native binaries. CI native-media smoke is evidence for host codec/process behavior, not a substitute for GUI smoke. Mark untested environments unverified.

## Gates
- Every commit: formatting/analyzers + unit + integration tests executable on runner.
- Each vertical slice: fixture rendering with decode/metadata validation; report provenance.
- Pre-release: all P0 stories functional, security/license review, executable UI smoke, install/relaunch/recover, accessibility review and package checksum.
- Test flakiness or skips: report why; never convert to green by swallowing errors.
