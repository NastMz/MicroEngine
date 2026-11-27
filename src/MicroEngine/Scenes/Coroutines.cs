using System.Collections;
using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>Yield instructions understood by <see cref="CoroutineRunner"/>.</summary>
public static class Wait
{
    /// <summary>
    /// Pauses the coroutine for <paramref name="seconds"/> of update time. The wait counts down in single precision, as
    /// a loop written by hand does: at R updates a second it lasts <c>seconds x R</c> updates, or one more when the
    /// steps add up to a hair under the time (0.5 seconds at 60 is 30 updates, 1 second is 61). For a wait of an exact
    /// number of updates use <see cref="Steps"/>.
    /// </summary>
    public static WaitSeconds Seconds(float seconds) => new(seconds);
    /// <summary>Pauses the coroutine until <paramref name="condition"/> returns true; checked once per update.</summary>
    public static WaitUntil Until(Func<bool> condition) => new(condition);
    /// <summary>Pauses the coroutine for <paramref name="steps"/> updates: 1 is the next update, as yielding null; 0 does not pause.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is negative.</exception>
    public static WaitSteps Steps(int steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        return new(steps);
    }
}

/// <summary>Yield instruction that pauses a coroutine for a number of seconds; create it with <see cref="Wait.Seconds"/>.</summary>
/// <param name="Seconds">Update time to wait, in seconds.</param>
public sealed record WaitSeconds(float Seconds);
/// <summary>Yield instruction that pauses a coroutine until a condition holds; create it with <see cref="Wait.Until"/>.</summary>
/// <param name="Condition">Checked once per update; the coroutine resumes in the update it first returns true.</param>
public sealed record WaitUntil(Func<bool> Condition);
/// <summary>Yield instruction that pauses a coroutine for a number of updates; create it with <see cref="Wait.Steps"/>.</summary>
/// <param name="Steps">Updates to wait.</param>
public sealed record WaitSteps(int Steps);

/// <summary>
/// Runs iterator-based sequences one step per update. A coroutine may yield null (next update),
/// <see cref="Wait.Seconds"/>, <see cref="Wait.Steps"/>, <see cref="Wait.Until"/>, a <see cref="Tween"/> (until it
/// ends) or another IEnumerator (awaited as a sub-routine).
/// Ideal for cutscenes, dialogue flow and timed effects without hand-written state machines.
/// </summary>
public sealed class CoroutineRunner : IStateHashed
{
    private sealed class Routine(IEnumerator root)
    {
        public readonly Stack<IEnumerator> Stack = new([root]);
        public float Wait;
        public int Steps;
        public Func<bool>? Until;
        public Tween? Tween;
        public bool Cancelled;
    }

    private readonly List<Routine> _routines = [];
    private readonly List<Routine> _added = [];

    /// <summary>Number of coroutines that are running or waiting to start.</summary>
    public int Count => _routines.Count(r => !r.Cancelled) + _added.Count;

    /// <summary>Starts a coroutine. Its first step runs on the next <see cref="Update"/>, not immediately.</summary>
    /// <param name="routine">The iterator, usually from a method that uses <c>yield return</c>.</param>
    /// <returns>A handle to cancel the coroutine or check whether it finished.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="routine"/> is null.</exception>
    public CoroutineHandle Start(IEnumerator routine)
    {
        ArgumentNullException.ThrowIfNull(routine);
        var r = new Routine(routine);
        _added.Add(r);
        return new CoroutineHandle(() => r.Cancelled = true, () => r.Cancelled || r.Stack.Count == 0);
    }

    /// <summary>Stops every coroutine, including those started but not yet run.</summary>
    public void StopAll()
    {
        foreach (var r in _routines) r.Cancelled = true;
        foreach (var r in _added) r.Cancelled = true;
        _added.Clear();
    }

    /// <summary>Advances every coroutine by one step. A scene calls this for its own <see cref="Scene.Coroutines"/>; call it yourself only for a runner you created.</summary>
    /// <param name="dt">Elapsed time in seconds, consumed by <see cref="Wait.Seconds"/>.</param>
    /// <exception cref="InvalidOperationException">A coroutine yielded a value other than null, a wait instruction, a tween or an <see cref="IEnumerator"/>.</exception>
    public void Update(float dt)
    {
        _routines.AddRange(_added);
        _added.Clear();
        foreach (var r in _routines)
        {
            if (r.Cancelled) continue;
            if (r.Wait > 0) { r.Wait -= dt; if (r.Wait > 0) continue; }
            if (r.Steps > 0 && --r.Steps > 0) continue;
            if (r.Until is not null) { if (!r.Until()) continue; r.Until = null; }
            if (r.Tween is not null) { if (!r.Tween.IsFinished) continue; r.Tween = null; }
            while (r.Stack.Count > 0 && !r.Cancelled)
            {
                var top = r.Stack.Peek();
                if (!top.MoveNext()) { r.Stack.Pop(); continue; }
                switch (top.Current)
                {
                    case null: break;
                    case WaitSeconds w: r.Wait = w.Seconds; break;
                    case WaitSteps s: if (s.Steps > 0) r.Steps = s.Steps; else continue; break;
                    case WaitUntil u: if (!u.Condition()) r.Until = u.Condition; else continue; break;
                    case Tween tween: if (!tween.IsFinished) r.Tween = tween; else continue; break;
                    case IEnumerator nested: r.Stack.Push(nested); continue;
                    default: throw new InvalidOperationException($"Unsupported yield value {top.Current.GetType().Name}.");
                }
                break;
            }
        }
        _routines.RemoveAll(r => r.Cancelled || r.Stack.Count == 0);
    }

    /// <summary>
    /// Adds the runner's state to the per-step state hash: for each coroutine, running or waiting to start, its nesting
    /// depth, the seconds and the updates left to wait and whether it waits for a condition or a tween. The iterators'
    /// own variables are not visible to it; a scene adds the state its coroutines change from its own
    /// <see cref="Scene.HashState"/>.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Count);
        foreach (var r in _routines)
        {
            if (r.Cancelled) continue;
            hash.Add(r.Stack.Count);
            hash.Add(r.Wait);
            hash.Add(r.Steps);
            hash.Add(r.Until is not null);
            hash.Add(r.Tween is not null);
        }
        foreach (var r in _added) hash.Add(r.Stack.Count);
    }
}

/// <summary>Controls a coroutine started with <see cref="CoroutineRunner.Start"/>.</summary>
public sealed class CoroutineHandle(Action cancel, Func<bool> finished)
{
    /// <summary>Whether the coroutine ran to its end or was cancelled.</summary>
    public bool IsFinished => finished();
    /// <summary>Stops the coroutine; it does not run another step. Safe to call more than once.</summary>
    public void Cancel() => cancel();
}

/// <summary>
/// A tween added to a <see cref="Tweener"/>: what a coroutine yields to wait for it, and the way to end it early.
/// </summary>
/// <example>
/// <code>
/// IEnumerator Intro()
/// {
///     yield return Tweens.Add(0.9f, t => _curtain = 1 - t, Ease.OutCubic);   // goes on when the curtain is open
///     var bob = Tweens.Add(0.3f, t => _hero.Offset = new Vector2(0, -6 * t));
///     yield return Wait.Until(() => Input.Pressed("action") || bob.IsFinished);
///     bob.Complete();                                                         // skipped: jump to the end
/// }
/// </code>
/// </example>
public sealed class Tween
{
    internal float Duration;
    internal Func<float, float> Ease = null!;
    internal Action<float> Apply = null!;
    internal Action? Done;
    internal float Time;
    internal float Delay;
    internal bool InSteps;
    internal bool Scaled;
    internal bool Started;
    internal bool Ended;
    internal bool Reached;   // the final value was applied
    internal bool Removed;
    internal IWorld? World;
    internal EntityId Entity;

    internal Tween() { }

    /// <summary>Whether the tween reached its end, was completed or was cancelled. A coroutine waiting for it goes on.</summary>
    public bool IsFinished => Ended;

    /// <summary>How far along the tween is, from 0 to 1, before easing; 0 while it waits for its delay.</summary>
    public float Progress => Duration > 0 ? Time / Duration : Reached ? 1 : 0;

    /// <summary>Stops the tween where it is: no further value is applied and <c>done</c> is not called. Safe to call more than once.</summary>
    public void Cancel() => Ended = true;

    /// <summary>
    /// Ends the tween now: applies its final value and calls <c>done</c>, as reaching the end does (skipping a cutscene,
    /// closing a menu mid-slide). Nothing happens when it already ended or was cancelled.
    /// </summary>
    public void Complete()
    {
        if (Ended) return;
        if (LostEntity()) { Ended = true; return; }
        Time = Duration;
        Ended = Reached = true;
        Apply(Ease(1f));
        Done?.Invoke();
    }

    /// <summary>
    /// Ties the tween to an entity: once <paramref name="entity"/> is no longer alive in <paramref name="world"/> the
    /// tween is cancelled instead of applying another value, so a tween that moves an object may outlive it.
    /// </summary>
    /// <returns>The tween, to chain or yield.</returns>
    public Tween Bind(IWorld world, EntityId entity)
    {
        ArgumentNullException.ThrowIfNull(world);
        World = world;
        Entity = entity;
        return this;
    }

    internal bool LostEntity() => World is not null && !World.IsAlive(Entity);
}

/// <summary>
/// Value tweens driven by update time: each calls an action with an eased progress from 0 to 1 over a length in
/// seconds or in updates. A scene has one (<see cref="Scene.Tweens"/>) and steps it; one you create is stepped with
/// <see cref="Update(float)"/>.
/// </summary>
/// <remarks>
/// <para>
/// A tween applies its starting value (<c>ease(0)</c>) the moment it is added, so what it moves is in place before
/// anything is drawn, and advances once per update from the next one. With a delay, it leaves its target alone until
/// the update the delay ends on, which applies the starting value. Its last update applies <c>ease(1)</c> and then
/// calls <c>done</c>. A tween of zero seconds or zero updates applies its final value on the next update.
/// </para>
/// <para>
/// An update whose step is zero, negative, infinite or not a number advances nothing: no value is applied and no tween
/// ends. A callback (<c>apply</c> or <c>done</c>) may add tweens, cancel or complete any tween, its own included, and
/// clear them all.
/// </para>
/// <para>
/// A tween in seconds adds each update's step to its time in single precision, exactly as the loop
/// <c>for (t = 0; t &lt; seconds; t += step)</c> does, and applies <c>ease(t / seconds)</c>: it replaces such a loop value
/// for value. At R updates a second it lasts <c>seconds x R</c> updates, or one more when the steps add up to a hair
/// under the length (0.5 seconds at 60 is 30 updates; 0.9 is 55, the 54th value being a hair under 1). A tween in steps
/// (<see cref="AddSteps"/>) lasts exactly its number of updates.
/// </para>
/// </remarks>
public sealed class Tweener : IStateHashed
{
    private readonly List<Tween> _tweens = [];
    private bool _updating;

    /// <summary>Number of tweens playing or waiting for their delay.</summary>
    public int Count
    {
        get
        {
            int count = 0;
            foreach (var tween in _tweens) if (!tween.Ended) count++;
            return count;
        }
    }

    /// <summary>
    /// Calls <paramref name="apply"/> with eased progress 0..1 every update for <paramref name="duration"/> seconds, then
    /// <paramref name="done"/>. The starting value is applied at once (after <paramref name="delay"/>, when given).
    /// </summary>
    /// <param name="duration">The length in seconds, zero or more; zero applies the final value on the next update.</param>
    /// <param name="apply">Receives the eased progress: 0 at the start, 1 at the end.</param>
    /// <param name="ease">The curve; <see cref="MicroEngine.Ease.OutQuad"/> when null.</param>
    /// <param name="done">Called once, after the final value is applied; not when the tween is cancelled.</param>
    /// <param name="delay">Seconds before the tween starts.</param>
    /// <param name="scaled">
    /// True to advance by the second clock of <see cref="Update(float, float)"/>: in a scene, the step of the world's
    /// clock, so the tween slows down and freezes with the world (an object's squash during a hitstop). False (the
    /// default) keeps the engine's time, which interface and cutscene tweens want.
    /// </param>
    /// <returns>The tween: yield it from a coroutine to wait for it, or keep it to cancel or complete it.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> or <paramref name="delay"/> is negative, infinite or not a number.</exception>
    public Tween Add(float duration, Action<float> apply, Func<float, float>? ease = null, Action? done = null, float delay = 0, bool scaled = false)
    {
        ArgumentNullException.ThrowIfNull(apply);
        if (!(duration >= 0) || float.IsInfinity(duration)) throw new ArgumentOutOfRangeException(nameof(duration), duration, "A length is zero or more seconds.");
        if (!(delay >= 0) || float.IsInfinity(delay)) throw new ArgumentOutOfRangeException(nameof(delay), delay, "A delay is zero or more seconds.");
        return Start(new Tween { Duration = duration, Ease = ease ?? MicroEngine.Ease.OutQuad, Apply = apply, Done = done, Delay = delay, Scaled = scaled });
    }

    /// <summary>
    /// A tween whose length is a number of updates: <paramref name="apply"/> receives the eased value of 1/steps, 2/steps...
    /// on the updates after the one it is added on, and 1 on the last. It counts updates, not time: slow motion does not
    /// stretch it, and an update of zero seconds (a frozen world, for a scaled tween) is not counted.
    /// </summary>
    /// <param name="steps">The length in updates; 0 and 1 both apply the final value on the next update.</param>
    /// <param name="apply">Receives the eased progress.</param>
    /// <param name="ease">The curve; <see cref="MicroEngine.Ease.OutQuad"/> when null.</param>
    /// <param name="done">Called once, after the final value is applied.</param>
    /// <param name="delaySteps">Updates before the tween starts.</param>
    /// <param name="scaled">True to count the updates of the second clock of <see cref="Update(float, float)"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> or <paramref name="delaySteps"/> is negative.</exception>
    public Tween AddSteps(int steps, Action<float> apply, Func<float, float>? ease = null, Action? done = null, int delaySteps = 0, bool scaled = false)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        ArgumentOutOfRangeException.ThrowIfNegative(delaySteps);
        return Start(new Tween { Duration = steps, Ease = ease ?? MicroEngine.Ease.OutQuad, Apply = apply, Done = done, Delay = delaySteps, Scaled = scaled, InSteps = true });
    }

    private Tween Start(Tween tween)
    {
        _tweens.Add(tween);
        if (!(tween.Delay > 0))
        {
            tween.Started = true;
            tween.Apply(tween.Ease(0f));
        }
        return tween;
    }

    /// <summary>Advances every tween by <paramref name="dt"/> seconds and calls the <c>done</c> callback of those that ended, in the order they were added. A scene calls this for its own <see cref="Scene.Tweens"/>.</summary>
    public void Update(float dt) => Update(dt, dt);

    /// <summary>
    /// Advances the tweens on two clocks: <paramref name="dt"/> for the ordinary ones and <paramref name="scaledDt"/> for
    /// those added with <c>scaled: true</c>. A scene gives the engine's step and its world's. A clock whose step is no
    /// time (zero, negative, infinite, not a number) stands still: its tweens apply nothing and do not end.
    /// </summary>
    public void Update(float dt, float scaledDt)
    {
        bool moved = dt > 0 && !float.IsPositiveInfinity(dt), scaledMoved = scaledDt > 0 && !float.IsPositiveInfinity(scaledDt);
        bool outer = _updating;
        _updating = true;
        try
        {
            // A tween added by a callback already holds its starting value: it advances from the next update. The
            // list only grows while this runs: a callback that clears or ends tweens marks them, and the sweep below
            // removes them.
            int count = _tweens.Count;
            for (int i = 0; i < count; i++)
            {
                var t = _tweens[i];
                if (t.Ended || !(t.Scaled ? scaledMoved : moved)) continue;
                if (t.LostEntity()) { t.Ended = true; continue; }
                float step = t.InSteps ? 1 : t.Scaled ? scaledDt : dt;
                if (!t.Started)
                {
                    t.Delay -= step;
                    if (t.Delay > 0) continue;
                    t.Started = true;
                    t.Apply(t.Ease(0f));
                    continue;
                }
                t.Time = MathF.Min(t.Time + step, t.Duration);
                bool last = t.Time >= t.Duration;
                if (last) t.Reached = true;
                t.Apply(t.Ease(last ? 1f : t.Time / t.Duration));
                if (last && !t.Ended)
                {
                    t.Ended = true;
                    t.Done?.Invoke();
                }
            }
        }
        finally { _updating = outer; }
        if (!_updating) _tweens.RemoveAll(static t => t.Ended);
    }

    /// <summary>
    /// Cancels every tween: no final value is applied and no <c>done</c> callback is called. A coroutine waiting for one
    /// goes on. Safe from a callback of one of the tweens.
    /// </summary>
    public void Clear()
    {
        foreach (var tween in _tweens) tween.Ended = true;
        if (!_updating) _tweens.Clear();
    }

    /// <summary>Adds the tweens' state to the per-step state hash: for each tween, its length, time played, delay left, and how it counts.</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Count);
        foreach (var t in _tweens)
        {
            if (t.Ended) continue;
            hash.Add(t.Duration);
            hash.Add(t.Time);
            hash.Add(t.Delay);
            hash.Add(t.Started);
            hash.Add(t.InSteps);
            hash.Add(t.Scaled);
        }
    }
}
