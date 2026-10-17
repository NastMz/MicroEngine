using MicroEngine.Loop;

namespace MapExplorer.Model;

public enum Zone { Meadow, Ruins }
public readonly record struct Landmark(Rect Bounds, Rgba Color);

public sealed class MapLayout
{
    public int Width { get; }
    public int Height { get; }
    public Rgba Background { get; }
    public IReadOnlyList<Landmark> Landmarks { get; }
    private MapLayout(int width, int height, Rgba background, params Landmark[] landmarks)
        => (Width, Height, Background, Landmarks) = (width, height, background, Array.AsReadOnly(landmarks));

    private static readonly MapLayout Meadow = new(1280, 720, new(24, 40, 32),
        new(new(200, 80, 100, 60), new(95, 130, 165)),
        new(new(400, 240, 100, 80), new(180, 100, 70)),
        new(new(900, 500, 120, 80), new(80, 150, 90)));
    private static readonly MapLayout Ruins = new(960, 540, new(32, 24, 48),
        new(new(160, 100, 100, 100), new(95, 130, 165)),
        new(new(700, 360, 120, 80), new(180, 100, 70)));

    public static MapLayout For(Zone zone) => zone switch
    {
        Zone.Meadow => Meadow, Zone.Ruins => Ruins,
        _ => throw new ArgumentOutOfRangeException(nameof(zone))
    };
}
