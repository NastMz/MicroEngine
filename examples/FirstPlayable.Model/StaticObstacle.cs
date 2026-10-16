namespace FirstPlayable.Model;

public readonly record struct StaticObstacle
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public StaticObstacle(float x, float y, float width, float height)
    {
        if (!IsValid(x, y, width, height)) throw new ArgumentOutOfRangeException(nameof(width), "Static solid must be finite, positive and inside the sample bounds.");
        (X, Y, Width, Height) = (x, y, width, height);
    }

    internal static bool IsValid(float x, float y, float width, float height)
        => float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(width) && float.IsFinite(height)
            && float.IsFinite(x + width) && float.IsFinite(y + height)
            && x >= 0 && y >= 0 && width > 0 && height > 0 && x + width <= 640 && y + height <= 300;
}
