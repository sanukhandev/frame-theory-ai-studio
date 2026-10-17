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
            .Select(entry => $"{entry.Name}: {entry.State.ToString().ToLowerInvariant()}"));
}

public interface ICapabilityDetector
{
    Task<CapabilityReport> DetectAsync(CancellationToken cancellationToken);
}
