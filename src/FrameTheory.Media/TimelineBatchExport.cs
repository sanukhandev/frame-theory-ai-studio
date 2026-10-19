namespace FrameTheory.Media;

public sealed record TimelineBatchExportTarget
{
    public TimelineBatchExportTarget(
        string name,
        TimelineVideoRenderPlan plan,
        string destinationPath,
        ExportPreset preset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.Width != plan.Width || preset.Height != plan.Height || preset.FrameRate != plan.FrameRate)
        {
            throw new ArgumentException("The batch target preset must match its render plan.", nameof(preset));
        }

        Name = name;
        Plan = plan;
        DestinationPath = Path.GetFullPath(destinationPath);
        Preset = preset;
    }

    public string Name { get; }

    public TimelineVideoRenderPlan Plan { get; }

    public string DestinationPath { get; }

    public ExportPreset Preset { get; }
}

public sealed record TimelineBatchExportProgress(
    string TargetName,
    int CompletedTargets,
    int TotalTargets,
    ExportProgress Progress);

public sealed class TimelineBatchExporter
{
    private readonly ITimelineVideoExporter _exporter;

    public TimelineBatchExporter(ITimelineVideoExporter exporter) => _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));

    public async Task<IReadOnlyList<ExportReceipt>> ExportAsync(
        IReadOnlyList<TimelineBatchExportTarget> targets,
        IProgress<TimelineBatchExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targets);
        if (targets.Count == 0)
        {
            throw new ArgumentException("At least one batch export target is required.", nameof(targets));
        }

        var targetSnapshot = targets.ToArray();
        var paths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var target in targetSnapshot)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (!paths.Add(target.DestinationPath))
            {
                throw new ArgumentException("Batch export destinations must be unique.", nameof(targets));
            }
        }

        var receipts = new List<ExportReceipt>(targetSnapshot.Length);
        for (var index = 0; index < targetSnapshot.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = targetSnapshot[index];
            var completed = index;
            var targetProgress = new InlineProgress<ExportProgress>(update =>
                progress?.Report(new TimelineBatchExportProgress(target.Name, completed, targetSnapshot.Length, update)));
            receipts.Add(await _exporter.ExportAsync(target.Plan, target.DestinationPath, target.Preset, targetProgress, cancellationToken).ConfigureAwait(false));
            progress?.Report(new TimelineBatchExportProgress(
                target.Name,
                index + 1,
                targetSnapshot.Length,
                new ExportProgress("Completed", 1)));
        }

        return receipts;
    }

    private sealed class InlineProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }
}
