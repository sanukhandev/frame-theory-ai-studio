namespace FrameTheory.Domain;

public sealed record TimelineStabilizationSettings
{
    public TimelineStabilizationSettings(int searchRadius = 8)
    {
        if (searchRadius is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(searchRadius), "The stabilization search radius must be between 1 and 64.");
        }

        SearchRadius = searchRadius;
    }

    public int SearchRadius { get; }
}
