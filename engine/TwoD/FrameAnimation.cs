using MicroEngine.Loop;
namespace MicroEngine.TwoD;

/// <summary>Looping frame selection only; no rendering, simulation state or native ownership.</summary>
public sealed class FrameAnimation
{
    private readonly Rect[] _frames;
    private readonly long _duration;
    private long _phase;
    private int _index;
    public Rect Current => _frames[_index];
    public FrameAnimation(IEnumerable<Rect> frames, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        _frames = frames.ToArray();
        if (_frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));
        foreach (var frame in _frames)
        {
            StaticWorld.Validate(frame);
            if (frame.X < 0 || frame.Y < 0 || frame.X % 1 != 0 || frame.Y % 1 != 0 || frame.Width % 1 != 0 || frame.Height % 1 != 0)
                throw new ArgumentOutOfRangeException(nameof(frames));
        }
        _duration = duration.Ticks;
    }
    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        long frames = elapsed.Ticks / _duration;
        long remainder = elapsed.Ticks % _duration;
        if (remainder >= _duration - _phase) { frames++; _phase = remainder - (_duration - _phase); }
        else _phase += remainder;
        _index = (int)((_index + frames % _frames.Length) % _frames.Length);
    }
    public void Reset() { _phase = 0; _index = 0; }
}
