using MicroEngine.Kernel;

namespace MicroEngine;

/// <summary>A timer of a <see cref="Timers"/>; <see cref="None"/> names no timer.</summary>
/// <param name="Id">The timer's number within its <see cref="Timers"/>; 0 for <see cref="None"/>.</param>
public readonly record struct TimerHandle(int Id)
{
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
/// its n-th call follows the same rule for <c>n x seconds</c>. A timer in updates counts the updates of more than zero
/// seconds.
/// </para>
/// <para>
/// Timers are called in the order they were added. A timer added by an action waits for the next update; an action may
/// cancel any timer, its own included.
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
    // A delay counts as reached within this much of it: see StateMachine.
    private const double Epsilon = 1e-5;
    private const double RelativeEpsilon = 5e-7;

    private sealed class Timer
    {
        public int Id;
        public double Elapsed;   // counted since the timer was added
        public double Due;       // the count of its next call
        public double Interval;
        public double Remaining => Due - Elapsed;
        // The sum of single-precision steps is not the exact product, and the error grows with the time counted.
        public double Tolerance => Steps ? 0.5 : Math.Max(Epsilon, Due * RelativeEpsilon);
        public int Left; // calls left; -1 repeats until cancelled
        public bool Steps;
        public bool Unscaled;
        public bool Dead;
        public required Action Action;
    }

    private readonly List<Timer> _timers = [];
    private int _nextId = 1;

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
    /// <param name="seconds">The delay; 0 calls it in the next update.</param>
    /// <param name="action">What to call.</param>
    /// <param name="unscaled">True to count the second clock of <see cref="Update(float, float)"/> (a scene's engine step), so the timer runs through slow motion and a frozen world.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is negative or not a number.</exception>
    public TimerHandle After(float seconds, Action action, bool unscaled = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!(seconds >= 0) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A delay is zero or more seconds.");
        return Add(seconds, 0, 1, action, steps: false, unscaled);
    }

    /// <summary>Calls <paramref name="action"/> every <paramref name="seconds"/>, the first time one interval from now.</summary>
    /// <param name="seconds">The interval, above zero. A step longer than the interval calls the action once per interval that passed.</param>
    /// <param name="action">What to call.</param>
    /// <param name="times">How many calls before the timer ends; 0 (the default) repeats until it is cancelled.</param>
    /// <param name="unscaled">True to count the second clock of <see cref="Update(float, float)"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is not above zero, or <paramref name="times"/> is negative.</exception>
    public TimerHandle Every(float seconds, Action action, int times = 0, bool unscaled = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!(seconds > 0) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "An interval is above zero seconds.");
        ArgumentOutOfRangeException.ThrowIfNegative(times);
        return Add(seconds, seconds, times == 0 ? -1 : times, action, steps: false, unscaled);
    }

    /// <summary>
    /// Calls <paramref name="action"/> once, on the <paramref name="steps"/>-th update from now: for a game that thinks in
    /// frames, and for a hand-kept timer moved here without changing the update it fires on. It counts updates, not time:
    /// slow motion does not stretch it, and an update of zero seconds (a frozen world) is not counted.
    /// </summary>
    /// <param name="steps">The delay in updates; 0 and 1 both call it in the next update.</param>
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
        var timer = new Timer
        {
            Id = _nextId++, Due = delay, Interval = interval, Left = left, Action = action, Steps = steps, Unscaled = unscaled,
        };
        _timers.Add(timer);
        return new TimerHandle(timer.Id);
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
    public float Remaining(TimerHandle handle) => Find(handle) is { } timer ? (float)Math.Max(timer.Remaining, 0) : 0;

    private Timer? Find(TimerHandle handle)
    {
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
    /// <param name="dt">The step in seconds; 0 advances nothing.</param>
    public void Update(float dt) => Update(dt, dt);

    /// <summary>
    /// Advances the timers on two clocks: <paramref name="dt"/> for the ordinary ones and <paramref name="unscaledDt"/>
    /// for those added with <c>unscaled: true</c>. A scene gives its world's step and the engine's, so an unscaled timer
    /// runs while the world is slowed or frozen.
    /// </summary>
    public void Update(float dt, float unscaledDt)
    {
        // Timers added by an action wait for the next update.
        int count = _timers.Count;
        for (int i = 0; i < count; i++)
        {
            var timer = _timers[i];
            if (timer.Dead) continue;
            float step = timer.Unscaled ? unscaledDt : dt;
            if (timer.Steps)
            {
                if (!(step > 0)) continue;
                timer.Elapsed += 1;
            }
            else timer.Elapsed += step;
            while (!timer.Dead && timer.Remaining <= timer.Tolerance)
            {
                if (timer.Left > 0) timer.Left--;
                if (timer.Left == 0) timer.Dead = true;
                else timer.Due += timer.Interval;
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
            hash.Add(timer.Elapsed);
            hash.Add(timer.Due);
            hash.Add(timer.Interval);
            hash.Add(timer.Left);
            hash.Add(timer.Steps);
            hash.Add(timer.Unscaled);
        }
    }
}
