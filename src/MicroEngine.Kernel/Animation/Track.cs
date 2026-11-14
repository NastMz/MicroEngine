namespace MicroEngine;

/// <summary>One key of a <see cref="Track{T}"/>: the value the track has at a time.</summary>
/// <typeparam name="T">The type of the animated value.</typeparam>
/// <param name="Time">When the track reaches <paramref name="Value"/>, in seconds.</param>
/// <param name="Value">The value at <paramref name="Time"/>.</param>
/// <param name="Ease">
/// The curve of the way from the previous key to this one (an <see cref="MicroEngine.Ease"/> function or your own);
/// null is linear. The first key's curve is not used.
/// </param>
public readonly record struct Keyframe<T>(float Time, T Value, Func<float, float>? Ease = null);

/// <summary>
/// An immutable value that changes over time through keys: <see cref="At"/> gives the value at any time, blended
/// between the two keys around it. It holds no playhead, so one track can be shared by everything that moves the same
/// way; a <c>Timeline</c> plays tracks, or call <see cref="At"/> with a time of your own.
/// </summary>
/// <typeparam name="T">The type of the animated value.</typeparam>
public sealed class Track<T>
{
    private readonly Keyframe<T>[] _keys;
    private readonly Func<T, T, float, T> _blend;
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<Keyframe<T>> _readOnlyKeys;

    /// <summary>Creates a track from its keys (copied) and the function that blends two values.</summary>
    /// <param name="blend">
    /// Returns the value between its first argument (at 0) and its second (at 1), for example <see cref="MathX.Lerp"/>.
    /// It receives the eased progress, which is below 0 or above 1 where a curve overshoots.
    /// </param>
    /// <param name="keys">The keys, in order of time, each later than the one before: keys are not sorted, and two keys cannot share a time.</param>
    /// <exception cref="ArgumentException">No keys, a time that is infinite or not a number, or times that do not increase.</exception>
    public Track(Func<T, T, float, T> blend, params Keyframe<T>[] keys)
    {
        ArgumentNullException.ThrowIfNull(blend);
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Length == 0) throw new ArgumentException("A track needs at least one key.", nameof(keys));
        for (int i = 0; i < keys.Length; i++)
        {
            if (!float.IsFinite(keys[i].Time)) throw new ArgumentException($"Key {i} has no finite time.", nameof(keys));
            if (i > 0 && !(keys[i].Time > keys[i - 1].Time))
                throw new ArgumentException($"Key {i} (at {keys[i].Time} s) is not after key {i - 1} (at {keys[i - 1].Time} s).", nameof(keys));
        }
        _blend = blend;
        _keys = [.. keys];
        _readOnlyKeys = Array.AsReadOnly(_keys);
    }

    /// <summary>The keys, in order of time.</summary>
    public IReadOnlyList<Keyframe<T>> Keys => _readOnlyKeys;

    /// <summary>The time of the last key, in seconds.</summary>
    public float Duration => _keys[^1].Time;

    /// <summary>
    /// The value at <paramref name="time"/> seconds: the first key's value before it, the last key's value after it, a
    /// key's own value at its time (whatever its curve), and between two keys their blend along the later key's
    /// curve. A time that is not a number gives the first key's value.
    /// </summary>
    public T At(float time)
    {
        var keys = _keys;
        if (!(time > keys[0].Time)) return keys[0].Value;
        if (time >= keys[^1].Time) return keys[^1].Value;
        // The last key at or before the time; the checks above leave at least one key after it.
        int low = 0, high = keys.Length - 1;
        while (high - low > 1)
        {
            int middle = (low + high) / 2;
            if (keys[middle].Time <= time) low = middle; else high = middle;
        }
        ref readonly var from = ref keys[low];
        ref readonly var to = ref keys[high];
        if (time == from.Time) return from.Value;
        float progress = (time - from.Time) / (to.Time - from.Time);
        return _blend(from.Value, to.Value, to.Ease is { } ease ? ease(progress) : progress);
    }
}

/// <summary>Creates the usual kinds of <see cref="Track{T}"/>.</summary>
public static class Track
{
    /// <summary>A track of numbers (a position on one axis, an angle, a scale, an opacity), blended linearly.</summary>
    /// <param name="keys">The keys, in order of time.</param>
    /// <exception cref="ArgumentException">No keys, a time that is infinite or not a number, or times that do not increase.</exception>
    public static Track<float> Float(params Keyframe<float>[] keys) => new(MathX.Lerp, keys);

    /// <summary>A track of points or sizes, blended linearly.</summary>
    /// <param name="keys">The keys, in order of time.</param>
    /// <exception cref="ArgumentException">No keys, a time that is infinite or not a number, or times that do not increase.</exception>
    public static Track<System.Numerics.Vector2> Vector2(params Keyframe<System.Numerics.Vector2>[] keys) =>
        new(static (a, b, t) => a + (b - a) * t, keys);

    /// <summary>A track of colours, blended per channel; a curve that overshoots stops at the key's colour.</summary>
    /// <param name="keys">The keys, in order of time.</param>
    /// <exception cref="ArgumentException">No keys, a time that is infinite or not a number, or times that do not increase.</exception>
    public static Track<MicroEngine.Color> Color(params Keyframe<MicroEngine.Color>[] keys) => new(MicroEngine.Color.Lerp, keys);

    /// <summary>
    /// A track that holds each key's value until the next key, for values with nothing in between: a sprite, a name, a
    /// flag. The keys' curves are not used.
    /// </summary>
    /// <param name="keys">The keys, in order of time.</param>
    /// <exception cref="ArgumentException">No keys, a time that is infinite or not a number, or times that do not increase.</exception>
    public static Track<T> Step<T>(params Keyframe<T>[] keys) => new(static (a, _, _) => a, keys);
}
