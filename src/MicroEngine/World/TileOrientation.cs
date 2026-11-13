using MicroEngine.Graphics;

namespace MicroEngine.World;

/// <summary>
/// How one cell's tile is mirrored or turned, as map editors store it (Tiled's flip bits). The diagonal flip (x and y
/// swapped) applies first, then the horizontal and the vertical flips, so the eight combinations cover every quarter
/// turn with or without a mirror: <c>Diagonal | FlipX</c> is a quarter turn clockwise, <c>Diagonal | FlipY</c> a
/// quarter turn anticlockwise, <c>FlipX | FlipY</c> a half turn.
/// </summary>
[Flags]
public enum TileOrientation : byte
{
    /// <summary>Drawn as it is in the tileset.</summary>
    None = 0,
    /// <summary>Mirrored left to right.</summary>
    FlipX = 1,
    /// <summary>Mirrored top to bottom.</summary>
    FlipY = 2,
    /// <summary>Mirrored across the top-left to bottom-right diagonal (x and y swapped), before the other flips.</summary>
    Diagonal = 4,
}

/// <summary>Turns a <see cref="TileOrientation"/> into drawing options.</summary>
public static class TileOrientations
{
    /// <summary>
    /// The flips and rotation that draw a tile of <paramref name="size"/> pixels in <paramref name="orientation"/>,
    /// turning about the tile's centre so it stays in its cell. Pass the result to <see cref="IRenderer.Draw"/> with
    /// the tile's top-left as the position.
    /// </summary>
    public static DrawOptions ToDrawOptions(this TileOrientation orientation, System.Numerics.Vector2 size)
    {
        bool x = (orientation & TileOrientation.FlipX) != 0, y = (orientation & TileOrientation.FlipY) != 0;
        if ((orientation & TileOrientation.Diagonal) == 0) return new DrawOptions(FlipX: x, FlipY: y);
        // The renderer mirrors the source, then turns it clockwise. The diagonal flip is a three-quarter turn of the
        // mirrored tile; adding the horizontal flip cancels the mirror and leaves a quarter turn, and so on.
        const float Quarter = MathF.PI / 2;
        var origin = size / 2;
        return (x, y) switch
        {
            (false, false) => new DrawOptions(FlipX: true, Rotation: 3 * Quarter, Origin: origin),
            (true, false) => new DrawOptions(Rotation: Quarter, Origin: origin),
            (false, true) => new DrawOptions(Rotation: 3 * Quarter, Origin: origin),
            _ => new DrawOptions(FlipX: true, Rotation: Quarter, Origin: origin),
        };
    }
}
