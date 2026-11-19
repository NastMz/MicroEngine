using System.Numerics;
using Arch.Core;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace EcsSwarm;

#region components
// The store's components: plain structs, one row per object. A position kept twice, so drawing can blend from where
// the object was when the step started to where it is now.
public struct Position { public Vector2 Value; }
public struct Previous { public Vector2 Value; }
public struct Velocity { public Vector2 Value; }
/// <summary>A member of the swarm: how hard it pulls towards the beacon.</summary>
public struct Chase { public float Pull; }
/// <summary>What is left of a caught member: gone when the steps run out.</summary>
public struct Spark { public int StepsLeft; }
/// <summary>The entity of the scene's world that stands for this row (a carrier's, which has a child).</summary>
public struct Twin { public EntityId Id; }
#endregion

#region twin
/// <summary>
/// The attachment of a carrier's world entity. It remembers the carrier's row in the store, and the world tells it
/// when the entity is released, so destroying the entity removes the row as well.
/// </summary>
public sealed class Carrier(SwarmScene scene, Entity row) : IDestroyedCallback
{
    /// <summary>The carrier's row in the store.</summary>
    public Entity Row => row;
    /// <inheritdoc/>
    public void OnDestroyed(EntityId entity) => scene.Forget(row);
}
#endregion

/// <summary>
/// The scene joins the two models. The scene's world (<see cref="Scene.Objects"/>) holds what needs a transform
/// hierarchy: the beacon with its halo, and each carrier with its flag. The component store holds everything that is
/// many and alike: the swarm and its sparks, and the carriers' motion.
/// </summary>
public sealed class SwarmScene : Scene
{
    public const int Members = 20_000, Carriers = 6, SparkSteps = 20;
    public const float CatchRadius = 5, MaxSpeed = 70, BeaconSpeed = 90;

    #region store
    // The store belongs to the scene: created when the scene enters, destroyed when it exits. The engine never sees it.
    private World _rows = null!;
    private static readonly QueryDescription Moving = new QueryDescription().WithAll<Position, Previous, Velocity>();
    private static readonly QueryDescription Chasers = new QueryDescription().WithAll<Position, Velocity, Chase>();
    private static readonly QueryDescription Sparks = new QueryDescription().WithAll<Spark>();
    private static readonly QueryDescription Carried = new QueryDescription().WithAll<Position, Velocity, Twin>();
    private static readonly QueryDescription DrawnMembers = new QueryDescription().WithAll<Position, Previous, Chase>();
    private static readonly QueryDescription DrawnSparks = new QueryDescription().WithAll<Position, Previous, Spark>();
    private readonly List<Entity> _caught = [], _spent = [];

    // Only a NativeAOT build needs this: there the library cannot make an array of a component type nobody named,
    // so each one is named once, before the first row.
    static SwarmScene()
    {
        ArrayRegistry.Add<Position>();
        ArrayRegistry.Add<Previous>();
        ArrayRegistry.Add<Velocity>();
        ArrayRegistry.Add<Chase>();
        ArrayRegistry.Add<Spark>();
        ArrayRegistry.Add<Twin>();
    }

    public override void Enter()
    {
        _rows = World.Create();

        // The beacon is an entity of the scene's world, with a child that circles it.
        Beacon = Objects.Create();
        Objects.Local(Beacon).Position2D = new Vector2(Game.Width / 2f, Game.Height / 2f);
        Halo = Objects.Create(Beacon);

        for (int i = 0; i < Members; i++) AddMember(RandomPoint());
        for (int i = 0; i < Carriers; i++) AddCarrier(RandomPoint(), new Vector2(Game.Random.Next(-20, 21), Game.Random.Next(-20, 21)));
    }

    public override void Exit() => World.Destroy(_rows);
    #endregion

    /// <summary>The entity the player moves.</summary>
    public EntityId Beacon { get; private set; }
    /// <summary>The beacon's child.</summary>
    public EntityId Halo { get; private set; }
    /// <summary>The component store, for whoever inspects the scene (its tests).</summary>
    public World Rows => _rows;
    /// <summary>Members caught by the beacon so far.</summary>
    public int Caught { get; private set; }
    /// <summary>The carriers' world entities, in the order they were added.</summary>
    public List<EntityId> CarrierTwins { get; } = [];

    private Vector2 RandomPoint() => new(Game.Random.NextSingle() * Game.Width, Game.Random.NextSingle() * Game.Height);

    private void AddMember(Vector2 at) => _rows.Create(
        new Position { Value = at }, new Previous { Value = at }, new Velocity(), new Chase { Pull = 60 + Game.Random.Next(120) });

    #region link
    // A carrier is one row in the store and one entity in the world. The row carries the entity's id (Twin); the
    // entity carries the row (Carrier). Each side can find the other, and removing one removes both.
    private void AddCarrier(Vector2 at, Vector2 velocity)
    {
        var twin = Objects.Create();
        Objects.Local(twin).Position2D = at;
        var flag = Objects.Create(twin);                      // a child: the world's hierarchy keeps it with the carrier
        Objects.Local(flag).Position2D = new Vector2(0, -7);
        var row = _rows.Create(new Position { Value = at }, new Previous { Value = at }, new Velocity { Value = velocity }, new Twin { Id = twin });
        Objects.Attach(twin, new Carrier(this, row));
        CarrierTwins.Add(twin);
    }

    /// <summary>Removes a carrier from the store's side: its row goes now, its entity (and the flag) with the step.</summary>
    public void RemoveRow(Entity row)
    {
        if (!_rows.IsAlive(row)) return;      // a handle whose row is gone, even if a newer row took its place
        var twin = _rows.Get<Twin>(row).Id;
        _rows.Destroy(row);
        Objects.Destroy(twin);
    }

    /// <summary>Called by a carrier's entity when the world releases it: the row goes too, if it is still there.</summary>
    internal void Forget(Entity row)
    {
        if (_rows.IsAlive(row)) _rows.Destroy(row);
    }
    #endregion

    #region step
    public override void Update(float dt)
    {
        // The beacon: an entity of the world, moved through its transform.
        ref var beacon = ref Objects.Local(Beacon);
        var target = beacon.Position2D + Input.Vector("left", "right", "up", "down") * BeaconSpeed * dt;
        target = Vector2.Clamp(target, Vector2.Zero, new Vector2(Game.Width, Game.Height));
        beacon.Position2D = target;
        Objects.Local(Halo).Position2D = new Vector2(MathF.Cos(Age * 3), MathF.Sin(Age * 3)) * 9;
        if (Input.Pressed("confirm") && CarrierTwins.Count > 0) Objects.Destroy(CarrierTwins[^1]);   // from the world's side

        // Every row that moves: remember where it was, then move it.
        foreach (ref var chunk in _rows.Query(in Moving))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var was = chunk.GetSpan<Previous>()[..chunk.Count];
            var v = chunk.GetSpan<Velocity>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++)
            {
                was[i].Value = p[i].Value;
                p[i].Value += v[i].Value * dt;
            }
        }

        // The swarm: steer towards the beacon; a member that reaches it is caught.
        foreach (ref var chunk in _rows.Query(in Chasers))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var v = chunk.GetSpan<Velocity>()[..chunk.Count];
            var chase = chunk.GetSpan<Chase>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++)
            {
                var away = p[i].Value - target;
                float distance = away.Length();
                if (distance < CatchRadius) { _caught.Add(chunk.Entity(i)); continue; }
                var velocity = v[i].Value - away / distance * (chase[i].Pull * dt);
                float speed = velocity.Length();
                v[i].Value = speed > MaxSpeed ? velocity * (MaxSpeed / speed) : velocity;
            }
        }

        // Carriers bounce off the edges, and their place goes to their world entity, which carries the flag.
        foreach (ref var chunk in _rows.Query(in Carried))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var v = chunk.GetSpan<Velocity>()[..chunk.Count];
            var twin = chunk.GetSpan<Twin>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Value.X < 0 || p[i].Value.X > Game.Width) v[i].Value.X = -v[i].Value.X;
                if (p[i].Value.Y < 0 || p[i].Value.Y > Game.Height) v[i].Value.Y = -v[i].Value.Y;
                Objects.Local(twin[i].Id).Position2D = p[i].Value;
            }
        }

        foreach (ref var chunk in _rows.Query(in Sparks))
        {
            var spark = chunk.GetSpan<Spark>()[..chunk.Count];
            for (int i = 0; i < spark.Length; i++)
                if (--spark[i].StepsLeft <= 0) _spent.Add(chunk.Entity(i));
        }

        // Rows are added and removed after the walks, never during one.
        foreach (var row in _caught)
        {
            var at = _rows.Get<Position>(row).Value;
            var velocity = _rows.Get<Velocity>(row).Value;
            _rows.Destroy(row);
            _rows.Create(new Position { Value = at }, new Previous { Value = at }, new Velocity { Value = -velocity * 0.5f }, new Spark { StepsLeft = SparkSteps });
            AddMember(Game.Random.Next(2) == 0
                ? new Vector2(Game.Random.Next(2) * Game.Width, Game.Random.NextSingle() * Game.Height)
                : new Vector2(Game.Random.NextSingle() * Game.Width, Game.Random.Next(2) * Game.Height));
            Caught++;
        }
        foreach (var row in _spent) _rows.Destroy(row);
        _caught.Clear();
        _spent.Clear();
        CarrierTwins.RemoveAll(twin => !Objects.IsAlive(twin));
    }
    #endregion

    #region draw
    public override void Draw(IRenderer r)
    {
        // The blend of the scene's world: the frame's blend while the scene plays, 1 while it is frozen under an
        // overlay or in a transition. The store's rows are blended by hand with it; the world's entities by the world.
        float blend = Objects.Blend;
        var dot = ((SwarmGame)Game).Dot;
        foreach (ref var chunk in _rows.Query(in DrawnMembers))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var was = chunk.GetSpan<Previous>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++) r.Draw(dot, Vector2.Lerp(was[i].Value, p[i].Value, blend), Member);
        }
        foreach (ref var chunk in _rows.Query(in DrawnSparks))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var was = chunk.GetSpan<Previous>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++) r.Draw(dot, Vector2.Lerp(was[i].Value, p[i].Value, blend), Ember);
        }

        foreach (var twin in CarrierTwins)
        {
            if (!Objects.IsAlive(twin)) continue;
            r.FillRect(RectF.FromCenter(Objects.DrawPosition2D(twin), 8, 5), Hull);
            foreach (var flag in Objects.Children(twin)) r.FillRect(RectF.FromCenter(Objects.DrawPosition2D(flag), 3, 3), Flag);
        }
        r.FillCircle(Objects.DrawPosition2D(Beacon), CatchRadius, Light);
        r.FillCircle(Objects.DrawPosition2D(Halo), 1.5f, Flag);
        r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
        Font.Draw(r, $"ARROWS: MOVE   Z: DROP A CARRIER   SWARM {Members}   CAUGHT {Caught}", new Vector2(4, 3), Color.White);
    }

    private static readonly Color Member = Color.Hex("73eff7"), Ember = Color.Hex("ffcd75"), Hull = Color.Hex("b13e53"),
        Flag = Color.Hex("a7f070"), Light = Color.Hex("f4f4f4");
    #endregion

    #region hash
    // The store is simulation state the engine cannot see, so the scene adds it to the state hash: a replay or a
    // determinism check then covers the swarm like everything else.
    public override void HashState(StateHasher hash)
    {
        hash.Add(_rows.Size);
        hash.Add(Caught);
        foreach (ref var chunk in _rows.Query(in Moving))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var v = chunk.GetSpan<Velocity>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++)
            {
                hash.Add(p[i].Value);
                hash.Add(v[i].Value);
            }
        }
        foreach (ref var chunk in _rows.Query(in Sparks))
            foreach (ref var spark in chunk.GetSpan<Spark>()[..chunk.Count]) hash.Add(spark.StepsLeft);
    }
    #endregion
}
