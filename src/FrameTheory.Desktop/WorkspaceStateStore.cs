using System.Text.Json;
using FrameTheory.Domain;

namespace FrameTheory.Desktop;

public interface IWorkspaceStateStore
{
    int LoadSelectedWorkspaceIndex();

    void SaveSelectedWorkspaceIndex(int index);
}

public sealed class FileWorkspaceStateStore : IWorkspaceStateStore
{
    private readonly string _path;

    public FileWorkspaceStateStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FrameTheoryStudio",
            "workspace-state.json");
    }

    public int LoadSelectedWorkspaceIndex()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return 0;
            }

            if (LocalPathSecurity.ContainsReparsePoint(_path))
            {
                return 0;
            }

            var state = JsonSerializer.Deserialize<WorkspaceState>(File.ReadAllText(_path));
            return state?.SelectedWorkspaceIndex ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    public void SaveSelectedWorkspaceIndex(int index)
    {
        var directory = Path.GetDirectoryName(_path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(directory);
            if (LocalPathSecurity.ContainsReparsePoint(directory) || (File.Exists(_path) && LocalPathSecurity.ContainsReparsePoint(_path)))
            {
                return;
            }

            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                using (var stream = new FileStream(temporaryPath, new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.CreateNew,
                    Share = FileShare.None,
                    Options = FileOptions.WriteThrough
                }))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonSerializer.Serialize(new WorkspaceState(index)));
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temporaryPath, _path, true);
            }
            finally
            {
                TryDelete(temporaryPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record WorkspaceState(int SelectedWorkspaceIndex);
}

internal sealed class NoopWorkspaceStateStore : IWorkspaceStateStore
{
    public int LoadSelectedWorkspaceIndex() => 0;

    public void SaveSelectedWorkspaceIndex(int index)
    {
    }
}
