using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FrameTheory.AI;

public sealed record AiEditAction
{
    public AiEditAction(
        string id,
        string type,
        string targetId,
        long startFrame,
        long durationFrames,
        IReadOnlyDictionary<string, JsonElement> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Id = id;
        Type = type;
        TargetId = targetId;
        StartFrame = startFrame;
        DurationFrames = durationFrames;
        Params = new ReadOnlyDictionary<string, JsonElement>(parameters.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Clone(),
            StringComparer.Ordinal));
    }

    public string Id { get; }

    public string Type { get; }

    public string TargetId { get; }

    public long StartFrame { get; }

    public long DurationFrames { get; }

    public IReadOnlyDictionary<string, JsonElement> Params { get; }
}

public sealed record AiEditPlan
{
    public AiEditPlan(
        string schemaVersion,
        string planId,
        string projectId,
        string intent,
        IReadOnlyList<AiEditAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        SchemaVersion = schemaVersion;
        PlanId = planId;
        ProjectId = projectId;
        Intent = intent;
        Actions = Array.AsReadOnly(actions.ToArray());
    }

    public string SchemaVersion { get; }

    public string PlanId { get; }

    public string ProjectId { get; }

    public string Intent { get; }

    public IReadOnlyList<AiEditAction> Actions { get; }
}

public sealed class AiPlanFormatException : FormatException
{
    public AiPlanFormatException(string message) : base(message)
    {
    }
}

public sealed record AiOperationDescriptor
{
    public AiOperationDescriptor(string type, string targetKind, string description, IReadOnlyList<string> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Type = type;
        TargetKind = targetKind;
        Description = description;
        Parameters = Array.AsReadOnly(parameters.ToArray());
    }

    public string Type { get; }

    public string TargetKind { get; }

    public string Description { get; }

    public IReadOnlyList<string> Parameters { get; }
}

public static class AiOperationCatalog
{
    private static readonly IReadOnlyDictionary<string, AiOperationDescriptor> Descriptors = new Dictionary<string, AiOperationDescriptor>(StringComparer.Ordinal)
    {
        ["color.adjust"] = new("color.adjust", "media", "Bounded exposure, contrast, saturation, temperature and tint.", ["exposureEV", "contrast", "saturation", "temperature", "tint"]),
        ["clip.trim"] = new("clip.trim", "clip", "Trim an existing clip to the requested frame range.", []),
        ["clip.split"] = new("clip.split", "clip", "Split an existing clip at a frame boundary.", []),
        ["clip.move"] = new("clip.move", "clip", "Move an existing clip by a bounded frame offset.", ["offsetFrames"]),
        ["clip.transform"] = new("clip.transform", "clip", "Apply bounded clip scale and offsets.", ["scale", "offsetX", "offsetY", "rotation"]),
        ["transition.add"] = new("transition.add", "clip", "Add a supported transition to neighboring clips.", ["kind"]),
        ["title.add"] = new("title.add", "title-track", "Add text using an approved font ID and named animation preset.", ["text", "fontId", "preset"]),
        ["caption.add"] = new("caption.add", "caption-track", "Add a source-grounded or explicitly authored caption.", ["text", "provenance"]),
        ["animation.keyframes"] = new("animation.keyframes", "clip", "Add bounded opacity and transform keyframes.", ["property", "keyframes"]),
        ["audio.gain"] = new("audio.gain", "audio", "Apply bounded gain, fades and ducking.", ["gainDb", "fadeInFrames", "fadeOutFrames", "duckingDb"]),
        ["preset.apply"] = new("preset.apply", "media", "Apply a trusted local preset ID.", ["presetId"]),
        ["reel.before_after"] = new("reel.before_after", "project", "Create an editable before/after reveal composition.", ["style"])
    };

    public static IReadOnlyList<AiOperationDescriptor> Supported => Array.AsReadOnly(Descriptors.Values.OrderBy(descriptor => descriptor.Type).ToArray());

    public static bool TryGet(string type, out AiOperationDescriptor descriptor) => Descriptors.TryGetValue(type, out descriptor!);
}

public static class AiEditPlanParser
{
    private const int MaximumBytes = 1024 * 1024;
    private static readonly Regex Identifier = new("^[a-zA-Z0-9._-]+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> PlanProperties = ["schemaVersion", "planId", "projectId", "intent", "actions"];
    private static readonly HashSet<string> ActionProperties = ["id", "type", "targetId", "startFrame", "durationFrames", "params"];

    public static AiEditPlan Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || System.Text.Encoding.UTF8.GetByteCount(json) > MaximumBytes)
        {
            throw new AiPlanFormatException("The edit plan is empty or exceeds the supported size.");
        }

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            var root = RequireObject(document.RootElement, "plan");
            RejectUnknownProperties(root, PlanProperties, "plan");
            var schemaVersion = ReadString(root, "schemaVersion", 1, 16);
            if (!string.Equals(schemaVersion, "1.0", StringComparison.Ordinal))
            {
                throw new AiPlanFormatException("Only edit plan schema version 1.0 is supported.");
            }

            var planId = ReadIdentifier(root, "planId");
            var projectId = ReadIdentifier(root, "projectId");
            var intent = ReadString(root, "intent", 1, 500);
            var actionsElement = RequireProperty(root, "actions");
            if (actionsElement.ValueKind != JsonValueKind.Array || actionsElement.GetArrayLength() > 100)
            {
                throw new AiPlanFormatException("The edit plan actions array must contain at most 100 actions.");
            }

            var actions = new List<AiEditAction>();
            var actionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var actionElement in actionsElement.EnumerateArray())
            {
                var action = ParseAction(actionElement);
                if (!actionIds.Add(action.Id))
                {
                    throw new AiPlanFormatException($"Duplicate action ID '{action.Id}'.");
                }

                actions.Add(action);
            }

            return new AiEditPlan(schemaVersion, planId, projectId, intent, actions);
        }
        catch (JsonException exception)
        {
            throw new AiPlanFormatException($"The edit plan JSON is malformed: {exception.Message}");
        }
    }

    private static AiEditAction ParseAction(JsonElement element)
    {
        var action = RequireObject(element, "action");
        RejectUnknownProperties(action, ActionProperties, "action");
        var id = ReadIdentifier(action, "id");
        var type = ReadString(action, "type", 1, 64);
        if (!AiOperationCatalog.TryGet(type, out _))
        {
            throw new AiPlanFormatException($"The operation '{type}' is not allowlisted.");
        }

        var targetId = ReadIdentifier(action, "targetId");
        var startFrame = ReadInteger(action, "startFrame", 0, 100_000_000);
        var durationFrames = ReadInteger(action, "durationFrames", 1, 100_000_000);
        var parameters = ReadParameters(RequireProperty(action, "params"));
        return new AiEditAction(id, type, targetId, startFrame, durationFrames, parameters);
    }

    private static IReadOnlyDictionary<string, JsonElement> ReadParameters(JsonElement element)
    {
        var parameters = RequireObject(element, "params");
        if (parameters.EnumerateObject().Count() > 32)
        {
            throw new AiPlanFormatException("An action may contain at most 32 parameters.");
        }

        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in parameters.EnumerateObject())
        {
            if (!Identifier.IsMatch(property.Name) || property.Name.Length > 64 || !result.TryAdd(property.Name, property.Value.Clone()))
            {
                throw new AiPlanFormatException($"The action contains an invalid or duplicate parameter '{property.Name}'.");
            }
        }

        return result;
    }

    private static string ReadIdentifier(JsonElement parent, string name)
    {
        var value = ReadString(parent, name, 1, 128);
        if (!Identifier.IsMatch(value))
        {
            throw new AiPlanFormatException($"'{name}' must be an identifier.");
        }

        return value;
    }

    private static string ReadString(JsonElement parent, string name, int minimumLength, int maximumLength)
    {
        var property = RequireProperty(parent, name);
        if (property.ValueKind != JsonValueKind.String)
        {
            throw new AiPlanFormatException($"'{name}' must be a string.");
        }

        var value = property.GetString() ?? string.Empty;
        if (value.Length < minimumLength || value.Length > maximumLength)
        {
            throw new AiPlanFormatException($"'{name}' is outside the supported length.");
        }

        return value;
    }

    private static long ReadInteger(JsonElement parent, string name, long minimum, long maximum)
    {
        var property = RequireProperty(parent, name);
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var value) || value < minimum || value > maximum)
        {
            throw new AiPlanFormatException($"'{name}' must be an integer in the range {minimum}..{maximum}.");
        }

        return value;
    }

    private static JsonElement RequireProperty(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var property)
            ? property
            : throw new AiPlanFormatException($"The {parent.ValueKind} object is missing '{name}'.");

    private static JsonElement RequireObject(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
            ? element
            : throw new AiPlanFormatException($"The {name} must be an object.");

    private static void RejectUnknownProperties(JsonElement element, IReadOnlySet<string> allowed, string name)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name) || !seen.Add(property.Name))
            {
                throw new AiPlanFormatException($"The {name} contains an unknown or duplicate property '{property.Name}'.");
            }
        }
    }
}

public sealed record AiPlanValidationContext
{
    public AiPlanValidationContext(string projectId, IReadOnlySet<string> targetIds, long projectDurationFrames)
    {
        if (string.IsNullOrWhiteSpace(projectId) || targetIds is null || projectDurationFrames <= 0)
        {
            throw new ArgumentException("A project ID, target set and positive project duration are required.");
        }

        ProjectId = projectId;
        TargetIds = targetIds;
        ProjectDurationFrames = projectDurationFrames;
    }

    public string ProjectId { get; }

    public IReadOnlySet<string> TargetIds { get; }

    public long ProjectDurationFrames { get; }
}

public sealed record AiPlanValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class AiEditPlanValidator
{
    public static AiPlanValidationResult Validate(AiEditPlan plan, AiPlanValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);
        var errors = new List<string>();
        if (!string.Equals(plan.ProjectId, context.ProjectId, StringComparison.Ordinal))
        {
            errors.Add("The plan project ID does not match the active project.");
        }

        foreach (var action in plan.Actions)
        {
            if (!context.TargetIds.Contains(action.TargetId))
            {
                errors.Add($"Action '{action.Id}' targets an unknown object.");
            }

            if (action.StartFrame > context.ProjectDurationFrames || action.DurationFrames > context.ProjectDurationFrames - action.StartFrame)
            {
                errors.Add($"Action '{action.Id}' exceeds the project frame range.");
            }

            if (!AiOperationCatalog.TryGet(action.Type, out var descriptor))
            {
                errors.Add($"Action '{action.Id}' uses an unsupported operation.");
                continue;
            }

            foreach (var parameter in action.Params.Keys)
            {
                if (!descriptor.Parameters.Contains(parameter))
                {
                    errors.Add($"Action '{action.Id}' contains unsupported parameter '{parameter}'.");
                }
            }

            ValidateActionParameters(action, errors);
        }

        return new AiPlanValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateActionParameters(AiEditAction action, ICollection<string> errors)
    {
        switch (action.Type)
        {
            case "color.adjust":
                ValidateNumber(action, "exposureEV", -8, 8, errors);
                ValidateNumber(action, "contrast", -1, 1, errors);
                ValidateNumber(action, "saturation", -1, 1, errors);
                ValidateNumber(action, "temperature", -1, 1, errors);
                ValidateNumber(action, "tint", -1, 1, errors);
                break;
            case "clip.move":
                ValidateIntegerParameter(action, "offsetFrames", -100_000_000, 100_000_000, errors);
                break;
            case "clip.transform":
                ValidateNumber(action, "scale", 0.01, 20, errors);
                ValidateNumber(action, "offsetX", -1, 1, errors);
                ValidateNumber(action, "offsetY", -1, 1, errors);
                ValidateNumber(action, "rotation", -360, 360, errors);
                break;
            case "transition.add":
                ValidateEnum(action, "kind", ["cross-dissolve", "wipe"], errors);
                break;
            case "title.add":
                ValidateString(action, "text", 1, 1000, errors);
                ValidateIdentifierParameter(action, "fontId", errors);
                ValidateEnum(action, "preset", ["fade-in", "fade-out", "push-in", "pull-out"], errors);
                break;
            case "caption.add":
                ValidateString(action, "text", 1, 10_000, errors);
                ValidateEnum(action, "provenance", ["verified-transcript", "authored-caption"], errors);
                break;
            case "animation.keyframes":
                ValidateEnum(action, "property", ["opacity", "transform"], errors);
                if (!action.Params.TryGetValue("keyframes", out var keyframes) || keyframes.ValueKind != JsonValueKind.Array || keyframes.GetArrayLength() is < 1 or > 100)
                {
                    errors.Add($"Action '{action.Id}' requires 1..100 keyframes.");
                }

                break;
            case "audio.gain":
                ValidateNumber(action, "gainDb", -60, 12, errors);
                ValidateIntegerParameter(action, "fadeInFrames", 0, 100_000_000, errors);
                ValidateIntegerParameter(action, "fadeOutFrames", 0, 100_000_000, errors);
                ValidateNumber(action, "duckingDb", 0, 60, errors);
                break;
            case "preset.apply":
                ValidateIdentifierParameter(action, "presetId", errors);
                break;
            case "reel.before_after":
                ValidateEnum(action, "style", ["split", "wipe", "timed-reveal"], errors);
                break;
        }
    }

    private static void ValidateNumber(AiEditAction action, string name, double minimum, double maximum, ICollection<string> errors)
    {
        if (!action.Params.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || double.IsNaN(number) || double.IsInfinity(number) || number < minimum || number > maximum)
        {
            errors.Add($"Action '{action.Id}' parameter '{name}' must be finite and in range {minimum}..{maximum}.");
        }
    }

    private static void ValidateIntegerParameter(AiEditAction action, string name, long minimum, long maximum, ICollection<string> errors)
    {
        if (!action.Params.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var number) || number < minimum || number > maximum)
        {
            errors.Add($"Action '{action.Id}' parameter '{name}' must be an integer in range {minimum}..{maximum}.");
        }
    }

    private static void ValidateString(AiEditAction action, string name, int minimum, int maximum, ICollection<string> errors)
    {
        var length = action.Params.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Length ?? 0
            : 0;
        if (length < minimum || length > maximum)
        {
            errors.Add($"Action '{action.Id}' parameter '{name}' is missing or outside the supported length.");
        }
    }

    private static void ValidateIdentifierParameter(AiEditAction action, string name, ICollection<string> errors)
    {
        if (!action.Params.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.String || value.GetString() is not { Length: > 0 and <= 128 } identifier || !Regex.IsMatch(identifier, "^[a-zA-Z0-9._-]+$", RegexOptions.CultureInvariant))
        {
            errors.Add($"Action '{action.Id}' parameter '{name}' must be a safe local identifier.");
        }
    }

    private static void ValidateEnum(AiEditAction action, string name, IReadOnlyCollection<string> allowed, ICollection<string> errors)
    {
        if (!action.Params.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.String || !allowed.Contains(value.GetString() ?? string.Empty))
        {
            errors.Add($"Action '{action.Id}' parameter '{name}' is not supported.");
        }
    }
}
