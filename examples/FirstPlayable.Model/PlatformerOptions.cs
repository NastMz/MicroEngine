namespace FirstPlayable.Model;

/// <summary>Per-controller gameplay choices, never engine-wide movement rules.</summary>
public sealed class PlatformerOptions
{
    public TimeSpan JumpBufferWindow { get; }

    public PlatformerOptions(TimeSpan jumpBufferWindow)
    {
        if (jumpBufferWindow < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(jumpBufferWindow));
        JumpBufferWindow = jumpBufferWindow;
    }
}
