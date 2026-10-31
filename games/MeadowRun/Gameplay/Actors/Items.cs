using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;

namespace MeadowRun.Gameplay.Actors;

/// <summary>A sun-coin placed in the level.</summary>
internal sealed class Coin : Actor
{
    private readonly float _phase;
    public Coin(Stage stage, Vector2 tileTopLeft) : base(stage, tileTopLeft)
    {
        Hitbox = new RectF(3, 2, 10, 12);
        _phase = (tileTopLeft.X + tileTopLeft.Y) * 0.013f;
    }

    public override bool AlwaysActive => true;
    protected override void Tick(float dt) { }

    public override void Render(IRenderer r, Assets a, float time)
    {
        var frames = a.Frames("coin");
        // Coins spin in a slow wave across the level instead of in lockstep.
        int f = (int)((time + _phase) * 8) % frames.Length;
        r.Draw(frames[f], Snap(Position + new Vector2(8, 8)));
    }
}

/// <summary>The coin that springs out of a "?" block; it is collected immediately.</summary>
internal sealed class PopCoin : Actor
{
    public PopCoin(Stage stage, Vector2 position) : base(stage, position) => Velocity = new Vector2(0, -260);
    public override bool AlwaysActive => true;

    protected override void Tick(float dt)
    {
        Velocity.Y += 900 * dt;
        Position += Velocity * dt;
        if (Velocity.Y > 120)
        {
            Stage.Sparkle(Position, Pal.Gold);
            Destroy();
        }
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        var frames = a.Frames("coin");
        r.Draw(frames[(int)(Age * 24) % frames.Length], Snap(Position));
    }
}

internal enum PowerKind { Sunpetal, EmberSeed, LifeSprout }

/// <summary>A power-up rising out of a block. Sunpetals and life sprouts wander; ember seeds stay put.</summary>
internal sealed class PowerItem : Actor
{
    private const float RiseTime = 0.6f;
    private readonly float _baseY;
    public PowerKind Kind { get; }
    public bool Emerging => Age < RiseTime;

    public PowerItem(Stage stage, Vector2 blockTopLeft, PowerKind kind) : base(stage, blockTopLeft)
    {
        Kind = kind;
        Hitbox = new RectF(2, 2, 12, 14);
        _baseY = blockTopLeft.Y;
        Velocity.X = kind == PowerKind.LifeSprout ? 70 : 45;
    }

    public override bool AlwaysActive => true;

    protected override void Tick(float dt)
    {
        if (Emerging)
        {
            Position = Position with { Y = _baseY - 16 * (Age / RiseTime) };
            return;
        }
        if (Kind == PowerKind.EmberSeed) return;
        var hit = MoveBody(dt);
        if (hit.HitWall) Velocity.X = -Velocity.X;
        if (Position.Y > Stage.Level.PixelHeight + 32) Destroy();
    }

    /// <summary>Knocked up by a block bumped underneath it.</summary>
    public void Pop()
    {
        if (Emerging || Kind == PowerKind.EmberSeed) return;
        Velocity.Y = -200;
        Velocity.X = MathF.Abs(Velocity.X) * (Stage.Player.Center.X < Center.X ? 1 : -1);
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        var pos = Snap(Feet);
        Sprite sprite = Kind switch
        {
            PowerKind.Sunpetal => a.Frames("sunpetal")[(int)(time * 6) % 4],
            PowerKind.EmberSeed => a.Frames("emberseed")[(int)(time * 4) % 2],
            _ => a["lifesprout"],
        };
        if (Emerging)
        {
            // Reveal from inside the block: clip to everything above the block's top edge.
            float top = pos.Y - sprite.Origin.Y;
            float visible = MathF.Min(sprite.Source.Height, MathF.Round(_baseY - top));
            if (visible >= 1)
                r.Draw(sprite.Texture, sprite.Source with { Height = visible }, new Vector2(pos.X - sprite.Origin.X, top), Color.White);
            return;
        }
        if (Kind == PowerKind.EmberSeed)
        {
            r.SetBlendMode(BlendMode.Additive);
            r.Draw(a["glow_small"], pos + new Vector2(0, -7), Pal.Flame.WithAlpha(0.6f + MathX.Wave(time, 2, 0.2f)));
            r.SetBlendMode(BlendMode.Alpha);
        }
        r.Draw(sprite, pos);
    }
}

/// <summary>A tumbling brick fragment from a broken block.</summary>
internal sealed class Shard : Actor
{
    private readonly float _spin;
    public Shard(Stage stage, Vector2 position, Vector2 velocity) : base(stage, position)
    {
        Velocity = velocity;
        _spin = velocity.X * 0.08f;
    }

    public override bool AlwaysActive => true;

    protected override void Tick(float dt)
    {
        Velocity.Y += 1000 * dt;
        Position += Velocity * dt;
        if (Age > 1.6f) Destroy();
    }

    public override void Render(IRenderer r, Assets a, float time)
    {
        var s = a["shard_" + Assets.Key(Stage.Level.Biome)];
        r.Draw(s.Texture, s.Source, Snap(Position) - s.Origin, Color.White, new DrawOptions(Rotation: Age * _spin, Origin: s.Origin));
    }
}
