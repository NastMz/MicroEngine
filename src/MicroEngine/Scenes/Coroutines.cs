using System.Collections;

namespace MicroEngine.Scenes;

/// <summary>Yield instructions understood by <see cref="CoroutineRunner"/>.</summary>
public static class Wait
{
    public static WaitSeconds Seconds(float seconds) => new(seconds);
    public static WaitUntil Until(Func<bool> condition) => new(condition);
}

public sealed record WaitSeconds(float Seconds);
public sealed record WaitUntil(Func<bool> Condition);

/// <summary>
/// Runs iterator-based sequences one step per update. A coroutine may yield null (next update),
/// <see cref="Wait.Seconds"/>, <see cref="Wait.Until"/> or another IEnumerator (awaited as a sub-routine).
/// Ideal for cutscenes, dialogue flow and timed effects without hand-written state machines.
/// </summary>
public sealed class CoroutineRunner
{
    private sealed class Routine(IEnumerator root)
    {
        public readonly Stack<IEnumerator> Stack = new([root]);
        public float Wait;
        public Func<bool>? Until;
        public bool Cancelled;
    }

    private readonly List<Routine> _routines = [];
    private readonly List<Routine> _added = [];

    public int Count => _routines.Count(r => !r.Cancelled) + _added.Count;

    public CoroutineHandle Start(IEnumerator routine)
    {
        ArgumentNullException.ThrowIfNull(routine);
        var r = new Routine(routine);
        _added.Add(r);
        return new CoroutineHandle(() => r.Cancelled = true, () => r.Cancelled || r.Stack.Count == 0);
    }

    public void StopAll()
    {
        foreach (var r in _routines) r.Cancelled = true;
        _added.Clear();
    }

    public void Update(float dt)
    {
        _routines.AddRange(_added);
        _added.Clear();
        foreach (var r in _routines)
        {
            if (r.Cancelled) continue;
            if (r.Wait > 0) { r.Wait -= dt; if (r.Wait > 0) continue; }
            if (r.Until is not null) { if (!r.Until()) continue; r.Until = null; }
            while (r.Stack.Count > 0 && !r.Cancelled)
            {
                var top = r.Stack.Peek();
                if (!top.MoveNext()) { r.Stack.Pop(); continue; }
                switch (top.Current)
                {
                    case null: break;
                    case WaitSeconds w: r.Wait = w.Seconds; break;
                    case WaitUntil u: if (!u.Condition()) r.Until = u.Condition; else continue; break;
                    case IEnumerator nested: r.Stack.Push(nested); continue;
                    default: throw new InvalidOperationException($"Unsupported yield value {top.Current.GetType().Name}.");
                }
                break;
            }
        }
        _routines.RemoveAll(r => r.Cancelled || r.Stack.Count == 0);
    }
}

public sealed class CoroutineHandle(Action cancel, Func<bool> finished)
{
    public bool IsFinished => finished();
    public void Cancel() => cancel();
}

/// <summary>Simple value tweens driven by update time.</summary>
public sealed class Tweener
{
    private sealed class Tween
    {
        public required float Duration;
        public required Func<float, float> Ease;
        public required Action<float> Apply;
        public Action? Done;
        public float Time;
        public float Delay;
    }

    private readonly List<Tween> _tweens = [];

    public int Count => _tweens.Count;

    /// <summary>Calls <paramref name="apply"/> with eased progress 0..1 every update for <paramref name="duration"/> seconds.</summary>
    public void Add(float duration, Action<float> apply, Func<float, float>? ease = null, Action? done = null, float delay = 0)
    {
        ArgumentNullException.ThrowIfNull(apply);
        _tweens.Add(new Tween { Duration = Math.Max(duration, 1e-4f), Ease = ease ?? MicroEngine.Ease.OutQuad, Apply = apply, Done = done, Delay = delay });
    }

    public void Update(float dt)
    {
        for (int i = 0; i < _tweens.Count; i++)
        {
            var t = _tweens[i];
            if (t.Delay > 0) { t.Delay -= dt; continue; }
            t.Time = MathF.Min(t.Time + dt, t.Duration);
            t.Apply(t.Ease(t.Time / t.Duration));
        }
        for (int i = _tweens.Count - 1; i >= 0; i--)
        {
            var t = _tweens[i];
            if (t.Delay <= 0 && t.Time >= t.Duration) { _tweens.RemoveAt(i); t.Done?.Invoke(); }
        }
    }

    public void Clear() => _tweens.Clear();
}
