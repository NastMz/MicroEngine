using System.Collections;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests;

/// <summary>
/// Tweens as a game uses them: a handle to wait for from a coroutine, cancel or complete; a starting value applied the
/// moment the tween is added; the scene's order (tweens, then coroutines, then the rest); the world's clock or the
/// engine's; a length in seconds or in updates.
/// </summary>
[TestClass]
public sealed class TweenTests
{
    private const float Step = 1f / 60f;
    /// <summary>A step whose sums are exact, for tests that count on it.</summary>
    private const float Q = 1f / 64f;

    private static int Percent(float t) => (int)MathF.Round(t * 100);

    private sealed class TestGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("tweens");
        protected override void Load() => Scenes.Push(first);
    }

    private sealed class Stage : Scene
    {
        public readonly List<string> Log = [];
        public int Updates;
        public Action<Stage>? OnEnter;
        public Action<Stage>? OnUpdate;
        public override void Enter() => OnEnter?.Invoke(this);
        public override void Update(float dt) { Updates++; OnUpdate?.Invoke(this); }
        public override void Draw(IRenderer renderer) { }
    }

    private static TestGame Start(Scene scene)
    {
        var game = new TestGame(scene);
        game.InitializeForTest(new HeadlessPlatform());
        return game;
    }

    [TestMethod]
    public void A_tween_applies_its_starting_value_when_it_is_added_and_advances_from_the_next_update()
    {
        var tweener = new Tweener();
        var values = new List<float>();
        float current = -1;
        var tween = tweener.Add(4 * Q, t => current = t, Ease.Linear);
        Assert.AreEqual(0f, current, "The target holds the starting value before anything is drawn.");
        Assert.IsFalse(tween.IsFinished);
        Assert.AreEqual(0f, tween.Progress);
        for (int i = 0; i < 6; i++) { tweener.Update(Q); values.Add(current); }
        Assert.AreEqual(0.25f, values[0], 1e-6f);
        Assert.AreEqual(0.5f, values[1], 1e-6f);
        Assert.AreEqual(0.75f, values[2], 1e-6f);
        Assert.AreEqual(1f, values[3]);
        Assert.IsTrue(tween.IsFinished);
        Assert.AreEqual(1f, tween.Progress);
        Assert.AreEqual(0, tweener.Count);
    }

    [TestMethod]
    public void A_delayed_tween_leaves_its_target_alone_until_the_delay_ends_then_starts_from_its_first_value()
    {
        var tweener = new Tweener();
        float current = -1;
        tweener.Add(2 * Q, t => current = t, Ease.Linear, delay: 2 * Q);
        Assert.AreEqual(-1f, current);
        tweener.Update(Q);
        Assert.AreEqual(-1f, current);
        tweener.Update(Q);
        Assert.AreEqual(0f, current, "The update the delay ends on applies the starting value.");
        tweener.Update(Q);
        Assert.AreEqual(0.5f, current, 1e-6f);
        tweener.Update(Q);
        Assert.AreEqual(1f, current);
    }

    [TestMethod]
    public void A_cancelled_tween_stops_where_it_is_and_a_completed_one_jumps_to_its_end()
    {
        var tweener = new Tweener();
        float a = -1, b = -1;
        int doneA = 0, doneB = 0;
        var cancelled = tweener.Add(1f, t => a = t, Ease.Linear, () => doneA++);
        var completed = tweener.Add(1f, t => b = t, Ease.Linear, () => doneB++);
        for (int i = 0; i < 30; i++) tweener.Update(Q);
        float before = a;
        cancelled.Cancel();
        completed.Complete();
        Assert.IsTrue(cancelled.IsFinished);
        Assert.IsTrue(completed.IsFinished);
        Assert.AreEqual(1f, b, "Completing applies the final value at once.");
        Assert.AreEqual((0, 1), (doneA, doneB), "Only the completed tween calls done.");
        Assert.AreEqual(0, tweener.Count);
        for (int i = 0; i < 60; i++) tweener.Update(Q);
        Assert.AreEqual(before, a);
        Assert.AreEqual((0, 1), (doneA, doneB));
        cancelled.Cancel();
        completed.Complete();                         // both safe to repeat
        cancelled.Complete();                         // a cancelled tween stays where it stopped
        Assert.AreEqual(before, a);
        Assert.AreEqual((0, 1), (doneA, doneB));
    }

    [TestMethod]
    public void Tweens_added_or_cancelled_from_a_callback_are_handled_and_done_runs_in_the_order_added()
    {
        var tweener = new Tweener();
        var log = new List<string>();
        Tween? victim = null;
        tweener.Add(2 * Q, _ => { }, done: () =>
        {
            log.Add("first");
            victim!.Cancel();
            tweener.Add(Q, t => log.Add($"chained {t}"), Ease.Linear, () => log.Add("chained done"));
        });
        tweener.Add(2 * Q, _ => { }, done: () => log.Add("second"));
        victim = tweener.Add(3 * Q, _ => { }, done: () => log.Add("never"));
        tweener.Update(Q);
        tweener.Update(Q);
        CollectionAssert.AreEqual(new[] { "first", "chained 0", "second" }, log);
        tweener.Update(Q);
        CollectionAssert.AreEqual(new[] { "first", "chained 0", "second", "chained 1", "chained done" }, log);
        Assert.AreEqual(0, tweener.Count);
    }

    [TestMethod]
    public void A_tween_bound_to_an_entity_ends_when_the_entity_is_destroyed()
    {
        var world = new SceneWorld();
        var coin = world.Create();
        var tweener = new Tweener();
        int done = 0;
        var tween = tweener.Add(1f, t => world.Local(coin).Position2D = new(t * 100, 0), Ease.Linear, () => done++).Bind(world, coin);
        world.BeginStep(1, Q);
        tweener.Update(Q);
        world.EndStep();
        Assert.IsFalse(tween.IsFinished);
        world.BeginStep(2, Q);
        world.Destroy(coin);
        world.EndStep();                               // the slot is released: writing through the id would throw
        world.BeginStep(3, Q);
        tweener.Update(Q);
        world.EndStep();
        Assert.IsTrue(tween.IsFinished);
        Assert.AreEqual(0, done, "A tween that lost its entity is cancelled, not completed.");
        Assert.AreEqual(0, tweener.Count);
    }

    [TestMethod]
    public void A_length_in_steps_takes_that_many_updates_and_moves_evenly()
    {
        foreach (float dt in new[] { Q, 1f / 144f, 0.2f })
        {
            var tweener = new Tweener();
            var values = new List<float>();
            float current = -1;
            var tween = tweener.AddSteps(4, t => current = t, Ease.Linear, delaySteps: 2);
            Assert.AreEqual(-1f, current);
            for (int i = 0; i < 7; i++) { tweener.Update(dt); values.Add(current); }
            CollectionAssert.AreEqual(new[] { -1f, 0f, 0.25f, 0.5f, 0.75f, 1f, 1f }, values, $"step {dt}");
            Assert.IsTrue(tween.IsFinished);
        }
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Tweener().AddSteps(-1, _ => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Tweener().AddSteps(3, _ => { }, delaySteps: -1));
    }

    [TestMethod]
    public void A_scaled_tween_follows_the_second_clock_and_the_state_hash_tells_them_apart()
    {
        var tweener = new Tweener();
        float engine = -1, world = -1;
        tweener.Add(4 * Q, t => engine = t, Ease.Linear);
        tweener.Add(4 * Q, t => world = t, Ease.Linear, scaled: true);
        tweener.AddSteps(4, _ => { }, scaled: true);
        for (int i = 0; i < 2; i++) tweener.Update(Q, 0);          // the world is frozen
        Assert.AreEqual(0.5f, engine, 1e-6f);
        Assert.AreEqual(0f, world);
        for (int i = 0; i < 2; i++) tweener.Update(Q, Q / 2);   // slow motion
        Assert.AreEqual(1f, engine);
        Assert.AreEqual(0.25f, world, 1e-6f);
        Assert.AreEqual(2, tweener.Count);

        static ulong Hash(Tweener t) { var h = new StateHasher(); t.HashState(h); return h.Value; }
        var a = new Tweener();
        var b = new Tweener();
        a.Add(1f, _ => { });
        b.Add(1f, _ => { }, scaled: true);
        Assert.AreNotEqual(Hash(a), Hash(b));
        var c = new Tweener();
        c.Add(1f, _ => { });
        Assert.AreEqual(Hash(a), Hash(c));
        a.Update(Q);
        Assert.AreNotEqual(Hash(a), Hash(c));
    }

    [TestMethod]
    public void A_coroutine_waits_for_a_tween_and_goes_on_in_the_update_it_ends()
    {
        var tweener = new Tweener();
        var runner = new CoroutineRunner();
        var log = new List<string>();
        int update = 0;
        IEnumerator Cutscene()
        {
            log.Add($"start {update}");
            yield return tweener.Add(3 * Q, t => log.Add($"{update}: {Percent(t)}"), Ease.Linear);
            log.Add($"after {update}");
            var skipped = tweener.Add(1f, _ => { });
            skipped.Cancel();
            yield return skipped;                      // already over: no update is spent
            log.Add($"same {update}");
        }
        runner.Start(Cutscene());
        for (update = 1; update <= 6; update++)
        {
            tweener.Update(Q);                      // a scene's order: tweens, then coroutines
            runner.Update(Q);
        }
        CollectionAssert.AreEqual(new[] { "start 1", "1: 0", "2: 33", "3: 67", "4: 100", "after 4", "same 4" }, log);
        Assert.AreEqual(0, runner.Count);
    }

    [TestMethod]
    public void A_coroutine_waiting_for_a_tween_goes_on_when_the_tween_is_cancelled_or_completed()
    {
        var tweener = new Tweener();
        var runner = new CoroutineRunner();
        Tween? tween = null;
        int reached = 0;
        IEnumerator Routine()
        {
            tween = tweener.Add(10f, _ => { });
            yield return tween;
            reached++;
            tween = tweener.Add(10f, _ => { });
            yield return tween;
            reached++;
        }
        runner.Start(Routine());
        void Tick() { tweener.Update(Q); runner.Update(Q); }
        Tick();
        Tick();
        Assert.AreEqual(0, reached);
        tween!.Cancel();
        Tick();
        Assert.AreEqual(1, reached);
        tween!.Complete();
        Tick();
        Assert.AreEqual(2, reached);
    }

    [TestMethod]
    public void A_coroutine_waits_a_number_of_updates()
    {
        var runner = new CoroutineRunner();
        var log = new List<int>();
        int update = 0;
        IEnumerator Routine()
        {
            log.Add(update);
            yield return Wait.Steps(3);
            log.Add(update);
            yield return Wait.Steps(1);
            log.Add(update);
            yield return Wait.Steps(0);
            log.Add(update);
        }
        runner.Start(Routine());
        for (update = 1; update <= 8; update++) runner.Update(Q);
        CollectionAssert.AreEqual(new[] { 1, 4, 5, 5 }, log);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Wait.Steps(-1));
    }

    [TestMethod]
    public void In_a_scene_tweens_advance_before_coroutines_wherever_they_were_added()
    {
        float fromCoroutine = -1, fromUpdate = -1, fromTimer = -1;
        var seen = new List<string>();
        IEnumerator Routine(Stage s)
        {
            yield return s.Tweens.Add(2 * Step, t => fromCoroutine = t, Ease.Linear);
            seen.Add($"resumed in update {s.Updates + 1}");
        }
        var scene = new Stage
        {
            OnEnter = s =>
            {
                s.Coroutines.Start(Routine(s));
                s.Timers.AfterSteps(1, () => s.Tweens.Add(2 * Step, t => fromTimer = t, Ease.Linear));
            },
            OnUpdate = s =>
            {
                if (s.Updates == 1) s.Tweens.Add(2 * Step, t => fromUpdate = t, Ease.Linear);
                seen.Add($"{s.Updates}: {Percent(fromCoroutine)} {Percent(fromTimer)} {Percent(fromUpdate)}");
            },
        };
        var game = Start(scene);
        for (int i = 0; i < 4; i++) game.StepForTest();
        // Added in update 1 by a coroutine, a timer and the scene's Update: each shows its starting value in that
        // update and the same value in every later one. The coroutine goes on in the update the tween ends.
        CollectionAssert.AreEqual(new[] { "1: 0 0 0", "2: 50 50 50", "resumed in update 3", "3: 100 100 100", "4: 100 100 100" }, seen);
    }

    [TestMethod]
    public void In_a_scene_a_scaled_tween_freezes_with_the_worlds_clock_and_the_others_keep_the_engines()
    {
        float scaled = -1, unscaled = -1;
        var scene = new Stage
        {
            OnEnter = s =>
            {
                s.Objects.Clock.Scale = 0;                       // the world stands still for three updates
                s.Tweens.Add(2 * Step, t => scaled = t, Ease.Linear, scaled: true);
                s.Tweens.Add(2 * Step, t => unscaled = t, Ease.Linear);
            },
            OnUpdate = s => { if (s.Updates == 3) s.Objects.Clock.Scale = 1; },
        };
        var game = Start(scene);
        for (int i = 0; i < 3; i++) game.StepForTest();
        Assert.AreEqual(1f, unscaled);
        Assert.AreEqual(0f, scaled, "Held while the world stands still.");
        for (int i = 0; i < 2; i++) game.StepForTest();
        Assert.AreEqual(1f, scaled);
    }

    /// <summary>Trail Duel's tween, as its battle scene writes it: an ease loop a cutscene waits for.</summary>
    private static IEnumerator HandWrittenTween(float seconds, Action<float> apply, Func<float, float>? ease, float engineStep)
    {
        ease ??= Ease.OutQuad;
        for (float t = 0; t < seconds; t += engineStep)
        {
            apply(ease(Math.Clamp(t / seconds, 0, 1)));
            yield return null;
        }
        apply(1);
    }

    /// <summary>The lengths and easings of the 24 tweens of Trail Duel's battle, and the fades of Courtyard Key.</summary>
    private static readonly (float Seconds, Func<float, float>? Ease)[] DemoTweens =
    [
        (0.09f, Ease.InQuad), (0.12f, null), (0.12f, null), (0.15f, null), (0.16f, null), (0.22f, null), (0.25f, Ease.InQuad),
        (0.25f, Ease.OutBack), (0.28f, Ease.OutBack), (0.2f, null), (0.2f, null), (0.35f, null), (0.35f, Ease.Linear),
        (0.3f, null), (0.3f, null), (0.3f, Ease.InQuad), (0.4f, null), (0.4f, null), (0.4f, Ease.InQuad), (0.4f, Ease.Linear),
        (0.5f, null), (0.5f, null), (0.9f, Ease.OutCubic), (0.6f, Ease.Linear),
        (0.45f, Ease.Linear), (1.2f, Ease.Linear), (2f, Ease.Linear),
    ];

    [TestMethod]
    public void A_yielded_tween_replaces_trail_duels_ease_loop_value_for_value_on_every_update()
    {
        foreach (int rate in new[] { 60, 50, 144 })
        {
            float step = (float)(1.0 / rate);
            foreach (var (seconds, ease) in DemoTweens)
            {
                // The cutscene as the game writes it, and as one line on the engine's tween; then a marker for the
                // update each goes on in.
                var byHand = new List<string>();
                var byEngine = new List<string>();
                int update = 0;
                var handRunner = new CoroutineRunner();
                var engineRunner = new CoroutineRunner();
                var tweener = new Tweener();
                IEnumerator Hand()
                {
                    yield return Wait.Steps(2);
                    yield return HandWrittenTween(seconds, t => byHand.Add($"{update}:{BitConverter.SingleToInt32Bits(t):x8}"), ease, step);
                    byHand.Add($"{update}:on");
                }
                IEnumerator Engine()
                {
                    yield return Wait.Steps(2);
                    yield return tweener.Add(seconds, t => byEngine.Add($"{update}:{BitConverter.SingleToInt32Bits(t):x8}"), ease);
                    byEngine.Add($"{update}:on");
                }
                handRunner.Start(Hand());
                engineRunner.Start(Engine());
                for (update = 1; update <= rate * 3; update++)
                {
                    handRunner.Update(step);
                    tweener.Update(step);
                    engineRunner.Update(step);
                }
                Assert.AreEqual(0, engineRunner.Count);
                Assert.IsTrue(byHand.Count > 3);
                CollectionAssert.AreEqual(byHand, byEngine, $"{seconds} s at {rate} updates a second");
            }
        }
    }
}
