using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.Testing;

namespace MicroEngine.Tests.Kernel;

[TestClass]
public sealed class StateMachineTests
{
    private enum S { Idle, Run, Jump, Attack, Stunned, Dead, Alive, Grounded }
    private enum E { Hit, Kill, Jump, Ignore }

    private const float Step = 1f / 60f;

    private static StateMachine<S, E> Logged(List<string> log, S initial = S.Idle, params S[] states)
    {
        var m = new StateMachine<S, E>(initial);
        foreach (var s in states.Length == 0 ? [S.Idle, S.Run, S.Jump, S.Attack, S.Stunned, S.Dead] : states)
        {
            m.State(s)
                .OnEnter(() => log.Add($"enter {s}"))
                .OnUpdate(_ => log.Add($"update {s}"))
                .OnExit(() => log.Add($"exit {s}"));
        }
        return m;
    }

    [TestMethod]
    public void The_initial_state_is_entered_at_the_first_step_and_updated_in_it()
    {
        var log = new List<string>();
        var m = Logged(log);
        Assert.AreEqual(S.Idle, m.Current);
        Assert.IsFalse(m.IsStarted);
        Assert.AreEqual(0, log.Count);
        m.Update(Step);
        CollectionAssert.AreEqual(new[] { "enter Idle", "update Idle" }, log);
        Assert.AreEqual(Step, m.Time, 1e-6f);
    }

    [TestMethod]
    public void A_condition_changes_the_state_after_the_update_and_the_new_state_updates_next_step()
    {
        var log = new List<string>();
        var m = Logged(log);
        bool moving = false;
        m.State(S.Idle).When(() => moving, S.Run);
        m.Changed += (from, to) => log.Add($"changed {from}>{to}");
        m.Update(Step);
        moving = true;
        m.Update(Step);
        m.Update(Step);
        CollectionAssert.AreEqual(new[]
        {
            "enter Idle", "update Idle",
            "update Idle", "exit Idle", "enter Run", "changed Idle>Run",
            "update Run",
        }, log);
        Assert.AreEqual(S.Idle, m.Previous);
        Assert.AreEqual(Step, m.Time, 1e-6f, "the time restarts at the change");
        Assert.AreEqual(1, m.Changes);
    }

    [TestMethod]
    public void The_first_way_out_declared_wins_and_only_one_is_taken_per_step()
    {
        var m = new StateMachine<S, E>(S.Idle);
        m.State(S.Idle).When(() => true, S.Attack).When(() => true, S.Run);
        m.State(S.Attack).When(() => true, S.Jump);
        m.Update(Step);
        Assert.AreEqual(S.Attack, m.Current, "declared first, taken first");
        m.Update(Step);
        Assert.AreEqual(S.Jump, m.Current);
    }

    [TestMethod]
    public void A_timed_way_out_fires_on_the_step_that_reaches_its_time_and_may_carry_a_condition()
    {
        var m = new StateMachine<S, E>(S.Stunned);
        bool grounded = false;
        m.State(S.Stunned).After(0.5f, S.Idle);
        m.State(S.Idle).After(0.1f, S.Run, () => grounded);
        int steps = 0;
        while (m.Current == S.Stunned) { m.Update(Step); steps++; }
        Assert.AreEqual(30, steps, "30 steps of 1/60 are half a second");
        for (int i = 0; i < 30; i++) m.Update(Step);
        Assert.AreEqual(S.Idle, m.Current, "the time passed but the condition holds it");
        grounded = true;
        m.Update(Step);
        Assert.AreEqual(S.Run, m.Current);
    }

    [TestMethod]
    public void A_zero_step_moves_no_time()
    {
        var m = new StateMachine<S, E>(S.Stunned);
        m.State(S.Stunned).After(0.1f, S.Idle);
        for (int i = 0; i < 100; i++) m.Update(0);
        Assert.AreEqual(S.Stunned, m.Current, "a frozen clock keeps a timed state");
        Assert.AreEqual(0f, m.Time);
    }

    [TestMethod]
    public void Events_follow_guards_and_any_applies_after_the_state()
    {
        var log = new List<string>();
        var m = Logged(log);
        bool armoured = true;
        m.State(S.Idle).On(E.Hit, S.Stunned, () => !armoured);
        m.Any.On(E.Kill, S.Dead).On(E.Hit, S.Attack);
        Assert.IsTrue(m.Fire(E.Hit));
        Assert.AreEqual(S.Attack, m.Current, "the state's guard refused, Any took it");
        m.Go(S.Idle);
        armoured = false;
        m.Fire(E.Hit);
        Assert.AreEqual(S.Stunned, m.Current, "the state's own way out comes before Any's");
        m.Fire(E.Kill);
        Assert.AreEqual(S.Dead, m.Current);
    }

    [TestMethod]
    public void An_event_nothing_takes_is_reported_ignored_or_refused()
    {
        var m = new StateMachine<S, E>(S.Idle);
        var seen = new List<string>();
        m.Unhandled += (state, e) => seen.Add($"{state}:{e}");
        m.State(S.Idle).Do(E.Ignore, () => seen.Add("ignored on purpose"));
        Assert.IsFalse(m.Fire(E.Jump));
        Assert.IsTrue(m.Fire(E.Ignore));
        Assert.AreEqual(S.Idle, m.Current);
        CollectionAssert.AreEqual(new[] { "Idle:Jump", "ignored on purpose" }, seen);
        m.ThrowOnUnhandled = true;
        Assert.ThrowsExactly<InvalidOperationException>(() => m.Fire(E.Jump));
    }

    [TestMethod]
    public void A_parent_is_entered_and_left_only_at_its_border_and_its_ways_out_apply_to_its_parts()
    {
        var log = new List<string>();
        var m = Logged(log, S.Idle, S.Idle, S.Run, S.Dead, S.Alive);
        m.State(S.Idle).SubstateOf(S.Alive).On(E.Jump, S.Run);
        m.State(S.Run).SubstateOf(S.Alive);
        m.State(S.Alive).On(E.Kill, S.Dead);
        m.Update(Step);
        m.Fire(E.Jump);
        Assert.IsTrue(m.IsIn(S.Alive));
        Assert.IsTrue(m.IsIn(S.Run));
        Assert.IsFalse(m.IsIn(S.Idle));
        m.Update(Step);
        m.Fire(E.Kill);
        CollectionAssert.AreEqual(new[]
        {
            "enter Alive", "enter Idle", "update Alive", "update Idle",
            "exit Idle", "enter Run",
            "update Alive", "update Run",
            "exit Run", "exit Alive", "enter Dead",
        }, log);
        Assert.IsFalse(m.IsIn(S.Alive));
    }

    [TestMethod]
    public void A_parents_timed_way_out_counts_the_parents_time()
    {
        var m = new StateMachine<S, E>(S.Idle);
        m.State(S.Idle).SubstateOf(S.Alive).After(0.1f, S.Run);
        m.State(S.Run).SubstateOf(S.Alive).After(0.1f, S.Idle);
        m.State(S.Alive).After(1f, S.Dead);
        for (int i = 0; i < 59; i++) m.Update(Step);
        Assert.IsTrue(m.IsIn(S.Alive));
        Assert.IsTrue(m.TimeIn(S.Alive) > m.Time, "the parts changed many times; the parent never left");
        m.Update(Step);
        m.Update(Step);
        Assert.AreEqual(S.Dead, m.Current);
    }

    [TestMethod]
    public void Hierarchy_is_checked()
    {
        var m = new StateMachine<S, E>(S.Idle);
        m.State(S.Idle).SubstateOf(S.Alive);
        Assert.ThrowsExactly<InvalidOperationException>(() => m.State(S.Idle).SubstateOf(S.Grounded));
        Assert.ThrowsExactly<InvalidOperationException>(() => m.State(S.Alive).SubstateOf(S.Idle));
        Assert.ThrowsExactly<InvalidOperationException>(() => m.State(S.Run).SubstateOf(S.Run));
        Assert.ThrowsExactly<InvalidOperationException>(() => m.Any.OnEnter(() => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => m.State(S.Idle).After(-1, S.Run));
    }

    [TestMethod]
    public void A_change_asked_from_a_handler_waits_until_the_current_one_is_complete()
    {
        var log = new List<string>();
        var m = Logged(log);
        m.State(S.Run).OnEnter(() => { m.Fire(E.Hit); log.Add($"during enter: {m.Current}"); });
        m.State(S.Idle).OnExit(() => m.Go(S.Jump));
        m.Any.On(E.Hit, S.Stunned);
        m.Changed += (from, to) => log.Add($"changed {from}>{to}");
        m.Start();
        log.Clear();
        m.Go(S.Run);
        CollectionAssert.AreEqual(new[]
        {
            "exit Idle", "enter Run", "during enter: Run", "changed Idle>Run",
            "exit Run", "enter Jump", "changed Run>Jump",
            "exit Jump", "enter Stunned", "changed Jump>Stunned",
        }, log, "requests apply in the order they were made, each one whole");
    }

    [TestMethod]
    public void Going_to_the_current_state_leaves_and_enters_it_again_and_updating_from_a_handler_is_refused()
    {
        var log = new List<string>();
        var m = Logged(log);
        m.Update(0.25f);
        log.Clear();
        m.Go(S.Idle);
        CollectionAssert.AreEqual(new[] { "exit Idle", "enter Idle" }, log);
        Assert.AreEqual(0f, m.Time);
        m.State(S.Idle).OnUpdate(_ => m.Update(Step));
        Assert.ThrowsExactly<InvalidOperationException>(() => m.Update(Step));
        m.State(S.Run);
        m.Go(S.Run);
        Assert.AreEqual(S.Run, m.Current, "the machine still works after a handler threw");
    }

    [TestMethod]
    public void A_snapshot_restores_the_state_and_its_time_without_running_handlers()
    {
        var log = new List<string>();
        var m = Logged(log);
        m.State(S.Stunned).After(0.5f, S.Idle);
        m.Go(S.Stunned);
        for (int i = 0; i < 12; i++) m.Update(Step);
        var saved = m.Snapshot();
        var hashBefore = Hash(m);

        var loaded = Logged(log);
        loaded.State(S.Stunned).After(0.5f, S.Idle);
        log.Clear();
        loaded.Restore(saved);
        Assert.AreEqual(0, log.Count, "restoring is not entering");
        Assert.AreEqual(S.Stunned, loaded.Current);
        Assert.AreEqual(saved.Time, loaded.Time, 1e-6f);
        Assert.AreEqual(hashBefore, Hash(loaded));
        int steps = 0;
        while (loaded.Current == S.Stunned) { loaded.Update(Step); steps++; }
        Assert.AreEqual(18, steps, "it finishes the half second it had started");
    }

    private static ulong Hash(IStateHashed state)
    {
        var hash = new StateHasher();
        state.HashState(hash);
        return hash.Value;
    }

    [TestMethod]
    public void The_hash_follows_the_state_and_its_time()
    {
        var a = new StateMachine<S>(S.Idle);
        var b = new StateMachine<S>(S.Idle);
        a.State(S.Run);
        b.State(S.Run);
        a.Update(Step);
        b.Update(Step);
        Assert.AreEqual(Hash(a), Hash(b));
        a.Update(Step);
        Assert.AreNotEqual(Hash(a), Hash(b));
        b.Update(Step);
        a.Go(S.Run);
        Assert.AreNotEqual(Hash(a), Hash(b));
    }

    [TestMethod]
    public void Stepping_and_firing_allocate_nothing()
    {
        var m = new StateMachine<S, E>(S.Idle);
        int ticks = 0;
        bool flip = false;
        m.State(S.Idle).OnUpdate(_ => ticks++).When(() => flip, S.Run).On(E.Hit, S.Stunned);
        m.State(S.Run).SubstateOf(S.Alive).OnEnter(() => ticks++).When(() => !flip, S.Idle);
        m.State(S.Stunned).After(0f, S.Idle);
        m.Changed += (_, _) => ticks++;
        AllocationAssert.None(() =>
        {
            flip = !flip;
            m.Update(Step);
            m.Fire(E.Hit);
            m.Update(Step);
        }, because: "a machine per enemy is stepped every update");
    }

    private sealed class FlowScene : Scene
    {
        public StateMachine<S> Flow = null!;
        public readonly List<string> Log = [];
        public override void Enter()
        {
            Flow = Machines.Add(new StateMachine<S>(S.Idle));
            Flow.State(S.Idle).After(0.5f, S.Run);
            Flow.State(S.Run).OnEnter(() => Log.Add("run"));
        }
        public override void Update(float dt) => Log.Add(Flow.Current.ToString());
        public override void Draw(IRenderer renderer) { }
    }

    private sealed class TestGame(Scene first) : Game
    {
        public override GameSettings Settings { get; } = new("machines");
        protected override void Load() => Scenes.Push(first);
    }

    [TestMethod]
    public void A_scene_steps_and_hashes_the_machines_it_runs_and_they_follow_its_clock()
    {
        var scene = new FlowScene();
        var game = new TestGame(scene);
        game.InitializeForTest(new HeadlessPlatform());
        for (int i = 0; i < 29; i++) game.StepForTest();
        Assert.AreEqual(S.Idle, scene.Flow.Current);
        ulong before = game.ComputeStateHash().Hash;
        game.StepForTest();
        Assert.AreNotEqual(before, game.ComputeStateHash().Hash, "the machine's state is in the scene's hash");
        Assert.AreEqual("Run", scene.Log[^1], "the machine changed before the scene's update read it");
        Assert.AreEqual(1, scene.Machines.Count);
        scene.Objects.Clock.Freeze(1f);
        float time = scene.Flow.Time;
        for (int i = 0; i < 10; i++) game.StepForTest();
        Assert.AreEqual(time, scene.Flow.Time, "a frozen world holds the scene's machines");
    }
}
