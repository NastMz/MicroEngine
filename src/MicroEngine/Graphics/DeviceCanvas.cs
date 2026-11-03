using System.Numerics;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>
/// The textures of the drawing API (<see cref="IGraphicsDevice"/>) made on a graphics device of the device contract: each
/// texture is a device texture whose straight-alpha pixels are premultiplied when they are uploaded, and its
/// <see cref="Texture.Filter"/> chooses the sampler it is drawn with. Backends built on a device use it as their
/// <see cref="Core.IPlatform.Graphics"/>.
/// </summary>
public sealed class DeviceGraphics : IGraphicsDevice
{
    private readonly HashSet<DeviceTexture> _live = [];

    /// <summary>Makes textures on <paramref name="device"/>.</summary>
    public DeviceGraphics(Dev.IGraphicsDevice device) => Device = device ?? throw new ArgumentNullException(nameof(device));

    /// <summary>The device the textures live on.</summary>
    public Dev.IGraphicsDevice Device { get; }

    /// <inheritdoc/>
    public TextureFilter DefaultFilter { get; set; }

    /// <summary>Textures created and not disposed yet.</summary>
    public int LiveTextures => _live.Count;

    /// <summary>Raised before a texture's pixels change, so renderers drawing it flush what they recorded first.</summary>
    internal event Action? Uploading;

    /// <inheritdoc/>
    public Texture CreateTexture(PixelImage image, TextureFilter filter)
    {
        ArgumentNullException.ThrowIfNull(image);
        var rgba = image.ToRgba();
        Dev.PremultipliedColor.PremultiplyInPlace(rgba);
        var native = Device.CreateTexture(new Dev.TextureDescription(image.Width, image.Height), rgba);
        var texture = new DeviceTexture(this, native, filter);
        _live.Add(texture);
        return texture;
    }

    internal void Upload(DeviceTexture texture, int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
    {
        Uploading?.Invoke();
        var rgba = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i++) (rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]) = (pixels[i].R, pixels[i].G, pixels[i].B, pixels[i].A);
        Dev.PremultipliedColor.PremultiplyInPlace(rgba);
        var commands = Device.BeginCommands();
        commands.UpdateTexture(texture.Native, new Dev.RectI(x, y, width, height), rgba);
        Device.Submit(commands);
    }

    internal void Forget(DeviceTexture texture) => _live.Remove(texture);

    /// <summary>Disposes every texture not disposed yet (the platform is closing).</summary>
    public void DisposeAll()
    {
        foreach (var texture in _live.ToArray()) texture.Dispose();
    }
}

/// <summary>A texture of the drawing API backed by a device texture (<see cref="DeviceGraphics"/>).</summary>
public sealed class DeviceTexture : Texture
{
    private readonly DeviceGraphics _graphics;

    internal DeviceTexture(DeviceGraphics graphics, Dev.Texture native, TextureFilter filter) : base(graphics, native.Width, native.Height, filter)
        => (_graphics, Native) = (graphics, native);

    /// <summary>The device texture holding the premultiplied pixels.</summary>
    public Dev.Texture Native { get; }

    /// <summary>The sampler the texture's filter draws with (clamped at the edges).</summary>
    public Dev.SamplerDescription Sampler => Filter == TextureFilter.Linear ? Dev.SamplerDescription.LinearClamp : Dev.SamplerDescription.PointClamp;

    /// <inheritdoc/>
    protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels) => _graphics.Upload(this, x, y, width, height, pixels);

    /// <inheritdoc/>
    protected override void ApplyFilter(TextureFilter filter) { } // the sampler is chosen per draw

    /// <inheritdoc/>
    protected override void Release()
    {
        _graphics.Forget(this);
        Native.Dispose();
    }
}

/// <summary>
/// The drawing API (<see cref="IRenderer"/>) as a façade over the 2D batcher (<see cref="Renderer2D"/>) on a graphics
/// device: every call is recorded into the batcher, and the frame reaches the device in as few draw calls as the draws'
/// textures and states allow. Straight-alpha colours are premultiplied on the way in, so the blend modes keep their
/// written equations (see <see cref="IRenderer.SetBlendMode"/>).
/// </summary>
public sealed class DeviceRenderer : IRenderer
{
    private const int CircleSegments = 36;
    private static readonly Vector2[] Rim = [.. Enumerable.Range(0, CircleSegments + 1).Select(i => new Vector2(MathF.Cos(i * MathF.Tau / CircleSegments), MathF.Sin(i * MathF.Tau / CircleSegments)))];
    private readonly Renderer2D _batcher;
    private readonly DeviceGraphics _graphics;
    private readonly Dev.RenderTarget _target;
    private readonly Stack<Matrix3x2> _cameras = new();
    private readonly Dev.Vertex2D[] _fan = new Dev.Vertex2D[CircleSegments + 2];
    private readonly ushort[] _fanIndices = BuildFanIndices();
    private Matrix3x2 _camera = Matrix3x2.Identity;
    private BlendMode _blend;
    private Dev.RectI? _clip;
    private bool _drawing;

    /// <summary>A renderer drawing into <paramref name="target"/> (the canvas) with the textures of <paramref name="graphics"/>.</summary>
    public DeviceRenderer(Renderer2D batcher, DeviceGraphics graphics, Dev.RenderTarget target)
    {
        _batcher = batcher ?? throw new ArgumentNullException(nameof(batcher));
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _graphics.Uploading += () => { if (_drawing) _batcher.Flush(); };
    }

    /// <inheritdoc/>
    public int Width => _target.Width;
    /// <inheritdoc/>
    public int Height => _target.Height;

    /// <summary>The batcher's counts for the last frame (sprites, draw calls, flushes): exact and the same on every device.</summary>
    public Renderer2DStatistics Statistics => _batcher.Statistics;

    /// <summary>Starts a frame: the canvas keeps its pixels, blending is <see cref="BlendMode.Alpha"/>, no clip, no camera.</summary>
    public void Begin()
    {
        _cameras.Clear();
        _camera = Matrix3x2.Identity;
        _blend = BlendMode.Alpha;
        _clip = null;
        _batcher.Begin(new BatchSettings(_target));
        _drawing = true;
        ApplyState();
    }

    /// <summary>Ends the frame and submits it.</summary>
    /// <exception cref="InvalidOperationException">A camera pushed this frame was not popped.</exception>
    public void End()
    {
        _drawing = false;
        _batcher.End();
        if (_cameras.Count != 0) throw new InvalidOperationException("Unbalanced PushCamera/PopCamera in this frame.");
    }

    /// <inheritdoc/>
    public void Clear(Color color) => _batcher.Clear(Dev.PremultipliedColor.FromStraight(color));

    /// <inheritdoc/>
    public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
    {
        var own = Texture.CheckDrawable<DeviceTexture>(texture, _graphics);
        var scale = options.Scale ?? Vector2.One;
        var world = Matrix3x2.CreateTranslation(-options.Origin) * Matrix3x2.CreateScale(scale) * Matrix3x2.CreateRotation(options.Rotation)
            * Matrix3x2.CreateTranslation(position + options.Origin * scale);
        var flip = (options.FlipX ? SpriteFlip.Horizontal : SpriteFlip.None) | (options.FlipY ? SpriteFlip.Vertical : SpriteFlip.None);
        DrawTexture(own, source, world, tint, flip);
    }

    /// <inheritdoc/>
    public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
    {
        var own = Texture.CheckDrawable<DeviceTexture>(texture, _graphics);
        if (source.Width == 0 || source.Height == 0) return;
        var world = Matrix3x2.CreateScale(destination.Width / source.Width, destination.Height / source.Height) * Matrix3x2.CreateTranslation(destination.X, destination.Y);
        DrawTexture(own, source, world, tint, SpriteFlip.None);
    }

    private void DrawTexture(DeviceTexture texture, RectF source, in Matrix3x2 world, Color tint, SpriteFlip flip)
    {
        var state = _batcher.State;
        if (state.Sampler != texture.Sampler) _batcher.State = state with { Sampler = texture.Sampler };
        _batcher.Draw(texture.Native, source, world * _camera, Dev.PremultipliedColor.FromStraight(tint), 0, flip);
    }

    /// <inheritdoc/>
    public void FillRect(RectF rect, Color color)
    {
        if (color.A == 0) return;
        _batcher.Draw(_batcher.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(rect.Width, rect.Height) * Matrix3x2.CreateTranslation(rect.X, rect.Y) * _camera,
            Dev.PremultipliedColor.FromStraight(color));
    }

    /// <inheritdoc/>
    public void FillCircle(Vector2 center, float radius, Color color)
    {
        if (color.A == 0 || radius <= 0) return;
        var c = Dev.PremultipliedColor.FromStraight(color);
        _fan[0] = new Dev.Vertex2D(Vector2.Transform(center, _camera), default, c);
        for (int i = 0; i <= CircleSegments; i++) _fan[i + 1] = new Dev.Vertex2D(Vector2.Transform(center + Rim[i] * radius, _camera), default, c);
        _batcher.DrawGeometry(null, _fan, _fanIndices);
    }

    /// <inheritdoc/>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (color.A == 0) return;
        var p = Dev.PremultipliedColor.FromStraight(color);
        _batcher.DrawGeometry(null, [new(Vector2.Transform(a, _camera), default, p), new(Vector2.Transform(b, _camera), default, p), new(Vector2.Transform(c, _camera), default, p)], [0, 1, 2]);
    }

    /// <inheritdoc/>
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
    {
        var d = to - from;
        if (color.A == 0 || d.LengthSquared() == 0 || thickness <= 0) return;
        var n = Vector2.Normalize(new Vector2(-d.Y, d.X)) * (thickness / 2);
        var p = Dev.PremultipliedColor.FromStraight(color);
        _batcher.DrawGeometry(null,
            [new(Vector2.Transform(from + n, _camera), default, p), new(Vector2.Transform(to + n, _camera), default, p),
             new(Vector2.Transform(to - n, _camera), default, p), new(Vector2.Transform(from - n, _camera), default, p)],
            [0, 1, 2, 0, 2, 3]);
    }

    /// <inheritdoc/>
    public void PushCamera(Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        _cameras.Push(_camera);
        _camera = camera.WorldToView;
    }

    /// <inheritdoc/>
    public void PopCamera() => _camera = _cameras.Count > 0 ? _cameras.Pop() : throw new InvalidOperationException("PopCamera without PushCamera.");

    /// <inheritdoc/>
    public void SetBlendMode(BlendMode mode)
    {
        _blend = mode;
        ApplyState();
    }

    /// <inheritdoc/>
    public void SetClip(RectF? screenRect)
    {
        _clip = screenRect is { } r ? new Dev.RectI((int)r.X, (int)r.Y, Math.Max(0, (int)r.Width), Math.Max(0, (int)r.Height)) : null;
        ApplyState();
    }

    private void ApplyState()
    {
        var blend = _blend switch
        {
            BlendMode.Additive => Dev.BlendState.Additive,
            BlendMode.Multiply => Dev.BlendState.Multiply,
            _ => Dev.BlendState.AlphaPremultiplied,
        };
        _batcher.State = _batcher.State with { Blend = blend, Scissor = _clip };
    }

    private static ushort[] BuildFanIndices()
    {
        var indices = new ushort[CircleSegments * 3];
        for (int i = 0; i < CircleSegments; i++) (indices[i * 3], indices[i * 3 + 1], indices[i * 3 + 2]) = (0, (ushort)(i + 1), (ushort)(i + 2));
        return indices;
    }
}

/// <summary>
/// A game's canvas on a graphics device, as backends built on the device contract use it: the canvas render target, the
/// textures (<see cref="Graphics"/>) and the drawing API (<see cref="Renderer"/>) over the 2D batcher, the composition of
/// the canvas into the window as the presentation says, and captures read back from the canvas.
/// </summary>
public sealed class DeviceCanvas : IDisposable
{
    private readonly Renderer2D _batcher;
    private readonly Renderer2D _composer;
    private readonly DeviceRenderer _renderer;
    private bool _disposed;

    /// <summary>Creates a canvas of <paramref name="width"/> × <paramref name="height"/> pixels on <paramref name="device"/>.</summary>
    public DeviceCanvas(Dev.IGraphicsDevice device, int width, int height)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        Target = device.CreateRenderTarget(new Dev.RenderTargetDescription(width, height, Name: "canvas"));
        Graphics = new DeviceGraphics(device);
        _batcher = new Renderer2D(device);
        _composer = new Renderer2D(device);
        _renderer = new DeviceRenderer(_batcher, Graphics, Target);
        // A new canvas is opaque black until the game clears it, as a window is.
        var clear = device.BeginCommands();
        clear.BeginPass(new Dev.PassDescription(new Dev.ColorAttachment(Target, Dev.LoadOp.Clear, Dev.StoreOp.Store, ColorF.Black)));
        clear.EndPass();
        device.Submit(clear);
    }

    /// <summary>The device everything is drawn with.</summary>
    public Dev.IGraphicsDevice Device { get; }
    /// <summary>The canvas: the render target the game draws into.</summary>
    public Dev.RenderTarget Target { get; }
    /// <summary>The textures of the drawing API, on <see cref="Device"/>.</summary>
    public DeviceGraphics Graphics { get; }
    /// <summary>The drawing API over the batcher, drawing into <see cref="Target"/>.</summary>
    public DeviceRenderer Renderer => _renderer;

    /// <summary>Starts drawing a frame into the canvas.</summary>
    public IRenderer BeginFrame()
    {
        _renderer.Begin();
        return _renderer;
    }

    /// <summary>Submits the frame drawn into the canvas.</summary>
    public void EndFrame() => _renderer.End();

    /// <summary>
    /// Draws the canvas into the window's target: the window cleared to <paramref name="letterbox"/> (opaque), then the
    /// canvas copied (its alpha ignored, so a translucent canvas never darkens) into <paramref name="frame"/> with the
    /// presentation's upscale sampler. Returns false, drawing nothing, when the window has no target this frame
    /// (minimised); the caller then skips presenting.
    /// </summary>
    public bool Compose(Presentation presentation, RectF frame, Color letterbox)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        if (!Device.TryAcquireSurfaceTarget(out var surface)) return false;
        _composer.Begin(new BatchSettings(surface, Blend: Dev.BlendState.Opaque, Sampler: presentation.Upscale,
            Clear: Dev.PremultipliedColor.FromStraight(letterbox with { A = 255 })));
        if (frame.Width > 0 && frame.Height > 0)
            _composer.Draw(Target.Color!, new RectF(0, 0, Target.Width, Target.Height),
                Matrix3x2.CreateScale(frame.Width / Target.Width, frame.Height / Target.Height) * Matrix3x2.CreateTranslation(frame.X, frame.Y), ColorF.White);
        _composer.End();
        return true;
    }

    /// <summary>The canvas as a capture stores it: every pixel opaque (alpha 255), the colour channels as stored.</summary>
    public PixelImage Capture()
    {
        var request = Device.ReadPixels(Target, Target.Bounds);
        request.Wait();
        var rgba = request.Pixels;
        var image = new PixelImage(Target.Width, Target.Height);
        for (int i = 0, n = Target.Width * Target.Height; i < n; i++)
            image[i % Target.Width, i / Target.Width] = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], 255);
        return image;
    }

    /// <summary>Writes <see cref="Capture"/> as a PNG, each canvas pixel repeated <paramref name="scale"/> times per axis.</summary>
    public void SaveCapture(string path, int scale)
    {
        var image = Capture();
        File.WriteAllBytes(path, PngEncoder.Encode(image.ToRgba(), image.Width, image.Height, Math.Max(1, scale)));
    }

    /// <summary>Releases the canvas, the batchers and every texture made with <see cref="Graphics"/>.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Graphics.DisposeAll();
        _batcher.Dispose();
        _composer.Dispose();
        Target.Dispose();
    }
}
