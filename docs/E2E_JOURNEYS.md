# End-to-End Journey Matrix

This matrix records executable evidence without confusing headless smoke with interactive UI acceptance.

| Journey | Executable evidence on Windows | Current result | Remaining gate |
|---|---|---|---|
| UC-01 Local photo edit | `FrameTheory.PhotoImportSmoke`, `FrameTheory.PhotoExportSmoke`, photo recovery/import/export tests | Real FFmpeg import, preview and PNG/JPEG export; source-preserving tests pass | Run through the Photo workspace and verify close/reopen manually |
| UC-02 Local video edit | `FrameTheory.TimelineExportSmoke <source-a> <source-b> <output.mp4> [font]` | Real multitrack video/audio composite, transition, title/caption and ffprobe receipt | UI journey and 1080x1920 acceptance on each OS |
| UC-03 Universal look | Color golden tests, LUT roundtrip tests and Color workspace inspector | Deterministic preview/export color path and LUT validation pass | Cross-media normalized sample comparison through UI |
| UC-04 Gemini loop | AI parser/validator/executor tests and timeline smoke AI grade/reel export | Invalid plans reject; approved actions mutate editable timeline; rollback/undo tested | Real provider request, follow-up history and consented interactive flow |
| UC-05 Offline and failures | No-key settings tests, capability detector, cancellation, diagnostics and atomic export tests | Manual paths remain local; failures do not replace outputs; diagnostics are local/redacted | Network-blocked interactive launch on Windows/macOS/Linux |
| UC-06 Before/after Reel | Timeline smoke AI `reel.before_after` action with real MP4 output | Editable one-clip split/wipe/timed reveal paths render and probe successfully | Multi-clip wizard, approved watermark and interactive 9:16 acceptance |

## Windows reference commands

Build first with `dotnet build FrameTheory.sln -c Release`, then run the smoke programs with synthetic or cleared local fixtures. Keep all output outside the repository. `docs/PERFORMANCE.md` records the exact decoder fixture and measured output used by FT-903.

The desktop application itself remains the final acceptance surface. A passing smoke command is not evidence that macOS/Linux GUI behavior, packaging, or manual keyboard acceptance has been verified.
