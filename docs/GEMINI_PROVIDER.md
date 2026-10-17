# Gemini Integration Implementation Notes

## External APIs
Use current Google Gemini documentation at implementation time:
- https://ai.google.dev/gemini-api/docs/structured-output
- https://ai.google.dev/gemini-api/docs/video-understanding
- https://ai.google.dev/gemini-api/docs/files
- https://ai.google.dev/gemini-api/docs/pricing

Do not embed model names or prices as unquestioned constants. Expose a configurable supported model, validate against provider capabilities and user quota. Model response schema support is not the same as semantic correctness.

## Design
`IGeminiClient` is isolated from UI and edit executor. Input contains optional consented sampled frames/audio, time-indexed shot boundaries, selected IDs/capabilities, project metadata and user intent. Response is a strict AI Edit Plan. Provider errors become typed errors: invalid key, rate limit, quota, unsupported media, timeout, cancellation, unsupported schema or malformed output.

## Cost/privacy controls
Default network-off until key configured and upload scope approved. Estimate bytes/cost where possible, show returned usage, preview sensitive thumbnails and filenames before upload, and permit per-job cancel. Full video upload is explicit, never implicit. Temp remote files should be cleaned up when the provider supports it.

## Transcript handling
Use a dedicated timestamped transcription capability with source provenance. Do not allow Gemini to invent words as speech captions. AI-authored social hooks/titles are separately tracked, editable objects.

## FT-602 implementation
`FrameTheory.Captions.GeminiTranscriptionProvider` is the consent-gated REST adapter for the Gemini Interactions transcription API. It accepts only a local supported audio file, reads it into a bounded inline request, sends the user-provided key only in the `x-goog-api-key` header, and computes a SHA-256 fingerprint while reading the source. The provider requests verbatim word timestamps and rejects successful responses that do not contain valid, non-overlapping `word_info` annotations. Provider errors are surfaced without returning provider response bodies or logging secrets.

The current adapter intentionally limits inline audio to 20 MiB and does not infer an audio track from a video container. Longer recordings require the Files API or a local ASR adapter; video callers must provide an extracted audio source. These limits prevent hidden media conversion or an unbounded upload while the desktop transcription workflow is wired to the provider.

## Version compatibility
If Google API request fields change, adapt only in provider module; preserve project and AI plan schema. Add contract tests with redacted recorded fixtures and negative responses.
