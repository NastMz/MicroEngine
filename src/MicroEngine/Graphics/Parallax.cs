using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// A background image that scrolls slower (or faster) than the camera and repeats horizontally.
/// Draw it in screen space (no camera pushed). <see cref="Factor"/> 0 is fixed to the screen, 1 moves
/// with the world. <see cref="Y"/> is the screen row of the image's top edge; the optional
/// <see cref="VerticalFactor"/> lets tall worlds shift it as the camera rises or falls.
/// </summary>
public sealed class ParallaxLayer(Texture texture)
{
    public Texture Texture { get; } = texture ?? throw new ArgumentNullException(nameof(texture));
    public float Factor { get; init; } = 0.5f;
    public float VerticalFactor { get; init; }
    public float Y { get; init; }
    /// <summary>Extra horizontal offset in screen pixels per second (drifting clouds, traffic).</summary>
    public float Drift { get; init; }
    public bool RepeatX { get; init; } = true;
    public Color Tint { get; init; } = Color.White;

    public void Draw(IRenderer r, Vector2 cameraPosition, float time = 0)
    {
        ArgumentNullException.ThrowIfNull(r);
        float width = Texture.Width;
        float offset = -(cameraPosition.X * Factor) + Drift * time;
        float y = MathF.Round(Y - cameraPosition.Y * VerticalFactor);
        if (!RepeatX)
        {
            r.Draw(Texture, Texture.Bounds, new Vector2(MathF.Round(offset), y), Tint);
            return;
        }
        float start = offset % width;
        if (start > 0) start -= width;
        for (float x = start; x < r.Width; x += width)
            r.Draw(Texture, Texture.Bounds, new Vector2(MathF.Round(x), y), Tint);
    }
}
