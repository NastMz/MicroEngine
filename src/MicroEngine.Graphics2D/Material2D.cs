using System.Runtime.InteropServices;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>
/// A fragment shader of the game's own with its parameters, that draws go through instead of the built-in pipeline:
/// push it with a state (<see cref="IRenderer.PushState"/>, <see cref="DrawState.Material"/>) and every draw made until
/// the pop (sprites, shapes, text, in any camera, with the state's blend and sampler) goes through the shader. A game
/// gets one from its assets (<c>Assets.Material("shaders/water.frag.shader")</c>) or from its graphics device
/// (<see cref="IGraphicsDevice.CreateMaterial"/>); the engine supplies the vertex stage
/// (<see cref="SpriteVertexShader"/>), so a material needs only the fragment shader, compiled ahead of time by
/// <c>microengine shaders</c>.
/// </summary>
/// <remarks>
/// <para>
/// Parameters are set by the names they have in the shader's source (<see cref="Set{T}(string, in T)"/>,
/// <see cref="SetTexture(string, Texture, SamplerDescription?)"/>) and checked: a name the shader does not have, or a
/// value of another size than the shader declares, is an <see cref="ArgumentException"/> that lists the shader's
/// parameters. A draw keeps the parameters the material had when the draw was made, so one frame can draw with the same
/// material and different parameters. Values never set are zero; a texture never set is an error at the draw.
/// </para>
/// <para>
/// Shaders run on a device that declares <see cref="GraphicsFeatures.Shaders"/> (<c>sdl-gpu</c>). On any other
/// (<see cref="IsSupported"/> false) the material is created all the same, checks its parameters the same way, and
/// its draws are made without the shader, with its <see cref="Fallback"/>. The game's drawing code is the same.
/// </para>
/// <para>
/// What the shaders receive: vertices in the layout of <see cref="Vertex2D"/> (position in target pixels relative to
/// the viewport at location 0, texture coordinate at 1, premultiplied colour at 2); in vertex uniform slot 0, when the
/// vertex shader declares one, the 4 × 4 matrix that takes those pixels to clip space; in fragment texture slot 0 the
/// draw's texture with the state's sampler, and in the slots from 1 the material's textures; in the fragment uniform
/// slots the material's values. The material creates one pipeline per blend state and target format it is drawn
/// with and releases them when it is disposed. Dispose a material between frames, not in the frame that drew with
/// it: the frame's draws reach the device when it ends.
/// </para>
/// </remarks>
public sealed class Material2D : IDisposable
{
    private static CompiledShader? s_spriteVertex;
    private readonly Dictionary<(BlendState Blend, TextureFormat Format), Pipeline> _pipelines = [];
    private readonly byte[]?[] _uniforms;
    // Whether draws already made hold a slot's array: the next named value is then written into a copy.
    private readonly bool[] _shared;
    private readonly (Devices.Texture? Texture, SamplerDescription Sampler)?[] _textures;
    private readonly bool _ownsShaders;
    private readonly string _label;
    private MaterialParameters? _parameters;
    private bool _disposed;

    /// <summary>Creates a material from a vertex and a fragment shader of one device. The shaders stay the caller's.</summary>
    /// <param name="vertex">A vertex shader taking <see cref="Vertex2D"/> vertices, with at most one uniform slot (the pixel-to-clip matrix).</param>
    /// <param name="fragment">A fragment shader.</param>
    /// <exception cref="ArgumentException">A shader of the wrong stage, shaders of two devices, or a vertex shader with more than one uniform slot.</exception>
    /// <exception cref="ObjectDisposedException">A disposed shader.</exception>
    public Material2D(Shader vertex, Shader fragment) : this(vertex, fragment, null) { }

    /// <summary>
    /// Creates a material from a vertex and a fragment shader of one device, with the names of the fragment shader's
    /// parameters (<see cref="CompiledShader.Reflection"/>) so they can be set by name. The shaders stay the caller's.
    /// </summary>
    /// <param name="vertex">A vertex shader taking <see cref="Vertex2D"/> vertices, with at most one uniform slot (the pixel-to-clip matrix).</param>
    /// <param name="fragment">A fragment shader.</param>
    /// <param name="reflection">The fragment shader's parameter names, or null to set parameters by slot only.</param>
    /// <exception cref="ArgumentException">A shader of the wrong stage, shaders of two devices, a vertex shader with more than one uniform slot, or names for slots the fragment shader does not have.</exception>
    /// <exception cref="ObjectDisposedException">A disposed shader.</exception>
    public Material2D(Shader vertex, Shader fragment, ShaderReflection? reflection) : this(vertex, fragment, reflection, false, null) { }

    private Material2D(Shader vertex, Shader fragment, ShaderReflection? reflection, bool ownsShaders, string? source)
        : this(Check(vertex, fragment).Resources, reflection, source)
    {
        Vertex = vertex;
        Fragment = fragment;
        _ownsShaders = ownsShaders;
    }

    /// <summary>The material's storage, with or without shaders on a device.</summary>
    private Material2D(ShaderResources fragment, ShaderReflection? reflection, string? source)
    {
        if (reflection is not null && (reflection.Blocks.Any(b => b.Slot >= fragment.UniformBuffers) || reflection.Textures.Any(t => t.Slot >= fragment.Samplers)))
            throw new ArgumentException("the reflection names a slot the fragment shader does not have", nameof(reflection));
        Reflection = reflection;
        _label = source is null ? "the material's fragment shader" : $"the shader '{source}'";
        _uniforms = new byte[]?[fragment.UniformBuffers];
        _shared = new bool[fragment.UniformBuffers];
        _textures = new (Devices.Texture?, SamplerDescription)?[Math.Max(1, fragment.Samplers)];
        // Named values start at zero: a draw never fails because one of them was not set.
        if (reflection is not null)
            foreach (var block in reflection.Blocks) _uniforms[block.Slot] = new byte[block.Size];
    }

    private static Shader Check(Shader vertex, Shader fragment)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        ArgumentNullException.ThrowIfNull(fragment);
        if (vertex.Stage != ShaderStage.Vertex) throw new ArgumentException($"'{vertex.Name}' is a {vertex.Stage} shader; a material needs a vertex shader first", nameof(vertex));
        if (fragment.Stage != ShaderStage.Fragment) throw new ArgumentException($"'{fragment.Name}' is a {fragment.Stage} shader; a material needs a fragment shader second", nameof(fragment));
        if (!ReferenceEquals(vertex.Device, fragment.Device)) throw new ArgumentException("the shaders belong to two devices", nameof(fragment));
        ObjectDisposedException.ThrowIf(vertex.IsDisposed, vertex);
        ObjectDisposedException.ThrowIf(fragment.IsDisposed, fragment);
        if (vertex.Resources.UniformBuffers > 1)
            throw new ArgumentException($"the vertex shader declares {vertex.Resources.UniformBuffers} uniform slots; a material's vertex shader has at most one, the pixel-to-clip matrix", nameof(vertex));
        return fragment;
    }

    /// <summary>
    /// Creates a material from a compiled fragment shader, with the engine's vertex stage
    /// (<see cref="SpriteVertexShader"/>). On a device that runs shaders the material owns the two shaders it creates
    /// there. With no device, or one without <see cref="GraphicsFeatures.Shaders"/>, the material is not supported
    /// (<see cref="IsSupported"/>): it keeps and checks its parameters and draws with <paramref name="fallback"/>.
    /// </summary>
    /// <param name="device">The device to draw on, or null for a renderer that has none (the headless platform).</param>
    /// <param name="fragment">A fragment shader compiled by <c>microengine shaders</c>.</param>
    /// <param name="fallback">What the draws become where the shader cannot run.</param>
    /// <exception cref="ArgumentException"><paramref name="fragment"/> is not a fragment shader.</exception>
    /// <exception cref="GraphicsCapabilityException">The device runs shaders, but none of the formats this one was compiled to.</exception>
    public static Material2D Create(Devices.IGraphicsDevice? device, CompiledShader fragment, MaterialFallback fallback = default)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        if (fragment.Stage != ShaderStage.Fragment)
            throw new ArgumentException($"{fragment} is a {fragment.Stage} shader; a material is made from a fragment shader (the engine supplies the vertex stage)", nameof(fragment));
        if (device is null || !device.Capabilities.Has(GraphicsFeatures.Shaders))
            return new Material2D(fragment.Resources, fragment.Reflection, fragment.Source) { Fallback = fallback };
        var vertex = SpriteVertexShader.Create(device);
        try
        {
            return new Material2D(vertex, fragment.Create(device), fragment.Reflection, true, fragment.Source) { Fallback = fallback };
        }
        catch
        {
            vertex.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The vertex stage every 2D material shares, compiled for every format: it passes the batcher's vertices on
    /// (texture coordinate in <c>TEXCOORD0</c>, premultiplied colour in <c>TEXCOORD1</c>) and takes their pixel
    /// positions to clip space. <see cref="Create"/> uses it; a game that builds a material from shaders of its own on
    /// the device can use it as that material's vertex shader (<c>SpriteVertexShader.Create(device)</c>).
    /// </summary>
    public static CompiledShader SpriteVertexShader => s_spriteVertex ??= LoadSpriteVertex();

    private static CompiledShader LoadSpriteVertex()
    {
        using var stream = typeof(Material2D).Assembly.GetManifestResourceStream("builtin/sprite.vert.shader")
            ?? throw new InvalidOperationException("The built-in sprite vertex shader is missing from MicroEngine.Graphics2D.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return CompiledShader.Parse(memory.ToArray());
    }

    /// <summary>
    /// Whether the graphics device runs the material's shader. False on a device without
    /// <see cref="GraphicsFeatures.Shaders"/>, where the material's draws are made with its <see cref="Fallback"/>.
    /// </summary>
    public bool IsSupported => Fragment is not null;

    /// <summary>What the material's draws become where its shader cannot run (<see cref="IsSupported"/> false); plain draws by default.</summary>
    public MaterialFallback Fallback { get; set; }

    /// <summary>The names of the fragment shader's parameters, or null when they are not known (a version 1 <c>.shader</c> file, or shaders given without them).</summary>
    public ShaderReflection? Reflection { get; }

    /// <summary>The device the material draws on, or null when it is not supported.</summary>
    public Devices.IGraphicsDevice? Device => Fragment?.Device;

    /// <summary>The vertex shader, or null when the material is not supported.</summary>
    public Shader? Vertex { get; }

    /// <summary>The fragment shader, or null when the material is not supported.</summary>
    public Shader? Fragment { get; }

    /// <summary>True once disposed; a disposed material cannot be drawn with.</summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Sets the value the shader's source names <paramref name="name"/> (a field of a <c>cbuffer</c>): a
    /// <see cref="float"/>, a <see cref="System.Numerics.Vector2"/>, <see cref="System.Numerics.Vector3"/> or
    /// <see cref="System.Numerics.Vector4"/>, a <see cref="ColorF"/>, a <see cref="System.Numerics.Matrix4x4"/>, or any
    /// value whose bytes are what the shader declares. Draws made afterwards use it; draws already made keep what they had.
    /// </summary>
    /// <exception cref="ArgumentException">The shader has no value of that name, or the value's size is not the size the shader declares; the message lists the shader's parameters.</exception>
    /// <exception cref="InvalidOperationException">The shader's parameter names are not known (<see cref="Reflection"/> is null).</exception>
    public void Set<T>(string name, in T value) where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(name);
        var reflection = Reflection ?? throw NoNames();
        if (!reflection.TryGetUniform(name, out var block, out var uniform))
        {
            if (reflection.Textures.Any(t => t.Name == name)) throw new ArgumentException($"'{name}' is a texture of {_label}; give it with SetTexture.", nameof(name));
            throw new ArgumentException($"{Capital(_label)} has no parameter named '{name}'. Its parameters: {Listed(reflection)}.", nameof(name));
        }
        var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value));
        if (bytes.Length != uniform.Size)
            throw new ArgumentException($"'{name}' takes {uniform.Size} bytes in {_label}; the {typeof(T).Name} given is {bytes.Length} bytes. " +
                "A float is 4 bytes, a float2 8, a float3 12, a float4 or a colour 16, a float4x4 64.", nameof(value));
        if (_shared[block.Slot])
        {
            _uniforms[block.Slot] = (byte[])_uniforms[block.Slot]!.Clone();
            _shared[block.Slot] = false;
        }
        bytes.CopyTo(_uniforms[block.Slot].AsSpan(uniform.Offset));
        _parameters = null;
    }

    /// <summary>
    /// Gives the shader the texture its source names <paramref name="name"/>, sampled with <paramref name="sampler"/>
    /// (null: the texture's own filter, clamped). Any texture of the drawing API works, a render target's included.
    /// The first texture a shader declares (slot 0) is the texture of each draw and is not set here.
    /// </summary>
    /// <exception cref="ArgumentException">The shader has no texture of that name, the name is the draw's own texture, or the texture belongs to another graphics device.</exception>
    /// <exception cref="InvalidOperationException">The shader's parameter names are not known (<see cref="Reflection"/> is null).</exception>
    /// <exception cref="ObjectDisposedException">A disposed texture.</exception>
    public void SetTexture(string name, Texture texture, SamplerDescription? sampler = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(texture);
        var reflection = Reflection ?? throw NoNames();
        ShaderTextureSlot? slot = null;
        foreach (var t in reflection.Textures)
            if (t.Name == name) slot = t;
        if (slot is null)
        {
            if (reflection.TryGetUniform(name, out _, out _)) throw new ArgumentException($"'{name}' is a value of {_label}, not a texture; set it with Set.", nameof(name));
            throw new ArgumentException($"{Capital(_label)} has no texture named '{name}'. Its parameters: {Listed(reflection)}.", nameof(name));
        }
        if (slot.Slot == 0)
            throw new ArgumentException($"'{name}' is the first texture of {_label}: it is the texture of each draw, so it is not set on the material.", nameof(name));
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        var chosen = sampler ?? (texture.Filter == TextureFilter.Linear ? SamplerDescription.LinearClamp : SamplerDescription.PointClamp);
        if (IsSupported)
        {
            var native = texture.Native;
            if (native is null || !ReferenceEquals(native.Device, Device))
                throw new ArgumentException($"The {texture.Width}x{texture.Height} texture given for '{name}' belongs to another graphics device than the material.", nameof(texture));
            _textures[slot.Slot] = (native, chosen);
        }
        else _textures[slot.Slot] = (null, chosen);
        _parameters = null;
    }

    /// <summary>
    /// Sets the data of a whole fragment uniform slot: the bytes of <paramref name="data"/> as laid out in memory, which
    /// must match the shader's block (a sequential struct of floats and vectors with the block's padding). Prefer
    /// <see cref="Set{T}(string, in T)"/>, which is checked; this is for shaders whose names are not known. Draws made
    /// afterwards use it; draws already made keep what they had.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The fragment shader declares no such slot, the data is empty or larger than <see cref="GraphicsLimits.MaxUniformBytes"/>, or its size is not the size of the block when that is known.</exception>
    public void SetUniforms<T>(in T data, int slot = 0) where T : unmanaged
        => SetUniforms(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in data)), slot);

    /// <summary>Sets the data of a whole fragment uniform slot from bytes (see <see cref="SetUniforms{T}(in T, int)"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The fragment shader declares no such slot, the data is empty or larger than <see cref="GraphicsLimits.MaxUniformBytes"/>, or its size is not the size of the block when that is known.</exception>
    public void SetUniforms(ReadOnlySpan<byte> data, int slot = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (slot < 0 || slot >= _uniforms.Length)
            throw new ArgumentOutOfRangeException(nameof(slot), $"the fragment shader declares {_uniforms.Length} uniform slot(s); slot {slot} is not one of them");
        int limit = Device?.Capabilities.Limits.MaxUniformBytes ?? int.MaxValue;
        if (data.Length == 0 || data.Length > limit) throw new ArgumentOutOfRangeException(nameof(data), $"uniform data must be 1..{limit} bytes");
        if (Reflection is { } reflection)
            foreach (var block in reflection.Blocks)
                if (block.Slot == slot && block.Size != data.Length)
                    throw new ArgumentOutOfRangeException(nameof(data), $"the block '{block.Name}' of {_label} is {block.Size} bytes; the data given is {data.Length} bytes");
        _uniforms[slot] = data.ToArray();
        _shared[slot] = false;
        _parameters = null;
    }

    /// <summary>
    /// Gives the fragment shader a device texture in a slot from 1 on (slot 0 is the texture of each draw), sampled with
    /// <paramref name="sampler"/> (null: nearest, clamped). Prefer
    /// <see cref="SetTexture(string, Texture, SamplerDescription?)"/>, by name. Draws made afterwards use it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Slot 0, or a slot the fragment shader does not declare.</exception>
    /// <exception cref="ArgumentException">A texture of another device.</exception>
    /// <exception cref="InvalidOperationException">The material is not supported: it has no device.</exception>
    /// <exception cref="ObjectDisposedException">A disposed texture.</exception>
    public void SetTexture(int slot, Devices.Texture texture, SamplerDescription? sampler = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texture);
        if (slot < 1 || slot >= _textures.Length)
            throw new ArgumentOutOfRangeException(nameof(slot), $"the fragment shader samples {_textures.Length} texture(s); a material sets slots 1..{_textures.Length - 1} (slot 0 is the texture of each draw)");
        if (Device is null) throw new InvalidOperationException("The material is not supported on this graphics device and takes no device texture; set its textures by name.");
        if (!ReferenceEquals(texture.Device, Device)) throw new ArgumentException($"'{texture.Name}' belongs to another device", nameof(texture));
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        _textures[slot] = (texture, sampler ?? SamplerDescription.PointClamp);
        _parameters = null;
    }

    /// <summary>Releases the pipelines the material created, and the shaders it created itself (<see cref="Create"/>). Shaders given to a constructor stay the caller's.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var pipeline in _pipelines.Values) pipeline.Dispose();
        _pipelines.Clear();
        if (!_ownsShaders) return;
        Fragment?.Dispose();
        Vertex?.Dispose();
    }

    /// <summary>
    /// Checks that the material can be drawn with now: not disposed, and every texture and unnamed uniform slot its
    /// shader declares was set. The same check on every device, supported or not.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The material was disposed.</exception>
    /// <exception cref="InvalidOperationException">A uniform slot or a texture the fragment shader declares was never set.</exception>
    internal void Validate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_parameters is not null) return;
        for (int slot = 0; slot < _uniforms.Length; slot++)
            if (_uniforms[slot] is null) throw new InvalidOperationException($"{Capital(_label)} declares uniform slot {slot}, which was never set; call SetUniforms before drawing.");
        for (int slot = 1; slot < _textures.Length; slot++)
            if (_textures[slot] is null)
            {
                string? name = Reflection?.Textures.FirstOrDefault(t => t.Slot == slot)?.Name;
                throw new InvalidOperationException(name is null
                    ? $"{Capital(_label)} samples slot {slot}, which has no texture; call SetTexture before drawing."
                    : $"{Capital(_label)} samples the texture '{name}', which was never given; call SetTexture(\"{name}\", ...) before drawing.");
            }
    }

    /// <summary>The parameters as they are now, checked complete: what a draw recorded now is drawn with.</summary>
    /// <exception cref="ArgumentException">The material belongs to another device than <paramref name="device"/>.</exception>
    /// <exception cref="InvalidOperationException">The material is not supported, or a uniform slot or a texture the fragment shader declares was never set.</exception>
    internal MaterialParameters Parameters(Devices.IGraphicsDevice device)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsSupported)
            throw new InvalidOperationException("The material's shader cannot run on this graphics device (it has no custom shaders), so the 2D batcher cannot draw with it. " +
                "Draw through IRenderer.PushState, which applies the material's fallback, or check IsSupported first.");
        if (!ReferenceEquals(device, Device)) throw new ArgumentException("the material belongs to another device than the batcher's");
        if (_parameters is not null) return _parameters;
        Validate();
        Array.Fill(_shared, true);
        return _parameters = new MaterialParameters(this, [.. _uniforms!], [.. _textures.Skip(1).Select(t => (t!.Value.Texture!, t.Value.Sampler))]);
    }

    /// <summary>The pipeline for a blend state and a target format, created on first use.</summary>
    internal Pipeline PipelineFor(BlendState blend, TextureFormat format)
    {
        if (!_pipelines.TryGetValue((blend, format), out var pipeline))
            _pipelines[(blend, format)] = pipeline = Device!.CreatePipeline(new PipelineDescription(Vertex!, Fragment!, VertexLayout.Vertex2D, blend,
                DepthState.None, new RasterState(), format, null));
        return pipeline;
    }

    private InvalidOperationException NoNames() => new(
        $"{Capital(_label)} carries no parameter names (a .shader file of format version 1, or shaders given without their reflection). " +
        "Compile it again with this SDK's 'microengine shaders', or set its data by slot with SetUniforms.");

    private static string Listed(ShaderReflection reflection)
    {
        string names = string.Join(", ", reflection.Names);
        return names.Length == 0 ? "none" : names;
    }

    private static string Capital(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}

/// <summary>A material's parameters at one moment: what the draws recorded then are drawn with.</summary>
/// <param name="Material">The material.</param>
/// <param name="Uniforms">The data of each fragment uniform slot.</param>
/// <param name="Textures">The textures of fragment slots 1 and up.</param>
internal sealed class MaterialParameters(Material2D Material, byte[][] Uniforms, (Devices.Texture Texture, SamplerDescription Sampler)[] Textures)
{
    public Material2D Material { get; } = Material;
    public byte[][] Uniforms { get; } = Uniforms;
    public (Devices.Texture Texture, SamplerDescription Sampler)[] Textures { get; } = Textures;
}
