using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;
using MicroEngine.Scenes;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The game's frame on an SDL window once its on-screen controls are shown (a phone, or a desktop touch screen in use):
/// the frame and the controls are laid out on the window, never on the game's canvas, which is the render target while
/// the game updates and draws.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlTouchFrameTests
{
    private sealed class TouchGame : Game
    {
        public override GameSettings Settings { get; } = new("touch frame", 320, 180, 2);
        protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
            new TouchDPad(),
            new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight });
        protected override void Load() => Scenes.Push(new Empty());

        private sealed class Empty : Scene
        {
            public override void Update(float dt) { }
            public override void Draw(IRenderer renderer) { }
        }
    }

    [TestMethod]
    [DataRow(false, DisplayName = "deterministic time, no start screen")]
    [DataRow(true, DisplayName = "real time with the start screen, as a player runs it")]
    public void Shown_controls_lay_the_frame_out_on_the_window_not_on_the_canvas(bool realTime)
    {
        SdlPlatform platform;
        try { platform = new SdlPlatform(new PlatformRequest(new GameSettings("touch frame", 320, 180, 2)) { Hidden = true, Mute = true }); }
        catch (InvalidOperationException e) { Native.Require(false, $"SDL cannot open a window here: {e.Message}"); return; }
        using (platform)
        {
            var game = new TouchGame();
            // A finger on the pad shows the controls (they follow the device in use), then lifts.
            var script = InputScript.Parse("""
                2 touch 1 dpad 1 0
                4 lift 1
                12 quit
                """);
            var loop = new GameLoop(game, platform, new RunOptions
            {
                Script = script,
                StartScreen = realTime,
                TimeMode = realTime ? Kernel.TimeMode.RealTime : Kernel.TimeMode.Deterministic,
            });
            if (loop.Start()) while (loop.Tick()) { }
            loop.Finish();

            var screen = ((IScreenView)platform).ScreenPixels;
            var frame = ((IScreenView)platform).WorldFrame;
            Assert.IsTrue(game.Touch!.Shown, "The scripted touch shows the controls.");
            Assert.IsTrue(screen.Width > 320, $"The window is larger than the canvas ({screen.Width}×{screen.Height}).");
            Assert.IsTrue(frame.Width > 320, $"The frame is laid out on the {screen.Width}×{screen.Height} window, not on the 320×180 canvas ({frame}).");
            var (center, _) = game.Touch.Geometry("jump");
            Assert.IsTrue(center.X > 320, $"The controls are laid out on the window: 'jump' at {center}.");
        }
    }
}
