using System.Runtime.InteropServices;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Graphics;

/// <summary>
/// A pair of shaders of the game's own, with their parameters, that the 2D batcher draws with instead of the built-in
/// pipeline: set it on <see cref="RenderState.Material"/> (or <see cref="BatchSettings.Material"/>) and every draw
/// recorded while it is set (sprites, geometry, in any transform, with the state's blend, sampler and scissor) goes
/// through its shaders. It needs a device with custom shaders (<see cref="GraphicsFeatures.Shaders"/>); the shaders
/// are created from files compiled ahead of time (<see cref="CompiledShader"/>), which refuse any other device with
/// <see cref="GraphicsCapabilityException"/>.
/// </summary>
/// <remarks>
/// What the shaders receive:
/// <list type="bullet">
/// <item>Vertices in the layout of <see cref="Vertex2D"/>: position in target pixels relative to the viewport
/// (location 0), texture coordinate (location 1), premultiplied colour (location 2).</item>
/// <item>Vertex uniform slot 0, when the vertex shader declares one: the 4 × 4 matrix that takes those pixels to clip
/// space (a <see cref="System.Numerics.Matrix4x4"/> as it is in memory; <c>mul(matrix, float4(position, 0, 1))</c> in HLSL).</item>
/// <item>Fragment texture slot 0: the draw's texture with the state's sampler. Slots from 1: the textures given to
/// <see cref="SetTexture"/>.</item>
/// <item>Fragment uniform slots: the data given to <see cref="SetUniforms{T}(in T, int)"/>.</item>
/// </list>
/// A draw keeps the parameters the material had when the draw was recorded, so one batch can draw with the same material
/// and different parameters. The material creates one pipeline per blend state and target format it is drawn with and
/// releases them when it is disposed; it does not own the shaders.
/// </remarks>
public sealed class Material2D : IDisposable
{
    private readonly Dictionary<(BlendState Blend, TextureFormat Format), Pipeline> _pipelines = [];
    private readonly byte[]?[] _uniforms;
    private readonly (Devices.Texture Texture, SamplerDescription Sampler)?[] _textures;
    private MaterialParameters? _parameters;
    private bool _disposed;

    /// <summary>Creates a material from a vertex and a fragment shader of one device.</summary>
    /// <param name="vertex">A vertex shader taking <see cref="Vertex2D"/> vertices, with at most one uniform slot (the pixel-to-clip matrix).</param>
    /// <param name="fragment">A fragment shader.</param>
    /// <exception cref="ArgumentException">A shader of the wrong stage, shaders of two devices, or a vertex shader with more than one uniform slot.</exception>
    /// <exception cref="ObjectDisposedException">A disposed shader.</exception>
    public Material2D(Shader vertex, Shader fragment)
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
        Vertex = vertex;
        Fragment = fragment;
        _uniforms = new byte[]?[fragment.Resources.UniformBuffers];
        _textures = new (Devices.Texture, SamplerDescription)?[Math.Max(1, fragment.Resources.Samplers)];
    }

    /// <summary>The device the material draws on.</summary>
    public Devices.IGraphicsDevice Device => Vertex.Device;

    /// <summary>The vertex shader.</summary>
    public Shader Vertex { get; }

    /// <summary>The fragment shader.</summary>
    public Shader Fragment { get; }

    /// <summary>True once disposed; a disposed material cannot be drawn with.</summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Sets the data of a fragment uniform slot: the bytes of <paramref name="data"/> as laid out in memory, which must
    /// match the shader's block (a sequential struct of floats and vectors with the block's padding). Draws recorded
    /// afterwards use it; draws already recorded keep what they had.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The fragment shader declares no such slot, or the data is empty or larger than <see cref="GraphicsLimits.MaxUniformBytes"/>.</exception>
    public void SetUniforms<T>(in T data, int slot = 0) where T : unmanaged
        => SetUniforms(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in data)), slot);

    /// <summary>Sets the data of a fragment uniform slot from bytes (see <see cref="SetUniforms{T}(in T, int)"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The fragment shader declares no such slot, or the data is empty or larger than <see cref="GraphicsLimits.MaxUniformBytes"/>.</exception>
    public void SetUniforms(ReadOnlySpan<byte> data, int slot = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (slot < 0 || slot >= _uniforms.Length)
            throw new ArgumentOutOfRangeException(nameof(slot), $"the fragment shader declares {_uniforms.Length} uniform slot(s); slot {slot} is not one of them");
        if (data.Length == 0 || data.Length > Device.Capabilities.Limits.MaxUniformBytes)
            throw new ArgumentOutOfRangeException(nameof(data), $"uniform data must be 1..{Device.Capabilities.Limits.MaxUniformBytes} bytes");
        _uniforms[slot] = data.ToArray();
        _parameters = null;
    }

    /// <summary>
    /// Gives the fragment shader a texture in a slot from 1 on (slot 0 is the texture of each draw), sampled with
    /// <paramref name="sampler"/> (null: nearest, clamped). Draws recorded afterwards use it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Slot 0, or a slot the fragment shader does not declare.</exception>
    /// <exception cref="ArgumentException">A texture of another device.</exception>
    /// <exception cref="ObjectDisposedException">A disposed texture.</exception>
    public void SetTexture(int slot, Devices.Texture texture, SamplerDescription? sampler = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texture);
        if (slot < 1 || slot >= _textures.Length)
            throw new ArgumentOutOfRangeException(nameof(slot), $"the fragment shader samples {Fragment.Resources.Samplers} texture(s); a material sets slots 1..{_textures.Length - 1} (slot 0 is the texture of each draw)");
        if (!ReferenceEquals(texture.Device, Device)) throw new ArgumentException($"'{texture.Name}' belongs to another device", nameof(texture));
        ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        _textures[slot] = (texture, sampler ?? SamplerDescription.PointClamp);
        _parameters = null;
    }

    /// <summary>Releases the pipelines the material created. The shaders stay the caller's.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var pipeline in _pipelines.Values) pipeline.Dispose();
        _pipelines.Clear();
    }

    /// <summary>The parameters as they are now, checked complete: what a draw recorded now is drawn with.</summary>
    /// <exception cref="ArgumentException">The material belongs to another device than <paramref name="device"/>.</exception>
    /// <exception cref="InvalidOperationException">A uniform slot or a texture slot the fragment shader declares was never set.</exception>
    internal MaterialParameters Parameters(Devices.IGraphicsDevice device)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!ReferenceEquals(device, Device)) throw new ArgumentException("the material belongs to another device than the batcher's");
        if (_parameters is not null) return _parameters;
        for (int slot = 0; slot < _uniforms.Length; slot++)
            if (_uniforms[slot] is null) throw new InvalidOperationException($"the material's fragment shader declares uniform slot {slot}, which was never set; call SetUniforms before drawing");
        for (int slot = 1; slot < _textures.Length; slot++)
            if (_textures[slot] is null) throw new InvalidOperationException($"the material's fragment shader samples slot {slot}, which has no texture; call SetTexture before drawing");
        return _parameters = new MaterialParameters(this, [.. _uniforms!], [.. _textures.Skip(1).Select(t => t!.Value)]);
    }

    /// <summary>The pipeline for a blend state and a target format, created on first use.</summary>
    internal Pipeline PipelineFor(BlendState blend, TextureFormat format)
    {
        if (!_pipelines.TryGetValue((blend, format), out var pipeline))
            _pipelines[(blend, format)] = pipeline = Device.CreatePipeline(new PipelineDescription(Vertex, Fragment, VertexLayout.Vertex2D, blend,
                DepthState.None, new RasterState(), format, null));
        return pipeline;
    }
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
