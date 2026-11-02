using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;

[assembly: DoNotParallelize]

namespace Arch1.Graphics.Conformance;

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
    public RectI BoundsOfNot(uint background)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (this[x, y] != background)
                {
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                }
        return maxX < 0 ? default : new RectI(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public static string Hex(uint pixel) => $"#{pixel:X8}";
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

    public static uint Pack(ColorF c) => (uint)(Byte(c.R) << 24 | Byte(c.G) << 16 | Byte(c.B) << 8 | Byte(c.A));

    public static byte Byte(float v) => (byte)Math.Clamp(MathF.Round(v * 255f), 0, 255);
}

/// <summary>A vertex of the depth test mesh shader (P3's mesh.vert: position Float3 @0, colour Float4 @12).</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MeshVertex(Vector3 Position, Vector4 Color)
{
    public static readonly VertexLayout Layout = new(28, [new(0, VertexFormat.Float3, 0), new(1, VertexFormat.Float4, 12)]);
}

/// <summary>Loads the offline-compiled custom shaders copied next to the test assembly.</summary>
public static class ShaderFiles
{
    private sealed record Reflection(int Samplers, int StorageTextures, int StorageBuffers, int UniformBuffers, int Inputs, int Outputs);

    public static ShaderDescription Load(string name, ShaderFormat format)
    {
        var (extension, entry) = format switch
        {
            ShaderFormat.Dxil => ("dxil", "main"),
            ShaderFormat.SpirV => ("spv", "main"),
            _ => ("msl", "main0"),
        };
        var directory = Path.Combine(AppContext.BaseDirectory, "shaders");
        var code = File.ReadAllBytes(Path.Combine(directory, $"{name}.{extension}"));
        var r = JsonSerializer.Deserialize<Reflection>(File.ReadAllText(Path.Combine(directory, $"{name}.json")))!;
        var stage = name.EndsWith(".vert", StringComparison.Ordinal) ? ShaderStage.Vertex : ShaderStage.Fragment;
        return new ShaderDescription(stage, format, code, entry, new ShaderResources(r.Samplers, r.UniformBuffers));
    }

    /// <summary>The format a device should be given: DXIL on Direct3D 12, SPIR-V on Vulkan, MSL on Metal.</summary>
    public static ShaderFormat For(GraphicsCapabilities caps) => caps.ApiName switch
    {
        "direct3d12" => ShaderFormat.Dxil,
        "metal" => ShaderFormat.Msl,
        _ => ShaderFormat.SpirV,
    };
}
