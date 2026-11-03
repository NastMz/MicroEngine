using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>The order in which a batch's draws reach the device (<see cref="BatchSettings.Sort"/>).</summary>
public enum SortMode
{
    /// <summary>Submission order (the default): later draws cover earlier ones.</summary>
    Deferred,
    /// <summary>Grouped by texture, then submission order: fewest draw calls, for draws that do not overlap.</summary>
    Texture,
    /// <summary>Largest depth first (painter's order), then submission order.</summary>
    BackToFront,
    /// <summary>Smallest depth first, then submission order.</summary>
    FrontToBack,
}

/// <summary>The settings of one batch, from <see cref="IRenderer2D.Begin"/> to <see cref="IRenderer2D.End"/>.</summary>
/// <param name="Target">Where the batch renders: an offscreen target or the surface target acquired this frame.</param>
/// <param name="View">Applied after every draw's transform (a camera's <c>WorldToView</c>); null is the identity.</param>
/// <param name="Blend">The batch's blend state; null is <see cref="BlendState.AlphaPremultiplied"/> (premultiplied colours).</param>
/// <param name="Sampler">The batch's sampler; null is <see cref="SamplerDescription.PointClamp"/>.</param>
/// <param name="Sort">The order draws reach the device.</param>
/// <param name="Scissor">The batch's scissor in target pixels; null draws on the whole target.</param>
/// <param name="Clear">A premultiplied colour the target is cleared to first; null keeps its contents.</param>
/// <param name="Viewport">The viewport in target pixels; null is the whole target.</param>
public readonly record struct BatchSettings(RenderTarget Target, Matrix3x2? View = null, BlendState? Blend = null,
    SamplerDescription? Sampler = null, SortMode Sort = SortMode.Deferred, RectI? Scissor = null, ColorF? Clear = null, RectI? Viewport = null);

/// <summary>The per-draw state of the 2D batcher (<see cref="IRenderer2D.State"/>): blend, sampler and scissor.</summary>
/// <param name="Blend">Blend state; the built-in presets work on every device.</param>
/// <param name="Sampler">Sampler of the draws' textures.</param>
/// <param name="Scissor">Scissor in target pixels; null draws on the whole viewport.</param>
public readonly record struct RenderState(BlendState Blend, SamplerDescription Sampler, RectI? Scissor)
{
    /// <summary>Premultiplied alpha blending, nearest clamped sampling, no scissor.</summary>
    public static RenderState Default { get; } = new(BlendState.AlphaPremultiplied, SamplerDescription.PointClamp, null);
}

/// <summary>Deterministic counts of the last batch (<see cref="IRenderer2D.Statistics"/>), the same on every device and run.</summary>
/// <param name="Sprites">Draws recorded (<c>Draw</c> and <c>DrawGeometry</c> calls).</param>
/// <param name="DrawCalls">Indexed draws sent to the device.</param>
/// <param name="Flushes">Command lists submitted (one per <c>End</c>, plus one per <c>Clear</c> or <c>Flush</c> that had work).</param>
public readonly record struct Renderer2DStatistics(int Sprites = 0, int DrawCalls = 0, int Flushes = 0);

/// <summary>
/// The 2D renderer over the graphics device, implemented once in core: it writes vertices and indices into device
/// buffers and draws them with the built-in <see cref="BuiltInPipeline.Textured"/> pipeline only, so it runs unchanged
/// on every tier. Colours (tints, vertex colours) are premultiplied, and the default blend state is premultiplied alpha.
/// </summary>
/// <remarks>
/// Rules: a draw's vertices are transformed on the CPU by its world matrix, then the pushed transforms, then the batch's
/// view, into target pixels; consecutive draws (after sorting) with the same texture and <see cref="RenderState"/> share
/// one indexed draw; a <see cref="Clear"/> or <see cref="Flush"/> submits what was recorded so far; nothing reaches the
/// device before <see cref="End"/>, <see cref="Clear"/> or <see cref="Flush"/>.
/// </remarks>
public interface IRenderer2D
{
    /// <summary>The target of the current batch, or null outside a batch.</summary>
    RenderTarget? Target { get; }
    /// <summary>The state draws are recorded with; set it, or push and pop it.</summary>
    RenderState State { get; set; }
    /// <summary>Counts of the current (or last) batch.</summary>
    Renderer2DStatistics Statistics { get; }
    /// <summary>Starts a batch.</summary>
    /// <exception cref="InvalidOperationException">A batch is already open.</exception>
    void Begin(in BatchSettings settings);
    /// <summary>
    /// Draws the <paramref name="source"/> texels of <paramref name="texture"/> as a quad of source.Width × source.Height
    /// local units placed by <paramref name="world"/>, multiplied by the premultiplied <paramref name="tint"/>.
    /// </summary>
    void Draw(Devices.Texture texture, RectF source, in Matrix3x2 world, ColorF tint, float depth = 0, SpriteFlip flip = SpriteFlip.None);
    /// <summary>Draws triangles (positions transformed like a draw's local units); a null texture draws the vertex colours.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An index selects no vertex, or more than 65,535 vertices.</exception>
    void DrawGeometry(Devices.Texture? texture, ReadOnlySpan<Vertex2D> vertices, ReadOnlySpan<ushort> indices, float depth = 0);
    /// <summary>Multiplies later draws' transforms by <paramref name="transform"/> (before the view) until <see cref="PopTransform"/>.</summary>
    void PushTransform(in Matrix3x2 transform);
    /// <summary>Returns to the transform before the last <see cref="PushTransform"/>.</summary>
    /// <exception cref="InvalidOperationException">Nothing was pushed.</exception>
    void PopTransform();
    /// <summary>Saves <see cref="State"/> and sets <paramref name="state"/> until <see cref="PopState"/>.</summary>
    void PushState(in RenderState state);
    /// <summary>Restores the state saved by the last <see cref="PushState"/>.</summary>
    /// <exception cref="InvalidOperationException">Nothing was pushed.</exception>
    void PopState();
    /// <summary>Submits what was recorded, then clears the whole target (viewport and scissor ignored) to a premultiplied colour.</summary>
    void Clear(ColorF color);
    /// <summary>Submits what was recorded so far (for example before reading the target back).</summary>
    void Flush();
    /// <summary>Submits what remains and ends the batch.</summary>
    /// <exception cref="InvalidOperationException">No batch is open, or a pushed transform or state was not popped.</exception>
    void End();
}

/// <summary>Mirrors a sprite's texels within its quad (<see cref="IRenderer2D.Draw"/>).</summary>
[Flags]
public enum SpriteFlip
{
    /// <summary>No mirroring.</summary>
    None = 0,
    /// <summary>Mirrored horizontally.</summary>
    Horizontal = 1,
    /// <summary>Mirrored vertically.</summary>
    Vertical = 2,
}

/// <summary>The engine's 2D batcher (<see cref="IRenderer2D"/>) over one graphics device.</summary>
public sealed class Renderer2D : IRenderer2D, IDisposable
{
    private const int MaxVerticesPerDraw = ushort.MaxValue;
    private readonly Devices.IGraphicsDevice _device;
    private readonly Devices.Texture _white;
    private readonly List<Vertex2D> _vertices = [];
    private readonly List<ushort> _indices = [];
    private readonly List<Item> _items = [];
    private readonly List<RenderState> _states = [];
    private readonly Stack<Matrix3x2> _transforms = new();
    private readonly Stack<RenderState> _savedStates = new();
    private readonly List<Vertex2D> _scratchVertices = [];
    private readonly List<ushort> _scratchIndices = [];
    private GpuBuffer? _vertexBuffer, _indexBuffer;
    private BatchSettings _settings;
    private Matrix3x2 _transform = Matrix3x2.Identity;
    private RenderState _state = RenderState.Default;
    private ColorF? _pendingClear;
    private Renderer2DStatistics _statistics;
    private bool _begun, _disposed;

    /// <summary>Creates the batcher (and its 1 × 1 white texture, which untextured draws sample).</summary>
    public Renderer2D(Devices.IGraphicsDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _white = device.CreateTexture(new TextureDescription(1, 1, Name: "renderer2d-white"), [255, 255, 255, 255]);
    }

    /// <summary>The device the batcher draws with.</summary>
    public Devices.IGraphicsDevice Device => _device;

    /// <summary>The 1 × 1 opaque white texture untextured draws sample.</summary>
    public Devices.Texture White => _white;

    /// <inheritdoc />
    public RenderTarget? Target => _begun ? _settings.Target : null;

    /// <inheritdoc />
    public RenderState State
    {
        get => _state;
        set => _state = value with { Blend = value.Blend ?? throw new ArgumentNullException(nameof(value), "a render state needs a blend state") };
    }

    /// <inheritdoc />
    public Renderer2DStatistics Statistics => _statistics;

    /// <inheritdoc />
    public void Begin(in BatchSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_begun) throw new InvalidOperationException("Begin called twice without End");
        ArgumentNullException.ThrowIfNull(settings.Target);
        _settings = settings;
        _begun = true;
        _transform = Matrix3x2.Identity;
        _transforms.Clear();
        _savedStates.Clear();
        _state = new RenderState(settings.Blend ?? BlendState.AlphaPremultiplied, settings.Sampler ?? SamplerDescription.PointClamp, settings.Scissor);
        _pendingClear = settings.Clear;
        _statistics = default;
    }

    /// <inheritdoc />
    public void Draw(Devices.Texture texture, RectF source, in Matrix3x2 world, ColorF tint, float depth = 0, SpriteFlip flip = SpriteFlip.None)
    {
        Begun();
        ArgumentNullException.ThrowIfNull(texture);
        var m = world * _transform * View;
        float u0 = source.X / texture.Width, v0 = source.Y / texture.Height;
        float u1 = (source.X + source.Width) / texture.Width, v1 = (source.Y + source.Height) / texture.Height;
        if ((flip & SpriteFlip.Horizontal) != 0) (u0, u1) = (u1, u0);
        if ((flip & SpriteFlip.Vertical) != 0) (v0, v1) = (v1, v0);
        int start = _vertices.Count;
        _vertices.Add(new Vertex2D(Vector2.Transform(Vector2.Zero, m), new Vector2(u0, v0), tint));
        _vertices.Add(new Vertex2D(Vector2.Transform(new Vector2(source.Width, 0), m), new Vector2(u1, v0), tint));
        _vertices.Add(new Vertex2D(Vector2.Transform(new Vector2(source.Width, source.Height), m), new Vector2(u1, v1), tint));
        _vertices.Add(new Vertex2D(Vector2.Transform(new Vector2(0, source.Height), m), new Vector2(u0, v1), tint));
        int indexStart = _indices.Count;
        _indices.AddRange([0, 1, 2, 0, 2, 3]);
        Record(texture, depth, start, 4, indexStart, 6);
    }

    /// <inheritdoc />
    public void DrawGeometry(Devices.Texture? texture, ReadOnlySpan<Vertex2D> vertices, ReadOnlySpan<ushort> indices, float depth = 0)
    {
        Begun();
        if (vertices.Length > MaxVerticesPerDraw) throw new ArgumentOutOfRangeException(nameof(vertices), $"at most {MaxVerticesPerDraw} vertices per call");
        foreach (var i in indices)
            if (i >= vertices.Length) throw new ArgumentOutOfRangeException(nameof(indices), $"index {i} selects none of the {vertices.Length} vertices");
        if (indices.Length % 3 != 0) throw new ArgumentException("indices come in whole triangles (a multiple of 3)", nameof(indices));
        var m = _transform * View;
        int start = _vertices.Count;
        foreach (var v in vertices) _vertices.Add(v with { Position = Vector2.Transform(v.Position, m) });
        int indexStart = _indices.Count;
        foreach (var i in indices) _indices.Add(i);
        Record(texture ?? _white, depth, start, vertices.Length, indexStart, indices.Length);
    }

    /// <inheritdoc />
    public void PushTransform(in Matrix3x2 transform)
    {
        Begun();
        _transforms.Push(_transform);
        _transform = transform * _transform;
    }

    /// <inheritdoc />
    public void PopTransform()
    {
        Begun();
        _transform = _transforms.Count > 0 ? _transforms.Pop() : throw new InvalidOperationException("PopTransform without PushTransform");
    }

    /// <inheritdoc />
    public void PushState(in RenderState state)
    {
        Begun();
        _savedStates.Push(_state);
        State = state;
    }

    /// <inheritdoc />
    public void PopState()
    {
        Begun();
        _state = _savedStates.Count > 0 ? _savedStates.Pop() : throw new InvalidOperationException("PopState without PushState");
    }

    /// <inheritdoc />
    public void Clear(ColorF color)
    {
        Begun();
        if (_items.Count > 0 || _pendingClear is not null) Submit();
        _pendingClear = color;
    }

    /// <inheritdoc />
    public void Flush()
    {
        Begun();
        if (_items.Count > 0 || _pendingClear is not null) Submit();
    }

    /// <inheritdoc />
    public void End()
    {
        Begun();
        if (_transforms.Count != 0) throw new InvalidOperationException("a pushed transform was not popped before End");
        if (_savedStates.Count != 0) throw new InvalidOperationException("a pushed state was not popped before End");
        try
        {
            if (_items.Count > 0 || _pendingClear is not null) Submit();
        }
        finally
        {
            _begun = false;
            _items.Clear();
            _vertices.Clear();
            _indices.Clear();
            _states.Clear();
        }
    }

    /// <summary>Releases the batcher's buffers and white texture.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _white.Dispose();
    }

    private Matrix3x2 View => _settings.View ?? Matrix3x2.Identity;

    private void Record(Devices.Texture texture, float depth, int vertexStart, int vertexCount, int indexStart, int indexCount)
    {
        int state = _states.IndexOf(_state);
        if (state < 0)
        {
            _states.Add(_state);
            state = _states.Count - 1;
        }
        _items.Add(new Item(texture, state, depth, _items.Count, vertexStart, vertexCount, indexStart, indexCount));
        _statistics = _statistics with { Sprites = _statistics.Sprites + 1 };
    }

    /// <summary>Records one command list: the buffer updates, one pass, one draw per run of texture and state.</summary>
    private void Submit()
    {
        var target = _settings.Target;
        IEnumerable<Item> ordered = _settings.Sort switch
        {
            SortMode.Texture => _items.OrderBy(i => i.Texture.Handle.Index).ThenBy(i => i.Order),
            SortMode.BackToFront => _items.OrderByDescending(i => i.Depth).ThenBy(i => i.Order),
            SortMode.FrontToBack => _items.OrderBy(i => i.Depth).ThenBy(i => i.Order),
            _ => _items,
        };
        // Lay the vertices out in draw order, grouped into runs that share texture and state and fit 16-bit indices.
        _scratchVertices.Clear();
        _scratchIndices.Clear();
        var runs = new List<Run>();
        foreach (var item in ordered)
        {
            bool join = runs.Count > 0 && ReferenceEquals(runs[^1].Texture, item.Texture) && runs[^1].State == item.State
                && runs[^1].VertexCount + item.VertexCount <= MaxVerticesPerDraw;
            if (!join) runs.Add(new Run(item.Texture, item.State, _scratchIndices.Count, 0, _scratchVertices.Count, 0));
            var run = runs[^1];
            int relative = run.VertexCount;
            for (int v = 0; v < item.VertexCount; v++) _scratchVertices.Add(_vertices[item.VertexStart + v]);
            for (int i = 0; i < item.IndexCount; i++) _scratchIndices.Add((ushort)(relative + _indices[item.IndexStart + i]));
            runs[^1] = run with { VertexCount = run.VertexCount + item.VertexCount, IndexCount = run.IndexCount + item.IndexCount };
        }
        var commands = _device.BeginCommands();
        if (_scratchVertices.Count > 0)
        {
            if (_scratchIndices.Count % 2 == 1) _scratchIndices.Add(0); // keep index uploads 4-byte sized
            _vertexBuffer = Ensure(_vertexBuffer, _scratchVertices.Count * Vertex2D.SizeInBytes, BufferUsage.Vertex, "renderer2d-vertices");
            _indexBuffer = Ensure(_indexBuffer, _scratchIndices.Count * sizeof(ushort), BufferUsage.Index, "renderer2d-indices");
            commands.UpdateBuffer<Vertex2D>(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_scratchVertices));
            commands.UpdateBuffer<ushort>(_indexBuffer, 0, CollectionsMarshal.AsSpan(_scratchIndices));
        }
        var clear = _pendingClear;
        commands.BeginPass(new PassDescription(new ColorAttachment(target, clear is null ? LoadOp.Load : LoadOp.Clear, StoreOp.Store, clear ?? default)));
        if (_settings.Viewport is { } viewport) commands.SetViewport(viewport);
        int draws = 0;
        if (runs.Count > 0)
        {
            commands.SetVertices(_vertexBuffer!);
            commands.SetIndices(_indexBuffer!, IndexSize.Bits16);
            Pipeline? pipeline = null;
            RectI? scissor = null;
            bool scissorSet = false;
            Devices.Texture? texture = null;
            SamplerDescription sampler = default;
            foreach (var run in runs)
            {
                var state = _states[run.State];
                var next = _device.GetBuiltInPipeline(BuiltInPipeline.Textured, state.Blend, target.ColorFormat);
                if (!ReferenceEquals(next, pipeline)) commands.SetPipeline(pipeline = next);
                if (!scissorSet || scissor != state.Scissor)
                {
                    commands.SetScissor(state.Scissor);
                    scissor = state.Scissor;
                    scissorSet = true;
                }
                if (!ReferenceEquals(texture, run.Texture) || sampler != state.Sampler)
                {
                    commands.SetTexture(0, run.Texture, _device.GetSampler(state.Sampler));
                    texture = run.Texture;
                    sampler = state.Sampler;
                }
                commands.DrawIndexed(run.IndexCount, run.FirstIndex, run.BaseVertex);
                draws++;
            }
        }
        commands.EndPass();
        _device.Submit(commands);
        _statistics = _statistics with { DrawCalls = _statistics.DrawCalls + draws, Flushes = _statistics.Flushes + 1 };
        _pendingClear = null;
        _items.Clear();
        _vertices.Clear();
        _indices.Clear();
        _states.Clear();
    }

    private GpuBuffer Ensure(GpuBuffer? buffer, int bytes, BufferUsage usage, string name)
    {
        if (buffer is not null && buffer.SizeInBytes >= bytes) return buffer;
        buffer?.Dispose(); // released by the device once submitted work no longer uses it
        return _device.CreateBuffer(new BufferDescription(Math.Max(bytes, Math.Max(4096, (buffer?.SizeInBytes ?? 0) * 2)), usage, name));
    }

    private void Begun()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_begun) throw new InvalidOperationException("call Begin first");
    }

    private readonly record struct Item(Devices.Texture Texture, int State, float Depth, int Order, int VertexStart, int VertexCount, int IndexStart, int IndexCount);

    private readonly record struct Run(Devices.Texture Texture, int State, int FirstIndex, int IndexCount, int BaseVertex, int VertexCount);
}
