using System.Numerics;
using MeadowRun.Art;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.World;

namespace MeadowRun.Gameplay.Actors;

/// <summary>
/// Base for every object in a stage. Simulation lives in <see cref="Tick"/> (no drawing or audio),
/// presentation in <see cref="Render"/>, so the stage can run headless in tests.
/// </summary>
internal abstract class Actor : Entity, IStateHashed
{
    protected Actor(Stage stage, Vector2 position)
    {
        Stage = stage;
        Position = position;
        PreviousPosition = position;
    }

    public Stage Stage { get; }
    public Vector2 Velocity;
    /// <summary>Where the actor was before the last stage update; frames are drawn between it and <see cref="Entity.Position"/>. Never read by the rules.</summary>
    public Vector2 PreviousPosition;
    public float Age { get; private set; }
    /// <summary>Actors sleep until the camera approaches them, so encounters play out as designed.</summary>
    public virtual bool AlwaysActive => false;
    public bool Awake { get; set; }

    public sealed override void Update(float dt)
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
    /// rule state. Leaves out <see cref="PreviousPosition"/> (only drawing reads it) and fixed setup (hitbox, home).
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
