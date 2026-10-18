# ADR-0008: Caption Transcription Engine

## Context

Caption words must be source-grounded and time-aligned. The offline editor cannot require a Gemini key, while remote transcription must remain explicitly consented and optional. Bundling a model or native inference binary would create platform, license, update and redistribution obligations.

## Decision

Keep transcription behind `IProgressiveTranscriptionProvider` and support two independent paths:

1. A user-selected Whisper-compatible CLI executable and model file, invoked with fixed `ArgumentList` parameters and no shell, network download, or model bundling.
2. The consent-gated Gemini transcription provider for users who supply their own key and approve the exact media scope.

The local path is the default offline acceptance path. Both paths must return source-fingerprinted, word-timed results; source mutation, invalid timing, untrusted files and cancellation fail without changing the timeline.

## Evidence

`tools/FrameTheory.LocalWhisperSmoke` exercised the production local adapter on Windows x64 on 2026-10-18 with `openai-whisper` 20250625, the official `tiny.en` model URL, a locally synthesized spoken fixture, and temporary FFmpeg decoding. The real run returned seven words, preserved the source SHA-256, and removed temporary JSON output. Focused parser/provider tests and the hosted Release matrix remain green.

## Consequences

- Manual captioning and verified-transcript import remain fully offline.
- Users are responsible for installing and licensing their selected local engine, model and native dependencies.
- The repository does not redistribute Whisper, model weights or FFmpeg through this decision.
- Live Gemini provider acceptance remains a separate consent, key, cost and privacy gate.

## Reversal

If a supported local engine cannot maintain word timing, source integrity or platform compatibility, keep the adapter available for verified sidecars and require the explicit Gemini path; do not silently substitute ungrounded captions.
