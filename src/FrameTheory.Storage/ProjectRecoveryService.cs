using FrameTheory.Domain;
using System.Security.Cryptography;

namespace FrameTheory.Storage;

public sealed class ProjectRecoveryService
{
    private readonly JsonProjectRepository _repository;

    public ProjectRecoveryService(JsonProjectRepository repository) => _repository = repository;

    public async Task SaveAutosaveAsync(string projectPath, ProjectSnapshot snapshot, CancellationToken cancellationToken)
    {
        await _repository.SaveAtomicAsync(GetAutosavePath(projectPath), snapshot, cancellationToken);
    }

    public async Task<ProjectSnapshot?> TryLoadAutosaveAsync(string projectPath, CancellationToken cancellationToken)
    {
        var autosavePath = GetAutosavePath(projectPath);
        if (LocalPathSecurity.ContainsReparsePoint(autosavePath))
        {
            return null;
        }

        if (!File.Exists(autosavePath))
        {
            return null;
        }

        return await _repository.LoadAsync(autosavePath, cancellationToken);
    }

    public void ClearAutosave(string projectPath)
    {
        var autosavePath = GetAutosavePath(projectPath);
        if (LocalPathSecurity.ContainsReparsePoint(autosavePath))
        {
            return;
        }

        if (File.Exists(autosavePath))
        {
            File.Delete(autosavePath);
        }
    }

    public IReadOnlyList<MediaReference> FindMissingMedia(string projectPath, ProjectSnapshot snapshot)
    {
        var projectDirectory = GetProjectDirectory(projectPath);
        return snapshot.MediaReferences
            .Where(reference => !File.Exists(ResolveContainedPath(projectDirectory, reference.RelativePath)))
            .ToArray();
    }

    public async Task<ProjectSnapshot> RelinkMediaAsync(
        string projectPath,
        ProjectSnapshot snapshot,
        MediaId mediaId,
        string replacementPath,
        CancellationToken cancellationToken)
    {
        var projectDirectory = GetProjectDirectory(projectPath);
        var replacementFullPath = Path.GetFullPath(replacementPath);
        if (LocalPathSecurity.ContainsReparsePoint(replacementFullPath))
        {
            throw new InvalidDataException("The replacement media path must not be a reparse point.");
        }

        if (!File.Exists(replacementFullPath))
        {
            throw new FileNotFoundException("The replacement media file was not found.", replacementFullPath);
        }

        var containedReplacementPath = ResolveContainedPath(projectDirectory, Path.GetRelativePath(projectDirectory, replacementFullPath));
        var relativePath = Path.GetRelativePath(projectDirectory, containedReplacementPath);
        var replacementFingerprint = await ComputeFingerprintAsync(containedReplacementPath, cancellationToken);
        var target = snapshot.MediaReferences.SingleOrDefault(reference => reference.Id == mediaId)
            ?? throw new InvalidOperationException("The media reference was not found.");
        var updated = snapshot.MediaReferences.Select(reference => reference.Id == mediaId
            ? new MediaReference(reference.Id, relativePath, replacementFingerprint)
            : reference);
        return snapshot.WithMediaReferences(updated);
    }

    public static string GetAutosavePath(string projectPath) =>
        Path.GetFullPath(projectPath) + ".ftstudio.autosave";

    private static string GetProjectDirectory(string projectPath) =>
        Path.GetDirectoryName(Path.GetFullPath(projectPath))
        ?? throw new ArgumentException("A project directory is required.", nameof(projectPath));

    public static string ResolveContainedPath(string projectDirectory, string relativePath)
    {
        var projectRoot = Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(projectDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A media path escapes the project directory.");
        }

        EnsureNoReparsePoint(projectDirectory, candidate);
        return candidate;
    }

    private static void EnsureNoReparsePoint(string projectDirectory, string candidate)
    {
        if (LocalPathSecurity.ContainsReparsePoint(projectDirectory))
        {
            throw new InvalidDataException("The project directory must not be a reparse point.");
        }

        if (LocalPathSecurity.ContainsReparsePoint(candidate))
        {
            throw new InvalidDataException("A media path uses a reparse-point directory or file.");
        }
    }

    private static async Task<string> ComputeFingerprintAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
