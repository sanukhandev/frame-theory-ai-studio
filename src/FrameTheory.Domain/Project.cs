namespace FrameTheory.Domain;

public sealed record MediaReference
{
    public MediaReference(MediaId id, string relativePath, string contentFingerprint)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("A relative media path is required.", nameof(relativePath));
        }

        var normalizedPath = relativePath.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(relativePath) || normalizedPath.StartsWith('/') ||
            normalizedPath == ".." || normalizedPath.StartsWith("../", StringComparison.Ordinal) ||
            normalizedPath.Contains("/../", StringComparison.Ordinal))
        {
            throw new ArgumentException("Media paths must remain relative to the project.", nameof(relativePath));
        }

        if (string.IsNullOrWhiteSpace(contentFingerprint))
        {
            throw new ArgumentException("A content fingerprint is required.", nameof(contentFingerprint));
        }

        Id = id;
        RelativePath = normalizedPath;
        ContentFingerprint = contentFingerprint.Trim();
    }

    public MediaId Id { get; }

    public string RelativePath { get; }

    public string ContentFingerprint { get; }
}

public sealed record ProjectSnapshot
{
    public const int CurrentSchemaVersion = 1;

    public ProjectSnapshot(
        string projectId,
        RationalFrameRate frameRate,
        int canvasWidth,
        int canvasHeight,
        string workingColorSpace,
        IEnumerable<MediaReference>? mediaReferences = null)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("A project ID is required.", nameof(projectId));
        }

        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(canvasWidth), "Canvas dimensions must be positive.");
        }

        if (string.IsNullOrWhiteSpace(workingColorSpace))
        {
            throw new ArgumentException("A working color space is required.", nameof(workingColorSpace));
        }

        ProjectId = projectId.Trim();
        FrameRate = frameRate;
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
        WorkingColorSpace = workingColorSpace.Trim();
        MediaReferences = mediaReferences?.ToArray() ?? [];
    }

    public int SchemaVersion => CurrentSchemaVersion;

    public string ProjectId { get; }

    public RationalFrameRate FrameRate { get; }

    public int CanvasWidth { get; }

    public int CanvasHeight { get; }

    public string WorkingColorSpace { get; }

    public IReadOnlyList<MediaReference> MediaReferences { get; }

    public ProjectSnapshot WithCanvas(int canvasWidth, int canvasHeight) =>
        new(ProjectId, FrameRate, canvasWidth, canvasHeight, WorkingColorSpace, MediaReferences);

    public ProjectSnapshot WithMediaReferences(IEnumerable<MediaReference> mediaReferences) =>
        new(ProjectId, FrameRate, CanvasWidth, CanvasHeight, WorkingColorSpace, mediaReferences);
}
