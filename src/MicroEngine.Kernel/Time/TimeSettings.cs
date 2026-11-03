namespace MicroEngine.Kernel;

/// <summary>How the loop advances time (<see cref="TimeSettings.Mode"/>).</summary>
public enum TimeMode
{
    /// <summary>
    /// Time follows the wall clock: each frame adds the elapsed time the loop measured (at most
    /// <see cref="TimeSettings.MaxFrameDelta"/>), and the simulation runs as many fixed steps as that time calls for.
    /// </summary>
    RealTime,
    /// <summary>
    /// Time is the loop's own: every frame stands for exactly one fixed step, whatever the wall clock says, so a run with
    /// the same input produces the same steps on any machine at any speed (scripted runs, tests, captures).
    /// </summary>
    Deterministic,
}

/// <summary>
/// The loop's time policy: the fixed simulation rate, how far a slow frame may catch up, and whether time follows the
/// wall clock. Every value is explicit; nothing is derived from the platform or from other options.
/// </summary>
public sealed record TimeSettings
{
    /// <summary>Fixed steps per second the simulation runs at (60 by default); must be positive.</summary>
    public int FixedRate { get; init; } = 60;
    /// <summary>
    /// At most this many fixed steps run in one frame (5 by default); must be positive. A frame that would need more
    /// (a slow machine, a hitch) runs these and keeps at most one step of time pending, so the game slows down instead
    /// of falling further behind.
    /// </summary>
    public int MaxStepsPerFrame { get; init; } = 5;
    /// <summary>
    /// The longest time in seconds one real-time frame may stand for (0.25 by default); must be positive. Longer frames
    /// (the window was dragged, a breakpoint) count as this much, so the game does not jump ahead.
    /// </summary>
    public double MaxFrameDelta { get; init; } = 0.25;
    /// <summary>Whether time follows the wall clock or advances one fixed step per frame (<see cref="TimeMode.RealTime"/> by default).</summary>
    public TimeMode Mode { get; init; } = TimeMode.RealTime;

    /// <summary>Seconds of one fixed step: <c>1 / FixedRate</c>.</summary>
    public double FixedDelta => 1.0 / FixedRate;

    /// <summary>Checks every value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range; the message names it.</exception>
    public void Validate()
    {
        if (FixedRate <= 0) throw new ArgumentOutOfRangeException(nameof(FixedRate), FixedRate, "TimeSettings.FixedRate must be positive.");
        if (MaxStepsPerFrame <= 0) throw new ArgumentOutOfRangeException(nameof(MaxStepsPerFrame), MaxStepsPerFrame, "TimeSettings.MaxStepsPerFrame must be positive.");
        if (!(MaxFrameDelta > 0) || double.IsInfinity(MaxFrameDelta))
            throw new ArgumentOutOfRangeException(nameof(MaxFrameDelta), MaxFrameDelta, "TimeSettings.MaxFrameDelta must be a positive number of seconds.");
        if (!Enum.IsDefined(Mode)) throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Not a time mode.");
    }
}
