using System.Globalization;
using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>
/// The time of a state machine as a rule a developer can predict: a way out by seconds is taken on the update on which
/// the time in the state reaches them (N seconds at R updates a second: N x R updates, the next whole number when that
/// is not one), a way out by steps counts updates, and the time and the steps already spent can be set.
/// </summary>
[TestClass]
public sealed class MachineTimeTests
{
    private enum S { Wait, Done, Other }

    /// <summary>Update rates a game runs at.</summary>
    internal static readonly int[] Rates = [30, 50, 60, 120, 144];

    /// <summary>Durations a game uses, from a blink to an hour.</summary>
    internal static readonly string[] Durations =
    [
        "0.02", "0.05", "0.08", "0.1", "0.12", "0.15", "0.16", "0.18", "0.2", "0.25", "0.28", "0.3", "0.35", "0.4", "0.45",
        "0.5", "0.6", "0.7", "0.75", "0.8", "0.9", "1", "1.1", "1.2", "1.3", "1.4", "1.5", "1.6", "1.8", "1.9", "2", "2.2",
        "2.4", "2.6", "2.8", "3.2", "3.4", "4", "4.2", "5.5", "10", "60", "83", "120", "300", "600", "3600",
    ];

    /// <summary>The rule: the first whole number of updates whose time is at least the duration.</summary>
    internal static int UpdatesFor(string seconds, int rate) =>
        (int)Math.Ceiling(decimal.Parse(seconds, CultureInfo.InvariantCulture) * rate);

    internal static float Seconds(string seconds) => float.Parse(seconds, CultureInfo.InvariantCulture);

    /// <summary>Steps <paramref name="machine"/> until it leaves <see cref="S.Wait"/>; the number of the update it left on.</summary>
    private static int UpdatesUntilDone(StateMachine<S> machine, float dt, int limit)
    {
        for (int update = 1; update <= limit; update++)
        {
            machine.Update(dt);
            if (machine.Current == S.Done) return update;
        }
        return -1;
    }

    [TestMethod]
    public void A_time_in_seconds_ends_after_exactly_seconds_times_rate_updates_at_every_rate()
    {
        var wrong = new List<string>();
        foreach (int rate in Rates)
        {
            float dt = (float)(1.0 / rate);
            foreach (string seconds in Durations)
            {
                var machine = new StateMachine<S>(S.Wait);
                machine.State(S.Wait).After(Seconds(seconds), S.Done);
                int expected = UpdatesFor(seconds, rate);
                int updates = UpdatesUntilDone(machine, dt, expected + 10);
                if (updates != expected) wrong.Add($"{seconds} s at {rate}: {updates}, expected {expected}");
            }
        }
        Assert.AreEqual("", string.Join("; ", wrong));
    }

    [TestMethod]
    public void The_rule_holds_for_a_state_entered_late_in_a_long_run()
    {
        // The clock is far from zero: the time in a state is still measured exactly.
        var machine = new StateMachine<S>(S.Other);
        machine.State(S.Other).When(() => false, S.Other);
        machine.State(S.Wait).After(1.9f, S.Done);
        const float dt = 1f / 60f;
        for (int i = 0; i < 60 * 60 * 30; i++) machine.Update(dt);   // half an hour
        machine.Go(S.Wait);
        Assert.AreEqual(114, UpdatesUntilDone(machine, dt, 200));
    }

    [TestMethod]
    public void A_time_in_steps_ends_after_that_many_updates_whatever_the_step()
    {
        foreach (float dt in new[] { 1f / 30f, 1f / 60f, 1f / 144f, 0.001f, 0.25f })
        {
            var machine = new StateMachine<S>(S.Wait);
            machine.State(S.Wait).AfterSteps(115, S.Done);
            Assert.AreEqual(115, UpdatesUntilDone(machine, dt, 200), $"step {dt}");
        }
    }

    [TestMethod]
    public void Steps_count_the_updates_in_the_state_and_an_update_of_zero_seconds_is_not_one()
    {
        var machine = new StateMachine<S>(S.Wait);
        machine.State(S.Wait).AfterSteps(3, S.Done);
        machine.State(S.Done).AfterSteps(0, S.Other);
        Assert.AreEqual(0, machine.Steps);
        machine.Update(1f / 60f);
        machine.Update(1f / 60f);
        Assert.AreEqual(2, machine.Steps);
        Assert.AreEqual(2, machine.StepsIn(S.Wait));
        Assert.AreEqual(0, machine.StepsIn(S.Done));
        machine.Update(0);                                 // a frozen world: no time, no step
        machine.Update(0);
        Assert.AreEqual(2, machine.Steps);
        Assert.AreEqual(S.Wait, machine.Current);
        machine.Update(1f / 60f);
        Assert.AreEqual(S.Done, machine.Current);
        Assert.AreEqual(0, machine.Steps, "A state starts at zero steps.");
        machine.Update(1f / 60f);
        Assert.AreEqual(S.Other, machine.Current, "Zero steps leaves at the first update in the state, as zero seconds does.");
    }

    [TestMethod]
    public void A_time_in_steps_takes_a_condition_and_a_parents_steps_are_its_own()
    {
        bool open = false;
        var machine = new StateMachine<S>(S.Wait);
        machine.State(S.Wait).AfterSteps(2, S.Done, () => open);
        for (int i = 0; i < 5; i++) machine.Update(1f / 60f);
        Assert.AreEqual(S.Wait, machine.Current);
        open = true;
        machine.Update(1f / 60f);
        Assert.AreEqual(S.Done, machine.Current);

        var nested = new StateMachine<S>(S.Wait);
        nested.State(S.Other).AfterSteps(4, S.Done);
        nested.State(S.Wait).SubstateOf(S.Other).AfterSteps(3, S.Wait);   // re-entered at the third update; the parent stays
        Assert.AreEqual(4, UpdatesUntilDone(nested, 1f / 60f, 10));
    }

    [TestMethod]
    public void A_negative_number_of_steps_is_refused()
    {
        var machine = new StateMachine<S>(S.Wait);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => machine.State(S.Wait).AfterSteps(-1, S.Done));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => machine.SetSteps(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => machine.SetTime(-0.1f));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => machine.SetTime(float.NaN));
    }

    [TestMethod]
    public void The_time_in_a_state_can_be_set_for_a_head_start_or_to_hold_it()
    {
        const float dt = 1f / 60f;
        var machine = new StateMachine<S>(S.Wait);
        machine.State(S.Wait).After(2f, S.Done);
        machine.Start();
        machine.SetTime(1.5f);                             // entered with a head start: half a second is left
        Assert.AreEqual(1.5f, machine.Time);
        Assert.AreEqual(30, UpdatesUntilDone(machine, dt, 100));

        // Held at half a second while something counts, then let go: Meadow Run's tally.
        var tally = new StateMachine<S>(S.Wait);
        int left = 40;
        tally.State(S.Wait)
            .OnUpdate(_ => { if (tally.Time >= 0.5f && left > 0) { left--; tally.SetTime(0.5f); } })
            .After(2f, S.Done);
        int updates = UpdatesUntilDone(tally, dt, 400);
        // 30 updates reach half a second and count the first; 39 more count the rest; 90 more reach two seconds.
        Assert.AreEqual(30 + 39 + 90, updates);
    }

    [TestMethod]
    public void Setting_the_time_moves_only_the_current_state_and_setting_the_steps_likewise()
    {
        var machine = new StateMachine<S>(S.Wait);
        machine.State(S.Wait).SubstateOf(S.Other);
        for (int i = 0; i < 6; i++) machine.Update(0.5f);
        machine.SetTime(0.25f);
        machine.SetSteps(40);
        Assert.AreEqual(0.25f, machine.TimeIn(S.Wait));
        Assert.AreEqual(3f, machine.TimeIn(S.Other));
        Assert.AreEqual(40, machine.Steps);
        Assert.AreEqual(6, machine.StepsIn(S.Other));
    }

    [TestMethod]
    public void A_snapshot_keeps_the_time_and_the_steps_and_a_restored_machine_leaves_on_the_same_update()
    {
        const float dt = 1f / 60f;
        StateMachine<S> Build()
        {
            var m = new StateMachine<S>(S.Other);
            m.State(S.Other).AfterSteps(7, S.Wait);
            m.State(S.Wait).After(1.9f, S.Done);
            return m;
        }

        var played = Build();
        for (int i = 0; i < 7 + 50; i++) played.Update(dt);
        var saved = played.Snapshot();
        Assert.AreEqual(S.Wait, saved.State);
        Assert.AreEqual(50, saved.Steps);
        int rest = UpdatesUntilDone(played, dt, 200);
        Assert.AreEqual(114 - 50, rest);

        var loaded = Build();
        loaded.Restore(saved);
        Assert.AreEqual(50, loaded.Steps);
        Assert.AreEqual(saved.Time, loaded.Time);
        Assert.AreEqual(rest, UpdatesUntilDone(loaded, dt, 200), "Restored mid-state, the machine leaves on the update the played one did.");

        // A way out by steps, restored mid-state.
        var early = Build();
        for (int i = 0; i < 4; i++) early.Update(dt);
        var again = Build();
        again.Restore(early.Snapshot());
        for (int i = 0; i < 2; i++) again.Update(dt);
        Assert.AreEqual(S.Other, again.Current);
        again.Update(dt);
        Assert.AreEqual(S.Wait, again.Current);
    }

    [TestMethod]
    public void The_steps_in_a_state_are_part_of_the_state_hash()
    {
        static ulong Hash(StateMachine<S> machine)
        {
            var hash = new StateHasher();
            machine.HashState(hash);
            return hash.Value;
        }
        var a = new StateMachine<S>(S.Wait);
        var b = new StateMachine<S>(S.Wait);
        a.Start();
        b.Start();
        Assert.AreEqual(Hash(a), Hash(b));
        a.SetSteps(3);
        Assert.AreNotEqual(Hash(a), Hash(b));
        b.SetSteps(3);
        Assert.AreEqual(Hash(a), Hash(b));
    }

    /// <summary>
    /// What a game that keeps its own timer does (<c>t += dt</c> in single precision, leaving when <c>t &gt; seconds</c>):
    /// the number of the update it leaves on at 60 updates a second.
    /// </summary>
    internal static int UpdatesOfAFloatTimer(float seconds, float dt = 1f / 60f)
    {
        float t = 0;
        for (int update = 1; ; update++)
        {
            t += dt;
            if (t > seconds) return update;
        }
    }

    /// <summary>
    /// The thresholds the demo games keep by hand and the update each is crossed on at 60 updates a second, as their
    /// records give them: Meadow Run's stage phases, Neon Rift's stage and bosses, Courtyard Key's hero and Warden.
    /// </summary>
    internal static readonly (string Game, float Seconds, int Update)[] RecordedThresholds =
    [
        ("Meadow Run", 1.9f, 115), ("Meadow Run", 2.0f, 121), ("Meadow Run", 2.4f, 145), ("Meadow Run", 4.2f, 253),
        ("Meadow Run", 0.3f, 18), ("Meadow Run", 0.5f, 30), ("Meadow Run", 0.6f, 36),
        ("Neon Rift", 2.6f, 157), ("Neon Rift", 1.3f, 79), ("Neon Rift", 2.4f, 145), ("Neon Rift", 3.2f, 193),
        ("Neon Rift", 1.0f, 61), ("Neon Rift", 2.8f, 169), ("Neon Rift", 4f, 241), ("Neon Rift", 2.2f, 133),
        ("Neon Rift", 5.5f, 330), ("Neon Rift", 0.35f, 21), ("Neon Rift", 0.7f, 43),
        ("Courtyard Key", 0.2f, 13), ("Courtyard Key", 0.7f, 43), ("Courtyard Key", 0.75f, 46), ("Courtyard Key", 0.9f, 55),
        ("Courtyard Key", 1.4f, 85), ("Courtyard Key", 1.6f, 97), ("Courtyard Key", 1.8f, 109),
    ];

    [TestMethod]
    public void The_demos_hand_kept_timers_move_onto_a_machine_in_steps_without_changing_an_update()
    {
        const float dt = 1f / 60f;
        foreach (var (game, seconds, recorded) in RecordedThresholds)
        {
            Assert.AreEqual(recorded, UpdatesOfAFloatTimer(seconds), $"{game}: the hand-kept timer of {seconds} s, as recorded");

            var bySteps = new StateMachine<S>(S.Wait);
            bySteps.State(S.Wait).AfterSteps(recorded, S.Done);
            Assert.AreEqual(recorded, UpdatesUntilDone(bySteps, dt, recorded + 5), $"{game}: {seconds} s kept as {recorded} steps");
        }
    }

    [TestMethod]
    public void In_seconds_meadow_runs_phases_are_the_regular_lengths_one_update_shorter_than_its_float_timer()
    {
        const float dt = 1f / 60f;
        (float Seconds, int Machine, int Game)[] phases = [(1.9f, 114, 115), (2.0f, 120, 121), (2.4f, 144, 145), (4.2f, 252, 253), (0.3f, 18, 18), (0.5f, 30, 30), (0.6f, 36, 36)];
        foreach (var (seconds, expected, game) in phases)
        {
            var machine = new StateMachine<S>(S.Wait);
            machine.State(S.Wait).After(seconds, S.Done);
            Assert.AreEqual(expected, UpdatesUntilDone(machine, dt, 300), $"{seconds} s on the machine");
            Assert.AreEqual(game, UpdatesOfAFloatTimer(seconds), $"{seconds} s on the game's timer");
        }
    }
}
