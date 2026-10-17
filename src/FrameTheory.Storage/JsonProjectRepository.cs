using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrameTheory.Application;
using FrameTheory.Domain;

namespace FrameTheory.Storage;

public sealed class JsonProjectRepository : IProjectRepository
{
    private const long MaximumProjectBytes = 10 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task SaveAtomicAsync(string path, ProjectSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("A project directory is required.", nameof(path));
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(directory);
        }

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            }))
            {
                await JsonSerializer.SerializeAsync(stream, ProjectFile.FromSnapshot(snapshot), JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    public async Task<ProjectSnapshot> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = GetFullPath(path);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("The project file was not found.", fullPath);
        }

        if (fileInfo.Length > MaximumProjectBytes)
        {
            throw new InvalidDataException("The project file exceeds the supported size limit.");
        }

        await using var stream = new FileStream(fullPath, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        var projectFile = await JsonSerializer.DeserializeAsync<ProjectFile>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The project file is empty.");
        return projectFile.ToSnapshot();
    }

    private static string GetFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A project path is required.", nameof(path));
        }

        return Path.GetFullPath(path);
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

    private sealed class ProjectFile
    {
        public int SchemaVersion { get; set; }

        public string? ProjectId { get; set; }

        public int FrameRateNumerator { get; set; }

        public int FrameRateDenominator { get; set; }

        public string? FrameRate { get; set; }

        public int CanvasWidth { get; set; }

        public int CanvasHeight { get; set; }

        public string? WorkingColorSpace { get; set; }

        public List<MediaFile>? Media { get; set; }

        public static ProjectFile FromSnapshot(ProjectSnapshot snapshot) => new()
        {
            SchemaVersion = ProjectSnapshot.CurrentSchemaVersion,
            ProjectId = snapshot.ProjectId,
            FrameRateNumerator = snapshot.FrameRate.Numerator,
            FrameRateDenominator = snapshot.FrameRate.Denominator,
            CanvasWidth = snapshot.CanvasWidth,
            CanvasHeight = snapshot.CanvasHeight,
            WorkingColorSpace = snapshot.WorkingColorSpace,
            Media = snapshot.MediaReferences.Select(MediaFile.FromReference).ToList()
        };

        public ProjectSnapshot ToSnapshot()
        {
            Migrate();
            if (SchemaVersion != ProjectSnapshot.CurrentSchemaVersion)
            {
                throw new InvalidDataException($"Unsupported project schema version {SchemaVersion}.");
            }

            try
            {
                var mediaReferences = (Media ?? []).Select(media => media.ToReference()).ToArray();
                return new ProjectSnapshot(
                    ProjectId ?? throw new InvalidDataException("Project ID is required."),
                    new RationalFrameRate(FrameRateNumerator, FrameRateDenominator),
                    CanvasWidth,
                    CanvasHeight,
                    WorkingColorSpace ?? throw new InvalidDataException("Working color space is required."),
                    mediaReferences);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Project values are invalid.", exception);
            }

        }

        private void Migrate()
        {
            if (SchemaVersion == ProjectSnapshot.CurrentSchemaVersion)
            {
                return;
            }

            if (SchemaVersion != 0 || string.IsNullOrWhiteSpace(FrameRate))
            {
                throw new InvalidDataException($"Unsupported project schema version {SchemaVersion}.");
            }

            var parts = FrameRate.Split('/', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var numerator) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var denominator))
            {
                throw new InvalidDataException("The legacy frame rate is invalid.");
            }

            FrameRateNumerator = numerator;
            FrameRateDenominator = denominator;
            SchemaVersion = ProjectSnapshot.CurrentSchemaVersion;
        }
    }

    private sealed class MediaFile
    {
        public string? Id { get; set; }

        public string? RelativePath { get; set; }

        public string? ContentFingerprint { get; set; }

        public static MediaFile FromReference(MediaReference reference) => new()
        {
            Id = reference.Id.Value,
            RelativePath = reference.RelativePath,
            ContentFingerprint = reference.ContentFingerprint
        };

        public MediaReference ToReference() => new(
            MediaId.Parse(Id ?? throw new InvalidDataException("Media ID is required.")),
            RelativePath ?? throw new InvalidDataException("Media path is required."),
            ContentFingerprint ?? throw new InvalidDataException("Media fingerprint is required."));
    }
}
