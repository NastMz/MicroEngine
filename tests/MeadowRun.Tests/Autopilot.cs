using System.Numerics;
using MeadowRun.Gameplay;
using MeadowRun.Gameplay.Actors;
using MicroEngine;
using MicroEngine.World;

namespace MeadowRun.Tests;

/// <summary>
/// A simple player used to prove levels are completable: runs right, jumps fully at walls, gaps,
/// hazards and enemies, waits at edges for moving platforms and waits out fire-bar sweeps.
/// </summary>
internal sealed class Autopilot(int variant = 0)
{
    private readonly float _enemyReach = 40 + variant * 14;
    private int _holdJump;
    private int _cooldown;
    private int _waited;
    private bool _planRun = true;

    public PlayerInput Next(Stage stage)
    {
        var p = stage.Player;
        if (_cooldown > 0) _cooldown--;
        bool held = _holdJump-- > 0;
        if (!p.Grounded) return new PlayerInput(false, true, false, false, held, _planRun, false);
        _planRun = true;

        var decision = Decide(stage);
        if (decision == Move.Wait && _waited++ < 60 * 8)
        {
            // While waiting on a moving platform, keep to its middle so it does not slide away underneath.
            if (p.Riding is { } ride)
            {
                float off = p.Center.X - ride.Bounds.Center.X;
                return new PlayerInput(off > 4, off < -4, false, false, false, false, false);
            }
            return default;
        }
        if (decision == Move.Wait) decision = Move.Jump;
        _waited = 0;
        if (decision == Move.Plan && _cooldown == 0)
        {
            _holdJump = _plan.Hold;
            _planRun = _plan.Run;
            _cooldown = 8;
            return new PlayerInput(false, true, false, true, true, _plan.Run, false);
        }
        bool jump = decision is Move.Jump or Move.Plan && _cooldown == 0;
        if (jump) { _holdJump = 24; _cooldown = 8; }
        return new PlayerInput(false, true, false, jump, jump || held, true, false);
    }

    private enum Move { Run, Jump, Wait, Plan }
    private (bool Run, int Hold) _plan;

    /// <summary>
    /// Simulates candidate jumps on a clone of Pip and returns one that lands on safe ground ahead,
    /// preferring solid ground and the nearest landing. Platforms are treated as static (good enough when they are near).
    /// </summary>
    private bool PlanJump(Stage stage)
    {
        var start = stage.Player;
        float bestX = float.MinValue;
        bool found = false;
        foreach (bool run in new[] { true, false })
        foreach (int hold in new[] { 24, 12, 6, 2 })
        {
            var sim = start.Clone();
            var predicted = stage.Platforms.Select(pl => new Predicted(pl)).ToList();
            bool left = false, ok = false;
            for (int f = 0; f < 120; f++)
            {
                var input = new PlayerInput(false, true, false, f == 0, f < hold, run, false);
                foreach (var pr in predicted) pr.Advance(stage.Clock + (f + 1) * StageHarness.Dt);
                sim.Step(StageHarness.Dt, input, stage.Level, predicted);
                if (!sim.Grounded) left = true;
                var flags = TileMotion.TouchedFlags(sim.Bounds, stage.Level.Grid);
                if ((flags & TileFlags.Hazard) != 0 || sim.Bounds.Top > stage.Level.PixelHeight) break;
                if (left && sim.Grounded) { ok = sim.Position.X > start.Position.X + 20; break; }
            }
            // Prefer solid ground over platforms, then the nearest safe landing (least risky).
            float score = ok ? (sim.Riding is null ? 10000 : 0) - sim.Position.X : float.MinValue;
            if (ok && score > bestX) { bestX = score; _plan = (run, hold); found = true; }
        }
        return found;
    }

    private Move Decide(Stage stage)
    {
        var p = stage.Player;
        var b = p.Bounds;
        var grid = stage.Level.Grid;

        if (FireBarAhead(stage, b)) return Move.Wait;

        float aheadX = b.Right + 6;
        if (Solid(grid, aheadX, b.Bottom - 2) || Solid(grid, aheadX, b.Top + 2)) return Move.Jump;

        foreach (var e in stage.Actors.OfType<Enemy>())
        {
            if (e.Dead || e is Rock) continue;
            float dx = e.Bounds.Left - b.Right;
            if (dx > -4 && dx < _enemyReach && e.Bounds.Bottom > b.Top - 30 && e.Bounds.Top < b.Bottom + 8) return Move.Jump;
        }

        // Hazards at foot level or just below in the next tiles.
        var below = TileMotion.TouchedFlags(new RectF(b.Right, b.Top, 26, b.Height + 10), grid);
        bool hazard = (below & TileFlags.Hazard) != 0;

        bool floorNear = Supported(stage, b.Right + 4, b.Bottom) && Supported(stage, b.Right + 14, b.Bottom);
        if (floorNear && !hazard) return Move.Run;

        // A gap or hazard: at the edge, plan a jump that lands safely.
        bool riding = p.Riding is not null;
        if (!EdgeClose(stage, b) && !riding) return Move.Run;
        if (PlanJump(stage)) return Move.Plan;
        if (p.Riding is FallingPlatform) return Move.Jump;
        bool platformsAround = stage.Platforms.Any(pl => pl is MovingPlatform && pl.Bounds.Left > b.Left - 16 && pl.Bounds.Left - b.Right < 220 && pl != p.Riding);
        if (platformsAround || riding) return Move.Wait;
        return Move.Jump;
    }

    /// <summary>True when Pip is at the very edge of his footing (time to commit to the jump).</summary>
    private static bool EdgeClose(Stage stage, RectF b) => !Supported(stage, b.Right + 6, b.Bottom) || HazardAt(stage, b.Right + 8, b.Bottom + 4);

    /// <summary>Waits until the next 1.3 seconds keep every fire bar ahead pointing away from Pip's path.</summary>
    private static bool FireBarAhead(Stage stage, RectF b)
    {
        foreach (var bar in stage.Actors.OfType<FireBar>())
        {
            float dx = bar.Pivot.X - b.Center.X;
            if (dx < -30 || dx > 100) continue;
            bool pathBelow = bar.Pivot.Y < b.Center.Y - 30;
            for (float t = 0; t <= 1.3f; t += 0.05f)
            {
                var tip = bar.LinkAt(FireBar.Links, t);
                bool away = pathBelow ? tip.Y < bar.Pivot.Y - 12 : tip.Y > bar.Pivot.Y + 12;
                if (!away) return true;
            }
        }
        return false;
    }

    /// <summary>A platform's predicted future position for jump planning.</summary>
    private sealed class Predicted(PlatformActor source) : IRideable
    {
        private Vector2 _position = source.Position;
        public RectF Bounds => source.Hitbox.Offset(_position);
        public Vector2 Delta { get; private set; }
        public void StoodOn() { }
        public void Advance(float clock)
        {
            var next = source is MovingPlatform m ? m.PositionAt(clock) : _position;
            Delta = next - _position;
            _position = next;
        }
    }

    private static bool Supported(Stage stage, float x, float feetY)
    {
        var g = stage.Level.Grid;
        int tx = (int)MathF.Floor(x / g.TileSize), ty = (int)MathF.Floor((feetY + 1) / g.TileSize);
        if ((g.FlagsAt(tx, ty) & (TileFlags.Solid | TileFlags.OneWay)) != 0 && (g.FlagsAt(tx, ty) & TileFlags.Hazard) == 0) return true;
        return stage.Platforms.Any(p => x >= p.Bounds.Left && x <= p.Bounds.Right && feetY + 1 >= p.Bounds.Top && feetY + 1 <= p.Bounds.Bottom);
    }

    private static bool HazardAt(Stage stage, float x, float y)
    {
        var g = stage.Level.Grid;
        return (g.FlagsAt((int)MathF.Floor(x / g.TileSize), (int)MathF.Floor(y / g.TileSize)) & TileFlags.Hazard) != 0;
    }

    private static bool Solid(ICollisionGrid g, float x, float y)
        => (g.FlagsAt((int)MathF.Floor(x / g.TileSize), (int)MathF.Floor(y / g.TileSize)) & TileFlags.Solid) != 0;
}
