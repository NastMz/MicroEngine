using System.Numerics;
using System.Runtime.InteropServices;

namespace P3Gpu;

/// <summary>
/// CPU-side numeric checks of System.Numerics conventions against SDL_GPU clip space (x right, y up, z into the screen,
/// depth 0..1, viewport origin top-left). The GPU runs confirm the same conventions with pixels.
/// </summary>
public static class Conventions
{
    private const float Epsilon = 1e-5f;
    private const string Float = "float32 rounding";

    public static int Run()
    {
        var c = new Checks("math");
        float n = 0.1f, f = 100f, aspect = 4f / 3f, fov = MathF.PI / 3f;
        var p = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, n, f);

        // Depth range: right-handed projection, camera looks down -Z; -near -> 0, -far -> 1 (D3D/Metal/Vulkan 0..1).
        var nearNdc = Ndc(new Vector3(0, 0, -n), p);
        var farNdc = Ndc(new Vector3(0, 0, -f), p);
        c.Approx("depth range", "CreatePerspectiveFieldOfView maps z=-near to ndc z", nearNdc.Z, 0, Epsilon, Float);
        c.Approx("depth range", "CreatePerspectiveFieldOfView maps z=-far to ndc z", farNdc.Z, 1, 1e-4, Float);
        var behind = Vector4.Transform(new Vector4(0, 0, n, 1), p);
        c.True("handedness", "a point at z=+near is behind the camera (clip w < 0)", behind.W < 0, $"w = {behind.W}");
        c.True("depth range", "clip w of a point in front equals its distance (-z_view)", MathF.Abs(Vector4.Transform(new Vector4(0, 0, -5, 1), p).W - 5) < Epsilon, "w = -z");

        // Axes: +x view -> +x ndc (right), +y view -> +y ndc (up). No mirroring by the projection.
        var right = Ndc(new Vector3(1, 0, -5), p);
        var up = Ndc(new Vector3(0, 1, -5), p);
        c.True("axes", "view +X projects to ndc +X (screen right)", right.X > 0 && MathF.Abs(right.Y) < Epsilon, $"ndc {right}");
        c.True("axes", "view +Y projects to ndc +Y (SDL_GPU: screen up)", up.Y > 0 && MathF.Abs(up.X) < Epsilon, $"ndc {up}");
        c.Approx("projection", "ndc x of (1,0,-5) = f/aspect/5", right.X, 1 / MathF.Tan(fov / 2) / aspect / 5, Epsilon, Float);

        // LookAt is right-handed: forward is -Z of view space.
        var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
        var target = Vector3.Transform(Vector3.Zero, view);
        var worldRight = Vector3.Transform(Vector3.UnitX, view);
        c.True("handedness", "CreateLookAt: target ends at view z = -distance", MathF.Abs(target.Z + 5) < Epsilon, $"view {target}");
        c.True("handedness", "CreateLookAt from +Z: world +X stays view +X", worldRight.X > 0, $"view {worldRight}");

        // Row vectors: Transform(v, A * B) == Transform(Transform(v, A), B); world * view * projection composes left to right.
        var a = Matrix4x4.CreateRotationY(0.7f) * Matrix4x4.CreateTranslation(1, 2, 3);
        var b = Matrix4x4.CreateScale(2) * Matrix4x4.CreateRotationX(0.3f);
        var v = new Vector4(0.3f, -1.2f, 2.5f, 1);
        var composed = Vector4.Transform(v, a * b);
        var chained = Vector4.Transform(Vector4.Transform(v, a), b);
        c.True("multiplication order", "Transform(v, A*B) == Transform(Transform(v, A), B) (row vectors)", Vector4.Distance(composed, chained) < 1e-4f, $"{composed} vs {chained}");
        var manual = new Vector4(
            v.X * a.M11 + v.Y * a.M21 + v.Z * a.M31 + v.W * a.M41,
            v.X * a.M12 + v.Y * a.M22 + v.Z * a.M32 + v.W * a.M42,
            v.X * a.M13 + v.Y * a.M23 + v.Z * a.M33 + v.W * a.M43,
            v.X * a.M14 + v.Y * a.M24 + v.Z * a.M34 + v.W * a.M44);
        c.True("multiplication order", "Transform(v, M) = v * M (row vector times matrix)", Vector4.Distance(manual, Vector4.Transform(v, a)) < 1e-5f, $"{manual}");

        // Memory layout: row-major, translation in M41..M43 (floats 12..14). HLSL's default column_major cbuffer packing
        // reads this as the transpose, so the shader's mul(M, v) equals C#'s v * M with no transpose on upload.
        var t = Matrix4x4.CreateTranslation(7, 8, 9);
        var floats = MemoryMarshal.Cast<Matrix4x4, float>(new ReadOnlySpan<Matrix4x4>(in t));
        c.True("memory layout", "Matrix4x4 memory is M11,M12,...,M44; translation at floats 12,13,14", floats[12] == 7 && floats[13] == 8 && floats[14] == 9 && floats[3] == 0, $"[{string.Join(",", floats.ToArray())}]");

        // Winding: a CCW quad in a right-handed world, seen from outside, stays CCW in ndc (y up). SDL_GPU's
        // FRONTFACE_COUNTER_CLOCKWISE is defined in that space; the GPU runs check the visible face colour.
        var mvp = Matrix4x4.CreateLookAt(new Vector3(0, 0, 6), Vector3.Zero, Vector3.UnitY) * p;
        var q0 = Ndc(new Vector3(-1, -1, 1), mvp);
        var q1 = Ndc(new Vector3(1, -1, 1), mvp);
        var q2 = Ndc(new Vector3(1, 1, 1), mvp);
        var area = (q1.X - q0.X) * (q2.Y - q0.Y) - (q2.X - q0.X) * (q1.Y - q0.Y);
        c.True("winding", "outward +Z face (CCW from outside) has positive signed area in ndc", area > 0, $"area {area}");

        // Left-handed helpers exist in .NET 10 (Matrix4x4.*LeftHanded): +Z forward, same 0..1 depth.
        var lh = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fov, aspect, n, f);
        c.Approx("depth range", "CreatePerspectiveFieldOfViewLeftHanded maps z=+near to 0", Ndc(new Vector3(0, 0, n), lh).Z, 0, Epsilon, Float);
        c.Approx("depth range", "CreatePerspectiveFieldOfViewLeftHanded maps z=+far to 1", Ndc(new Vector3(0, 0, f), lh).Z, 1, 1e-4, Float);
        var lhView = Matrix4x4.CreateLookAtLeftHanded(new Vector3(0, 0, -5), Vector3.Zero, Vector3.UnitY);
        c.True("handedness", "CreateLookAtLeftHanded from -Z: target at view z = +distance", MathF.Abs(Vector3.Transform(Vector3.Zero, lhView).Z - 5) < Epsilon, "+Z forward");

        // 2D: CreateOrthographicOffCenter(0, W, H, 0, 0, 1) gives a top-left pixel origin with y down.
        var ortho = Matrix4x4.CreateOrthographicOffCenter(0, 640, 480, 0, 0, 1);
        var tl = Ndc(new Vector3(0, 0, 0), ortho);
        var br = Ndc(new Vector3(640, 480, 0), ortho);
        c.True("2D projection", "ortho(0,W,H,0): pixel (0,0) -> ndc (-1,+1), (W,H) -> (+1,-1), z 0", tl.X == -1 && tl.Y == 1 && br.X == 1 && br.Y == -1 && tl.Z == 0, $"{tl} {br}");

        // Viewport: SDL_GPU maps ndc (-1,+1) to the top-left pixel corner; pixel = ((x+1)/2*W, (1-y)/2*H).
        var px = Scene.ToPixel(new Vector3(-1, 1, 0));
        c.True("viewport", "ndc (-1,+1) -> pixel (0,0)", px == Vector2.Zero, $"{px}");

        Console.WriteLine($"math: {c.Count - c.Failed}/{c.Count} passed");
        return c.Failed;
    }

    public static Vector3 Ndc(Vector3 point, in Matrix4x4 m)
    {
        var clip = Vector4.Transform(new Vector4(point, 1), m);
        return new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
    }
}
