namespace FrameTheory.Application;

public enum CapabilityState
{
    Available,
    Missing,
    Unknown
}

public sealed record CapabilityStatus(string Name, CapabilityState State, string Detail);

public sealed record CapabilityReport
{
    public CapabilityReport(string platform, string architecture, IReadOnlyList<CapabilityStatus> entries)
    {
        Platform = platform;
        Architecture = architecture;
        Entries = Array.AsReadOnly((entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
    }

    public string Platform { get; }

    public string Architecture { get; }

    public IReadOnlyList<CapabilityStatus> Entries { get; }

    public string Summary =>
        $"{Platform} {Architecture} · " +
        string.Join(" · ", Entries.Where(entry => entry.Name is "FFmpeg" or "ffprobe" or "Storage")
            .Select(entry => Format(entry)));

    private static string Format(CapabilityStatus entry)
    {
        if (entry.State != CapabilityState.Missing)
        {
            return entry.State == CapabilityState.Unknown && !string.IsNullOrWhiteSpace(entry.Detail)
                ? $"{entry.Name}: unknown ({entry.Detail})"
                : $"{entry.Name}: {entry.State.ToString().ToLowerInvariant()}";
        }

        var guidance = entry.Name is "FFmpeg" or "ffprobe"
            ? "install FFmpeg and add it to PATH"
            : "check writable local storage";
        return $"{entry.Name}: missing ({guidance})";
    }
}

public interface ICapabilityDetector
{
    Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken);
}
