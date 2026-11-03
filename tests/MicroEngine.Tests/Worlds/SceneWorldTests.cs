using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Worlds;

/// <summary>
/// Scenes own worlds: the engine steps a scene's world with the scene, sets its blend before drawing it from per-scene
/// step tracking (a scene frozen under an overlay draws at blend 1), advances its clock (time scale, pause, hitstop),
/// hashes it with the scene, and lets a scene return a result to the one below.
/// </summary>
[TestClass]
public sealed class SceneWorldTests
{
    private const float Dt = 1 / 60f;

    private sealed class TestGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("worlds");
        protected override void Load() => Scenes.Push(first);
    }

    /// <summary>A scene whose one entity moves 10 px per update; it records what drawing sees.</summary>
    private sealed class Mover(bool overlay = false, bool blocks = true) : Scene
    {
        public EntityId Body;
        public readonly List<float> Steps = [];
        public float DrawnBlend = -1;
        public Vector2 Drawn;
        public override bool IsOverlay => overlay;
        public override bool BlocksUpdateBelow => blocks;
        public override void Enter() => Body = Objects.Create();
        public override void Update(float dt)
        {
            Steps.Add(dt);
            Objects.Local(Body).Position2D += new Vector2(10, 0);
        }
        public override void Draw(IRenderer renderer)
        {
            DrawnBlend = Objects.Blend;
            Drawn = Objects.DrawPosition2D(Body);
        }
    }

    private sealed class Plain : Scene
    {
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class Question(List<string> log) : Scene<int>
    {
        public override bool IsOverlay => true;
        public override void Exit() => log.Add("exit question");
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
        public void Answer(int value) => Finish(value);
    }

    private sealed class Asker(List<string> log) : Scene
    {
        public override void Resume() => log.Add("resume asker");
        public override void Update(float dt) { }
        public override void Draw(IRenderer renderer) { }
    }

    private static (TestGame Game, HeadlessPlatform Platform) Start(Scene first)
    {
        var game = new TestGame(first);
        var platform = new HeadlessPlatform();
        game.InitializeForTest(platform);
        return (game, platform);
    }

    private static void Draw(TestGame game, HeadlessPlatform platform, float blend)
    {
        game.FrameBlend = blend;
        game.Render(platform.BeginFrame());
        game.FrameBlend = 1;
    }

    [TestMethod]
    public void A_scenes_world_steps_with_the_scene_and_its_clock_advances()
    {
        var level = new Mover();
        var (game, _) = Start(level);
        game.StepForTest();
        game.StepForTest();

        var world = (SceneWorld)level.Objects;
        Assert.AreEqual(game.UpdateCount, world.SteppedAt);
        CollectionAssert.AreEqual(new[] { Dt, Dt }, level.Steps, "Update receives the world clock's step, exactly the engine's at scale 1.");
        Assert.AreEqual(2 * (double)Dt, world.Clock.Time, 1e-9);
        Assert.AreEqual(new Vector2(20, 0), world.Local(level.Body).Position2D);
    }

    [TestMethod]
    public void A_playing_scene_is_drawn_between_its_last_two_steps()
    {
        var level = new Mover();
        var (game, platform) = Start(level);
        game.StepForTest();
        game.StepForTest();

        Draw(game, platform, 0.4f);

        Assert.AreEqual(0.4f, level.DrawnBlend);
        Assert.AreEqual(Vector2.Lerp(new(10, 0), new(20, 0), 0.4f), level.Drawn);
        Draw(game, platform, 1);
        Assert.AreEqual(new Vector2(20, 0), level.Drawn, "Exactly the last step at 1.");
    }

    [TestMethod]
    public void A_scene_frozen_under_an_overlay_is_drawn_as_its_last_step()
    {
        var level = new Mover();
        var pause = new Mover(overlay: true);
        var (game, platform) = Start(level);
        game.StepForTest();
        game.StepForTest();
        game.Scenes.Push(pause);
        game.StepForTest();
        game.StepForTest();

        Draw(game, platform, 0.4f);

        Assert.AreEqual(2, level.Steps.Count, "Frozen below the overlay.");
        Assert.AreEqual(1f, level.DrawnBlend, "Not rocking between its last two steps.");
        Assert.AreEqual(new Vector2(20, 0), level.Drawn);
        Assert.AreEqual(0.4f, pause.DrawnBlend, "The overlay itself steps: it blends.");
    }

    [TestMethod]
    public void A_scene_below_a_non_blocking_overlay_keeps_blending()
    {
        var level = new Mover();
        var toast = new Mover(overlay: true, blocks: false);
        var (game, platform) = Start(level);
        game.Scenes.Push(toast);
        game.StepForTest();
        game.StepForTest();

        Draw(game, platform, 0.25f);

        Assert.AreEqual(0.25f, level.DrawnBlend);
        Assert.AreEqual(0.25f, toast.DrawnBlend);
    }

    [TestMethod]
    public void A_scene_resumed_after_an_overlay_blends_again_from_its_next_step()
    {
        var level = new Mover();
        var (game, platform) = Start(level);
        game.StepForTest();
        game.Scenes.Push(new Mover(overlay: true));
        game.StepForTest();
        game.Scenes.Pop();
        game.StepForTest();

        Draw(game, platform, 0.5f);

        Assert.AreEqual(0.5f, level.DrawnBlend);
        Assert.AreEqual(new Vector2(15, 0), level.Drawn);
    }

    [TestMethod]
    public void Hitstop_and_slow_motion_change_only_the_step_the_scene_receives()
    {
        var level = new Mover();
        var (game, _) = Start(level);
        game.StepForTest();
        level.Objects.Clock.Freeze(0.05f);
        for (int i = 0; i < 4; i++) game.StepForTest();
        level.Objects.Clock.Scale = 0.5f;
        game.StepForTest();

        CollectionAssert.AreEqual(new[] { Dt, 0, 0, 0, Dt, Dt * 0.5f }, level.Steps);
        Assert.AreEqual(6 * Dt, level.Age, 1e-5, "The scene's own age, coroutines and tweens keep the engine's time.");
    }

    [TestMethod]
    public void A_scene_paused_under_an_overlay_consumes_no_freeze()
    {
        var level = new Mover();
        var (game, _) = Start(level);
        game.StepForTest();
        level.Objects.Clock.Freeze(0.05f);
        game.Scenes.Push(new Plain { });
        for (int i = 0; i < 10; i++) game.StepForTest();

        Assert.AreEqual(0.05f, level.Objects.Clock.FreezeRemaining);
    }

    [TestMethod]
    public void Entities_destroyed_in_a_step_are_released_when_the_scenes_step_ends()
    {
        var level = new Mover();
        var (game, _) = Start(level);
        game.StepForTest();
        var doomed = level.Objects.Create();
        level.Objects.Destroy(doomed);
        Assert.AreEqual(1, level.Objects.Count);
        game.StepForTest();

        Assert.ThrowsExactly<InvalidOperationException>(() => level.Objects.Local(doomed), "Released at the step's end.");
    }

    [TestMethod]
    public void A_scenes_world_is_part_of_its_state_hash_and_drawing_never_changes_it()
    {
        var level = new Mover();
        var (game, platform) = Start(level);
        game.StepForTest();
        var before = game.ComputeStateHash(sections: true);
        Draw(game, platform, 0.3f);
        Assert.AreEqual(before.Hash, game.ComputeStateHash().Hash, "Drawing never changes the hash.");

        level.Objects.Local(level.Body).Position2D = new Vector2(11, 0);
        var moved = game.ComputeStateHash(sections: true);
        Assert.AreNotEqual(before.Hash, moved.Hash);
        var changed = moved.Sections.Zip(before.Sections).Where(p => p.First.Hash != p.Second.Hash).Select(p => p.First.Name).ToList();
        CollectionAssert.AreEqual(new[] { "scene[0]:Mover" }, changed, "The change shows in the scene's own section.");
    }

    [TestMethod]
    public void A_scene_without_a_world_creates_none()
    {
        var plain = new Plain();
        var (game, platform) = Start(plain);
        game.StepForTest();
        Draw(game, platform, 0.5f);
        Assert.IsFalse(plain.HasWorld);
        _ = plain.Objects;
        Assert.IsTrue(plain.HasWorld);
    }

    [TestMethod]
    public void A_scene_returns_its_result_to_the_scene_below()
    {
        var log = new List<string>();
        var asker = new Asker(log);
        var (game, _) = Start(asker);
        var question = new Question(log);
        game.Scenes.Push(question, answer => log.Add($"answer {answer}"));
        game.StepForTest();

        question.Answer(7);
        Assert.AreEqual(2, game.Scenes.Count, "Deferred like Pop.");
        game.StepForTest();

        CollectionAssert.AreEqual(new[] { "exit question", "answer 7", "resume asker" }, log);
        Assert.AreSame(asker, game.Scenes.Current);
    }

    [TestMethod]
    public void A_scene_removed_without_finishing_returns_nothing_and_finishing_twice_returns_once()
    {
        var log = new List<string>();
        var (game, _) = Start(new Asker(log));
        var dropped = new Question(log);
        game.Scenes.Push(dropped, answer => log.Add($"answer {answer}"));
        game.StepForTest();
        game.Scenes.Pop();
        game.StepForTest();
        Assert.IsFalse(log.Any(l => l.StartsWith("answer", StringComparison.Ordinal)), "Popped by someone else: no result.");

        var twice = new Question(log);
        game.Scenes.Push(twice, answer => log.Add($"answer {answer}"));
        game.StepForTest();
        twice.Answer(1);
        twice.Answer(2);
        game.StepForTest();
        game.StepForTest();

        Assert.AreEqual(1, log.Count(l => l.StartsWith("answer", StringComparison.Ordinal)));
        Assert.IsTrue(log.Contains("answer 1"));
        Assert.AreEqual(1, game.Scenes.Count, "Finishing twice pops once.");
    }

    [TestMethod]
    public void A_result_scene_pushed_without_a_callback_just_pops_when_it_finishes()
    {
        var log = new List<string>();
        var (game, _) = Start(new Asker(log));
        var question = new Question(log);
        game.Scenes.Push(question);
        game.StepForTest();
        question.Answer(3);
        game.StepForTest();

        CollectionAssert.AreEqual(new[] { "exit question", "resume asker" }, log);
    }
}
