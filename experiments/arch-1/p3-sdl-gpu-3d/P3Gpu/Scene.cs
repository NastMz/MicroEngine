using System.Numerics;
using System.Runtime.InteropServices;

namespace P3Gpu;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct MeshVertex(Vector3 Position, Vector4 Color); // 28 bytes

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct SpriteVertex(Vector2 Position, Vector2 TexCoord, Vector4 Color); // 32 bytes

public sealed record SceneObject(string Name, Matrix4x4 World, MeshVertex[] Vertices);

public enum SpriteTexture { Opaque, Half }

public sealed record Sprite(string Name, SpriteTexture Texture, Vector2 Position, Vector2 Size, Vector4 Color);

/// <summary>
/// The test scene, shared by both variants: three cubes (near red, far green, an up-right blue marker), clip-space
/// probes for the depth range, and 2D overlay quads. Expected pixels are computed on the CPU with the same matrices.
/// </summary>
public static class Scene
{
    public const int Width = 640;
    public const int Height = 480;
    public const float Fov = MathF.PI / 3f;
    public const float Near = 0.1f;
    public const float Far = 100f;
    public const float Distance = 6f;

    // Byte-exact clear colour (32, 64, 96).
    public static readonly Vector4 Clear = new(32 / 255f, 64 / 255f, 96 / 255f, 1);
    public static readonly float HalfAlpha = 128 / 255f;

    public static readonly Vector4 Red = new(1, 0, 0, 1), Magenta = new(1, 0, 1, 1), Green = new(0, 1, 0, 1),
        Blue = new(0, 0, 1, 1), Cyan = new(0, 1, 1, 1), Yellow = new(1, 1, 0, 1), Grey = new(0.5f, 0.5f, 0.5f, 1);

    // Face order: +X, -X, +Y, -Y, +Z, -Z. +Z faces the camera at angle 0; -Z would show if culling were inverted.
    public static readonly SceneObject NearCube = new("near cube",
        Matrix4x4.CreateScale(0.5f) * Matrix4x4.CreateTranslation(0, 0, 1),
        Cube([new(0.6f, 0, 0, 1), new(0.6f, 0, 0, 1), new(0.8f, 0.2f, 0.2f, 1), new(0.4f, 0, 0, 1), Red, Magenta]));

    public static readonly SceneObject FarCube = new("far cube",
        Matrix4x4.CreateScale(1.5f) * Matrix4x4.CreateTranslation(0, 0, -2),
        Cube([new(0, 0.5f, 0, 1), new(0, 0.5f, 0, 1), new(0.2f, 0.7f, 0.2f, 1), new(0, 0.3f, 0, 1), Green, Grey]));

    public static readonly SceneObject Marker = new("up-right marker",
        Matrix4x4.CreateScale(0.3f) * Matrix4x4.CreateTranslation(1.8f, 1.2f, 0),
        Cube([new(0, 0, 0.5f, 1), new(0, 0, 0.5f, 1), new(0.2f, 0.2f, 0.7f, 1), new(0, 0, 0.3f, 1), Blue, new(0.5f, 0.5f, 1, 1)]));

    /// <summary>Drawn in this order: near before far, so only the depth test can keep the near cube in front.</summary>
    public static readonly SceneObject[] Objects = [NearCube, FarCube, Marker];

    /// <summary>Clip-space quads drawn with an identity matrix: z = -0.25 and z = 1.25 must be clipped (depth 0..1), z = 0.25 visible.</summary>
    public static readonly MeshVertex[] Probes =
    [
        .. Quad(new(-1, 0.8f), new(-0.8f, 1), -0.25f, Magenta), // top-left: clipped
        .. Quad(new(0.8f, 0.8f), new(1, 1), 0.25f, Cyan),       // top-right: visible
        .. Quad(new(0.8f, -1), new(1, -0.8f), 1.25f, Yellow),   // bottom-right: clipped
    ];

    public static Matrix4x4 View(float angleDegrees)
    {
        var a = angleDegrees * MathF.PI / 180f;
        return Matrix4x4.CreateLookAt(new Vector3(Distance * MathF.Sin(a), 0, Distance * MathF.Cos(a)), Vector3.Zero, Vector3.UnitY);
    }

    public static Matrix4x4 Projection => Matrix4x4.CreatePerspectiveFieldOfView(Fov, Width / (float)Height, Near, Far);

    public static Matrix4x4 Ortho => Matrix4x4.CreateOrthographicOffCenter(0, Width, Height, 0, 0, 1);

    public static Vector2 ToPixel(Vector3 ndc) => new((ndc.X + 1) / 2 * Width, (1 - ndc.Y) / 2 * Height);

    public static Vector2 Project(Vector3 world, float angleDegrees)
        => ToPixel(Conventions.Ndc(world, View(angleDegrees) * Projection));

    public static (int X, int Y) Pixel(Vector2 p) => ((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y));

    public static Vector2 Center => Project(new Vector3(0, 0, 1.5f), 0);

    /// <summary>2D overlay, pixel coordinates (top-left origin, y down), drawn after the 3D pass on the same frame.</summary>
    public static Sprite[] Sprites
    {
        get
        {
            var center = Center;
            var farRight = Project(new Vector3(1.0f, 0, -0.5f), 0);
            return
            [
                new("opaque overlay on the near cube", SpriteTexture.Opaque, center - new Vector2(40, 40), new(20, 20), Yellow),
                new("translucent overlay on the clear colour", SpriteTexture.Half, new(20, Height - 80), new(60, 60), Blue),
                new("translucent overlay on the far cube", SpriteTexture.Half, farRight - new Vector2(8, 8), new(16, 16), Blue),
            ];
        }
    }

    public static SpriteVertex[] SpriteVertices(IEnumerable<Sprite> sprites)
    {
        var list = new List<SpriteVertex>();
        foreach (var s in sprites)
        {
            Vector2 a = s.Position, b = s.Position + s.Size;
            list.AddRange([
                new(a, new(0, 0), s.Color), new(new(b.X, a.Y), new(1, 0), s.Color), new(b, new(1, 1), s.Color),
                new(a, new(0, 0), s.Color), new(b, new(1, 1), s.Color), new(new(a.X, b.Y), new(0, 1), s.Color)]);
        }
        return [.. list];
    }

    /// <summary>4x4 RGBA8 textures: opaque white, and white with alpha 128 (straight alpha).</summary>
    public static byte[] TexturePixels(SpriteTexture texture)
    {
        var pixels = new byte[4 * 4 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = pixels[i + 1] = pixels[i + 2] = 255;
            pixels[i + 3] = texture == SpriteTexture.Opaque ? (byte)255 : (byte)128;
        }
        return pixels;
    }

    public static readonly ushort[] CubeIndices = CreateCubeIndices();

    /// <summary>
    /// The frame assertions shared by both variants. <paramref name="angle"/> is the camera angle of the frame;
    /// overlay checks only apply at angle 0 where the overlay was laid out.
    /// </summary>
    public static void Assert(Checks c, Image image, float angle, bool withOverlay)
    {
        Rgba At((int X, int Y) p) => image[p.X, p.Y];
        var clear = Rgba.From(Clear);
        c.Exact("clear colour", "bottom-left corner, nothing drawn", At((2, Height - 3)), clear);
        c.Exact("depth range", "probe at clip z=-0.25 is clipped (top-left)", At((10, 10)), clear);
        c.Exact("depth range", "probe at clip z=+0.25 is visible (top-right)", At((Width - 10, 10)), Rgba.From(Cyan));
        c.Exact("depth range", "probe at clip z=1.25 is clipped (bottom-right)", At((Width - 10, Height - 10)), clear);

        var nearFace = Pixel(Project(new Vector3(0, 0, 1.5f), angle));
        c.Exact("depth ordering", $"near cube front face occludes the far cube at {nearFace} (near drawn first)", At(nearFace), Rgba.From(Red));
        var farFace = Pixel(Project(new Vector3(-1.0f, 0, -0.5f), angle));
        c.Exact("depth ordering", $"far cube front face visible beside the near cube at {farFace}", At(farFace), Rgba.From(Green));
        var marker = Pixel(Project(new Vector3(1.8f, 1.2f, 0.3f), angle));
        c.True("axes", $"world (+X,+Y) marker projects to the upper-right quadrant {marker}", marker.X > Width / 2 && marker.Y < Height / 2, "CPU projection");
        c.Exact("axes", $"GPU draws the marker where System.Numerics projects it {marker}", At(marker), Rgba.From(Blue));

        if (!withOverlay) return;
        var sprites = Sprites;
        var opaque = Pixel(sprites[0].Position + sprites[0].Size / 2);
        c.Exact("2D over 3D", $"opaque overlay on top of the depth-tested near cube at {opaque}", At(opaque), Rgba.From(Yellow));
        var onClear = Pixel(sprites[1].Position + sprites[1].Size / 2);
        c.Near("alpha blend", $"half-alpha blue over the clear colour at {onClear}", At(onClear), Blend(Blue, Clear), 1, "8-bit quantization of a blended value");
        var onFar = Pixel(sprites[2].Position + sprites[2].Size / 2);
        c.Near("alpha blend", $"half-alpha blue over the far cube at {onFar}", At(onFar), Blend(Blue, Green), 1, "8-bit quantization of a blended value");
    }

    /// <summary>Straight-alpha "over": rgb = src*a + dst*(1-a), alpha = a + dstA*(1-a).</summary>
    public static Rgba Blend(Vector4 src, Vector4 dst)
    {
        var a = HalfAlpha;
        var rgb = new Vector3(src.X, src.Y, src.Z) * a + new Vector3(dst.X, dst.Y, dst.Z) * (1 - a);
        return Rgba.From(new Vector4(rgb, a + dst.W * (1 - a)));
    }

    private static MeshVertex[] Cube(Vector4[] faceColors)
    {
        (Vector3 N, Vector3 U, Vector3 V)[] faces =
        [
            (Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY), (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
            (Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ), (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
            (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY), (-Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY),
        ];
        var vertices = new List<MeshVertex>();
        for (var i = 0; i < faces.Length; i++)
        {
            var (n, u, v) = faces[i]; // u x v = n, so (-u-v, u-v, u+v, -u+v) is CCW seen from outside.
            foreach (var corner in new[] { -u - v, u - v, u + v, -u + v })
                vertices.Add(new MeshVertex(n + corner, faceColors[i]));
        }
        return [.. vertices];
    }

    private static ushort[] CreateCubeIndices()
    {
        var indices = new List<ushort>();
        for (ushort f = 0; f < 6; f++)
        {
            var b = (ushort)(f * 4);
            indices.AddRange([b, (ushort)(b + 1), (ushort)(b + 2), b, (ushort)(b + 2), (ushort)(b + 3)]);
        }
        return [.. indices];
    }

    private static MeshVertex[] Quad(Vector2 min, Vector2 max, float z, Vector4 color) =>
    [
        new(new(min.X, min.Y, z), color), new(new(max.X, min.Y, z), color), new(new(max.X, max.Y, z), color),
        new(new(min.X, min.Y, z), color), new(new(max.X, max.Y, z), color), new(new(min.X, max.Y, z), color),
    ];
}
