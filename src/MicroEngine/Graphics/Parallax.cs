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
    /// <summary>The image; the layer does not dispose it.</summary>
    public Texture Texture { get; } = texture ?? throw new ArgumentNullException(nameof(texture));
    /// <summary>Horizontal scroll relative to the camera: 0 stays fixed, 1 moves with the world (0.5 by default).</summary>
    public float Factor { get; init; } = 0.5f;
    /// <summary>Vertical scroll relative to the camera (0 by default: the layer stays at <see cref="Y"/>).</summary>
    public float VerticalFactor { get; init; }
    /// <summary>Screen row of the image's top edge when the camera is at Y = 0.</summary>
    public float Y { get; init; }
    /// <summary>Extra horizontal offset in screen pixels per second (drifting clouds, traffic).</summary>
    public float Drift { get; init; }
    /// <summary>Whether the image repeats to cover the screen width (true by default).</summary>
    public bool RepeatX { get; init; } = true;
    /// <summary>Colour multiplier (white by default).</summary>
    public Color Tint { get; init; } = Color.White;

    /// <summary>Draws the layer in screen space.</summary>
    /// <param name="r">The renderer, with no camera pushed.</param>
    /// <param name="cameraPosition">The camera's <see cref="Camera2D.Position"/>.</param>
    /// <param name="time">Seconds used by <see cref="Drift"/>, for example <see cref="Core.Game.Time"/>.</param>
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
