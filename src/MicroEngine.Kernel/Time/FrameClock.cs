namespace MicroEngine.Kernel;

/// <summary>
/// The loop's clock: turns the time each frame stands for into fixed steps, following <see cref="TimeSettings"/>. The
/// platform only supplies a monotonic clock; how much simulated time a frame adds, how many steps it runs, how far a slow
/// frame catches up and the interpolation blend are decided here, the same way on every backend.
/// </summary>
/// <remarks>
/// One frame is: <see cref="BeginFrame"/> with the frame's elapsed time, <see cref="TryStep"/> until it returns false
/// (or until the caller stops early), then <see cref="EndSteps"/>.
/// <code>
/// clock.BeginFrame(clock.ClampElapsed(now - last));
/// while (clock.TryStep(out var step)) schedule.Run(FramePhase.FixedUpdate, step);
/// clock.EndSteps();
/// </code>
/// </remarks>
public sealed class FrameClock
{
    // Steps due within this margin of a whole step still run, so rounding in the accumulated sum never drops one.
    private const double Epsilon = 1e-9;

    /// <summary>A clock following <paramref name="settings"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A setting is out of range (<see cref="TimeSettings.Validate"/>).</exception>
    public FrameClock(TimeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        Settings = settings;
        FixedDelta = settings.FixedDelta;
    }

    /// <summary>The policy this clock follows.</summary>
    public TimeSettings Settings { get; }
    /// <summary>Whether time follows the wall clock or advances one step per frame.</summary>
    public TimeMode Mode => Settings.Mode;
    /// <summary>Seconds of one fixed step.</summary>
    public double FixedDelta { get; }
    /// <summary>Frames begun so far; the current frame's number while one runs.</summary>
    public long Frame { get; private set; }
    /// <summary>Fixed steps completed so far.</summary>
    public long FixedStep { get; private set; }
    /// <summary>Steps run in the current frame.</summary>
    public int StepsThisFrame { get; private set; }
    /// <summary>Time added but not simulated yet, in seconds.</summary>
    public double Pending { get; private set; }
    /// <summary>Seconds the current frame stands for (<see cref="FrameContext.Delta"/>).</summary>
    public double Delta { get; private set; }
    /// <summary>The current frame's interpolation blend, set by <see cref="EndSteps"/> (<see cref="FrameContext.Blend"/>).</summary>
    public float Blend { get; private set; }
    /// <summary>The frame's context for the phases that run once per frame.</summary>
    public FrameContext Context => new(Delta, FixedDelta, Blend, FixedStep, Frame);

    /// <summary>
    /// The time a real-time frame may stand for: <paramref name="elapsed"/> limited to 0..<see cref="TimeSettings.MaxFrameDelta"/>.
    /// The loop applies it to the time it measured before pacing that time to the display.
    /// </summary>
    public double ClampElapsed(double elapsed) => Math.Clamp(elapsed, 0, Settings.MaxFrameDelta);

    /// <summary>
    /// Starts a frame. In real time, <paramref name="elapsed"/> seconds are added to the pending time (pass a value already
    /// limited by <see cref="ClampElapsed"/>; negative values count as 0); in deterministic time exactly one step is added
    /// and <paramref name="elapsed"/> is ignored.
    /// </summary>
    public void BeginFrame(double elapsed)
    {
        Frame++;
        StepsThisFrame = 0;
        Delta = Mode == TimeMode.Deterministic ? FixedDelta : Math.Max(0, elapsed);
        Pending += Delta;
    }

    /// <summary>
    /// Takes the next fixed step if one is due and the frame has not run <see cref="TimeSettings.MaxStepsPerFrame"/>
    /// yet. The caller runs the fixed phases with <paramref name="step"/> when it returns true.
    /// </summary>
    /// <param name="step">The step's context: <see cref="FrameContext.FixedStep"/> is the index of this step, <see cref="FrameContext.Blend"/> is 0.</param>
    public bool TryStep(out FrameContext step)
    {
        if (Pending < FixedDelta - Epsilon || StepsThisFrame >= Settings.MaxStepsPerFrame)
        {
            step = default;
            return false;
        }
        step = new FrameContext(Delta, FixedDelta, 0, FixedStep, Frame);
        Pending -= FixedDelta;
        StepsThisFrame++;
        FixedStep++;
        return true;
    }

    /// <summary>
    /// Ends the frame's steps. A frame that ran <see cref="TimeSettings.MaxStepsPerFrame"/> steps keeps at most one step
    /// pending (the rest is dropped: the game slows down rather than falling further behind). Then sets
    /// <see cref="Blend"/>: the pending fraction of a step in real time, 1 in deterministic time.
    /// </summary>
    public void EndSteps()
    {
        if (StepsThisFrame >= Settings.MaxStepsPerFrame) Pending = Math.Min(Pending, FixedDelta);
        Blend = Mode == TimeMode.Deterministic ? 1f : (float)Math.Clamp(Pending / FixedDelta, 0, 1);
    }

    /// <summary>Forgets the pending time, so time spent away (a suspended app) is not simulated when it comes back.</summary>
    public void DiscardPending() => Pending = 0;
}
