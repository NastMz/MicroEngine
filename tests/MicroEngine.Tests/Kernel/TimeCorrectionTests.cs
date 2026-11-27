using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MicroEngine.Tests.Kernel;

/// <summary>
/// The edges of time: the rule for seconds over a day at any rate, steps that are not a time (zero, negative, not a
/// number, infinite), zero durations, callbacks that clear or end tweens, and handles of another owner.
/// </summary>
[TestClass]
public sealed class TimeCorrectionTests
{
    private enum S { Wait, Done, Other }

    private const float Step = 1f / 60f;
    private const float Q = 1f / 64f;

    private static readonly int[] HighRates = [60, 144, 240, 360, 1000];
    private static readonly int[] LongSeconds = [1800, 3600, 7200, 10800, 21600, 43200, 86400];

    // ---- D2: the rule for seconds, to a day and a thousand updates a second --------------------------------------

    [TestMethod]
    public void A_delay_of_hours_is_called_on_the_update_the_rule_gives_at_every_rate()
    {
        var wrong = new List<string>();
        foreach (int rate in HighRates)
        {
            float dt = (float)(1.0 / rate);
            var timers = new Timers();
            long update = 0;
            foreach (int seconds in LongSeconds)
            {
                long expected = (long)seconds * rate;
                timers.After(seconds, () => { if (update != expected) wrong.Add($"{seconds} s at {rate}: {update - expected:+0;-0}"); });
            }
            for (update = 1; update <= 86400L * rate + 3; update++) timers.Update(dt);
            Assert.AreEqual(0, timers.Count, $"every timer was called at {rate}");
        }
        Assert.AreEqual("", string.Join("; ", wrong));
    }

    [TestMethod]
    public void A_state_of_hours_is_left_on_the_update_the_rule_gives_at_every_rate()
    {
        var wrong = new List<string>();
        foreach (int rate in new[] { 60, 144, 1000 })
        {
            float dt = (float)(1.0 / rate);
            var machines = new List<(StateMachine<S> Machine, long Expected, int Seconds)>();
            foreach (int seconds in LongSeconds)
            {
                var machine = new StateMachine<S>(S.Wait);
                machine.State(S.Wait).After(seconds, S.Done);
                machines.Add((machine, (long)seconds * rate, seconds));
            }
            for (long update = 1; machines.Count > 0; update++)
            {
                for (int i = machines.Count - 1; i >= 0; i--)
                {
                    var (machine, expected, seconds) = machines[i];
                    machine.Update(dt);
                    if (machine.Current != S.Done && update <= expected + 3) continue;
                    if (update != expected) wrong.Add($"{seconds} s at {rate}: {update - expected:+0;-0}");
                    machines.RemoveAt(i);
                }
            }
        }
        Assert.AreEqual("", string.Join("; ", wrong));
    }

    [TestMethod]
    public void A_machine_restored_minutes_into_a_state_leaves_on_the_update_the_saved_one_does()
    {
        var wrong = new List<string>();
        foreach (int rate in new[] { 50, 60, 144, 1000 })
        {
            float dt = (float)(1.0 / rate);
            foreach (int savedAt in new[] { 1, 299, 300, 571, 599 })
            {
                var played = new StateMachine<S>(S.Wait);
                played.State(S.Wait).After(600f, S.Done);
                for (long i = 0; i < (long)savedAt * rate + 7; i++) played.Update(dt);
                var loaded = new StateMachine<S>(S.Wait);
                loaded.State(S.Wait).After(600f, S.Done);
                loaded.Restore(played.Snapshot());
                long expected = (long)(600 - savedAt) * rate - 7, update = 0;
                while (loaded.Current != S.Done && update < expected + 5) { loaded.Update(dt); update++; }
                if (update != expected) wrong.Add($"saved at {savedAt} s, {rate}: {update - expected:+0;-0}");
            }
        }
        Assert.AreEqual("", string.Join("; ", wrong));
    }

    [TestMethod]
    public void The_time_a_machine_reports_is_the_plain_sum_of_its_steps()
    {
        // What a game compares, draws and saves keeps its value: 420 steps of 1/60 as a float add up to a hair over 7.
        var machine = new StateMachine<S>(S.Wait);
        double sum = 0;
        for (int i = 0; i < 420; i++) { machine.Update(Step); sum += Step; }
        Assert.AreEqual((float)sum, machine.Time);
        Assert.IsTrue(machine.Time > 7f);
    }

    [TestMethod]
    public void A_timer_every_second_is_called_once_a_second_for_a_day()
    {
        foreach (int rate in new[] { 60, 144, 1000 })
        {
            float dt = (float)(1.0 / rate);
            var timers = new Timers();
            long update = 0, calls = 0, firstWrong = 0;
            timers.Every(1f, () =>
            {
                calls++;
                if (firstWrong == 0 && update != calls * rate) firstWrong = update;
            });
            for (update = 1; update <= 86400L * rate; update++) timers.Update(dt);
            Assert.AreEqual(0, firstWrong, $"at {rate}: the first call off its update");
            Assert.AreEqual(86400, calls, $"at {rate}");
        }
    }

    [TestMethod]
    public void The_rule_follows_slow_motion_and_a_step_that_is_no_whole_rate_is_summed()
    {
        // Half speed at 60 updates a second is the step of 120: a second takes 120 updates.
        var slow = new Timers();
        int fired = 0, update = 0;
        slow.After(1f, () => fired = update);
        for (update = 1; update <= 200; update++) slow.Update(Step * 0.5f);
        Assert.AreEqual(120, fired);

        // Steps that vary: the first update on which their sum reaches the time.
        var uneven = new Timers();
        fired = 0;
        uneven.After(1f, () => fired = update);
        float[] steps = [0.3f, 0.3f, 0.25f, 0.1f, 0.04f, 0.02f, 0.5f];
        for (update = 1; update <= steps.Length; update++) uneven.Update(steps[update - 1]);
        Assert.AreEqual(6, fired, "0.3 + 0.3 + 0.25 + 0.1 + 0.04 is 0.99; the sixth step passes 1.");
    }

    // ---- D5 and the machine: a step that is not a time ----------------------------------------------------------------

    [TestMethod]
    public void A_step_that_is_not_a_positive_finite_time_advances_nothing_in_timers()
    {
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f })
        {
            var timers = new Timers();
            int once = 0, every = 0, steps = 0;
            var handle = timers.After(0.1f, () => once++);
            timers.Every(0.5f, () => every++);
            timers.AfterSteps(2, () => steps++);
            timers.Update(bad);
            timers.Update(bad, bad);
            Assert.AreEqual((0, 0, 0), (once, every, steps), $"a step of {bad} calls nothing");
            Assert.AreEqual(0.1f, timers.Remaining(handle), $"a step of {bad} moves nothing");
            for (int i = 0; i < 6; i++) timers.Update(Step);
            Assert.AreEqual((1, 0, 1), (once, every, steps), $"and the timers go on after a step of {bad}");
        }
    }

    [TestMethod]
    public void An_interval_shorter_than_the_step_is_called_once_per_interval_up_to_a_bound_and_the_rest_is_dropped()
    {
        var timers = new Timers();
        int calls = 0;
        timers.Every(0.001f, () => calls++);
        timers.Update(0.0105f);
        Assert.AreEqual(10, calls, "Ten intervals passed in the step.");
        timers.Update(1000f);
        Assert.AreEqual(10 + Timers.MaxCallsPerUpdate, calls, "A step of a million intervals calls it the bound, no more.");
        timers.Update(0.0005f);
        Assert.AreEqual(10 + Timers.MaxCallsPerUpdate, calls, "What was left of the long step was dropped: the next call is one interval after it.");
        timers.Update(0.0006f);
        Assert.AreEqual(11 + Timers.MaxCallsPerUpdate, calls);
    }

    [TestMethod]
    public void A_step_that_is_not_a_positive_finite_time_does_not_move_a_machine()
    {
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, -1f })
        {
            var machine = new StateMachine<S>(S.Wait);
            int updates = 0;
            machine.State(S.Wait).OnUpdate(_ => updates++).After(0.1f, S.Done);
            machine.Update(bad);
            Assert.AreEqual(S.Wait, machine.Current, $"a step of {bad}");
            Assert.AreEqual(0f, machine.Time, $"a step of {bad} is no time");
            Assert.AreEqual(0, machine.Steps);
            Assert.AreEqual(1, updates, "The state's update still runs, as on a step of zero.");
            for (int i = 0; i < 6; i++) machine.Update(Step);
            Assert.AreEqual(S.Done, machine.Current, $"and the machine goes on after a step of {bad}");
        }
    }

    // ---- D4: one rule for zero ---------------------------------------------------------------------------------------

    [TestMethod]
    public void A_zero_duration_or_zero_steps_happens_on_the_next_update_that_has_time_never_on_one_of_zero_seconds()
    {
        // Timers.
        var timers = new Timers();
        var log = new List<string>();
        timers.After(0, () => log.Add("timer seconds"));
        timers.AfterSteps(0, () => log.Add("timer steps"));
        for (int i = 0; i < 3; i++) timers.Update(0);
        Assert.AreEqual(0, log.Count, "An update of zero seconds calls nothing.");
        timers.Update(Step);
        CollectionAssert.AreEqual(new[] { "timer seconds", "timer steps" }, log);

        // Machines: by seconds and by steps.
        foreach (bool bySteps in new[] { false, true })
        {
            var machine = new StateMachine<S>(S.Wait);
            bool open = false;
            if (bySteps) machine.State(S.Wait).AfterSteps(0, S.Done);
            else machine.State(S.Wait).After(0, S.Done);
            machine.State(S.Done).When(() => open, S.Other);
            machine.Start();
            for (int i = 0; i < 3; i++) machine.Update(0);
            Assert.AreEqual(S.Wait, machine.Current, $"steps {bySteps}: an update of zero seconds takes no way out by time");
            machine.Update(Step);
            Assert.AreEqual(S.Done, machine.Current, $"steps {bySteps}: the next update that has time does");
            open = true;
            machine.Update(0);
            Assert.AreEqual(S.Other, machine.Current, "A condition is still checked on an update of zero seconds.");
        }

        // Tweens: by seconds and by steps. The starting value at once, the final one on the next update with time.
        foreach (bool bySteps in new[] { false, true })
        {
            var tweener = new Tweener();
            var values = new List<float>();
            int done = 0;
            var tween = bySteps
                ? tweener.AddSteps(0, values.Add, Ease.Linear, () => done++)
                : tweener.Add(0, values.Add, Ease.Linear, () => done++);
            for (int i = 0; i < 3; i++) tweener.Update(0);
            CollectionAssert.AreEqual(new[] { 0f }, values, $"steps {bySteps}: nothing moves on an update of zero seconds");
            Assert.IsFalse(tween.IsFinished);
            Assert.AreEqual(0f, tween.Progress);
            tweener.Update(Step);
            CollectionAssert.AreEqual(new[] { 0f, 1f }, values);
            Assert.AreEqual((true, 1, 1f), (tween.IsFinished, done, tween.Progress));
        }
    }

    // ---- D3: tweens and lengths or steps that are not a time -------------------------------------------------------------

    [TestMethod]
    public void A_tween_refuses_a_length_or_a_delay_that_is_not_a_finite_time_of_zero_or_more()
    {
        var tweener = new Tweener();
        int applied = 0;
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f })
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tweener.Add(bad, _ => applied++), $"length {bad}");
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tweener.Add(1f, _ => applied++, delay: bad), $"delay {bad}");
        }
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tweener.AddSteps(-1, _ => applied++));
        Assert.AreEqual((0, 0), (applied, tweener.Count), "A refused tween applies nothing and is not added.");
    }

    [TestMethod]
    public void A_step_that_is_not_a_positive_finite_time_leaves_every_tween_as_it_was()
    {
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, -1f, 0f })
        {
            var tweener = new Tweener();
            var runner = new CoroutineRunner();
            float value = -1;
            bool resumed = false;
            var tween = tweener.Add(4 * Q, t => value = t, Ease.Linear);
            var delayed = tweener.Add(Q, _ => { }, delay: 2 * Q);
            var steps = tweener.AddSteps(4, _ => { });
            System.Collections.IEnumerator Wait() { yield return tween; resumed = true; }
            runner.Start(Wait());
            tweener.Update(bad);
            tweener.Update(bad, bad);
            Assert.AreEqual((0f, 0f, 0f), (value, tween.Progress, steps.Progress), $"a step of {bad}");
            for (int i = 0; i < 4; i++) { tweener.Update(Q); runner.Update(Q); }
            Assert.AreEqual(1f, value, $"the tween goes on after a step of {bad}");
            Assert.IsTrue(tween.IsFinished && delayed.IsFinished && steps.IsFinished && resumed, $"and everything ends after a step of {bad}");
        }
    }

    // ---- D1: callbacks that clear, cancel, complete and add -------------------------------------------------------------

    [TestMethod]
    public void A_callback_may_clear_cancel_complete_or_add_tweens_wherever_its_own_tween_is()
    {
        string[] actions = ["clear", "cancel earlier", "cancel later", "cancel self", "complete earlier", "complete later", "complete self", "add"];
        foreach (string from in new[] { "apply", "done" })
            foreach (string action in actions)
                foreach (int position in new[] { 0, 1, 2 })
                {
                    string what = $"{action} from {from}, tween {position} of 3";
                    var tweener = new Tweener();
                    var tweens = new Tween[3];
                    var applied = new int[3];
                    var done = new int[3];
                    int added = 0;
                    void Act()
                    {
                        switch (action)
                        {
                            case "clear": tweener.Clear(); break;
                            case "cancel earlier": tweens[Math.Max(position - 1, 0)].Cancel(); break;
                            case "cancel later": tweens[Math.Min(position + 1, 2)].Cancel(); break;
                            case "cancel self": tweens[position].Cancel(); break;
                            case "complete earlier": tweens[Math.Max(position - 1, 0)].Complete(); break;
                            case "complete later": tweens[Math.Min(position + 1, 2)].Complete(); break;
                            case "complete self": tweens[position].Complete(); break;
                            case "add": tweener.Add(2 * Q, _ => added++, Ease.Linear); break;
                        }
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        int index = i;
                        bool acts = index == position;
                        tweens[i] = tweener.Add(acts && from == "done" ? 2 * Q : 6 * Q,
                            _ => { applied[index]++; if (acts && from == "apply" && applied[index] == 3) Act(); },
                            Ease.Linear,
                            () => { done[index]++; if (acts && from == "done") Act(); });
                    }
                    for (int update = 0; update < 12; update++) tweener.Update(Q);   // used to throw for "clear"

                    Assert.AreEqual(0, tweener.Count, what);
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.IsTrue(tweens[i].IsFinished, $"{what}: tween {i} is over");
                        Assert.IsTrue(done[i] <= 1, $"{what}: done of tween {i} called {done[i]} times");
                    }
                    if (action == "add") Assert.AreEqual(3, added, $"{what}: the added tween played (start, half, end)");
                    if (action == "clear") Assert.AreEqual(from == "done" ? 1 : 0, done.Sum(), $"{what}: a cleared tween calls no done");
                    if (action.StartsWith("complete", StringComparison.Ordinal)) Assert.AreEqual(3, done.Sum(), $"{what}: every tween reached its end once");
                }
    }

    [TestMethod]
    public void Clearing_outside_an_update_ends_every_tween_and_the_tweener_goes_on()
    {
        var tweener = new Tweener();
        int done = 0;
        var a = tweener.Add(1f, _ => { }, done: () => done++);
        var b = tweener.Add(1f, _ => { }, delay: 1f, done: () => done++);
        tweener.Clear();
        Assert.IsTrue(a.IsFinished && b.IsFinished);
        Assert.AreEqual((0, 0), (tweener.Count, done));
        float value = -1;
        tweener.Add(2 * Q, t => value = t, Ease.Linear);
        tweener.Update(Q);
        tweener.Update(Q);
        Assert.AreEqual(1f, value);
    }

    // ---- D6: a handle belongs to its timers ------------------------------------------------------------------------

    [TestMethod]
    public void A_handle_of_one_set_of_timers_does_nothing_on_another()
    {
        var a = new Timers();
        var b = new Timers();
        int calledA = 0, calledB = 0;
        var ofA = a.After(1f, () => calledA++);
        var ofB = b.After(2f, () => calledB++);
        Assert.AreEqual(ofA.Id, ofB.Id, "Both are the first timer of their owner.");
        Assert.AreNotEqual(ofA, ofB);
        Assert.IsFalse(b.Cancel(ofA));
        Assert.IsFalse(b.IsActive(ofA));
        Assert.AreEqual(0f, b.Remaining(ofA));
        Assert.IsTrue(b.IsActive(ofB));
        Assert.AreEqual(2f, b.Remaining(ofB));
        for (int i = 0; i < 130; i++) { a.Update(Step); b.Update(Step); }
        Assert.AreEqual((1, 1), (calledA, calledB));
        Assert.IsFalse(a.Cancel(TimerHandle.None));
        Assert.IsTrue(default(TimerHandle).IsNone);

        MicroEngine.Testing.AllocationAssert.None(() => { b.IsActive(ofA); b.Cancel(ofA); a.Remaining(ofB); }, because: "asking with a handle");
    }
}
