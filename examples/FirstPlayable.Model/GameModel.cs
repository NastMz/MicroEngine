using MicroEngine.Loop;

namespace FirstPlayable.Model;

public sealed class GameModel
{
    private readonly long _jumpBufferWindowTicks;
    private long _pendingJumpTicks = -1;
    public float X { get; private set; } = 40;
    public float Y { get; private set; } = 268;
    public float VerticalVelocity { get; private set; }
    public bool Grounded { get; private set; } = true;

    public GameModel() : this(new PlatformerOptions(TimeSpan.Zero)) { }
    public GameModel(PlatformerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _jumpBufferWindowTicks = options.JumpBufferWindow.Ticks;
    }
    public void ClearJumpBuffer() => _pendingJumpTicks = -1;

    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        // Age only previous intent. Inputs and contact in this update share one discrete boundary.
        if (_pendingJumpTicks >= 0)
            _pendingJumpTicks = delta.Ticks > _pendingJumpTicks ? -1 : _pendingJumpTicks - delta.Ticks;
        var jumpPressed = input.Pressed.Contains("jump");
        if (jumpPressed && _jumpBufferWindowTicks > 0) _pendingJumpTicks = _jumpBufferWindowTicks;
        var seconds = (float)delta.TotalSeconds;
        var horizontal = (input.IsHeld("right") ? 1 : 0) - (input.IsHeld("left") ? 1 : 0);
        X = Math.Clamp(X + horizontal * 180 * seconds, 0, 616);
        if (Grounded && (jumpPressed || _pendingJumpTicks >= 0)) LaunchJump();
        if (!Grounded)
        {
            VerticalVelocity += 900 * seconds;
            Y += VerticalVelocity * seconds;
            if (Y >= 268)
            {
                Y = 268;
                VerticalVelocity = 0;
                Grounded = true;
                // Consume at contact, without integrating twice or granting airborne eligibility.
                if (_pendingJumpTicks >= 0) LaunchJump();
            }
        }
    }

    private void LaunchJump()
    {
        VerticalVelocity = -300;
        Grounded = false;
        _pendingJumpTicks = -1;
    }
}
