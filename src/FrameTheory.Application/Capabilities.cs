namespace FrameTheory.Application;

public enum CapabilityState
{
    Available,
    Missing,
    Unknown
}

public sealed record CapabilityStatus(string Name, CapabilityState State, string Detail);

public sealed record CapabilityReport(string Platform, string Architecture, IReadOnlyList<CapabilityStatus> Entries)
{
    public string Summary =>
        $"{Platform} {Architecture} · " +
        string.Join(" · ", Entries.Where(entry => entry.Name is "FFmpeg" or "ffprobe" or "Storage")
            .Select(entry => Format(entry)));

    private static string Format(CapabilityStatus entry)
    {
        if (entry.State != CapabilityState.Missing)
        {
            return $"{entry.Name}: {entry.State.ToString().ToLowerInvariant()}";
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
