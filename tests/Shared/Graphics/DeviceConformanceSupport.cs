using System.Numerics;
using MicroEngine.Graphics.Devices;
using MicroEngine.Platform;

namespace MicroEngine.Graphics.Conformance;

/// <summary>An RGBA8 image read back from a target.</summary>
public sealed class Image(int width, int height, byte[] rgba)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Rgba { get; } = rgba;

    /// <summary>Pixel as 0xRRGGBBAA.</summary>
    public uint this[int x, int y]
    {
        get
        {
            var i = (y * Width + x) * 4;
            return (uint)(Rgba[i] << 24 | Rgba[i + 1] << 16 | Rgba[i + 2] << 8 | Rgba[i + 3]);
        }
    }

    public byte Channel(int x, int y, int channel) => Rgba[(y * Width + x) * 4 + channel];

    /// <summary>The rectangle covering every pixel different from <paramref name="background"/> (empty when none).</summary>
    public RectI BoundsOfNot(uint background) => BoundsWhere(p => p != background);

    /// <summary>The rectangle covering every pixel equal to <paramref name="color"/> (empty when none).</summary>
    public RectI BoundsOf(uint color) => BoundsWhere(p => p == color);

    private RectI BoundsWhere(Func<uint, bool> match)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (match(this[x, y]))
                {
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                }
        return maxX < 0 ? default : new RectI(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}

/// <summary>Byte helpers for textures written as 0xRRGGBBAA values.</summary>
public static class Texels
{
    public static byte[] Bytes(params uint[] pixels)
    {
        var bytes = new byte[pixels.Length * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            bytes[i * 4] = (byte)(pixels[i] >> 24);
            bytes[i * 4 + 1] = (byte)(pixels[i] >> 16);
            bytes[i * 4 + 2] = (byte)(pixels[i] >> 8);
            bytes[i * 4 + 3] = (byte)pixels[i];
        }
        return bytes;
    }
}

/// <summary>One textured quad for <see cref="Painter"/>: a source rectangle placed by a world transform.</summary>
/// <param name="Texture">The texture.</param>
/// <param name="Source">The texel rectangle, drawn as Source.Width × Source.Height local units.</param>
/// <param name="World">Local units to target pixels.</param>
/// <param name="Tint">Premultiplied tint.</param>
public readonly record struct Quad(Texture Texture, RectF Source, Matrix3x2 World, ColorF Tint);

/// <summary>
/// The smallest client of the contract the suite needs: one command list per call, one pass, the built-in
/// <see cref="BuiltInPipeline.Textured"/> pipeline, one indexed draw per quad (no batching: the batcher has its own
/// tests). Vertices are transformed on the CPU into target pixels, as the contract's built-in vertex space requires.
/// </summary>
public sealed class Painter(IGraphicsDevice device) : IDisposable
{
    private GpuBuffer? _vertices, _indices;

    /// <summary>Draws <paramref name="quads"/> into <paramref name="target"/> in one pass.</summary>
    public void Paint(RenderTarget target, IReadOnlyList<Quad> quads, ColorF? clear = null, BlendState? blend = null,
        SamplerDescription? sampler = null, RectI? scissor = null, RectI? viewport = null)
    {
        var commands = device.BeginCommands();
        if (quads.Count > 0)
        {
            var vertices = new Vertex2D[quads.Count * 4];
            var indices = new ushort[quads.Count * 6];
            for (int q = 0; q < quads.Count; q++)
            {
                var (texture, s, m, tint) = quads[q];
                float u0 = s.X / texture.Width, v0 = s.Y / texture.Height, u1 = s.Right / texture.Width, v1 = s.Bottom / texture.Height;
                vertices[q * 4] = new(Vector2.Transform(new(0, 0), m), new(u0, v0), tint);
                vertices[q * 4 + 1] = new(Vector2.Transform(new(s.Width, 0), m), new(u1, v0), tint);
                vertices[q * 4 + 2] = new(Vector2.Transform(new(s.Width, s.Height), m), new(u1, v1), tint);
                vertices[q * 4 + 3] = new(Vector2.Transform(new(0, s.Height), m), new(u0, v1), tint);
                int b = q * 4;
                indices.AsSpan(q * 6, 6).SetValues((ushort)b, (ushort)(b + 1), (ushort)(b + 2), (ushort)b, (ushort)(b + 2), (ushort)(b + 3));
            }
            _vertices = Ensure(_vertices, vertices.Length * Vertex2D.SizeInBytes, BufferUsage.Vertex);
            _indices = Ensure(_indices, indices.Length * sizeof(ushort), BufferUsage.Index);
            commands.UpdateBuffer<Vertex2D>(_vertices, 0, vertices);
            commands.UpdateBuffer<ushort>(_indices, 0, indices);
        }
        commands.BeginPass(new PassDescription(new ColorAttachment(target, clear is null ? LoadOp.Load : LoadOp.Clear, StoreOp.Store, clear ?? default)));
        if (viewport is { } v) commands.SetViewport(v);
        if (scissor is { } r) commands.SetScissor(r);
        if (quads.Count > 0)
        {
            commands.SetPipeline(device.GetBuiltInPipeline(BuiltInPipeline.Textured, blend ?? BlendState.AlphaPremultiplied, target.ColorFormat));
            commands.SetVertices(_vertices!);
            commands.SetIndices(_indices!, IndexSize.Bits16);
            var s = device.GetSampler(sampler ?? SamplerDescription.PointClamp);
            for (int q = 0; q < quads.Count; q++)
            {
                commands.SetTexture(0, quads[q].Texture, s);
                commands.DrawIndexed(6, q * 6);
            }
        }
        commands.EndPass();
        device.Submit(commands);
    }

    /// <summary>One quad, in one pass.</summary>
    public void Paint(RenderTarget target, Quad quad, ColorF? clear = null, BlendState? blend = null, SamplerDescription? sampler = null)
        => Paint(target, [quad], clear, blend, sampler);

    private GpuBuffer Ensure(GpuBuffer? buffer, int bytes, BufferUsage usage)
    {
        if (buffer is not null && buffer.SizeInBytes >= bytes) return buffer;
        buffer?.Dispose();
        return device.CreateBuffer(new BufferDescription(Math.Max(bytes, 1024), usage));
    }

    public void Dispose()
    {
        _vertices?.Dispose();
        _indices?.Dispose();
    }
}

internal static class SpanExtensions
{
    public static void SetValues(this Span<ushort> span, params ReadOnlySpan<ushort> values) => values.CopyTo(span);
}

/// <summary>A surface for the suite: hidden windows on native backends, a size in memory on the reference device.</summary>
public interface ITestSurface : ISurface, IDisposable
{
    /// <summary>Minimises (true) or restores the surface; false when this surface cannot be minimised here.</summary>
    bool TrySetMinimized(bool minimized);
}

/// <summary>An in-memory surface: a fixed pixel size and a minimised flag, no window.</summary>
public sealed class MemorySurface(int width, int height) : ITestSurface
{
    private bool _minimized;

    public nint NativeWindow => 0;
    public (int Width, int Height) PixelSize => (width, height);
    public float PixelDensity => 1;
    public bool IsMinimized => _minimized;

    public event Action? Resized { add { } remove { } }
    public event Action? Lost { add { } remove { } }

    public bool TrySetMinimized(bool minimized)
    {
        _minimized = minimized;
        return true;
    }

    public void Dispose() { }
}
