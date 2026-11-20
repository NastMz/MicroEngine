using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// Colour grading: brightness, contrast around mid grey, saturation and a tint, applied in that order.
/// <see cref="PostEffectSupport.Full"/> where shaders run. Elsewhere contrast and saturation are left out
/// (<see cref="PostEffectSupport.Reduced"/>): the tint and the brightness still apply, made with blending.
/// </summary>
public sealed class ColorGrade : PostEffect
{
    /// <summary>Multiplies the colours; 1 changes nothing, 0 is black, 2 twice as bright (clamped at white).</summary>
    public float Brightness { get; set; } = 1f;
    /// <summary>Spreads the colours away from mid grey (above 1) or towards it (below 1); 1 changes nothing.</summary>
    public float Contrast { get; set; } = 1f;
    /// <summary>How much colour is kept: 1 changes nothing, 0 is black and white, above 1 is more vivid.</summary>
    public float Saturation { get; set; } = 1f;
    /// <summary>A colour every pixel is multiplied by; white changes nothing. Its alpha is ignored.</summary>
    public Color Tint { get; set; } = Color.White;

    /// <inheritdoc/>
    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) =>
        graphics.SupportsShaders || (Contrast == 1f && Saturation == 1f) ? PostEffectSupport.Full : PostEffectSupport.Reduced;

    /// <inheritdoc/>
    protected internal override void Apply(PostPass pass)
    {
        if (pass.ShadersRun)
        {
            var material = pass.BuiltIn(PostShaders.Grade);
            material.Set("Tint", new Vector4(Tint.R / 255f, Tint.G / 255f, Tint.B / 255f, 1f));
            material.Set("Brightness", MathF.Max(0, Brightness));
            material.Set("Contrast", MathF.Max(0, Contrast));
            material.Set("Saturation", MathF.Max(0, Saturation));
            pass.Copy(material);
            return;
        }
        // Without a shader: the picture times the tint and the brightness up to 1, then the rest of the brightness added.
        float brightness = MathF.Max(0, Brightness), darker = MathF.Min(1f, brightness);
        var before = pass.Source;
        var tint = new Color((byte)MathF.Round(Tint.R * darker), (byte)MathF.Round(Tint.G * darker), (byte)MathF.Round(Tint.B * darker));
        // The added part reads the picture as it was, so it needs the copy even when the copy changes nothing.
        if (tint != Color.White || brightness > 1f) pass.Copy(tint: tint);
        if (brightness <= 1f) return;
        var r = pass.BeginOver();
        r.PushState(DrawState.Additive.Filtered(TextureFilter.Nearest));
        for (float more = brightness - 1f; more > 0.002f; more -= 1f)
        {
            float part = MathF.Min(1f, more);
            var add = new Color((byte)MathF.Round(Tint.R * part), (byte)MathF.Round(Tint.G * part), (byte)MathF.Round(Tint.B * part));
            r.DrawStretched(before, before.Bounds, new RectF(0, 0, r.Width, r.Height), add);
        }
        r.PopState();
        pass.EndOver();
    }
}

/// <summary>
/// Blurs the picture. <see cref="PostEffectSupport.Full"/> where shaders run: a Gaussian blur, across then down.
/// Elsewhere (<see cref="PostEffectSupport.Reduced"/>) the picture is shrunk by halves with linear sampling and
/// enlarged again, which blurs by about the same radius with a boxier look.
/// </summary>
public sealed class Blur : PostEffect
{
    /// <summary>How far the blur reaches, in pixels of the picture. 0 leaves the picture as it is.</summary>
    public float Radius { get; set; } = 4f;

    /// <inheritdoc/>
    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) =>
        graphics.SupportsShaders ? PostEffectSupport.Full : PostEffectSupport.Reduced;

    /// <inheritdoc/>
    protected internal override void Apply(PostPass pass)
    {
        if (!(Radius > 0)) return;
        pass.Replace(Blurred(pass, pass.Source, Radius, 0f, "blur"));
    }

    /// <summary>
    /// A blurred copy of <paramref name="picture"/> in a smaller target of the chain's, keeping only what is brighter
    /// than <paramref name="threshold"/> when it is positive (what a bloom adds back).
    /// </summary>
    internal static Texture Blurred(PostPass pass, Texture picture, float radius, float threshold, string name)
    {
        int width = pass.Width, height = pass.Height;
        if (pass.ShadersRun)
        {
            // Nine samples reach the radius when they are a quarter of it apart; at half size from 4 pixels on.
            int divisor = radius >= 4 ? 2 : 1;
            var across = pass.Scratch(name + "-across", Math.Max(1, width / divisor), Math.Max(1, height / divisor));
            var down = pass.Scratch(name + "-down", across.Width, across.Height);
            var material = pass.BuiltIn(PostShaders.Blur);
            var state = DrawState.Opaque.Filtered(TextureFilter.Linear) with { Material = material };
            material.Set("Step", new Vector2(radius / 4f / width, 0));
            material.Set("Threshold", Math.Clamp(threshold, 0f, 0.999f));
            pass.Draw(picture, across, state);
            material.Set("Step", new Vector2(0, radius / 4f / height));
            material.Set("Threshold", 0f);
            pass.Draw(across.Texture, down, state);
            return down.Texture;
        }
        // No shader: halve the picture until one of its pixels is about the radius wide, then come back up through the
        // same sizes; every step is a linear copy, which averages neighbours.
        int levels = Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(radius, 2f))), 1, Levels.Length);
        var copy = DrawState.Opaque.Filtered(TextureFilter.Linear);
        var current = picture;
        for (int level = 0; level < levels; level++)
        {
            var smaller = pass.Scratch(name + Levels[level], Math.Max(1, width >> (level + 1)), Math.Max(1, height >> (level + 1)));
            pass.Draw(current, smaller, copy);
            if (level == 0 && threshold > 0) Darken(pass, picture, smaller, threshold);
            current = smaller.Texture;
        }
        for (int level = levels - 2; level >= 0; level--)
        {
            var larger = pass.Scratch(name + Levels[level] + "-up", Math.Max(1, width >> (level + 1)), Math.Max(1, height >> (level + 1)));
            pass.Draw(current, larger, copy);
            current = larger.Texture;
        }
        return current;
    }

    private static readonly string[] Levels = ["-2", "-4", "-8", "-16", "-32"];

    /// <summary>
    /// Keeps the bright parts without a shader: the picture multiplied by itself, which leaves white as it is and
    /// takes mid grey to a quarter; twice from a threshold of a half on.
    /// </summary>
    private static void Darken(PostPass pass, Texture picture, RenderTarget2D target, float threshold)
    {
        var multiply = DrawState.Multiply.Filtered(TextureFilter.Linear);
        pass.Draw(picture, target, multiply);
        if (threshold >= 0.5f) pass.Draw(picture, target, multiply);
    }
}

/// <summary>
/// Bloom: the bright parts of the picture, blurred, added back over it, so light sources glow.
/// <see cref="PostEffectSupport.Full"/> where shaders run (what is brighter than <see cref="Threshold"/>, with a
/// Gaussian blur). Elsewhere (<see cref="PostEffectSupport.Reduced"/>) the bright parts are picked by multiplying the
/// picture by itself, without an exact threshold, and blurred by halving: a softer, wider glow.
/// </summary>
public sealed class Bloom : PostEffect
{
    /// <summary>How bright a pixel's brightest channel must be to glow, 0..1; 0 makes everything glow.</summary>
    public float Threshold { get; set; } = 0.6f;
    /// <summary>How much of the glow is added: 1 adds it once, 0 nothing, 2 twice.</summary>
    public float Strength { get; set; } = 1f;
    /// <summary>How far the glow reaches, in pixels of the picture.</summary>
    public float Radius { get; set; } = 6f;

    /// <inheritdoc/>
    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) =>
        graphics.SupportsShaders ? PostEffectSupport.Full : PostEffectSupport.Reduced;

    /// <inheritdoc/>
    protected internal override void Apply(PostPass pass)
    {
        if (!(Strength > 0) || !(Radius > 0)) return;
        var glow = Blur.Blurred(pass, pass.Source, Radius, MathF.Max(Threshold, pass.ShadersRun ? 0f : 0.01f), "bloom");
        var r = pass.BeginOver();
        r.PushState(DrawState.Additive.Filtered(TextureFilter.Linear));
        for (float left = Strength; left > 0.002f; left -= 1f)
        {
            byte part = (byte)MathF.Round(MathF.Min(1f, left) * 255);
            r.DrawStretched(glow, glow.Bounds, new RectF(0, 0, r.Width, r.Height), new Color(part, part, part));
        }
        r.PopState();
        pass.EndOver();
    }
}

/// <summary>
/// Darkens the picture towards its corners (or multiplies it towards <see cref="Color"/>). Made of a texture and the
/// multiply blend state, so it is <see cref="PostEffectSupport.Full"/> and the same on every device, and it keeps the
/// picture's transparency.
/// </summary>
public sealed class Vignette : PostEffect
{
    private const int Size = 64;

    /// <summary>How strong the darkening is at the corners: 0 nothing, 1 the corners are the picture times <see cref="Color"/> (black, by default).</summary>
    public float Strength { get; set; } = 0.5f;
    /// <summary>Where the darkening starts, from the centre (0) to the corners (1).</summary>
    public float Radius { get; set; } = 0.45f;
    /// <summary>How gradually it darkens after <see cref="Radius"/>, as a share of the way to the corners; 0 is a hard edge.</summary>
    public float Softness { get; set; } = 0.55f;
    /// <summary>The colour the corners are multiplied by; black (the default) darkens, another colour tints. Its alpha is ignored.</summary>
    public Color Color { get; set; } = Color.Black;

    /// <inheritdoc/>
    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) => PostEffectSupport.Full;

    /// <inheritdoc/>
    protected internal override void Apply(PostPass pass)
    {
        float strength = Math.Clamp(Strength, 0f, 1f);
        if (strength <= 0) return;
        var shape = pass.Image("vignette", HashCode.Combine(Radius, Softness), this, static vignette => vignette.Shape());
        var r = pass.BeginOver();
        // Multiplied in: the picture keeps its transparency, so a vignette never darkens what shows through it.
        r.PushState(DrawState.Multiply.Filtered(TextureFilter.Linear));
        r.DrawStretched(shape, shape.Bounds, new RectF(0, 0, r.Width, r.Height), Color.WithAlpha(strength));
        r.PopState();
        pass.EndOver();
    }

    /// <summary>White whose alpha is how much of the vignette's colour each place gets: 0 inside the radius, 1 at the corners.</summary>
    private PixelImage Shape()
    {
        var image = new PixelImage(Size, Size);
        float start = Math.Clamp(Radius, 0f, 1f), end = MathF.Max(start + MathF.Max(Softness, 0f), start + 1e-4f);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                var fromCentre = new Vector2((x + 0.5f) / Size * 2 - 1, (y + 0.5f) / Size * 2 - 1);
                float t = Math.Clamp((fromCentre.Length() / MathF.Sqrt(2) - start) / (end - start), 0f, 1f);
                image[x, y] = new Color(255, 255, 255, (byte)MathF.Round(t * t * (3 - 2 * t) * 255));
            }
        return image;
    }
}

/// <summary>
/// Darkens every other line of the picture, as a television's lines show. Made of a texture and the multiply blend
/// state, so it is <see cref="PostEffectSupport.Full"/> and the same on every device, and it keeps the picture's transparency.
/// </summary>
public sealed class Scanlines : PostEffect
{
    /// <summary>How dark the dark lines are: 0 nothing, 1 black.</summary>
    public float Strength { get; set; } = 0.25f;
    /// <summary>
    /// How many lines the picture has: each is a bright row and a dark row. 0 (the default) is half the picture's
    /// height, one row of pixels each. On a picture larger than the game's canvas (the window), give the canvas's height.
    /// </summary>
    public int Lines { get; set; }

    /// <inheritdoc/>
    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) => PostEffectSupport.Full;

    /// <inheritdoc/>
    protected internal override void Apply(PostPass pass) => PostLines.Draw(pass, Strength, Lines);
}

/// <summary>The dark rows of <see cref="Scanlines"/>, which <see cref="Crt"/> also draws where its shader does not run.</summary>
internal static class PostLines
{
    public static void Draw(PostPass pass, float strength, int lines)
    {
        strength = Math.Clamp(strength, 0f, 1f);
        if (strength <= 0) return;
        int count = Math.Clamp(lines > 0 ? lines : pass.Height / 2, 1, 4096);
        // One texel per row of a line: the first clear, the second opaque; stretched over the picture with nearest sampling.
        var rows = pass.Image("scanlines", count, count, static count =>
        {
            var image = new PixelImage(1, count * 2);
            for (int line = 0; line < count; line++) image[0, line * 2 + 1] = Color.White;
            return image;
        }, TextureFilter.Nearest);
        var r = pass.BeginOver();
        // Multiplied in, so the picture keeps its transparency.
        r.PushState(DrawState.Multiply.Filtered(TextureFilter.Nearest));
        r.DrawStretched(rows, rows.Bounds, new RectF(0, 0, r.Width, r.Height), Color.Black.WithAlpha(strength));
        r.PopState();
        pass.EndOver();
    }
}

/// <summary>
/// The picture as a cathode-ray tube shows it: the glass bulges (<see cref="Curvature"/>), the lines of the picture
/// show (<see cref="Scanlines"/>) and so do the phosphor's coloured stripes (<see cref="Mask"/>).
/// <see cref="PostEffectSupport.Full"/> where shaders run. Elsewhere only the lines are drawn
/// (<see cref="PostEffectSupport.Reduced"/>): the picture stays flat and has no stripes.
/// </summary>
public sealed class Crt : PostEffect
{
    /// <summary>How much the glass bulges: 0 is flat, 0.1 a gentle curve. What falls outside the curved picture is black.</summary>
    public float Curvature { get; set; } = 0.1f;
    /// <summary>How dark the picture is between two of its lines: 0 nothing, 1 black.</summary>
    public float Scanlines { get; set; } = 0.25f;
    /// <summary>
    /// How many lines the picture has. 0 (the default) is half the picture's height. On a picture larger than the game's
    /// canvas (the window), give the canvas's height, so each canvas row is one line.
    /// </summary>
    public int Lines { get; set; }
    /// <summary>How visible the red, green and blue stripes are: 0 nothing, 1 full. They are three pixels of the picture wide.</summary>
    public float Mask { get; set; } = 0.15f;

    /// <inheritdoc/>
    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) =>
        graphics.SupportsShaders ? PostEffectSupport.Full : Scanlines > 0 ? PostEffectSupport.Reduced : PostEffectSupport.None;

    /// <inheritdoc/>
    protected internal override void Apply(PostPass pass)
    {
        if (!pass.ShadersRun)
        {
            PostLines.Draw(pass, Scanlines, Lines);
            return;
        }
        var material = pass.BuiltIn(PostShaders.Crt);
        material.Set("Curvature", MathF.Max(0, Curvature));
        material.Set("Scanlines", Math.Clamp(Scanlines, 0f, 1f));
        material.Set("Lines", (float)(Lines > 0 ? Lines : Math.Max(1, pass.Height / 2)));
        material.Set("Mask", Math.Clamp(Mask, 0f, 1f));
        material.Set("Columns", pass.Width / 3f);
        pass.Replace(pass.Source, TextureFilter.Linear, material);
    }
}

/// <summary>
/// A full-screen pass through a material of the game's own (a fragment shader compiled by <c>microengine shaders</c>,
/// loaded with <c>Assets.Material</c>): the picture is the draw's texture, in slot 0, and the shader's output is the
/// new picture. <see cref="PostEffectSupport.Full"/> where shaders run; <see cref="PostEffectSupport.None"/> elsewhere,
/// where the picture passes through unchanged (the material's own fallback is not used). The game owns the material.
/// </summary>
public sealed class ShaderEffect : PostEffect
{
    /// <summary>Creates a pass through <paramref name="material"/>.</summary>
    public ShaderEffect(Material2D material) => Material = material ?? throw new ArgumentNullException(nameof(material));

    /// <summary>The material the picture is drawn through. Set its parameters at any time before the frame is drawn.</summary>
    public Material2D Material { get; }

    /// <summary>How the picture is sampled by the shader: nearest (the default) reads each pixel as it is.</summary>
    public TextureFilter Filter { get; set; } = TextureFilter.Nearest;

    /// <summary>
    /// Called just before the pass, to set parameters that depend on the picture (its size in
    /// <see cref="PostPass.Width"/> and <see cref="PostPass.Height"/>); null when there are none.
    /// </summary>
    public Action<Material2D, PostPass>? Prepare { get; set; }

    /// <inheritdoc/>
    public override PostEffectSupport SupportOn(IGraphicsDevice graphics) =>
        Material.IsSupported && graphics.SupportsShaders ? PostEffectSupport.Full : PostEffectSupport.None;

    /// <inheritdoc/>
    protected internal override void Apply(PostPass pass)
    {
        Prepare?.Invoke(Material, pass);
        pass.Replace(pass.Source, Filter, Material);
    }
}
