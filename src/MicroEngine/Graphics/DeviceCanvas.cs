using System.Numerics;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>
/// The textures, render targets and materials of the drawing API (<see cref="IGraphicsDevice"/>) made on a graphics
/// device of the device contract: each texture is a device texture whose straight-alpha pixels are premultiplied when
/// they are uploaded, and its <see cref="Texture.Filter"/> chooses the sampler it is drawn with. A device texture or
/// render target the game made itself becomes a texture of the drawing API with <see cref="Wrap(Dev.Texture, TextureFilter?, bool)"/>.
/// Backends built on a device use it as their <see cref="Core.IPlatform.Graphics"/>.
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
        return Track(new DeviceTexture(this, native, null, filter, true));
    }

    /// <inheritdoc/>
    public Texture CreateTexture(PixelImage image) => CreateTexture(image, DefaultFilter);

    /// <inheritdoc/>
    public RenderTarget2D CreateRenderTarget(int width, int height) => CreateRenderTarget(width, height, DefaultFilter);

    /// <inheritdoc/>
    public RenderTarget2D CreateRenderTarget(int width, int height, TextureFilter filter)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "A render target needs a positive size.");
        var native = Device.CreateRenderTarget(new Dev.RenderTargetDescription(width, height));
        // A new target is transparent until the game draws into it.
        var clear = Device.BeginCommands();
        clear.BeginPass(new Dev.PassDescription(new Dev.ColorAttachment(native, Dev.LoadOp.Clear, Dev.StoreOp.Store, default)));
        clear.EndPass();
        Device.Submit(clear);
        return new RenderTarget2D(Track(new DeviceTexture(this, native.Color!, native, filter, true)));
    }

    /// <inheritdoc/>
    public Material2D CreateMaterial(Dev.CompiledShader fragment, MaterialFallback fallback = default) => Material2D.Create(Device, fragment, fallback);

    /// <summary>
    /// A device texture the game created itself (in any format the device samples: a single channel, a compressed
    /// format, with mip levels) as a texture of the drawing API, so <see cref="IRenderer"/> draws it and a
    /// <see cref="Sprite"/> can cut from it. Its pixels are taken as premultiplied, as the device holds them.
    /// </summary>
    /// <param name="native">A texture of <see cref="Device"/>, not a render target's colour (wrap the target instead).</param>
    /// <param name="filter">How it is sampled; null for <see cref="DefaultFilter"/>.</param>
    /// <param name="ownsNative">Whether disposing the returned texture disposes <paramref name="native"/> too.</param>
    /// <exception cref="ArgumentException">A texture of another device, or a render target's colour texture.</exception>
    /// <exception cref="ObjectDisposedException">A disposed texture.</exception>
    public Texture Wrap(Dev.Texture native, TextureFilter? filter = null, bool ownsNative = false)
    {
        ArgumentNullException.ThrowIfNull(native);
        if (!ReferenceEquals(native.Device, Device)) throw new ArgumentException($"'{native.Name}' belongs to another graphics device.", nameof(native));
        ObjectDisposedException.ThrowIf(native.IsDisposed, native);
        if (native.OwnerTarget is not null) throw new ArgumentException($"'{native.Name}' is a render target's picture; wrap the render target.", nameof(native));
        return Track(new DeviceTexture(this, native, null, filter ?? DefaultFilter, ownsNative));
    }

    /// <summary>
    /// A device render target the game created itself as a render target of the drawing API: <see cref="IRenderer.PushTarget"/>
    /// draws into it and its texture is drawn like any other.
    /// </summary>
    /// <param name="native">An offscreen target of <see cref="Device"/>.</param>
    /// <param name="filter">How its texture is sampled; null for <see cref="DefaultFilter"/>.</param>
    /// <param name="ownsNative">Whether disposing the returned target disposes <paramref name="native"/> too.</param>
    /// <exception cref="ArgumentException">A target of another device, or the surface target.</exception>
    /// <exception cref="ObjectDisposedException">A disposed target.</exception>
    public RenderTarget2D Wrap(Dev.RenderTarget native, TextureFilter? filter = null, bool ownsNative = false)
    {
        ArgumentNullException.ThrowIfNull(native);
        if (!ReferenceEquals(native.Device, Device)) throw new ArgumentException($"'{native.Name}' belongs to another graphics device.", nameof(native));
        ObjectDisposedException.ThrowIf(native.IsDisposed, native);
        if (native.Color is not { } color) throw new ArgumentException("The surface target cannot be sampled, so it is not a render target of the drawing API.", nameof(native));
        return new RenderTarget2D(Track(new DeviceTexture(this, color, native, filter ?? DefaultFilter, ownsNative)));
    }

    private DeviceTexture Track(DeviceTexture texture)
    {
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

/// <summary>
/// A texture of the drawing API backed by a device texture (<see cref="DeviceGraphics"/>): an uploaded image, a render
/// target's picture, or a device texture the game wrapped.
/// </summary>
public sealed class DeviceTexture : Texture
{
    private readonly DeviceGraphics _graphics;
    private readonly bool _ownsNative;

    internal DeviceTexture(DeviceGraphics graphics, Dev.Texture native, Dev.RenderTarget? target, TextureFilter filter, bool ownsNative)
        : base(graphics, native.Width, native.Height, filter)
        => (_graphics, Native, Target, _ownsNative) = (graphics, native, target, ownsNative);

    /// <summary>The device texture holding the premultiplied pixels.</summary>
    public override Dev.Texture Native { get; }

    /// <summary>The device render target this texture is the picture of, or null for an ordinary texture.</summary>
    public Dev.RenderTarget? Target { get; }

    /// <inheritdoc/>
    public override bool IsRenderTarget => Target is not null;

    /// <summary>The memory of every mip level in the device texture's format.</summary>
    public override long SizeInBytes
    {
        get
        {
            long bytes = 0;
            int width = Width, height = Height;
            for (int level = 0; level < Math.Max(1, Native.Description.MipLevels); level++)
            {
                bytes += Dev.TextureFormats.ByteCount(Native.Format, width, height);
                (width, height) = (Math.Max(1, width / 2), Math.Max(1, height / 2));
            }
            return bytes;
        }
    }

    /// <summary>The sampler the texture's filter draws with (clamped at the edges).</summary>
    public Dev.SamplerDescription Sampler => Filter == TextureFilter.Linear ? Dev.SamplerDescription.LinearClamp : Dev.SamplerDescription.PointClamp;

    /// <inheritdoc/>
    protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
    {
        if (Target is not null) throw new InvalidOperationException("A render target's pixels are drawn (IRenderer.PushTarget), not updated.");
        if (Native.Format != Dev.TextureFormat.Rgba8Unorm) throw new InvalidOperationException($"Colours can only update an 8-bit RGBA texture; this one is {Native.Format}.");
        _graphics.Upload(this, x, y, width, height, pixels);
    }

    /// <inheritdoc/>
    protected override void ApplyFilter(TextureFilter filter) { } // the sampler is chosen per draw

    /// <inheritdoc/>
    protected override void Release()
    {
        _graphics.Forget(this);
        if (!_ownsNative) return;
        // A target owns its colour texture: the two go together.
        if (Target is not null) Target.Dispose();
        else Native.Dispose();
    }
}

/// <summary>
/// The drawing API (<see cref="IRenderer"/>) as a façade over the 2D batcher (<see cref="Renderer2D"/>) on a graphics
/// device: every call is recorded into the batcher, and the frame reaches the device in as few draw calls as the draws'
/// textures and states allow. Straight-alpha colours are premultiplied on the way in, so the blend modes keep their
/// written equations (see <see cref="IRenderer.SetBlendMode"/>). States and materials (<see cref="PushState"/>) become
/// the batcher's state, and a pushed render target (<see cref="PushTarget"/>) becomes the batch's target, so a frame
/// that uses them is still one batch in recording order.
/// </summary>
public sealed class DeviceRenderer : IRenderer
{
    private const int CircleSegments = 36;
    private static readonly Vector2[] Rim = [.. Enumerable.Range(0, CircleSegments + 1).Select(i => new Vector2(MathF.Cos(i * MathF.Tau / CircleSegments), MathF.Sin(i * MathF.Tau / CircleSegments)))];
    private readonly Renderer2D _batcher;
    private readonly DeviceGraphics _graphics;
    private readonly Dev.RenderTarget _canvas;
    private readonly float _canvasScale;
    private readonly Stack<Matrix3x2> _cameras = new();
    private readonly Stack<Drawing> _states = new();
    private readonly Stack<Saved> _targets = new();
    private readonly Dev.Vertex2D[] _fan = new Dev.Vertex2D[CircleSegments + 2];
    private readonly ushort[] _fanIndices = BuildFanIndices();
    private Dev.RenderTarget _target;
    private float _scale;
    private Matrix3x2 _camera = Matrix3x2.Identity;
    private Matrix3x2 _view = Matrix3x2.Identity;
    private Drawing _drawing = Drawing.Default;
    private Dev.RectI? _clip;
    // What the current material's fallback does to draws, when its shader cannot run on this device.
    private bool _hidden;
    private Color? _tint;
    private Material2D? _unsupported;
    private bool _open;

    /// <summary>What draws are made with: the blend state, the sampler that replaces each texture's own, and the material.</summary>
    private readonly record struct Drawing(Dev.BlendState Blend, Dev.SamplerDescription? Sampler, Material2D? Material)
    {
        public static Drawing Default => new(Dev.BlendState.AlphaPremultiplied, null, null);
    }

    /// <summary>What a pushed target replaced, restored when it is popped.</summary>
    private readonly record struct Saved(Dev.RenderTarget Target, float Scale, Matrix3x2 Camera, Dev.RectI? Clip, Drawing Drawing, int Cameras, int States);

    /// <summary>A renderer drawing into <paramref name="target"/> (the canvas) with the textures of <paramref name="graphics"/>.</summary>
    public DeviceRenderer(Renderer2D batcher, DeviceGraphics graphics, Dev.RenderTarget target) : this(batcher, graphics, target, 1f) { }

    /// <summary>
    /// A renderer whose units are <paramref name="scale"/> target pixels each (the UI view: UI units at the window's
    /// resolution). <see cref="Width"/> and <see cref="Height"/> are the target's size in those units; the clip is in them too.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> is not positive.</exception>
    public DeviceRenderer(Renderer2D batcher, DeviceGraphics graphics, Dev.RenderTarget target, float scale)
    {
        _canvasScale = _scale = float.IsFinite(scale) && scale > 0 ? scale : throw new ArgumentOutOfRangeException(nameof(scale), "The scale must be positive.");
        _batcher = batcher ?? throw new ArgumentNullException(nameof(batcher));
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _canvas = _target = target ?? throw new ArgumentNullException(nameof(target));
        _graphics.Uploading += () => { if (_open) _batcher.Flush(); };
    }

    /// <inheritdoc/>
    public int Width => (int)MathF.Floor(_target.Width / _scale + 1e-4f);
    /// <inheritdoc/>
    public int Height => (int)MathF.Floor(_target.Height / _scale + 1e-4f);

    /// <summary>The batcher's counts for the last frame (sprites, draw calls, flushes): exact and the same on every device.</summary>
    public Renderer2DStatistics Statistics => _batcher.Statistics;

    /// <summary>
    /// The device render target this renderer is drawing into: the canvas (or the UI view's target), or the target
    /// pushed with <see cref="PushTarget"/>. Valid only during the frame, between <see cref="Begin"/> and
    /// <see cref="End"/>: the canvas can be replaced between frames (an expanding presentation resizes it), so keep the
    /// renderer, not the target. Work submitted to the device into this target between two <see cref="Flush"/> calls
    /// (a 3D pass) lands between the 2D drawn before and after it.
    /// </summary>
    public Dev.RenderTarget Target => _target;

    /// <summary>
    /// Submits to the device everything this renderer recorded since the frame began or since the last flush (the frame's
    /// <see cref="Clear"/> included, so a clear always lands before the first flush's other work) and keeps the frame
    /// open: state, camera and clip stay as they are, and draws recorded after the flush are submitted after anything
    /// submitted to the device in between. Use it to put device work (a 3D pass into <see cref="Target"/>) between 2D
    /// draws; materials and render targets of the drawing API need no flush. Outside a frame it does nothing.
    /// </summary>
    public void Flush()
    {
        if (_open) _batcher.Flush();
    }

    /// <summary>Starts a frame: the canvas keeps its pixels, blending is <see cref="BlendMode.Alpha"/>, no clip, no camera, no state.</summary>
    public void Begin()
    {
        _cameras.Clear();
        _states.Clear();
        _targets.Clear();
        (_target, _scale) = (_canvas, _canvasScale);
        _camera = Matrix3x2.Identity;
        _view = Matrix3x2.CreateScale(_scale);
        _drawing = Drawing.Default;
        _clip = null;
        _batcher.Begin(new BatchSettings(_target));
        _open = true;
        ApplyState();
    }

    /// <summary>Ends the frame and submits it.</summary>
    /// <exception cref="InvalidOperationException">A camera, a state or a target pushed this frame was not popped.</exception>
    public void End()
    {
        _open = false;
        _batcher.End();
        if (_targets.Count != 0) throw new InvalidOperationException("Unbalanced PushTarget/PopTarget in this frame.");
        if (_states.Count != 0) throw new InvalidOperationException("Unbalanced PushState/PopState in this frame.");
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
        if (texture.Target is not null && ReferenceEquals(texture.Target, _target))
            throw new InvalidOperationException("A render target cannot be drawn into itself. Pop it first, or draw it into another target.");
        if (!Shown(ref tint)) return;
        var sampler = _drawing.Sampler ?? texture.Sampler;
        var state = _batcher.State;
        if (state.Sampler != sampler) _batcher.State = state with { Sampler = sampler };
        _batcher.Draw(texture.Native, source, world * _view, Dev.PremultipliedColor.FromStraight(tint), 0, flip);
    }

    /// <summary>
    /// Applies the fallback of a material whose shader cannot run here to a draw's colour, after checking the material
    /// as a device that runs it would. Returns whether the draw is made at all.
    /// </summary>
    private bool Shown(ref Color color)
    {
        if (_unsupported is null) return true;
        _unsupported.Validate();
        if (_hidden) return false;
        if (_tint is { } t) color = new Color((byte)((color.R * t.R + 127) / 255), (byte)((color.G * t.G + 127) / 255), (byte)((color.B * t.B + 127) / 255), (byte)((color.A * t.A + 127) / 255));
        return true;
    }

    /// <inheritdoc/>
    public void FillRect(RectF rect, Color color)
    {
        if (!Shown(ref color) || color.A == 0) return;
        _batcher.Draw(_batcher.White, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(rect.Width, rect.Height) * Matrix3x2.CreateTranslation(rect.X, rect.Y) * _view,
            Dev.PremultipliedColor.FromStraight(color));
    }

    /// <inheritdoc/>
    public void FillCircle(Vector2 center, float radius, Color color)
    {
        if (!Shown(ref color) || color.A == 0 || radius <= 0) return;
        var c = Dev.PremultipliedColor.FromStraight(color);
        _fan[0] = new Dev.Vertex2D(Vector2.Transform(center, _view), default, c);
        for (int i = 0; i <= CircleSegments; i++) _fan[i + 1] = new Dev.Vertex2D(Vector2.Transform(center + Rim[i] * radius, _view), default, c);
        _batcher.DrawGeometry(null, _fan, _fanIndices);
    }

    /// <inheritdoc/>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (!Shown(ref color) || color.A == 0) return;
        var p = Dev.PremultipliedColor.FromStraight(color);
        _batcher.DrawGeometry(null, [new(Vector2.Transform(a, _view), default, p), new(Vector2.Transform(b, _view), default, p), new(Vector2.Transform(c, _view), default, p)], [0, 1, 2]);
    }

    /// <inheritdoc/>
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
    {
        var d = to - from;
        if (!Shown(ref color) || color.A == 0 || d.LengthSquared() == 0 || thickness <= 0) return;
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
        if (_cameras.Count <= (_targets.Count > 0 ? _targets.Peek().Cameras : 0)) throw new InvalidOperationException("PopCamera without PushCamera.");
        _camera = _cameras.Pop();
        _view = _camera * Matrix3x2.CreateScale(_scale);
    }

    /// <inheritdoc/>
    public void SetBlendMode(BlendMode mode)
    {
        _drawing = _drawing with
        {
            Blend = mode switch
            {
                BlendMode.Additive => Dev.BlendState.Additive,
                BlendMode.Multiply => Dev.BlendState.Multiply,
                _ => Dev.BlendState.AlphaPremultiplied,
            },
        };
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

    /// <inheritdoc/>
    public void PushState(in DrawState state)
    {
        if (state.Material is { } material)
        {
            ObjectDisposedException.ThrowIf(material.IsDisposed, material);
            if (material.IsSupported && !ReferenceEquals(material.Device, _graphics.Device))
                throw new ArgumentException("The material was created by another graphics device; create it with the game's Graphics or Assets.", nameof(state));
        }
        if (state.Blend is { } blend && !_graphics.Device.Capabilities.BuiltInBlendStates.Contains(blend) && !_graphics.Device.Capabilities.Has(Dev.GraphicsFeatures.CustomBlend))
            throw new Dev.GraphicsCapabilityException("PushState(blend)", _graphics.Device.Capabilities.Backend, Dev.GraphicsFeatures.CustomBlend);
        _states.Push(_drawing);
        _drawing = new Drawing(state.Blend ?? _drawing.Blend, state.Sampler ?? _drawing.Sampler, state.Material ?? _drawing.Material);
        ApplyState();
    }

    /// <inheritdoc/>
    public void PopState()
    {
        if (_states.Count <= (_targets.Count > 0 ? _targets.Peek().States : 0)) throw new InvalidOperationException("PopState without PushState.");
        _drawing = _states.Pop();
        ApplyState();
    }

    /// <inheritdoc/>
    public void PushTarget(RenderTarget2D target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var texture = Texture.CheckDrawable<DeviceTexture>(target.Texture, _graphics);
        var native = texture.Target ?? throw new ArgumentException("The texture is not a render target of this graphics device.", nameof(target));
        if (ReferenceEquals(native, _target) || _targets.Any(saved => ReferenceEquals(saved.Target, native)))
            throw new InvalidOperationException("The render target is already being drawn into; pop it before pushing it again.");
        _targets.Push(new Saved(_target, _scale, _camera, _clip, _drawing, _cameras.Count, _states.Count));
        (_target, _scale) = (native, 1f);
        _camera = _view = Matrix3x2.Identity;
        _clip = null;
        _drawing = Drawing.Default;
        if (_open) _batcher.SetTarget(native);
        ApplyState();
    }

    /// <inheritdoc/>
    public void PopTarget()
    {
        if (_targets.Count == 0) throw new InvalidOperationException("PopTarget without PushTarget.");
        var saved = _targets.Peek();
        if (_states.Count != saved.States) throw new InvalidOperationException("A state pushed inside the render target was not popped before PopTarget.");
        if (_cameras.Count != saved.Cameras) throw new InvalidOperationException("A camera pushed inside the render target was not popped before PopTarget.");
        _targets.Pop();
        (_target, _scale, _camera, _clip, _drawing) = (saved.Target, saved.Scale, saved.Camera, saved.Clip, saved.Drawing);
        _view = _camera * Matrix3x2.CreateScale(_scale);
        if (_open) _batcher.SetTarget(_target);
        ApplyState();
    }

    private void ApplyState()
    {
        var material = _drawing.Material;
        var blend = _drawing.Blend;
        (_unsupported, _hidden, _tint) = (null, false, null);
        if (material is { IsSupported: false })
        {
            // The device cannot run the shader: the draws are made the usual way, with the material's fallback.
            var fallback = material.Fallback;
            (_unsupported, _hidden, _tint) = (material, fallback.Hidden, fallback.Tint);
            blend = fallback.Blend ?? blend;
            material = null;
        }
        if (!_open) return;
        _batcher.State = _batcher.State with { Blend = blend, Scissor = _clip, Material = material };
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
    private CanvasPresenter? _presenter;
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
    /// presentation's upscale sampler, enlarged first by its prescale when it has one (<see cref="Presentation.Prescale"/>,
    /// through a <see cref="CanvasPresenter"/>), and the frame's UI view over everything. Returns false, drawing nothing,
    /// when the window has no target this frame (minimised); the caller then skips presenting.
    /// </summary>
    public bool Compose(Presentation presentation, RectF frame, Color letterbox)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        if (presentation.Prescale != 1)
        {
            _presenter ??= new CanvasPresenter(Device);
            bool presented = _presenter.Present(presentation, Target, _uiReady ? _ui : null, frame, letterbox);
            if (presented) _uiReady = false;
            return presented;
        }
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
        _presenter?.Dispose();
        _uiBatcher.Dispose();
        _ui?.Dispose();
        Target.Dispose();
    }
}
