using MicroEngine.Kernel;

namespace MicroEngine.Tests.Kernel;

/// <summary>The loop-owned clock: fixed steps from elapsed time, limits from <see cref="TimeSettings"/>, the blend.</summary>
[TestClass]
public sealed class FrameClockTests
{
    /// <summary>A fake monotonic clock: frames arrive every <paramref name="period"/> seconds.</summary>
    private static (long Steps, List<float> Blends) Run(FrameClock clock, double period, int frames)
    {
        long steps = 0;
        var blends = new List<float>();
        double now = 0, last = 0;
        for (int i = 0; i < frames; i++)
        {
            now += period;
            clock.BeginFrame(clock.ClampElapsed(now - last));
            last = now;
            while (clock.TryStep(out _)) steps++;
            clock.EndSteps();
            blends.Add(clock.Blend);
        }
        return (steps, blends);
    }

    [TestMethod]
    [DataRow(30)]
    [DataRow(60)]
    [DataRow(120)]
    [DataRow(144)]
    public void Real_time_runs_the_fixed_rate_whatever_the_display_rate(int rate)
    {
        foreach (int display in new[] { 30, 60, 75, 144, 240 })
        {
            var clock = new FrameClock(new TimeSettings { FixedRate = rate, MaxStepsPerFrame = 10 });
            var (steps, _) = Run(clock, 1.0 / display, display * 2);
            Assert.AreEqual(2 * rate, steps, 1, $"{rate} Hz simulated for two seconds of {display} Hz frames");
            Assert.AreEqual(steps, clock.FixedStep);
            Assert.AreEqual(display * 2, clock.Frame);
            Assert.AreEqual(1.0 / rate, clock.FixedDelta, 1e-12);
            Assert.AreEqual(1.0 / display, clock.Delta, 1e-9, "a real-time frame stands for the time it took");
        }
    }

    [TestMethod]
    [DataRow(30)]
    [DataRow(60)]
    [DataRow(120)]
    [DataRow(144)]
    public void Deterministic_time_runs_one_step_per_frame_whatever_the_clock_says(int rate)
    {
        var clock = new FrameClock(new TimeSettings { FixedRate = rate, Mode = TimeMode.Deterministic });
        foreach (double elapsed in new[] { 0, 0.001, 1.0 / rate, 0.2, 5 })
        {
            clock.BeginFrame(elapsed);
            Assert.IsTrue(clock.TryStep(out var step));
            Assert.AreEqual(clock.FixedStep - 1, step.FixedStep, "the step's index");
            Assert.AreEqual(0f, step.Blend);
            Assert.IsFalse(clock.TryStep(out _), "exactly one step per frame");
            clock.EndSteps();
            Assert.AreEqual(1f, clock.Blend, "the frame shows exactly the last step");
            Assert.AreEqual(1.0 / rate, clock.Delta, 1e-12, "a deterministic frame stands for one step");
        }
        Assert.AreEqual(5, clock.FixedStep);
        Assert.AreEqual(new FrameContext(1.0 / rate, 1.0 / rate, 1f, 5, 5), clock.Context);
    }

    [TestMethod]
    public void A_long_frame_counts_as_max_frame_delta()
    {
        var clock = new FrameClock(new TimeSettings { FixedRate = 60, MaxFrameDelta = 0.1, MaxStepsPerFrame = 100 });
        Assert.AreEqual(0.1, clock.ClampElapsed(3));
        Assert.AreEqual(0, clock.ClampElapsed(-1));
        Assert.AreEqual(0.05, clock.ClampElapsed(0.05));
        clock.BeginFrame(clock.ClampElapsed(3));
        int steps = 0;
        while (clock.TryStep(out _)) steps++;
        Assert.AreEqual(6, steps, "0.1 s at 60 Hz, not 3 s");
        Assert.AreEqual(0.25, new FrameClock(new TimeSettings()).ClampElapsed(1), "0.25 s by default");
    }

    [TestMethod]
    public void A_frame_runs_at_most_max_steps_and_keeps_at_most_one_step_pending()
    {
        foreach (int max in new[] { 1, 5, 8 })
        {
            var clock = new FrameClock(new TimeSettings { FixedRate = 60, MaxStepsPerFrame = max, MaxFrameDelta = 1 });
            clock.BeginFrame(clock.ClampElapsed(1));
            int steps = 0;
            while (clock.TryStep(out _)) steps++;
            Assert.AreEqual(max, steps);
            clock.EndSteps();
            Assert.AreEqual(1.0 / 60, clock.Pending, 1e-12, "the rest is dropped: the game slows down instead of spiralling");
            Assert.AreEqual(1f, clock.Blend);
        }
    }

    [TestMethod]
    public void Blend_is_the_unsimulated_fraction_of_a_step()
    {
        var clock = new FrameClock(new TimeSettings { FixedRate = 60 });
        // 144 Hz frames over 60 Hz steps: the pending time grows by 1/144 and drops by 1/60 at each step.
        var (_, blends) = Run(clock, 1.0 / 144, 6);
        float[] expected = [60f / 144, 120f / 144, 36f / 144, 96f / 144, 12f / 144, 72f / 144];
        for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], blends[i], 1e-5f, $"frame {i + 1}");
        Assert.AreEqual(2, clock.FixedStep);

        var exact = new FrameClock(new TimeSettings { FixedRate = 60 });
        exact.BeginFrame(1.0 / 60);
        Assert.IsTrue(exact.TryStep(out _));
        exact.EndSteps();
        Assert.AreEqual(0f, exact.Blend, 1e-6f, "a frame that ends on a step boundary blends nothing");
    }

    [TestMethod]
    public void A_frame_can_stop_its_steps_early_and_the_rest_waits()
    {
        var clock = new FrameClock(new TimeSettings { FixedRate = 60 });
        clock.BeginFrame(3.0 / 60);
        Assert.IsTrue(clock.TryStep(out _));
        clock.EndSteps();
        Assert.AreEqual(2.0 / 60, clock.Pending, 1e-12);
        clock.BeginFrame(0);
        Assert.IsTrue(clock.TryStep(out _));
        Assert.IsTrue(clock.TryStep(out _));
        Assert.IsFalse(clock.TryStep(out _));
    }

    [TestMethod]
    public void Discarding_pending_time_forgets_time_spent_away()
    {
        var clock = new FrameClock(new TimeSettings { FixedRate = 60 });
        clock.BeginFrame(0.01);
        clock.DiscardPending();
        Assert.AreEqual(0, clock.Pending);
        Assert.IsFalse(clock.TryStep(out _));
        clock.BeginFrame(-1);
        Assert.AreEqual(0, clock.Delta, "a negative elapsed time counts as none");
    }
}
