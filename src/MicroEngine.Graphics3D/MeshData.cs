using System.Numerics;
using System.Runtime.InteropServices;

namespace MicroEngine.Graphics3D;

/// <summary>
/// The vertex of the 3D path, 32 bytes: position, normal and texture coordinate, in the layout
/// <see cref="Layout"/> describes (locations 0, 1 and 2 of the mesh shader).
/// </summary>
/// <param name="Position">Model-space position.</param>
/// <param name="Normal">Model-space normal (unit length, or zero when the mesh has none).</param>
/// <param name="TexCoord">Texture coordinate; (0, 0) is the top-left corner of the base colour image (glTF's convention).</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MeshVertex(Vector3 Position, Vector3 Normal, Vector2 TexCoord)
{
    /// <summary>Size of one vertex in bytes.</summary>
    public const int SizeInBytes = 32;

    /// <summary>The vertex layout of the mesh pipeline.</summary>
    public static readonly Graphics.Devices.VertexLayout Layout = new(SizeInBytes,
        [new(0, Graphics.Devices.VertexFormat.Float3, 0), new(1, Graphics.Devices.VertexFormat.Float3, 12), new(2, Graphics.Devices.VertexFormat.Float2, 24)]);
}

/// <summary>An RGBA8 image in memory: rows top to bottom, straight (not premultiplied) alpha, as glTF images are.</summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Rgba">Width × Height × 4 bytes.</param>
public sealed record ImageData(int Width, int Height, byte[] Rgba);

/// <summary>One drawable part of a mesh: an indexed triangle list with one material.</summary>
/// <param name="Vertices">Vertices.</param>
/// <param name="Indices">Triangle list indices into <paramref name="Vertices"/>, counter-clockwise front faces.</param>
/// <param name="BaseColor">The material's base colour factor (straight RGBA, multiplied with the texture).</param>
/// <param name="BaseColorTexture">The material's base colour image, or null for the factor alone.</param>
public sealed record MeshPrimitive(MeshVertex[] Vertices, uint[] Indices, Vector4 BaseColor, ImageData? BaseColorTexture);

/// <summary>
/// A mesh on the CPU, as the <c>mesh</c> asset kind loads it from glTF 2.0: one or more primitives, each with its base
/// colour. The renderer uploads it once (<see cref="Renderer3D.Upload"/>).
/// </summary>
/// <param name="Name">The glTF mesh name, or the file name.</param>
/// <param name="Primitives">The primitives, in file order.</param>
public sealed record MeshData(string Name, IReadOnlyList<MeshPrimitive> Primitives)
{
    /// <summary>Total vertex count.</summary>
    public int VertexCount => Primitives.Sum(p => p.Vertices.Length);

    /// <summary>Total index count.</summary>
    public int IndexCount => Primitives.Sum(p => p.Indices.Length);

    /// <summary>The axis-aligned bounds of every vertex (model space).</summary>
    public (Vector3 Min, Vector3 Max) Bounds()
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in Primitives)
            foreach (var v in p.Vertices)
            {
                min = Vector3.Min(min, v.Position);
                max = Vector3.Max(max, v.Position);
            }
        return (min, max);
    }

    /// <summary>Bytes the CPU copy holds (vertices, indices and images), for the asset store's diagnostics.</summary>
    public long SizeInBytes => Primitives.Sum(p => (long)p.Vertices.Length * MeshVertex.SizeInBytes + p.Indices.Length * 4L + (p.BaseColorTexture?.Rgba.Length ?? 0));
}
