using System.Numerics;
using System.Runtime.InteropServices;

namespace Arch1.Graphics.Batching;

/// <summary>Settings of one batch (Begin..End).</summary>
/// <param name="Target">Where the batch renders (offscreen or the acquired surface target).</param>
/// <param name="View">View transform applied after each world transform (null = identity).</param>
/// <param name="Blend">Blend state (null = premultiplied alpha).</param>
/// <param name="Sampler">Sampler for every texture of the batch (null = point clamp).</param>
/// <param name="Scissor">Scissor in target pixels (null = none).</param>
/// <param name="Clear">Clear colour at the start of the batch (null = keep the target's contents).</param>
public readonly record struct BatchSettings(RenderTarget Target, Matrix3x2? View = null, BlendState? Blend = null,
    SamplerDescription? Sampler = null, RectI? Scissor = null, ColorF? Clear = null);

/// <summary>
/// The 2D batcher, implemented once over <see cref="IGraphicsDevice"/>: it writes vertices and indices into device
/// buffers and draws them with built-in pipelines only, so it runs unchanged on every tier. Vertices are transformed on
/// the CPU (world, then view) into target pixels. Draws are kept in submission order; consecutive draws with the same
/// texture and pipeline share one indexed draw.
/// </summary>
public sealed class Renderer2D : IDisposable
{
    private readonly IGraphicsDevice _device;
    private readonly Texture _white;
    private readonly List<Vertex2D> _vertices = [];
    private readonly List<ushort> _indices = [];
    private readonly List<Batch> _batches = [];
    private GpuBuffer? _vertexBuffer, _indexBuffer;
    private BatchSettings _settings;
    private Matrix3x2 _view;
    private bool _begun;

    /// <summary>Creates the batcher (and its 1×1 white texture for untextured geometry).</summary>
    public Renderer2D(IGraphicsDevice device)
    {
        _device = device;
        _white = device.CreateTexture(new TextureDescription(1, 1, Name: "batcher-white"), [255, 255, 255, 255]);
    }

    /// <summary>Indexed draws issued by the last <see cref="End"/> (a deterministic counter).</summary>
    public int LastDrawCalls { get; private set; }

    /// <summary>Starts a batch.</summary>
    public void Begin(in BatchSettings settings)
    {
        if (_begun) throw new InvalidOperationException("Begin called twice without End");
        ArgumentNullException.ThrowIfNull(settings.Target);
        _settings = settings;
        _view = settings.View ?? Matrix3x2.Identity;
        _begun = true;
    }

    /// <summary>
    /// Draws the <paramref name="source"/> region of a texture as a quad of source.Width×source.Height local units,
    /// placed by <paramref name="world"/> (then the batch view), multiplied by the premultiplied <paramref name="tint"/>.
    /// </summary>
    public void Draw(Texture texture, RectF source, Matrix3x2 world, ColorF tint)
    {
        Begun();
        var m = world * _view;
        float u0 = source.X / texture.Width, v0 = source.Y / texture.Height;
        float u1 = (source.X + source.Width) / texture.Width, v1 = (source.Y + source.Height) / texture.Height;
        var start = Reserve(BuiltInPipeline.Sprite, texture, 4, 6);
        _vertices.Add(new Vertex2D(Vector2.Transform(new Vector2(0, 0), m), new Vector2(u0, v0), tint));
        _vertices.Add(new Vertex2D(Vector2.Transform(new Vector2(source.Width, 0), m), new Vector2(u1, v0), tint));
        _vertices.Add(new Vertex2D(Vector2.Transform(new Vector2(source.Width, source.Height), m), new Vector2(u1, v1), tint));
        _vertices.Add(new Vertex2D(Vector2.Transform(new Vector2(0, source.Height), m), new Vector2(u0, v1), tint));
        _indices.AddRange([(ushort)start, (ushort)(start + 1), (ushort)(start + 2), (ushort)start, (ushort)(start + 2), (ushort)(start + 3)]);
    }

    /// <summary>Draws triangles; positions are transformed by the batch view; a null texture draws the vertex colours.</summary>
    public void DrawGeometry(Texture? texture, ReadOnlySpan<Vertex2D> vertices, ReadOnlySpan<ushort> indices)
    {
        Begun();
        if (vertices.Length > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(vertices), "at most 65535 vertices per call");
        var start = Reserve(BuiltInPipeline.TexturedGeometry, texture ?? _white, vertices.Length, indices.Length);
        foreach (var v in vertices) _vertices.Add(v with { Position = Vector2.Transform(v.Position, _view) });
        foreach (var i in indices)
        {
            if (i >= vertices.Length) throw new ArgumentOutOfRangeException(nameof(indices), $"index {i} outside {vertices.Length} vertices");
            _indices.Add((ushort)(start + i));
        }
    }

    /// <summary>Records the batch into one command list (buffer updates, one pass, the draws) and submits it.</summary>
    public void End()
    {
        Begun();
        _begun = false;
        var target = _settings.Target;
        var commands = _device.BeginCommands();
        if (_indices.Count % 2 == 1) _indices.Add(0); // keep index uploads 4-byte sized
        if (_vertices.Count > 0)
        {
            _vertexBuffer = Ensure(_vertexBuffer, _vertices.Count * Vertex2D.SizeInBytes, BufferUsage.Vertex, "batcher-vertices");
            _indexBuffer = Ensure(_indexBuffer, _indices.Count * sizeof(ushort), BufferUsage.Index, "batcher-indices");
            commands.UpdateBuffer<Vertex2D>(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_vertices));
            commands.UpdateBuffer<ushort>(_indexBuffer, 0, CollectionsMarshal.AsSpan(_indices));
        }
        var load = _settings.Clear is null ? LoadOp.Load : LoadOp.Clear;
        commands.BeginPass(new PassDescription(new ColorAttachment(target, load, StoreOp.Store, _settings.Clear ?? default)));
        if (_settings.Scissor is { } scissor) commands.SetScissor(scissor);
        LastDrawCalls = 0;
        if (_batches.Count > 0)
        {
            commands.SetVertices(_vertexBuffer!);
            commands.SetIndices(_indexBuffer!, IndexSize.Bits16);
            var sampler = _device.GetSampler(_settings.Sampler ?? SamplerDescription.PointClamp);
            var blend = _settings.Blend ?? BlendState.AlphaPremultiplied;
            Pipeline? bound = null;
            foreach (var batch in _batches)
            {
                var pipeline = _device.GetBuiltInPipeline(batch.Kind, blend, target.ColorFormat);
                if (!ReferenceEquals(pipeline, bound)) commands.SetPipeline(bound = pipeline);
                commands.SetTexture(0, batch.Texture, sampler);
                commands.DrawIndexed(batch.IndexCount, batch.FirstIndex, batch.BaseVertex);
                LastDrawCalls++;
            }
        }
        commands.EndPass();
        _device.Submit(commands);
        _vertices.Clear();
        _indices.Clear();
        _batches.Clear();
    }

    /// <summary>Releases the batcher's buffers and texture.</summary>
    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _white.Dispose();
    }

    private int Reserve(BuiltInPipeline kind, Texture texture, int vertexCount, int indexCount)
    {
        // Indices are 16-bit and relative to the batch's base vertex; a batch that would exceed 65535 vertices starts a new one.
        if (_batches.Count > 0)
        {
            var last = _batches[^1];
            var relative = _vertices.Count - last.BaseVertex;
            if (last.Kind == kind && ReferenceEquals(last.Texture, texture) && relative + vertexCount <= ushort.MaxValue)
            {
                _batches[^1] = last with { IndexCount = last.IndexCount + indexCount };
                return relative;
            }
        }
        _batches.Add(new Batch(kind, texture, _indices.Count, 0, _vertices.Count) { IndexCount = indexCount });
        return 0;
    }

    private GpuBuffer Ensure(GpuBuffer? buffer, int bytes, BufferUsage usage, string name)
    {
        if (buffer is not null && buffer.SizeInBytes >= bytes) return buffer;
        buffer?.Dispose(); // deferred by the device until submitted work no longer uses it
        return _device.CreateBuffer(new BufferDescription(Math.Max(bytes, (buffer?.SizeInBytes ?? 4096) * 2), usage, name));
    }

    private void Begun()
    {
        if (!_begun) throw new InvalidOperationException("call Begin first");
    }

    private readonly record struct Batch(BuiltInPipeline Kind, Texture Texture, int FirstIndex, int IndexCount, int BaseVertex);
}
