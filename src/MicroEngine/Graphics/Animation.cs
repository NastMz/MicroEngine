namespace MicroEngine.Graphics;

/// <summary>An immutable sequence of sprites played at a fixed rate.</summary>
public sealed class AnimationClip
{
    public IReadOnlyList<Sprite> Frames { get; }
    public float FramesPerSecond { get; }
    public bool Loop { get; }

    public AnimationClip(IEnumerable<Sprite> frames, float framesPerSecond, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(frames);
        Frames = frames.ToArray();
        if (Frames.Count == 0) throw new ArgumentException("A clip needs at least one frame.", nameof(frames));
        if (framesPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        FramesPerSecond = framesPerSecond;
        Loop = loop;
    }

    public float Duration => Frames.Count / FramesPerSecond;

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
    public string? CurrentName { get; private set; }
    public AnimationClip? Current { get; private set; }
    public float Time { get; private set; }
    public float Speed { get; set; } = 1f;
    public bool Finished => Current is { Loop: false } c && Time >= c.Duration;

    public Animator Add(string name, AnimationClip clip)
    {
        _clips[name] = clip ?? throw new ArgumentNullException(nameof(clip));
        if (Current is null) Play(name);
        return this;
    }

    public void Play(string name, bool restart = false)
    {
        if (!restart && name == CurrentName) return;
        Current = _clips.TryGetValue(name, out var clip) ? clip : throw new KeyNotFoundException($"Clip '{name}' not found.");
        CurrentName = name;
        Time = 0;
    }

    public void Update(float dt) => Time += dt * Speed;
    public Sprite Frame => (Current ?? throw new InvalidOperationException("No clip is playing.")).FrameAt(Time);
}
