# Recovery Guide

This guide applies to the current portable validation build. It is not an installer or a V1 release promise.

## Preserve user work

- Project files use atomic replacement; keep the `.ftstudio` file and its referenced media together when moving a project.
- Originals are read-only inputs. Reopen the project before deleting or relocating source media.
- If media moved, use the Video or Photo missing-media relink flow and select replacements inside the project directory.
- If an export is canceled or fails, use the retry action after checking capability status and the output path; incomplete temporary outputs are not final exports.

## Missing dependencies

- Start the application without Gemini or FFmpeg to keep manual project inspection available.
- Use **Refresh capability check** after installing or correcting `ffmpeg` and `ffprobe` on the host.
- Treat `Missing` and `Unknown` capability states as actionable failures; do not distribute a package that silently assumes native tools are present.

## AI and privacy

- Gemini is optional. Do not enter an API key into logs, project files or shell arguments.
- Review the displayed upload scope and approve it before sending media. Canceling an analysis must leave the local project unchanged.
- Load recipes for review only; actions require explicit per-operation approval before applying.

## Current release limitation

The repository has hosted native-media evidence, checksummed portable publishes, unsigned validation installers and a hosted-validated optional reviewed FFmpeg/ffprobe bundle mechanism. Approved production redistribution, signing/notarization, approved branding and clean-host install/relaunch evidence remain open release gates.
