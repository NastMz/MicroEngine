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
    public bool GoalEnabled { get; }
    public bool Completed { get; private set; }
    public static Rect Goal => new(580, 260, 40, 40);

    public GameModel() : this(new PlatformerOptions(TimeSpan.Zero)) { }
    public IReadOnlyList<StaticObstacle> Obstacles { get; }
    public GameModel(PlatformerOptions options) : this(options, []) { }
    public GameModel(PlatformerOptions options, IEnumerable<StaticObstacle> obstacles) : this(options, obstacles, false) { }
    public GameModel(PlatformerOptions options, IEnumerable<StaticObstacle> obstacles, bool goalEnabled)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(obstacles);
        _jumpBufferWindowTicks = options.JumpBufferWindow.Ticks;
        GoalEnabled = goalEnabled;
        var copy = new List<StaticObstacle>();
        foreach (var solid in obstacles)
        {
            if (!StaticObstacle.IsValid(solid.X, solid.Y, solid.Width, solid.Height))
                throw new ArgumentOutOfRangeException(nameof(obstacles));
            if (Overlap(X, 24, solid.X, solid.Width) && Overlap(Y, 32, solid.Y, solid.Height))
                throw new ArgumentException("A static solid overlaps the initial player.", nameof(obstacles));
            copy.Add(solid);
        }
        Obstacles = copy.AsReadOnly();
    }
    public void ClearJumpBuffer() => _pendingJumpTicks = -1;

    public void Update(TimeSpan delta, InputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (delta <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        if (Completed)
        {
            if (input.Pressed.Contains("jump"))
            {
                X = 40; Y = 268; VerticalVelocity = 0; Grounded = true; Completed = false;
                ClearJumpBuffer();
            }
            return; // Reset consumes this input; it cannot also move or launch the player.
        }
        // Age only previous intent. Inputs and contact in this update share one discrete boundary.
        if (_pendingJumpTicks >= 0)
            _pendingJumpTicks = delta.Ticks > _pendingJumpTicks ? -1 : _pendingJumpTicks - delta.Ticks;
        var jumpPressed = input.Pressed.Contains("jump");
        if (jumpPressed && _jumpBufferWindowTicks > 0) _pendingJumpTicks = _jumpBufferWindowTicks;
        var seconds = (float)delta.TotalSeconds;
        var horizontal = (input.IsHeld("right") ? 1 : 0) - (input.IsHeld("left") ? 1 : 0);
        var requestedX = Math.Clamp(X + horizontal * 180 * seconds, 0, 616);
        var resolvedX = requestedX;
        foreach (var solid in Obstacles)
        {
            if (!Overlap(Y, 32, solid.Y, solid.Height)) continue;
            if (requestedX > X && X + 24 <= solid.X && requestedX + 24 >= solid.X)
                resolvedX = Math.Min(resolvedX, solid.X - 24);
            if (requestedX < X && X >= solid.X + solid.Width && requestedX <= solid.X + solid.Width)
                resolvedX = Math.Max(resolvedX, solid.X + solid.Width);
        }
        X = resolvedX;
        // Contact coordinates alone must never re-ground a just-buffered launch.
        Grounded = Grounded && (Y == 268 || Obstacles.Any(solid => Y + 32 == solid.Y && Overlap(X, 24, solid.X, solid.Width)));
        if (Grounded && (jumpPressed || _pendingJumpTicks >= 0)) LaunchJump();
        if (!Grounded)
        {
            VerticalVelocity += 900 * seconds;
            var requestedY = Y + VerticalVelocity * seconds;
            var resolvedY = requestedY;
            var downwardContact = requestedY > Y && Y <= 268 && requestedY >= 268;
            var upwardContact = false;
            if (downwardContact) resolvedY = 268;
            foreach (var solid in Obstacles)
            {
                if (!Overlap(X, 24, solid.X, solid.Width)) continue;
                if (requestedY > Y && Y + 32 <= solid.Y && requestedY + 32 >= solid.Y)
                {
                    resolvedY = Math.Min(resolvedY, solid.Y - 32);
                    downwardContact = true;
                }
                if (requestedY < Y && Y >= solid.Y + solid.Height && requestedY <= solid.Y + solid.Height)
                {
                    resolvedY = Math.Max(resolvedY, solid.Y + solid.Height);
                    upwardContact = true;
                }
            }
            Y = resolvedY;
            if (upwardContact || downwardContact)
            {
                VerticalVelocity = 0;
                Grounded = downwardContact;
                // Consume at contact, without integrating twice or granting airborne eligibility.
                if (Grounded && _pendingJumpTicks >= 0) LaunchJump();
            }
        }
        // Finish only after contact and buffered launch resolution, never while airborne.
        if (GoalEnabled && Grounded && Y == 268 && Overlap(X, 24, Goal.X, Goal.Width) && Overlap(Y, 32, Goal.Y, Goal.Height))
        {
            Completed = true;
            VerticalVelocity = 0;
            ClearJumpBuffer();
        }
    }

    private static bool Overlap(float start, float length, float other, float otherLength)
        => start < other + otherLength && start + length > other;

    private void LaunchJump()
    {
        VerticalVelocity = -300;
        Grounded = false;
        _pendingJumpTicks = -1;
    }
}
