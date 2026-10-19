using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.Application;
using FrameTheory.Domain;
using FrameTheory.Storage;

namespace FrameTheory.Desktop;

public sealed record MissingMediaViewModel(MediaId Id, string RelativePath)
{
    public override string ToString() => RelativePath;
}

public sealed class PhotoRecoveryViewModel : INotifyPropertyChanged
{
    private readonly ProjectRecoveryService _recovery;
    private readonly IProjectRepository _repository;
    private ProjectSnapshot? _snapshot;
    private string _projectPath = string.Empty;
    private string _statusText = "Open a project to check photo assets or recover its autosave.";
    private MissingMediaViewModel? _selectedMissingMedia;

    public PhotoRecoveryViewModel(ProjectRecoveryService recovery, IProjectRepository repository)
    {
        _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public string ProjectPath
    {
        get => _projectPath;
        set
        {
            if (value == _projectPath)
            {
                return;
            }

            _projectPath = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (value == _statusText)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<MissingMediaViewModel> MissingMedia { get; private set; } = [];

    public MissingMediaViewModel? SelectedMissingMedia
    {
        get => _selectedMissingMedia;
        set
        {
            if (value == _selectedMissingMedia)
            {
                return;
            }

            _selectedMissingMedia = value;
            OnPropertyChanged();
        }
    }

    public async Task OpenProjectAsync(CancellationToken cancellationToken)
    {
        try
        {
            var projectPath = Path.GetFullPath(ProjectPath);
            var autosave = await _recovery.TryLoadAutosaveAsync(projectPath, cancellationToken);
            _snapshot = autosave ?? await _repository.LoadAsync(projectPath, cancellationToken);
            ProjectPath = projectPath;
            RefreshMissing(projectPath, _snapshot);
            StatusText = autosave is null
                ? $"Opened project; {MissingMedia.Count} missing media file(s)."
                : $"Recovered autosave; {MissingMedia.Count} missing media file(s).";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Project recovery canceled.";
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            StatusText = $"Project could not be opened: {exception.Message}";
        }
    }

    public async Task RelinkSelectedAsync(string replacementPath, CancellationToken cancellationToken)
    {
        if (_snapshot is null || SelectedMissingMedia is null)
        {
            StatusText = "Open a project and select a missing media file first.";
            return;
        }

        try
        {
            var projectPath = Path.GetFullPath(ProjectPath);
            var relinked = await _recovery.RelinkMediaAsync(projectPath, _snapshot, SelectedMissingMedia.Id, replacementPath, cancellationToken);
            await _repository.SaveAtomicAsync(projectPath, relinked, cancellationToken);
            _snapshot = relinked;
            RefreshMissing(projectPath, relinked);
            StatusText = $"Relinked media; {MissingMedia.Count} missing media file(s) remain.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Media relink canceled.";
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            StatusText = $"Media could not be relinked: {exception.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RefreshMissing(string projectPath, ProjectSnapshot snapshot)
    {
        MissingMedia = Array.AsReadOnly(_recovery.FindMissingMedia(projectPath, snapshot)
            .Select(reference => new MissingMediaViewModel(reference.Id, reference.RelativePath))
            .ToArray());
        SelectedMissingMedia = MissingMedia.FirstOrDefault();
        OnPropertyChanged(nameof(MissingMedia));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
