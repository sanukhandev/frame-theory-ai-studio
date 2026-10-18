# Security, Privacy and Data-Handling Requirements

## Baseline posture
Editing is local. Gemini is an **optional online service** that requires a user-supplied API key, informed media-upload consent, network availability and provider-account limits/charges.

## Assets, authentication, storage
- Never hardcode API keys in source, tests, screenshots, settings JSON or logs. Use DPAPI/Credential Manager on Windows, macOS Keychain, and a supported Linux secret service via a tested `ISecretStore` adapter.
- Never include secrets in telemetry, crash dumps or environment diagnostics; redact known tokens and file URLs.
- Local diagnostics are written only to the per-user application-data log, redact Gemini token shapes plus API-key/authorization assignments, and never send reports over the network.
- Project saves include media references and edit metadata but no raw API keys and by default no duplicated source media.
- Separate private per-user cache and thumbnail/transcription directories; user-configurable retention and deletion.
- Reject path traversal and symlink escapes; never let Gemini choose absolute paths, executable names or file/network actions.

## Upload and model analysis
- UI explicitly previews **which frames/segments/audio** are being sent and identifies destination/provider.
- Default: select sampled frames plus coarse metadata, excluding audio unless explicitly selected. Allow full video analysis only with separate opt-in.
- Include cost/usage disclosures; model inventory and pricing are fetched/configured, not falsely assumed.
- Respect provider media size/duration limits at runtime; bounded retries, cancellation, local extracted-audio cleanup and remote temporary-file deletion for Gemini Files API uploads.
- Gemini plan inputs reject malformed inline base64, inline images larger than 20 MiB decoded, and text metadata larger than 1 MiB before a request is sent.
- Gemini response bodies are bounded to 4 MiB before deserialization and oversized responses are reported as malformed provider output.
- Do not promise data is never retained by the third-party model; link to provider terms and display network/privacy notice.
- Manual editing, presets, imports, exports and saved projects work offline without API access.

## AI threat model
Attackers can place instructions inside video frames, transcript text, imported subtitles, project metadata and malicious Gemini output. Treat all source content and provider output as untrusted. Use strict response schemas, safe object deserialization, enum allowlists, operation-specific semantic validation, size limits, and transactional edit commands.

Threats: prompt injection -> blocked by command allowlist; command injection -> ArgumentList and no shell; memory exhaustion -> limits and file probes; media parser CVEs -> maintained native deps/sandboxed workers; destructive edits -> copy-on-write snapshots, atomic output writes, undo checkpoints; model cost blowout -> quotas/confirmations; exfiltration -> user consent and no hidden uploads.

## Consent and UX
- The upload consent is per request/scope, revocable; auto-apply is separate from upload permission.
- Failed analysis cannot mutate the project.
- Do not send sensitive filenames, full folder listings or unused media to Gemini.
- Provide AI activity log showing what was uploaded, when, model, operation group, and estimated/returned usage (redacted).

## Supply chain
- Track SBOM, package vulnerability checks, binaries, native licenses, signing/notarization requirements.
- Native media bundling is opt-in and fail-closed: package scripts require both reviewed `ffmpeg` and `ffprobe` binaries, reject symlink/reparse-point inputs, copy the adjacent reviewed support files needed by native builds, place them beside the app, and never download or select binaries from Gemini output.
- Native validation manifests record bounded version output and SHA-256 for every staged bundle file; the verifier re-hashes the directory before packaging without treating provenance as license or codec approval.
- Package checksum verification resolves manifest entries with path-relative containment; sibling-prefix paths and traversal attempts are rejected before hashing, and every file except the exact manifest path is counted, with hosted regressions covering valid, nested-file and escaping manifests.
- No executable plugins from Gemini; no arbitrary plugin execution from external downloaded packages.
- Harden GitHub workflows with least privilege; do not run untrusted release secrets on PRs.

## Mandatory tests
Offline no-key launch, refused upload, OS key storage roundtrip, bad token redaction, malicious caption/prompt injection, project path traversal, malformed LUT, media decoder crash, failed FFmpeg export, atomic rollback, deleted cache, account rate limit and project recovery.
