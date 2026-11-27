using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>
/// <see cref="Timers"/> on their own: owned and stepped by a plain object, counted in seconds by a rule that can be
/// worked out, or in updates, and part of the owner's state hash.
/// </summary>
[TestClass]
public sealed class TimersTests
{
    private const float Step = 1f / 60f;

    private static ulong Hash(Timers timers)
    {
        var hash = new StateHasher();
        timers.HashState(hash);
        return hash.Value;
    }

    /// <summary>The rules of a game kept in a plain class, as a test steps them: no scene, no game.</summary>
    private sealed class Stage : IStateHashed
    {
        private readonly Timers _timers = new();
        public readonly List<string> Log = [];
        public int Updates;

        public void BossDefeated()
        {
            _timers.After(2.6f, () => Log.Add($"explosion {Updates}"));
            _timers.After(3.4f, () => Log.Add($"fanfare {Updates}"));
        }

        public void Update(float dt)
        {
            Updates++;
            _timers.Update(dt);
        }

        public void HashState(StateHasher hash) => hash.Add(_timers);
    }

    [TestMethod]
    public void A_plain_object_owns_and_steps_its_timers_without_a_scene()
    {
        var stage = new Stage();
        for (int i = 0; i < 100; i++) stage.Update(Step);
        stage.BossDefeated();
        for (int i = 0; i < 300; i++) stage.Update(Step);
        // 2.6 s and 3.4 s at 60 updates a second: 156 and 204 updates after the one they were added on.
        CollectionAssert.AreEqual(new[] { "explosion 256", "fanfare 304" }, stage.Log);
    }

    [TestMethod]
    public void A_delay_in_seconds_ends_after_exactly_seconds_times_rate_updates_at_every_rate()
    {
        var wrong = new List<string>();
        foreach (int rate in MachineTimeTests.Rates)
        {
            float dt = (float)(1.0 / rate);
            foreach (string seconds in MachineTimeTests.Durations)
            {
                var timers = new Timers();
                int fired = 0, update = 0;
                timers.After(MachineTimeTests.Seconds(seconds), () => fired = update);
                int expected = MachineTimeTests.UpdatesFor(seconds, rate);
                for (update = 1; update <= expected + 10 && fired == 0; update++) timers.Update(dt);
                if (fired != expected) wrong.Add($"{seconds} s at {rate}: {fired}, expected {expected}");
            }
        }
        Assert.AreEqual("", string.Join("; ", wrong));
    }

    [TestMethod]
    public void A_repeating_timer_keeps_its_cadence_for_as_long_as_it_runs()
    {
        var wrong = new List<string>();
        foreach (var (rate, seconds, every) in new[] { (60, 0.1f, 6), (60, 0.5f, 30), (144, 0.25f, 36), (50, 0.18f, 9), (120, 1.9f, 228) })
        {
            float dt = (float)(1.0 / rate);
            var timers = new Timers();
            int calls = 0, update = 0;
            timers.Every(seconds, () =>
            {
                calls++;
                if (update != calls * every && wrong.Count < 5) wrong.Add($"{seconds} s at {rate}: call {calls} on update {update}");
            });
            for (update = 1; update <= rate * 600; update++) timers.Update(dt);   // ten minutes
            Assert.AreEqual(rate * 600 / every, calls, $"{seconds} s at {rate}");
        }
        Assert.AreEqual("", string.Join("; ", wrong));
    }

    [TestMethod]
    public void An_interval_that_is_not_a_whole_number_of_updates_keeps_what_is_left_over()
    {
        // 0.04 s at 60 updates a second is 2.4 updates: calls on the updates that first reach 0.04, 0.08, 0.12...
        var timers = new Timers();
        var on = new List<int>();
        int update = 0;
        timers.Every(0.04f, () => on.Add(update), times: 6);
        for (update = 1; update <= 20; update++) timers.Update(Step);
        CollectionAssert.AreEqual(new[] { 3, 5, 8, 10, 12, 15 }, on);
        Assert.AreEqual(0, timers.Count);
    }

    [TestMethod]
    public void A_delay_in_steps_counts_updates_whatever_their_length_and_not_those_of_zero_seconds()
    {
        foreach (float dt in new[] { 1f / 30f, 1f / 60f, 0.001f, 0.4f })
        {
            var timers = new Timers();
            int fired = 0, update = 0;
            timers.AfterSteps(157, () => fired = update);
            for (update = 1; update <= 200; update++) timers.Update(dt);
            Assert.AreEqual(157, fired, $"step {dt}");
        }

        var frozen = new Timers();
        var log = new List<int>();
        int now = 0;
        var handle = frozen.EverySteps(3, () => log.Add(now), times: 2);
        Assert.AreEqual(3, frozen.Remaining(handle));
        for (now = 1; now <= 2; now++) frozen.Update(Step);
        for (now = 3; now <= 6; now++) frozen.Update(0);          // frozen: no update counts
        Assert.AreEqual(1, frozen.Remaining(handle));
        for (now = 7; now <= 12; now++) frozen.Update(Step);
        CollectionAssert.AreEqual(new[] { 7, 10 }, log);

        var next = new Timers();
        int zero = 0, one = 0;
        next.AfterSteps(0, () => zero++);
        next.AfterSteps(1, () => one++);
        next.Update(Step);
        Assert.AreEqual((1, 1), (zero, one), "Zero and one step are both the next update.");
    }

    [TestMethod]
    public void Timers_are_called_in_the_order_added_and_one_added_by_an_action_waits_for_the_next_update()
    {
        var timers = new Timers();
        var log = new List<string>();
        timers.After(0.05f, () => log.Add("a"));
        timers.AfterSteps(3, () => { log.Add("b"); timers.After(0, () => log.Add("late")); });
        timers.After(0.05f, () => log.Add("c"));
        for (int i = 0; i < 3; i++) timers.Update(Step);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, log);
        Assert.AreEqual(1, timers.Count);
        timers.Update(Step);
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "late" }, log);
    }

    [TestMethod]
    public void A_timer_can_be_cancelled_asked_for_what_is_left_and_may_cancel_itself()
    {
        var timers = new Timers();
        int calls = 0;
        var once = timers.After(1f, () => calls++);
        TimerHandle self = default;
        int selfCalls = 0;
        self = timers.Every(0.1f, () => { selfCalls++; timers.Cancel(self); });
        Assert.AreEqual(2, timers.Count);
        Assert.IsTrue(timers.IsActive(once));
        Assert.AreEqual(1f, timers.Remaining(once));
        for (int i = 0; i < 30; i++) timers.Update(Step);
        Assert.AreEqual(0.5f, timers.Remaining(once), 1e-4f);
        Assert.AreEqual(1, selfCalls);
        Assert.IsFalse(timers.IsActive(self));
        Assert.IsTrue(timers.Cancel(once));
        Assert.IsFalse(timers.Cancel(once));
        Assert.IsFalse(timers.IsActive(once));
        Assert.AreEqual(0f, timers.Remaining(once));
        Assert.IsFalse(timers.Cancel(TimerHandle.None));
        for (int i = 0; i < 60; i++) timers.Update(Step);
        Assert.AreEqual(0, calls);

        timers.After(1f, () => calls++);
        timers.Clear();
        Assert.AreEqual(0, timers.Count);
        for (int i = 0; i < 90; i++) timers.Update(Step);
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public void A_step_longer_than_the_interval_calls_once_per_interval_that_passed()
    {
        var timers = new Timers();
        int calls = 0;
        timers.Every(0.1f, () => calls++);
        timers.Update(0.35f);
        Assert.AreEqual(3, calls);
    }

    [TestMethod]
    public void An_unscaled_timer_counts_the_second_clock()
    {
        var timers = new Timers();
        var log = new List<string>();
        timers.After(0.1f, () => log.Add("world"));
        timers.After(0.1f, () => log.Add("engine"), unscaled: true);
        timers.AfterSteps(2, () => log.Add("world steps"));
        timers.AfterSteps(2, () => log.Add("engine steps"), unscaled: true);
        for (int i = 0; i < 10; i++) timers.Update(0, Step);       // the world is frozen
        CollectionAssert.AreEqual(new[] { "engine steps", "engine" }, log);
        for (int i = 0; i < 6; i++) timers.Update(Step, Step);
        CollectionAssert.AreEqual(new[] { "engine steps", "engine", "world steps", "world" }, log);
    }

    [TestMethod]
    public void Arguments_are_checked()
    {
        var timers = new Timers();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.After(-1f, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.After(float.NaN, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.Every(0f, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.Every(1f, () => { }, times: -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.AfterSteps(-1, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.EverySteps(0, () => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timers.EverySteps(2, () => { }, times: -1));
        Assert.ThrowsExactly<ArgumentNullException>(() => timers.After(1f, null!));
    }

    [TestMethod]
    public void The_waiting_timers_are_part_of_the_owners_state_hash()
    {
        var a = new Stage();
        var b = new Stage();
        var hash = new StateHasher();
        ulong Of(Stage stage) { hash.Reset(); stage.HashState(hash); return hash.Value; }
        Assert.AreEqual(Of(a), Of(b));
        a.BossDefeated();
        Assert.AreNotEqual(Of(a), Of(b));
        b.BossDefeated();
        Assert.AreEqual(Of(a), Of(b));
        a.Update(Step);
        Assert.AreNotEqual(Of(a), Of(b), "What is left of a timer is state.");
        b.Update(Step);
        Assert.AreEqual(Of(a), Of(b));

        var seconds = new Timers();
        var steps = new Timers();
        seconds.After(3, () => { });
        steps.AfterSteps(3, () => { });
        Assert.AreNotEqual(Hash(seconds), Hash(steps), "Three seconds and three updates are different timers.");
    }

    /// <summary>
    /// Neon Rift's scheduler, as its stage keeps it: a list of absolute times in a <c>float</c> clock, called on the
    /// first update whose clock is not under them.
    /// </summary>
    private sealed class HandKeptSchedule
    {
        private readonly List<(float At, Action Action)> _scheduled = [];
        private float _time;

        public void Schedule(float delay, Action action) => _scheduled.Add((_time + delay, action));

        public void Update(float dt)
        {
            _time += dt;
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                if (_scheduled[i].At > _time) continue;
                var action = _scheduled[i].Action;
                _scheduled.RemoveAt(i);
                action();
            }
        }
    }

    /// <summary>The delays Neon Rift schedules: the arena's alarms, a boss's end, and the volleys of its three bosses.</summary>
    private static readonly float[] NeonRiftDelays =
    [
        0.9f, 1.8f, 2.6f, 3.4f, 0.2f,
        0 * 0.12f, 1 * 0.12f, 2 * 0.12f, 3 * 0.12f, 4 * 0.12f, 5 * 0.12f,
        1 * 0.09f, 2 * 0.09f, 3 * 0.09f, 4 * 0.09f, 5 * 0.09f, 6 * 0.09f, 7 * 0.09f, 8 * 0.09f, 9 * 0.09f, 10 * 0.09f,
    ];

    [TestMethod]
    public void Neon_rifts_schedule_moves_onto_timers_in_steps_without_changing_an_update()
    {
        int earlier = 0, cases = 0;
        foreach (int startedAt in new[] { 0, 1, 137, 1000, 4321, 20000 })
        {
            foreach (float delay in NeonRiftDelays)
            {
                // The game's own update for this delay, scheduled when its clock has run `startedAt` updates.
                var game = new HandKeptSchedule();
                for (int i = 0; i < startedAt; i++) game.Update(Step);
                int gameUpdate = 0, update = 0;
                game.Schedule(delay, () => gameUpdate = update);
                for (update = 1; gameUpdate == 0; update++) game.Update(Step);

                // The same delay counted in steps: the same update, wherever the clock is.
                var bySteps = new Timers();
                int stepsUpdate = 0;
                bySteps.AfterSteps(gameUpdate, () => stepsUpdate = update);
                for (update = 1; update <= gameUpdate + 2; update++) bySteps.Update(Step);
                Assert.AreEqual(gameUpdate, stepsUpdate, $"{delay} s scheduled at update {startedAt}");

                // In seconds the timer follows the rule, which the game's absolute float clock does not always.
                var bySeconds = new Timers();
                int secondsUpdate = 0;
                bySeconds.After(delay, () => secondsUpdate = update);
                for (update = 1; update <= gameUpdate + 2; update++) bySeconds.Update(Step);
                int regular = Math.Max(1, (int)Math.Ceiling((decimal)delay * 60 - 0.001m));
                Assert.AreEqual(regular, secondsUpdate, $"{delay} s in seconds");
                Assert.IsTrue(gameUpdate - secondsUpdate is 0 or 1 or -1, $"{delay} s at {startedAt}: game {gameUpdate}, timer {secondsUpdate}");
                cases++;
                if (secondsUpdate != gameUpdate) earlier++;
            }
        }
        // The game's update depends on where its clock is; the timer's does not.
        Assert.IsTrue(earlier > 0 && earlier < cases, $"{earlier} of {cases}");
    }
}
