using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

[TestClass]
public sealed class InputTests
{
    private static (InputMap Map, HeadlessInput Keys) Create()
    {
        var map = new InputMap().Bind("jump", Key.Space, Key.Z);
        return (map, new HeadlessInput());
    }

    [TestMethod]
    public void Pressed_is_true_for_exactly_one_update()
    {
        var (map, keys) = Create();
        keys.Set(Key.Space, true);
        map.Sample(keys);
        Assert.IsTrue(map.Pressed("jump"));
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsFalse(map.Pressed("jump"));
        Assert.IsTrue(map.Down("jump"));
    }

    [TestMethod]
    public void A_tap_shorter_than_an_update_is_not_lost()
    {
        var (map, keys) = Create();
        keys.Set(Key.Z, true);
        map.Sample(keys);
        keys.Set(Key.Z, false);
        map.Sample(keys); // released before the next fixed update ran
        Assert.IsTrue(map.Pressed("jump"));
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsFalse(map.Pressed("jump"));
        Assert.IsFalse(map.Down("jump"));
    }

    [TestMethod]
    public void Released_fires_once_after_release()
    {
        var (map, keys) = Create();
        keys.Set(Key.Space, true);
        map.Sample(keys); map.EndUpdate();
        keys.Set(Key.Space, false);
        map.Sample(keys);
        Assert.IsTrue(map.Released("jump"));
        map.EndUpdate();
        map.Sample(keys);
        Assert.IsFalse(map.Released("jump"));
    }

    [TestMethod]
    public void Reset_requires_held_keys_to_be_pressed_again()
    {
        var (map, keys) = Create();
        keys.Set(Key.Space, true);
        map.Sample(keys);
        map.Reset();
        Assert.IsFalse(map.Pressed("jump"));
        Assert.IsTrue(map.Down("jump"));
    }

    [TestMethod]
    public void Axis_combines_opposing_actions()
    {
        var map = InputMap.CreateDefault();
        var keys = new HeadlessInput();
        keys.Set(Key.Left, true);
        map.Sample(keys);
        Assert.AreEqual(-1, map.Axis("left", "right"));
        keys.Set(Key.D, true);
        map.Sample(keys);
        Assert.AreEqual(0, map.Axis("left", "right"));
    }
}

[TestClass]
public sealed class RunnerTests
{
    private sealed class CountingGame : Game
    {
        public override GameSettings Settings { get; } = new("test");
        public CountingScene Scene { get; } = new();
        protected override void Load() => Scenes.Push(Scene);
    }

    private sealed class CountingScene : Scene
    {
        public int Updates, Draws, Confirms;
        public override void Update(float dt)
        {
            Updates++;
            if (Input.Pressed("confirm")) Confirms++;
        }
        public override void Draw(IRenderer renderer) { Draws++; renderer.FillRect(new RectF(0, 0, 1, 1), Color.White); }
    }

    [TestMethod]
    public void Headless_run_is_one_update_per_frame_and_honours_script_quit()
    {
        var game = new CountingGame();
        var script = InputScript.Parse("10 press confirm\n20 press confirm\n30 capture shot.png\n40 quit");
        var platform = new HeadlessPlatform();
        var result = GameRunner.Run(game, platform, new RunOptions { TimeMode = MicroEngine.Kernel.TimeMode.Deterministic, Script = script });
        Assert.IsNull(result.Error);
        Assert.AreEqual(40, result.Updates);
        Assert.AreEqual(40, game.Scene.Updates);
        Assert.AreEqual(2, game.Scene.Confirms);
        Assert.AreEqual(1, result.Captures.Count);
        Assert.AreEqual(1, platform.Screenshots.Count);
    }

    [TestMethod]
    public void Max_updates_stops_the_run()
    {
        var result = GameRunner.Run(new CountingGame(), new HeadlessPlatform(), new RunOptions { TimeMode = MicroEngine.Kernel.TimeMode.Deterministic, MaxUpdates = 25 });
        Assert.AreEqual(25, result.Updates);
    }

    [TestMethod]
    public void Errors_are_reported_not_thrown()
    {
        var game = new ThrowingGame();
        var result = GameRunner.Run(game, new HeadlessPlatform(), new RunOptions { TimeMode = MicroEngine.Kernel.TimeMode.Deterministic, MaxUpdates = 10 });
        Assert.IsInstanceOfType<InvalidOperationException>(result.Error);
    }

    private sealed class ThrowingGame : Game
    {
        public override GameSettings Settings { get; } = new("boom");
        protected override void Load() => throw new InvalidOperationException("boom");
    }

    [TestMethod]
    public void Frame_by_frame_loop_matches_the_blocking_run()
    {
        var game = new CountingGame();
        var platform = new HeadlessPlatform();
        var loop = new GameLoop(game, platform, new RunOptions { TimeMode = MicroEngine.Kernel.TimeMode.Deterministic, Script = InputScript.Parse("10 press confirm\n20 press confirm\n30 capture shot.png\n40 quit") });
        Assert.IsTrue(loop.Start());
        int ticks = 0;
        while (loop.Tick()) ticks++;
        Assert.IsFalse(loop.Tick(), "a finished loop stays finished");
        var result = loop.Finish();
        Assert.IsNull(result.Error);
        Assert.AreEqual(40, result.Updates);
        Assert.AreEqual(39, ticks, "the frame that reaches the quit update reports the end");
        Assert.AreEqual(2, game.Scene.Confirms);
        Assert.AreEqual(1, platform.Screenshots.Count);
        Assert.AreSame(result, loop.Finish(), "finishing twice shuts the game down once");
    }

    [TestMethod]
    public void Real_time_loop_steps_by_elapsed_time_not_by_frame_rate()
    {
        foreach (var (fps, frames, expected) in new[] { (30, 30, 60), (60, 60, 60), (144, 144, 60) })
        {
            var game = new CountingGame();
            var platform = new ClockPlatform();
            var loop = new GameLoop(game, platform);
            loop.Start();
            for (int i = 0; i < frames; i++)
            {
                platform.Now += 1.0 / fps;
                loop.Tick();
            }
            Assert.AreEqual(expected, game.UpdateCount, 1, $"{fps} fps for one second");
            Assert.AreEqual(frames, game.Scene.Draws, $"{fps} fps renders every frame");
        }
    }

    [TestMethod]
    public void Loop_start_failure_is_reported_and_stops_ticking()
    {
        var loop = new GameLoop(new ThrowingGame(), new HeadlessPlatform());
        Assert.IsFalse(loop.Start());
        Assert.IsFalse(loop.Tick());
        Assert.IsInstanceOfType<InvalidOperationException>(loop.Finish().Error);
    }

    /// <summary>A real-time platform whose clock the test advances by hand.</summary>
    private sealed class ClockPlatform : TestPlatform
    {
        public ClockPlatform() => Now = 0;
    }

    [TestMethod]
    public void Script_parser_supports_relative_lines_and_rejects_garbage()
    {
        var script = InputScript.Parse("# intro\n10 hold right 5\n@+20 capture a.png\n@+5 quit");
        Assert.AreEqual("a.png", script.CaptureAt(30));
        Assert.AreEqual(35L, script.QuitUpdate);
        Assert.ThrowsExactly<FormatException>(() => InputScript.Parse("10 dance confirm"));
        Assert.ThrowsExactly<FormatException>(() => InputScript.Parse("ten press confirm"));
    }
}
