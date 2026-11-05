using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// Darkness with point lights for caves, dungeons and night scenes. Each frame the game clears the
/// map, adds lights in world space and draws it over the finished scene:
/// <list type="number">
/// <item>a coarse brightness grid (<see cref="CellSize"/> pixels, quantised to <see cref="Levels"/> bands)
/// is multiplied over the screen, toward <see cref="Shade"/> where it is dark;</item>
/// <item>lights with a glow colour add a soft radial halo on top.</item>
/// </list>
/// Rows of equal cells merge into single rectangles, so a full screen costs a few hundred quads.
/// With <see cref="Darkness"/> 1 and lights of brightness 0 it becomes a flat colour grade plus glows.
/// </summary>
public sealed class LightMap : IDisposable
{
    private readonly record struct Light(Vector2 Position, float Radius, float Brightness, Color? Glow, float GlowRadius);

    private readonly List<Light> _lights = [];
    private readonly Texture? _glow;
    private readonly int _glowSize;

    /// <param name="device">Creates the halo texture; pass null to draw darkness only (e.g. headless tests).</param>
    /// <param name="glowImage">Optional square white halo to match a game's style (e.g. banded pixel glows);
    /// defaults to <see cref="CreateGlowImage"/>. It is scaled to each light's glow radius.</param>
    public LightMap(IGraphicsDevice? device, PixelImage? glowImage = null)
    {
        var image = glowImage ?? CreateGlowImage(64);
        _glowSize = image.Width;
        if (device is not null) _glow = device.CreateTexture(image);
    }

    /// <summary>Ambient darkness: 0 = no effect, 1 = fully <see cref="Shade"/> where unlit.</summary>
    public float Darkness { get; set; }
    /// <summary>The colour full darkness multiplies the scene toward.</summary>
    public Color Shade { get; set; } = Color.Hex("101020");
    /// <summary>Multiplier applied to every glow's alpha (e.g. fade halos with the darkness).</summary>
    public float GlowIntensity { get; set; } = 1f;
    /// <summary>Size of a brightness cell in pixels (4 by default); larger cells are cheaper and blockier.</summary>
    public int CellSize { get; set; } = 4;
    /// <summary>Number of brightness bands (12 by default); fewer bands give a more stylised look.</summary>
    public int Levels { get; set; } = 12;
    /// <summary>Number of lights added since the last <see cref="Clear"/>.</summary>
    public int LightCount => _lights.Count;

    /// <summary>Removes every light; call it at the start of each frame before adding lights.</summary>
    public void Clear() => _lights.Clear();

    /// <summary>
    /// Adds a light. <paramref name="brightness"/> lifts darkness inside <paramref name="radius"/> with a smooth
    /// falloff; <paramref name="glow"/> (alpha respected) adds an additive halo of <paramref name="glowRadius"/>
    /// (default 90% of the radius). A light may only glow (brightness 0) or only brighten (no glow).
    /// </summary>
    public void Add(Vector2 worldPosition, float radius, float brightness, Color? glow = null, float? glowRadius = null)
    {
        if (radius <= 0 || (brightness <= 0 && glow is null)) return;
        _lights.Add(new Light(worldPosition, radius, MathF.Max(0, brightness), glow, glowRadius ?? radius * 0.9f));
    }

    /// <summary>Brightness 0..1 at a world point, before banding (useful for tests and stealth/AI rules).</summary>
    public float BrightnessAt(Vector2 world)
    {
        float value = 1 - Darkness;
        foreach (var l in _lights)
        {
            if (l.Brightness <= 0) continue;
            float d = Vector2.Distance(world, l.Position) / l.Radius;
            if (d >= 1) continue;
            float f = 1 - d;
            value += f * f * (3 - 2 * f) * l.Brightness;
        }
        return Math.Clamp(value, 0, 1);
    }

    /// <summary>Draws over <paramref name="screenArea"/>; <paramref name="cameraTopLeft"/> is the world point at screen (0,0).</summary>
    public void Draw(IRenderer r, Vector2 cameraTopLeft, RectF screenArea)
    {
        ArgumentNullException.ThrowIfNull(r);
        if (Darkness > 0.01f) DrawDarkness(r, cameraTopLeft, screenArea);
        if (_glow is null || GlowIntensity <= 0) return;
        r.SetBlendMode(BlendMode.Additive);
        foreach (var l in _lights)
        {
            if (l.Glow is not { } tint) continue;
            byte alpha = (byte)Math.Clamp(tint.A * GlowIntensity, 0, 255);
            if (alpha == 0) continue;
            float scale = l.GlowRadius * 2 / _glowSize;
            var center = l.Position - cameraTopLeft;
            r.Draw(_glow, _glow.Bounds, center - new Vector2(_glowSize / 2f * scale), tint with { A = alpha },
                new DrawOptions(Scale: new Vector2(scale)));
        }
        r.SetBlendMode(BlendMode.Alpha);
    }

    /// <summary>Convenience overload using a camera's rendered position (including shake).</summary>
    public void Draw(IRenderer r, Camera2D camera) => Draw(r, -camera.RenderOffset, new RectF(0, 0, r.Width, r.Height));

    private void DrawDarkness(IRenderer r, Vector2 cameraTopLeft, RectF area)
    {
        int cell = Math.Max(1, CellSize), levels = Math.Max(1, Levels);
        int cols = (int)MathF.Ceiling(area.Width / cell), rows = (int)MathF.Ceiling(area.Height / cell);
        r.SetBlendMode(BlendMode.Multiply);
        for (int cy = 0; cy < rows; cy++)
        {
            int runStart = 0, runLevel = -1;
            for (int cx = 0; cx <= cols; cx++)
            {
                int level = -1;
                if (cx < cols)
                {
                    var world = cameraTopLeft + new Vector2(area.X + (cx + 0.5f) * cell, area.Y + (cy + 0.5f) * cell);
                    level = Math.Clamp((int)MathF.Round(BrightnessAt(world) * levels), 0, levels);
                }
                if (level == runLevel) continue;
                if (runLevel is >= 0 && runLevel < levels)
                {
                    var color = Color.Lerp(Shade, Color.White, runLevel / (float)levels);
                    float x = area.X + runStart * cell, y = area.Y + cy * cell;
                    r.FillRect(new RectF(x, y, MathF.Min((cx - runStart) * cell, area.Right - x), MathF.Min(cell, area.Bottom - y)), color);
                }
                runStart = cx;
                runLevel = level;
            }
        }
        r.SetBlendMode(BlendMode.Alpha);
    }

    /// <summary>A white radial halo with a smooth (squared smoothstep) falloff.</summary>
    public static PixelImage CreateGlowImage(int size)
    {
        var image = new PixelImage(size, size);
        float c = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = MathF.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) / c;
            if (d >= 1) continue;
            float f = 1 - d;
            float a = f * f * (3 - 2 * f);
            image[x, y] = new Color(255, 255, 255, (byte)(a * a * 255));
        }
        return image;
    }

    /// <summary>Releases the halo texture.</summary>
    public void Dispose() => _glow?.Dispose();
}
