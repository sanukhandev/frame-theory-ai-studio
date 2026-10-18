using System.Text.Json;

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
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new WorkspaceState(index)));
            File.Move(temporaryPath, _path, true);
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
