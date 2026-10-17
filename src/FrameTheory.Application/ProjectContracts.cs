using FrameTheory.Domain;

namespace FrameTheory.Application;

public interface IProjectRepository
{
    Task<ProjectSnapshot> LoadAsync(string path, CancellationToken cancellationToken);

    Task SaveAtomicAsync(string path, ProjectSnapshot snapshot, CancellationToken cancellationToken);
}
