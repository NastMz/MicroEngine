using System.Numerics;
using NeonRift.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace NeonRift.Gameplay;

/// <summary>
/// Anything living in a stage: an entity of the stage's world (<see cref="Stage.World"/>) with this object attached.
/// Its position is the entity's transform, so the world interpolates its drawing between updates. An actor is built
/// before it joins a stage (<see cref="Stage.Spawn{T}"/>); until then, and after the world released it, it keeps its
/// position itself. Simulation lives in <see cref="Update"/>; drawing needs the assets.
/// </summary>
public abstract class Actor : IDestroyedCallback
{
    private Vector2 _position;
    private bool _dead;
    private IWorld? _world;

    /// <summary>
    /// Adds the actor's simulation state to the per-step state hash: whether it is alive, its position, velocity and
    /// age; subclasses add their own timers and counters after it. The hitbox (fixed per kind) is left out. The stage
    /// hashes its actors in sections of its own (<see cref="Stage.HashState"/>); the world hashes only their entities.
    /// </summary>
    public virtual void HashState(StateHasher hash)
    {
        hash.Add(Alive);
        hash.Add(Position);
        hash.Add(Velocity);
        hash.Add(Age);
    }

    /// <summary>Adds the living actors of <paramref name="actors"/>: their count, then each one's state in spawn order.</summary>
    internal static void HashAll<T>(StateHasher hash, Attachments<T> actors) where T : Actor
    {
        hash.Add(Count(actors));
        foreach (var actor in actors) actor.HashState(hash);
    }

    /// <summary>How many living actors <paramref name="actors"/> holds.</summary>
    internal static int Count<T>(Attachments<T> actors) where T : Actor
    {
        int count = 0;
        foreach (var _ in actors) count++;
        return count;
    }

    /// <summary>The stage the actor joined; null before <see cref="Stage.Spawn{T}"/>.</summary>
    public Stage Stage { get; private set; } = null!;
    /// <summary>The actor's entity in the stage's world; <see cref="EntityId.None"/> before it joined and after it was released.</summary>
    public EntityId Id { get; private set; }
    /// <summary>World position in pixels: the entity's transform while the actor is in a stage.</summary>
    public Vector2 Position
    {
        get => _world is { } world ? world.Local(Id).Position2D : _position;
        set
        {
            if (_world is { } world) world.Local(Id).Position2D = value;
            else _position = value;
        }
    }
    /// <summary>Hitbox relative to <see cref="Position"/>.</summary>
    public RectF Hitbox { get; set; } = new(0, 0, 16, 16);
    /// <summary>False after <see cref="Destroy"/>: the stage stops updating, colliding and drawing it at once.</summary>
    public bool Alive => _world is { } world ? world.IsAlive(Id) : !_dead;
    /// <summary>The hitbox in world pixels.</summary>
    public RectF Bounds => Hitbox.Offset(Position);
    /// <summary>The centre of <see cref="Bounds"/>.</summary>
    public Vector2 Center => Bounds.Center;
    public Vector2 Velocity { get; set; }
    public float Age { get; protected set; }

    /// <summary>Destroys the actor: every walk of the stage skips it at once; the world releases it when its step ends.</summary>
    public void Destroy()
    {
        if (_world is { } world) world.Destroy(Id);
        else _dead = true;
    }

    /// <summary>
    /// Joins <paramref name="stage"/>: creates the actor's entity at its position and attaches the actor under its kind
    /// (<see cref="AttachTo"/>); an actor destroyed before it joined is destroyed at once.
    /// </summary>
    /// <exception cref="InvalidOperationException">The actor already joined a stage.</exception>
    internal void Join(Stage stage, IWorld world)
    {
        if (_world is not null || Stage is not null) throw new InvalidOperationException("The actor already joined a stage.");
        var id = world.Create();
        world.Local(id).Position2D = _position;
        (Stage, _world, Id) = (stage, world, id);
        AttachTo(world, id);
        if (_dead) world.Destroy(id);
    }

    /// <summary>Attaches the actor to its entity under the type the stage walks it by (<see cref="Stage.Enemies"/> and the like).</summary>
    private protected abstract void AttachTo(IWorld world, EntityId id);

    /// <summary>Keeps the released entity's position, so a destroyed actor (a defeated boss) stays readable.</summary>
    void IDestroyedCallback.OnDestroyed(EntityId entity)
    {
        if (_world is not { } world) return;
        _position = world.Local(entity).Position2D;
        (_world, Id, _dead) = (null, EntityId.None, true);
    }

    /// <summary>Ages the actor; called once per update while it is alive.</summary>
    public virtual void Update(float dt) => Age += dt;

    /// <summary>Where to draw the actor: the world's interpolated position between the previous update and this one.</summary>
    internal Vector2 DrawPosition => _world is { } world ? world.DrawPosition2D(Id) : _position;

    /// <summary>Draws the actor at its current position until the next update, so a jump (a respawn) is not drawn as a streak.</summary>
    public void Teleport() => _world?.Teleport(Id);

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

    private protected sealed override void AttachTo(IWorld world, EntityId id) => world.Attach<Enemy>(id, this);

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
            Stage.Deflect(at);
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
/// Nothing in play reads effects. Their lifetime is the strip's length, which the stage reads from the art when the
/// effect first updates (<see cref="Stage.StripLength"/>), never from drawing, so how often frames are drawn changes
/// nothing; the stage's own sections leave them out, the world hashes their entities like any other.
/// </remarks>
public sealed class Effect : Actor
{
    private readonly string _sprite;
    private readonly float _fps, _rotation;
    private readonly bool _flipX;
    private int _frames;

    public Effect(string sprite, Vector2 at, float fps = 16, float rotation = 0, bool flipX = false)
    {
        (_sprite, _fps, _rotation, _flipX) = (sprite, fps, rotation, flipX);
        Position = at;
        Hitbox = new RectF(-2, -2, 4, 4);
    }

    public string Sprite => _sprite;
    private protected override void AttachTo(IWorld world, EntityId id) => world.Attach<Effect>(id, this);
    public float Scale { get; init; } = 1;

    public override void Update(float dt)
    {
        base.Update(dt);
        if (_frames == 0) _frames = Stage.StripLength(_sprite);
        Position += Velocity * dt;
        if (Age * _fps >= _frames) Destroy();
    }

    internal override void Render(IRenderer r, Assets art)
    {
        var frames = art.Frames(_sprite);
        int i = Math.Min((int)(Age * _fps), frames.Count - 1);
        r.DrawPivoted(frames[i], Vector2.Round(DrawPosition), _rotation, new Vector2(Scale), flipX: _flipX);
    }
}

/// <summary>Shared ground physics for walkers: gravity, tile collision and a grounded flag.</summary>
public struct Body
{
    public RectF Box;
    public bool Grounded;
    public TileMoveResult Last;

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
