using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MicroEngine.Graphics.Devices;

/// <summary>Kind of a recorded command (backend-facing; see <see cref="Command"/> for the fields each kind uses).</summary>
public enum CommandKind
{
    /// <summary><see cref="Command.Pass"/>.</summary>
    BeginPass,
    /// <summary>No fields.</summary>
    EndPass,
    /// <summary><see cref="Command.Rect"/>.</summary>
    SetViewport,
    /// <summary><see cref="Command.Rect"/> (already clipped to the target).</summary>
    SetScissor,
    /// <summary><see cref="Command.Resource"/> is the <see cref="Pipeline"/>.</summary>
    SetPipeline,
    /// <summary><see cref="Command.Slot"/>, <see cref="Command.Resource"/> (<see cref="Texture"/>), <see cref="Command.Sampler"/>.</summary>
    SetTexture,
    /// <summary><see cref="Command.Resource"/> (<see cref="GpuBuffer"/>).</summary>
    SetVertices,
    /// <summary><see cref="Command.Resource"/> (<see cref="GpuBuffer"/>), <see cref="Command.IndexSize"/>.</summary>
    SetIndices,
    /// <summary><see cref="Command.Stage"/>, <see cref="Command.Slot"/>, payload.</summary>
    SetUniforms,
    /// <summary><see cref="Command.Count"/> vertices from <see cref="Command.First"/>, <see cref="Command.Instances"/>.</summary>
    Draw,
    /// <summary><see cref="Command.Count"/> indices from <see cref="Command.First"/>, <see cref="Command.BaseVertex"/>, <see cref="Command.Instances"/>.</summary>
    DrawIndexed,
    /// <summary><see cref="Command.Resource"/> (<see cref="GpuBuffer"/>), byte offset in <see cref="Command.First"/>, payload.</summary>
    UpdateBuffer,
    /// <summary><see cref="Command.Resource"/> (<see cref="Texture"/>), <see cref="Command.Rect"/> region, payload.</summary>
    UpdateTexture,
}

/// <summary>One validated command, as a backend reads it from <see cref="CommandList.Commands"/>.</summary>
/// <param name="Kind">Kind.</param>
/// <param name="Resource">Pipeline, texture or buffer, by kind.</param>
/// <param name="Sampler">Sampler for <see cref="CommandKind.SetTexture"/>.</param>
/// <param name="Pass">Pass for <see cref="CommandKind.BeginPass"/>.</param>
/// <param name="Rect">Viewport, scissor or texture region.</param>
/// <param name="Slot">Texture or uniform slot.</param>
/// <param name="Count">Vertex or index count.</param>
/// <param name="First">First vertex, first index, or byte offset.</param>
/// <param name="BaseVertex">Added to each index.</param>
/// <param name="Instances">Instance count.</param>
/// <param name="Stage">Uniform stage.</param>
/// <param name="IndexSize">Index size.</param>
/// <param name="PayloadOffset">Offset of the data in the list's payload.</param>
/// <param name="PayloadLength">Length of the data.</param>
public readonly record struct Command(
    CommandKind Kind,
    GraphicsResource? Resource = null,
    Sampler? Sampler = null,
    PassDescription Pass = default,
    RectI Rect = default,
    int Slot = 0,
    int Count = 0,
    int First = 0,
    int BaseVertex = 0,
    int Instances = 1,
    ShaderStage Stage = ShaderStage.Vertex,
    IndexSize IndexSize = IndexSize.Bits16,
    int PayloadOffset = 0,
    int PayloadLength = 0);

/// <summary>
/// A list of commands for one device, recorded on the render thread and validated here, in core, as it is recorded
/// (before any backend sees it). Every violation throws at the call that causes it:
/// <list type="bullet">
/// <item>a resource of another device: <see cref="ArgumentException"/>; a disposed one: <see cref="ObjectDisposedException"/>;</item>
/// <item>wrong nesting (pass in a pass, draw or binding outside a pass, update inside a pass, recording after submit): <see cref="InvalidOperationException"/>;</item>
/// <item>a pipeline whose attachment formats differ from the pass's, a draw without the bindings the pipeline needs, a
/// range outside a buffer, sampling the pass's own colour target: <see cref="InvalidOperationException"/> or
/// <see cref="ArgumentOutOfRangeException"/>;</item>
/// <item>an undeclared capability (instancing): <see cref="GraphicsCapabilityException"/>.</item>
/// </list>
/// Rules: commands execute in recording order (an update affects only draws recorded after it); bindings, pipeline,
/// viewport and scissor are pass-scoped (each pass starts with none bound, the whole target as viewport and no scissor);
/// uniform data persist for the rest of the list once set.
/// </summary>
public sealed class CommandList
{
    private readonly List<Command> _commands = [];
    private byte[] _payload = new byte[256];
    private int _payloadLength;

    private bool _inPass;
    private PassDescription _pass;
    private Pipeline? _pipeline;
    private GpuBuffer? _vertices;
    private GpuBuffer? _indices;
    private IndexSize _indexSize;
    private readonly Dictionary<int, Texture> _textures = [];
    private readonly HashSet<(ShaderStage, int)> _uniforms = [];

    /// <summary>Creates an empty list; backends call this from <see cref="IGraphicsDevice.BeginCommands"/>.</summary>
    public CommandList(IGraphicsDevice device) => Device = device;

    /// <summary>The device the list records for.</summary>
    public IGraphicsDevice Device { get; }

    /// <summary>True once submitted; a submitted list cannot record or be submitted again.</summary>
    public bool IsSubmitted { get; private set; }

    /// <summary>True between <see cref="BeginPass"/> and <see cref="EndPass"/>.</summary>
    public bool IsInPass => _inPass;

    /// <summary>The recorded commands (backend-facing).</summary>
    public ReadOnlySpan<Command> Commands => CollectionsMarshal.AsSpan(_commands);

    /// <summary>The data of an update or uniform command (backend-facing).</summary>
    public ReadOnlySpan<byte> Payload(in Command command) => _payload.AsSpan(command.PayloadOffset, command.PayloadLength);

    /// <summary>Begins a pass; resets bindings, viewport (whole target) and scissor (none).</summary>
    public void BeginPass(in PassDescription pass)
    {
        Recording();
        if (_inPass) throw new InvalidOperationException("BeginPass inside a pass; call EndPass first");
        var color = pass.Color.Target ?? throw new ArgumentNullException(nameof(pass), "the colour attachment needs a target");
        Use(color, nameof(pass));
        if (pass.Depth is { } depth)
        {
            Use(depth.Target, nameof(pass));
            if (depth.Target.DepthFormat is null) throw new InvalidOperationException($"depth attachment '{depth.Target.Name}' has no depth texture");
            if (depth.Target.Width != color.Width || depth.Target.Height != color.Height)
                throw new InvalidOperationException("depth and colour attachments differ in size");
            if (depth.ClearDepth is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(pass), "clear depth must be within 0..1");
        }
        _inPass = true;
        _pass = pass;
        _pipeline = null;
        _vertices = _indices = null;
        _textures.Clear();
        Add(new Command(CommandKind.BeginPass, Pass: pass));
    }

    /// <summary>Ends the current pass.</summary>
    public void EndPass()
    {
        InPass();
        _inPass = false;
        Add(new Command(CommandKind.EndPass));
    }

    /// <summary>Sets the viewport: built-in vertex positions are pixels relative to its origin, and it clips. Must lie inside the colour target.</summary>
    public void SetViewport(in RectI viewport)
    {
        InPass();
        if (viewport.IsEmpty || !_pass.Color.Target.Bounds.Contains(viewport))
            throw new ArgumentOutOfRangeException(nameof(viewport), $"viewport {viewport} must be non-empty and inside the target {_pass.Color.Target.Bounds}");
        Add(new Command(CommandKind.SetViewport, Rect: viewport));
    }

    /// <summary>Sets the scissor in target pixels (absolute, independent of the viewport), clipped to the target; null = none. Clears ignore it.</summary>
    public void SetScissor(RectI? scissor)
    {
        InPass();
        var bounds = _pass.Color.Target.Bounds;
        if (scissor is { } s && (s.Width < 0 || s.Height < 0)) throw new ArgumentOutOfRangeException(nameof(scissor), "scissor size must not be negative");
        Add(new Command(CommandKind.SetScissor, Rect: scissor is { } r ? bounds.Intersect(r) : bounds));
    }

    /// <summary>Binds a pipeline; its colour and depth formats must equal the pass attachments'.</summary>
    public void SetPipeline(Pipeline pipeline)
    {
        InPass();
        Use(pipeline, nameof(pipeline));
        var passDepth = _pass.Depth?.Target.DepthFormat;
        if (pipeline.ColorFormat != _pass.Color.Target.ColorFormat || pipeline.DepthFormat != passDepth)
            throw new InvalidOperationException($"pipeline renders to {pipeline.ColorFormat}/{pipeline.DepthFormat?.ToString() ?? "no depth"} but the pass has {_pass.Color.Target.ColorFormat}/{passDepth?.ToString() ?? "no depth"}");
        _pipeline = pipeline;
        Add(new Command(CommandKind.SetPipeline, Resource: pipeline));
    }

    /// <summary>Binds a texture and sampler to a fragment slot. The pass's own colour target cannot be sampled.</summary>
    public void SetTexture(int slot, Texture texture, Sampler sampler)
    {
        InPass();
        Use(texture, nameof(texture));
        if (!ReferenceEquals(sampler.Device, Device)) throw new ArgumentException("the sampler belongs to another device", nameof(sampler));
        if (slot < 0 || slot >= Device.Capabilities.Limits.MaxTextureSlots)
            throw new ArgumentOutOfRangeException(nameof(slot), $"slot {slot} outside 0..{Device.Capabilities.Limits.MaxTextureSlots - 1}");
        if (texture.OwnerTarget is { } owner && ReferenceEquals(owner, _pass.Color.Target))
            throw new InvalidOperationException("a pass cannot sample its own colour target");
        _textures[slot] = texture;
        Add(new Command(CommandKind.SetTexture, Resource: texture, Sampler: sampler, Slot: slot));
    }

    /// <summary>Binds the vertex buffer (slot 0).</summary>
    public void SetVertices(GpuBuffer buffer)
    {
        InPass();
        Use(buffer, nameof(buffer));
        if ((buffer.Usage & BufferUsage.Vertex) == 0) throw new InvalidOperationException("the buffer was not created with BufferUsage.Vertex");
        _vertices = buffer;
        Add(new Command(CommandKind.SetVertices, Resource: buffer));
    }

    /// <summary>Binds the index buffer.</summary>
    public void SetIndices(GpuBuffer buffer, IndexSize size = IndexSize.Bits16)
    {
        InPass();
        Use(buffer, nameof(buffer));
        if ((buffer.Usage & BufferUsage.Index) == 0) throw new InvalidOperationException("the buffer was not created with BufferUsage.Index");
        if (size is not (IndexSize.Bits16 or IndexSize.Bits32)) throw new ArgumentOutOfRangeException(nameof(size));
        _indices = buffer;
        _indexSize = size;
        Add(new Command(CommandKind.SetIndices, Resource: buffer, IndexSize: size));
    }

    /// <summary>Pushes uniform data for a slot of a stage of the bound custom pipeline (built-in pipelines have none).</summary>
    public void SetUniforms(ShaderStage stage, int slot, ReadOnlySpan<byte> data)
    {
        InPass();
        var pipeline = _pipeline ?? throw new InvalidOperationException("SetUniforms needs a bound pipeline");
        var declared = stage == ShaderStage.Vertex ? pipeline.VertexResources.UniformBuffers : pipeline.FragmentResources.UniformBuffers;
        if (slot < 0 || slot >= declared)
            throw new InvalidOperationException($"the bound pipeline declares {declared} {stage} uniform slot(s); slot {slot} is not one of them");
        if (data.Length == 0 || data.Length > Device.Capabilities.Limits.MaxUniformBytes)
            throw new ArgumentOutOfRangeException(nameof(data), $"uniform data must be 1..{Device.Capabilities.Limits.MaxUniformBytes} bytes");
        _uniforms.Add((stage, slot));
        var (offset, length) = Store(data);
        Add(new Command(CommandKind.SetUniforms, Stage: stage, Slot: slot, PayloadOffset: offset, PayloadLength: length));
    }

    /// <summary>Typed overload of <see cref="SetUniforms(ShaderStage, int, ReadOnlySpan{byte})"/>: the bytes of <paramref name="data"/>, as laid out in memory.</summary>
    public void SetUniforms<T>(ShaderStage stage, int slot, in T data) where T : unmanaged
        => SetUniforms(stage, slot, MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in data)));

    /// <summary>Draws <paramref name="vertexCount"/> vertices from <paramref name="firstVertex"/>.</summary>
    public void Draw(int vertexCount, int firstVertex = 0, int instances = 1)
    {
        var pipeline = ReadyToDraw(instances);
        if (vertexCount <= 0 || firstVertex < 0) throw new ArgumentOutOfRangeException(nameof(vertexCount), "need a positive count and a non-negative first vertex");
        if ((long)(firstVertex + vertexCount) * pipeline.Layout.Stride > _vertices!.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(vertexCount), $"vertices {firstVertex}..{firstVertex + vertexCount} exceed the vertex buffer ({_vertices.SizeInBytes} bytes, stride {pipeline.Layout.Stride})");
        Add(new Command(CommandKind.Draw, Count: vertexCount, First: firstVertex, Instances: instances));
    }

    /// <summary>
    /// Draws <paramref name="indexCount"/> indices from <paramref name="firstIndex"/>; each index plus <paramref name="baseVertex"/>
    /// selects a vertex. Index values are not validated (they live in device memory): an index that selects a vertex
    /// outside the bound buffer is undefined behaviour (a tier-0 backend may throw at <see cref="IGraphicsDevice.Submit"/>).
    /// </summary>
    public void DrawIndexed(int indexCount, int firstIndex = 0, int baseVertex = 0, int instances = 1)
    {
        ReadyToDraw(instances);
        if (_indices is null) throw new InvalidOperationException("DrawIndexed needs SetIndices in this pass");
        if (indexCount <= 0 || firstIndex < 0 || baseVertex < 0) throw new ArgumentOutOfRangeException(nameof(indexCount), "need a positive count and non-negative first index and base vertex");
        if ((long)(firstIndex + indexCount) * (int)_indexSize > _indices.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(indexCount), $"indices {firstIndex}..{firstIndex + indexCount} exceed the index buffer ({_indices.SizeInBytes} bytes)");
        Add(new Command(CommandKind.DrawIndexed, Count: indexCount, First: firstIndex, BaseVertex: baseVertex, Instances: instances));
    }

    /// <summary>Writes <paramref name="data"/> into a buffer at <paramref name="offset"/>, outside passes; draws recorded later see it.</summary>
    public void UpdateBuffer(GpuBuffer buffer, int offset, ReadOnlySpan<byte> data)
    {
        OutsidePass();
        Use(buffer, nameof(buffer));
        if (offset < 0 || data.Length == 0 || (long)offset + data.Length > buffer.SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(data), $"update {offset}+{data.Length} outside the buffer ({buffer.SizeInBytes} bytes)");
        var (o, l) = Store(data);
        Add(new Command(CommandKind.UpdateBuffer, Resource: buffer, First: offset, PayloadOffset: o, PayloadLength: l));
    }

    /// <summary>Typed overload of <see cref="UpdateBuffer(GpuBuffer, int, ReadOnlySpan{byte})"/>.</summary>
    public void UpdateBuffer<T>(GpuBuffer buffer, int offset, ReadOnlySpan<T> data) where T : unmanaged
        => UpdateBuffer(buffer, offset, MemoryMarshal.AsBytes(data));

    /// <summary>Writes tightly packed texel data into a region of mip level 0, outside passes.</summary>
    public void UpdateTexture(Texture texture, in RectI region, ReadOnlySpan<byte> data)
    {
        OutsidePass();
        Use(texture, nameof(texture));
        if (region.IsEmpty || !new RectI(0, 0, texture.Width, texture.Height).Contains(region))
            throw new ArgumentOutOfRangeException(nameof(region), $"region {region} outside the texture {texture.Width}x{texture.Height}");
        if (data.Length != TextureFormats.ByteCount(texture.Format, region.Width, region.Height))
            throw new ArgumentException($"expected {TextureFormats.ByteCount(texture.Format, region.Width, region.Height)} bytes of {texture.Format}", nameof(data));
        var (o, l) = Store(data);
        Add(new Command(CommandKind.UpdateTexture, Resource: texture, Rect: region, PayloadOffset: o, PayloadLength: l));
    }

    /// <summary>Core, at submit: the list is finished and every resource it references is still alive. Marks it submitted.</summary>
    internal void Seal()
    {
        if (IsSubmitted) throw new InvalidOperationException("the command list was already submitted");
        if (_inPass) throw new InvalidOperationException("the command list has an open pass; call EndPass before Submit");
        foreach (var c in _commands)
        {
            if (c.Resource is { IsDisposed: true } r) throw new ObjectDisposedException(r.GetType().Name, $"'{r.Name}' was disposed after it was recorded");
            if (c.Kind == CommandKind.BeginPass && (c.Pass.Color.Target.IsDisposed || c.Pass.Depth?.Target.IsDisposed == true))
                throw new ObjectDisposedException(nameof(RenderTarget), "a pass target was disposed (or the surface target expired) after it was recorded");
        }
        IsSubmitted = true;
    }

    private Pipeline ReadyToDraw(int instances)
    {
        InPass();
        var pipeline = _pipeline ?? throw new InvalidOperationException("draw without a pipeline; call SetPipeline");
        if (_vertices is null) throw new InvalidOperationException("draw without vertices; call SetVertices in this pass");
        for (var slot = 0; slot < pipeline.FragmentResources.Samplers; slot++)
            if (!_textures.ContainsKey(slot)) throw new InvalidOperationException($"the pipeline samples slot {slot}, which has no texture in this pass");
        for (var slot = 0; slot < pipeline.VertexResources.UniformBuffers; slot++)
            if (!_uniforms.Contains((ShaderStage.Vertex, slot))) throw new InvalidOperationException($"vertex uniform slot {slot} was never set in this list");
        for (var slot = 0; slot < pipeline.FragmentResources.UniformBuffers; slot++)
            if (!_uniforms.Contains((ShaderStage.Fragment, slot))) throw new InvalidOperationException($"fragment uniform slot {slot} was never set in this list");
        if (instances < 1) throw new ArgumentOutOfRangeException(nameof(instances));
        if (instances > 1 && !Device.Capabilities.Has(GraphicsFeatures.Instancing))
            throw new GraphicsCapabilityException("Draw(instances > 1)", Device.Capabilities.Backend, GraphicsFeatures.Instancing);
        return pipeline;
    }

    private void Use(GraphicsResource resource, string parameter, [CallerMemberName] string operation = "")
    {
        ArgumentNullException.ThrowIfNull(resource, parameter);
        if (!ReferenceEquals(resource.Device, Device)) throw new ArgumentException($"{operation}: '{resource.Name}' belongs to another device", parameter);
        if (resource.IsDisposed) throw new ObjectDisposedException(resource.GetType().Name, $"{operation}: '{resource.Name}' is disposed");
    }

    private void Recording()
    {
        if (IsSubmitted) throw new InvalidOperationException("the command list was submitted; start a new one with BeginCommands");
    }

    private void InPass([CallerMemberName] string operation = "")
    {
        Recording();
        if (!_inPass) throw new InvalidOperationException($"{operation} outside a pass; call BeginPass first");
    }

    private void OutsidePass([CallerMemberName] string operation = "")
    {
        Recording();
        if (_inPass) throw new InvalidOperationException($"{operation} inside a pass; updates are recorded between passes");
    }

    private (int Offset, int Length) Store(ReadOnlySpan<byte> data)
    {
        if (_payloadLength + data.Length > _payload.Length) Array.Resize(ref _payload, Math.Max(_payload.Length * 2, _payloadLength + data.Length));
        data.CopyTo(_payload.AsSpan(_payloadLength));
        var offset = _payloadLength;
        _payloadLength += data.Length;
        return (offset, data.Length);
    }

    private void Add(in Command command) => _commands.Add(command);
}
