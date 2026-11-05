using System.Numerics;
using MeadowRun.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace MeadowRun.Gameplay.Actors;

/// <summary>
/// Base for every object in a stage: an entity of the stage's world (<see cref="Stage.Objects"/>) whose transform holds
/// its position. Simulation lives in <see cref="Tick"/> (no drawing or audio), presentation in <see cref="Render"/>, so
/// the stage can run headless in tests. Constructing an actor creates its entity; <see cref="Stage.Spawn"/> attaches
/// it, which is when the stage's walks start to see it.
/// </summary>
internal abstract class Actor : IStateHashed
{
    protected Actor(Stage stage, Vector2 position)
    {
        Stage = stage;
        Id = stage.Objects.Create();
        Position = position;
    }

    public Stage Stage { get; }
    /// <summary>The actor's entity in <see cref="Stage.Objects"/>.</summary>
    public EntityId Id { get; }
    /// <summary>World position in pixels: the entity's transform.</summary>
    public Vector2 Position
    {
        get => Stage.Objects.Local(Id).Position2D;
        set => Stage.Objects.Local(Id).Position2D = value;
    }
    /// <summary>Hitbox relative to <see cref="Position"/>.</summary>
    public RectF Hitbox { get; set; } = new(0, 0, 16, 16);
    /// <summary>The hitbox in world pixels.</summary>
    public RectF Bounds => Hitbox.Offset(Position);
    /// <summary>The centre of <see cref="Bounds"/>.</summary>
    public Vector2 Center => Bounds.Center;
    /// <summary>False once <see cref="Destroy"/> was called: the stage's walks skip the actor at once.</summary>
    public bool Alive => Stage.Objects.IsAlive(Id);
    public Vector2 Velocity;
    public float Age { get; private set; }
    /// <summary>Actors sleep until the camera approaches them, so encounters play out as designed.</summary>
    public virtual bool AlwaysActive => false;
    public bool Awake { get; set; }

    /// <summary>Removes the actor: walks skip it at once; the world releases its entity when the step ends.</summary>
    public void Destroy() => Stage.Objects.Destroy(Id);

    /// <summary>A jump (respawn, eruption): frames draw the actor where it is now instead of sliding it from where it was.</summary>
    protected void Teleport() => Stage.Objects.Teleport(Id);

    /// <summary>Where frames draw the actor: between its last two positions, as the world interpolates it. Never read by the rules.</summary>
    public Vector2 DrawPosition => Stage.Objects.DrawPosition2D(Id);
    /// <summary>The hitbox at <see cref="DrawPosition"/>.</summary>
    protected RectF DrawBounds => Hitbox.Offset(DrawPosition);
    /// <summary>The centre of <see cref="DrawBounds"/>.</summary>
    public Vector2 DrawCenter => DrawBounds.Center;
    /// <summary>The feet (bottom centre of <see cref="DrawBounds"/>), where most sprites are anchored.</summary>
    protected Vector2 DrawFeet
    {
        get
        {
            var b = DrawBounds;
            return new(b.Center.X, b.Bottom);
        }
    }

    public void Update(float dt)
    {
        if (!Awake)
        {
            if (!AlwaysActive && !Stage.IsNearView(Bounds, 40)) return;
            Awake = true;
        }
        Age += dt;
        Tick(dt);
    }

    protected abstract void Tick(float dt);

    /// <summary>
    /// Hashes the actor's kind, whether it is alive and awake, its position, velocity and age; subclasses add their own
    /// rule state. Leaves out fixed setup (hitbox, home).
    /// </summary>
    public virtual void HashState(StateHasher hash)
    {
        hash.Add(GetType().Name);
        hash.Add(Alive);
        hash.Add(Awake);
        hash.Add(Position);
        hash.Add(Velocity);
        hash.Add(Age);
    }

    /// <summary>Draws the actor; positions come from <see cref="DrawPosition"/> and its relatives, never from the rules' ones.</summary>
    public abstract void Render(IRenderer r, Assets a, float time);

    /// <summary>Moves through tiles with gravity; returns the collision result.</summary>
    protected TileMoveResult MoveBody(float dt, float gravity = 900f, float terminal = 260f)
    {
        Velocity.Y = MathF.Min(Velocity.Y + gravity * dt, terminal);
        var result = TileMotion.Move(Bounds, Velocity * dt, Stage.Level.Grid);
        Position = result.Position - Hitbox.Position;
        if (result.HitFloor && Velocity.Y > 0) Velocity.Y = 0;
        if (result.HitCeiling && Velocity.Y < 0) Velocity.Y = 0;
        return result;
    }

    /// <summary>True when the tile ahead-and-below is empty (used by ledge-aware walkers).</summary>
    protected bool LedgeAhead(int direction)
    {
        var b = Bounds;
        float x = direction > 0 ? b.Right + 1 : b.Left - 1;
        var probe = new Vector2(x, b.Bottom + 2);
        int tx = (int)MathF.Floor(probe.X / Level.TileSize), ty = (int)MathF.Floor(probe.Y / Level.TileSize);
        return (Stage.Level.Grid.FlagsAt(tx, ty) & (TileFlags.Solid | TileFlags.OneWay)) == 0;
    }

    /// <summary>Feet position (bottom centre of the hitbox), where most sprites are anchored.</summary>
    public Vector2 Feet => new(Bounds.Center.X, Bounds.Bottom);

    protected static Vector2 Snap(Vector2 v) => new(MathF.Round(v.X), MathF.Round(v.Y));
}
