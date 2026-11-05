using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Conformance.Graphics;

/// <summary>
/// The suite's custom shaders (tests/Shared/Graphics/Shaders: HLSL sources and their offline-compiled DXIL, SPIR-V and
/// MSL with the reflected resource counts), copied next to the test assembly under <c>shaders/</c>.
/// </summary>
public static class ShaderFiles
{
    private sealed record Reflection(int Samplers, int StorageTextures, int StorageBuffers, int UniformBuffers);

    /// <summary>The shader <paramref name="name"/> ("mesh.vert", "color.frag") in <paramref name="format"/>.</summary>
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

    /// <summary>The format to give a device: DXIL on Direct3D 12, MSL on Metal, SPIR-V otherwise.</summary>
    public static ShaderFormat For(GraphicsCapabilities caps) => caps.ApiName switch
    {
        "direct3d12" => ShaderFormat.Dxil,
        "metal" => ShaderFormat.Msl,
        _ => ShaderFormat.SpirV,
    };
}

/// <summary>The vertex of the suite's mesh shader: position (location 0) and colour (location 1), 28 bytes.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MeshVertex(Vector3 Position, Vector4 Color)
{
    public static readonly VertexLayout Layout = new(28, [new(0, VertexFormat.Float3, 0), new(1, VertexFormat.Float4, 12)]);
}
