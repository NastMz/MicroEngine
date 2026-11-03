using System.Numerics;
using NeonRift.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.World;

namespace NeonRift.Gameplay;

/// <summary>Anything living in a stage. Simulation lives in <see cref="Entity.Update"/>; drawing needs the assets.</summary>
public abstract class Actor : Entity, IStateHashed
{
    /// <summary>
    /// Adds the actor's simulation state to the per-step state hash: whether it is alive, its position, velocity and
    /// age; subclasses add their own timers and counters after it. The previous position kept for drawing between
    /// updates (<see cref="DrawPosition"/>) and the hitbox (fixed per kind) are left out.
    /// </summary>
    public virtual void HashState(StateHasher hash)
    {
        hash.Add(Alive);
        hash.Add(Position);
        hash.Add(Velocity);
        hash.Add(Age);
    }

    /// <summary>Adds the living entities of <paramref name="list"/>: their count, then each one's state in list order.</summary>
    internal static void HashAll<T>(StateHasher hash, EntityList<T> list) where T : Actor
    {
        int count = 0;
        foreach (var _ in list) count++;
        hash.Add(count);
        foreach (var actor in list) actor.HashState(hash);
    }

    public Stage Stage { get; internal set; } = null!;
    public Vector2 Velocity { get; set; }
    public float Age { get; protected set; }
    private Vector2 _previous;
    private bool _hasPrevious;

    /// <summary>Records the position this update starts from (for drawing between updates), then ages the actor.</summary>
    public override void Update(float dt)
    {
        _previous = Position;
        _hasPrevious = true;
        Age += dt;
    }

    /// <summary>Where to draw the actor: <see cref="Stage.Blend"/> of the way from its previous position to the current one.
    /// Before its first update, or after <see cref="Snap"/>, there is no previous position and it is drawn where it is.</summary>
    internal Vector2 DrawPosition => _hasPrevious ? Stage.Between(_previous, Position) : Position;

    /// <summary>Forgets the previous position after a jump (respawn, teleport) so the actor is not drawn streaking across.</summary>
    public void Snap() => _hasPrevious = false;

    internal virtual void Render(IRenderer r, Assets art) { }

    /// <summary>Draws a sprite and, while <paramref name="flash"/> is positive, an additive copy for a hit flash.</summary>
    internal static void DrawFlashing(IRenderer r, Sprite sprite, Vector2 at, bool flipX, float flash, float rotation = 0)
    {
        r.DrawPivoted(sprite, at, rotation, flipX: flipX);
        if (flash <= 0) return;
        r.SetBlendMode(BlendMode.Additive);
        r.DrawPivoted(sprite, at, rotation, tint: Color.White.WithAlpha(0.85f), flipX: flipX);
        r.SetBlendMode(BlendMode.Alpha);
    }
}

/// <summary>A hostile actor with health. Touching an enemy that has <see cref="Contact"/> kills the player.</summary>
public abstract class Enemy : Actor
{
    public int Health { get; protected set; } = 1;
    public int Points { get; protected init; } = 100;
    /// <summary>Armoured enemies absorb shots with a clank instead of taking damage.</summary>
    public bool Armored { get; protected set; }
    public virtual bool Contact => true;
    public virtual bool Big => false;
    protected float Flash { get; set; }

    /// <summary>Adds the actor's state, then health and armour; the hit flash (drawing only) is left out.</summary>
    public override void HashState(StateHasher hash)
    {
        base.HashState(hash);
        hash.Add(Health);
        hash.Add(Armored);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        Flash = MathF.Max(0, Flash - dt);
    }

    /// <summary>Applies damage; returns whether the shot was stopped (armour or flesh).</summary>
    public virtual bool Hit(int damage, Vector2 at)
    {
        if (!Alive) return false;
        if (Armored)
        {
            Stage.Sound("clank", 0.5f);
            Stage.Spark(at);
            return true;
        }
        Health -= damage;
        Flash = 0.07f;
        if (Health <= 0) Die();
        else Stage.Sound("hit", 0.45f);
        return true;
    }

    protected virtual void Die()
    {
        Stage.Explode(Center, Big);
        Stage.Sound(Big ? "explode-medium" : "explode", 0.7f, pan: Stage.Pan(Center.X));
        Stage.AddScore(Points);
        Stage.DefeatSink?.Invoke(this);
        Destroy();
    }

    /// <summary>True once the actor is far behind the scrolling screen and can be dropped silently.</summary>
    protected bool LeftBehind(float margin = 96) => Bounds.Right < Stage.Camera.View.Left - margin || Bounds.Top > Stage.Def.PixelHeight + 32;
}

/// <summary>Short-lived visual effect (explosion, muzzle spark) that plays a strip once.</summary>
/// <remarks>
/// Presentation only, so the stage leaves effects out of the state hash: nothing in play reads them, and their lifetime
/// depends on the strip length learnt while drawing (<c>_frames</c>, set in <see cref="Render"/>).
/// </remarks>
public sealed class Effect : Actor
{
    private readonly string _sprite;
    private readonly float _fps, _rotation;
    private readonly bool _flipX;
    private int _frames = 1;

    public Effect(string sprite, Vector2 at, float fps = 16, float rotation = 0, bool flipX = false)
    {
        (_sprite, _fps, _rotation, _flipX) = (sprite, fps, rotation, flipX);
        Position = at;
        Hitbox = new RectF(-2, -2, 4, 4);
    }

    public string Sprite => _sprite;
    public float Scale { get; init; } = 1;

    public override void Update(float dt)
    {
        base.Update(dt);
        Position += Velocity * dt;
        if (Age * _fps >= _frames) Destroy();
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var frames = art.Frames(_sprite);
        _frames = frames.Count;
        int i = Math.Min((int)(Age * _fps), frames.Count - 1);
        r.DrawPivoted(frames[i], Vector2.Round(DrawPosition), _rotation, new Vector2(Scale), flipX: _flipX);
    }
}

/// <summary>Shared ground physics for walkers: gravity, tile collision and a grounded flag.</summary>
public struct Body
{
    public RectF Box;
    public bool Grounded;
    public MoveResult Last;

    public void Move(ref Vector2 position, ref Vector2 velocity, float dt, ICollisionGrid grid, float gravity = 980, float maxFall = 420, bool dropThrough = false)
    {
        velocity.Y = MathF.Min(velocity.Y + gravity * dt, maxFall);
        var result = TileMotion.Move(Box.Offset(position), velocity * dt, grid, ignoreOneWay: dropThrough);
        position = result.Position - Box.Position;
        if (result.HitFloor || result.HitCeiling) velocity.Y = 0;
        if (result.HitWall) velocity.X = 0;
        Grounded = result.HitFloor || velocity.Y >= 0 && TileMotion.IsOnGround(Box.Offset(position), grid, ignoreOneWay: dropThrough);
        Last = result;
    }
}
