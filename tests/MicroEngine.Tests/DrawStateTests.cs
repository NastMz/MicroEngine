using System.Numerics;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>
/// States, materials and render targets on the normal draw path: what they cost the batcher on a graphics device (the
/// CPU reference device here), what the headless renderer records for them, how a material checks its parameters, and
/// how the assets own shaders, materials and render targets. What they draw on every backend is in the renderer
/// conformance suite (tests/MicroEngine.Desktop.Tests/RendererConformance.cs).
/// </summary>
[TestClass]
public sealed class DrawStateTests
{
    private static readonly Color Bg = new(10, 20, 30);

    /// <summary>A compiled fragment shader with named parameters and no code a device would run: enough for a device without shaders.</summary>
    private static Dev.CompiledShader Fragment(bool named = true, Dev.ShaderStage stage = Dev.ShaderStage.Fragment) => new(stage,
        new Dev.ShaderResources(Samplers: 2, UniformBuffers: 1),
        new Dictionary<Dev.ShaderFormat, Dev.ShaderCode> { [Dev.ShaderFormat.SpirV] = new(new byte[] { 3, 2, 35, 7 }, "main") }, "look.frag.hlsl",
        named ? new Dev.ShaderReflection(
            [new Dev.ShaderUniformBlock("Look", 0, 32, [new Dev.ShaderUniform("Tint", 0, 16), new Dev.ShaderUniform("Time", 16, 4), new Dev.ShaderUniform("Step", 20, 8)])],
            [new Dev.ShaderTextureSlot("Scene", 0), new Dev.ShaderTextureSlot("Glow", 1)]) : null);

    // ---- The batcher ---------------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_frame_that_pushes_no_state_or_target_batches_as_before_and_a_state_that_changes_nothing_costs_no_draw_call()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 32, 16);
        var texture = canvas.Graphics.CreateTexture(new PixelImage(2, 2).Clear(Color.White));
        void Sprites(IRenderer r, int count, int y)
        {
            for (int i = 0; i < count; i++) r.Draw(texture, texture.Bounds, new Vector2(i * 3, y), Color.White);
        }

        var r = canvas.BeginFrame();
        Sprites(r, 8, 0);
        canvas.EndFrame();
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 8, DrawCalls: 1, Flushes: 1), canvas.Renderer.Statistics, "no state: one draw call, as before");

        r = canvas.BeginFrame();
        Sprites(r, 4, 0);
        r.PushState(DrawState.Alpha);            // the blending the frame already has
        Sprites(r, 4, 4);
        r.PopState();
        Sprites(r, 4, 8);
        canvas.EndFrame();
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 12, DrawCalls: 1, Flushes: 1), canvas.Renderer.Statistics, "a state equal to the current one joins the same draw call");

        r = canvas.BeginFrame();
        Sprites(r, 4, 0);
        r.PushState(DrawState.Additive);
        Sprites(r, 4, 4);
        r.PopState();
        Sprites(r, 4, 8);
        canvas.EndFrame();
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 12, DrawCalls: 3, Flushes: 1), canvas.Renderer.Statistics, "a different blend state splits the run, in one submission");
    }

    [TestMethod]
    public void A_pushed_target_is_part_of_the_frames_batch_and_its_draws_land_in_recording_order()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 16, 8);
        using var target = canvas.Graphics.CreateRenderTarget(4, 4);
        var r = canvas.BeginFrame();
        r.Clear(Bg);
        r.FillRect(new RectF(0, 0, 4, 4), new Color(255, 0, 0));       // canvas, before the target
        r.PushTarget(target);
        r.Clear(new Color(0, 255, 0));
        r.PopTarget();
        r.Draw(target.Texture, target.Bounds, new Vector2(2, 2), Color.White);   // over the red square
        canvas.EndFrame();
        var frame = canvas.Capture();
        Assert.AreEqual(new Color(255, 0, 0), frame[1, 1]);
        Assert.AreEqual(new Color(0, 255, 0), frame[2, 2], "the target's picture was drawn after the square it covers");
        Assert.AreEqual(new Color(0, 255, 0), frame[5, 5]);
        Assert.AreEqual(Bg, frame[6, 6]);
        // Three submissions: the canvas so far, the target, the canvas again.
        Assert.AreEqual(new Renderer2DStatistics(Sprites: 2, DrawCalls: 2, Flushes: 3), canvas.Renderer.Statistics);
        Assert.AreSame(canvas.Target, canvas.Renderer.Target, "after the frame the renderer is back on the canvas");
    }

    [TestMethod]
    public void A_frame_that_ends_with_a_target_or_a_state_still_pushed_says_so()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 8, 8);
        using var target = canvas.Graphics.CreateRenderTarget(4, 4);
        canvas.BeginFrame().PushTarget(target);
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(canvas.EndFrame).Message, "PushTarget");
        canvas.BeginFrame().PushState(DrawState.Additive);
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(canvas.EndFrame).Message, "PushState");
        // The next frame starts clean, on the canvas.
        var r = canvas.BeginFrame();
        Assert.AreEqual((8, 8), (r.Width, r.Height));
        r.Clear(Bg);
        canvas.EndFrame();
        Assert.AreEqual(Bg, canvas.Capture()[0, 0]);
    }

    [TestMethod]
    public void A_blend_state_the_device_does_not_have_is_refused_when_it_is_pushed()
    {
        var custom = new Dev.BlendState(Dev.BlendFactor.Zero, Dev.BlendFactor.SrcAlpha, Dev.BlendOp.Add, Dev.BlendFactor.Zero, Dev.BlendFactor.SrcAlpha, Dev.BlendOp.Add);
        using var device = new Dev.ReferenceGraphicsDevice();   // declares custom blending
        using var canvas = new DeviceCanvas(device, 8, 4);
        var r = canvas.BeginFrame();
        r.Clear(new Color(200, 100, 50));
        r.PushState(new DrawState(Blend: custom));
        r.FillRect(new RectF(0, 0, 4, 4), new Color(255, 255, 255, 128));   // keeps of the canvas what the source's alpha covers
        r.PopState();
        canvas.EndFrame();
        var kept = canvas.Capture()[1, 1];
        Assert.IsTrue(Math.Abs(kept.R - 100) <= 1 && Math.Abs(kept.G - 50) <= 1 && Math.Abs(kept.B - 25) <= 1, kept.ToString());

        // The headless rasterizer computes any blend state the same way.
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var headless = new HeadlessRenderer(8, 4, graphics);
        headless.Clear(new Color(200, 100, 50));
        headless.PushState(new DrawState(Blend: custom));
        headless.FillRect(new RectF(0, 0, 4, 4), new Color(255, 255, 255, 128));
        headless.PopState();
        var same = headless.Frame![1, 1];
        Assert.IsTrue(Math.Abs(same.R - 100) <= 1 && Math.Abs(same.G - 50) <= 1 && Math.Abs(same.B - 25) <= 1, same.ToString());
    }

    // ---- One texture type ----------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_device_texture_is_a_texture_of_the_drawing_API_once_wrapped_and_reports_the_memory_of_its_format()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 8, 8);
        // Premultiplied bytes, as the device holds them: an opaque green 2 x 2.
        var native = device.CreateTexture(new Dev.TextureDescription(2, 2), [0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255]);
        var texture = canvas.Graphics.Wrap(native, TextureFilter.Nearest);
        Assert.AreSame(native, texture.Native);
        Assert.IsFalse(texture.IsRenderTarget);
        Assert.AreEqual(16, texture.SizeInBytes);
        var r = canvas.BeginFrame();
        r.Clear(Bg);
        r.Draw(texture, texture.Bounds, new Vector2(1, 1), Color.White);
        r.Draw(new Sprite(texture, new RectF(0, 0, 1, 1), Vector2.Zero), new Vector2(5, 5));
        canvas.EndFrame();
        var frame = canvas.Capture();
        Assert.AreEqual(new Color(0, 255, 0), frame[2, 2]);
        Assert.AreEqual(new Color(0, 255, 0), frame[5, 5]);
        Assert.AreEqual(Bg, frame[6, 5]);
        texture.Dispose();
        Assert.IsFalse(native.IsDisposed, "a wrapped texture stays the caller's unless the wrapper was given it");
        var owned = canvas.Graphics.Wrap(native, ownsNative: true);
        owned.Dispose();
        Assert.IsTrue(native.IsDisposed);

        // A format that is not four bytes per pixel is sized as it is.
        if (device.Capabilities.Formats.TryGetValue(Dev.TextureFormat.R8Unorm, out var usage) && usage.HasFlag(Dev.TextureUsage.Sampled))
        {
            using var mask = canvas.Graphics.Wrap(device.CreateTexture(new Dev.TextureDescription(4, 4, Dev.TextureFormat.R8Unorm), new byte[16]), ownsNative: true);
            Assert.AreEqual(16, mask.SizeInBytes, "one byte per pixel");
            Assert.ThrowsExactly<InvalidOperationException>(() => mask.Update(0, 0, 1, 1, [Color.White]), "colours update 8-bit RGBA textures only");
        }

        using var other = new Dev.ReferenceGraphicsDevice();
        var foreign = other.CreateTexture(new Dev.TextureDescription(1, 1), [0, 0, 0, 0]);
        Assert.ThrowsExactly<ArgumentException>(() => canvas.Graphics.Wrap(foreign));
    }

    [TestMethod]
    public void A_device_render_target_wraps_into_a_target_of_the_drawing_API()
    {
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, 8, 8);
        var native = device.CreateRenderTarget(new Dev.RenderTargetDescription(4, 4));
        Assert.ThrowsExactly<ArgumentException>(() => canvas.Graphics.Wrap(native.Color!), "a target's picture is wrapped through its target");
        var target = canvas.Graphics.Wrap(native);
        Assert.IsTrue(target.Texture.IsRenderTarget);
        Assert.AreSame(native.Color, target.Texture.Native);
        Assert.AreEqual(64, target.SizeInBytes);
        var r = canvas.BeginFrame();
        r.Clear(Bg);
        r.PushTarget(target);
        r.Clear(new Color(0, 0, 255));
        r.PopTarget();
        r.Draw(target.Texture, target.Bounds, Vector2.Zero, Color.White);
        canvas.EndFrame();
        Assert.AreEqual(new Color(0, 0, 255), canvas.Capture()[3, 3]);
        target.Dispose();
        Assert.IsFalse(native.IsDisposed);
        native.Dispose();
        Assert.ThrowsExactly<ArgumentException>(() => new RenderTarget2D(canvas.Graphics.CreateTexture(new PixelImage(1, 1))), "an image's texture cannot be drawn into");
    }

    // ---- The headless renderer -----------------------------------------------------------------------------------------

    [TestMethod]
    public void The_headless_renderer_records_states_and_targets_and_counts_without_pixels()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var r = new HeadlessRenderer(16, 8, graphics);
        using var target = graphics.CreateRenderTarget(4, 4);
        using var material = graphics.CreateMaterial(Fragment(), new MaterialFallback(Tint: new Color(255, 0, 0)));
        Assert.IsFalse(material.IsSupported, "nothing runs shaders without a device");
        material.SetTexture("Glow", target.Texture);
        r.PushTarget(target);
        r.Clear(Color.White);
        r.PopTarget();
        r.PushState(new DrawState(Material: material).Filtered(TextureFilter.Linear));
        r.FillRect(new RectF(0, 0, 2, 2), Color.White);
        r.PopState();
        CollectionAssert.AreEqual(new[]
        {
            "target #1 4x4", "clear #FFFFFFFF", "target canvas",
            "state blend alpha sampler linear material fallback", "rect 0,0 2x2 #FF0000FF", "state blend alpha sampler texture material none",
        }, r.Commands.ToArray());
        Assert.AreEqual(new Color(255, 0, 0), r.Frame![1, 1], "the fallback's tint");
        r.PushTarget(target);
        Assert.AreEqual(1, r.TargetDepth);
        Assert.ThrowsExactly<InvalidOperationException>(r.Reset, "a frame that left a target pushed");
        Assert.AreEqual((16, 8, 0), (r.Width, r.Height, r.TargetDepth), "and the next frame is back on the canvas");

        // A counting renderer keeps sizes and balance without rasterizing anything.
        var counting = new HeadlessGraphicsDevice();
        var c = new HeadlessRenderer(16, 8);
        using var counted = counting.CreateRenderTarget(4, 2);
        c.PushTarget(counted);
        Assert.AreEqual((4, 2), (c.Width, c.Height));
        c.FillRect(new RectF(0, 0, 1, 1), Color.White);
        c.Draw(counting.CreateTexture(new PixelImage(1, 1)), new RectF(0, 0, 1, 1), Vector2.Zero, Color.White);
        Assert.ThrowsExactly<InvalidOperationException>(() => c.Draw(counted.Texture, counted.Bounds, Vector2.Zero, Color.White));
        c.PopTarget();
        Assert.AreEqual(2, c.DrawCalls);
        Assert.IsEmpty(c.Commands);
        Assert.AreEqual(2, counting.TexturesCreated);
    }

    [TestMethod]
    public void A_renderer_that_does_not_implement_states_or_targets_refuses_them()
    {
        IRenderer r = new Bare();
        Assert.ThrowsExactly<NotSupportedException>(() => r.PushState(DrawState.Additive));
        Assert.ThrowsExactly<NotSupportedException>(r.PopState);
        Assert.ThrowsExactly<NotSupportedException>(r.PopTarget);
    }

    /// <summary>A renderer written before states and targets existed: the members every renderer had.</summary>
    private sealed class Bare : IRenderer
    {
        public int Width => 1;
        public int Height => 1;
        public void Clear(Color color) { }
        public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default) { }
        public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint) { }
        public void FillRect(RectF rect, Color color) { }
        public void FillCircle(Vector2 center, float radius, Color color) { }
        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color) { }
        public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color) { }
        public void PushCamera(Camera2D camera) { }
        public void PopCamera() { }
        public void SetBlendMode(BlendMode mode) { }
        public void SetClip(RectF? screenRect) { }
    }

    // ---- Materials -----------------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_material_checks_names_and_sizes_and_says_what_the_shader_has()
    {
        using var material = Material2D.Create(null, Fragment());
        material.Set("Tint", new ColorF(1, 0, 0, 1));
        material.Set("Tint", new Vector4(1, 0, 0, 1));
        material.Set("Time", 2f);
        material.Set("Step", new Vector2(1, 0));
        var unknown = Assert.ThrowsExactly<ArgumentException>(() => material.Set("Speed", 1f));
        StringAssert.Contains(unknown.Message, "'look.frag.hlsl'");
        StringAssert.Contains(unknown.Message, "Tint, Time, Step, Scene, Glow", "the message lists what the shader has");
        var size = Assert.ThrowsExactly<ArgumentException>(() => material.Set("Step", 1f));
        StringAssert.Contains(size.Message, "'Step' takes 8 bytes");
        StringAssert.Contains(size.Message, "Single given is 4 bytes");
        StringAssert.Contains(Assert.ThrowsExactly<ArgumentException>(() => material.Set("Glow", 1f)).Message, "SetTexture");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => material.SetUniforms(new Vector4(1, 2, 3, 4)), "a whole block of another size than the shader's");
        material.SetUniforms(new Matrix4x4().M11 == 0 ? stackalloc byte[32] : default);

        var graphics = new HeadlessGraphicsDevice();
        var texture = graphics.CreateTexture(new PixelImage(1, 1));
        StringAssert.Contains(Assert.ThrowsExactly<ArgumentException>(() => material.SetTexture("Time", texture)).Message, "not a texture");
        StringAssert.Contains(Assert.ThrowsExactly<ArgumentException>(() => material.SetTexture("Scene", texture)).Message, "each draw");
        var r = new HeadlessRenderer(4, 4);
        r.PushState(new DrawState(Material: material));
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => r.FillRect(new RectF(0, 0, 1, 1), Color.White)).Message, "'Glow'");
        material.SetTexture("Glow", texture);
        r.FillRect(new RectF(0, 0, 1, 1), Color.White);
        r.PopState();
        texture.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => material.SetTexture("Glow", texture));
    }

    [TestMethod]
    public void A_shader_file_without_names_still_makes_a_material_whose_data_is_set_by_slot()
    {
        using var material = Material2D.Create(null, Fragment(named: false));
        Assert.IsNull(material.Reflection);
        var e = Assert.ThrowsExactly<InvalidOperationException>(() => material.Set("Time", 1f));
        StringAssert.Contains(e.Message, "microengine shaders");
        Assert.ThrowsExactly<InvalidOperationException>(() => material.SetTexture("Glow", new HeadlessGraphicsDevice().CreateTexture(new PixelImage(1, 1))));
        material.SetUniforms(new Vector4(1, 2, 3, 4));
        Assert.ThrowsExactly<ArgumentException>(() => Material2D.Create(null, Fragment(stage: Dev.ShaderStage.Vertex)), "a material is made from a fragment shader");
    }

    [TestMethod]
    public void The_engine_ships_the_vertex_stage_of_every_2D_material()
    {
        var vertex = Material2D.SpriteVertexShader;
        Assert.AreSame(vertex, Material2D.SpriteVertexShader);
        Assert.AreEqual(Dev.ShaderStage.Vertex, vertex.Stage);
        Assert.AreEqual(Dev.ShaderFormat.Dxil | Dev.ShaderFormat.SpirV | Dev.ShaderFormat.Msl, vertex.Formats);
        Assert.AreEqual(new Dev.ShaderResources(UniformBuffers: 1), vertex.Resources, "one uniform slot: the pixel-to-clip matrix");
        Assert.AreEqual(64, vertex.Reflection!.Blocks[0].Size);
    }

    // ---- Assets --------------------------------------------------------------------------------------------------------

    private static (AssetCache Assets, HeadlessGraphicsDevice Device, DirectoryInfo Folder) Content()
    {
        var folder = Directory.CreateTempSubdirectory("microengine-render-assets-");
        Directory.CreateDirectory(Path.Combine(folder.FullName, "shaders"));
        File.WriteAllBytes(Path.Combine(folder.FullName, "shaders", "look.frag.shader"), Fragment().ToBytes());
        File.WriteAllBytes(Path.Combine(folder.FullName, "shaders", "sprite.vert.shader"), Material2D.SpriteVertexShader.ToBytes());
        File.WriteAllText(Path.Combine(folder.FullName, "shaders", "broken.frag.shader"), "{}");
        var device = new HeadlessGraphicsDevice();
        return (new AssetCache(device, folder.FullName), device, folder);
    }

    [TestMethod]
    public void Materials_and_shaders_are_assets_loaded_once_and_released_with_their_scope()
    {
        var (assets, _, folder) = Content();
        try
        {
            var scope = assets.CreateScope("level");
            var material = scope.Material("shaders/look.frag.shader", new MaterialFallback(Tint: new Color(0, 0, 255)));
            Assert.AreSame(material, scope.Material("shaders/look.frag.shader", new MaterialFallback(Tint: new Color(0, 0, 255))), "the same request gives the same material");
            Assert.AreNotSame(material, scope.Material("shaders/look.frag.shader"), "another fallback is another material");
            Assert.AreEqual(new Color(0, 0, 255), material.Fallback.Tint);
            material.Set("Time", 1f);
            var shader = scope.Shader("shaders/look.frag.shader");
            Assert.AreSame(shader, assets.Shader("shaders/look.frag.shader"));
            Assert.AreEqual("Look", shader.Reflection!.Blocks[0].Name);
            CollectionAssert.AreEquivalent(new[] { "material", "material", "shader" }, scope.Loaded.Select(a => a.Kind).ToArray());
            var handle = scope.Scope.Load<Material2D>("material", "shaders/look.frag.shader");
            Assert.AreNotSame(material, scope.Scope.Get(handle), "the typed handle of the plain material");
            scope.Dispose();
            Assert.IsTrue(material.IsDisposed, "released with the scope that held it");
            Assert.AreEqual(1, assets.Count, "the root still holds the shader it asked for");

            StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => assets.Material("shaders/sprite.vert.shader")).Message, "fragment shader");
            StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => assets.Material("shaders/broken.frag.shader")).Message, "compiled shader");
            StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => assets.Shader("shaders/missing.frag.shader")).Message, "not found");
        }
        finally
        {
            assets.Dispose();
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void A_render_target_is_an_asset_a_scope_owns_by_name()
    {
        var device = new HeadlessGraphicsDevice();
        using var assets = new AssetCache(device);
        var scope = assets.CreateScope("scene");
        var target = scope.RenderTarget("water/above", 320, 180);
        Assert.AreSame(target, scope.RenderTarget("water/above", 320, 180), "the same name and size give the same target");
        Assert.AreNotSame(target, scope.RenderTarget("water/above", 160, 90), "another size is another target");
        Assert.AreEqual((320, 180), (target.Width, target.Height));
        Assert.AreEqual(2, device.LiveTextures);
        Assert.AreEqual(320L * 180 * 4 + 160 * 90 * 4, scope.Statistics.TextureBytes, "render targets count as texture memory");
        Assert.IsTrue(scope.Loaded.All(a => a.Kind == "render target" && a.Path == "water/above"));
        var handle = scope.Scope.Load<RenderTarget2D>("render target", "ui/minimap", new RenderTargetSize(64, 64));
        Assert.AreEqual(64, scope.Scope.Get(handle).Width, "the typed handle path makes one too");
        StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => scope.RenderTarget("bad", 0, 4)).Message, "positive");
        StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => scope.Scope.Load<RenderTarget2D>("render target", "nameless")).Message, "Assets.RenderTarget");
        scope.Dispose();
        Assert.IsTrue(target.IsDisposed);
        Assert.IsTrue(target.Texture.IsDisposed);
        Assert.AreEqual(0, device.LiveTextures, "released with the scope");
        Assert.AreEqual(0, assets.Count);
    }
}
