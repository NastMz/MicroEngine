namespace MicroEngine;

/// <summary>
/// The time a timer or a state machine counts: the sum of the steps it was given, kept so that a time in seconds is
/// reached on the update a developer can work out.
/// </summary>
/// <remarks>
/// <para>
/// A step arrives as a <c>float</c>, and the <c>float</c> nearest to 1/60 is not 1/60: summed, it drifts from the true
/// time by a few hundredths of a millionth of it, a whole update after some hours. So a step that is exactly the
/// <c>float</c> of <c>1 / R</c> for a whole rate R (what the engine gives, also at a clock scale of a half or a
/// quarter) is counted as <c>1 / R</c> in double precision, and while the step stays the same the clock is a count
/// of updates times that step, not a running sum. Any other step is summed as it is.
/// </para>
/// <para>
/// A step that is not a positive, finite number is no time: it advances nothing.
/// </para>
/// </remarks>
internal struct StepClock
{
    private double _base;     // the time when the step last changed
    private long _count;      // updates of the current step since then
    private double _step;     // the current step, as counted
    private float _last;      // the current step, as given

    /// <summary>Updates that had time.</summary>
    public long Updates { get; private set; }

    /// <summary>The time counted, in seconds.</summary>
    public readonly double Now => _base + _count * _step;

    /// <summary>
    /// How far short of <paramref name="seconds"/> a time on this clock still counts as reached: a hundred-thousandth of
    /// a second, for a time worked out in single precision (<c>3 * 0.3f</c> is a hair over 0.9), but never more than a
    /// hundredth of the current step, so no time is reached an update early; plus the rounding of the clock's own
    /// arithmetic, which is far smaller.
    /// </summary>
    public readonly double Reach(double seconds)
    {
        double now = Math.Abs(Now);
        return Math.Min(1e-5, 0.01 * _step) + 1e-10 * Math.Max(1, seconds) + 4 * (Math.BitIncrement(now) - now);
    }

    /// <summary>Whether <paramref name="dt"/> is a time a clock advances by.</summary>
    public static bool IsTime(float dt) => dt > 0 && !float.IsPositiveInfinity(dt);

    /// <summary>Adds one update of <paramref name="dt"/> seconds; false, and nothing changes, when it is not a time.</summary>
    public bool Advance(float dt)
    {
        if (!IsTime(dt)) return false;
        if (dt != _last)
        {
            _base = Now;
            _count = 0;
            _last = dt;
            _step = Counted(dt);
        }
        _count++;
        Updates++;
        return true;
    }

    /// <summary>The step counted for <paramref name="dt"/>: <c>1 / R</c> when it is exactly the float of that for a whole R, itself otherwise.</summary>
    internal static double Counted(float dt)
    {
        double rate = Math.Round(1.0 / dt);
        return rate >= 1 && rate <= 1e7 && (float)(1.0 / rate) == dt ? 1.0 / rate : dt;
    }

    /// <summary>
    /// A time given in seconds as the number its author wrote: the <c>float</c> 0.1 is 0.100000001490116, and the delay
    /// meant is 0.1. Seven significant digits are kept, which is all a <c>float</c> holds.
    /// </summary>
    internal static double Meant(float seconds) =>
        seconds is > 1e-20f and < 1e20f ? (double)(decimal)seconds : seconds;
}
