using System.ComponentModel;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using FrameTheory.Application;
using FrameTheory.Domain;
using FrameTheory.Media;
using FrameTheory.Photo;
using FrameTheory.Storage;

namespace FrameTheory.Desktop;

public sealed class PhotoEditorViewModel : INotifyPropertyChanged
{
    private readonly PhotoImporter _importer;
    private readonly PhotoExporter _exporter;
    private readonly ExportQueueService? _exportQueue;
    private readonly IProjectRepository? _projectRepository;
    private PhotoEditHistory? _history;
    private string _statusText = "Open a local image to begin a non-destructive edit.";

    public PhotoEditorViewModel(
        PhotoImporter importer,
        PhotoExporter exporter,
        ExportQueueService? exportQueue = null,
        IProjectRepository? projectRepository = null)
    {
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _exportQueue = exportQueue;
        _projectRepository = projectRepository;
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

    public async Task<bool> SaveProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        if (_projectRepository is null)
        {
            StatusText = "Photo project persistence is unavailable in this host.";
            return false;
        }

        if (Current is null)
        {
            StatusText = "Open a photo before saving a project.";
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(projectPath);
            var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A project directory is required.", nameof(projectPath));
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            var sourcePath = GetSourcePath(Current.Source.SourceUri);
            if ((File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Symlinked photo sources are not accepted.");
            }

            var relativePath = Path.GetRelativePath(directory, sourcePath);
            await using var stream = File.OpenRead(sourcePath);
            var fingerprint = $"sha256:{Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant()}";
            var snapshot = new ProjectSnapshot(
                "photo-project",
                new RationalFrameRate(1, 1),
                Math.Max(1, Current.Source.Metadata.Width),
                Math.Max(1, Current.Source.Metadata.Height),
                "sRGB",
                [new MediaReference(Current.Source.Metadata.Id, relativePath, fingerprint)],
                photoStateJson: PhotoProjectStateJson.Serialize(Current));
            await _projectRepository.SaveAtomicAsync(fullPath, snapshot, cancellationToken);
            StatusText = $"Saved photo project {Path.GetFileName(fullPath)}.";
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Photo project save canceled; the previous file is unchanged.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            StatusText = $"Photo project could not be saved: {exception.Message}";
            return false;
        }
    }

    public async Task<bool> OpenProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        if (_projectRepository is null)
        {
            StatusText = "Photo project persistence is unavailable in this host.";
            return false;
        }

        try
        {
            var fullPath = Path.GetFullPath(projectPath);
            var snapshot = await _projectRepository.LoadAsync(fullPath, cancellationToken);
            if (snapshot.MediaReferences.Count != 1)
            {
                throw new InvalidDataException("A photo project must contain exactly one photo source.");
            }

            var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A project directory is required.", nameof(projectPath));
            var reference = snapshot.MediaReferences[0];
            var sourcePath = Path.GetFullPath(Path.Combine(directory, reference.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Saved photo is missing: {reference.RelativePath}", sourcePath);
            }

            await VerifySourceFingerprintAsync(sourcePath, reference.ContentFingerprint, cancellationToken);
            var document = await _importer.ImportAsync(new Uri(sourcePath), cancellationToken);
            var state = snapshot.PhotoStateJson is null
                ? new PhotoEditState(document)
                : PhotoProjectStateJson.Deserialize(snapshot.PhotoStateJson, document);
            _history = new PhotoEditHistory(state);
            StatusText = $"Opened photo project {Path.GetFileName(fullPath)}.";
            NotifyDocumentChanged();
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Photo project open canceled; the current photo remains loaded.";
            return false;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or MediaImportException or MediaProbeException or MediaDecodeException or UnauthorizedAccessException)
        {
            StatusText = $"Photo project could not be opened: {exception.Message}";
            return false;
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

    public async Task ExportAsync(
        string destinationPath,
        PhotoExportFormat format,
        int quality,
        CancellationToken cancellationToken,
        SocialPhotoExportPreset? preset = null)
    {
        if (Current is null)
        {
            StatusText = "Open a photo before exporting.";
            return;
        }

        try
        {
            PhotoExportReceipt? receipt;
            if (_exportQueue is null)
            {
                receipt = await _exporter.ExportAsync(Current, destinationPath, new PhotoExportOptions(format, quality, preset), null, cancellationToken);
            }
            else
            {
                receipt = null;
                var job = _exportQueue.Enqueue(async queueCancellation =>
                {
                    var result = await _exporter.ExportAsync(Current, destinationPath, new PhotoExportOptions(format, quality, preset), null, queueCancellation);
                    receipt = result;
                    return new ExportReceipt(result.JobId, result.OutputPath, result.OutputBytes, result.VerifiedMetadata);
                });
                using var cancellationRegistration = cancellationToken.Register(job.Cancel);
                await job.Completion.WaitAsync(cancellationToken);
            }

            if (receipt is null)
            {
                throw new PhotoExportException("The queued photo export completed without a receipt.");
            }

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

    private static string GetSourcePath(Uri source)
    {
        if (!source.IsFile)
        {
            throw new InvalidDataException("Only local photo sources can be saved in a project.");
        }

        var path = Path.GetFullPath(source.LocalPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The photo source was not found.", path);
        }

        return path;
    }

    private static async Task VerifySourceFingerprintAsync(string sourcePath, string expectedFingerprint, CancellationToken cancellationToken)
    {
        if (!expectedFingerprint.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var stream = File.OpenRead(sourcePath);
        var actualFingerprint = $"sha256:{Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant()}";
        if (!string.Equals(actualFingerprint, expectedFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The saved photo source fingerprint does not match the project.");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
