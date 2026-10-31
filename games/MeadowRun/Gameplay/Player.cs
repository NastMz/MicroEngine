using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MicroEngine;
using MicroEngine.World;

namespace MeadowRun.Gameplay;

/// <summary>Something Pip can stand on that moves: it reports its bounds and its movement this step.</summary>
public interface IRideable
{
    RectF Bounds { get; }
    Vector2 Delta { get; }
    void StoodOn();
}

/// <summary>What happened to Pip during one step; the stage turns these into sound and particles.</summary>
[Flags]
public enum PlayerEvents
{
    None = 0,
    Jumped = 1,
    Landed = 2,
    HeadBump = 4,
    Skid = 8,
    DroppedThrough = 16,
}

/// <summary>
/// Pip's movement controller. Pure simulation (no drawing, no audio) so feel rules are unit-testable:
/// acceleration and skid, run modifier, variable jump height, coyote time, jump buffering, apex
/// hang, terminal velocity, ceiling corner correction, one-way drop-through and moving platforms.
/// </summary>
public sealed class Player
{
    public const int Width = 12, Height = 14;

    public Vector2 Position;
    public Vector2 Velocity;
    public int Facing { get; private set; } = 1;
    public bool Grounded { get; private set; }
    public bool Skidding { get; private set; }
    public bool Jumping { get; private set; }
    public float CoyoteTimer { get; private set; }
    public float BufferTimer { get; private set; }
    public float DropTimer { get; private set; }
    public float LandImpact { get; private set; }
    public float AirTime { get; private set; }
    public IRideable? Riding { get; private set; }
    public PlayerEvents Events { get; private set; }
    /// <summary>Tile hit by Pip's head this step (valid when <see cref="PlayerEvents.HeadBump"/> is set).</summary>
    public (int X, int Y) BumpedTile { get; private set; }

    public PipForm Form { get; set; } = PipForm.Sprout;
    public float Invulnerable { get; set; }
    /// <summary>When false (cutscenes, flag slide) input is ignored but physics still runs.</summary>
    public bool Controllable { get; set; } = true;

    public RectF Bounds => new(Position.X, Position.Y, Width, Height);
    public Vector2 Feet => new(Position.X + Width / 2f, Position.Y + Height);
    public Vector2 Center => Bounds.Center;

    public Player(Vector2 position) => Position = position;

    /// <summary>Copies the full movement state, for prediction (e.g. AI planning a jump) without touching this player.</summary>
    public Player Clone() => (Player)MemberwiseClone();

    /// <summary>Starts a jump-like launch. When <paramref name="cuttable"/>, releasing jump shortens it (variable height).</summary>
    public void Launch(float speed, bool cuttable = true)
    {
        Velocity.Y = -speed;
        Jumping = cuttable;
        Grounded = false;
        Riding = null;
        CoyoteTimer = 0;
    }

    public void Step(float dt, PlayerInput input, Level level, IReadOnlyList<IRideable>? rideables = null)
    {
        Events = PlayerEvents.None;
        if (!Controllable) input = default;
        var grid = level.Grid;
        int axis = input.Axis;
        bool wasGrounded = Grounded;
        float maxSpeed = input.RunHeld ? Tuning.RunSpeed : Tuning.WalkSpeed;

        // Horizontal acceleration with skid on reversal.
        Skidding = false;
        if (Grounded)
        {
            if (axis != 0 && Velocity.X != 0 && MathF.Sign(Velocity.X) != axis && MathF.Abs(Velocity.X) > 30)
            {
                Velocity.X = MathX.Approach(Velocity.X, 0, Tuning.SkidDecel * dt);
                Skidding = true;
                Events |= PlayerEvents.Skid;
            }
            else if (axis != 0)
            {
                float accel = MathF.Abs(Velocity.X) > maxSpeed ? Tuning.GroundFriction : input.RunHeld ? Tuning.RunAccel : Tuning.GroundAccel;
                Velocity.X = MathX.Approach(Velocity.X, axis * maxSpeed, accel * dt);
            }
            else Velocity.X = MathX.Approach(Velocity.X, 0, Tuning.GroundFriction * dt);
        }
        else if (axis != 0)
        {
            // Air control keeps run momentum: never slow down toward a lower cap while pushing forward.
            float target = axis * MathF.Max(maxSpeed, MathF.Sign(Velocity.X) == axis ? MathF.Abs(Velocity.X) : 0);
            Velocity.X = MathX.Approach(Velocity.X, target, Tuning.AirAccel * dt);
        }
        else Velocity.X = MathX.Approach(Velocity.X, 0, Tuning.AirDrag * dt);
        if (axis != 0 && !Skidding) Facing = axis;

        // Jump buffer and coyote time.
        BufferTimer = input.JumpPressed ? Tuning.JumpBuffer : MathF.Max(0, BufferTimer - dt);
        CoyoteTimer = Grounded ? Tuning.CoyoteTime : MathF.Max(0, CoyoteTimer - dt);
        DropTimer = MathF.Max(0, DropTimer - dt);
        if (BufferTimer > 0 && CoyoteTimer > 0)
        {
            bool onOneWayOnly = Grounded && Riding is null && input.Down
                && !TileMotion.IsOnGround(Bounds, grid, ignoreOneWay: true) && TileMotion.IsOnGround(Bounds, grid);
            BufferTimer = 0;
            CoyoteTimer = 0;
            if (onOneWayOnly)
            {
                DropTimer = Tuning.DropThroughTime;
                Grounded = false;
                Events |= PlayerEvents.DroppedThrough;
            }
            else
            {
                float bonus = Tuning.RunJumpBonus * MathF.Min(1, MathF.Abs(Velocity.X) / Tuning.RunSpeed);
                Launch(Tuning.JumpSpeed + bonus);
                Events |= PlayerEvents.Jumped;
            }
        }

        // Gravity: lighter while rising with jump held, heavier on release (variable height), floaty at the apex.
        float gravity;
        if (Velocity.Y < 0)
            gravity = Jumping && !input.JumpHeld ? Tuning.ReleaseGravity
                : MathF.Abs(Velocity.Y) < Tuning.ApexThreshold ? Tuning.ApexGravity : Tuning.RiseGravity;
        else gravity = input.JumpHeld && Velocity.Y < Tuning.ApexThreshold && !Grounded ? Tuning.ApexGravity : Tuning.FallGravity;
        Velocity.Y = MathF.Min(Velocity.Y + gravity * dt, Tuning.TerminalVelocity);
        if (Velocity.Y >= 0) Jumping = false;

        bool ignoreOneWay = DropTimer > 0;
        // Ride: apply the platform's own movement first so Pip is carried.
        if (Riding is { } ride && ride.Delta != Vector2.Zero)
            Position = TileMotion.Move(Bounds, ride.Delta, grid, null, ignoreOneWay).Position;

        var horizontal = TileMotion.Move(Bounds, new Vector2(Velocity.X * dt, 0), grid, null, ignoreOneWay);
        Position = horizontal.Position;
        if (horizontal.HitWall) Velocity.X = 0;

        float dy = Velocity.Y * dt;
        float previousBottom = Bounds.Bottom;
        if (dy < 0)
        {
            CorrectCorner(dy, grid);
            if (HiddenBlockAbove(dy, level) is { } hidden)
            {
                Position.Y = (hidden.Y + 1) * Level.TileSize;
                Velocity.Y = 0;
                Jumping = false;
                BumpedTile = hidden;
                Events |= PlayerEvents.HeadBump;
                dy = 0;
            }
        }
        var vertical = TileMotion.Move(Bounds, new Vector2(0, dy), grid, null, ignoreOneWay);
        Position = vertical.Position;
        if (vertical.HitCeiling)
        {
            BumpedTile = FindBumpedTile(level);
            Events |= PlayerEvents.HeadBump;
            Velocity.Y = 20;
            Jumping = false;
        }

        // Moving platforms behave like one-way floors.
        Riding = null;
        if (Velocity.Y >= 0 && rideables is not null)
            foreach (var r in rideables)
            {
                var b = r.Bounds;
                if (Bounds.Right <= b.Left + 1 || Bounds.Left >= b.Right - 1) continue;
                float top = b.Top;
                if (previousBottom <= top - Math.Min(0, r.Delta.Y) + 2.5f && Bounds.Bottom >= top - 0.5f)
                {
                    Position.Y = top - Height;
                    Velocity.Y = 0;
                    Riding = r;
                    r.StoodOn();
                    break;
                }
            }

        bool onTiles = vertical.HitFloor || (Velocity.Y >= 0 && TileMotion.IsOnGround(Bounds, grid, null, ignoreOneWay));
        Grounded = onTiles || Riding is not null;
        if (Grounded)
        {
            if (Velocity.Y > 0) Velocity.Y = 0;
            if (!wasGrounded)
            {
                Events |= PlayerEvents.Landed;
                LandImpact = MathF.Min(1, AirTime / 0.6f);
            }
            AirTime = 0;
            Jumping = false;
        }
        else AirTime += dt;
        Invulnerable = MathF.Max(0, Invulnerable - dt);
    }

    /// <summary>Nudges Pip sideways when only the edge of his head would clip a ceiling tile.</summary>
    private void CorrectCorner(float dy, ICollisionGrid grid)
    {
        var up = Bounds.Offset(0, dy);
        if (!Overlaps(up, grid)) return;
        for (int s = 1; s <= Tuning.CornerCorrection; s++)
            foreach (int dir in new[] { -1, 1 })
            {
                var shifted = up.Offset(dir * s, 0);
                if (!Overlaps(shifted, grid) && !Overlaps(Bounds.Offset(dir * s, 0), grid))
                {
                    Position.X += dir * s;
                    return;
                }
            }
    }

    private static bool Overlaps(RectF box, ICollisionGrid grid)
    {
        int ts = grid.TileSize;
        for (int y = (int)MathF.Floor(box.Top / ts); y <= (int)MathF.Floor((box.Bottom - 0.001f) / ts); y++)
        for (int x = (int)MathF.Floor(box.Left / ts); x <= (int)MathF.Floor((box.Right - 0.001f) / ts); x++)
            if ((grid.FlagsAt(x, y) & TileFlags.Solid) != 0) return true;
        return false;
    }

    private (int X, int Y)? HiddenBlockAbove(float dy, Level level)
    {
        float top = Bounds.Top, newTop = top + dy;
        int row = (int)MathF.Floor(newTop / Level.TileSize);
        float bottom = (row + 1) * Level.TileSize;
        if (row < 0 || top < bottom - 0.01f || newTop >= bottom) return null;
        int cx = (int)MathF.Floor(Center.X / Level.TileSize);
        return level.Tiles[cx, row] == Tile.HiddenLife ? (cx, row) : null;
    }

    private (int X, int Y) FindBumpedTile(Level level)
    {
        int row = (int)MathF.Floor((Bounds.Top - 1) / Level.TileSize);
        int center = (int)MathF.Floor(Center.X / Level.TileSize);
        if (level.IsSolid(center, row)) return (center, row);
        int left = (int)MathF.Floor(Bounds.Left / Level.TileSize), right = (int)MathF.Floor((Bounds.Right - 0.01f) / Level.TileSize);
        return level.IsSolid(left, row) ? (left, row) : (right, row);
    }
}
