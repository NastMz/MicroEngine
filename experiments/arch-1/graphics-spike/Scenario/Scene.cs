using System.Numerics;
using Arch1.Graphics.Batching;

namespace Arch1.Graphics.Scenario;

/// <summary>
/// The scenario scene, 160×120: a 2D layer drawn by the batcher (identical code for every device) and, on devices that
/// declare depth, a depth-tested 3D mesh drawn first into the same target. Expected values come from the written rules,
/// computed here on the CPU.
/// </summary>
public static class Scene
{
    public const int Width = 160, Height = 120;
    public static readonly ColorF Background = ColorF.FromBytes(32, 64, 96);
    private static readonly float[] Bg = [32 / 255f, 64 / 255f, 96 / 255f, 1];

    private static readonly Matrix4x4 View = Matrix4x4.CreateLookAt(new Vector3(0, 0, 6), Vector3.Zero, Vector3.UnitY);
    private static readonly Matrix4x4 Projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3, Width / (float)Height, 0.1f, 100);
    private static readonly (Vector3 Center, float Size, Vector4 Front, Vector4 Other)[] Cubes =
    [
        (new Vector3(0, -0.6f, 1), 1, new Vector4(1, 0, 0, 1), new Vector4(1, 0, 1, 1)),   // near: front face red, drawn first
        (new Vector3(1.2f, -0.4f, -2), 2, new Vector4(0, 1, 0, 1), new Vector4(0, 0, 1, 1)), // far: front face green
    ];

    // 2D elements (target pixels)
    private static readonly RectI Checker = new(8, 8, 8, 8);
    private static readonly RectI Half = new(24, 8, 16, 8);
    private static readonly RectI AddRed = new(48, 8, 12, 8), AddGreen = new(54, 8, 12, 8);
    private static readonly RectI MultiplyRect = new(48, 24, 12, 8);
    private static readonly RectI ScissorRect = new(130, 8, 20, 10);
    private static readonly (int X, int Y) RoundTrip = (8, 40);
    private static readonly RectI Gradient = new(8, 60, 32, 4);
    private static readonly Vector2 RotatedCenter = new(100.37f, 30.21f);
    private static readonly uint[] CheckerTexels = [0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0xFFFFFFFF];
    private static readonly uint[] Pattern = [0x11223344, 0xFF0000FF, 0x00FF00FF, 0x0000FFFF, 0xFFFF00FF, 0xFFFFFFFF];

    /// <summary>Where the near cube's front face projects (the 2D overlay sits on it).</summary>
    public static Vector2 NearFrontPixel(float dx = 0, float dy = 0) => Project(Cubes[0].Center + new Vector3(dx, dy, Cubes[0].Size / 2));

    private static RectI Overlay => new((int)NearFrontPixel().X - 8, (int)NearFrontPixel(0, 0.3f).Y - 3, 6, 6);
    private static RectI OverlayHalf => new((int)NearFrontPixel().X + 2, (int)NearFrontPixel(0, 0.3f).Y - 3, 6, 6);

    /// <summary>Draws the 3D mesh (depth tested, near cube first) into <paramref name="target"/>, which must have a depth texture.</summary>
    public static void Draw3D(IGraphicsDevice device, RenderTarget target)
    {
        using var vertex = device.CreateShader(ShaderFiles.Load("mesh.vert", device.Capabilities));
        using var fragment = device.CreateShader(ShaderFiles.Load("color.frag", device.Capabilities));
        using var pipeline = device.CreatePipeline(new PipelineDescription(vertex, fragment, MeshVertex.Layout, BlendState.Opaque,
            DepthState.LessWrite, new RasterState(CullMode.Back, FrontFace.CounterClockwise), target.ColorFormat, target.DepthFormat));
        var mesh = Cubes.SelectMany(c => Cube(c.Size, c.Front, c.Other)).ToArray();
        using var buffer = device.CreateBuffer(new BufferDescription(mesh.Length * 28, BufferUsage.Vertex, "cubes"));
        var list = device.BeginCommands();
        list.UpdateBuffer<MeshVertex>(buffer, 0, mesh);
        list.BeginPass(new PassDescription(new ColorAttachment(target, LoadOp.Clear, StoreOp.Store, Background), new DepthAttachment(target)));
        list.SetPipeline(pipeline);
        list.SetVertices(buffer);
        for (var i = 0; i < Cubes.Length; i++)
        {
            // System.Numerics row vectors: World * View * Projection, uploaded as-is (mul(M, v) in HLSL; P3 convention).
            list.SetUniforms(ShaderStage.Vertex, 0, Matrix4x4.CreateTranslation(Cubes[i].Center) * View * Projection);
            list.Draw(36, i * 36);
        }
        list.EndPass();
        device.Submit(list);
    }

    /// <summary>Draws the 2D layer with the batcher; <paramref name="clear"/> false composites it over what the target holds.</summary>
    public static void Draw2D(IGraphicsDevice device, Renderer2D batcher, RenderTarget target, bool clear)
    {
        var checker = device.CreateTexture(new TextureDescription(2, 2, Name: "checker"), Bytes(CheckerTexels));
        var white = device.CreateTexture(new TextureDescription(1, 1, Name: "white"), Bytes(0xFFFFFFFF));
        var halfBlue = device.CreateTexture(new TextureDescription(1, 1, Name: "half-blue"), Bytes(0x00008080)); // premultiplied blue at alpha 128
        var grey = device.CreateTexture(new TextureDescription(1, 1, Name: "grey"), Bytes(0x808080FF));
        var pair = device.CreateTexture(new TextureDescription(2, 1, Name: "pair"), Bytes(0x000000FF, 0xFFFFFFFF));
        var pattern = device.CreateTexture(new TextureDescription(3, 2, Name: "pattern"), Bytes(Pattern));
        var small = device.CreateRenderTarget(new RenderTargetDescription(8, 4, Name: "round-trip"));

        // Offscreen first: a pattern into a small target, read back later through the scene.
        batcher.Begin(new BatchSettings(small, Blend: BlendState.Opaque, Clear: ColorF.Transparent));
        batcher.Draw(pattern, new RectF(0, 0, 3, 2), Matrix3x2.Identity, ColorF.White);
        batcher.End();

        // Premultiplied alpha batch: checker, half-transparent quad, rotated quad, the round-trip target, the overlay.
        batcher.Begin(new BatchSettings(target, Clear: clear ? Background : null));
        batcher.Draw(checker, new RectF(0, 0, 2, 2), Matrix3x2.CreateScale(4) * Matrix3x2.CreateTranslation(Checker.X, Checker.Y), ColorF.White);
        batcher.Draw(halfBlue, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(Half.Width, Half.Height) * Matrix3x2.CreateTranslation(Half.X, Half.Y), ColorF.White);
        var rotated = Matrix3x2.CreateTranslation(-10, -6) * Matrix3x2.CreateRotation(MathF.PI / 6) * Matrix3x2.CreateTranslation(RotatedCenter);
        batcher.DrawGeometry(null, RotatedQuad(rotated), [0, 1, 2, 0, 2, 3]);
        batcher.Draw(small.Color!, new RectF(0, 0, 8, 4), Matrix3x2.CreateScale(2) * Matrix3x2.CreateTranslation(RoundTrip.X, RoundTrip.Y), ColorF.White);
        batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(Overlay.Width, Overlay.Height) * Matrix3x2.CreateTranslation(Overlay.X, Overlay.Y), new ColorF(1, 1, 0, 1));
        batcher.Draw(halfBlue, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(OverlayHalf.Width, OverlayHalf.Height) * Matrix3x2.CreateTranslation(OverlayHalf.X, OverlayHalf.Y), ColorF.White);
        batcher.End();

        batcher.Begin(new BatchSettings(target, Blend: BlendState.Additive));
        batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(AddRed.Width, AddRed.Height) * Matrix3x2.CreateTranslation(AddRed.X, AddRed.Y), new ColorF(1, 0, 0, 1));
        batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(AddGreen.Width, AddGreen.Height) * Matrix3x2.CreateTranslation(AddGreen.X, AddGreen.Y), new ColorF(0, 1, 0, 1));
        batcher.End();

        batcher.Begin(new BatchSettings(target, Blend: BlendState.Multiply));
        batcher.Draw(grey, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(MultiplyRect.Width, MultiplyRect.Height) * Matrix3x2.CreateTranslation(MultiplyRect.X, MultiplyRect.Y), ColorF.White);
        batcher.End();

        batcher.Begin(new BatchSettings(target, Blend: BlendState.Opaque, Sampler: SamplerDescription.LinearClamp));
        batcher.Draw(pair, new RectF(0, 0, 2, 1), Matrix3x2.CreateScale(16, 4) * Matrix3x2.CreateTranslation(Gradient.X, Gradient.Y), ColorF.White);
        batcher.End();

        batcher.Begin(new BatchSettings(target, Blend: BlendState.Opaque, Scissor: ScissorRect));
        batcher.Draw(white, new RectF(0, 0, 1, 1), Matrix3x2.CreateScale(Width, Height), ColorF.White);
        batcher.End();

        foreach (var r in new GraphicsResource[] { checker, white, halfBlue, grey, pair, pattern, small }) r.Dispose();
    }

    /// <summary>Property checks of the 2D layer; with <paramref name="with3D"/>, of the 3D mesh and the composite too.</summary>
    public static void Check(Checks c, Frame f, bool with3D)
    {
        // Placement (exact): the checker's texel blocks.
        var checkerOk = true;
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
                checkerOk &= f[Checker.X + x, Checker.Y + y] == CheckerTexels[(y / 4) * 2 + x / 4];
        c.True("position", "scaled checker texel blocks exact at (8,8)", checkerOk);
        c.True("position", "checker edges exact", f[Checker.X - 1, Checker.Y] == Pack(Background) && f[Checker.Right, Checker.Y] == Pack(Background));

        // Colour (±1/255, 8-bit quantization): premultiplied over, additive, multiply.
        c.Near("colour", "half-alpha blue over the background", [Bg[0] * (1 - 128 / 255f), Bg[1] * (1 - 128 / 255f), 128 / 255f + Bg[2] * (1 - 128 / 255f), 128 / 255f + (1 - 128 / 255f)], f, Half.X + 3, Half.Y + 3, 1, "8-bit quantization");
        c.Exact("colour", "additive red (saturates exactly)", 0xFF4060FF, f[AddRed.X + 1, AddRed.Y + 1]);
        c.Exact("colour", "additive red + green overlap (saturates exactly)", 0xFFFF60FF, f[AddGreen.X + 1, AddGreen.Y + 1]);
        c.Exact("colour", "additive green", 0x20FF60FF, f[AddGreen.Right - 1, AddGreen.Y + 1]);
        c.Near("colour", "multiply grey 128", [Bg[0] * 128 / 255f, Bg[1] * 128 / 255f, Bg[2] * 128 / 255f, 1], f, MultiplyRect.X + 2, MultiplyRect.Y + 2, 1, "8-bit quantization");

        // Coverage (exact): a 30° rotated quad covers exactly the pixels whose centres are inside it (no centre lies
        // within 1e-3 px of an edge, so the tie rule is not exercised).
        var rotated = Matrix3x2.CreateTranslation(-10, -6) * Matrix3x2.CreateRotation(MathF.PI / 6) * Matrix3x2.CreateTranslation(RotatedCenter);
        var corners = RotatedQuad(rotated).Select(v => v.Position).ToArray();
        int mismatched = 0, covered = 0, ambiguous = 0;
        for (var y = 0; y < Height; y++)
            for (var x = 75; x < 125; x++)
            {
                var (inside, margin) = Inside(corners, new Vector2(x + 0.5f, y + 0.5f));
                if (margin < 1e-3f) { ambiguous++; continue; }
                var white = f[x, y] == 0xFFFFFFFF;
                if (inside) covered++;
                if (inside != white) mismatched++;
            }
        c.True("coverage", $"rotated quad covers exactly the {covered} pixels whose centres are inside", mismatched == 0 && ambiguous == 0, $"{mismatched} mismatched, {ambiguous} ambiguous");

        // Render target round trip (exact): the small target, drawn at ×2.
        var roundTripOk = true;
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 8; x++)
            {
                var src = x < 3 && y < 2 ? Pattern[y * 3 + x] : 0u;
                var expected = (src & 0xFF) == 0xFF ? src : src == 0 ? Pack(Background) : 0; // translucent texel checked separately
                if (expected == 0) continue;
                for (var sy = 0; sy < 2; sy++)
                    for (var sx = 0; sx < 2; sx++)
                        roundTripOk &= f[RoundTrip.X + x * 2 + sx, RoundTrip.Y + y * 2 + sy] == expected;
            }
        c.True("state", "render target round trip exact (opaque texels and transparent area)", roundTripOk);
        var t = Pattern[0]; // 0x11223344: premultiplied, alpha 0x44 over the background
        var a = (t & 0xFF) / 255f;
        c.Near("colour", "translucent texel through the round trip", [(t >> 24) / 255f + Bg[0] * (1 - a), (t >> 16 & 0xFF) / 255f + Bg[1] * (1 - a), (t >> 8 & 0xFF) / 255f + Bg[2] * (1 - a), a + (1 - a)], f, RoundTrip.X, RoundTrip.Y, 1, "8-bit quantization");

        // Filtering: linear gradient clamped ends exact, monotonic ramp (kernel bounds ±2/255).
        var ramp = Enumerable.Range(0, 32).Select(x => (int)f.Channel(Gradient.X + x, Gradient.Y + 1, 0)).ToArray();
        c.True("filtering", "linear: clamped ends exact, monotonic ramp within the kernel", ramp.Take(8).All(v => v == 0) && ramp.Skip(24).All(v => v == 255)
            && ramp.Zip(ramp.Skip(1)).All(p => p.Second >= p.First)
            && Enumerable.Range(8, 16).All(x => Math.Abs(ramp[x] - ((x + 0.5f) / 16f - 0.5f) * 255f) <= 2), string.Join(",", ramp));

        // Scissor (exact).
        var scissorOk = true;
        for (var y = 0; y < 30; y++)
            for (var x = 120; x < Width; x++)
                scissorOk &= (f[x, y] == 0xFFFFFFFF) == (x >= ScissorRect.X && x < ScissorRect.Right && y >= ScissorRect.Y && y < ScissorRect.Bottom);
        c.True("position", "scissored fill exactly the scissor rectangle", scissorOk);

        c.Exact("colour", "opaque overlay (yellow)", 0xFFFF00FF, f[Overlay.X + 2, Overlay.Y + 2]);
        if (!with3D)
        {
            c.Exact("colour", "background corner", Pack(Background), f[Width - 1, Height - 1]);
            return;
        }

        // 3D (exact flat face colours at projected points) and the 2D-over-3D composite.
        var near = NearFrontPixel(0, -0.3f);
        c.Exact("depth", "near cube front face (drawn first, kept by the depth test)", 0xFF0000FF, f[(int)near.X, (int)near.Y]);
        var overlap = NearFrontPixel(0.4f, 0.1f);
        c.Exact("depth", "near cube in front of the far cube where both project", 0xFF0000FF, f[(int)overlap.X, (int)overlap.Y]);
        var far = Project(Cubes[1].Center + new Vector3(0.8f, 0.3f, 1));
        c.Exact("depth", "far cube front face", 0x00FF00FF, f[(int)far.X, (int)far.Y]);
        c.Near("colour", "half-alpha blue over the red face (2D composited over 3D)", [1 - 128 / 255f, 0, 128 / 255f, 1], f, OverlayHalf.X + 2, OverlayHalf.Y + 2, 1, "8-bit quantization");
        c.Exact("colour", "background corner", Pack(Background), f[Width - 1, Height - 1]);
        c.True("depth", "far cube is partly hidden: the far cube's front at the overlap pixel would be green", IsFarFrontAt(overlap), "CPU ray test");
    }

    private static bool IsFarFrontAt(Vector2 pixel)
    {
        // Unproject the pixel to a ray and intersect it with the far cube's front face plane (z = -1).
        Matrix4x4.Invert(View * Projection, out var inverse);
        var ndc = new Vector2(pixel.X / Width * 2 - 1, 1 - pixel.Y / Height * 2);
        var p0 = Vector4.Transform(new Vector4(ndc, 0, 1), inverse);
        var p1 = Vector4.Transform(new Vector4(ndc, 1, 1), inverse);
        var a = new Vector3(p0.X, p0.Y, p0.Z) / p0.W;
        var b = new Vector3(p1.X, p1.Y, p1.Z) / p1.W;
        var z = Cubes[1].Center.Z + Cubes[1].Size / 2;
        var hit = a + (b - a) * ((z - a.Z) / (b.Z - a.Z));
        var h = Cubes[1].Size / 2;
        return Math.Abs(hit.X - Cubes[1].Center.X) < h && Math.Abs(hit.Y - Cubes[1].Center.Y) < h;
    }

    private static Vector2 Project(Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1), View * Projection);
        return new Vector2((clip.X / clip.W + 1) / 2 * Width, (1 - clip.Y / clip.W) / 2 * Height);
    }

    private static Vertex2D[] RotatedQuad(Matrix3x2 m)
    {
        Vector2[] local = [new(0, 0), new(20, 0), new(20, 12), new(0, 12)];
        return [.. local.Select(p => new Vertex2D(Vector2.Transform(p, m), default, ColorF.White))];
    }

    private static (bool Inside, float Margin) Inside(Vector2[] poly, Vector2 p)
    {
        // Convex polygon: inside when on the same side of every edge; margin = distance to the nearest edge line.
        var margin = float.MaxValue;
        bool positive = false, negative = false;
        for (var i = 0; i < poly.Length; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % poly.Length];
            var edge = b - a;
            var cross = edge.X * (p.Y - a.Y) - edge.Y * (p.X - a.X);
            var along = Math.Clamp(Vector2.Dot(p - a, edge) / edge.LengthSquared(), 0, 1);
            margin = Math.Min(margin, Vector2.Distance(p, a + edge * along)); // distance to the edge segment
            if (cross > 0) positive = true; else negative = true;
        }
        return (!(positive && negative), margin);
    }

    private static IEnumerable<MeshVertex> Cube(float size, Vector4 front, Vector4 other)
    {
        var h = size / 2;
        // (normal, u, v) with u × v = normal, so (-u-v, +u-v, +u+v) is counter-clockwise seen from outside.
        (Vector3 N, Vector3 U, Vector3 V)[] faces =
        [
            (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY), (-Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY),
            (Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY), (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
            (Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ), (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
        ];
        foreach (var (n, u, v) in faces)
        {
            var color = n == Vector3.UnitZ ? front : other;
            Vector3 P(float su, float sv) => (n + u * su + v * sv) * h;
            foreach (var p in new[] { P(-1, -1), P(1, -1), P(1, 1), P(-1, -1), P(1, 1), P(-1, 1) }) yield return new MeshVertex(p, color);
        }
    }

    public static uint Pack(ColorF c) => (uint)(B(c.R) << 24 | B(c.G) << 16 | B(c.B) << 8 | B(c.A));

    private static int B(float v) => (int)Math.Clamp(MathF.Round(v * 255), 0, 255);

    private static byte[] Bytes(params uint[] pixels) => [.. pixels.SelectMany(p => new[] { (byte)(p >> 24), (byte)(p >> 16), (byte)(p >> 8), (byte)p })];
}
