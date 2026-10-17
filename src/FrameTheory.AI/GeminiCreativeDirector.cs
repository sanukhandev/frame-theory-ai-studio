using System.Text.Json;

namespace FrameTheory.AI;

public sealed record GeminiCreativeDirectorResult(
    string InteractionId,
    string PlanJson,
    GeminiUsage? Usage,
    int Attempts,
    int UploadedSampleCount,
    long UploadedBytes);

public sealed class GeminiCreativeDirectorClient
{
    private const int MaximumSamples = 12;
    private const long MaximumInlineBytes = 20L * 1024 * 1024;
    private static readonly JsonElement PlanSchema = JsonDocument.Parse("""
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["schemaVersion", "planId", "projectId", "intent", "actions"],
          "properties": {
            "schemaVersion": { "const": "1.0" },
            "planId": { "type": "string", "minLength": 1, "maxLength": 128, "pattern": "^[a-zA-Z0-9._-]+$" },
            "projectId": { "type": "string", "minLength": 1, "maxLength": 128, "pattern": "^[a-zA-Z0-9._-]+$" },
            "intent": { "type": "string", "minLength": 1, "maxLength": 500 },
            "actions": {
              "type": "array",
              "maxItems": 100,
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["id", "type", "targetId", "startFrame", "durationFrames", "params"],
                "properties": {
                  "id": { "type": "string", "minLength": 1, "maxLength": 128, "pattern": "^[a-zA-Z0-9._-]+$" },
                  "type": { "type": "string" },
                  "targetId": { "type": "string", "minLength": 1, "maxLength": 128, "pattern": "^[a-zA-Z0-9._-]+$" },
                  "startFrame": { "type": "integer", "minimum": 0, "maximum": 100000000 },
                  "durationFrames": { "type": "integer", "minimum": 1, "maximum": 100000000 },
                  "params": { "type": "object", "maxProperties": 32 }
                }
              }
            }
          }
        }
        """).RootElement.Clone();
    private readonly GeminiInteractionsClient _client;

    public GeminiCreativeDirectorClient(GeminiInteractionsClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<GeminiCreativeDirectorResult> CreatePlanAsync(
        LocalMediaAnalysisReport report,
        IReadOnlySet<string> targetIds,
        long projectDurationFrames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(targetIds);
        if (!report.UploadConsent)
        {
            throw new InvalidOperationException("Creative direction requires explicit consent for the selected samples.");
        }

        if (projectDurationFrames <= 0 || targetIds.Count == 0)
        {
            throw new ArgumentException("A positive project duration and at least one target are required.");
        }

        var samples = report.Samples.ToArray();
        if (samples.Length == 0)
        {
            throw new InvalidOperationException("Creative direction requires at least one approved media sample.");
        }

        if (samples.Length > MaximumSamples)
        {
            throw new InvalidOperationException($"Creative direction accepts at most {MaximumSamples} media samples per request.");
        }

        var inputs = new List<GeminiInteractionInput>(samples.Length + 1)
        {
            GeminiInteractionInput.CreateText(BuildPrompt(targetIds, projectDurationFrames, samples))
        };
        long uploadedBytes = 0;
        foreach (var sample in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (sample.SampleKind)
            {
                case "video-frame-ppm":
                case "photo-preview-ppm":
                    var png = PpmPngEncoder.Encode(sample.Payload);
                    uploadedBytes = checked(uploadedBytes + png.LongLength);
                    if (uploadedBytes > MaximumInlineBytes)
                    {
                        throw new InvalidOperationException("The approved visual sample scope exceeds Gemini's 20 MiB inline limit; reduce the sample scope before retrying.");
                    }

                    inputs.Add(GeminiInteractionInput.CreateImage(png));
                    break;
                case "audio-waveform":
                    if (sample.Payload.Length > 1_048_576)
                    {
                        throw new InvalidOperationException("The approved audio waveform sample exceeds the supported 1 MiB metadata limit.");
                    }

                    uploadedBytes = checked(uploadedBytes + sample.Payload.LongLength);
                    inputs.Add(GeminiInteractionInput.CreateText($"Approved audio waveform sample: {System.Text.Encoding.UTF8.GetString(sample.Payload)}"));
                    break;
                default:
                    throw new InvalidOperationException($"The analysis sample kind '{sample.SampleKind}' is not supported for Gemini creative direction.");
            }
        }

        var result = await _client.CreatePlanAsync(inputs, PlanSchema, cancellationToken);
        return new GeminiCreativeDirectorResult(
            result.InteractionId,
            result.OutputText,
            result.Usage,
            result.Attempts,
            samples.Length,
            uploadedBytes);
    }

    private static string BuildPrompt(
        IReadOnlySet<string> targetIds,
        long projectDurationFrames,
        IReadOnlyList<LocalAnalysisSample> samples)
    {
        var targetList = string.Join(", ", targetIds.Order(StringComparer.Ordinal));
        var sampleSummary = string.Join("; ", samples.Select((sample, index) =>
            $"sample {index + 1}: kind={sample.SampleKind}, frame={(sample.FrameIndex?.ToString() ?? "n/a")}, timestamp={(sample.Timestamp?.ToString() ?? "n/a")}"));
        return $"""
            You are Frame Theory Studio's Creative Director. Analyze only the attached, user-approved media samples and return one editable plan.
            Return JSON only, matching the supplied schema exactly. Never return commands, code, URLs, file paths, uploads, or arbitrary operations.
            Use only allowlisted operation types from the schema and only these local target IDs: {targetList}.
            The project duration is {projectDurationFrames} frames. Keep every action within that range.
            Do not invent a transcript. Caption actions must use authored-caption unless source-timed transcript evidence is supplied.
            Attached sample metadata: {sampleSummary}.
            Prefer a small number of concrete, bounded actions; return an empty actions array when no safe edit is justified.
            """;
    }
}
