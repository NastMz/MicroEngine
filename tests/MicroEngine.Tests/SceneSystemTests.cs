using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

[TestClass]
public sealed class SceneSystemTests
{
    private sealed class TestGame(Scene first, Action<EngineBuilder>? configure = null) : Game
    {
        public override GameSettings Settings { get; } = new("scene-systems");
        public override void Configure(EngineBuilder builder) => configure?.Invoke(builder);
        protected override void Load() => Scenes.Push(first);
    }

    private class Recorder(string name, List<string> log, int order = 0) : SceneSystem
    {
        public int State;
        public override int Order => order;
        protected override void Start() => log.Add($"start {name}");
        protected override void Update(float dt) => log.Add($"update {name}");
        protected override void LateUpdate(float dt) => log.Add($"late {name}");
        protected override void BeginDraw(float blend) => log.Add($"draw {name} {blend:0.##}");
        protected override void Stop() => log.Add($"stop {name}");
        public override void HashState(StateHasher hash) => hash.Add(State);
    }

    private sealed class Other(string name, List<string> log, int order = 0) : Recorder(name, log, order);
    private sealed class Third(string name, List<string> log, int order = 0) : Recorder(name, log, order);

    private sealed class Host(List<string> log, bool overlay = false) : Scene
    {
        public Action<Host>? OnEnter;
        public Action<Host>? OnUpdate;
        public override bool IsOverlay => overlay;
        public override void Enter() { log.Add("enter"); OnEnter?.Invoke(this); }
        public override void Exit() => log.Add("exit");
        public override void Update(float dt) { log.Add("scene update"); OnUpdate?.Invoke(this); }
        public override void Draw(IRenderer renderer) => log.Add("scene draw");
    }

    private static (TestGame Game, HeadlessPlatform Platform) Start(Scene first, Action<EngineBuilder>? configure = null)
    {
        var game = new TestGame(first, configure);
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        return (game, platform);
    }

    [TestMethod]
    public void A_system_starts_before_enter_steps_around_the_update_and_stops_after_exit()
    {
        var log = new List<string>();
        var scene = new Host(log);
        scene.Systems.Add(new Recorder("a", log));
        var (game, _) = Start(scene);
        game.StepForTest();
        game.Scenes.Pop();
        game.StepForTest();
        CollectionAssert.AreEqual(
            new[] { "start a", "enter", "update a", "scene update", "late a", "exit", "stop a" }, log);
    }

    [TestMethod]
    public void Systems_run_by_order_then_by_addition_and_stop_in_reverse()
    {
        var log = new List<string>();
        var scene = new Host(log);
        scene.Systems.Add(new Recorder("late", log, order: 10));
        scene.Systems.Add(new Other("first", log, order: -5));
        scene.Systems.Add(new Third("second", log, order: -5));
        var (game, _) = Start(scene);
        game.StepForTest();
        game.Scenes.Pop();
        game.StepForTest();
        CollectionAssert.AreEqual(new[]
        {
            "start first", "start second", "start late", "enter",
            "update first", "update second", "update late", "scene update",
            "late first", "late second", "late late",
            "exit", "stop late", "stop second", "stop first",
        }, log);
    }

    [TestMethod]
    public void A_system_added_from_enter_starts_at_once_and_one_added_during_a_step_joins_the_next_call()
    {
        var log = new List<string>();
        var scene = new Host(log) { OnEnter = s => s.Systems.Add(new Recorder("a", log)) };
        bool added = false;
        scene.OnUpdate = s =>
        {
            if (added) return;
            added = true;
            s.Systems.Add(new Other("b", log));
        };
        var (game, _) = Start(scene);
        game.StepForTest();
        CollectionAssert.AreEqual(
            new[] { "enter", "start a", "update a", "scene update", "start b", "late a", "late b" }, log);
    }

    [TestMethod]
    public void A_scene_has_one_system_per_type_and_a_system_one_scene()
    {
        var log = new List<string>();
        var scene = new Host(log);
        var system = scene.Systems.Add(new Recorder("a", log));
        Assert.ThrowsExactly<InvalidOperationException>(() => scene.Systems.Add(new Recorder("again", log)));
        Assert.ThrowsExactly<InvalidOperationException>(() => new Host(log).Systems.Add(system));
        Assert.AreSame(system, scene.Systems.Get<Recorder>());
        Assert.IsFalse(scene.Systems.TryGet<Other>(out _));
        Assert.ThrowsExactly<InvalidOperationException>(() => scene.Systems.Get<Other>());
    }

    [TestMethod]
    public void A_removed_system_stops_once_and_can_be_added_again()
    {
        var log = new List<string>();
        var scene = new Host(log);
        var system = scene.Systems.Add(new Recorder("a", log));
        var (game, _) = Start(scene);
        Assert.IsTrue(scene.Systems.Remove(system));
        Assert.IsFalse(scene.Systems.Remove(system));
        Assert.IsFalse(system.IsAttached);
        game.StepForTest();
        scene.Systems.Add(system);
        CollectionAssert.AreEqual(new[] { "start a", "enter", "stop a", "scene update", "start a" }, log);
    }

    [TestMethod]
    public void A_scene_frozen_under_an_overlay_does_not_step_its_systems_and_draws_them_still()
    {
        var log = new List<string>();
        var scene = new Host(log);
        scene.Systems.Add(new Recorder("a", log));
        var (game, platform) = Start(scene);
        game.StepForTest();
        game.Scenes.Push(new Host([], overlay: true));
        game.StepForTest();
        log.Clear();
        game.StepForTest();
        game.Render(platform.BeginFrame());
        CollectionAssert.AreEqual(new[] { "draw a 1", "scene draw" }, log);
    }

    [TestMethod]
    public void A_scene_without_systems_hashes_as_before_and_a_systems_state_is_in_the_hash()
    {
        ulong Hash(Action<Host>? setup)
        {
            var scene = new Host([]) { OnEnter = setup };
            var (game, _) = Start(scene);
            game.StepForTest();
            return game.ComputeStateHash().Hash;
        }

        ulong plain = Hash(null);
        Assert.AreEqual(plain, Hash(s => _ = s.Systems.Count), "reading Systems adds nothing to the hash");
        ulong withSystem = Hash(s => s.Systems.Add(new Recorder("a", [])));
        Assert.AreNotEqual(plain, withSystem);
        Assert.AreNotEqual(withSystem, Hash(s => s.Systems.Add(new Recorder("a", []) { State = 7 })));
        Assert.AreEqual(withSystem, Hash(s => s.Systems.Add(new Recorder("renamed", []))), "only what HashState adds counts");
    }

    [TestMethod]
    public void The_manager_gives_every_scene_a_system_and_reports_scenes_entering_and_leaving()
    {
        var log = new List<string>();
        var first = new Host(log);
        var (game, _) = Start(first, builder =>
        {
            var scenes = builder.Services.Get<SceneManager>();
            scenes.AddSystem(scene => scene is Host { IsOverlay: false } ? new Recorder("auto", log) : null);
            scenes.Entered += scene => log.Add($"entered {scene.GetType().Name}");
            scenes.Exited += scene => log.Add($"exited {scene.GetType().Name}");
        });
        Assert.IsTrue(first.Systems.Contains<Recorder>());
        var overlay = new Host([], overlay: true);
        game.Scenes.Push(overlay);
        game.StepForTest();
        Assert.AreEqual(0, overlay.Systems.Count);
        game.Scenes.Reset(new Host([]));
        game.StepForTest();
        CollectionAssert.AreEqual(new[]
        {
            "start auto", "enter", "entered Host", "entered Host",
            "exited Host", "exit", "stop auto", "exited Host", "start auto", "entered Host",
            "update auto", "late auto",
        }, log);
    }

    [TestMethod]
    public void A_system_that_throws_on_stop_does_not_keep_the_others_from_stopping()
    {
        var log = new List<string>();
        var scene = new Host(log);
        scene.Systems.Add(new Recorder("a", log));
        scene.Systems.Add(new Failing());
        var (game, _) = Start(scene);
        game.Scenes.Pop();
        Assert.ThrowsExactly<InvalidOperationException>(game.StepForTest);
        CollectionAssert.Contains(log, "stop a");
        Assert.IsFalse(scene.OnStack);
    }

    private sealed class Failing : SceneSystem
    {
        protected override void Stop() => throw new InvalidOperationException("stop failed");
    }

    [TestMethod]
    public void Lifecycle_reports_the_start_and_the_shutdown_and_module_sections_are_hashed()
    {
        var log = new List<string>();
        int gold = 3;
        var scene = new Host(log);
        var (game, _) = Start(scene, builder =>
        {
            builder.Lifecycle.Started += () => log.Add("started");
            builder.Lifecycle.Stopping += () => log.Add("stopping");
            builder.StateSections.Add("test.economy", hash => hash.Add(gold));
        });
        CollectionAssert.AreEqual(new[] { "enter", "started" }, log);
        var before = game.ComputeStateHash(sections: true);
        Assert.IsTrue(before.Sections.Any(s => s.Name == "test.economy"));
        gold = 4;
        Assert.AreNotEqual(before.Hash, game.ComputeStateHash().Hash);
        game.Shutdown();
        CollectionAssert.AreEqual(new[] { "enter", "started", "exit", "stopping" }, log);
    }

    [TestMethod]
    public void State_sections_have_unique_names()
    {
        var sections = new StateSections();
        sections.Add("a", _ => { });
        Assert.ThrowsExactly<InvalidOperationException>(() => sections.Add("a", _ => { }));
        Assert.ThrowsExactly<ArgumentException>(() => sections.Add(" ", _ => { }));
    }

    private sealed class TimerScene : Scene
    {
        public readonly List<string> Log = [];
        public int Steps;
        public Action<TimerScene>? OnEnter;
        public override void Enter() => OnEnter?.Invoke(this);
        public override void Update(float dt) => Steps++;
        public override void Draw(IRenderer renderer) { }
    }

    [TestMethod]
    public void A_timer_calls_its_action_once_after_its_delay_before_the_scenes_update()
    {
        var scene = new TimerScene { OnEnter = s => s.Timers.After(0.5f, () => s.Log.Add($"fired at {s.Steps}")) };
        var (game, _) = Start(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();
        // 60 steps a second: the 30th step reaches half a second, and timers run before the scene counts that step.
        CollectionAssert.AreEqual(new[] { "fired at 29" }, scene.Log);
        Assert.AreEqual(0, scene.Timers.Count);
    }

    [TestMethod]
    public void A_repeating_timer_keeps_its_rhythm_ends_after_its_count_and_can_be_cancelled()
    {
        TimerHandle forever = default;
        var scene = new TimerScene
        {
            OnEnter = s =>
            {
                s.Timers.Every(0.25f, () => s.Log.Add($"three {s.Steps}"), times: 3);
                forever = s.Timers.Every(0.5f, () => s.Log.Add($"forever {s.Steps}"));
            },
        };
        var (game, _) = Start(scene);
        for (int i = 0; i < 60; i++) game.StepForTest();
        CollectionAssert.AreEqual(new[] { "three 14", "three 29", "forever 29", "three 44", "forever 59" }, scene.Log);
        Assert.IsTrue(scene.Timers.IsActive(forever));
        Assert.IsTrue(scene.Timers.Cancel(forever));
        Assert.IsFalse(scene.Timers.Cancel(forever));
        for (int i = 0; i < 60; i++) game.StepForTest();
        Assert.AreEqual(5, scene.Log.Count);
    }

    [TestMethod]
    public void Timers_follow_the_worlds_clock_unless_unscaled_and_a_timer_may_cancel_itself()
    {
        TimerHandle self = default;
        var scene = new TimerScene
        {
            OnEnter = s =>
            {
                s.Objects.Clock.Freeze(10f);
                s.Timers.After(0.1f, () => s.Log.Add("scaled"));
                s.Timers.After(0.1f, () => s.Log.Add("unscaled"), unscaled: true);
                self = s.Timers.Every(0.05f, () => { s.Log.Add("self"); s.Timers.Cancel(self); }, unscaled: true);
            },
        };
        var (game, _) = Start(scene);
        for (int i = 0; i < 30; i++) game.StepForTest();
        CollectionAssert.AreEqual(new[] { "self", "unscaled" }, scene.Log);
        Assert.AreEqual(1, scene.Timers.Count, "the frozen world's timer is still waiting");
    }

    [TestMethod]
    public void Timers_are_part_of_the_state_hash_and_end_with_the_scene()
    {
        var scene = new TimerScene { OnEnter = s => s.Timers.After(5f, () => s.Log.Add("never")) };
        var (game, _) = Start(scene);
        game.StepForTest();
        ulong first = game.ComputeStateHash().Hash;
        game.StepForTest();
        Assert.AreNotEqual(first, game.ComputeStateHash().Hash);
        var timers = scene.Timers;
        game.Scenes.Pop();
        game.StepForTest();
        Assert.AreEqual(0, timers.Count);
        Assert.IsFalse(timers.IsAttached);
    }

    [TestMethod]
    public void Timer_arguments_are_checked()
    {
        var timers = new SceneTimers();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.After(-1f, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.After(float.NaN, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.Every(0f, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.Every(1f, () => { }, times: -1));
    }
}
