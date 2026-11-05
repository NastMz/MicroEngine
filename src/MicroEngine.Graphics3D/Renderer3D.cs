using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Graphics.Devices;
using MicroEngine.Scenes;

namespace MicroEngine.Graphics3D;

/// <summary>A mesh on the device: one vertex and index buffer per primitive and its base colour.</summary>
public sealed class GpuMesh : IDisposable
{
    internal readonly List<Part> Parts = [];

    internal GpuMesh(string name, (Vector3 Min, Vector3 Max) bounds)
    {
        Name = name;
        Bounds = bounds;
    }

    /// <summary>The mesh's name.</summary>
    public string Name { get; }

    /// <summary>Model-space bounds of every vertex.</summary>
    public (Vector3 Min, Vector3 Max) Bounds { get; }

    /// <summary>Triangles drawn per <see cref="Renderer3D.Draw"/>.</summary>
    public int Triangles => Parts.Sum(p => p.IndexCount / 3);

    /// <summary>Releases the device resources (deferred until submitted work no longer uses them).</summary>
    public void Dispose()
    {
        foreach (var part in Parts)
        {
            part.Vertices.Dispose();
            part.Indices.Dispose();
            if (part.OwnsTexture) part.Texture.Dispose();
        }
        Parts.Clear();
    }

    internal sealed record Part(GpuBuffer Vertices, GpuBuffer Indices, int IndexCount, Texture Texture, bool OwnsTexture, ColorF BaseColor);
}

/// <summary>What an entity of a world draws in 3D: a mesh and a tint (see <see cref="Renderer3D.DrawWorld"/>).</summary>
public sealed class MeshRenderer(GpuMesh mesh)
{
    /// <summary>The mesh.</summary>
    public GpuMesh Mesh { get; set; } = mesh ?? throw new ArgumentNullException(nameof(mesh));

    /// <summary>Premultiplied tint multiplied with the material's base colour.</summary>
    public ColorF Tint { get; set; } = ColorF.White;

    /// <summary>False skips the entity (and not its children: they have their own renderer).</summary>
    public bool Visible { get; set; } = true;
}

/// <summary>Draw counts of the last <see cref="Renderer3D.End"/>.</summary>
/// <param name="Meshes">Meshes drawn.</param>
/// <param name="DrawCalls">Device draw calls (one per primitive).</param>
/// <param name="Triangles">Triangles submitted.</param>
public readonly record struct Renderer3DStatistics(int Meshes, int DrawCalls, int Triangles);

/// <summary>
/// The 3D path on a tier-1 device: meshes (vertex and index buffers plus a base colour texture) drawn with one
/// material (<c>basic.vert</c>/<c>basic.frag</c>, compiled ahead of time and embedded: base colour texture × base colour ×
/// tint, one directional light and an ambient term), a perspective <see cref="Camera3D"/>, depth testing (less, write)
/// and back-face culling with counter-clockwise front faces. A frame is <see cref="Begin"/>, any number of
/// <see cref="Draw"/> or <see cref="DrawWorld"/>, then <see cref="End"/>, which records one pass into the target and
/// submits it; 2D drawn into the same target afterwards (the 2D batcher without a clear) lands over the 3D.
/// </summary>
/// <remarks>
/// A device without custom shaders or depth refuses the renderer at construction with
/// <see cref="GraphicsCapabilityException"/> (tier 0 has no 3D path). Targets need a depth texture in
/// <see cref="DepthFormat"/> (<see cref="CreateTarget"/> makes one).
/// </remarks>
public sealed class Renderer3D : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct VertexUniforms
    {
        public Matrix4x4 WorldViewProjection;
        public Matrix4x4 World;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FragmentUniforms
    {
        public Vector4 BaseColor;
        public Vector3 LightDirection;
        public float Ambient;
    }

    private readonly record struct Item(GpuMesh Mesh, Matrix4x4 World, ColorF Tint);

    private static readonly CompiledShader VertexShader = Embedded("basic.vert"), FragmentShader = Embedded("basic.frag");
    private static readonly SamplerDescription TextureSampler = SamplerDescription.LinearClamp;

    private readonly IGraphicsDevice _device;
    private readonly Shader _vertex, _fragment;
    private readonly Pipeline _pipeline;
    private readonly Texture _white;
    private readonly List<Item> _items = [];
    private RenderTarget? _target;
    private Camera3D? _camera;
    private ColorF? _clear;
    private RectI? _viewport;
    private Vector3 _lightDirection = Vector3.Normalize(new Vector3(-0.4f, 1, 0.6f));

    /// <summary>Creates the renderer's material pipeline on <paramref name="device"/>.</summary>
    /// <param name="device">A device with <see cref="GraphicsFeatures.Shaders"/> and <see cref="GraphicsFeatures.DepthBuffer"/>.</param>
    /// <param name="colorFormat">The colour format of the targets it draws into.</param>
    /// <param name="depthFormat">The depth format, or null for the first the device supports (D24S8, D32F, D16).</param>
    /// <exception cref="GraphicsCapabilityException">The device has no custom shaders, no depth, or no depth format.</exception>
    public Renderer3D(IGraphicsDevice device, TextureFormat colorFormat = TextureFormat.Rgba8Unorm, TextureFormat? depthFormat = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        var caps = device.Capabilities;
        if (!caps.Has(GraphicsFeatures.Shaders)) throw new GraphicsCapabilityException("Renderer3D", caps.Backend, GraphicsFeatures.Shaders);
        if (!caps.Has(GraphicsFeatures.DepthBuffer)) throw new GraphicsCapabilityException("Renderer3D", caps.Backend, GraphicsFeatures.DepthBuffer);
        DepthFormat = depthFormat ?? new[] { TextureFormat.D24UnormS8, TextureFormat.D32Float, TextureFormat.D16Unorm }.Cast<TextureFormat?>()
            .FirstOrDefault(f => caps.Supports(f!.Value, TextureUsage.DepthStencil))
            ?? throw new GraphicsCapabilityException("Renderer3D", caps.Backend, GraphicsFeatures.None, "a depth format usable as DepthStencil");
        ColorFormat = colorFormat;
        _vertex = VertexShader.Create(device);
        _fragment = FragmentShader.Create(device);
        _pipeline = device.CreatePipeline(new PipelineDescription(_vertex, _fragment, MeshVertex.Layout, BlendState.Opaque,
            DepthState.LessWrite, new RasterState(CullMode.Back, FrontFace.CounterClockwise), colorFormat, DepthFormat));
        _white = device.CreateTexture(new TextureDescription(1, 1, Name: "Renderer3D white"), [255, 255, 255, 255]);
    }

    /// <summary>The device.</summary>
    public IGraphicsDevice Device => _device;

    /// <summary>The colour format of the targets.</summary>
    public TextureFormat ColorFormat { get; }

    /// <summary>The depth format of the targets.</summary>
    public TextureFormat DepthFormat { get; }

    /// <summary>The unit direction from surfaces towards the light, in world space.</summary>
    /// <exception cref="ArgumentException">Zero or not finite.</exception>
    public Vector3 LightDirection
    {
        get => _lightDirection;
        set => _lightDirection = value.LengthSquared() > 0 && float.IsFinite(value.LengthSquared()) ? Vector3.Normalize(value) : throw new ArgumentException("the light direction needs a finite, non-zero vector", nameof(value));
    }

    /// <summary>Light that reaches surfaces facing away from the light, 0..1.</summary>
    public float Ambient { get; set; } = 0.35f;

    /// <summary>The counts of the last frame.</summary>
    public Renderer3DStatistics Statistics { get; private set; }

    /// <summary>A target the renderer can draw into: <paramref name="width"/> × <paramref name="height"/> in <see cref="ColorFormat"/> with depth in <see cref="DepthFormat"/>.</summary>
    public RenderTarget CreateTarget(int width, int height, string? name = null)
        => _device.CreateRenderTarget(new RenderTargetDescription(width, height, ColorFormat, DepthFormat, name));

    /// <summary>Uploads a mesh: one vertex and index buffer per primitive, its base colour texture premultiplied.</summary>
    public GpuMesh Upload(MeshData mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var gpu = new GpuMesh(mesh.Name, mesh.Bounds());
        var list = _device.BeginCommands();
        foreach (var p in mesh.Primitives)
        {
            if (p.Vertices.Length == 0 || p.Indices.Length == 0) continue;
            var vertices = _device.CreateBuffer(new BufferDescription(p.Vertices.Length * MeshVertex.SizeInBytes, BufferUsage.Vertex, $"{mesh.Name} vertices"));
            var indices = _device.CreateBuffer(new BufferDescription(p.Indices.Length * sizeof(uint), BufferUsage.Index, $"{mesh.Name} indices"));
            list.UpdateBuffer<MeshVertex>(vertices, 0, p.Vertices);
            list.UpdateBuffer<uint>(indices, 0, p.Indices);
            Texture texture = _white;
            if (p.BaseColorTexture is { } image)
            {
                var texels = (byte[])image.Rgba.Clone();
                PremultipliedColor.PremultiplyInPlace(texels);
                texture = _device.CreateTexture(new TextureDescription(image.Width, image.Height, Name: $"{mesh.Name} base colour"), texels);
            }
            var c = p.BaseColor;
            gpu.Parts.Add(new GpuMesh.Part(vertices, indices, p.Indices.Length, texture, !ReferenceEquals(texture, _white), new ColorF(c.X * c.W, c.Y * c.W, c.Z * c.W, c.W)));
        }
        _device.Submit(list);
        return gpu;
    }

    /// <summary>Starts a frame into <paramref name="target"/> seen by <paramref name="camera"/>.</summary>
    /// <param name="target">A target with a depth texture in <see cref="DepthFormat"/> and colour in <see cref="ColorFormat"/>.</param>
    /// <param name="camera">The camera; its aspect is the viewport's.</param>
    /// <param name="clear">A premultiplied colour to clear to first, or null to draw over the target's contents (depth is always cleared).</param>
    /// <param name="viewport">The viewport in target pixels, or null for the whole target.</param>
    /// <exception cref="InvalidOperationException">A frame is already open, or the target's formats differ from the renderer's.</exception>
    public void Begin(RenderTarget target, Camera3D camera, ColorF? clear = null, RectI? viewport = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(camera);
        if (_target is not null) throw new InvalidOperationException("Begin inside a frame; call End first");
        if (target.ColorFormat != ColorFormat || target.DepthFormat != DepthFormat)
            throw new InvalidOperationException($"the target is {target.ColorFormat}/{target.DepthFormat?.ToString() ?? "no depth"}; the renderer draws to {ColorFormat}/{DepthFormat} (use CreateTarget)");
        _target = target;
        _camera = camera;
        _clear = clear;
        _viewport = viewport;
        _items.Clear();
    }

    /// <summary>Draws <paramref name="mesh"/> placed by <paramref name="world"/> (model to world), tinted.</summary>
    public void Draw(GpuMesh mesh, in Matrix4x4 world, ColorF? tint = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (_target is null) throw new InvalidOperationException("Draw outside a frame; call Begin first");
        _items.Add(new Item(mesh, world, tint ?? ColorF.White));
    }

    /// <summary>
    /// Draws every visible <see cref="MeshRenderer"/> of <paramref name="world"/> at its interpolated world matrix
    /// (<see cref="IWorld.WorldMatrix"/>: the transform hierarchy at the world's blend), in attach order.
    /// </summary>
    public void DrawWorld(IWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var all = world.All<MeshRenderer>();
        for (var e = all.GetEnumerator(); e.MoveNext();)
            if (e.Current.Visible) Draw(e.Current.Mesh, world.WorldMatrix(e.Entity), e.Current.Tint);
    }

    /// <summary>Records the frame as one pass (colour loaded or cleared, depth cleared) and submits it.</summary>
    public void End()
    {
        var target = _target ?? throw new InvalidOperationException("End without Begin");
        var camera = _camera!;
        _target = null;
        var viewport = _viewport ?? target.Bounds;
        var viewProjection = camera.ViewProjection((float)viewport.Width / viewport.Height);
        var sampler = _device.GetSampler(TextureSampler);
        var list = _device.BeginCommands();
        var color = _clear is { } clear ? new ColorAttachment(target, LoadOp.Clear, StoreOp.Store, clear) : new ColorAttachment(target, LoadOp.Load);
        list.BeginPass(new PassDescription(color, new DepthAttachment(target, LoadOp.Clear, StoreOp.DontCare, 1)));
        if (_viewport is { } v) list.SetViewport(v);
        list.SetPipeline(_pipeline);
        int draws = 0, triangles = 0;
        foreach (var item in _items)
        {
            var vertexUniforms = new VertexUniforms { WorldViewProjection = item.World * viewProjection, World = item.World };
            list.SetUniforms(ShaderStage.Vertex, 0, vertexUniforms);
            foreach (var part in item.Mesh.Parts)
            {
                var c = part.BaseColor;
                var t = item.Tint;
                var fragmentUniforms = new FragmentUniforms { BaseColor = new Vector4(c.R * t.R, c.G * t.G, c.B * t.B, c.A * t.A), LightDirection = _lightDirection, Ambient = Ambient };
                list.SetUniforms(ShaderStage.Fragment, 0, fragmentUniforms);
                list.SetTexture(0, part.Texture, sampler);
                list.SetVertices(part.Vertices);
                list.SetIndices(part.Indices, IndexSize.Bits32);
                list.DrawIndexed(part.IndexCount);
                draws++;
                triangles += part.IndexCount / 3;
            }
        }
        list.EndPass();
        _device.Submit(list);
        Statistics = new Renderer3DStatistics(_items.Count, draws, triangles);
        _items.Clear();
    }

    /// <summary>Releases the pipeline, shaders and the white texture (meshes are released by their owner).</summary>
    public void Dispose()
    {
        _pipeline.Dispose();
        _vertex.Dispose();
        _fragment.Dispose();
        _white.Dispose();
    }

    private static CompiledShader Embedded(string name)
    {
        using var stream = typeof(Renderer3D).Assembly.GetManifestResourceStream($"shaders/{name}{CompiledShader.FileExtension}")
                           ?? throw new InvalidOperationException($"missing embedded shader {name}");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return CompiledShader.Parse(bytes);
    }
}
