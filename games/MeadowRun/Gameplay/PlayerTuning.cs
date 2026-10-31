namespace MeadowRun.Gameplay;

/// <summary>The controls a player can apply in one fixed step, independent of devices.</summary>
public readonly record struct PlayerInput(bool Left, bool Right, bool Down, bool JumpPressed, bool JumpHeld, bool RunHeld, bool RunPressed)
{
    public int Axis => (Right ? 1 : 0) - (Left ? 1 : 0);
}

/// <summary>
/// Every number that shapes how Pip moves. Units are pixels and seconds. Kept in one place so
/// feel tuning never hunts through gameplay code, and so tests can assert the rules they encode.
/// </summary>
public static class Tuning
{
    public const float WalkSpeed = 92f;
    public const float RunSpeed = 152f;
    public const float GroundAccel = 520f;
    public const float RunAccel = 600f;
    public const float GroundFriction = 620f;
    public const float SkidDecel = 1500f;
    public const float AirAccel = 430f;
    public const float AirDrag = 140f;

    public const float JumpSpeed = 302f;
    /// <summary>Extra jump speed at full run speed, scaled by horizontal speed.</summary>
    public const float RunJumpBonus = 26f;
    public const float RiseGravity = 640f;
    /// <summary>Gravity while rising after jump is released: this is what makes jump height variable.</summary>
    public const float ReleaseGravity = 1900f;
    public const float ApexGravity = 380f;
    public const float ApexThreshold = 48f;
    public const float FallGravity = 1050f;
    public const float TerminalVelocity = 270f;

    public const float CoyoteTime = 0.1f;
    public const float JumpBuffer = 0.12f;
    /// <summary>Max horizontal nudge when the head clips a ceiling corner.</summary>
    public const int CornerCorrection = 4;
    public const float DropThroughTime = 0.22f;

    public const float StompBounce = 230f;
    public const float StompBounceHeld = 330f;
    public const float SpringBounce = 350f;
    public const float SpringBounceHeld = 412f;
    public const float HurtInvulnerability = 1.6f;
    /// <summary>A stomp counts when Pip's feet were at most this far below the enemy top last step.</summary>
    public const float StompTolerance = 6f;
}
