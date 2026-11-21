using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Tests;

/// <summary>
/// Post-processing chains (<see cref="PostProcess"/>) where no shader runs: on the headless rasterizer and on a
/// graphics device of tier 0 (the CPU reference device). What each ready effect draws there, what a chain owns and
/// releases, and how a scene's and a game's chains take part in a frame. What the effects draw where shaders run is in
/// tests/MicroEngine.Desktop.Tests/PostProcessDeviceTests.cs.
/// </summary>
[TestClass]
public sealed class PostProcessTests
{
    private static readonly Color Bg = new(40, 80, 120), White = Color.White, Red = new(255, 0, 0);

    /// <summary>
    /// A 32 × 16 frame drawn on the headless rasterizer or on the reference device. A chain used in it is left to the
    /// next frame or to its device: like any texture, what a chain made is released between frames, never in one.
    /// </summary>
    private static PixelImage Frame(string backend, Action<IRenderer, IGraphicsDevice> draw, int width = 32, int height = 16)
    {
        if (backend == "headless")
        {
            var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
            var r = new HeadlessRenderer(width, height, graphics);
            r.Clear(Bg);
            draw(r, graphics);
            return r.Frame!;
        }
        using var device = new Dev.ReferenceGraphicsDevice();
        using var canvas = new DeviceCanvas(device, width, height);
        var renderer = canvas.BeginFrame();
        renderer.Clear(Bg);
        draw(renderer, canvas.Graphics);
        canvas.EndFrame();
        return canvas.Capture();
    }

    private static void Near(Color expected, Color actual, string what, int tolerance = 2) =>
        Assert.IsTrue(Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance,
            $"{what}: expected about {expected}, got {actual}");

    private static int Brightness(Color c) => c.R + c.G + c.B;

    // ---- The chain -----------------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_chain_without_an_enabled_effect_does_nothing_and_makes_nothing()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var r = new HeadlessRenderer(8, 8, graphics);
        var chain = new PostProcess();
        Assert.IsFalse(chain.IsActive);
        Assert.IsFalse(chain.Begin(r, graphics), "nothing to run: no picture is started");
        r.FillRect(new RectF(0, 0, 2, 2), Red);
        chain.End(r);
        Assert.AreEqual(Red, r.Frame![0, 0], "the draw went straight to the canvas");
        Assert.AreEqual(0, graphics.TexturesCreated, "no target was made");

        chain.Add(new Vignette { Enabled = false });
        Assert.IsFalse(chain.IsActive, "a disabled effect does not count");
        chain.Get<Vignette>()!.Enabled = true;
        chain.Enabled = false;
        Assert.IsFalse(chain.IsActive, "nor does any effect of a disabled chain");
        Assert.IsFalse(chain.Begin(r, graphics));
        Assert.AreEqual(0, graphics.TexturesCreated);
        Assert.AreEqual(0, r.TargetDepth);
    }

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void What_is_drawn_between_begin_and_end_goes_through_the_effects_and_keeps_its_transparency(string backend)
    {
        var chain = new PostProcess().Add(new ColorGrade { Tint = new Color(255, 0, 0) });
        var frame = Frame(backend, (r, graphics) =>
        {
            Assert.IsTrue(chain.Begin(r, graphics));
            Assert.AreEqual((32, 16), (r.Width, r.Height), "the picture has the renderer's size");
            r.FillRect(new RectF(0, 0, 8, 8), White);          // the picture: a white square on nothing
            chain.End(r);
        });
        Assert.AreEqual(Red, frame[2, 2], "the square went through the tint");
        Assert.AreEqual(Bg, frame[20, 10], "where the picture is transparent, what was under it shows, untinted");
        chain.Dispose();
    }

    [TestMethod]
    public void Effects_run_in_the_chains_order_and_a_disabled_one_is_skipped()
    {
        // Black on every other row, then a blur, or the other way round: the dark rows are black only when they come last.
        var half = new ColorGrade { Brightness = 0.5f };
        var lines = new Scanlines { Strength = 1 };
        var blur = new Blur { Radius = 2 };
        PixelImage Picture(PostProcess chain) => Frame("headless", (r, graphics) =>
        {
            chain.Begin(r, graphics, White);
            chain.End(r, opaque: true);
        });
        Assert.AreEqual(Color.Black, Picture(new PostProcess().Add(blur).Add(lines))[5, 5], "blurred, then lined: the dark rows are black");
        var mixed = Picture(new PostProcess().Add(lines).Add(blur))[5, 5];
        Assert.IsTrue(mixed.R is > 60 and < 200, $"lined, then blurred: the rows are mixed into grey ({mixed})");
        blur.Enabled = false;
        Assert.AreEqual(Color.Black, Picture(new PostProcess().Add(lines).Add(blur))[5, 5], "the disabled effect is skipped");

        var chain = new PostProcess().Add(half);
        Assert.ThrowsExactly<ArgumentException>(() => chain.Add(half), "an effect runs once per chain");
        Assert.AreSame(half, chain.Get<ColorGrade>());
        Assert.IsNull(chain.Get<Bloom>());
        Assert.HasCount(1, chain.Effects);
    }

    [TestMethod]
    public void A_chain_owns_what_its_effects_need_and_releases_it_and_can_be_used_again()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var r = new HeadlessRenderer(16, 8, graphics);
        var vignette = new Vignette();
        var chain = new PostProcess().Add(new Blur { Radius = 4 }).Add(vignette);
        void Run()
        {
            chain.Begin(r, graphics, Bg);
            chain.End(r, opaque: true);
        }
        Run();
        int alive = graphics.LiveTextures;
        Assert.IsTrue(alive >= 4, $"the picture, the blur's targets and the vignette's texture: {alive}");
        Assert.IsTrue(chain.SizeInBytes > 16 * 8 * 4, "their memory is counted");
        int created = graphics.TexturesCreated;
        Run();
        Assert.AreEqual(created, graphics.TexturesCreated, "a second frame of the same size makes nothing new");

        Assert.IsTrue(chain.Remove(vignette));
        Assert.AreEqual(alive - 1, graphics.LiveTextures, "removing an effect releases what was made for it");
        Assert.IsFalse(chain.Remove(vignette));

        chain.Dispose();
        Assert.AreEqual(0, graphics.LiveTextures, "disposing the chain releases everything");
        chain.Dispose();
        Run();
        Assert.IsTrue(graphics.LiveTextures > 0, "a released chain makes what it needs again");
        chain.Clear();
        Assert.AreEqual(0, graphics.LiveTextures);
        Assert.HasCount(0, chain.Effects);
    }

    [TestMethod]
    public void The_picture_follows_the_size_it_is_drawn_at()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var chain = new PostProcess().Add(new Scanlines { Strength = 1 });
        foreach (var (width, height) in new[] { (8, 8), (16, 4) })
        {
            var r = new HeadlessRenderer(width, height, graphics);
            chain.Begin(r, graphics, White);
            Assert.AreEqual((width, height), (r.Width, r.Height));
            chain.End(r, opaque: true);
            Assert.AreEqual(White, r.Frame![width - 1, 0], "the first row of the picture, at its right edge");
            Assert.AreEqual(Color.Black, r.Frame![width - 1, 1]);
        }
        chain.Dispose();
    }

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void Apply_runs_the_chain_on_a_picture_that_already_exists_without_changing_it(string backend)
    {
        var chain = new PostProcess().Add(new ColorGrade { Tint = new Color(0, 255, 0) });
        var frame = Frame(backend, (r, graphics) =>
        {
            var picture = graphics.CreateRenderTarget(8, 8, TextureFilter.Nearest);   // released with its device, after the frame
            r.PushTarget(picture);
            r.Clear(White);
            r.PopTarget();
            chain.Apply(r, graphics, picture.Texture, new RectF(0, 0, 8, 8), opaque: true);
            r.Draw(picture.Texture, picture.Bounds, new Vector2(16, 0), White);     // the picture itself, afterwards
            chain.Enabled = false;
            chain.Apply(r, graphics, picture.Texture, new RectF(8, 8, 8, 8));        // an inactive chain draws it as it is
            chain.Enabled = true;
        });
        Assert.AreEqual(new Color(0, 255, 0), frame[4, 4], "the result, where it was asked for");
        Assert.AreEqual(White, frame[20, 4], "the picture given was not changed");
        Assert.AreEqual(White, frame[12, 12]);
        Assert.AreEqual(Bg, frame[28, 12]);
    }

    [TestMethod]
    public void The_result_covers_the_whole_target_whatever_camera_is_pushed()
    {
        var chain = new PostProcess().Add(new ColorGrade { Tint = Red });
        var camera = new Camera2D(32, 16) { Position = new Vector2(100, 50) };
        var frame = Frame("headless", (r, graphics) =>
        {
            r.PushCamera(camera);
            chain.Begin(r, graphics, White);
            chain.End(r, opaque: true);
            r.PopCamera();
        });
        Assert.AreEqual(Red, frame[0, 0]);
        Assert.AreEqual(Red, frame[31, 15]);
    }

    // ---- The ready effects, where no shader runs -----------------------------------------------------------------------

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void A_vignette_darkens_towards_the_corners_and_leaves_the_centre(string backend)
    {
        var vignette = new Vignette { Strength = 1f, Radius = 0.3f, Softness = 0.5f };
        var chain = new PostProcess().Add(vignette);
        PixelImage Picture() => Frame(backend, (r, graphics) =>
        {
            Assert.AreEqual(PostEffectSupport.Full, vignette.SupportOn(graphics), "made of a texture and blending: the same everywhere");
            chain.Begin(r, graphics, White);
            chain.End(r, opaque: true);
        }, 64, 64);
        var frame = Picture();
        Assert.AreEqual(White, frame[32, 32], "the centre is untouched");
        Assert.IsTrue(Brightness(frame[0, 0]) < 40, $"the corner is nearly black: {frame[0, 0]}");
        Assert.IsTrue(Brightness(frame[32, 6]) < Brightness(frame[32, 20]), "darker the farther from the centre");
        Assert.AreEqual(frame[3, 60], frame[60, 3], "the same in every corner");

        vignette.Strength = 0.5f;
        vignette.Color = Red;
        var tinted = Picture()[0, 0];
        Assert.IsTrue(tinted.R > 240 && Math.Abs(tinted.G - 128) < 12 && Math.Abs(tinted.B - 128) < 12, $"white half way to white times the vignette's colour: {tinted}");
        vignette.Strength = 0;
        Assert.AreEqual(White, Picture()[0, 0], "no strength, no change");
    }

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void Scanlines_darken_every_other_row(string backend)
    {
        var lines = new Scanlines { Strength = 0.5f };
        var chain = new PostProcess().Add(lines);
        PixelImage Picture() => Frame(backend, (r, graphics) =>
        {
            Assert.AreEqual(PostEffectSupport.Full, lines.SupportOn(graphics));
            chain.Begin(r, graphics, White);
            chain.End(r, opaque: true);
        });
        var frame = Picture();
        for (int y = 0; y < 16; y += 2)
        {
            Assert.AreEqual(White, frame[5, y], $"row {y} is as it was");
            Near(new Color(128, 128, 128), frame[5, y + 1], $"row {y + 1} is half as bright");
        }
        lines.Lines = 4;                      // four lines in sixteen rows: two bright rows, two dark rows
        frame = Picture();
        Assert.AreEqual(White, frame[5, 1]);
        Near(new Color(128, 128, 128), frame[5, 2], "the dark half of the first line");
        Near(new Color(128, 128, 128), frame[5, 3], "the dark half of the first line");
        Assert.AreEqual(White, frame[5, 4]);
    }

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void Without_shaders_colour_grading_applies_its_tint_and_brightness_and_says_what_it_leaves_out(string backend)
    {
        var grade = new ColorGrade();
        var chain = new PostProcess().Add(grade);
        Color Graded(Color colour) => Frame(backend, (r, graphics) =>
        {
            chain.Begin(r, graphics, colour);
            chain.End(r, opaque: true);
        })[3, 3];
        var graphics = new HeadlessGraphicsDevice();
        Assert.IsFalse(graphics.SupportsShaders);

        grade.Tint = new Color(255, 128, 0);
        Assert.AreEqual(PostEffectSupport.Full, grade.SupportOn(graphics), "a tint needs no shader");
        Near(new Color(200, 100, 0), Graded(new Color(200, 200, 200)), "the tint multiplies");
        grade.Tint = White;
        grade.Brightness = 0.5f;
        Near(new Color(100, 50, 25), Graded(new Color(200, 100, 50)), "half as bright");
        grade.Brightness = 1.5f;
        Assert.AreEqual(PostEffectSupport.Full, grade.SupportOn(graphics));
        Near(new Color(150, 75, 255), Graded(new Color(100, 50, 200)), "half as bright again, clamped at white");
        grade.Brightness = 3f;
        Near(new Color(120, 60, 30), Graded(new Color(40, 20, 10)), "three times as bright");

        grade.Brightness = 1f;
        grade.Saturation = 0f;
        Assert.AreEqual(PostEffectSupport.Reduced, grade.SupportOn(graphics), "saturation needs the shader");
        Assert.AreEqual(new Color(200, 100, 50), Graded(new Color(200, 100, 50)), "left out where no shader runs: the colours stay");
        grade.Saturation = 1f;
        grade.Contrast = 2f;
        Assert.AreEqual(PostEffectSupport.Reduced, grade.SupportOn(graphics), "so does contrast");
    }

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void Without_shaders_a_blur_spreads_the_picture_by_halving_it(string backend)
    {
        var blur = new Blur { Radius = 4 };
        var chain = new PostProcess().Add(blur);
        PixelImage Picture() => Frame(backend, (r, graphics) =>
        {
            Assert.AreEqual(PostEffectSupport.Reduced, blur.SupportOn(graphics));
            chain.Begin(r, graphics, Color.Black);
            r.FillRect(new RectF(12, 4, 8, 8), White);          // a white square in the middle of black
            chain.End(r, opaque: true);
        });
        var frame = Picture();
        Assert.IsTrue(Brightness(frame[16, 8]) > 300, $"the middle of the square stays bright: {frame[16, 8]}");
        Assert.IsTrue(Brightness(frame[10, 8]) is > 0 and < 600, $"its light reaches outside its edge: {frame[10, 8]}");
        Assert.IsTrue(Brightness(frame[13, 8]) < 765, $"and its edge is no longer white: {frame[13, 8]}");
        Assert.AreEqual(Color.Black, frame[0, 0], "far from it nothing changes");
        Assert.AreEqual(frame[10, 8].R, frame[10, 8].G, "a grey blur of a grey picture");

        blur.Radius = 0;
        frame = Picture();
        Assert.AreEqual(White, frame[12, 8], "no radius: the picture as it is");
        Assert.AreEqual(Color.Black, frame[11, 8]);
    }

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void Without_shaders_a_bloom_adds_the_blurred_bright_parts_and_hardly_touches_the_dark_ones(string backend)
    {
        var bloom = new Bloom { Threshold = 0.6f, Strength = 1f, Radius = 4 };
        var chain = new PostProcess().Add(bloom);
        var grey = new Color(60, 60, 60);
        PixelImage Picture() => Frame(backend, (r, graphics) =>
        {
            Assert.AreEqual(PostEffectSupport.Reduced, bloom.SupportOn(graphics));
            chain.Begin(r, graphics, Color.Black);
            r.FillRect(new RectF(0, 0, 8, 16), grey);            // a dim area on the left
            r.FillRect(new RectF(20, 6, 4, 4), White);           // a light on the right
            chain.End(r, opaque: true);
        });
        var frame = Picture();
        Assert.AreEqual(White, frame[21, 7], "the light itself stays white");
        Assert.IsTrue(Brightness(frame[18, 7]) > 30, $"a glow around the light: {frame[18, 7]}");
        Assert.IsTrue(Brightness(frame[2, 8]) - Brightness(grey) < 12, $"the dim area gains almost nothing: {frame[2, 8]}");
        Assert.IsTrue(Brightness(frame[18, 7]) > (Brightness(frame[2, 8]) - Brightness(grey)) * 3, "far less than the light's surroundings");

        bloom.Strength = 0;
        Assert.AreEqual(Color.Black, Picture()[18, 7], "no strength, no glow");
    }

    [TestMethod]
    public void Without_shaders_a_crt_draws_only_its_lines_and_a_material_pass_is_left_out()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var crt = new Crt { Curvature = 0.2f, Scanlines = 0.5f, Mask = 0.5f };
        Assert.AreEqual(PostEffectSupport.Reduced, crt.SupportOn(graphics), "the lines, without the curve and the stripes");
        var r = new HeadlessRenderer(16, 8, graphics);
        var chain = new PostProcess().Add(crt);
        chain.Begin(r, graphics, White);
        chain.End(r, opaque: true);
        Assert.AreEqual(White, r.Frame![0, 0], "flat: the corner is not curved away");
        Near(new Color(128, 128, 128), r.Frame![0, 1], "the dark row of a line");
        Assert.AreEqual(r.Frame![3, 1], r.Frame![4, 1], "no stripes");

        crt.Scanlines = 0;
        Assert.AreEqual(PostEffectSupport.None, crt.SupportOn(graphics), "nothing is left of it without lines");
        chain.Dispose();

        using var material = graphics.CreateMaterial(PostShaders.Get(PostShaders.Grade), MaterialFallback.Hide);
        var pass = new ShaderEffect(material) { Prepare = (_, _) => Assert.Fail("an effect that is left out is not prepared") };
        Assert.AreEqual(PostEffectSupport.None, pass.SupportOn(graphics));
        var only = new PostProcess().Add(pass);
        Assert.IsTrue(only.IsActive);
        var support = only.SupportOn(graphics);
        Assert.AreEqual((pass, PostEffectSupport.None), (support[0].Effect, support[0].Support));
        r.Reset();
        only.Begin(r, graphics, Red);
        only.End(r, opaque: true);
        Assert.AreEqual(Red, r.Frame![5, 5], "the picture passes through unchanged, whatever the material's fallback");
        only.Dispose();
    }

    [TestMethod]
    public void The_built_in_shaders_declare_the_parameters_the_effects_set()
    {
        (string Shader, string[] Names)[] expected =
        [
            (PostShaders.Blur, ["Step", "Threshold"]),
            (PostShaders.Grade, ["Tint", "Brightness", "Contrast", "Saturation"]),
            (PostShaders.Crt, ["Curvature", "Scanlines", "Lines", "Mask", "Columns"]),
        ];
        foreach (var (name, parameters) in expected)
        {
            var shader = PostShaders.Get(name);
            Assert.AreEqual(Dev.ShaderStage.Fragment, shader.Stage, name);
            var declared = shader.Reflection!.Blocks.SelectMany(block => block.Uniforms).Select(uniform => uniform.Name).ToArray();
            CollectionAssert.AreEquivalent(parameters, declared, name);
            Assert.AreEqual(1, shader.Resources.Samplers, $"{name} samples the picture only");
            foreach (var format in new[] { Dev.ShaderFormat.Dxil, Dev.ShaderFormat.SpirV, Dev.ShaderFormat.Msl })
                Assert.IsTrue(shader.TryGetCode(format, out _), $"{name} is compiled to {format}");
        }
    }

    // ---- An effect of the game's own -----------------------------------------------------------------------------------

    /// <summary>Mirrors the picture's left half onto its right half, with a scratch target and a drawing over the picture.</summary>
    private sealed class Mirror : PostEffect
    {
        public bool Unbalanced;
        public override PostEffectSupport SupportOn(IGraphicsDevice graphics) => PostEffectSupport.Full;
        protected internal override void Apply(PostPass pass)   // a game writes "protected override"; this assembly sees the engine's internals
        {
            var half = pass.Scratch("half", pass.Width / 2, pass.Height, TextureFilter.Nearest);
            pass.Renderer.PushTarget(half);
            pass.Renderer.Draw(pass.Source, new RectF(0, 0, pass.Width / 2, pass.Height), Vector2.Zero, Color.White);
            pass.Renderer.PopTarget();
            var r = pass.BeginOver();
            r.PushState(DrawState.Opaque);
            r.Draw(half.Texture, half.Bounds, new Vector2(pass.Width / 2, 0), Color.White, new DrawOptions(FlipX: true));
            r.PopState();
            if (!Unbalanced) pass.EndOver();
        }
    }

    [TestMethod]
    [DataRow("headless")]
    [DataRow("reference")]
    public void A_game_writes_an_effect_of_its_own_with_the_pass(string backend)
    {
        var chain = new PostProcess().Add(new Mirror());
        var frame = Frame(backend, (r, graphics) =>
        {
            chain.Begin(r, graphics, Color.Black);
            r.FillRect(new RectF(2, 2, 3, 3), Red);
            chain.End(r, opaque: true);
        });
        Assert.AreEqual(Red, frame[3, 3]);
        Assert.AreEqual(Red, frame[32 - 1 - 3, 3], "mirrored onto the right half");
        Assert.AreEqual(Color.Black, frame[20, 3]);
    }

    [TestMethod]
    public void An_effect_that_leaves_the_picture_open_is_refused()
    {
        var graphics = new HeadlessGraphicsDevice { KeepsPixels = true };
        var r = new HeadlessRenderer(8, 8, graphics);
        var chain = new PostProcess().Add(new Mirror { Unbalanced = true });
        chain.Begin(r, graphics);
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => chain.End(r));
        StringAssert.Contains(error.Message, "Mirror");
    }

    // ---- In a game -----------------------------------------------------------------------------------------------------

    private sealed class Painted(Color colour, RectF area, bool overlay = false) : Scene
    {
        public override bool IsOverlay => overlay;
        public override void Update(float dt) { }
        public override void Draw(IRenderer r) => r.FillRect(area, colour);
        public override void DrawUi(IRenderer ui) => ui.FillRect(new RectF(0, 0, 2, 2), White);
    }

    private sealed class PostGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("post", 32, 16) { Background = Bg };
        protected override void Load() => Scenes.Push(first);
    }

    private static PixelImage Render(Game game, HeadlessPlatform platform)
    {
        game.Render(platform.BeginFrame());
        platform.EndFrame();
        return ((IViewCapture)platform).CaptureWorld();
    }

    [TestMethod]
    public void A_scenes_chain_processes_that_scenes_picture_alone_and_is_released_when_the_scene_leaves()
    {
        var under = new Painted(new Color(0, 0, 255), new RectF(0, 0, 32, 16));
        var game = new PostGame(under);
        var platform = new HeadlessPlatform(32, 16) { Rasterize = true };
        game.InitializeForTest(platform);
        var graphics = (HeadlessGraphicsDevice)platform.Graphics;
        var over = new Painted(White, new RectF(8, 4, 8, 8), overlay: true);
        game.Scenes.Push(over);
        game.StepForTest();
        int before = graphics.LiveTextures;
        Assert.AreEqual(White, Render(game, platform)[10, 6]);
        Assert.AreEqual(before, graphics.LiveTextures, "a scene that never read its chain made nothing");

        over.PostProcess.Add(new ColorGrade { Tint = Red });
        var frame = Render(game, platform);
        Assert.AreEqual(Red, frame[10, 6], "the scene's own picture went through its chain");
        Assert.AreEqual(new Color(0, 0, 255), frame[2, 2], "the scene under it shows around it, untouched");
        Assert.IsTrue(graphics.LiveTextures > before, "the chain made its picture");

        game.Scenes.Pop();
        game.StepForTest();
        Assert.AreEqual(before, graphics.LiveTextures, "leaving the stack releases the chain's targets");
        Assert.HasCount(1, over.PostProcess.Effects, "the effects stay with the scene");
        Assert.AreEqual(new Color(0, 0, 255), Render(game, platform)[10, 6]);
    }

    [TestMethod]
    public void A_scenes_chain_draws_under_the_scenes_camera()
    {
        var scene = new Painted(White, new RectF(100, 50, 4, 4)) { WorldCamera = new Camera2D(32, 16) { Position = new Vector2(98, 48) } };
        var game = new PostGame(scene);
        var platform = new HeadlessPlatform(32, 16) { Rasterize = true };
        game.InitializeForTest(platform);
        scene.PostProcess.Add(new ColorGrade { Tint = Red });
        var frame = Render(game, platform);
        Assert.AreEqual(Red, frame[3, 3], "world (100, 50) is canvas (2, 2) under the camera, inside the chain's picture");
        Assert.AreEqual(Bg, frame[10, 10]);
    }

    [TestMethod]
    public void The_games_chain_processes_the_whole_canvas_and_not_the_interface()
    {
        var game = new PostGame(new Painted(White, new RectF(8, 4, 8, 8)));
        var platform = new HeadlessPlatform(32, 16) { Rasterize = true };
        game.InitializeForTest(platform);
        var graphics = (HeadlessGraphicsDevice)platform.Graphics;
        game.PostProcess.Add(new ColorGrade { Brightness = 0.5f });
        var frame = Render(game, platform);
        Near(new Color(128, 128, 128), frame[10, 6], "the scene");
        Near(new Color(20, 40, 60), frame[1, 10], "and the background behind it");
        var ui = ((IViewCapture)platform).CaptureUi()!;
        Assert.AreEqual(White, ui[0, 0], "the interface is drawn after the canvas's chain, as it is");

        game.PostProcess.Enabled = false;
        Assert.AreEqual(White, Render(game, platform)[10, 6]);
        Assert.IsTrue(graphics.LiveTextures > 0);
        game.Shutdown();
        Assert.AreEqual(0, graphics.LiveTextures, "the game releases its chain when it ends");
    }
}
