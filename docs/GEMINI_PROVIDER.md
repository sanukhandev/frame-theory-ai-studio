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
`FrameTheory.Captions.GeminiTranscriptionProvider` is the consent-gated REST adapter for the Gemini Interactions transcription API. It accepts only a local supported audio file, reads and fingerprints it with SHA-256, uses a bounded inline request up to 20 MiB, and switches larger audio to the resumable Gemini Files API. Files API uploads are sent as URI inputs to Interactions and deleted in a bounded cleanup path after the request, including failed transcription responses. The provider sends the user-provided key only in the `x-goog-api-key` header, requests verbatim word timestamps, and rejects successful responses that do not contain valid, non-overlapping `word_info` annotations. Provider errors are surfaced without returning provider response bodies or logging secrets.

The current adapter intentionally keeps inline audio at 20 MiB and requires the Files API upload URL to remain on the official Gemini host. The desktop workflow still extracts audio from video before this provider is called; it never uploads video frames for transcription. Local ASR fallback and live approved-key/spoken-fixture verification remain separate release work.

## FT-1104 reference analysis
`FrameTheory.AI.GeminiReferenceAnalysisClient` accepts only explicitly consented bounded visual PPM samples. It converts samples to PNG, keeps them inline while the request remains within the 20 MiB budget, and switches additional bytes to the resumable Files API when needed. Uploaded images are referenced only through validated HTTPS Gemini-hosted URIs and are deleted in a bounded cleanup path after the interaction, including failed responses. The API key is sent only as a header; the returned grade is strict JSON with locally enforced bounds and is never applied automatically. Live provider execution and reference-grade semantic quality still require an approved user key and fixture.

## FT-706 follow-up transport
`FrameTheory.AI.GeminiInteractionsClient` provides text-only follow-up requests against the fixed Interactions endpoint. It sends the previous interaction ID as provider conversation state, applies a per-attempt timeout, retries only bounded 408/429/5xx failures, classifies invalid-key/quota/rate-limit/timeout/cancellation/transport failures, and returns provider usage without applying output. `FileGeminiInteractionHistory` stores timestamp, model, interaction ID, outcome, attempt count, prompt length and token usage as local JSONL; it never stores the prompt, API key or raw provider response. Callers must pass returned text through the existing local parser and validator before any edit can be approved.

## Version compatibility
If Google API request fields change, adapt only in provider module; preserve project and AI plan schema. Add contract tests with redacted recorded fixtures and negative responses.
