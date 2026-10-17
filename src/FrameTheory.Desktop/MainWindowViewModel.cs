using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FrameTheory.Desktop;

public sealed record WorkspaceViewModel(string Name, string Description, string OfflineMessage);

public interface IWorkspaceCatalog
{
    IReadOnlyList<WorkspaceViewModel> GetWorkspaces();
}

public sealed class WorkspaceCatalog : IWorkspaceCatalog
{
    public IReadOnlyList<WorkspaceViewModel> GetWorkspaces() =>
    [
        new("Video", "Frame-accurate editing, playback and export.", "Manual editing remains available offline."),
        new("Photo", "Non-destructive raster editing and image export.", "Local media stays on this device."),
        new("Color", "Shared cinematic grades for images and video.", "Color operations do not require Gemini."),
        new("AI", "Optional Gemini planning with review before apply.", "No key or network is required for manual editing.")
    ];
}

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private int _selectedWorkspaceIndex;

    public MainWindowViewModel(IWorkspaceCatalog workspaceCatalog)
    {
        Workspaces = workspaceCatalog.GetWorkspaces();
        if (Workspaces.Count == 0)
        {
            throw new InvalidOperationException("At least one workspace is required.");
        }
    }

    public IReadOnlyList<WorkspaceViewModel> Workspaces { get; }

    public int SelectedWorkspaceIndex
    {
        get => _selectedWorkspaceIndex;
        set
        {
            if (value < 0 || value >= Workspaces.Count || value == _selectedWorkspaceIndex)
            {
                return;
            }

            _selectedWorkspaceIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedWorkspace));
        }
    }

    public WorkspaceViewModel SelectedWorkspace => Workspaces[SelectedWorkspaceIndex];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DesktopServices
{
    public MainWindowViewModel CreateMainWindowViewModel() =>
        new(new WorkspaceCatalog());
}
