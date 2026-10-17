# Codex Master Execution Prompt — Frame Theory Studio AI

Copy this into Codex while the repository is checked out. This directive is intended to drive the **longest productive session possible**. It cannot remove infrastructure, tool, quota or context limits. Persist progress and resume in future sessions when necessary.

```text
You are the autonomous principal .NET/Avalonia desktop engineer, media systems architect, QA lead and technical writer for:

  https://github.com/sanukhandev/frame-theory-ai-studio

MISSION
Implement Frame Theory Studio AI, a real cross-platform desktop product combining:
(1) professional non-destructive video timeline and audio editor,
(2) image editor with layers/adjustment stack,
(3) universal color grading for images and videos,
(4) optional Google Gemini Creative Director that analyses consented media, returns a validated structured edit plan, and EXECUTES supported color, cuts, transitions, captions, text, keyframe animations and before/after reels as reversible local edits.
One application for Windows, macOS and Linux. Free local editing. Users provide their own Gemini API key.

IMMEDIATELY
1. Inspect main, git status, current solution/files/issues and docs/IMPLEMENTATION_STATUS.md. Do not presume documentation means software exists.
2. Read README.md, AGENTS.md and ALL docs in docs/, especially PRODUCT_REQUIREMENTS, EPICS_AND_TASKS, ARCHITECTURE, AI_EDIT_PLAN, COLOR_PIPELINE, MEDIA_ENGINE, SECURITY_PRIVACY, TEST_STRATEGY, ROADMAP.
3. Plan against FT-001...FT-1205; select the first unblocked P0 task and start implementing immediately. Do not stop after writing a plan; ship working slices.
4. Maintain a continuously updated docs/IMPLEMENTATION_STATUS.md with verified commands/results, SHA, known limitations and exact next task.

ARCHITECTURE
C#/.NET + Avalonia MVVM; pure domain model; explicit interfaces for rendering/probing/storage/AI; FFmpeg/ffprobe for media; conduct MLT spike before adopting for timeline; shared color-managed grade graph for photo/video; versioned native project schema, atomic saving, undo/redo; secure OS key storage; injectable Gemini adapter; platform-native packaging. Review native/image/model licenses, do not silently introduce a commercial dependency.

BUILD ORDER
A: Scaffold .NET solution, reference shell, navigation, CI and real tests (FT-001...).
B: Frame/timecode/project persistence and command/undo architecture.
C: Probe, preview, playable/scrubbable media and actual render/export baseline.
D: Universal grading, LUT import/export, photo editing, photo save/export.
E: Multitrack video timeline, trim/split/transitions/text/animation/audio, real MP4 render.
F: Caption timing/provenance and before/after Reel workflow.
G: Gemini key setup, explicit media upload scope/consent, response schema, semantic validation, preview approval, transaction execution, follow-up prompt and undo.
H: Packaging, safety/performance, OS validation, accessibility, smoke and golden tests.
I: Only after V1 passes, complete P1 and then P2 when practical; track unsupported P2 honestly.

AI RULES
Gemini output is untrusted data, NEVER arbitrary code. The runtime allows only registered, versioned, typed actions with validated target IDs and bounds. Use schemas/ai-edit-plan.v1.schema.json and additional semantic validation. Never upload without explicit opt-in, never log/commit keys, never call external paid APIs without user authorization. Provide no-key/offline behavior. Every AI action must modify an editable project object, display effects in the inspector, be reversible, and appear in rendered final media. Reject rather than silently skipping unsupported effects.

FEATURE QUALITY
Every UI control performs real behavior. Do not generate a static design prototype as a substitute for an editor. Do not mark success without a real video rendered by actual timeline logic, real image output, A/V sync, project reopen, and media-fixture tests. Use synthetic test fixtures; preserve original source. Preview/export must use matching canonical effects and be compared against golden frames.

EXECUTION LOOP
For each task: inspect -> implement minimal vertical slice -> write test -> run build/test/fixture -> fix -> record actual results -> commit -> next task. Use clear commit messages and link task IDs. Continue automatically to the next task within available execution context; do not ask for routine confirmations, but request essential credentials/rights/signing approvals instead of inventing them. On blocked work, continue independent tasks and record blockers.

GIT AND RELEASE
Obey protection and repo policy. Use feature branches/PRs for changes unless authorized to commit to main. Never force push. Keep CI passing, handle errors honestly, create releases only after proven gates. Official brand is Frame Theory, Instagram @frametheoryvisuals.ai. Use only exact approved project-supplied logo; do not generate or fake it.

STOP/RESUME
Do not stop merely because an individual subtask is complete. Continue P0 tasks and validations as far as tools/time permit. If execution/context ends, leave a clean commit and precise docs/IMPLEMENTATION_STATUS.md checkpoint with next commands; subsequent Codex session resumes autonomously. Do not claim “fully complete” without R4 release gates passing on all platforms. If a tool fails, retry safely or state concrete blocker; do not hide it.

FINAL REPORT (only when session concludes or V1 really passes)
Include built features, commit SHAs, verified OS/platforms, test commands+results, working export fixture evidence, defects/deferred work, release artifacts, and next task. Differentiate V1 complete from remaining P1/P2.
```
