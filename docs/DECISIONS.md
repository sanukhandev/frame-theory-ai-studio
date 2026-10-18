# Architecture Decisions and Open Questions

Decisions must be revisited only with evidence. Create `docs/adr/NNNN-*.md` for each approved change.

| ID | Decision | Status | Rationale / next proof |
|---|---|---|---|
| ADR-0001 | .NET C# + Avalonia for shared desktop shell | Accepted direction, pending real boot | Free core, Windows/macOS/Linux support; test native packages |
| ADR-0002 | Offline core; Gemini optional and consent-based | Accepted | Manual workflows must survive no key/internet |
| ADR-0003 | Structured AI plan + strict local executor | Accepted | Reversibility and safety; see AI_EDIT_PLAN |
| ADR-0004 | FFmpeg/ffprobe for decode/export with a deterministic timeline compositor; do not adopt MLT yet | **Accepted for current path** | Hosted three-OS seek, multitrack, audio-mix, AI-edit and Reel exports are recorded in [ADR-0004](adr/0004-ffmpeg-spike.md); interactive GUI parity and release licensing remain separate gates |
| ADR-0005 | Unified color graph with explicit profiles | Accepted direction | Prevent differing photo vs video results |
| ADR-0006 | Versioned project JSON, optional SQLite indexes | Accepted direction | Portable edit history and cheap cache invalidation |
| ADR-0007 | Image renderer library selection | **Open** | SkiaSharp vs ImageSharp: cost/licensing, GPU, ICC, fidelity, cross-platform |
| ADR-0008 | Caption transcription engine | Accepted optional local path; Gemini remains optional provider | User-supplied Whisper-compatible CLI/model provides an offline path; live Gemini remains an explicit consented alternative |
| ADR-0009 | Native packaging / license policy | **Open** | FFmpeg build flags, MLT LGPL, codecs, model weights, signing |
| ADR-0010 | Real-time GPU preview backends | **Open** | Provide consistent CPU fallback; no fictional universal acceleration |

Open questions should not stop core domain work. Prototype and record benchmark/compatibility evidence before binding major native dependencies to public interfaces.

## Process
Any changed decision includes context, alternatives, consequences, rollback plan, test evidence and license/privacy implications. Architectural documents must match implementation.
