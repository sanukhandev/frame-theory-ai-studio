namespace FrameTheory.Media;

public sealed record MotionFrame
{
    public MotionFrame(int frameIndex, int width, int height, ReadOnlyMemory<byte> rgbBytes)
    {
        if (frameIndex < 0 || width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        if (rgbBytes.Length != checked(width * height * 3))
        {
            throw new ArgumentException("Motion frames require packed RGB bytes.", nameof(rgbBytes));
        }

        FrameIndex = frameIndex;
        Width = width;
        Height = height;
        RgbBytes = rgbBytes.ToArray();
    }

    public int FrameIndex { get; }

    public int Width { get; }

    public int Height { get; }

    public byte[] RgbBytes { get; }
}

public sealed record MotionVector
{
    public MotionVector(int frameIndex, int offsetX, int offsetY, float confidence)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        if (float.IsNaN(confidence) || float.IsInfinity(confidence) || confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence));
        }

        FrameIndex = frameIndex;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Confidence = confidence;
    }

    public int FrameIndex { get; }

    public int OffsetX { get; }

    public int OffsetY { get; }

    public float Confidence { get; }
}

public sealed record StabilizationTransform(int FrameIndex, int OffsetX, int OffsetY);

public sealed class StabilizationPlan
{
    public StabilizationPlan(IReadOnlyList<StabilizationTransform> transforms)
    {
        ArgumentNullException.ThrowIfNull(transforms);
        Transforms = Array.AsReadOnly(transforms.ToArray());
    }

    public IReadOnlyList<StabilizationTransform> Transforms { get; }
}

public sealed class TranslationMotionTracker
{
    private readonly int _searchRadius;
    private readonly int _sampleStride;

    public TranslationMotionTracker(int searchRadius = 8, int sampleStride = 4)
    {
        if (searchRadius is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(searchRadius));
        }

        if (sampleStride is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleStride));
        }

        _searchRadius = searchRadius;
        _sampleStride = sampleStride;
    }

    public IReadOnlyList<MotionVector> Track(IReadOnlyList<MotionFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count < 2)
        {
            throw new ArgumentException("Motion tracking requires at least two frames.", nameof(frames));
        }

        var vectors = new List<MotionVector>(frames.Count - 1);
        for (var index = 1; index < frames.Count; index++)
        {
            var previous = frames[index - 1];
            var current = frames[index];
            if (previous.Width != current.Width || previous.Height != current.Height)
            {
                throw new ArgumentException("Motion frames must have matching dimensions.", nameof(frames));
            }

            var bestScore = double.MaxValue;
            var bestX = 0;
            var bestY = 0;
            var sampleCount = 0;
            var verticalRadius = Math.Min(_searchRadius, previous.Height - 1);
            var horizontalRadius = Math.Min(_searchRadius, previous.Width - 1);
            for (var offsetY = -verticalRadius; offsetY <= verticalRadius; offsetY++)
            {
                for (var offsetX = -horizontalRadius; offsetX <= horizontalRadius; offsetX++)
                {
                    long score = 0;
                    var count = 0;
                    for (var y = 0; y < previous.Height; y += _sampleStride)
                    {
                        for (var x = 0; x < previous.Width; x += _sampleStride)
                        {
                            var currentX = x + offsetX;
                            var currentY = y + offsetY;
                            if (currentX < 0 || currentX >= current.Width || currentY < 0 || currentY >= current.Height)
                            {
                                continue;
                            }

                            score += Math.Abs(Luma(previous, x, y) - Luma(current, currentX, currentY));
                            count++;
                        }
                    }

                    var averageScore = count == 0 ? double.MaxValue : score / (double)count;
                    if (count > 0 && averageScore < bestScore)
                    {
                        bestScore = averageScore;
                        bestX = offsetX;
                        bestY = offsetY;
                        sampleCount = count;
                    }
                }
            }

            var normalizedError = sampleCount == 0 ? 1 : bestScore / 255;
            vectors.Add(new MotionVector(current.FrameIndex, bestX, bestY, Math.Clamp((float)(1 - normalizedError), 0, 1)));
        }

        return vectors;
    }

    private static int Luma(MotionFrame frame, int x, int y)
    {
        var offset = (y * frame.Width + x) * 3;
        return (int)MathF.Round(0.2126f * frame.RgbBytes[offset] + 0.7152f * frame.RgbBytes[offset + 1] + 0.0722f * frame.RgbBytes[offset + 2]);
    }
}

public static class StabilizationPlanner
{
    public static StabilizationPlan Plan(IReadOnlyList<MotionVector> vectors, int smoothingRadius = 2, int maximumCompensation = 64)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        if (smoothingRadius is < 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(smoothingRadius));
        }

        if (maximumCompensation is < 0 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCompensation));
        }

        var cumulative = new List<(int FrameIndex, int X, int Y)>(vectors.Count);
        var totalX = 0;
        var totalY = 0;
        foreach (var vector in vectors)
        {
            totalX = checked(totalX + vector.OffsetX);
            totalY = checked(totalY + vector.OffsetY);
            cumulative.Add((vector.FrameIndex, totalX, totalY));
        }

        var transforms = cumulative.Select((current, index) =>
        {
            var start = Math.Max(0, index - smoothingRadius);
            var end = Math.Min(cumulative.Count - 1, index + smoothingRadius);
            var averageX = cumulative.Skip(start).Take(end - start + 1).Average(item => item.X);
            var averageY = cumulative.Skip(start).Take(end - start + 1).Average(item => item.Y);
            return new StabilizationTransform(
                current.FrameIndex,
                Math.Clamp((int)Math.Round(averageX - current.X), -maximumCompensation, maximumCompensation),
                Math.Clamp((int)Math.Round(averageY - current.Y), -maximumCompensation, maximumCompensation));
        }).ToArray();
        return new StabilizationPlan(transforms);
    }
}
