# Many alike objects and Entity Component Systems

A scene's world keeps each object as an entity with a transform and the attachments your game gives it. That is the
model every example uses and the right one for almost every game. This page is for the games it was not shaped for:
hundreds of thousands of projectiles, particles of your own or simulated creatures that all carry the same few
numbers, where an Entity Component System (an ECS: objects as rows of plain data, walked column by column) does the
same work in a fraction of the time and memory.

MicroEngine has no ECS of its own and needs none: a game adds the library it prefers and runs it beside the scene's
world, through the same places every scene already has.

| To do this | Use |
| --- | --- |
| Keep a few thousand objects of different kinds, with children, bodies and behaviour | the scene's world (`Objects`) |
| Walk every object of one kind | `Objects.All<T>()` |
| Keep hundreds of thousands of alike objects, or create and destroy thousands every step | a component store beside the world |
| Give one of those objects a child, a physics body or a 3D mesh | an entity of the world linked to its row ([below](#an-object-on-both-sides)) |
| Find the objects near a point, in either model | `SpatialIndex2D<T>` ([Large worlds and nearby objects](large-worlds.md)) |
| Draw particles | `ParticleSystem`, which needs neither |

## How far the scene's world goes

Further than most games need. On a desktop, one update of a world of 100,000 entities that walks them all through
`All<T>` and moves each one through `Local` takes about half a millisecond, of the 16.7 a 60 Hz update has; a million
take about 6.5. Replacing a tenth of those 100,000 every step (10,000 destroyed and 10,000 created) takes about 1.7
milliseconds. Asking for a combination ("every entity with a `Motion` and a `Vitals`") is a walk over one attachment
with `TryGet<T>` for the other: about 1 millisecond for three such questions over 100,000 entities.

Two things to know while the world is the store:

- **Reuse the attachments of destroyed objects.** An attachment is an object of your own class. A game that creates a
  new one for every projectile produces garbage in proportion to what it spawns: 10,000 spawns a step are 400 KB a
  step and a collection every few dozen frames, each a pause of up to a few milliseconds. Keep the attachments of
  destroyed entities in a `Stack<T>` and take them from there; then spawning allocates nothing.
- **An entity costs about 240 bytes** with one attachment (two transforms, its links in the hierarchy, its
  attachment). A million of them are about 240 MB.

What limits a game long before the world does is what it draws and what collides: a frame of a few hundred thousand
sprites, or a few thousand moving bodies, fills an update whichever way the objects are stored.

## What a component store changes

An ECS library keeps objects as rows of structs, one array per component, the rows with the same components side by
side. Compared with the scene's world on the same work, on a desktop:

| | Scene's world | Component store |
| --- | --- | --- |
| Moving every object, per object | 4.5 to 6.5 ns | 1.5 to 2.5 ns |
| Three questions about combinations over 100,000 objects | about 1 ms | about 0.1 ms |
| Replacing 10,000 of 100,000 objects in a step | about 1.7 ms | 0.5 to 0.9 ms |
| A game step of 375,000 objects (a swarm, shots, deaths, all drawn) | about 22 ms: over a 60 Hz update | about 6.5 ms |
| Largest such game step within a 60 Hz update | about 280,000 objects | 600,000 to 675,000 objects |
| Memory per object (position, previous position, velocity) | about 240 bytes | 55 to 65 bytes |
| Drawing each object as a sprite | the same | the same |
| Garbage when objects spawn | none with pooled attachments | none |

So the store matters when the objects are very many. Under about 75,000 objects simulated per step the difference
is a fraction of a millisecond, and the world's hierarchy, attachments of any class and interpolation are worth more.
From about 200,000 objects that are created and destroyed as the game goes, the world's objects end up scattered
in memory and a step costs more than their count suggests, while a store keeps its rows packed; there, or when
memory is tight (a browser tab), a store gives a game back milliseconds and megabytes. It does not make drawing or
physics cheaper.

## A store beside the world

The `EcsSwarm` sample keeps a swarm of 20,000 in a store (it uses [Arch](https://github.com/genaray/Arch), an
Apache-2.0 library) next to a beacon and a few carriers that are entities of the scene's world. The library is a
package reference of the game; the scene joins the two in five places.

**1. The scene owns the store.** It is created in `Enter` and destroyed in `Exit`. Components are structs; the
position is kept twice so drawing can blend between two steps.

<!-- code: samples/EcsSwarm/SwarmScene.cs#components -->
```csharp
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
```

<!-- code: samples/EcsSwarm/SwarmScene.cs#store -->
```csharp
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
```

**2. The step runs the systems.** `Update` is called at the fixed step, so a system is a loop in it (or a method it
calls, in the order you call them). Rows are added and removed after the walks, never during one.

<!-- code: samples/EcsSwarm/SwarmScene.cs#step -->
```csharp
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
```

**3. Drawing blends by hand.** The world interpolates its entities (`DrawPosition2D`); for the store's rows the scene
does it, with the world's `Blend`: the frame's blend while the scene plays, and 1 while it is frozen under an overlay
or in a transition, so a paused swarm stands still. Read `Objects` once in `Enter` (creating an entity does) so the
scene has a world to ask.

<!-- code: samples/EcsSwarm/SwarmScene.cs#draw -->
```csharp
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
```

**4. The state hash includes the store.** The engine hashes the simulation state after every step for replays and
determinism checks, and it cannot see the store. `HashState` adds it.

<!-- code: samples/EcsSwarm/SwarmScene.cs#hash -->
```csharp
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
```

### An object on both sides

**5. A row that needs the engine gets an entity.** Physics bodies belong to an `EntityId`
(`IPhysicsWorld2D.CreateBody`), children hang from an entity, and the 3D renderer draws the world's hierarchy. An
object of the store that needs any of them gets an entity of the world as well: the row carries the entity's id in a
component, the entity carries the row in an attachment, and `IDestroyedCallback` tells the attachment when the world
releases the entity.

<!-- code: samples/EcsSwarm/SwarmScene.cs#twin -->
```csharp
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
```

<!-- code: samples/EcsSwarm/SwarmScene.cs#link -->
```csharp
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
```

Everything else the engine offers takes values, not entities, so a row uses it directly: a `SpatialIndex2D<T>` can
file the library's own row handles, the renderer draws a sprite at any position, sounds play at a pan your code
computes, and a tile map answers `TileGrid` and `TileMotion` questions for any rectangle.

## Identity and lifetime, side by side

| | Scene's world | The store (Arch in the sample) |
| --- | --- | --- |
| Identity | `EntityId`: a slot and a generation; a stale id never reaches the entity that reused its slot | the library's handle (`Entity`: an id and a version); `IsAlive` is false once its row is gone, also after a new row takes the id |
| Creation | at once; a walk already running does not visit it | at once, outside a walk |
| Destruction | at once for every question; the transform and attachments stay readable until the step ends, then `IDestroyedCallback` runs | at once, outside a walk; nothing is readable afterwards |
| Order of a walk | attach order, stable | the library's rows; removing a row moves the last one into its place |
| Stepping | by the scene, with the world's clock (`Objects.Clock`: time scale, freezes) | by your `Update`, with the `dt` it receives (already scaled by that clock) |
| Interpolation | by the world | by your `Draw`, with `Objects.Blend` |
| State hash | included | added by your `HashState` |

Two rules keep the sides in step: remove a linked object from one side and let the link remove the other (the
sample's `RemoveRow` and `Carrier.OnDestroyed`), and keep the library's handle, not a bare index, wherever a row is
remembered across steps.

## Choosing and replacing the library

The engine references no ECS library, and no part of it (graphics, physics, input, audio, scenes) knows one is there:
the five places above are the whole surface between the two. Replacing the library, or writing a store of your own,
changes those five places in your scenes and nothing else.

What to ask of a library:

- **Components as structs and walks over spans**, so a system is a loop over arrays.
- **No allocation once its buffers have grown**, like the rest of a frame.
- **A build that publishes trimmed without warnings**, if the game is published trimmed, with NativeAOT or for
  browsers ([Trimmed and NativeAOT publishing](platforms.md#trimmed-and-nativeaot-publishing)). Libraries find
  component types at run time unless told; under NativeAOT each component type has to be named before the first row
  (the sample's `ArrayRegistry.Add<T>()` calls, which other builds do not need).
- **A deterministic order** of its rows for the same sequence of calls, if the game relies on replays.

[Arch](https://github.com/genaray/Arch) (Apache-2.0) and [Friflo.Engine.ECS](https://github.com/friflo/Friflo.Engine.ECS)
(MIT) both fit and walk their rows at the same speed. They differ in what creating and destroying a row costs, which
matters to a game that replaces thousands of objects every step, and in publishing: Arch publishes trimmed and with
NativeAOT without warnings; Friflo.Engine.ECS reports trimming warnings and asks for its component types to be
registered in a NativeAOT build.

## Limits

- The engine does not step, interpolate, hash or save a store: the scene does, as shown.
- An object that needs a physics body, a child or a mesh needs an entity of the world; a store of bodies is not a
  physics world.
- A walk over a store must not add or remove rows; collect them and apply the changes after it.
- The figures on this page are for one desktop processor and scale with it; the proportions between the two models
  are what carries over.
