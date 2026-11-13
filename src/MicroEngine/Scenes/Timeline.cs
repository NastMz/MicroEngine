using MicroEngine.Kernel;

namespace MicroEngine.Scenes;

/// <summary>
/// A playhead that drives several <see cref="Track{T}"/>s together and raises named events at set times: a sequence
/// of movements with the moments the game reacts to (a footstep, the frame a swing lands). Advance it with
/// <see cref="Update"/>, like an <c>Animator</c>; several timelines over different values play as layers.
/// </summary>
public sealed class Timeline : IStateHashed
{
    private readonly List<Action<float>> _tracks = [];
    private readonly List<(float Time, string Name)> _events = [];
    private float _speed = 1f;
    // Whether an update already played from the current place, so an event exactly there has fired.
    private bool _started;
    // Whether a timeline that does not loop applied its last values.
    private bool _ended;
    // Counts Seek calls, so an event handler that moves the playhead ends that update's events.
    private int _moves;

    /// <summary>Creates a timeline at time 0.</summary>
    /// <param name="duration">Its length in seconds; positive.</param>
    /// <param name="loop">Whether it starts over at the end; otherwise it stops there.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is not a positive number.</exception>
    public Timeline(float duration, bool loop = false)
    {
        if (!(duration > 0) || float.IsInfinity(duration)) throw new ArgumentOutOfRangeException(nameof(duration), duration, "A timeline's duration is positive.");
        Duration = duration;
        Loop = loop;
    }

    /// <summary>Length of one pass, in seconds.</summary>
    public float Duration { get; }
    /// <summary>Whether the timeline starts over at the end; otherwise it stops there.</summary>
    public bool Loop { get; }
    /// <summary>The playhead, in seconds from the start: 0 to <see cref="Duration"/>.</summary>
    public float Time { get; private set; }
    /// <summary>Playback speed multiplier: 1 by default, 0 pauses, 2 plays twice as fast.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Set to a negative value or NaN (timelines do not play backwards).</exception>
    public float Speed
    {
        get => _speed;
        set => _speed = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Timeline speed must be 0 or more.");
    }
    /// <summary>Whether a timeline that does not loop reached its end. Always false for a looping one.</summary>
    public bool Finished => !Loop && Time >= Duration;

    /// <summary>
    /// Raised with an event's name when the playhead reaches its time, after the tracks were applied for that update.
    /// Events fire in time order (in the order they were added for equal times), once per pass.
    /// </summary>
    public event Action<string>? Reached;

    /// <summary>Plays <paramref name="track"/> on this timeline: each update, <paramref name="apply"/> receives its value at the playhead.</summary>
    /// <returns>This timeline, for chaining.</returns>
    public Timeline Add<T>(Track<T> track, Action<T> apply)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(apply);
        _tracks.Add(time => apply(track.At(time)));
        return this;
    }

    /// <summary>
    /// Adds the event <paramref name="name"/> at <paramref name="time"/> seconds. An event at 0 fires in the first
    /// update and, when looping, each time the timeline starts over.
    /// </summary>
    /// <returns>This timeline, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="time"/> is outside 0..<see cref="Duration"/>.</exception>
    public Timeline At(float time, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!(time >= 0 && time <= Duration)) throw new ArgumentOutOfRangeException(nameof(time), time, $"An event lies within the timeline (0 to {Duration} s).");
        int index = _events.Count;
        while (index > 0 && _events[index - 1].Time > time) index--;
        _events.Insert(index, (time, name));
        return this;
    }

    /// <summary>
    /// Advances the playhead by <paramref name="dt"/> seconds times <see cref="Speed"/>, applies every track and raises
    /// the events it reached; an update longer than a looping timeline raises the events of every pass it covered. With
    /// nothing to advance (a pause, 0 seconds) it only applies the tracks. A finished timeline does nothing, so the
    /// game may change the values afterwards.
    /// </summary>
    public void Update(float dt)
    {
        if (_ended) return;
        float advance = dt * _speed;
        if (!(advance > 0)) { Apply(); return; }

        float from = Time, now = from + advance;
        bool inclusive = !_started;
        int passes = 0;
        if (Loop) while (now >= Duration) { now -= Duration; passes++; }
        else if (now >= Duration) { now = Duration; _ended = true; }
        _started = true;
        Time = now;
        Apply();

        int moves = _moves;
        for (int pass = 0; pass < passes; pass++)
        {
            if (!Raise(from, Duration, inclusive, moves)) return;
            from = 0;
            inclusive = true;
        }
        Raise(from, now, inclusive, moves);
    }

    /// <summary>
    /// Moves the playhead to <paramref name="time"/> seconds (kept within 0..<see cref="Duration"/>) and applies the
    /// tracks there, raising no events; <c>Seek(0)</c> plays the timeline again. The next update raises the events
    /// from that time on, one exactly there included.
    /// </summary>
    public void Seek(float time)
    {
        Time = Math.Clamp(time, 0, Duration);
        _started = false;
        _ended = false;
        _moves++;
        Apply();
    }

    /// <summary>Adds the timeline's state to the per-step state hash: its playhead, its speed and whether it started playing from there.</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Time);
        hash.Add(_speed);
        hash.Add(_started);
    }

    private void Apply()
    {
        foreach (var track in _tracks) track(Time);
    }

    // Raises the events in (from, to], or [from, to] when inclusive; false when a handler moved the playhead.
    private bool Raise(float from, float to, bool inclusive, int moves)
    {
        if (Reached is null) return true;
        for (int i = 0; i < _events.Count; i++)
        {
            var (time, name) = _events[i];
            if (time > to) break;
            if (inclusive ? time < from : time <= from) continue;
            Reached?.Invoke(name);
            if (_moves != moves) return false;
        }
        return true;
    }
}
