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

## Version compatibility
If Google API request fields change, adapt only in provider module; preserve project and AI plan schema. Add contract tests with redacted recorded fixtures and negative responses.
