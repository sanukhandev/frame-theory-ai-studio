# ADR-0005 — Prepare local video audio before Gemini transcription

**Date:** 2026-10-17  
**Status:** Accepted for the current transcription baseline

## Context

The desktop transcription workflow accepts local videos with audio, while the Gemini transcription contract uploads audio inputs. Passing a video path directly would either disclose more media than requested or fail content-type validation.

## Decision

Keep the upload boundary audio-only. `FfmpegAudioSourcePreparer` passes supported local audio through unchanged and extracts only the first audio stream from a supported local video to a temporary mono 48 kHz PCM WAV. The temporary file is bounded to 20 MiB, deleted on success, cancellation or failure, and is never persisted in the project. Reparse-point inputs are rejected before FFmpeg runs. The transcript is re-associated with the original selected media URI while retaining the SHA-256 fingerprint of the bytes sent to Gemini.

The UI requires explicit per-request consent that names the selected local audio or extracted video audio track. No provider response can change this scope or execute a command.

## Consequences

- Video transcription remains source-grounded without uploading video frames.
- Long audio is rejected clearly until a reviewed Files API path is implemented.
- FFmpeg remains an optional host capability; manual editing and local caption workflows remain available without it.
- Temporary audio extraction is covered by unit tests and synthetic H.264/AAC smoke output on Windows plus hosted Windows/macOS/Linux CI.
