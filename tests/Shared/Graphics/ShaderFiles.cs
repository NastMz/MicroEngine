using System.Numerics;
using System.Runtime.InteropServices;
using MicroEngine.Graphics.Devices;

namespace MicroEngine.Conformance.Graphics;

/// <summary>
/// The suite's custom shaders: HLSL in tests/Shared/Graphics/Shaders, compiled ahead of time by
/// <c>microengine shaders</c> into <c>.shader</c> files (DXIL, SPIR-V and MSL with reflected resource counts), copied next
/// to the test assembly under <c>shaders/</c>.
/// </summary>
public static class ShaderFiles
{
    /// <summary>The compiled shader <paramref name="name"/> ("mesh.vert", "palette.frag"...).</summary>
    public static CompiledShader Compiled(string name)
        => CompiledShader.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "shaders", name + CompiledShader.FileExtension)));

    /// <summary>The shader <paramref name="name"/> in <paramref name="format"/>, whatever the device declares.</summary>
    public static ShaderDescription Load(string name, ShaderFormat format)
    {
        var compiled = Compiled(name);
        if (!compiled.TryGetCode(format, out var code)) throw new InvalidOperationException($"{name} was not compiled for {format}");
        return new ShaderDescription(compiled.Stage, format, code.Bytes, code.EntryPoint, compiled.Resources);
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
