namespace FrameTheory.Domain;

public sealed record TimelineSpeedPoint
{
    public TimelineSpeedPoint(long frame, double speed)
    {
        if (frame < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frame));
        }

        if (double.IsNaN(speed) || double.IsInfinity(speed) || speed is < 0.25 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be between 0.25x and 4x.");
        }

        Frame = frame;
        Speed = speed;
    }

    public long Frame { get; }

    public double Speed { get; }
}

public sealed class TimelineSpeedRamp
{
    public TimelineSpeedRamp(IEnumerable<TimelineSpeedPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var orderedPoints = points.ToArray();
        if (orderedPoints.Length < 2 || orderedPoints.Zip(orderedPoints.Skip(1)).Any(pair => pair.First.Frame >= pair.Second.Frame))
        {
            throw new ArgumentException("A speed ramp requires at least two strictly increasing points.", nameof(points));
        }

        Points = Array.AsReadOnly(orderedPoints);
    }

    public IReadOnlyList<TimelineSpeedPoint> Points { get; }

    public double SpeedAt(long frame)
    {
        if (frame <= Points[0].Frame)
        {
            return Points[0].Speed;
        }

        for (var index = 1; index < Points.Count; index++)
        {
            var right = Points[index];
            if (frame <= right.Frame)
            {
                var left = Points[index - 1];
                var fraction = (double)(frame - left.Frame) / (right.Frame - left.Frame);
                return left.Speed + (right.Speed - left.Speed) * fraction;
            }
        }

        return Points[^1].Speed;
    }
}
