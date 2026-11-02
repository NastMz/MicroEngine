using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;

namespace MeadowRun.Gameplay.Actors;

/// <summary>A spring pad; landing on it launches Pip (higher when jump is held).</summary>
internal sealed class Spring : Actor
{
    public float Compress { get; set; }
    public Spring(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft) => Hitbox = new RectF(1, 6, 14, 10);
    public override bool AlwaysActive => true;
    protected override void Tick(float dt) => Compress = MathF.Max(0, Compress - dt);
    public override void Render(IRenderer r, Assets a, float time) => r.Draw(a.Frames("spring")[Compress > 0 ? 1 : 0], Snap(Feet));
}

/// <summary>A lantern post; touching it saves the respawn point for this level.</summary>
internal sealed class Checkpoint : Actor
{
    public int Index { get; }
    public bool Lit { get; set; }
    public Checkpoint(Stage stage, Vector2 tileTopLeft, int index, bool lit) : base(stage, tileTopLeft + new Vector2(0, -16))
    {
        Hitbox = new RectF(2, 0, 12, 32);
        Index = index;
        Lit = lit;
    }

    public override bool AlwaysActive => true;
    protected override void Tick(float dt) { }

    public override void Render(IRenderer r, Assets a, float time)
    {
        var pos = Snap(Feet);
        if (Lit)
        {
            r.SetBlendMode(BlendMode.Additive);
            r.Draw(a["glow"], pos + new Vector2(0, -26), Pal.Amber.WithAlpha(0.45f + MathX.Wave(time, 3, 0.08f)));
            r.SetBlendMode(BlendMode.Alpha);
        }
        r.Draw(a.Frames("checkpoint")[Lit ? 1 + (int)(time * 6) % 2 : 0], pos);
    }
}

/// <summary>The goal pole. The pennant slides down with Pip when it is grabbed.</summary>
internal sealed class GoalPole : Actor
{
    public const int Tiles = 9;
    public float PennantDrop { get; set; }
    public GoalPole(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft + new Vector2(0, 16 - Tiles * 16))
        // The grab zone reaches far above the visible pole so a high jump can never sail over it.
        => Hitbox = new RectF(6, 6 - 400, 4, Tiles * 16 - 6 + 400);
    public override bool AlwaysActive => true;
    public float Top => Position.Y + 6;
    public float Bottom => Bounds.Bottom;
    protected override void Tick(float dt) { }

    public override void Render(IRenderer r, Assets a, float time)
    {
        var foot = Snap(new Vector2(Position.X + 8, Bounds.Bottom));
        r.Draw(a["goalpole"], foot);
        var pennant = a.Frames("pennant")[(int)(time * 8) % 4];
        r.Draw(pennant, Snap(new Vector2(Position.X + 9, Position.Y + 16 + PennantDrop)));
    }
}

/// <summary>Shared rendering and riding contract for platforms that carry Pip.</summary>
internal abstract class PlatformActor(Stage stage, Vector2 position, int tiles) : Actor(stage, position), IRideable
{
    protected readonly Vector2 Origin = position;
    private readonly string[] _parts = [.. Enumerable.Range(0, 3).Select(part => $"platform_{Assets.Key(stage.Level.Biome)}_{part}")];
    public int Tiles { get; } = tiles;
    public Vector2 Delta { get; protected set; }
    public bool StoodThisStep { get; private set; }
    public override bool AlwaysActive => true;
    public virtual void StoodOn() => StoodThisStep = true;
    protected void ClearStood() => StoodThisStep = false;

    protected void DrawPlatform(IRenderer r, Assets a, Vector2 offset)
    {
        var p = Snap(Position + offset);
        for (int i = 0; i < Tiles; i++)
        {
            int part = i == 0 ? 0 : i == Tiles - 1 ? 2 : 1;
            r.Draw(a[_parts[part]], p + new Vector2(i * 16, 0));
        }
    }
}

/// <summary>A platform moving on a smooth back-and-forth path (horizontal or vertical).</summary>
internal sealed class MovingPlatform : PlatformActor
{
    private readonly bool _vertical;
    private const float Amplitude = 48f, Period = 4.2f;

    public MovingPlatform(Stage stage, Vector2 tileTopLeft, bool vertical) : base(stage, tileTopLeft, 3)
    {
        _vertical = vertical;
        Hitbox = new RectF(0, 0, 48, 8);
    }

    protected override void Tick(float dt)
    {
        ClearStood();
        var target = PositionAt(Stage.Clock);
        Delta = target - Position;
        Position = target;
    }

    /// <summary>Where the platform is at a given stage clock; paths are deterministic so they can be predicted.</summary>
    public Vector2 PositionAt(float clock)
    {
        float phase = MathF.Sin(clock / Period * MathF.Tau);
        return Origin + (_vertical ? new Vector2(0, phase * Amplitude) : new Vector2(phase * Amplitude, 0));
    }

    public override void Render(IRenderer r, Assets a, float time) => DrawPlatform(r, a, Vector2.Zero);
}

/// <summary>A platform that shakes and drops shortly after Pip stands on it, then returns.</summary>
internal sealed class FallingPlatform : PlatformActor
{
    private float _standTime, _fallTime = -1;
    public FallingPlatform(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft, 2) => Hitbox = new RectF(0, 0, 32, 8);

    protected override void Tick(float dt)
    {
        var before = Position;
        if (_fallTime >= 0)
        {
            _fallTime += dt;
            Velocity.Y = MathF.Min(Velocity.Y + 700 * dt, 260);
            Position += Velocity * dt;
            if (_fallTime > 4f)
            {
                _fallTime = -1;
                _standTime = 0;
                Velocity = Vector2.Zero;
                Position = Origin;
                Stage.Puff(Center, Pal.Cloud);
            }
        }
        else if (StoodThisStep || _standTime > 0)
        {
            _standTime += dt;
            if (_standTime > 0.45f) _fallTime = 0;
        }
        ClearStood();
        Delta = Position - before;
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        float shake = _standTime > 0 && _fallTime < 0 ? MathF.Round(MathF.Sin(time * 70)) : 0;
        DrawPlatform(r, a, new Vector2(shake, 0));
    }
}

/// <summary>A chain of fireballs rotating around a block. Pure hazard.</summary>
internal sealed class FireBar : Actor
{
    public const int Links = 5;
    private readonly float _speed;
    public FireBar(Stage stage, Vector2 tileTopLeft, bool clockwise) : base(stage, tileTopLeft)
    {
        Hitbox = new RectF(-Links * 8, -Links * 8, 16 + Links * 16, 16 + Links * 16);
        _speed = clockwise ? 1.9f : -1.9f;
    }

    public override bool AlwaysActive => true;
    public Vector2 Pivot => Position + new Vector2(8, 8);
    public Vector2 Link(int i) => LinkAt(i, 0);
    /// <summary>Where link <paramref name="i"/> will be <paramref name="secondsAhead"/> from now (bars are deterministic).</summary>
    public Vector2 LinkAt(int i, float secondsAhead) => Pivot + MathX.FromAngle((Stage.Clock + secondsAhead) * _speed, i * 8);
    protected override void Tick(float dt) { }

    public bool Touches(RectF box)
    {
        for (int i = 1; i <= Links; i++)
        {
            var c = Link(i);
            float nx = Math.Clamp(c.X, box.Left, box.Right), ny = Math.Clamp(c.Y, box.Top, box.Bottom);
            if ((nx - c.X) * (nx - c.X) + (ny - c.Y) * (ny - c.Y) < 3.5f * 3.5f) return true;
        }
        return false;
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        r.SetBlendMode(BlendMode.Additive);
        for (int i = 1; i <= Links; i++) r.Draw(a["glow_small"], Snap(Link(i)), Pal.Flame.WithAlpha(0.35f));
        r.SetBlendMode(BlendMode.Alpha);
        for (int i = 1; i <= Links; i++) r.Draw(a.Frames("fireball")[(int)(time * 12 + i) % 2], Snap(Link(i)));
    }
}

/// <summary>A light source: torches in the castle, crystal clusters in the caves, lamps elsewhere.</summary>
internal sealed class LightProp : Actor
{
    public LightProp(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft) => Hitbox = new RectF(0, 0, 16, 16);
    public override bool AlwaysActive => true;
    protected override void Tick(float dt) { }

    public override void Render(IRenderer r, Assets a, float time)
    {
        var foot = Snap(Feet);
        bool caves = Stage.Level.Biome == Biome.Caves;
        var glowColor = caves ? (Position.X % 32 < 16 ? Pal.Violet : Pal.Teal) : Pal.Amber;
        r.SetBlendMode(BlendMode.Additive);
        r.Draw(a["glow"], foot + new Vector2(0, caves ? -6 : -14), glowColor.WithAlpha(0.5f + MathX.Wave(time + Position.X * 0.01f, 1.3f, 0.12f)));
        r.SetBlendMode(BlendMode.Alpha);
        if (caves) r.Draw(a["deco_crystal"], foot);
        else r.Draw(a.Frames("torch")[(int)(time * 9 + Position.X) % 3], foot);
    }
}
