using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>Named systems per phase, ordered by insertion and by after/before constraints; cycles are reported.</summary>
[TestClass]
public sealed class FrameScheduleTests
{
    private static readonly FrameContext Context = new(1 / 60.0, 1 / 60.0, 0, 0, 1);

    private static (FrameSchedule Schedule, List<string> Ran) Recording(params (FramePhase Phase, string Name, string? After, string? Before)[] systems)
    {
        var schedule = new FrameSchedule();
        var ran = new List<string>();
        foreach (var (phase, name, after, before) in systems) schedule.Add(phase, name, _ => ran.Add(name), after, before);
        return (schedule, ran);
    }

    [TestMethod]
    public void Systems_of_a_phase_run_in_the_order_they_were_added()
    {
        var (schedule, ran) = Recording((FramePhase.Update, "a", null, null), (FramePhase.Update, "b", null, null), (FramePhase.Render, "draw", null, null), (FramePhase.Update, "c", null, null));
        schedule.Run(FramePhase.Update, Context);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, ran);
        CollectionAssert.AreEqual(new[] { "draw" }, schedule.Order(FramePhase.Render).ToArray());
        CollectionAssert.AreEqual(Array.Empty<string>(), schedule.Order(FramePhase.Physics).ToArray());
    }

    [TestMethod]
    public void After_and_before_move_a_system_and_keep_the_rest_in_insertion_order()
    {
        var (schedule, ran) = Recording(
            (FramePhase.FixedUpdate, "physics-sync", "scenes", null),
            (FramePhase.FixedUpdate, "input", null, "scenes"),
            (FramePhase.FixedUpdate, "scenes", null, null),
            (FramePhase.FixedUpdate, "audio", null, null),
            (FramePhase.FixedUpdate, "early", null, "input"));
        schedule.Run(FramePhase.FixedUpdate, Context);
        CollectionAssert.AreEqual(new[] { "early", "input", "scenes", "physics-sync", "audio" }, ran);
    }

    [TestMethod]
    public void A_cycle_is_reported_with_the_systems_in_it()
    {
        var (schedule, _) = Recording(
            (FramePhase.Update, "free", null, null),
            (FramePhase.Update, "a", "c", null),
            (FramePhase.Update, "b", "a", null),
            (FramePhase.Update, "c", "b", null));
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => schedule.Freeze());
        StringAssert.Contains(error.Message, "Frame systems in Update form a cycle: ");
        StringAssert.Matches(error.Message, new System.Text.RegularExpressions.Regex(@"cycle: (a -> b -> c -> a|b -> c -> a -> b|c -> a -> b -> c)\."));
        Assert.IsFalse(error.Message.Contains("free"), "a system outside the cycle is not named");
        Assert.IsFalse(schedule.IsFrozen);
    }

    [TestMethod]
    public void A_system_ordered_against_itself_is_a_cycle()
    {
        var (schedule, _) = Recording((FramePhase.Update, "self", null, "self"));
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => schedule.Order(FramePhase.Update)).Message, "cycle: self -> self");
    }

    [TestMethod]
    public void Constraints_naming_a_missing_system_or_another_phase_fail_clearly()
    {
        var (missing, _) = Recording((FramePhase.Update, "camera", "player", null));
        Assert.AreEqual("Frame system 'camera' (Update) runs after 'player', but no system named 'player' is scheduled.",
            Assert.ThrowsExactly<InvalidOperationException>(() => missing.Freeze()).Message);
        var (other, _) = Recording((FramePhase.FixedUpdate, "player", null, null), (FramePhase.Update, "camera", null, "player"));
        Assert.AreEqual("Frame system 'camera' (Update) runs before 'player', which is in FixedUpdate; after and before order systems within one phase.",
            Assert.ThrowsExactly<InvalidOperationException>(() => other.Freeze()).Message);
    }

    [TestMethod]
    public void Names_are_unique_and_replace_keeps_the_place()
    {
        var (schedule, ran) = Recording((FramePhase.Update, "a", null, null), (FramePhase.Update, "b", null, null));
        var error = Assert.ThrowsExactly<ArgumentException>(() => schedule.Add(FramePhase.Render, "a", _ => { }));
        StringAssert.Contains(error.Message, "'a' is already scheduled (in Update)");
        schedule.Replace("a", _ => ran.Add("a2"));
        schedule.Run(FramePhase.Update, Context);
        CollectionAssert.AreEqual(new[] { "a2", "b" }, ran);
        Assert.AreEqual(FramePhase.Update, schedule.PhaseOf("b"));
        Assert.IsTrue(schedule.Remove("b"));
        Assert.IsNull(schedule.PhaseOf("b"));
        Assert.ThrowsExactly<KeyNotFoundException>(() => schedule.Replace("b", _ => { }));
        Assert.ThrowsExactly<ArgumentException>(() => schedule.Add(FramePhase.Update, " ", _ => { }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => schedule.Add((FramePhase)99, "x", _ => { }));
    }

    [TestMethod]
    public void A_frozen_schedule_runs_but_cannot_change()
    {
        var (schedule, ran) = Recording((FramePhase.Update, "a", null, null));
        schedule.Freeze();
        Assert.IsTrue(schedule.IsFrozen);
        Assert.ThrowsExactly<InvalidOperationException>(() => schedule.Add(FramePhase.Update, "b", _ => { }));
        Assert.ThrowsExactly<InvalidOperationException>(() => schedule.Replace("a", _ => { }));
        Assert.ThrowsExactly<InvalidOperationException>(() => schedule.Remove("a"));
        schedule.Run(FramePhase.Update, Context);
        CollectionAssert.AreEqual(new[] { "a" }, ran);
    }

    [TestMethod]
    public void Systems_receive_the_context_and_the_fixed_phases_run_per_step()
    {
        var builder = new EngineBuilder();
        var seen = new List<string>();
        foreach (var phase in Enum.GetValues<FramePhase>())
            builder.Schedule.Add(phase, phase.ToString(), c => seen.Add($"{phase}:{c.FixedStep}"));
        var engine = builder.Build();
        var step = new FrameContext(0, 1 / 60.0, 0, 7, 3);
        engine.RunFixedStep(step);
        CollectionAssert.AreEqual(new[] { "PreFixed:7", "FixedUpdate:7", "Physics:7", "PostFixed:7" }, seen);
        Assert.IsTrue(FrameSchedule.IsFixed(FramePhase.Physics));
        Assert.IsFalse(FrameSchedule.IsFixed(FramePhase.Update));
        Assert.IsFalse(FrameSchedule.IsFixed(FramePhase.Input));
    }

    [TestMethod]
    public void Running_a_phase_allocates_nothing()
    {
        var schedule = new FrameSchedule();
        int count = 0;
        for (int i = 0; i < 8; i++) schedule.Add(FramePhase.Update, $"s{i}", _ => count++);
        schedule.Freeze();
        schedule.Run(FramePhase.Update, Context);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) schedule.Run(FramePhase.Update, Context);
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.AreEqual(808, count);
    }
}
