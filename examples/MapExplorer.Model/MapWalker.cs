using MicroEngine.Loop;

namespace MapExplorer.Model;

public sealed class MapWalker
{
    public Zone Zone { get; private set; }
    public float X { get; private set; } = 40;
    public float Y { get; private set; } = 304;
    public float CameraX => Math.Clamp(X + 12 - 320, 0, MapLayout.For(Zone).Width - 640);
    public float CameraY => Math.Clamp(Y + 16 - 180, 0, MapLayout.For(Zone).Height - 360);

    public static MapWalker Create(Zone zone, float x, float y)
    {
        var map = MapLayout.For(zone);
        if (!float.IsFinite(x) || x < 0 || x > map.Width - 24) throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y) || y < 0 || y > map.Height - 32) throw new ArgumentOutOfRangeException(nameof(y));
        return new() { Zone = zone, X = x, Y = y };
    }

    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        var map = MapLayout.For(Zone);
        float dx = (input.IsHeld("right") ? 1 : 0) - (input.IsHeld("left") ? 1 : 0);
        float dy = (input.IsHeld("down") ? 1 : 0) - (input.IsHeld("up") ? 1 : 0);
        if (dx != 0 && dy != 0) { dx /= MathF.Sqrt(2); dy /= MathF.Sqrt(2); }
        float distance = 180 * (float)delta.TotalSeconds;
        float requestedX = X + dx * distance;
        float resolvedY = Math.Clamp(Y + dy * distance, 0, map.Height - 32);
        if (resolvedY >= 240 && resolvedY + 32 <= 400)
        {
            if (Zone == Zone.Meadow && requestedX > map.Width - 24)
            {
                Zone = Zone.Ruins; X = 8; Y = 304; return;
            }
            if (Zone == Zone.Ruins && requestedX < 0)
            {
                Zone = Zone.Meadow; X = 1248; Y = 304; return;
            }
        }
        X = Math.Clamp(requestedX, 0, map.Width - 24);
        Y = resolvedY;
    }
}
