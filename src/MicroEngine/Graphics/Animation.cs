namespace MicroEngine.Graphics;

/// <summary>An immutable sequence of sprites played at a fixed rate.</summary>
public sealed class AnimationClip
{
    /// <summary>The frames in playing order (at least one).</summary>
    public IReadOnlyList<Sprite> Frames { get; }
    /// <summary>Playback rate in frames per second.</summary>
    public float FramesPerSecond { get; }
    /// <summary>Whether the clip starts over after its last frame; otherwise it holds the last frame.</summary>
    public bool Loop { get; }

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
    }

    /// <summary>Length of one pass through the frames, in seconds.</summary>
    public float Duration => Frames.Count / FramesPerSecond;

    /// <summary>The frame shown <paramref name="time"/> seconds after the clip started (0 or more).</summary>
    public Sprite FrameAt(float time)
    {
        int index = (int)(time * FramesPerSecond);
        return Frames[Loop ? index % Frames.Count : Math.Min(index, Frames.Count - 1)];
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
