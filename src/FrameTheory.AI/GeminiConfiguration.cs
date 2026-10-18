using FrameTheory.Application;

namespace FrameTheory.AI;

public sealed record GeminiModelOption(string Id, string DisplayName);

public static class GeminiModelCatalog
{
    public static IReadOnlyList<GeminiModelOption> Supported { get; } = Array.AsReadOnly<GeminiModelOption>([
        new("gemini-2.5-flash", "Gemini 2.5 Flash"),
        new("gemini-2.5-pro", "Gemini 2.5 Pro")
    ]);

    public static bool Contains(string modelId) => Supported.Any(model => string.Equals(model.Id, modelId, StringComparison.Ordinal));
}

public sealed record GeminiConfiguration(string ModelId, bool HasApiKey);

public sealed class GeminiConfigurationService
{
    public const string ApiKeyName = "gemini-api-key";
    private readonly ISecretStore _secretStore;

    public GeminiConfigurationService(ISecretStore secretStore)
    {
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    }

    public async Task<GeminiConfiguration> LoadAsync(CancellationToken cancellationToken)
    {
        var apiKey = await _secretStore.ReadAsync(ApiKeyName, cancellationToken);
        return new GeminiConfiguration(GeminiModelCatalog.Supported[0].Id, !string.IsNullOrEmpty(apiKey));
    }

    public Task<string?> ReadApiKeyAsync(CancellationToken cancellationToken) => _secretStore.ReadAsync(ApiKeyName, cancellationToken);

    public async Task<GeminiConfiguration> SaveAsync(string modelId, string apiKey, CancellationToken cancellationToken)
    {
        if (!GeminiModelCatalog.Contains(modelId))
        {
            throw new ArgumentException("The selected Gemini model is not supported.", nameof(modelId));
        }

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length > 4096)
        {
            throw new ArgumentException("A user-provided Gemini API key is required.", nameof(apiKey));
        }

        await _secretStore.StoreAsync(ApiKeyName, apiKey.Trim(), cancellationToken);
        return new GeminiConfiguration(modelId, true);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken)
    {
        await _secretStore.DeleteAsync(ApiKeyName, cancellationToken);
    }
}
