# GitHub Delivery Tracker — Frame Theory Studio AI

**Created:** 2026-10-17. **Current state:** all work items **open**; tasks represent planned implementation, **not completed features**.

## Summary

- **13 epics** with GitHub issue tracking.
- **69 numbered implementation tasks** (FT-001 through FT-1205, grouped into E00-E12).
- **P0 = V1 launch scope** (58 tasks), **P1 = professional enhancements** (6 tasks), **P2 = advanced/research** (5 tasks).
- The complete acceptance criteria and dependency order live in [EPICS_AND_TASKS.md](EPICS_AND_TASKS.md).
- Codex instructions: [AGENTS.md](../AGENTS.md) and [CODEX_MASTER_PROMPT.md](CODEX_MASTER_PROMPT.md).

## Epic index

| Epic issue                                                             | Area                        | Priority | Task IDs         |
| ---------------------------------------------------------------------- | --------------------------- | -------- | ---------------- |
| [E00](https://github.com/sanukhandev/frame-theory-ai-studio/issues/1)  | Foundation                  | P0       | FT-001..FT-006   |
| [E01](https://github.com/sanukhandev/frame-theory-ai-studio/issues/2)  | Projects, persistence, undo | P0       | FT-101..FT-105   |
| [E02](https://github.com/sanukhandev/frame-theory-ai-studio/issues/3)  | Playback and media export   | P0       | FT-201..FT-205   |
| [E03](https://github.com/sanukhandev/frame-theory-ai-studio/issues/4)  | Universal color grading     | P0       | FT-301..FT-306   |
| [E04](https://github.com/sanukhandev/frame-theory-ai-studio/issues/5)  | Photo workspace             | P0       | FT-401..FT-405   |
| [E05](https://github.com/sanukhandev/frame-theory-ai-studio/issues/6)  | Video timeline              | P0       | FT-501..FT-506   |
| [E06](https://github.com/sanukhandev/frame-theory-ai-studio/issues/7)  | Captions, motion, audio     | P0       | FT-601..FT-605   |
| [E07](https://github.com/sanukhandev/frame-theory-ai-studio/issues/8)  | Gemini executor             | P0       | FT-701..FT-706   |
| [E08](https://github.com/sanukhandev/frame-theory-ai-studio/issues/9)  | Creator Reel and exports    | P0       | FT-801..FT-804   |
| [E09](https://github.com/sanukhandev/frame-theory-ai-studio/issues/10) | UX and performance          | P0       | FT-901..FT-904   |
| [E10](https://github.com/sanukhandev/frame-theory-ai-studio/issues/11) | Release, QA, security       | P0       | FT-1001..FT-1006 |
| [E11](https://github.com/sanukhandev/frame-theory-ai-studio/issues/12) | Competitive professional    | P1       | FT-1101..FT-1106 |
| [E12](https://github.com/sanukhandev/frame-theory-ai-studio/issues/13) | Advanced research           | P2       | FT-1201..FT-1205 |

## Task issue lookup

Tasks are tracked in [GitHub issues](https://github.com/sanukhandev/frame-theory-ai-studio/issues). Completed implementation issues are closed with evidence comments; partial, blocked and future work remains open. Each issue starts with its stable ID, e.g. [FT-001](https://github.com/sanukhandev/frame-theory-ai-studio/issues/14), [FT-301](https://github.com/sanukhandev/frame-theory-ai-studio/issues/30), [FT-705](https://github.com/sanukhandev/frame-theory-ai-studio/issues/56) and [FT-1006](https://github.com/sanukhandev/frame-theory-ai-studio/issues/71). Use the title ID, not issue number, when linking commits across reorganization.

## Recommended Codex workflow

1. Read docs/IMPLEMENTATION_STATUS.md to locate the first unblocked task.
2. Work from E00 through E10; do not interleave P1/P2 until R4 V1 gate passes.
3. Put issue ID in commit and close only after real functionality and test evidence are attached.
4. Update status checkpoint after each commit and report next unblocked work.
5. If a session ends, restart Codex with [the master execution prompt](CODEX_MASTER_PROMPT.md) and resume from the checkpoint.

## Important caveat

GitHub issues were programmatically created based on the spec; no software features were automatically implemented, tested, installed or released. “Complete application” requires actual binaries, fixtures, package validation and the V1 release gate.
