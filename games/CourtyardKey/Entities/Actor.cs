using System.Numerics;
using CourtyardKey.Visuals;
using CourtyardKey.Scenes;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.World;

namespace CourtyardKey.Entities;

public enum HitKind { Sword, Boomerang }

/// <summary>An attack landing on something. <see cref="From"/> is where the blow came from.</summary>
public readonly record struct Hit(int Damage, Vector2 From, HitKind Kind);

public enum HitOutcome { Ignored, Damaged, Blocked, Stunned, Destroyed }

/// <summary>
/// Base for everything in a room. <see cref="Entity.Position"/> is the feet point: hitboxes sit
/// around it and draw order is the feet Y, which gives correct top-down overlap for free.
/// </summary>
public abstract class Actor(PlayScene world) : Entity, IStateHashed
{
    public PlayScene World { get; } = world;
    protected Sprites Art => World.Art;
    public override float Depth => Position.Y;

    /// <summary>
    /// State hash: the actor's kind, position and whether it is alive, then its own state (<see cref="HashOwnState"/>).
    /// The position held for blending between updates (<see cref="RememberPosition"/>) is drawing state and stays out.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(GetType().Name);
        hash.Add(Position);
        hash.Add(Alive);
        HashOwnState(hash);
    }

    /// <summary>Adds the subclass's simulation state (timers, velocities, flags); nothing by default (static scenery).</summary>
    protected virtual void HashOwnState(StateHasher hash) { }

    /// <summary>Blocks the hero and enemies (chests, pots, closed doors, NPCs).</summary>
    public virtual bool IsSolid => false;
    /// <summary>The rectangle other bodies collide with when <see cref="IsSolid"/>.</summary>
    public virtual RectF SolidBox => Bounds;
    /// <summary>Drawn under everything else in the same pass (shadows, floor decals).</summary>
    public virtual void DrawShadow(IRenderer r) { }
    /// <summary>Adds light in dark rooms.</summary>
    public virtual void EmitLight(LightMap light) { }

    private Vector2 _previousPosition;
    private bool _hasPrevious;

    /// <summary>Remembers the position at the start of an update: the start of the motion drawn between updates.</summary>
    public void RememberPosition()
    {
        _previousPosition = Position;
        _hasPrevious = true;
    }

    /// <summary>Draws at the current position until the next update, so a jump (teleport, room change) never streaks.</summary>
    public void SnapDrawPosition() => _hasPrevious = false;

    /// <summary>
    /// Where to draw the actor <paramref name="blend"/> (0..1) of the way from the previous update to the current
    /// one. At 1 (fixed-step runs) it is exactly <see cref="Entity.Position"/>.
    /// </summary>
    public Vector2 DrawPositionAt(float blend) => Blend(_hasPrevious ? _previousPosition : Position, Position, blend);

    /// <summary>The position to draw this frame (<see cref="DrawPositionAt"/> at the scene's blend).</summary>
    protected Vector2 DrawPosition => DrawPositionAt(World.DrawBlend);

    /// <summary>Lerp that returns <paramref name="current"/> exactly when there is nothing to blend.</summary>
    public static Vector2 Blend(Vector2 previous, Vector2 current, float blend)
        => blend >= 1 || previous == current ? current : Vector2.Lerp(previous, current, blend);

    protected void DrawSprite(IRenderer r, Sprite sprite, Vector2 at, bool flipX = false, Color? tint = null)
        => r.Draw(sprite, new Vector2(MathF.Round(at.X), MathF.Round(at.Y)), tint, flipX);

    protected TileMoveResult Move(Vector2 delta, bool flying = false, bool throughActors = false) => World.MoveActor(this, delta, flying, throughActors);
}

/// <summary>Something the hero can talk to, read or open by pressing confirm while facing it.</summary>
public interface IInteractable
{
    bool CanInteract { get; }
    void Interact(Hero hero);
}

/// <summary>Something that reacts to the sword or the boomerang.</summary>
public interface IHittable
{
    RectF Hurtbox { get; }
    HitOutcome OnHit(in Hit hit);
}

/// <summary>A short sprite animation that removes itself when done (puffs, sparks).</summary>
public sealed class Effect : Actor
{
    private readonly IReadOnlyList<Sprite> _frames;
    private readonly float _fps;
    private float _time;
    private readonly float _depthBias;

    public Effect(PlayScene world, Vector2 at, IReadOnlyList<Sprite> frames, float fps, float depthBias = 8) : base(world)
    {
        Position = at;
        _frames = frames;
        _fps = fps;
        _depthBias = depthBias;
        Hitbox = new RectF(-4, -4, 8, 8);
    }

    public override float Depth => Position.Y + _depthBias;

    /// <summary>State hash: the animation time, which decides when the effect removes itself.</summary>
    protected override void HashOwnState(StateHasher hash) => hash.Add(_time);

    public override void Update(float dt)
    {
        _time += dt;
        if (_time * _fps >= _frames.Count) Destroy();
    }

    public override void Draw(IRenderer r)
    {
        int f = Math.Min(_frames.Count - 1, (int)(_time * _fps));
        DrawSprite(r, _frames[f], DrawPosition);
    }
}

/// <summary>Static, Y-sorted scenery drawn over its solid tile footprint (trees, houses, pillars).</summary>
public sealed class Prop : Actor
{
    private readonly Sprite _sprite;

    public Prop(PlayScene world, Vector2 bottomLeft, Sprite sprite, float depthOffset = 0) : base(world)
    {
        _sprite = sprite;
        Position = bottomLeft;
        DepthY = bottomLeft.Y + depthOffset;
        Hitbox = new RectF(0, -sprite.Height, sprite.Width, sprite.Height);
    }

    private float DepthY { get; }
    public override float Depth => DepthY;
    public override void Draw(IRenderer r) => r.Draw(_sprite, Position);
}

/// <summary>An animated flame on a wall torch or brazier; also the room's main light source.</summary>
public sealed class Flame : Actor
{
    private readonly float _seed;
    private readonly float _radius;

    public Flame(PlayScene world, Vector2 at, float radius) : base(world)
    {
        Position = at;
        _radius = radius;
        _seed = at.X * 0.37f + at.Y * 0.11f;
        Hitbox = new RectF(-4, -10, 8, 10);
    }

    public override float Depth => Position.Y + 1;

    public override void Update(float dt)
    {
        if (World.Rng.NextDouble() < dt * 3)
            World.Glows.Emit(Position + new Vector2(0, -8), Fx.Ember, 1);
    }

    public override void Draw(IRenderer r)
    {
        int f = (int)((World.Age * 9 + _seed) % 4);
        DrawSprite(r, Art.Frames("flame")[f], Position);
    }

    public override void EmitLight(LightMap light)
    {
        float flicker = 1 + MathF.Sin(World.Age * 11 + _seed) * 0.04f + MathF.Sin(World.Age * 23 + _seed * 2) * 0.03f;
        light.Add(Position + new Vector2(0, -4), _radius * flicker, 1f, Pal.TorchGlow);
    }
}
