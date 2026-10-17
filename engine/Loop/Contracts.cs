using System.Collections.Frozen;

namespace MicroEngine.Loop;

public sealed class FrameApiException : InvalidOperationException
{
    internal FrameApiException(string message) : base(message) { }
}

public sealed class LoopOptions
{
    public TimeSpan Step { get; }
    public int MaxUpdates { get; }
    public TimeSpan MaxElapsed { get; }
    public int MaxPendingInputs { get; }

    public LoopOptions(TimeSpan step, int maxUpdates, TimeSpan maxElapsed, int maxPendingInputs = 256)
    {
        if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(step));
        if (maxUpdates <= 0) throw new ArgumentOutOfRangeException(nameof(maxUpdates));
        if (maxElapsed <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maxElapsed));
        if (maxPendingInputs <= 0) throw new ArgumentOutOfRangeException(nameof(maxPendingInputs));
        Step = step;
        MaxUpdates = maxUpdates;
        MaxElapsed = maxElapsed;
        MaxPendingInputs = maxPendingInputs;
    }
}

public sealed class InputSnapshot
{
    public IReadOnlySet<string> Held { get; }
    public IReadOnlyList<string> Pressed { get; }

    public InputSnapshot(IEnumerable<string> held, IEnumerable<string> pressed)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(pressed);
        var heldCopy = held.ToArray();
        var pressedCopy = pressed.ToArray();
        foreach (var action in heldCopy.Concat(pressedCopy)) ArgumentException.ThrowIfNullOrWhiteSpace(action);
        Held = heldCopy.ToFrozenSet(StringComparer.Ordinal);
        Pressed = Array.AsReadOnly(pressedCopy);
    }

    public bool IsHeld(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return Held.Contains(action);
    }
}

public readonly record struct PlatformFrame(TimeSpan Elapsed, InputSnapshot Input, bool Active = true, bool CloseRequested = false);
public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255);
public readonly record struct Rect(float X, float Y, float Width, float Height);

public interface ICanvas
{
    void Clear(Rgba color);
    void Fill(Rect rectangle, Rgba color);
}

/// <summary>Optional provider-owned default-face labels, not a general typography API.</summary>
public interface ILabelCanvas : ICanvas
{
    void Label(string text, int x, int y, int pixelSize, Rgba color);
}

public interface IFramePlatform : IDisposable
{
    PlatformFrame Poll();
    void Begin();
    ICanvas Canvas { get; }
    void End();
}

public sealed class FrameControl
{
    private readonly int _owner = Environment.CurrentManagedThreadId;
    internal bool Stopped { get; private set; }
    internal FrameControl() { }

    public void RequestStop()
    {
        if (Environment.CurrentManagedThreadId != _owner)
            throw new FrameApiException("Frame control belongs to its creating thread.");
        Stopped = true;
    }
}

public sealed class UpdateFrame
{
    public TimeSpan Delta { get; }
    public InputSnapshot Input { get; }
    public FrameControl Control { get; }
    internal UpdateFrame(TimeSpan delta, InputSnapshot input, FrameControl control)
        => (Delta, Input, Control) = (delta, input, control);
}

public sealed class PresentationFrame
{
    public double Alpha { get; }
    public TimeSpan DiscardedTime { get; }
    public FrameControl Control { get; }
    internal PresentationFrame(double alpha, TimeSpan discardedTime, FrameControl control)
        => (Alpha, DiscardedTime, Control) = (alpha, discardedTime, control);
}
