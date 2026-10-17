namespace FrameTheory.Application;

public interface ISecretStore
{
    Task StoreAsync(string name, string secret, CancellationToken cancellationToken = default);

    Task<string?> ReadAsync(string name, CancellationToken cancellationToken = default);

    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
}
