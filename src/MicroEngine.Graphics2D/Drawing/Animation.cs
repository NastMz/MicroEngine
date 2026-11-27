namespace MicroEngine.Graphics;

/// <summary>An immutable sequence of sprites played at a fixed rate, or with a duration per frame (Aseprite timing).</summary>
public sealed class AnimationClip
{
    // Times at which each frame ends, for clips with a duration per frame; null for a fixed rate.
    private readonly float[]? _ends;

    /// <summary>The frames in playing order (at least one).</summary>
    public IReadOnlyList<Sprite> Frames { get; }
    /// <summary>Playback rate in frames per second; for a clip with a duration per frame, its average rate.</summary>
    public float FramesPerSecond { get; }
    /// <summary>Whether the clip starts over after its last frame; otherwise it holds the last frame.</summary>
    public bool Loop { get; }
    /// <summary>How long each frame shows, in seconds (all equal for a clip with a fixed rate).</summary>
    public IReadOnlyList<float> FrameDurations { get; }

    /// <summary>Creates a clip whose frames each show for their own time, in seconds; frames and durations are copied.</summary>
    /// <param name="frames">The frames in playing order.</param>
    /// <param name="frameDurations">One positive duration per frame, in seconds.</param>
    /// <param name="loop">Whether the clip repeats.</param>
    /// <exception cref="ArgumentException">No frames, or not one duration per frame.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A duration is not positive.</exception>
    public AnimationClip(IEnumerable<Sprite> frames, IEnumerable<float> frameDurations, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(frameDurations);
        Frames = frames.ToArray();
        var durations = frameDurations.ToArray();
        if (Frames.Count == 0) throw new ArgumentException("A clip needs at least one frame.", nameof(frames));
        if (durations.Length != Frames.Count) throw new ArgumentException($"{Frames.Count} frames need {Frames.Count} durations, got {durations.Length}.", nameof(frameDurations));
        _ends = new float[durations.Length];
        float total = 0;
        for (int i = 0; i < durations.Length; i++)
        {
            if (!(durations[i] > 0) || float.IsInfinity(durations[i])) throw new ArgumentOutOfRangeException(nameof(frameDurations), durations[i], "Frame durations are positive.");
            _ends[i] = total += durations[i];
        }
        FrameDurations = durations;
        FramesPerSecond = Frames.Count / total;
        Loop = loop;
    }

    /// <summary>Creates a clip; the frames are copied.</summary>
    /// <param name="frames">The frames, for example from <see cref="SpriteAtlas.Frames"/>.</param>
    /// <param name="framesPerSecond">Playback rate; must be positive.</param>
    /// <param name="loop">Whether the clip repeats.</param>
    /// <exception cref="ArgumentException"><paramref name="frames"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="framesPerSecond"/> is not positive.</exception>
    public AnimationClip(IEnumerable<Sprite> frames, float framesPerSecond, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(frames);
        Frames = frames.ToArray();
        if (Frames.Count == 0) throw new ArgumentException("A clip needs at least one frame.", nameof(frames));
        if (framesPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        FramesPerSecond = framesPerSecond;
        Loop = loop;
        FrameDurations = Enumerable.Repeat(1 / framesPerSecond, Frames.Count).ToArray();
    }

    /// <summary>Length of one pass through the frames, in seconds.</summary>
    public float Duration => _ends is { } ends ? ends[^1] : Frames.Count / FramesPerSecond;

    /// <summary>Whether each frame has a duration of its own (otherwise the clip plays at one rate).</summary>
    internal bool HasFrameDurations => _ends is not null;

    /// <summary>The frame at <paramref name="time"/> of a single pass, whether or not the clip loops (a one-shot effect).</summary>
    internal int FrameIndexOnce(float time)
    {
        if (_ends is not { } ends) return Math.Clamp((int)(time * FramesPerSecond), 0, Frames.Count - 1);
        int found = Array.BinarySearch(ends, MathF.Max(time, 0));
        return Math.Min(found >= 0 ? found + 1 : ~found, Frames.Count - 1);
    }

    /// <summary>The frame shown <paramref name="time"/> seconds after the clip started (0 or more).</summary>
    public Sprite FrameAt(float time) => Frames[FrameIndexAt(time)];

    /// <summary>
    /// The frame shown at <paramref name="time"/> by an instance that started <paramref name="frameOffset"/> frames
    /// into the clip: the links of a chain, a row of torches or of coins share one clock and one clip, each a frame
    /// (or a fraction of one) apart.
    /// </summary>
    /// <param name="time">Seconds on the clock the instances share.</param>
    /// <param name="frameOffset">How many frames ahead this instance is; see <see cref="FrameIndexAt"/>.</param>
    public Sprite FrameAt(float time, float frameOffset) => Frames[FrameIndexAt(time, frameOffset)];

    /// <summary>
    /// The index in <see cref="Frames"/> of the frame shown at <paramref name="time"/>, for an instance that started
    /// <paramref name="frameOffset"/> frames into the clip.
    /// </summary>
    /// <param name="time">Seconds since the clip started.</param>
    /// <param name="frameOffset">
    /// How many frames ahead this instance is: 1 shows the second frame at time 0, 0.5 is half a frame ahead, a negative
    /// value is behind. At a fixed rate the index is <c>time * FramesPerSecond + frameOffset</c> rounded down (for a
    /// time and an offset of 0 or more, <c>(int)(time * FramesPerSecond + frameOffset)</c>), wrapped by a looping clip
    /// and held at the ends by one that plays once; with a duration per frame, the offset counts frames of their own
    /// lengths. A value that is not a number is 0.
    /// </param>
    /// <remarks>
    /// A time before the start counts backwards the same way an offset does: half a frame before 0, a looping clip shows
    /// its last frame, and a clip that plays once its first.
    /// </remarks>
    public int FrameIndexAt(float time, float frameOffset = 0)
    {
        if (float.IsNaN(frameOffset)) frameOffset = 0;
        int count = Frames.Count;
        if (_ends is { } ends)
        {
            if (frameOffset != 0) time += OffsetSeconds(ends, frameOffset);
            float t = Loop ? time % ends[^1] : time;
            if (t < 0) t = Loop ? t + ends[^1] : 0;
            int found = Array.BinarySearch(ends, t);
            // An exact end time belongs to the next frame; otherwise BinarySearch gives the first end after t.
            int at = found >= 0 ? found + 1 : ~found;
            return Math.Min(at, count - 1);
        }
        // Rounded down, so time and offset are one scale across 0. From 0 on that is (int)(time * rate + offset), and
        // without an offset (int)(time * rate), as it always was: adding 0 changes no bit.
        float position = time * FramesPerSecond + frameOffset;
        if (!(position > int.MinValue) || position >= int.MaxValue) position = position > 0 ? int.MaxValue - 256 : 0;
        int index = (int)MathF.Floor(position);
        if (!Loop) return Math.Clamp(index, 0, count - 1);
        index %= count;
        return index < 0 ? index + count : index;
    }

    /// <summary>The time, from the start of a pass, at which the frame <paramref name="frameOffset"/> frames in begins (a fraction lies inside that frame).</summary>
    private float OffsetSeconds(float[] ends, float frameOffset)
    {
        int count = ends.Length;
        float whole = MathF.Floor(frameOffset), fraction = frameOffset - whole;
        // Whole passes shift a looping clip by nothing; one that plays once counts them as time.
        float passes = MathF.Floor(whole / count);
        int frame = (int)(whole - passes * count);
        float start = frame == 0 ? 0 : ends[frame - 1];
        return (Loop ? 0 : passes * ends[^1]) + start + fraction * (ends[frame] - start);
    }
}

/// <summary>Plays named clips; switching to the clip already playing does not restart it.</summary>
public sealed class Animator
{
    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.Ordinal);
    /// <summary>Name of the clip playing, or null before the first clip is added.</summary>
    public string? CurrentName { get; private set; }
    /// <summary>The clip playing, or null before the first clip is added.</summary>
    public AnimationClip? Current { get; private set; }
    /// <summary>Seconds played in the current clip, scaled by <see cref="Speed"/>; reset to 0 when another clip starts.</summary>
    public float Time { get; private set; }
    /// <summary>Playback speed multiplier: 1 by default, 0 pauses, 2 plays twice as fast.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Set to a negative value or NaN (clips do not play backwards).</exception>
    public float Speed
    {
        get => _speed;
        set => _speed = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Animation speed must be 0 or more.");
    }
    private float _speed = 1f;
    /// <summary>Whether a non-looping clip has reached its end. Always false for looping clips.</summary>
    public bool Finished => Current is { Loop: false } c && Time >= c.Duration;

    /// <summary>Registers <paramref name="clip"/> under <paramref name="name"/> (replacing a clip with that name). The first clip added starts playing.</summary>
    /// <returns>This animator, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clip"/> is null.</exception>
    public Animator Add(string name, AnimationClip clip)
    {
        _clips[name] = clip ?? throw new ArgumentNullException(nameof(clip));
        if (Current is null) Play(name);
        return this;
    }

    /// <summary>Switches to the clip <paramref name="name"/> from its first frame. Playing the current clip again does nothing unless <paramref name="restart"/> is true.</summary>
    /// <exception cref="KeyNotFoundException">No clip has that name.</exception>
    public void Play(string name, bool restart = false)
    {
        if (!restart && name == CurrentName) return;
        Current = _clips.TryGetValue(name, out var clip) ? clip : throw new KeyNotFoundException($"Clip '{name}' not found.");
        CurrentName = name;
        Time = 0;
    }

    /// <summary>Advances the current clip by <paramref name="dt"/> seconds times <see cref="Speed"/>.</summary>
    public void Update(float dt) => Time += dt * Speed;
    /// <summary>The frame to draw now.</summary>
    /// <exception cref="InvalidOperationException">No clip was added.</exception>
    public Sprite Frame => (Current ?? throw new InvalidOperationException("No clip is playing.")).FrameAt(Time);
}
