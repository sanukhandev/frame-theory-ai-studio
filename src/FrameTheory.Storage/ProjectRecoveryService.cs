using FrameTheory.Domain;

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
        if (!File.Exists(autosavePath))
        {
            return null;
        }

        return await _repository.LoadAsync(autosavePath, cancellationToken);
    }

    public void ClearAutosave(string projectPath)
    {
        var autosavePath = GetAutosavePath(projectPath);
        if (File.Exists(autosavePath))
        {
            File.Delete(autosavePath);
        }
    }

    public IReadOnlyList<MediaReference> FindMissingMedia(string projectPath, ProjectSnapshot snapshot)
    {
        var projectDirectory = GetProjectDirectory(projectPath);
        return snapshot.MediaReferences
            .Where(reference => !File.Exists(ResolveRelativePath(projectDirectory, reference.RelativePath)))
            .ToArray();
    }

    public ProjectSnapshot RelinkMedia(string projectPath, ProjectSnapshot snapshot, MediaId mediaId, string replacementPath)
    {
        var projectDirectory = GetProjectDirectory(projectPath);
        var replacementFullPath = Path.GetFullPath(replacementPath);
        if (!File.Exists(replacementFullPath))
        {
            throw new FileNotFoundException("The replacement media file was not found.", replacementFullPath);
        }

        // shortcut: symlink containment checks remain in FT-105; upgrade before accepting untrusted relink roots.
        var relativePath = Path.GetRelativePath(projectDirectory, replacementFullPath);
        var target = snapshot.MediaReferences.SingleOrDefault(reference => reference.Id == mediaId)
            ?? throw new InvalidOperationException("The media reference was not found.");
        var updated = snapshot.MediaReferences.Select(reference => reference.Id == mediaId
            ? new MediaReference(reference.Id, relativePath, reference.ContentFingerprint)
            : reference);
        return snapshot.WithMediaReferences(updated);
    }

    public static string GetAutosavePath(string projectPath) =>
        Path.GetFullPath(projectPath) + ".ftstudio.autosave";

    private static string GetProjectDirectory(string projectPath) =>
        Path.GetDirectoryName(Path.GetFullPath(projectPath))
        ?? throw new ArgumentException("A project directory is required.", nameof(projectPath));

    private static string ResolveRelativePath(string projectDirectory, string relativePath) =>
        Path.GetFullPath(Path.Combine(projectDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
