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
    private readonly float _scale;
    private Matrix3x2 _view = Matrix3x2.Identity;
    private BlendMode _blend;
    private Dev.RectI? _clip;
    private bool _drawing;

    /// <summary>A renderer drawing into <paramref name="target"/> (the canvas) with the textures of <paramref name="graphics"/>.</summary>
    public DeviceRenderer(Renderer2D batcher, DeviceGraphics graphics, Dev.RenderTarget target) : this(batcher, graphics, target, 1f) { }

    /// <summary>
    /// A renderer whose units are <paramref name="scale"/> target pixels each (the UI view: UI units at the window's
    /// resolution). <see cref="Width"/> and <see cref="Height"/> are the target's size in those units; the clip is in them too.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> is not positive.</exception>
    public DeviceRenderer(Renderer2D batcher, DeviceGraphics graphics, Dev.RenderTarget target, float scale)
    {
        _scale = float.IsFinite(scale) && scale > 0 ? scale : throw new ArgumentOutOfRangeException(nameof(scale), "The scale must be positive.");
        _batcher = batcher ?? throw new ArgumentNullException(nameof(batcher));
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _graphics.Uploading += () => { if (_drawing) _batcher.Flush(); };
    }

    /// <inheritdoc/>
    public int Width => (int)MathF.Floor(_target.Width / _scale + 1e-4f);
    /// <inheritdoc/>
    public int Height => (int)MathF.Floor(_target.Height / _scale + 1e-4f);

    /// <summary>The batcher's counts for the last frame (sprites, draw calls, flushes): exact and the same on every device.</summary>
    public Renderer2DStatistics Statistics => _batcher.Statistics;

    /// <summary>Starts a frame: the canvas keeps its pixels, blending is <see cref="BlendMode.Alpha"/>, no clip, no camera.</summary>
    public void Begin()
    {
        _cameras.Clear();
        _camera = Matrix3x2.Identity;
        _view = Matrix3x2.CreateScale(_scale);
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
        _batcher.Draw(texture.Native, source, world * _view, Dev.PremultipliedColor.FromStraight(tint), 0, flip);
    }

    /// <inheritdoc/>
    public void FillRect(RectF rect, Color color)
    {
        if (color.A == 0) return;
        _batcher.Draw(_batcher.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(rect.Width, rect.Height) * Matrix3x2.CreateTranslation(rect.X, rect.Y) * _view,
            Dev.PremultipliedColor.FromStraight(color));
    }

    /// <inheritdoc/>
    public void FillCircle(Vector2 center, float radius, Color color)
    {
        if (color.A == 0 || radius <= 0) return;
        var c = Dev.PremultipliedColor.FromStraight(color);
        _fan[0] = new Dev.Vertex2D(Vector2.Transform(center, _view), default, c);
        for (int i = 0; i <= CircleSegments; i++) _fan[i + 1] = new Dev.Vertex2D(Vector2.Transform(center + Rim[i] * radius, _view), default, c);
        _batcher.DrawGeometry(null, _fan, _fanIndices);
    }

    /// <inheritdoc/>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (color.A == 0) return;
        var p = Dev.PremultipliedColor.FromStraight(color);
        _batcher.DrawGeometry(null, [new(Vector2.Transform(a, _view), default, p), new(Vector2.Transform(b, _view), default, p), new(Vector2.Transform(c, _view), default, p)], [0, 1, 2]);
    }

    /// <inheritdoc/>
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
    {
        var d = to - from;
        if (color.A == 0 || d.LengthSquared() == 0 || thickness <= 0) return;
        var n = Vector2.Normalize(new Vector2(-d.Y, d.X)) * (thickness / 2);
        var p = Dev.PremultipliedColor.FromStraight(color);
        _batcher.DrawGeometry(null,
            [new(Vector2.Transform(from + n, _view), default, p), new(Vector2.Transform(to + n, _view), default, p),
             new(Vector2.Transform(to - n, _view), default, p), new(Vector2.Transform(from - n, _view), default, p)],
            [0, 1, 2, 0, 2, 3]);
    }

    /// <inheritdoc/>
    public void PushCamera(Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        _cameras.Push(_camera);
        _camera = camera.WorldToView;
        _view = _camera * Matrix3x2.CreateScale(_scale);
    }

    /// <inheritdoc/>
    public void PopCamera()
    {
        _camera = _cameras.Count > 0 ? _cameras.Pop() : throw new InvalidOperationException("PopCamera without PushCamera.");
        _view = _camera * Matrix3x2.CreateScale(_scale);
    }

    /// <inheritdoc/>
    public void SetBlendMode(BlendMode mode)
    {
        _blend = mode;
        ApplyState();
    }

    /// <inheritdoc/>
    public void SetClip(RectF? screenRect)
    {
        _clip = screenRect is { } r
            ? new Dev.RectI((int)(r.X * _scale), (int)(r.Y * _scale), Math.Max(0, (int)(r.Width * _scale)), Math.Max(0, (int)(r.Height * _scale)))
            : null;
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
    private readonly Renderer2D _uiBatcher;
    private DeviceRenderer _renderer;
    private Dev.RenderTarget? _ui;
    private DeviceRenderer? _uiRenderer;
    private float _uiScale = 1;
    private bool _uiDrawn, _uiReady, _disposed;

    /// <summary>Creates a canvas of <paramref name="width"/> × <paramref name="height"/> pixels on <paramref name="device"/>.</summary>
    public DeviceCanvas(Dev.IGraphicsDevice device, int width, int height)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        Graphics = new DeviceGraphics(device);
        _batcher = new Renderer2D(device);
        _composer = new Renderer2D(device);
        _uiBatcher = new Renderer2D(device);
        (Target, _renderer) = NewCanvas(width, height);
    }

    private (Dev.RenderTarget, DeviceRenderer) NewCanvas(int width, int height)
    {
        var target = Device.CreateRenderTarget(new Dev.RenderTargetDescription(width, height, Name: "canvas"));
        // A new canvas is opaque black until the game clears it, as a window is.
        var clear = Device.BeginCommands();
        clear.BeginPass(new Dev.PassDescription(new Dev.ColorAttachment(target, Dev.LoadOp.Clear, Dev.StoreOp.Store, ColorF.Black)));
        clear.EndPass();
        Device.Submit(clear);
        return (target, new DeviceRenderer(_batcher, Graphics, target));
    }

    /// <summary>
    /// Makes the canvas <paramref name="width"/> × <paramref name="height"/> pixels (an expanding presentation grows it
    /// with the window); a new canvas starts black. Call it between frames.
    /// </summary>
    /// <returns>Whether the size changed.</returns>
    public bool EnsureSize(int width, int height)
    {
        if (width == Target.Width && height == Target.Height) return false;
        var old = Target;
        (Target, _renderer) = NewCanvas(width, height);
        old.Dispose();
        return true;
    }

    /// <summary>The device everything is drawn with.</summary>
    public Dev.IGraphicsDevice Device { get; }
    /// <summary>The canvas: the render target the game draws into.</summary>
    public Dev.RenderTarget Target { get; private set; }

    /// <summary>The UI view's target (window pixels) once a frame drew a UI; null before.</summary>
    public Dev.RenderTarget? UiTarget => _ui;

    /// <summary>The UI view's renderer of the frame being drawn, or of the last one.</summary>
    public DeviceRenderer? UiRenderer => _uiRenderer;
    /// <summary>The textures of the drawing API, on <see cref="Device"/>.</summary>
    public DeviceGraphics Graphics { get; }
    /// <summary>The drawing API over the batcher, drawing into <see cref="Target"/>.</summary>
    public DeviceRenderer Renderer => _renderer;

    /// <summary>Starts drawing a frame into the canvas.</summary>
    public IRenderer BeginFrame()
    {
        _uiReady = false;
        _renderer.Begin();
        return _renderer;
    }

    /// <summary>Submits the frame drawn into the canvas (and the UI view, when the frame drew one).</summary>
    public void EndFrame()
    {
        _renderer.End();
        if (!_uiDrawn) return;
        _uiRenderer!.End();
        _uiDrawn = false;
        _uiReady = true;
    }

    /// <summary>
    /// Starts the frame's UI view: a transparent target of the window's size in pixels, drawn in UI units of
    /// <paramref name="scale"/> pixels, composited over the world by <see cref="Compose"/>. Call it after
    /// <see cref="BeginFrame"/>, at most once per frame.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The size or the scale is not positive.</exception>
    public IRenderer BeginUi(int pixelWidth, int pixelHeight, float scale)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0) throw new ArgumentOutOfRangeException(nameof(pixelWidth), "The UI view needs a positive size.");
        if (_ui is null || _ui.Width != pixelWidth || _ui.Height != pixelHeight || _uiScale != scale)
        {
            _ui?.Dispose();
            _ui = Device.CreateRenderTarget(new Dev.RenderTargetDescription(pixelWidth, pixelHeight, Name: "ui"));
            _uiRenderer = new DeviceRenderer(_uiBatcher, Graphics, _ui, scale);
            _uiScale = scale;
        }
        _uiRenderer!.Begin();
        _uiRenderer.Clear(Color.Transparent);
        _uiDrawn = true;
        return _uiRenderer;
    }

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
        if (_uiReady && _ui is not null)
        {
            // The UI view over the window, one UI target pixel per window pixel, premultiplied over the world.
            _composer.PushState(new RenderState(Dev.BlendState.AlphaPremultiplied, Dev.SamplerDescription.PointClamp, null));
            _composer.Draw(_ui.Color!, new RectF(0, 0, _ui.Width, _ui.Height), Matrix3x2.Identity, ColorF.White);
            _composer.PopState();
        }
        _composer.End();
        _uiReady = false;
        return true;
    }

    /// <summary>The UI view of the last frame as stored (premultiplied, transparent where nothing was drawn), or null.</summary>
    public PixelImage? CaptureUi()
    {
        if (_ui is null) return null;
        var request = Device.ReadPixels(_ui, _ui.Bounds);
        request.Wait();
        var rgba = request.Pixels;
        var image = new PixelImage(_ui.Width, _ui.Height);
        for (int i = 0, n = _ui.Width * _ui.Height; i < n; i++)
            image[i % _ui.Width, i / _ui.Width] = new Color(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
        return image;
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
        _uiBatcher.Dispose();
        _ui?.Dispose();
        Target.Dispose();
    }
}
