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
        if (map.Solids.Any(wall => x < wall.X + wall.Width && x + 24 > wall.X && y < wall.Y + wall.Height && y + 32 > wall.Y))
            throw new ArgumentException("The player position overlaps a solid.");
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
        // Deliberate Y-then-X swept-axis path; this is not continuous diagonal time of impact.
        foreach (var wall in map.Solids)
        {
            if (X >= wall.X + wall.Width || X + 24 <= wall.X) continue;
            if (resolvedY > Y && Y + 32 <= wall.Y && resolvedY + 32 > wall.Y)
                resolvedY = MathF.Min(resolvedY, wall.Y - 32);
            else if (resolvedY < Y && Y >= wall.Y + wall.Height && resolvedY < wall.Y + wall.Height)
                resolvedY = MathF.Max(resolvedY, wall.Y + wall.Height);
        }
        foreach (var wall in map.Solids)
        {
            if (resolvedY >= wall.Y + wall.Height || resolvedY + 32 <= wall.Y) continue;
            if (requestedX > X && X + 24 <= wall.X && requestedX + 24 > wall.X)
                requestedX = MathF.Min(requestedX, wall.X - 24);
            else if (requestedX < X && X >= wall.X + wall.Width && requestedX < wall.X + wall.Width)
                requestedX = MathF.Max(requestedX, wall.X + wall.Width);
        }
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
