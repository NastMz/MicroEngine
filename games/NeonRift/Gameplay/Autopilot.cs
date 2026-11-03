using System.Numerics;
using NeonRift.Art;
using MicroEngine.Kernel;

namespace NeonRift.Gameplay;

/// <summary>
/// A simple bot that plays a stage: runs right firing, jumps gaps, lava and walls at the edge,
/// aims at the nearest enemy, and in the arena keeps distance from the boss while hopping.
/// Used by the playthrough tests and by capture runs (<c>RIFT_AUTO=1</c>); it is not part of play.
/// </summary>
public sealed class Autopilot : IStateHashed
{
    private float _time, _hop;

    /// <summary>Adds the bot's clock and its hop timer (the rest of its decision is read from the stage each update).</summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(_time);
        hash.Add(_hop);
    }

    public PlayerInput Next(Stage stage, float dt)
    {
        _time += dt;
        var p = stage.Player;
        if (p.State != PlayerState.Alive) return PlayerInput.None;
        var feet = p.Position;
        bool arena = stage.InArena;
        var target = stage.Enemies.OfType<Enemy>()
            .Where(e => e.Contact && stage.OnScreen(e.Bounds) || e is Carrier && stage.OnScreen(e.Bounds) || e is DreadTurret)
            .OrderBy(e => e is Carrier ? -1 : e.Armored ? 1 : 0).ThenBy(e => Vector2.DistanceSquared(e.Center, feet))
            .FirstOrDefault();

        bool right = true, left = false;
        if (arena)
        {
            // Stand where the target sits on a 45-degree line (or straight ahead when it is low).
            float desired = stage.Def.ArenaX + 110;
            if (target is not null)
            {
                float rise = feet.Y - 30 - target.Center.Y;
                desired = rise > 30 ? target.Center.X - rise : desired;
            }
            desired = Math.Clamp(desired, stage.Def.ArenaX + 24, stage.Def.ArenaX + 260);
            right = feet.X < desired - 12;
            left = feet.X > desired + 12;
        }
        bool jump = false;
        if (p.Grounded)
        {
            float edge = NeedsJump(stage, feet);
            if (right && edge >= 0) jump = true;
            if (arena)
            {
                _hop -= dt;
                // Hop regularly: it sweeps horizontal fire across heights and dodges low shots.
                if (_hop <= 0) { jump = true; _hop = 0.7f; }
            }
        }
        bool up = false, down = false, aimRight = right, aimLeft = left;
        if (target is not null)
        {
            var d = target.Center - (feet + new Vector2(0, -30));
            if (d.Y < -MathF.Abs(d.X) * 0.4f) up = true;
            else if (d.Y > MathF.Abs(d.X) * 0.6f && !p.Grounded) down = true;
            if (MathF.Abs(d.X) > 24) { aimRight = d.X > 0; aimLeft = d.X < 0; }
            else if (up) { aimRight = aimLeft = false; }
        }
        // While standing still in the arena, plant the feet so the diagonal aim does not walk into the boss.
        bool plant = arena && !right && !left && p.Grounded && !jump;
        bool moveRight = right || plant && aimRight, moveLeft = left || plant && aimLeft;
        if (!plant && up && aimRight && !right) moveRight = true;
        return new PlayerInput(moveLeft, moveRight, up, down, jump, jump || !p.Grounded, true, plant);
    }

    /// <summary>Distance to an edge that must be jumped (pit, lava or a wall/step ahead), or -1.</summary>
    private static float NeedsJump(Stage stage, Vector2 feet)
    {
        int ts = Tiles.Size;
        // Walls and steps: jump a little before touching them.
        for (float dx = 8; dx <= 16; dx += 4)
            if (MicroEngine.World.TileMotion.IsSolidAt(new Vector2(feet.X + dx, feet.Y - 6), stage.Grid)) return dx;
        // Pits and lava: jump right at the edge to use the whole arc.
        float x = feet.X + 9;
        var ground = stage.GroundBelow(x, feet.Y - 2);
        if (ground is null || ground > feet.Y + ts * 3) return 9;
        if (stage.Def.Layer.Get((int)(x / ts), (int)(ground.Value / ts)) == TileId.Lava) return 9;
        return -1;
    }
}
