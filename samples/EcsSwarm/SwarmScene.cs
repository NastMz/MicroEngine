using System.Numerics;
using Arch.Core;
using MicroEngine;
using MicroEngine.Ecs;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace EcsSwarm;

#region components
// The store's components: plain structs, one row per object. Each says what of it is simulation state.
public struct Position : IStateHashed
{
    public Vector2 Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

public struct Velocity : IStateHashed
{
    public Vector2 Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

/// <summary>A member of the swarm: how hard it pulls towards the beacon.</summary>
public struct Chase : IStateHashed
{
    public float Pull;
    public readonly void HashState(StateHasher hash) => hash.Add(Pull);
}

/// <summary>What is left of a caught member: gone when the steps run out.</summary>
public struct Spark : IStateHashed
{
    public int StepsLeft;
    public readonly void HashState(StateHasher hash) => hash.Add(StepsLeft);
}
#endregion

/// <summary>
/// The scene uses the two models together. The scene's world (<see cref="Scene.Objects"/>) holds what needs a
/// transform hierarchy: the beacon with its halo, and each carrier with its flag. The component store holds everything
/// that is many and alike: the swarm and its sparks, and the carriers' motion.
/// </summary>
public sealed class SwarmScene : Scene
{
    public const int Members = 20_000, Carriers = 6, SparkSteps = 20;
    public const float CatchRadius = 5, MaxSpeed = 70, BeaconSpeed = 90;

    #region store
    // What the store's rows are made of, declared once: every component is simulation state here, a position is drawn
    // between two steps, and a linked row's position is its entity's.
    public static readonly EcsComponents Components = new EcsComponents()
        .AddState<Position>().AddState<Velocity>().AddState<Chase>().AddState<Spark>()
        .Interpolate<Position>(static (in Position from, in Position to, float blend) => new Position { Value = Vector2.Lerp(from.Value, to.Value, blend) })
        .SyncTransform<Position>(static (in Position position, ref Transform local) => local.Position2D = position.Value);

    private static readonly QueryDescription Moving = new QueryDescription().WithAll<Position, Velocity>();
    private static readonly QueryDescription Chasers = new QueryDescription().WithAll<Position, Velocity, Chase>();
    private static readonly QueryDescription Sparks = new QueryDescription().WithAll<Spark>();
    private static readonly QueryDescription Carried = new QueryDescription().WithAll<Position, Velocity, SceneLink>();
    private static readonly QueryDescription DrawnMembers = new QueryDescription().WithAll<Position, Chase>();
    private static readonly QueryDescription DrawnSparks = new QueryDescription().WithAll<Position, Spark>();

    private EcsStore _ecs = null!;
    private EcsInterpolation<Position> _places = null!;

    public override void Enter()
    {
        // The store lives with the scene, and its systems run in this order after the scene's Update has moved the beacon.
        _ecs = this.AddEcs(Components);
        _ecs.LateSystems.Add("move", Move);
        _ecs.LateSystems.Add("sparks", BurnOut);      // before the chase, so a spark made this step lasts all its steps
        _ecs.LateSystems.Add("chase", ChaseBeacon);
        _ecs.LateSystems.Add("bounce", Bounce);
        _places = _ecs.Interpolated<Position>();

        // The beacon is an entity of the scene's world, with a child that circles it.
        Beacon = Objects.Create();
        Objects.Local(Beacon).Position2D = new Vector2(Game.Width / 2f, Game.Height / 2f);
        Halo = Objects.Create(Beacon);

        for (int i = 0; i < Members; i++) AddMember(RandomPoint());
        for (int i = 0; i < Carriers; i++) AddCarrier(RandomPoint(), new Vector2(Game.Random.Next(-20, 21), Game.Random.Next(-20, 21)));
    }
    #endregion

    /// <summary>The entity the player moves.</summary>
    public EntityId Beacon { get; private set; }
    /// <summary>The beacon's child.</summary>
    public EntityId Halo { get; private set; }
    /// <summary>The scene's component store.</summary>
    public EcsStore Ecs => _ecs;
    /// <summary>The store's rows, for whoever inspects the scene (its tests).</summary>
    public World Rows => _ecs.World;
    /// <summary>Members caught by the beacon so far.</summary>
    public int Caught { get; private set; }
    /// <summary>The carriers' world entities, in the order they were added.</summary>
    public List<EntityId> CarrierTwins { get; } = [];

    private Vector2 RandomPoint() => new(Game.Random.NextSingle() * Game.Width, Game.Random.NextSingle() * Game.Height);

    private void AddMember(Vector2 at) => _ecs.World.Create(
        new Position { Value = at }, new Velocity(), new Chase { Pull = 60 + Game.Random.Next(120) });

    #region link
    // A carrier is one row in the store and one entity in the world, linked: the row's position moves the entity,
    // each is found from the other, and destroying either removes both when the step ends.
    private void AddCarrier(Vector2 at, Vector2 velocity)
    {
        var twin = Objects.Create();
        Objects.Local(twin).Position2D = at;
        var flag = Objects.Create(twin);                      // a child: the world's hierarchy keeps it with the carrier
        Objects.Local(flag).Position2D = new Vector2(0, -7);
        var row = _ecs.World.Create(new Position { Value = at }, new Velocity { Value = velocity });
        _ecs.Link(row, twin);
        CarrierTwins.Add(twin);
    }
    #endregion

    #region step
    private readonly List<Entity> _caught = [], _spent = [];
    private Predicate<EntityId>? _gone;   // kept, so asking each step allocates nothing
    private Vector2 _target;

    public override void Update(float dt)
    {
        // The beacon: an entity of the world, moved through its transform. The store's systems run after this.
        ref var beacon = ref Objects.Local(Beacon);
        _target = Vector2.Clamp(beacon.Position2D + Input.Vector("left", "right", "up", "down") * BeaconSpeed * dt, Vector2.Zero, new Vector2(Game.Width, Game.Height));
        beacon.Position2D = _target;
        Objects.Local(Halo).Position2D = new Vector2(MathF.Cos(Age * 3), MathF.Sin(Age * 3)) * 9;
        if (Input.Pressed("confirm") && CarrierTwins.Count > 0) Objects.Destroy(CarrierTwins[^1]);   // from the world's side
        CarrierTwins.RemoveAll(_gone ??= twin => !Objects.IsAlive(twin));
    }

    private static void Move(EcsStore ecs, float dt)
    {
        foreach (ref var chunk in ecs.World.Query(in Moving))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var v = chunk.GetSpan<Velocity>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++) p[i].Value += v[i].Value * dt;
        }
    }

    // The swarm: steer towards the beacon; a member that reaches it is caught.
    private void ChaseBeacon(EcsStore ecs, float dt)
    {
        var world = ecs.World;
        foreach (ref var chunk in world.Query(in Chasers))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var v = chunk.GetSpan<Velocity>()[..chunk.Count];
            var chase = chunk.GetSpan<Chase>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++)
            {
                var away = p[i].Value - _target;
                float distance = away.Length();
                if (distance < CatchRadius) { _caught.Add(chunk.Entity(i)); continue; }
                var velocity = v[i].Value - away / distance * (chase[i].Pull * dt);
                float speed = velocity.Length();
                v[i].Value = speed > MaxSpeed ? velocity * (MaxSpeed / speed) : velocity;
            }
        }
        // Rows are added and removed after the walk, never during one.
        foreach (var row in _caught)
        {
            var at = world.Get<Position>(row).Value;
            var velocity = world.Get<Velocity>(row).Value;
            world.Destroy(row);
            world.Create(new Position { Value = at }, new Velocity { Value = -velocity * 0.5f }, new Spark { StepsLeft = SparkSteps });
            AddMember(Game.Random.Next(2) == 0
                ? new Vector2(Game.Random.Next(2) * Game.Width, Game.Random.NextSingle() * Game.Height)
                : new Vector2(Game.Random.NextSingle() * Game.Width, Game.Random.Next(2) * Game.Height));
            Caught++;
        }
        _caught.Clear();
    }

    // Carriers bounce off the edges. Their place reaches their world entity, which carries the flag, when the step ends.
    private void Bounce(EcsStore ecs, float dt)
    {
        foreach (ref var chunk in ecs.World.Query(in Carried))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            var v = chunk.GetSpan<Velocity>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Value.X < 0 || p[i].Value.X > Game.Width) v[i].Value.X = -v[i].Value.X;
                if (p[i].Value.Y < 0 || p[i].Value.Y > Game.Height) v[i].Value.Y = -v[i].Value.Y;
            }
        }
    }

    private void BurnOut(EcsStore ecs, float dt)
    {
        foreach (ref var chunk in ecs.World.Query(in Sparks))
        {
            var spark = chunk.GetSpan<Spark>()[..chunk.Count];
            for (int i = 0; i < spark.Length; i++)
                if (--spark[i].StepsLeft <= 0) _spent.Add(chunk.Entity(i));
        }
        foreach (var row in _spent) ecs.World.Destroy(row);
        _spent.Clear();
    }
    #endregion

    #region draw
    public override void Draw(IRenderer r)
    {
        // The store keeps each position from the start of the step; a row is drawn between that and where it is now,
        // and exactly where it is while the scene is frozen under an overlay. The world's entities are blended by the world.
        var dot = ((SwarmGame)Game).Dot;
        foreach (ref var chunk in _ecs.World.Query(in DrawnMembers))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++) r.Draw(dot, _places.Blended(chunk.Entity(i), in p[i]).Value, Member);
        }
        foreach (ref var chunk in _ecs.World.Query(in DrawnSparks))
        {
            var p = chunk.GetSpan<Position>()[..chunk.Count];
            for (int i = 0; i < p.Length; i++) r.Draw(dot, _places.Blended(chunk.Entity(i), in p[i]).Value, Ember);
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
    // The store adds its rows to the state hash itself. The scene adds only what it keeps outside the store.
    public override void HashState(StateHasher hash) => hash.Add(Caught);
    #endregion
}
