using MicroEngine.Kernel;

namespace MicroEngine;

/// <summary>A timer of a <see cref="Timers"/>; <see cref="None"/> names no timer.</summary>
/// <param name="Id">The timer's number within its <see cref="Timers"/>; 0 for <see cref="None"/>.</param>
public readonly record struct TimerHandle(int Id)
{
    /// <summary>The <see cref="Timers"/> that gave the handle: a handle does nothing on another one.</summary>
    internal int Owner { get; init; }
    /// <summary>No timer.</summary>
    public static TimerHandle None => default;
    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Id == 0;
}

/// <summary>
/// Calls actions after a time or at intervals, counted in seconds or in updates. Whatever owns it calls
/// <see cref="Update(float)"/> once per step: a scene has one of its own (<c>Scene.Timers</c>), and the rules of a game
/// kept in a plain class (a stage, a board, a match that tests step without a scene) create theirs. Its state takes
/// part in the state hash.
/// </summary>
/// <remarks>
/// <para>
/// A timer in seconds is called on the update on which the time since it was added reaches its delay. At a fixed rate
/// of R updates a second that is after exactly <c>seconds x R</c> updates when the product is a whole number
/// (0.5 seconds at 60: 30) and after the next whole number otherwise; a repeating timer keeps what is left over, so
/// its n-th call follows the same rule for <c>n x seconds</c>. The rule holds for a step that is <c>1 / R</c> for a
/// whole R, which is counted as exactly that; any other step is summed as given. A time counts as reached within a
/// hundred-thousandth of a second of it, and never within more than a hundredth of an update. A timer in updates counts the updates that have time.
/// </para>
/// <para>
/// An update whose step is zero, negative, infinite or not a number advances nothing and calls nothing, so a delay of
/// zero seconds or zero updates is called on the next update that has time. A repeating timer whose interval is
/// shorter than the step is called once per interval that passed, at most <see cref="MaxCallsPerUpdate"/> times in
/// one update; what is left beyond that is dropped and its next call is one interval later.
/// </para>
/// <para>
/// Timers are called in the order they were added. A timer added by an action waits for the next update; an action may
/// cancel any timer, its own included. A <see cref="TimerHandle"/> belongs to the <see cref="Timers"/> that gave it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class Stage
/// {
///     private readonly Timers _timers = new();
///
///     public void BossDefeated()
///     {
///         _timers.After(2.6f, BigExplosion);
///         _timers.After(3.4f, Fanfare);
///     }
///
///     public void Update(float dt)
///     {
///         _timers.Update(dt);
///         // the rules
///     }
///
///     public void HashState(StateHasher hash) => hash.Add(_timers);
/// }
/// </code>
/// </example>
public sealed class Timers : IStateHashed
{
    /// <summary>The most calls one repeating timer gets in one update; what is due beyond them is dropped.</summary>
    public const int MaxCallsPerUpdate = 1000;

    private static int s_owners;

    private sealed class Timer
    {
        public int Id;
        public double Start;     // its clock (seconds, or updates) when it was added, or last dropped what was left
        public double Delay;     // from the start to the first call
        public double Interval;
        public long Calls;       // calls made since the start
        // The n-th call is due at a product, never at a running sum, so a repeating timer cannot drift.
        public double Due => Delay + Calls * Interval;
        public int Left; // calls left; -1 repeats until cancelled
        public bool Steps;
        public bool Unscaled;
        public bool Dead;
        public required Action Action;
    }

    private readonly List<Timer> _timers = [];
    private readonly int _owner = Interlocked.Increment(ref s_owners);
    private int _nextId = 1;
    private StepClock _clock, _unscaled;

    /// <summary>Where <paramref name="timer"/>'s count stands: seconds or updates of its clock.</summary>
    private double Now(Timer timer)
    {
        ref var clock = ref timer.Unscaled ? ref _unscaled : ref _clock;
        return timer.Steps ? clock.Updates : clock.Now;
    }

    /// <summary>What is left before <paramref name="timer"/>'s next call, in its unit.</summary>
    private double Left(Timer timer) => timer.Due - (Now(timer) - timer.Start);

    /// <summary>How many timers are waiting.</summary>
    public int Count
    {
        get
        {
            int count = 0;
            foreach (var timer in _timers) if (!timer.Dead) count++;
            return count;
        }
    }

    /// <summary>Calls <paramref name="action"/> once, <paramref name="seconds"/> from now.</summary>
    /// <param name="seconds">The delay; 0 calls it in the next update that has time.</param>
    /// <param name="action">What to call.</param>
    /// <param name="unscaled">True to count the second clock of <see cref="Update(float, float)"/> (a scene's engine step), so the timer runs through slow motion and a frozen world.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is negative or not a number.</exception>
    public TimerHandle After(float seconds, Action action, bool unscaled = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!(seconds >= 0) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A delay is zero or more seconds.");
        return Add(StepClock.Meant(seconds), 0, 1, action, steps: false, unscaled);
    }

    /// <summary>Calls <paramref name="action"/> every <paramref name="seconds"/>, the first time one interval from now.</summary>
    /// <param name="seconds">The interval, above zero. A step longer than the interval calls the action once per interval that passed, up to <see cref="MaxCallsPerUpdate"/>.</param>
    /// <param name="action">What to call.</param>
    /// <param name="times">How many calls before the timer ends; 0 (the default) repeats until it is cancelled.</param>
    /// <param name="unscaled">True to count the second clock of <see cref="Update(float, float)"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is not above zero, or <paramref name="times"/> is negative.</exception>
    public TimerHandle Every(float seconds, Action action, int times = 0, bool unscaled = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!(seconds > 0) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "An interval is above zero seconds.");
        ArgumentOutOfRangeException.ThrowIfNegative(times);
        return Add(StepClock.Meant(seconds), StepClock.Meant(seconds), times == 0 ? -1 : times, action, steps: false, unscaled);
    }

    /// <summary>
    /// Calls <paramref name="action"/> once, on the <paramref name="steps"/>-th update from now: for a game that thinks in
    /// frames, and for a hand-kept timer moved here without changing the update it fires on. It counts updates, not time:
    /// slow motion does not stretch it, and an update of zero seconds (a frozen world) is not counted.
    /// </summary>
    /// <param name="steps">The delay in updates; 0 and 1 both call it in the next update that has time.</param>
    /// <param name="action">What to call.</param>
    /// <param name="unscaled">True to count the updates of the second clock of <see cref="Update(float, float)"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is negative.</exception>
    public TimerHandle AfterSteps(int steps, Action action, bool unscaled = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        return Add(steps, 0, 1, action, steps: true, unscaled);
    }

    /// <summary>Calls <paramref name="action"/> every <paramref name="steps"/> updates, the first time that many updates from now.</summary>
    /// <param name="steps">The interval in updates, above zero.</param>
    /// <param name="action">What to call.</param>
    /// <param name="times">How many calls before the timer ends; 0 (the default) repeats until it is cancelled.</param>
    /// <param name="unscaled">True to count the updates of the second clock of <see cref="Update(float, float)"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is not above zero, or <paramref name="times"/> is negative.</exception>
    public TimerHandle EverySteps(int steps, Action action, int times = 0, bool unscaled = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);
        ArgumentOutOfRangeException.ThrowIfNegative(times);
        return Add(steps, steps, times == 0 ? -1 : times, action, steps: true, unscaled);
    }

    private TimerHandle Add(double delay, double interval, int left, Action action, bool steps, bool unscaled)
    {
        var timer = new Timer { Id = _nextId++, Delay = delay, Interval = interval, Left = left, Action = action, Steps = steps, Unscaled = unscaled };
        timer.Start = Now(timer);
        _timers.Add(timer);
        return new TimerHandle(timer.Id) { Owner = _owner };
    }

    /// <summary>Cancels a timer; false when it already ended or was cancelled. A timer may cancel itself from its action.</summary>
    public bool Cancel(TimerHandle handle)
    {
        if (Find(handle) is not { } timer) return false;
        timer.Dead = true;
        return true;
    }

    /// <summary>Whether <paramref name="handle"/> names a timer that is still waiting.</summary>
    public bool IsActive(TimerHandle handle) => Find(handle) is not null;

    /// <summary>
    /// What is left before the next call of a timer: seconds for one added with <see cref="After"/> or <see cref="Every"/>,
    /// updates for one added with <see cref="AfterSteps"/> or <see cref="EverySteps"/>; 0 when it ended or was cancelled.
    /// With it the owner of a save writes what it needs to add the timer again when the save is loaded.
    /// </summary>
    public float Remaining(TimerHandle handle) => Find(handle) is { } timer ? (float)Math.Max(Left(timer), 0) : 0;

    private Timer? Find(TimerHandle handle)
    {
        if (handle.Owner != _owner) return null;
        foreach (var timer in _timers)
            if (timer.Id == handle.Id) return timer.Dead ? null : timer;
        return null;
    }

    /// <summary>Cancels every timer.</summary>
    public void Clear()
    {
        foreach (var timer in _timers) timer.Dead = true;
    }

    /// <summary>Advances every timer by one update of <paramref name="dt"/> seconds and calls those that are due, in the order they were added.</summary>
    /// <param name="dt">The step in seconds. Zero, a negative number, infinity and not-a-number are no time: nothing advances and nothing is called.</param>
    public void Update(float dt) => Update(dt, dt);

    /// <summary>
    /// Advances the timers on two clocks: <paramref name="dt"/> for the ordinary ones and <paramref name="unscaledDt"/>
    /// for those added with <c>unscaled: true</c>. A scene gives its world's step and the engine's, so an unscaled timer
    /// runs while the world is slowed or frozen. A clock whose step is no time (zero, negative, infinite, not a number)
    /// stands still, and its timers are not called.
    /// </summary>
    public void Update(float dt, float unscaledDt)
    {
        bool moved = _clock.Advance(dt), unscaledMoved = _unscaled.Advance(unscaledDt);
        if (!moved && !unscaledMoved) return;
        // Timers added by an action wait for the next update.
        int count = _timers.Count;
        for (int i = 0; i < count; i++)
        {
            var timer = _timers[i];
            if (timer.Dead || !(timer.Unscaled ? unscaledMoved : moved)) continue;
            double now = Now(timer);
            // In seconds: within the rounding of the arithmetic. In updates: whole numbers, so half of one.
            double tolerance = timer.Steps ? 0.5 : (timer.Unscaled ? _unscaled : _clock).Reach(timer.Due);
            int calls = 0;
            while (!timer.Dead && timer.Due - (now - timer.Start) <= tolerance)
            {
                if (calls == MaxCallsPerUpdate)
                {
                    // More intervals passed than one update may call: the rest is dropped.
                    timer.Start = now;
                    timer.Delay = timer.Interval;
                    timer.Calls = 0;
                    break;
                }
                calls++;
                if (timer.Left > 0) timer.Left--;
                if (timer.Left == 0) timer.Dead = true;
                else timer.Calls++;
                timer.Action();
            }
        }
        _timers.RemoveAll(static timer => timer.Dead);
    }

    /// <summary>Adds the waiting timers to the state hash: for each, its number, what is left, its interval, its calls left and how it counts.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(Count);
        foreach (var timer in _timers)
        {
            if (timer.Dead) continue;
            hash.Add(timer.Id);
            hash.Add(Now(timer) - timer.Start);
            hash.Add(timer.Due);
            hash.Add(timer.Interval);
            hash.Add(timer.Left);
            hash.Add(timer.Steps);
            hash.Add(timer.Unscaled);
        }
    }
}
