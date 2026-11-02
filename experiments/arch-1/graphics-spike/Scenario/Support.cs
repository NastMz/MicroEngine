using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using SDL;
using static SDL.SDL3;

namespace Arch1.Graphics.Scenario;

/// <summary>An RGBA8 frame read back from a target.</summary>
public sealed class Frame(int width, int height, byte[] rgba)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Rgba { get; } = rgba;

    public uint this[int x, int y] => (uint)(Rgba[(y * Width + x) * 4] << 24 | Rgba[(y * Width + x) * 4 + 1] << 16 | Rgba[(y * Width + x) * 4 + 2] << 8 | Rgba[(y * Width + x) * 4 + 3]);

    public byte Channel(int x, int y, int c) => Rgba[(y * Width + x) * 4 + c];

    public static Frame Read(IGraphicsDevice device, RenderTarget target)
    {
        var request = device.ReadPixels(target, target.Bounds);
        request.Wait();
        return new Frame(target.Width, target.Height, request.Pixels.ToArray());
    }

    public static unsafe Frame LoadPng(string path)
    {
        var loaded = SDL_LoadPNG(path);
        if (loaded == null) throw new InvalidOperationException($"SDL_LoadPNG({path}): {SDL_GetError()}");
        var surface = SDL_ConvertSurface(loaded, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
        SDL_DestroySurface(loaded);
        var rgba = new byte[surface->w * surface->h * 4];
        for (var y = 0; y < surface->h; y++)
            new ReadOnlySpan<byte>((byte*)surface->pixels + y * surface->pitch, surface->w * 4).CopyTo(rgba.AsSpan(y * surface->w * 4));
        var frame = new Frame(surface->w, surface->h, rgba);
        SDL_DestroySurface(surface);
        return frame;
    }

    public unsafe void SavePng(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        fixed (byte* pixels = Rgba)
        {
            var surface = SDL_CreateSurfaceFrom(Width, Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (nint)pixels, Width * 4);
            if (surface == null || !SDL_SavePNG(surface, path)) throw new InvalidOperationException($"SDL_SavePNG: {SDL_GetError()}");
            SDL_DestroySurface(surface);
        }
    }
}

/// <summary>Named property checks; the exit code is the number of failures.</summary>
public sealed class Checks(string scope)
{
    public int Count { get; private set; }
    public int Failed { get; private set; }

    public void True(string property, string what, bool ok, string detail = "")
    {
        Count++;
        if (!ok) Failed++;
        Console.WriteLine($"  [{(ok ? "pass" : "FAIL")}] {scope} {property}: {what}{(detail.Length > 0 ? $" ({detail})" : "")}");
    }

    public void Exact(string property, string what, uint expected, uint actual)
        => True(property, what, expected == actual, $"expected #{expected:X8}, got #{actual:X8}");

    /// <summary>Per-channel tolerance with its named source of variation.</summary>
    public void Near(string property, string what, float[] expected, Frame frame, int x, int y, float tolerance, string source)
    {
        var worst = 0f;
        for (var c = 0; c < 4; c++) worst = Math.Max(worst, Math.Abs(frame.Channel(x, y, c) - expected[c] * 255f));
        True(property, what, worst <= tolerance,
            $"expected ({string.Join(", ", expected.Select(e => (e * 255f).ToString("F1")))}), got #{frame[x, y]:X8}, worst {worst:F2} <= {tolerance} [{source}]");
    }
}

/// <summary>A mesh vertex of P3's mesh shader: position Float3 @0, colour Float4 @12.</summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MeshVertex(Vector3 Position, Vector4 Color)
{
    public static readonly VertexLayout Layout = new(28, [new(0, VertexFormat.Float3, 0), new(1, VertexFormat.Float4, 12)]);
}

public static class ShaderFiles
{
    private sealed record Reflection(int Samplers, int StorageTextures, int StorageBuffers, int UniformBuffers, int Inputs, int Outputs);

    public static ShaderDescription Load(string name, GraphicsCapabilities caps)
    {
        var (format, extension, entry) = caps.ApiName switch
        {
            "direct3d12" => (ShaderFormat.Dxil, "dxil", "main"),
            "metal" => (ShaderFormat.Msl, "msl", "main0"),
            _ => (ShaderFormat.SpirV, "spv", "main"),
        };
        var directory = Path.Combine(AppContext.BaseDirectory, "shaders");
        var r = JsonSerializer.Deserialize<Reflection>(File.ReadAllText(Path.Combine(directory, $"{name}.json")))!;
        return new ShaderDescription(name.EndsWith(".vert", StringComparison.Ordinal) ? ShaderStage.Vertex : ShaderStage.Fragment, format,
            File.ReadAllBytes(Path.Combine(directory, $"{name}.{extension}")), entry, new ShaderResources(r.Samplers, r.UniformBuffers));
    }
}
