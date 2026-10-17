# V1 Acceptance Audit

Audit checkpoint: 2026-10-17, commit `cc64553`, Windows 11 x64 plus hosted CI runs `39050810748`, `39051936860`, `39052895799`, `39053781388`, `39055332326`, `39056049489`, `39056614730`, `39057464353` and `39057754819`.

## Evidence

- GitHub task visibility: 47 implementation issues are closed; remaining implementation issues are partial, blocked, or future P1/P2 work; epic issues remain open until their aggregate gates pass.
- Local Release gate: `dotnet format` exit 0, Release build exit 0 with 0 warnings/errors, 131 unit tests passed, 2 golden tests passed, and `git diff --check` passed. Focused FT-602 provider/preparer coverage passed 17 tests.
- Hosted CI evidence: runs `39050810748`, `39051936860`, `39052895799`, `39053781388`, `39055332326`, `39056049489`, `39056614730`, `39057464353` and `39057754819` passed docs/schema validation plus restore/build/format/test/publish/SHA-256 artifact jobs on Windows, macOS and Linux; `39051936860`, `39053781388`, `39055332326`, `39056049489`, `39056614730`, `39057464353` and `39057754819` also passed the native FFmpeg media smoke on all three runners, with `39056049489` publishing dependency and codec-build evidence artifacts. The last two runs additionally validate bounded preview-proxy creation, ffprobe dimensions and content-fingerprint reuse.
- Real media evidence: Windows FFmpeg/ffprobe photo/video import, extracted transcription audio, color, timeline, audio, AI edit, Reel and queue-retry smoke outputs are recorded in the implementation checkpoint and E2E matrix. Hosted Windows/macOS/Linux native-media import, still-image conversion/import/PNG export, extracted-audio, repeated-seek and deterministic two-video/two-audio timeline exports passed in `39056614730`, including AI and before/after Reel outputs.
- Packaging evidence: framework-dependent per-OS publish artifacts and SHA-256 manifests are verified; no installer, native FFmpeg bundle, signed artifact or notarized artifact is claimed.

## P0 blockers before V1

| Area | Open task(s) | Reason V1 cannot be tagged |
|---|---|---|
| Source-grounded transcription | FT-602 | Desktop/provider adapter, safe video-audio extraction, resumable Files API cleanup and fake-HTTP coverage exist; live approved-key/spoken-fixture verification and local ASR fallback remain open |
| Gemini production loop | FT-706 | Desktop revision validation, retries, usage and redacted history exist; live provider-to-plan execution remains unverified |
| Creator Reel/export | FT-803 | Multi-clip Reel, social photo dimensions and queue retry are verified; approved Frame Theory watermark asset/branded export remains unavailable |
| UX/performance/reliability | FT-901..FT-904 | Interactive/manual GUI acceptance remains open; bounded headless proxy evidence now passes but has not replaced the required UI acceptance |
| QA/release | FT-1002, FT-1003, FT-1005, FT-1006 | Manual journeys, license/CVE clearance, installer/native media redistribution, relaunch/recovery and final audit remain open; FT-1001/FT-1004 are verified |

P1/P2 issues are intentionally not part of a V1 completion claim. V1 remains **not complete** until the P0 blockers above have executable evidence.
