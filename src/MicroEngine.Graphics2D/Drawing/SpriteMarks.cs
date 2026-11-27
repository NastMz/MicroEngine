using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// A named point of a frame: a shoulder, a gun tip, where a hat sits. Written in a <c>.sheet</c> file
/// (<c>"points": { "muzzle": [14, 3] }</c>) or drawn in Aseprite as the pivot of a slice.
/// </summary>
/// <param name="Name">The point's name.</param>
/// <param name="Position">Where it lies, in the frame's pixels from its top-left corner (like <see cref="Sprite.Origin"/>); a pixel's centre is half a pixel in.</param>
public readonly record struct SpritePoint(string Name, Vector2 Position);

/// <summary>
/// A named rectangle of a frame: a hit box, a hurt box, the part that is solid. Written in a <c>.sheet</c> file
/// (<c>"boxes": { "hit": [10, 2, 6, 4] }</c>) or drawn in Aseprite as a slice.
/// </summary>
/// <param name="Name">The box's name.</param>
/// <param name="Bounds">Its rectangle, in the frame's pixels from its top-left corner.</param>
public readonly record struct SpriteBox(string Name, RectF Bounds);

/// <summary>Reads points and boxes the way a sprite gives them: from the anchor, mirrored with the sprite.</summary>
internal static class SpriteMarks
{
    public static bool TryPoint(IReadOnlyList<SpritePoint> points, string name, Vector2 origin, bool flipX, out Vector2 point)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (!string.Equals(points[i].Name, name, StringComparison.Ordinal)) continue;
            var p = points[i].Position - origin;
            point = flipX ? new Vector2(-p.X, p.Y) : p;
            return true;
        }
        point = default;
        return false;
    }

    public static bool TryBox(IReadOnlyList<SpriteBox> boxes, string name, Vector2 origin, bool flipX, out RectF box)
    {
        for (int i = 0; i < boxes.Count; i++)
        {
            if (!string.Equals(boxes[i].Name, name, StringComparison.Ordinal)) continue;
            var b = boxes[i].Bounds;
            float x = b.X - origin.X;
            box = new RectF(flipX ? -(x + b.Width) : x, b.Y - origin.Y, b.Width, b.Height);
            return true;
        }
        box = default;
        return false;
    }

    /// <summary>The same marks seen from a corner moved by <paramref name="by"/> (trimming cuts the frame's margins away).</summary>
    public static IReadOnlyList<SpritePoint> Moved(IReadOnlyList<SpritePoint> points, Vector2 by)
    {
        if (points.Count == 0 || by == Vector2.Zero) return points;
        var moved = new SpritePoint[points.Count];
        for (int i = 0; i < moved.Length; i++) moved[i] = points[i] with { Position = points[i].Position + by };
        return moved;
    }

    /// <inheritdoc cref="Moved(IReadOnlyList{SpritePoint}, Vector2)"/>
    public static IReadOnlyList<SpriteBox> Moved(IReadOnlyList<SpriteBox> boxes, Vector2 by)
    {
        if (boxes.Count == 0 || by == Vector2.Zero) return boxes;
        var moved = new SpriteBox[boxes.Count];
        for (int i = 0; i < moved.Length; i++)
        {
            var b = boxes[i].Bounds;
            moved[i] = boxes[i] with { Bounds = new RectF(b.X + by.X, b.Y + by.Y, b.Width, b.Height) };
        }
        return moved;
    }

    public static string Names(IReadOnlyList<SpritePoint> points) => points.Count == 0 ? "none" : string.Join(", ", points.Select(p => p.Name));
    public static string Names(IReadOnlyList<SpriteBox> boxes) => boxes.Count == 0 ? "none" : string.Join(", ", boxes.Select(b => b.Name));
}
