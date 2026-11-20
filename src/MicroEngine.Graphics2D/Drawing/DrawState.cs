using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>
/// What a renderer draws with between <see cref="IRenderer.PushState"/> and <see cref="IRenderer.PopState"/>: a blend
/// state, a sampler and a material. A part left null keeps what the renderer was drawing with, so a state names only
/// what it changes.
/// </summary>
/// <param name="Blend">
/// How draws combine with what is there (colours are premultiplied by then). The four presets
/// (<see cref="BlendState.Opaque"/>, <see cref="BlendState.AlphaPremultiplied"/>, <see cref="BlendState.Additive"/>,
/// <see cref="BlendState.Multiply"/>) work on every device; any other needs a device that declares
/// <see cref="GraphicsFeatures.CustomBlend"/>. Null keeps the current blending.
/// </param>
/// <param name="Sampler">How textures are sampled; null lets each texture's <see cref="Texture.Filter"/> choose, as usual.</param>
/// <param name="Material">The shader draws go through (<see cref="Material2D"/>); null keeps the current one.</param>
/// <example>
/// <code>
/// renderer.PushState(DrawState.Additive);                              // one blend state
/// renderer.PushState(DrawState.Opaque with { Material = water });      // a material, replacing what is under it
/// renderer.PushState(new DrawState(Material: outline).Filtered(TextureFilter.Linear));
/// </code>
/// </example>
public readonly record struct DrawState(BlendState? Blend = null, SamplerDescription? Sampler = null, Material2D? Material = null)
{
    /// <summary>Draws replace what is there, alpha included (<see cref="BlendState.Opaque"/>): copies of whole pictures.</summary>
    public static DrawState Opaque => new(BlendState.Opaque);
    /// <summary>Normal transparency (<see cref="BlendState.AlphaPremultiplied"/>), as <see cref="BlendMode.Alpha"/>.</summary>
    public static DrawState Alpha => new(BlendState.AlphaPremultiplied);
    /// <summary>Draws add light (<see cref="BlendState.Additive"/>), as <see cref="BlendMode.Additive"/>.</summary>
    public static DrawState Additive => new(BlendState.Additive);
    /// <summary>Draws multiply what is there (<see cref="BlendState.Multiply"/>), as <see cref="BlendMode.Multiply"/>.</summary>
    public static DrawState Multiply => new(BlendState.Multiply);

    /// <summary>This state sampling every texture with <paramref name="filter"/>, clamped at the edges, whatever the texture's own filter.</summary>
    public DrawState Filtered(TextureFilter filter) =>
        this with { Sampler = filter == TextureFilter.Nearest ? SamplerDescription.PointClamp : SamplerDescription.LinearClamp };
}

/// <summary>
/// What a material's draws become on a graphics device that cannot run its shader (one without
/// <see cref="GraphicsFeatures.Shaders"/>: SDL's default device and its software renderer, raylib, browser builds and
/// the headless platform). There, every draw made under the material is drawn the usual way, without the shader, with
/// this fallback applied; the game's code is the same on every device.
/// </summary>
/// <param name="Tint">A colour every draw is multiplied by; null leaves the draws as they are.</param>
/// <param name="Blend">A blend state the draws use instead of the state's; null keeps the state's.</param>
/// <param name="Hidden">Whether the draws are left out altogether: for an effect that means nothing without its shader (a glow drawn over the picture).</param>
public readonly record struct MaterialFallback(Color? Tint = null, BlendState? Blend = null, bool Hidden = false)
{
    /// <summary>The draws as they are, without the shader. The default.</summary>
    public static MaterialFallback Plain => default;
    /// <summary>Nothing is drawn where the shader cannot run.</summary>
    public static MaterialFallback Hide => new(Hidden: true);
}

/// <summary>
/// A picture off screen that a renderer draws into (<see cref="IRenderer.PushTarget"/>) and that is then drawn like any
/// texture: <see cref="Texture"/> is a texture of the drawing API, so it works with every <c>Draw</c> overload, as the
/// source of a <see cref="Sprite"/> and as a material's texture. Create one with
/// <see cref="IGraphicsDevice.CreateRenderTarget(int, int, TextureFilter)"/>, or through a game's assets
/// (<c>Assets.RenderTarget</c>), which release it with their scope. A target keeps its pixels between frames until it
/// is drawn into again; they are premultiplied, like the canvas.
/// </summary>
public sealed class RenderTarget2D : IDisposable
{
    /// <summary>
    /// Wraps the texture a graphics device made to be drawn into. Backends call it from
    /// <see cref="IGraphicsDevice.CreateRenderTarget(int, int, TextureFilter)"/>; a game asks its graphics device.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="texture"/> cannot be drawn into.</exception>
    public RenderTarget2D(Texture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (!texture.IsRenderTarget) throw new ArgumentException("The texture was not created to be drawn into; ask the graphics device for a render target.", nameof(texture));
        Texture = texture;
    }

    /// <summary>The target's picture as a texture. It is the same resource: disposing either disposes both.</summary>
    public Texture Texture { get; }
    /// <summary>Width in pixels.</summary>
    public int Width => Texture.Width;
    /// <summary>Height in pixels.</summary>
    public int Height => Texture.Height;
    /// <summary>The whole target as a source rectangle.</summary>
    public RectF Bounds => Texture.Bounds;
    /// <summary>Whether the target was disposed; a disposed target cannot be drawn into or drawn.</summary>
    public bool IsDisposed => Texture.IsDisposed;
    /// <summary>Memory the target takes on the device, in bytes.</summary>
    public long SizeInBytes => Texture.SizeInBytes;
    /// <summary>Releases the target and its texture. Safe to call more than once.</summary>
    public void Dispose() => Texture.Dispose();
}
