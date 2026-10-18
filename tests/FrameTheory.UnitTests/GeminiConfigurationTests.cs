using FrameTheory.AI;
using FrameTheory.Application;
using FrameTheory.Platform;

namespace FrameTheory.UnitTests;

public sealed class GeminiConfigurationTests
{
    [Xunit.Fact]
    public async Task ConfigurationLoadsOfflineWithoutAKeyAndStoresOnlyThroughSecretStore()
    {
        var secrets = new InMemorySecretStore();
        var configuration = new GeminiConfigurationService(secrets);

        var offline = await configuration.LoadAsync(CancellationToken.None);
        var saved = await configuration.SaveAsync(" gemini-2.5-pro ", "test-key-value", CancellationToken.None);
        var loaded = await configuration.LoadAsync(CancellationToken.None);
        var storedSecret = secrets.Values[GeminiConfigurationService.ApiKeyName];
        await configuration.DeleteAsync(CancellationToken.None);

        Xunit.Assert.False(offline.HasApiKey);
        Xunit.Assert.Equal("gemini-2.5-pro", saved.ModelId);
        Xunit.Assert.True(loaded.HasApiKey);
        Xunit.Assert.Equal("test-key-value", storedSecret);
        Xunit.Assert.False((await configuration.LoadAsync(CancellationToken.None)).HasApiKey);
    }

    [Xunit.Fact]
    public async Task ConfigurationRejectsUnknownModelsAndBlankKeys()
    {
        var configuration = new GeminiConfigurationService(new InMemorySecretStore());

        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => configuration.SaveAsync("unknown-model", "key", CancellationToken.None));
        await Xunit.Assert.ThrowsAsync<ArgumentException>(() => configuration.SaveAsync("gemini-2.5-flash", " ", CancellationToken.None));
    }

    [Xunit.Fact]
    public async Task WindowsCredentialManagerRoundTripsWithoutExposingTheSecretInTheApplication()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var name = $"test-{Guid.NewGuid():N}";
        var store = new OperatingSystemSecretStore();
        try
        {
            await store.StoreAsync(name, "roundtrip-secret", CancellationToken.None);
            Xunit.Assert.Equal("roundtrip-secret", await store.ReadAsync(name, CancellationToken.None));
        }
        finally
        {
            await store.DeleteAsync(name, CancellationToken.None);
        }
    }

    private sealed class InMemorySecretStore : ISecretStore
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

        public Task StoreAsync(string name, string secret, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Values[name] = secret;
            return Task.CompletedTask;
        }

        public Task<string?> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Values.TryGetValue(name, out var secret) ? secret : null);
        }

        public Task DeleteAsync(string name, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Values.Remove(name);
            return Task.CompletedTask;
        }
    }
}
