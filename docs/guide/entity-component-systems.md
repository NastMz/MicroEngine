# Entity Component Systems

MicroEngine has two ways to model the objects of a game. The scene's world (`Objects`) keeps each object as an entity
with a transform and attachments of your own classes; it is what every template, tutorial and example uses and the
right model for most games. The optional `MicroEngine.Ecs` package adds the other one: an Entity Component System
(an ECS), where an object is a row of plain components, a behaviour is a system that walks every row with the
components it needs, and what an object can do changes by adding and removing components while it lives.

```xml
<PackageReference Include="MicroEngine.Ecs" Version="0.6.0-alpha.1" />
```

The package is a component store that lives in a scene. It is built on [Arch](https://github.com/genaray/Arch) (an
Apache-2.0 library that the package brings with it) and does not hide it: rows, components and queries are Arch's,
used as its documentation shows. What the package adds is everything between the store and the engine.

| To do this | Use |
| --- | --- |
| Give a scene a component store | `this.AddEcs(components)` in `Enter` |
| Give every scene one | `Scenes.AddEcs(components)` in the game's `Load` |
| Say which component types exist, which are simulation state and which are drawn between steps | `EcsComponents` |
| Run behaviours in a fixed order, before and after the scene's `Update` | `ecs.Systems`, `ecs.LateSystems` |
| Add, remove or destroy from inside a walk | `ecs.Commands` |
| Give a row a transform with children, a physics body or a place in a spatial index | `ecs.Link(row, entity)` |
| Draw a row between two steps | `ecs.Interpolated<T>()` |
| Ask for "everything with this and that but not that" | a query description (`WithAll`, `WithAny`, `WithNone`) |

## Which model fits

Choose by what the game models, not by how many objects it has.

**The scene's world fits when the kinds of object are few and stay what they are.** A player, three kinds of enemy,
pickups and doors: each kind is a class, its state is fields, its rules are the code that reads them. This is the
shortest and plainest way to write such a game. Attachments are walked in the order they were attached, an object can
be changed while the world is being walked, and a rule reads as the `if` it is. Each object can have children, and a
[state machine](state-machines.md) for the states it is in one at a time.

**Components and systems fit when what an object is changes, or kinds overlap.** The signs:

- *Capabilities come and go on a live object*: burning, frozen, shielded, carrying, possessed, selected. On the
  world they are counters and flags in a class, tested in every loop they affect. As components, an effect is there
  or not, and a behaviour it switches off leaves those rows out of its query.
- *Kinds share some behaviours and not others*: a crate that burns and breaks like an enemy but does not move or
  freeze. On the world the shared part becomes a base class or is split into several attachments, and the code that
  assumed one kind is rewritten. As components, the crate is a row with a different set, and no system changes.
- *The game asks about combinations*: every burning thing near here that has no shield. On the world that is a walk
  over everything with a test per condition. As a query it is a description, and only matching rows are walked.
- *The same behaviour is written again* for each kind of object that has it.

What components cost in return: a row cannot gain, lose or be destroyed during a walk (changes are asked of
`Commands` or made after it), rows are walked in no promised order (sort when order is a rule), and a rule that
touches another row reads through `Has`, `Get`, `Add` and `Remove` instead of fields. For a game without the signs
above, that is ceremony with nothing in return.

**The two work together.** A scene can keep its player, its interface and its level in the world and its creatures,
projectiles and effects in a store, and an object that needs both is a row [linked](#an-object-on-both-sides) to an
entity.

**Very many alike objects** are a second reason for a store: rows of structs are walked several times faster than
attachments and take a quarter of the memory. It starts to matter far later than most games reach
([below](#many-alike-objects)).

## The components

Components are plain structs. Declare every type of the game once, in an `EcsComponents`, and say what each is:

- `AddState<T>()`: simulation state. Each row's value is part of the state hash
  ([testing](../examples/testing.md)), through the component's own `HashState` (`IStateHashed`), or through a
  function for a type you cannot change.
- `AddTag<T>()`: a component without data whose presence is state (which rows are shielded).
- `Add<T>()`: not state: what only drawing needs, or what can be rebuilt.
- `Interpolate<T>(blend)`: the store keeps the value from the start of each step so drawing can blend.
- `SyncTransform<T>(write)`: the component is the place of rows linked to an entity.

<!-- code: samples/EcsEffects/Components.cs#components -->
```csharp
// What a thing is made of. A walker, a crate and a bolt are not classes: each is a row with some of these.
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

public struct Health : IStateHashed
{
    public int Value;
    public readonly void HashState(StateHasher hash) => hash.Add(Value);
}

/// <summary>A bolt in flight.</summary>
public struct Shot : IStateHashed
{
    public int Damage, Life, Fired;
    public Element Element;
    public readonly void HashState(StateHasher hash)
    {
        hash.Add(Damage);
        hash.Add(Life);
        hash.Add(Fired);
        hash.Add((int)Element);
    }
}

// Effects come and go on a live row: adding the component starts the effect, removing it ends it.
/// <summary>On fire: hurt at each tick, and at each tick the fire reaches what is near.</summary>
public struct Burning : IStateHashed
{
    public int Left, Tick;
    public readonly void HashState(StateHasher hash)
    {
        hash.Add(Left);
        hash.Add(Tick);
    }
}

/// <summary>In ice: it does not move, and the next hit shatters it for twice the damage.</summary>
public struct Frozen : IStateHashed
{
    public int Left;
    public readonly void HashState(StateHasher hash) => hash.Add(Left);
}

/// <summary>Takes the next hit whole, and is gone.</summary>
public struct Shielded;

// What a kind of thing can have: a walker carries both, a crate neither.
/// <summary>The row can be frozen.</summary>
public struct Freezes;
/// <summary>The row can take a shield.</summary>
public struct Shieldable;
/// <summary>The row is a crate: it counts as broken, not as a kill.</summary>
public struct Crate;
```

<!-- code: samples/EcsEffects/Components.cs#declared -->
```csharp
// Every component type of the game, declared once: the data is simulation state (in the state hash), the empty
// ones are tags (which rows have them is state), and a position is drawn between two steps.
public static readonly EcsComponents Components = new EcsComponents()
    .AddState<Position>().AddState<Velocity>().AddState<Health>().AddState<Shot>().AddState<Burning>().AddState<Frozen>()
    .AddTag<Shielded>().AddTag<Freezes>().AddTag<Shieldable>().AddTag<Crate>()
    .Interpolate<Position>(static (in Position from, in Position to, float blend) => new Position { Value = Vector2.Lerp(from.Value, to.Value, blend) });
```

A row with a component that was not declared stops the game at the end of the system that made it, with a message
that names the type. That is deliberate: a build published with NativeAOT cannot make rows of a component type
nobody named, and a forgotten one would otherwise fail only there. Declaring a type also names it for that build.

## The store and its systems

A scene adds its store in `Enter` and lists its systems. The store is created at that moment and destroyed when the
scene leaves; nothing in the scene's `Exit` mentions it.

<!-- code: samples/EcsEffects/ArenaScene.cs#scene -->
```csharp
public override void Enter()
{
    // The store lives with the scene. Its systems run in the order written here, every step: the ones in Systems
    // before this scene's Update, the ones in LateSystems after it.
    _ecs = this.AddEcs(Rules.Components);
    _ecs.Systems.Add(new Movement());
    _ecs.Systems.Add(new Hits());
    _ecs.Systems.Add(new Effects());
    _ecs.LateSystems.Add(new Deaths());
    _places = _ecs.Interpolated<Position>();

    Caster = new Vector2(Game.Width / 2f, Game.Height - 12);
    // Two rows of crates with a gap, a shielded guard in the gap, another one to the left, and walkers above.
    for (int i = 0; i < 6; i++) AddCrate(new Vector2(Game.Width / 2f + (i < 3 ? -42 + i * 14 : -28 + i * 14), 96));
    AddWalker(new Vector2(Game.Width / 2f, 84), Vector2.Zero, shielded: true);
    AddWalker(new Vector2(Game.Width / 2f - 60, 60), Vector2.Zero);
    for (int i = 0; i < 6; i++) AddWalker(new Vector2(30 + i * 50, 30 + i % 2 * 14), new Vector2(i % 2 == 0 ? 22 : -18, 6), shielded: i % 3 == 0);
    // Walkers keep coming: one of the scene's timers adds a row every few seconds.
    Timers.Every(4f, () => AddWalker(new Vector2(8, 30 + Game.Random.Next(80)), new Vector2(18 + Game.Random.Next(12), 5), shielded: Game.Random.Next(3) == 0));
}
```

In each step of the scene:

1. Rows linked to an entity that something else moves read its transform.
2. The components marked with `Interpolate` are kept as they are now.
3. `Systems` run, in the order they were added.
4. The scene's own `Update` runs.
5. `LateSystems` run, in the order they were added.
6. Links that lost a side are settled, and linked rows write their place to their entity.

The order of behaviours is the order of the `Add` calls: one place to read it and to change it. A system has an
`Enabled` switch (a cutscene that stops combat and keeps movement), and a scene frozen under an overlay or in a
transition does not step its store, like the rest of the scene.

A system is a class with the queries it walks and an `Update`:

<!-- code: samples/EcsEffects/Systems.cs#movement -->
```csharp
/// <summary>Everything with a place and a velocity moves, unless it is frozen: a bolt and a walker alike.</summary>
public sealed class Movement : EcsSystem
{
    private static readonly QueryDescription Movers = new QueryDescription().WithAll<Position, Velocity>().WithNone<Frozen>();
    private static readonly QueryDescription Walkers = new QueryDescription().WithAll<Position, Velocity, Health>().WithNone<Frozen>();

    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Movers))
        {
            var positions = chunk.GetSpan<Position>();
            var velocities = chunk.GetSpan<Velocity>();
            for (int i = 0; i < chunk.Count; i++) positions[i].Value += velocities[i].Value * dt;
        }
        // What has health turns at the edges; a bolt flies out and ends.
        foreach (ref var chunk in World.Query(in Walkers))
        {
            var positions = chunk.GetSpan<Position>();
            var velocities = chunk.GetSpan<Velocity>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (positions[i].Value.X < 4 || positions[i].Value.X > Game.Width - 4) velocities[i].Value.X = -velocities[i].Value.X;
                if (positions[i].Value.Y < 16 || positions[i].Value.Y > Game.Height - 30) velocities[i].Value.Y = -velocities[i].Value.Y;
            }
        }
    }
}
```

Inside a system, `World` is the store's rows (Arch's `World`), `Commands` its deferred changes, `Store`, `Scene` and
`Game` what they say. A behaviour too small for a class is a function: `ecs.Systems.Add("gravity", (store, dt) => ...)`.
A system that keeps something between steps (a counter, a timer) adds it to the state hash in its own `HashState`.

## Changing rows

Creating a row, destroying one, and adding or removing a component change the store's layout, which a walk does not
allow under itself. Two ways to do it:

- **Ask `Commands`** from inside the walk. The store applies what was asked when the system returns, and after the
  scene's `Update`; the next system sees the result. `Store.Flush()` applies it at once, between two walks of one
  system.
- **Collect and change after the walk**, inside the same system. Outside a walk every call of the library is
  allowed, and the change is visible at once, which a rule that depends on the previous change needs.

The hit system of the sample does both: bolts that end are destroyed through `Commands`, and hits are settled after
the walk, in the order the bolts were fired, because what one hit removes (a shield) decides what the next one does.

<!-- code: samples/EcsEffects/Systems.cs#hits -->
```csharp
/// <summary>A bolt hits the nearest thing with health in reach, and what the hit does depends on what that thing has.</summary>
public sealed class Hits : EcsSystem
{
    private static readonly QueryDescription Flying = new QueryDescription().WithAll<Position, Shot>();
    private static readonly QueryDescription Targets = new QueryDescription().WithAll<Position, Health>();
    private readonly List<(int Fired, Entity Target, int Damage, Element Element)> _hits = [];

    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Flying))
        {
            var positions = chunk.GetSpan<Position>();
            var shots = chunk.GetSpan<Shot>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (Nearest(positions[i].Value, out var target)) _hits.Add((shots[i].Fired, target, shots[i].Damage, shots[i].Element));
                else if (--shots[i].Life > 0) continue;
                Commands.Destroy(chunk.Entity(i));   // applied when this system returns
            }
        }
        // A hit adds and removes components, which a walk does not allow, so hits are settled after it: in the order
        // the bolts were fired, because the store walks rows in no promised order.
        _hits.Sort(static (a, b) => a.Fired.CompareTo(b.Fired));
        foreach (var hit in _hits) Hit(World, hit.Target, hit.Damage, hit.Element);
        _hits.Clear();
    }

    private bool Nearest(Vector2 at, out Entity nearest)
    {
        nearest = Entity.Null;
        float best = Rules.HitRadius * Rules.HitRadius;
        foreach (ref var chunk in World.Query(in Targets))
        {
            var positions = chunk.GetSpan<Position>();
            for (int i = 0; i < chunk.Count; i++)
            {
                float distance = Vector2.DistanceSquared(positions[i].Value, at);
                var row = chunk.Entity(i);
                if (distance < best || (distance == best && nearest != Entity.Null && row.Id < nearest.Id)) (nearest, best) = (row, distance);
            }
        }
        return nearest != Entity.Null;
    }

    // The order of these checks is the rule: a shield first, then ice, then the hit and what it carries.
    private static void Hit(World world, Entity target, int damage, Element element)
    {
        if (world.Has<Shielded>(target))
        {
            world.Remove<Shielded>(target);
            return;
        }
        if (world.Has<Frozen>(target))
        {
            world.Remove<Frozen>(target);
            world.Get<Health>(target).Value -= element == Element.Fire ? damage : damage * 2;   // fire thaws, anything else shatters
            return;
        }
        world.Get<Health>(target).Value -= damage;
        if (element == Element.Fire) Effects.Ignite(world, target);
        if (element == Element.Ice)
        {
            if (world.Has<Burning>(target)) world.Remove<Burning>(target);
            else if (world.Has<Freezes>(target)) world.Add(target, new Frozen { Left = Rules.FrozenSteps });
        }
    }
}
```

`Commands` allocates a little each time it is applied with something in it (around half a kilobyte, more with many
changes). A system that changes many rows every step keeps them in a list of its own instead, as the swarm sample
does; then a step allocates nothing.

Effects pass their time in a system of their own. It uses `Flush` so the rows whose fire ended are out of the way
before the fire spreads:

<!-- code: samples/EcsEffects/Systems.cs#effects -->
```csharp
/// <summary>Effects pass their time: ice ends, fire hurts, spreads to what is near and ends.</summary>
public sealed class Effects : EcsSystem
{
    private static readonly QueryDescription Iced = new QueryDescription().WithAll<Frozen>();
    private static readonly QueryDescription Alight = new QueryDescription().WithAll<Burning, Health, Position>();
    private static readonly QueryDescription NotAlight = new QueryDescription().WithAll<Position, Health>().WithNone<Burning>();
    private readonly List<(Entity Row, Vector2 At)> _sources = [];
    private readonly List<Entity> _reached = [];

    public static void Ignite(World world, Entity target)
    {
        if (world.Has<Burning>(target)) world.Get<Burning>(target).Left = Rules.BurnSteps;
        else world.Add(target, new Burning { Left = Rules.BurnSteps, Tick = Rules.BurnTick });
    }

    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Iced))
        {
            var frozen = chunk.GetSpan<Frozen>();
            for (int i = 0; i < chunk.Count; i++)
                if (--frozen[i].Left == 0) Commands.Remove<Frozen>(chunk.Entity(i));
        }
        foreach (ref var chunk in World.Query(in Alight))
        {
            var burning = chunk.GetSpan<Burning>();
            var health = chunk.GetSpan<Health>();
            var positions = chunk.GetSpan<Position>();
            for (int i = 0; i < chunk.Count; i++)
            {
                if (--burning[i].Tick == 0)
                {
                    health[i].Value--;
                    burning[i].Tick = Rules.BurnTick;
                    _sources.Add((chunk.Entity(i), positions[i].Value));
                }
                if (--burning[i].Left == 0) Commands.Remove<Burning>(chunk.Entity(i));
            }
        }
        // The effects that ended leave their rows now: the walk below must not see them.
        Store.Flush();

        // Fire reaches from every row that burned this step. Who is reached is decided before any row is changed.
        foreach (var (source, from) in _sources)
            foreach (ref var chunk in World.Query(in NotAlight))
            {
                var positions = chunk.GetSpan<Position>();
                for (int i = 0; i < chunk.Count; i++)
                {
                    var row = chunk.Entity(i);
                    if (row == source || _reached.Contains(row)) continue;
                    if (Vector2.DistanceSquared(positions[i].Value, from) < Rules.SpreadRadius * Rules.SpreadRadius) _reached.Add(row);
                }
            }
        foreach (var row in _reached)
        {
            if (World.Has<Frozen>(row)) World.Remove<Frozen>(row);   // fire takes the ice and does not catch
            else Ignite(World, row);
        }
        _sources.Clear();
        _reached.Clear();
    }
}
```

## Kinds of object, and questions

A kind of object is a choice of components, made where the row is created. Nothing declares "a crate"; a crate is a
row without a velocity and without the two tags a walker carries, so movement never finds it and ice and shields
pass it by. No system mentions crates to leave them out.

<!-- code: samples/EcsEffects/ArenaScene.cs#spawn -->
```csharp
// A kind of thing is a choice of components. A crate has no velocity, so nothing ever moves it; it has neither
// Freezes nor Shieldable, so ice and shields pass it by. No system mentions crates to leave them out.
public Entity AddWalker(Vector2 at, Vector2 velocity, bool shielded = false)
{
    var row = _ecs.World.Create(new Position { Value = at }, new Velocity { Value = velocity }, new Health { Value = WalkerHealth }, new Freezes(), new Shieldable());
    if (shielded) _ecs.World.Add<Shielded>(row);
    return row;
}

public Entity AddCrate(Vector2 at) => _ecs.World.Create(new Position { Value = at }, new Health { Value = CrateHealth }, new Crate());

public Entity Shoot(Vector2 from, Vector2 velocity, int damage, Element element) => _ecs.World.Create(
    new Position { Value = from }, new Velocity { Value = velocity }, new Shot { Damage = damage, Life = Rules.ShotLife, Fired = _fired++, Element = element });
```

A question about a combination is a query description, kept in a static field:

<!-- code: samples/EcsEffects/ArenaScene.cs#question -->
```csharp
private static readonly QueryDescription AlightAndOpen = new QueryDescription().WithAll<Position, Burning>().WithNone<Shielded>();

/// <summary>
/// Everything burning within <paramref name="radius"/> of <paramref name="center"/> that has no shield: walkers
/// and crates alike. The question is a description of the rows wanted, and only those rows are walked.
/// </summary>
public int BurningNear(Vector2 center, float radius, List<Entity>? found = null)
{
    int count = 0;
    foreach (ref var chunk in _ecs.World.Query(in AlightAndOpen))
    {
        var positions = chunk.GetSpan<Position>();
        for (int i = 0; i < chunk.Count; i++)
        {
            if (Vector2.DistanceSquared(positions[i].Value, center) >= radius * radius) continue;
            found?.Add(chunk.Entity(i));
            count++;
        }
    }
    return count;
}
```

To find rows near a point among very many, keep them in a `SpatialIndex2D<T>`
([Large worlds and nearby objects](large-worlds.md)) with the row as its value; a query alone walks every matching
row.

## Drawing

The scene draws its rows in `Draw`. For a component marked with `Interpolate`, `ecs.Interpolated<T>()` gives a
reader; `Blended(row, in now)` returns the value between where the row was when the step started and where it is,
at the frame's blend. It is exactly the current value for a row that is new this step and while the scene is frozen
under an overlay, as with entities of the world. Keep the reader in a field.

<!-- code: samples/EcsEffects/ArenaScene.cs#draw -->
```csharp
private static readonly QueryDescription Things = new QueryDescription().WithAll<Position, Health>();
private static readonly QueryDescription Bolts = new QueryDescription().WithAll<Position, Shot>();

public override void Draw(IRenderer r)
{
    var world = _ecs.World;
    bool flicker = (int)(Age * 12) % 2 == 0;
    foreach (ref var chunk in world.Query(in Things))
    {
        var positions = chunk.GetSpan<Position>();
        // What a chunk's rows have is the same for all of them: asked once, not per row.
        bool crate = chunk.Has<Crate>(), frozen = chunk.Has<Frozen>(), burning = chunk.Has<Burning>(), shielded = chunk.Has<Shielded>();
        for (int i = 0; i < chunk.Count; i++)
        {
            // The store kept each position from the start of the step: a row is drawn between that and where it is.
            var at = _places.Blended(chunk.Entity(i), in positions[i]).Value;
            var body = RectF.FromCenter(at, crate ? 10 : 7, crate ? 10 : 7);
            r.FillRect(body, frozen ? Ice : crate ? Wood : Walker);
            if (shielded) r.DrawRect(RectF.FromCenter(at, 11, 11), Color.White);
            if (burning) r.FillCircle(at + new Vector2(0, flicker ? -6 : -7), flicker ? 2.5f : 2, Flame);
            if (burning && !shielded && Vector2.DistanceSquared(at, Caster) < Rules.NearRadius * Rules.NearRadius) r.DrawRect(RectF.FromCenter(at, 15, 15), Flame);
        }
    }
    foreach (ref var chunk in world.Query(in Bolts))
    {
        var positions = chunk.GetSpan<Position>();
        var shots = chunk.GetSpan<Shot>();
        for (int i = 0; i < chunk.Count; i++)
            r.FillCircle(_places.Blended(chunk.Entity(i), in positions[i]).Value, 1.5f, Tint(shots[i].Element));
    }
    r.FillRect(RectF.FromCenter(Caster, 9, 5), Tint(Loaded));
    r.FillRect(RectF.FromCenter(Caster + new Vector2(0, -4), 3, 4), Tint(Loaded));

    var deaths = _ecs.LateSystems.Get<Deaths>();
    r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
    Font.Draw(r, $"Z: SHOOT {Loaded.ToString().ToUpperInvariant()}   X: SWITCH   KILLS {deaths.Kills}   CRATES {deaths.CratesBroken}   BURNING NEAR {BurningNear(Caster, Rules.NearRadius)}", new Vector2(4, 3), Color.White);
}

private static Color Tint(Element element) => element switch { Element.Fire => Flame, Element.Ice => Ice, _ => Color.Hex("f4f4f4") };

private static readonly Color Walker = Color.Hex("b13e53"), Wood = Color.Hex("a66a3a"), Ice = Color.Hex("73eff7"), Flame = Color.Hex("ffcd75");
```

## An object on both sides

Some objects need what the scene's world gives: a child that follows them, a physics body, a place in a spatial
index, a 3D mesh. Create the entity, create the row, and link them:

<!-- code: samples/EcsSwarm/SwarmScene.cs#link -->
```csharp
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
```

- `ecs.TryGetEntity(row, out entity)` and `ecs.TryGetRow(entity, out row)` find one from the other. Both answer
  false for a handle that is stale, also when a newer row or entity took its place.
- **Destroying either destroys both** when the step ends: `Objects.Destroy(entity)`, the library's
  `World.Destroy(row)`, or `Commands.Destroy(row)`. `ecs.Unlink(row)` ends the link and destroys neither.
- With `SyncTransform<T>` declared, the row's component is written to the entity's transform when each step ends
  (`EcsLink.RowMovesEntity`, the default), so the entity's children, its body and its drawing follow the row. For an
  entity that something else moves (a physics body, a tween), link with `EcsLink.EntityMovesRow` and give
  `SyncTransform` a second function: the row then reads the entity when each step starts. `EcsLink.Unsynced` copies
  nothing.
- A linked row has a `SceneLink` component, so a query can ask for the linked rows (`WithAll<Position, SceneLink>`).

Most rows are never linked. A link costs an entity, so link the few objects that need one.

## Many alike objects

A store also keeps very many alike objects cheaply. Compared with the scene's world on the same work, on a desktop:

| | Scene's world | Component store |
| --- | --- | --- |
| Moving every object, per object | 4.5 to 6.5 ns | 1.5 to 2.5 ns |
| Three questions about combinations over 100,000 objects | about 1 ms | about 0.1 ms |
| Replacing 10,000 of 100,000 objects in a step | about 1.7 ms | 0.5 to 0.9 ms |
| A game step of 375,000 objects (a swarm, shots, deaths, all drawn) | about 22 ms: over a 60 Hz update | about 6.5 ms |
| Memory per object (position, previous position, velocity) | about 240 bytes | 55 to 65 bytes |
| Drawing each object as a sprite | the same | the same |

Under about 75,000 objects simulated per step the difference is a fraction of a millisecond of the 16.7 a 60 Hz
update has. What limits a game long before either store does is what it draws and what collides. On the world, reuse
the attachments of destroyed objects (keep them in a `Stack<T>`): a new attachment per spawn is garbage in
proportion to what the game spawns.

The `EcsSwarm` sample keeps a swarm of 20,000 in a store beside a beacon and a few carriers that are entities of the
world. Its systems are functions of the scene, listed in order after the scene's `Update` has moved the beacon:

<!-- code: samples/EcsSwarm/SwarmScene.cs#store -->
```csharp
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
```

## Identity and lifetime, side by side

| | Scene's world | The store |
| --- | --- | --- |
| Identity | `EntityId`: a slot and a generation | Arch's `Entity`: an id and a version; `World.IsAlive(row)` is false once the row is gone, also after a new row takes the id |
| Creation | at once; a walk already running does not visit it | at once outside a walk; through `Commands` inside one |
| Destruction | at once for every question; readable until the step ends | at once outside a walk; nothing is readable afterwards |
| Order of a walk | attach order, stable | not promised: a removed row's place is taken by the last one |
| Stepping | by the scene, with the world's clock | by the scene, with the step its `Update` receives |
| Interpolation | by the world (`DrawPosition2D`) | by the store, for the components marked for it |
| State hash | included | included, for the components declared as state or tags |

## Platforms and limits

- The package is managed code. It publishes trimmed and with NativeAOT without warnings, and runs in browsers
  ([Trimmed and NativeAOT publishing](platforms.md#trimmed-and-nativeaot-publishing)).
- Arch's types are part of what your game uses, so the package is tied to one version of Arch, and a new one arrives
  with a new version of the package.
- A walk must not add, remove or destroy rows: use `Commands`, or change them after the walk.
- A store has no events, relations between rows or saving of its own. Systems pass work through components and
  lists; Arch offers events and relations as separate packages, which a game can add.
- A store of bodies is not a physics world: an object that collides is linked to an entity with a body.
- Inside a namespace that starts with `MicroEngine`, `World` names an engine namespace, not Arch's type; a game in
  its own namespace is not affected.
- The figures on this page are for one desktop processor and scale with it; the proportions are what carries over.

## Another library, or your own

Nothing in the engine knows the package: a store is one of a scene's systems
([Scene systems](../manual/scenes.md#scene-systems)). To use another library, or a store of your own, write a
`SceneSystem` that creates it in `Start`, steps it in `Update` and `LateUpdate`, keeps what drawing needs in
`BeginDraw`, adds its state in `HashState` and releases it in `Stop`. That is all the package's `EcsStore` is.
