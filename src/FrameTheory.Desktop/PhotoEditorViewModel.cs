using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Photo;

namespace FrameTheory.Desktop;

public sealed class PhotoEditorViewModel : INotifyPropertyChanged
{
    private readonly PhotoImporter _importer;
    private readonly PhotoExporter _exporter;
    private PhotoEditHistory? _history;
    private string _statusText = "Open a local image to begin a non-destructive edit.";

    public PhotoEditorViewModel(PhotoImporter importer, PhotoExporter exporter)
    {
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PhotoEditState? Current => _history?.Current;

    public byte[]? PreviewPpm => Current?.RenderPreview();

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

    public async Task OpenAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var document = await _importer.ImportAsync(new Uri(fullPath), cancellationToken);
            _history = new PhotoEditHistory(new PhotoEditState(document));
            StatusText = $"Opened {Path.GetFileName(fullPath)}; non-destructive history is active.";
            NotifyDocumentChanged();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Photo import canceled.";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or MediaImportException or MediaProbeException or MediaDecodeException)
        {
            StatusText = $"Photo import failed: {exception.Message}";
        }
    }

    public void RotateRight()
    {
        if (Current is null)
        {
            StatusText = "Open a photo before editing.";
            return;
        }

        _history!.Execute(Current.Rotate(90));
        StatusText = "Applied a non-destructive 90° rotation.";
        NotifyDocumentChanged();
    }

    public void Undo()
    {
        if (_history?.Undo() != true)
        {
            StatusText = "No photo edit is available to undo.";
            return;
        }

        StatusText = "Undid the last photo edit.";
        NotifyDocumentChanged();
    }

    public async Task ExportAsync(string destinationPath, PhotoExportFormat format, int quality, CancellationToken cancellationToken)
    {
        if (Current is null)
        {
            StatusText = "Open a photo before exporting.";
            return;
        }

        try
        {
            var receipt = await _exporter.ExportAsync(
                Current,
                destinationPath,
                new PhotoExportOptions(format, quality),
                null,
                cancellationToken);
            StatusText = $"Exported {Path.GetFileName(receipt.OutputPath)} ({receipt.OutputBytes} bytes).";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Photo export canceled.";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or PhotoExportException or MediaProbeException)
        {
            StatusText = $"Photo export failed: {exception.Message}";
        }
    }

    private void NotifyDocumentChanged()
    {
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(PreviewPpm));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
