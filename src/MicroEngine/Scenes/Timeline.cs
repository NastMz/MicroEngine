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
    // The passes of a looping timeline whose events one update raises; a longer update skips the passes in between.
    private const int MaxPassesPerUpdate = 1000;

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
    /// <exception cref="ArgumentOutOfRangeException">Set to a negative value, infinity or NaN (timelines do not play backwards).</exception>
    public float Speed
    {
        get => _speed;
        set => _speed = value >= 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Timeline speed must be 0 or more.");
    }
    /// <summary>
    /// Whether a timeline that does not loop is at its end, played or moved there with <see cref="Seek"/>; it stays
    /// finished until <see cref="Seek"/> moves it back. Always false for a looping one.
    /// </summary>
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
    /// update and, when looping, each time the timeline starts over; an event at <see cref="Duration"/> fires when
    /// the timeline ends and, when looping, at the end of each pass, before the events at 0 of the next one.
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
    /// the events it reached; an update longer than a looping timeline raises the events of every pass it covered, up
    /// to 1,000 passes (a longer update skips the passes in between and ends at the same place). With nothing to
    /// advance (a pause, 0 seconds) it only applies the tracks and raises nothing. A finished timeline does nothing,
    /// so the game may change the values afterwards.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dt"/> is negative, infinite or NaN (timelines do not play backwards).</exception>
    public void Update(float dt)
    {
        if (!(dt >= 0) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt), dt, "A timeline advances by 0 seconds or more.");
        if (_ended) return;
        // In double precision, so a long step keeps its place inside the pass and never overflows.
        double total = Time + (double)dt * _speed;
        if (!(total > Time)) { Apply(); return; }

        float from = Time, now = (float)total;
        bool inclusive = !_started;
        int passes = 0;
        if (Loop)
        {
            double covered = Math.Floor(total / Duration);
            now = (float)(total - covered * Duration);
            // The place may round up to the end of the pass, which is the start of the next one.
            if (!(now < Duration)) { now = 0; covered++; }
            passes = (int)Math.Min(covered, MaxPassesPerUpdate);
        }
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
    /// Moves the playhead to <paramref name="time"/> seconds (kept within 0..<see cref="Duration"/>, on a looping
    /// timeline too) and applies the tracks there, raising no events, neither the ones it jumps over nor one at that
    /// time; <c>Seek(0)</c> plays the timeline again. The next update that advances raises the events from that time
    /// on, one exactly there included. Moving a timeline that does not loop to its end finishes it: the tracks are
    /// applied there and nothing more plays, the events at the end included, until it is moved back.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="time"/> is NaN.</exception>
    public void Seek(float time)
    {
        if (float.IsNaN(time)) throw new ArgumentOutOfRangeException(nameof(time), time, "A timeline cannot move to a time that is not a number.");
        Time = Math.Clamp(time, 0, Duration);
        _ended = !Loop && Time >= Duration;
        // A finished timeline has nothing left to raise, as if it had played to its end.
        _started = _ended;
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
