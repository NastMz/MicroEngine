# Tile maps and collision

The `MicroEngine.World` namespace holds what most 2D games build their levels from: grids of tiles, what each tile
means for movement, a function that moves a box through the grid without passing through walls, and a list type for
game objects. It is deliberately small: there are no forces, masses, slopes or rotating bodies and no level editor format.
Movement rules are game code that calls these pieces. A world of bodies with collision layers, queries and sensors is
described in [Physics worlds](#physics-worlds).

## The pieces

| Type | Role |
| --- | --- |
| `TileLayer` | a grid of integer tile ids (0 means empty); built in code or from text rows with `Parse` |
| `TileFlags` | what a tile does to movement: `Solid`, `OneWay`, `Hazard`, `Water`, `Custom1`..`Custom4` |
| `TileGrid` | a layer plus a table from id to flags, and the tile size: the collision world (`ICollisionGrid`) |
| `TileMotion` | moves a box through a grid: `Move`, `IsOnGround`, `TouchedFlags`, `IsSolidAt` |
| `TileRenderer` | draws the visible cells of a layer, asking the game which sprite each id uses |
| `Autotile` | `Mask4`: which of the four neighbours match, to pick edge and corner sprites |

## Maps as text

Writing a map as rows of characters keeps it readable in code and in reviews:

<!-- code: samples/Examples/Examples/PlatformerExample.cs#level -->
```csharp
private const int Tile = 16, Wall = 1, Ledge = 2;

// '#' solid, '=' one-way platform (land on it from above, jump through it from below), 'c' a coin.
private static readonly string[] Rows =
[
    "####################",
    "#                  #",
    "#  c            c  #",
    "#  ====      ====  #",
    "#        c         #",
    "#       ====       #",
    "#                  #",
    "#   ===         c  #",
    "#             ##   #",
    "#     ##      ##   #",
    "####################",
];

private readonly TileGrid _grid = new(
    TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Wall, ['='] = Ledge }),
    Tile,
    new Dictionary<int, TileFlags> { [Wall] = TileFlags.Solid, [Ledge] = TileFlags.OneWay });
```

- `TileLayer.Parse(rows, legend)` maps each character through the legend; characters not in it become 0. Rows may have
  different lengths; the layer is as wide as the longest.
- `TileLayer.Find(rows, 'c')` lists the cells holding a marker, for placing objects (the coins here, the gems and
  slimes in the [tutorial](../tutorial/06-garden.md)).
- `new TileGrid(layer, tileSize, flags)` gives ids their meaning. Cells outside the layer use `OutsideFlags`, `Solid` by
  default, so a level is closed; set `OutsideRule` to open an edge (a pit at the bottom of a platformer).
- Several layers are normal: a background layer only drawn, a collision layer in the grid, a foreground layer drawn
  after the characters. The grid uses one layer; draw as many as you like.

## Moving through the grid

`TileMotion.Move(box, delta, grid, solids = null, ignoreOneWay = false)` returns where the box ends up and what it hit.
The result type is `TileMoveResult`, distinct from the `MoveResult` of a physics world; `TileMotion` is a stateless
convenience over the same swept test as the tile backend below.

| `TileMoveResult` member | Meaning |
| --- | --- |
| `Bounds`, `Position` | the box after the move |
| `HitLeft`, `HitRight`, `HitWall` | stopped horizontally |
| `HitFloor`, `HitCeiling` | stopped vertically |
| `Touched` | the union of the flags of every tile the box overlaps afterwards (`Hazard`, `Water`, custom flags) |

How it works, and why it matters:

- **X first, then Y.** A box moving diagonally into a wall stops on one axis and keeps sliding on the other.
- **A swept test in sub-steps of at most half a tile.** Each sub-step stops the box flush against the first solid
  tile or rectangle in its way, including one thinner than the step, so a fast box never passes through an extra
  rectangle thinner than a sub-step.
- **Solid** tiles block from every side. **One-way** tiles block only a box moving down onto their top edge from above:
  jump up through them, land on them, and pass `ignoreOneWay: true` to drop through.
- **`solids`**: extra rectangles that block like solid tiles (moving platforms, doors, push blocks). A rectangle the
  box already overlaps does not block it, so a box is never stuck inside one. One exception, a rectangle that rose
  into a box moving down: when its top edge is
  inside the box's lower half, the box is first lifted onto it (if nothing solid is there), so a platform moved up into
  a standing player carries him instead of swallowing him, and `HitFloor` and `IsOnGround` are true.
- `IsOnGround(box, grid, solids)` is true when one more pixel down would collide: standing on a floor, a one-way
  platform or a solid rectangle.

A side-view player with gravity, jumps, one-way platforms and a moving platform:

<!-- code: samples/Examples/Examples/PlatformerExample.cs#update -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

    // The lift moves first and carries the player when he stands on it.
    var lift = _lift with { X = 176 + MathF.Sin(Age) * 48 };
    bool riding = _player.Bottom == _lift.Top && _player.Right > _lift.Left && _player.Left < _lift.Right;
    if (riding) _player = TileMotion.Move(_player, new Vector2(lift.X - _lift.X, 0), _grid).Bounds;
    _lift = lift;
    IReadOnlyList<RectF> solids = [_lift];

    bool grounded = TileMotion.IsOnGround(_player, _grid, solids);
    _velocity.X = Input.Axis("left", "right") * RunSpeed;
    _velocity.Y = MathF.Min(_velocity.Y + Gravity * dt, MaxFall);
    if (grounded && Input.Pressed("confirm"))
    {
        if (Input.Down("down")) _dropTime = 0.2f;          // down + jump: drop through a one-way platform
        else _velocity.Y = -JumpSpeed;
    }
    _dropTime = MathF.Max(0, _dropTime - dt);

    // One call resolves walls, floors, ceilings, one-way platforms and the lift.
    var move = TileMotion.Move(_player, _velocity * dt, _grid, solids, ignoreOneWay: _dropTime > 0);
    _player = move.Bounds;
    if (move.HitFloor || move.HitCeiling) _velocity.Y = 0;

    // Objects against the player: the game compares rectangles.
    foreach (var coin in _coins)
        if (coin.Bounds.Intersects(_player)) { coin.Collected = true; _collected++; }
    _coins.RemoveAll(static coin => coin.Collected);      // removed after the walk, never during it
    foreach (var coin in _coins) coin.Update(dt);
}
```

Everything the engine does not decide is visible here as game code: gravity and jump speed, when a jump is allowed
(`grounded`), how dropping through works (a short timer), and how a moving platform carries the player (move him by the
platform's own movement first). Tune those numbers per game; the collision stays exact.

A top-down game uses the same call without gravity: see the gardener and the slimes of the
[tutorial](../tutorial/07-collisions.md), which turn around on `HitWall`.

## Objects against objects

`TileMotion` handles boxes against the world. Between objects (a player and a coin, a bullet and an enemy), compare
rectangles: `a.Intersects(b)` is true when they overlap (sharing an edge does not count). `RectF.FromCenter`,
`Offset`, `Inflate` and `Intersection` build and adjust the rectangles.

There is no spatial index: checking every pair is fast for the few hundred objects a scene of a pixel-art game holds.
A game with thousands of objects would bucket them by tile itself.

## Entities

A scene keeps its objects in `Scene.Objects`, an `IWorld` with entity ids, transforms, attachments, interpolation for
drawing and a clock ([The scene world](scenes.md#the-scene-world)), or in plain classes of its own, as the platformer example does
with its coins:

<!-- code: samples/Examples/Examples/PlatformerExample.cs#coin -->
```csharp
/// <summary>A game object as a plain class: a position, a box around it, its own Update and Draw.</summary>
public sealed class Coin
{
    private float _time;

    public Vector2 Position { get; init; }
    public RectF Bounds => new(Position.X, Position.Y, 8, 8);
    public bool Collected { get; set; }

    public void Update(float dt) => _time += dt;

    public void Draw(IRenderer r) =>
        r.FillCircle(Bounds.Center + new Vector2(0, MathF.Sin(_time * 4)), 4, Color.Hex("ffec27"));
}
```

The tutorial's player and slimes are plain classes too. MicroEngine has no entity-component system.

## Drawing tiles

`TileRenderer.Draw(r, layer, tileSize, camera, spriteFor)` draws the cells the camera sees (with a one-tile margin),
calling `spriteFor(id, x, y)` for each non-empty cell; return `null` to skip one. Call it between `PushCamera` and
`PopCamera`. `Autotile.Mask4(layer, x, y, same)` returns a 4-bit mask of matching neighbours (1 up, 2 right, 4 down,
8 left), the usual way to choose a wall's edge or corner sprite.

## Performance

`TileMotion.Move` checks only the cells the box sweeps at each sub-step: a handful of lookups per moving object.
`TileRenderer` touches only visible cells, so a large map costs what the screen shows. Both are fine for hundreds of
moving objects per step. A `TilePhysicsWorld2D` files its box shapes in a grid of 64-unit cells, so a move, a query or
a sensor looks only at the shapes near it; the results are the same as looking at every shape.

## Limits

- Axis-aligned boxes only: no slopes, circles or rotated shapes. `TileMotion` moves one box against rectangles that
  stand still during the call; a physics world's moving solids push and carry other bodies.
- One tile size per grid; no isometric or hexagonal grids.
- No importer for map editors (Tiled, LDtk): read their files yourself into a `TileLayer` if you use one.

## Common mistakes

- **Colliding with the sprite's rectangle.** Use a smaller box at the feet (top-down) or around the body (side view);
  draw the sprite from the box's position.
- **Checking `IsOnGround` after moving.** Check it before deciding to jump, then move.
- **Resetting vertical speed only on `HitFloor`.** Reset it on `HitCeiling` too, or the player sticks to ceilings.

## Physics worlds

`IPhysicsWorld2D` (namespace
`MicroEngine.Physics`) is a world of bodies owned by entities of the [scene world](scenes.md#the-scene-world). Every
physics backend offers the same core; anything else is a capability the backend declares, and asking for one it does
not declare throws `PhysicsCapabilityException` instead of being approximated. The engine's backend is
`TilePhysicsWorld2D` (namespace `MicroEngine.Physics.Tiles`), built on the same swept test as `TileMotion`:

```csharp
var physics = new TilePhysicsWorld2D();
var level = physics.CreateBody(levelEntity, BodyDescription.Static());
physics.AddTiles(level, grid);                                   // the tile grid as static geometry

var player = physics.CreateBody(playerEntity, BodyDescription.Kinematic(new Vector2(40, 120)));
physics.AddShape(player, ShapeDescription.Box(new Vector2(10, 14)));

// Each step: collide-and-slide, then read what the box touched.
var move = physics.Move(player, velocity * dt);
if (move.OnGround) velocity.Y = 0;
```

The core every backend offers:

| Part | What it does |
| --- | --- |
| Bodies | `CreateBody(owner, description)`: `Static` (level geometry) or `Kinematic` (moved only by your code). Ids are generation-checked: a destroyed body's id never reaches the body that reuses its slot. `SetPosition` teleports. |
| Shapes | `AddShape(body, ShapeDescription.Box(size, offset))`: an axis-aligned box centred on the body's origin plus its offset. |
| Layers | `Category` (the layers a shape is in) and `Mask` (the layers it collides with), 64 bits; two shapes collide only when each one's mask holds the other's category. `QueryFilter(category, mask)` selects what a move or a query sees. |
| `Move(body, delta, filter)` | Moves a kinematic body's box, X then Y, and stops it against what is in its way however fast it moves. `MoveResult` has the position, the distance actually moved, `OnGround`, `GroundNormal`, `HitWall`, `HitCeiling` and every contact with its shape, owner and normal. A shape the box already overlaps does not block it. |
| `RayCast(from, to, filter, out hit)` | The closest shape the segment enters: shape, owner, point, normal and fraction. |
| `Overlap(area, filter, results)` | The shapes that truly overlap a rectangle (sharing an edge does not count). |
| Sensors | `ShapeDescription.Sensor(size)`: never blocks; after each `Step`, `SensorEvents` lists the shapes of moving bodies that entered or left it. Queries leave sensors out unless the filter sets `IncludeSensors`. |
| `Step(dt)` | Computes the step's events; they stay readable until the next step. No game code runs during it. |

What a backend may declare (`physics.Capabilities`):

| Capability | Meaning | Tile backend | Box2D backend |
| --- | --- | --- | --- |
| `TileGeometry` | `AddTiles(body, grid)`: solid cells block, one-way cells are platforms, changing a tile changes collisions at once | yes | no: build the level from boxes |
| `OneWayPlatforms` | `ShapeDescription.OneWay`: stops only bodies coming down onto its top; `QueryFilter.PassOneWay` drops through | yes | yes, for moved and dynamic bodies |
| `MovingSolids` | `BodyDescription.PushesBodies`: a kinematic body that moves the whole way, pushes kinematic bodies out of its way and carries those standing on it | yes | no |
| `DynamicBodies` | `BodyKind.Dynamic`: bodies moved by the solver (gravity, `SetVelocity`, contacts); `ContactEvents` reports when they begin and stop touching | no | yes |
| `Rotation` | `BodyDescription.Rotation`, and bodies the solver turns (`RotationOf`) | no | yes |
| `Circles`, `Capsules`, `Polygons` | `ShapeDescription.Circle`, `Capsule` (also as the shape `Move` moves) and `Polygon` (convex) | no | yes (polygons of up to 8 vertices) |
| `ContinuousCollision` | `BodyDescription.Bullet`: a fast dynamic body never passes through what it hits | no | yes |
| `FrictionRestitution`, `MassProperties` | `ShapeDescription.Friction`, `Restitution`, `Density` | no | yes |
| `Joints` | bodies joined together | no | no |

Undeclared capabilities are refused with `PhysicsCapabilityException`.

The tile backend rests boxes exactly on surfaces (`Skin` is 0), uses the engine's Y-down space (`Up` is (0, -1)) and
gives identical results on every platform. A backend with dynamic bodies may leave a small gap, at most its `Skin`.

### The Box2D backend

`Box2DPhysicsWorld2D` (namespace `MicroEngine.Physics.Box2D`) runs Box2D 3.2 behind the same `IPhysicsWorld2D`, for
games that need what the tile backend refuses: dynamic bodies, rotation, circles, capsules and polygons, continuous
collision, friction and mass. It is an optional package; the tile backend needs none:

```xml
<PackageReference Include="MicroEngine.Physics.Box2D" Version="0.5.0-alpha.2" />
```

Choose the backend where the world is made; code that uses only the core then runs on either. Keep the tile backend
for characters moving through tile levels: it rests exactly on surfaces, builds levels from tile grids (`AddTiles`),
has moving platforms that push and carry (`MovingSolids`) and moves kinematic boxes several times faster. Choose Box2D
when bodies must fall, stack, roll and push each other.

<!-- code: samples/PhysicsSubstitution/Program.cs#backend -->
```csharp
static IPhysicsWorld2D Make(string backend) => backend switch
{
    "tiles" => new TilePhysicsWorld2D(),
    // Box2D in the engine's pixel space: 16 pixels (a tile) to the metre, y down.
    "box2d" => new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true }),
    _ => throw new ArgumentException($"Unknown backend '{backend}': tiles or box2d."),
};
```

A crate is a dynamic body with a box; the solver moves and turns it on each `Step`:

<!-- code: samples/PhysicsSubstitution/Crates.cs#crate -->
```csharp
private static BodyId Crate(IPhysicsWorld2D world, Vector2 position, float rotation)
{
    var body = world.CreateBody(Owner, BodyDescription.Dynamic(position) with { Rotation = rotation });
    world.AddShape(body, ShapeDescription.Box(new Vector2(16, 16)));
    return body;
}
```

The package carries Box2D's native library for every runtime it supports, and the browser build links it into the
page:

| Platform | Support |
| --- | --- |
| Windows x64, Linux x64, macOS (Apple silicon and Intel), browsers | supported |
| Android (arm64, x64), iOS (arm64) | experimental: the library is included for these devices |

What differs from the tile backend, within what the core allows:

- `Move` stops a box a little short of the surface in its way: it rests up to `Skin` away (1.5 cm in metres, about half
  a pixel at 32 pixels a metre), never inside. A capsule moved by `Move` may sink into the floor by up to the same amount.
- `ContactEvents` only has pairs with a dynamic body: a kinematic character's contacts are in its `MoveResult`.
- Events of the same step can come in another order than on the tile backend, and a contact can begin one step
  earlier or later than the tile backend would report the touch.
- Angles go through Box2D's own sine and cosine: an angle reads back within about 0.002 radians of the one set.
- Identical results on every platform for the same build; never identical to the tile backend or another Box2D version.

The same platformer, written against the core only, runs on both backends; on Box2D (16 pixels a metre) it rests about
0.16 pixels higher, and its sensors fire in the same step. Code that creates a dynamic body on the tile backend gets
`PhysicsCapabilityException` before anything changes, so a game can check `physics.Capabilities` first or catch it.
- A world owns native memory: dispose it. Box2D keeps at most 128 worlds alive in a process, created and disposed from
  one thread at a time. A world you forget to dispose is released later, on the thread that next creates or disposes a
  world, after the garbage collector finds it; when all 128 are in use, creating another first collects and retries,
  then throws `InvalidOperationException`.

To step worlds with the game, add the physics module and register each world with the scene world whose entities own
its bodies:

```csharp
public override void Configure(EngineBuilder builder) => builder.Add(new PhysicsModule());

// In the scene that owns the world:
physicsSystem.Add(physics, world);       // physicsSystem: the PhysicsSystem service the module registers
```

The module steps the world in the frame's `Physics` phase, after the scene's update, only in the steps its scene world
took and by that world's scaled time (so pausing, slow motion and hitstop apply), then writes every moving body's
position to its owner's `Transform`, which the scene world interpolates for drawing. Owners of moving bodies are root
entities. `PhysicsSync.Teleport(physics, world, body, position)` moves a body and its entity together and draws it there
at once.

## Related

- [Tutorial step 6: tile map and camera](../tutorial/06-garden.md), [step 7: collisions](../tutorial/07-collisions.md)
- [Graphics](rendering.md#tile-maps): drawing layers
- API: [`TileMotion`](../api/MicroEngine.World.TileMotion.md), [`TileGrid`](../api/MicroEngine.World.TileGrid.md),
  [`IWorld`](../api/MicroEngine.Scenes.IWorld.md),
  [`IPhysicsWorld2D`](../api/MicroEngine.Physics.IPhysicsWorld2D.md),
  [`TilePhysicsWorld2D`](../api/MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md); `Box2DPhysicsWorld2D` is not in the API
  reference until its package is published
