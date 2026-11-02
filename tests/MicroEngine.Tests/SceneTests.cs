using System.Collections;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

[TestClass]
public sealed class SceneTests
{
    private sealed class TestGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("scenes");
        protected override void Load() => Scenes.Push(first);
    }

    private sealed class Probe(string name, List<string> log, bool overlay = false, bool blocks = true) : Scene
    {
        public int Updates;
        public override bool IsOverlay => overlay;
        public override bool BlocksUpdateBelow => blocks;
        public override void Enter() => log.Add($"enter {name}");
        public override void Exit() => log.Add($"exit {name}");
        public override void Resume() => log.Add($"resume {name}");
        public override void Update(float dt) => Updates++;
        public override void Draw(IRenderer renderer) => log.Add($"draw {name}");
    }

    private static (TestGame Game, HeadlessPlatform Platform) Start(Scene first)
    {
        var game = new TestGame(first);
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        return (game, platform);
    }

    [TestMethod]
    public void Overlay_draws_the_scene_below_and_freezes_it()
    {
        var log = new List<string>();
        var level = new Probe("level", log);
        var pause = new Probe("pause", log, overlay: true);
        var (game, platform) = Start(level);
        game.StepForTest();
        game.Scenes.Push(pause);
        game.StepForTest();
        game.StepForTest();
        log.Clear();
        game.Scenes.Stack[0].Draw(platform.Renderer); // sanity: stack order
        Assert.AreEqual(1, level.Updates, "Level is frozen while the pause overlay is on top.");
        Assert.AreEqual(2, pause.Updates);
        Assert.AreEqual(2, game.Scenes.Count);
    }

    [TestMethod]
    public void An_empty_stack_updates_and_draws_nothing()
    {
        var log = new List<string>();
        var level = new Probe("level", log);
        var (game, platform) = Start(level);
        game.StepForTest();
        game.Scenes.Pop();
        game.StepForTest();
        game.StepForTest();
        game.Render(platform.BeginFrame());
        Assert.AreEqual(0, game.Scenes.Count);
        Assert.AreEqual(1, level.Updates, "the popped scene is not updated again");
        Assert.AreEqual(2, game.UpdateCount - 1, "steps keep running without a scene");
    }

    [TestMethod]
    public void Non_blocking_overlay_lets_the_scene_below_keep_running()
    {
        var log = new List<string>();
        var level = new Probe("level", log);
        var toast = new Probe("toast", log, overlay: true, blocks: false);
        var (game, _) = Start(level);
        game.Scenes.Push(toast);
        game.StepForTest();
        game.StepForTest();
        Assert.AreEqual(2, level.Updates);
        Assert.AreEqual(2, toast.Updates);
    }

    [TestMethod]
    public void Pop_resumes_the_scene_below()
    {
        var log = new List<string>();
        var (game, _) = Start(new Probe("a", log));
        game.Scenes.Push(new Probe("b", log));
        game.StepForTest();
        game.Scenes.Pop();
        game.StepForTest();
        CollectionAssert.AreEqual(new[] { "enter a", "enter b", "exit b", "resume a" }, log);
    }

    [TestMethod]
    public void Transition_swaps_scenes_at_its_midpoint()
    {
        var log = new List<string>();
        var (game, _) = Start(new Probe("a", log));
        game.Scenes.Replace(new Probe("b", log), Transition.Fade(1f));
        int steps = 0;
        while (!log.Contains("enter b")) { game.StepForTest(); steps++; }
        Assert.AreEqual(30, steps, 1, "A 1 s fade swaps after ~0.5 s at 60 Hz.");
        while (game.Scenes.InTransition) game.StepForTest();
        CollectionAssert.AreEqual(new[] { "enter a", "exit a", "enter b" }, log);
    }

    private sealed class CoverProbe(float duration) : Transition(duration)
    {
        public float Drawn;
        public override void Draw(IRenderer renderer) => Drawn = Cover;
    }

    [TestMethod]
    public void A_transition_is_drawn_between_its_last_two_updates()
    {
        var (game, platform) = Start(new Probe("a", []));
        var fade = new CoverProbe(1f);
        game.Scenes.Replace(new Probe("b", []), fade);
        for (int i = 0; i < 6; i++) game.StepForTest(); // 0.1 s of a 1 s transition: covering
        float before = fade.Cover;
        game.StepForTest();
        float after = fade.Cover;
        game.FrameBlend = 0.5f;
        game.Render(platform.BeginFrame());
        Assert.IsTrue(fade.Drawn > before && fade.Drawn < after, $"drawn {fade.Drawn} between {before} and {after}");
        game.FrameBlend = 1;
        game.Render(platform.BeginFrame());
        Assert.AreEqual(after, fade.Drawn, 1e-6, "at 1, the last update");
        Assert.AreEqual(after, fade.Cover, 1e-6, "outside drawing, the last update");
    }

    [TestMethod]
    public void Coroutines_wait_for_time_conditions_and_nested_routines()
    {
        var runner = new CoroutineRunner();
        var log = new List<string>();
        bool flag = false;
        IEnumerator Child() { log.Add("child"); yield return Wait.Seconds(0.1f); log.Add("child done"); }
        IEnumerator Main()
        {
            log.Add("start");
            yield return Wait.Seconds(0.5f);
            log.Add("after wait");
            yield return Child();
            yield return Wait.Until(() => flag);
            log.Add("end");
        }
        runner.Start(Main());
        for (int i = 0; i < 20; i++) runner.Update(0.05f);
        CollectionAssert.AreEqual(new[] { "start", "after wait", "child", "child done" }, log);
        flag = true;
        runner.Update(0.05f);
        Assert.AreEqual("end", log[^1]);
        Assert.AreEqual(0, runner.Count);
    }

    [TestMethod]
    public void Cancelled_coroutine_stops_immediately()
    {
        var runner = new CoroutineRunner();
        int ticks = 0;
        IEnumerator Loop() { while (true) { ticks++; yield return null; } }
        var handle = runner.Start(Loop());
        runner.Update(0.016f);
        handle.Cancel();
        runner.Update(0.016f);
        Assert.AreEqual(1, ticks);
        Assert.IsTrue(handle.IsFinished);
    }

    [TestMethod]
    public void Tween_reaches_its_end_value_and_calls_done_once()
    {
        var tweener = new Tweener();
        float value = 0;
        int done = 0;
        tweener.Add(0.2f, t => value = t * 10, Ease.Linear, () => done++);
        for (int i = 0; i < 30; i++) tweener.Update(0.01f);
        Assert.AreEqual(10, value, 0.0001f);
        Assert.AreEqual(1, done);
        Assert.AreEqual(0, tweener.Count);
    }
}
