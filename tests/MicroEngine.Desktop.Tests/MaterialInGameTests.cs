using System.Numerics;
using MicroEngine.Conformance.Graphics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using Dev = MicroEngine.Graphics.Devices;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// Materials from inside a game on the tier-1 device, through the normal composition path: the drawing API
/// (<see cref="IRenderer"/>) drawing a scene's sprites through a material, and a pass over the whole finished canvas from
/// the game's <c>DrawAboveScenes</c>. Each frame's canvas is read back inside the frame, after flushing.
/// </summary>
[TestClass]
[TestCategory("Native")]
[TestCategory("Gpu")]
public sealed class MaterialInGameTests
{
    private const int W = 64, H = 32;

    private sealed class EffectGame : Game
    {
        public override GameSettings Settings { get; } = new("material test", W, H, 2) { Background = new MicroEngine.Color(0, 0, 0) };
        public bool WholeFrame;
        public Image? Canvas;
        private Renderer2D? _batcher;
        private Dev.Shader? _vertex, _fragment;
        private Dev.RenderTarget? _copy;
        public Material2D? Gray;
        public Renderer2D Batcher => _batcher!;

        protected override void Load()
        {
            var device = ((DeviceGraphics)Graphics).Device;
            _batcher = new Renderer2D(device);
            _vertex = ShaderFiles.Compiled("pixel.vert").Create(device);
            _fragment = ShaderFiles.Compiled("grayscale.frag").Create(device);
            Gray = new Material2D(_vertex, _fragment);
            Gray.SetUniforms(new Vector4(1, 0, 0, 0));
            _copy = device.CreateRenderTarget(new Dev.RenderTargetDescription(W, H));
            Scenes.Push(new EffectScene());
        }

        protected override void DrawAboveScenes(IRenderer renderer)
        {
            var canvas = (DeviceRenderer)renderer;
            canvas.Flush();
            if (WholeFrame)
            {
                // The finished canvas, copied, then drawn back through the material: a pass over everything the scenes drew.
                var whole = new RectF(0, 0, W, H);
                _batcher!.Begin(new BatchSettings(_copy!, Blend: Dev.BlendState.Opaque));
                _batcher.Draw(canvas.Target.Color!, whole, Matrix3x2.Identity, ColorF.White);
                _batcher.End();
                _batcher.Begin(new BatchSettings(canvas.Target, Blend: Dev.BlendState.Opaque) { Material = Gray });
                _batcher.Draw(_copy!.Color!, whole, Matrix3x2.Identity, ColorF.White);
                _batcher.End();
            }
            var request = canvas.Target.Device.ReadPixels(canvas.Target, canvas.Target.Bounds);
            request.Wait();
            Canvas = new Image(W, H, request.Pixels.ToArray());
        }
    }

    private sealed class EffectScene : Scene
    {
        private MicroEngine.Graphics.Texture? _red;

        public override void Enter()
        {
            var image = new PixelImage(4, 4);
            image.Clear(new MicroEngine.Color(255, 0, 0));
            _red = Game.Graphics.CreateTexture(image, TextureFilter.Nearest);
        }

        public override void Update(float dt) { }

        public override void Draw(IRenderer r)
        {
            var game = (EffectGame)Game;
            r.Draw(_red!, new RectF(0, 0, 4, 4), new Vector2(4, 4), MicroEngine.Color.White);   // the usual drawing: red
            if (game.WholeFrame) return;
            // The drawing API over a batcher of the scene's own, into the canvas, with a material on its state.
            var canvas = (DeviceRenderer)r;
            canvas.Flush();
            var effects = new DeviceRenderer(game.Batcher, (DeviceGraphics)Game.Graphics, canvas.Target);
            effects.Begin();
            game.Batcher.State = game.Batcher.State with { Material = game.Gray };
            effects.Draw(_red!, new RectF(0, 0, 4, 4), new Vector2(20, 4), MicroEngine.Color.White);
            effects.End();
        }
    }

    private static EffectGame Run(bool wholeFrame)
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS");
        var game = new EffectGame { WholeFrame = wholeFrame };
        var errors = new StringWriter();
        var (previousError, previousOut) = (Console.Error, Console.Out);
        Console.SetError(errors);
        Console.SetOut(new StringWriter());
        int code;
        try { code = DesktopLauncher.Run(game, ["--platform", "sdl", "--graphics", "sdl-gpu", "--hidden", "--frames", "3"]); }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOut);
        }
        if (code != 0 && errors.ToString().Contains("cannot start here")) Native.Require(false, errors.ToString().Trim());
        Assert.AreEqual(0, code, errors.ToString());
        return game;
    }

    private static void AssertGrayOfRed(Image image, int x, int y)
    {
        for (int channel = 0; channel < 3; channel++)
            Assert.IsTrue(Math.Abs(image.Channel(x, y, channel) - 0.299f * 255) <= 1, $"({x},{y}) channel {channel}: the luma of red is 76, got {image.Channel(x, y, channel)}");
    }

    [TestMethod]
    public void The_drawing_API_draws_a_scene_s_sprites_through_a_material()
    {
        var canvas = Run(wholeFrame: false).Canvas!;
        Assert.AreEqual(0xFF0000FFu, canvas[5, 5], "the sprite drawn as usual");
        AssertGrayOfRed(canvas, 21, 5);
        Assert.AreEqual(0x000000FFu, canvas[40, 20], "the background");
    }

    [TestMethod]
    public void A_pass_from_DrawAboveScenes_covers_everything_the_scenes_drew()
    {
        var canvas = Run(wholeFrame: true).Canvas!;
        AssertGrayOfRed(canvas, 5, 5);
        Assert.AreEqual(0x000000FFu, canvas[40, 20], "black stays black");
    }
}
