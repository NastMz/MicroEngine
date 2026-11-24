# Physics

A physics world holds bodies, moves them and tells you what they touch. Every scene can have one; the engine steps
it, keeps it in the state hash and disposes it. This page covers choosing a backend, characters, bodies and forces,
joints, queries, contacts and tile levels.

## A scene's physics world

Read `Physics` in a scene and it has a world:

```csharp
public override void Enter()
{
    var world = Physics.World;                                   // IPhysicsWorld2D
    var level = world.CreateBody(default, BodyDescription.Static());
    world.AddTiles(level, grid, grid.Layer.Width, grid.Layer.Height);
}
```

`Scene.Physics` is a scene system (`ScenePhysics`), added the first time it is read:

- **Stepping.** The world steps after the scene's `Update`, by the step `Update` received: slow motion slows physics,
  and a frozen world or a scene covered by a blocking overlay stands still. Move kinematic bodies, step characters
  and apply forces in `Update`; the events of the step are ready for the next `Update`.
- **Transforms.** After each step, the position (and rotation) of every moving body is written to the `Transform` of
  the entity that owns it, which the scene world interpolates for drawing. Owners of moving bodies are root entities.
  A scene that draws from the physics world itself sets `Physics.SyncTransforms = false`.
- **State hash.** The world is part of the scene's state hash, with every controller you hand to `Physics.Track`.
- **Lifetime.** The world is disposed when the scene leaves the stack.

A world that belongs to no scene is stepped by hand (`world.Step(dt)`), or by `PhysicsModule` in the frame's physics
phase.

## Choosing a backend

Two backends implement the same `IPhysicsWorld2D`:

| Backend | Package | For |
| --- | --- | --- |
| Tiles (`TilePhysicsWorld2D`), the default | none: it is in `MicroEngine` | characters and platforms in tile levels: exact resting, tile grids read live, solids that push and carry |
| Box2D (`Box2DPhysicsWorld2D`) | `MicroEngine.Physics.Box2D` (optional) | bodies that fall, stack, roll and are joined: dynamic bodies, rotation, round and polygon shapes, joints |

The backend is named in one place, `Game.PhysicsBackend`, and every scene's `Physics` uses it:

<!-- code: samples/PhysicsPlayground/PlaygroundGame.cs#backend -->
```csharp
// Every scene's physics world is Box2D, in the engine's pixel space: 16 pixels to the metre, y down.
PhysicsBackend = () => new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true });
```

```xml
<PackageReference Include="MicroEngine.Physics.Box2D" Version="0.6.0-alpha.1" />
```

A scene that needs its own world adds the system itself before reading `Physics`:
`Systems.Add(new ScenePhysics(world))`.

Everything beyond the common core is a capability a backend declares (`world.Capabilities`). Asking for one it does
not declare throws `PhysicsCapabilityException` before anything changes, and the message names the backend that has
it; nothing is approximated silently.

| Capability | Meaning | Tiles | Box2D |
| --- | --- | --- | --- |
| `TileGeometry` | `AddTiles`: a tile grid as static geometry | yes | yes |
| `OneWayPlatforms` | `ShapeDescription.OneWay`, `QueryFilter.PassOneWay` | yes | yes |
| `DynamicBodies` | bodies moved by the solver: gravity, velocities, forces, impulses, contact events | no | yes |
| `Rotation` | rotated bodies, angular velocity, torque | no | yes |
| `Joints` | `CreateJoint`: revolute, distance, prismatic, weld, wheel and target joints | no | yes |
| `Capsules` | `ShapeDescription.Capsule`, also as a character's shape | no | yes |
| `Circles` | `ShapeDescription.Circle` | no | yes |
| `Polygons` | `ShapeDescription.Polygon`: convex, up to 8 vertices | no | yes |
| `ContinuousCollision` | `BodyDescription.Bullet`: a fast body never passes through what it hits | no | yes |
| `FrictionRestitution` | `ShapeDescription.Friction`, `Restitution` | no | yes |
| `MassProperties` | `ShapeDescription.Density`, `MassOf`, `CenterOfMassOf`, `InertiaOf` | no | yes |
| `MovingSolids` | `BodyDescription.PushesBodies`: a kinematic body that pushes and carries kinematic bodies | yes | no |
| `ShapeInspection` | `ShapesOf`, `DescriptionOf`, `BoundsOf`: what a debug view needs | yes | yes |
| `ShapeQueries` | `RayCastAll`, `ShapeCast`, `Overlap` with a box, a circle or a capsule, `OverlapPoint`, `ClosestPoint` | yes | yes |
| `ContactDetail` | `ContactImpacts` (speed and impulse of each hit), `ContactsOf` (the points a body touches at) | no | yes |
| `FilterPredicates` | `QueryFilter.Where`: a predicate that leaves single shapes out of a move or a query | yes | yes |
| `TileSlopes` | slope tiles in tile geometry: `TileFlags.SlopeRight`, `TileFlags.SlopeLeft` | no | yes |

## The common core

| Part | What it does |
| --- | --- |
| Bodies | `CreateBody(owner, description)`: `Static` (level geometry) or `Kinematic` (moved only by your code). Ids are generation-checked: a destroyed body's id never reaches the body that reuses its slot. `SetPosition` teleports. |
| Shapes | `AddShape(body, ShapeDescription.Box(size, offset))`: an axis-aligned box centred on the body's origin plus its offset. |
| Layers | `Category` (the layers a shape is in) and `Mask` (the layers it collides with), 64 bits; two shapes collide only when each one's mask holds the other's category. `QueryFilter(category, mask)` selects what a move or a query sees. |
| `Move(body, delta, filter)` | Moves a kinematic body's box, X then Y, and stops it against what is in its way however fast it moves. `MoveResult` has the position, the distance moved, `OnGround`, `GroundNormal`, `Ground`, `HitWall`, `HitCeiling` and every contact with its shape, owner and normal. A shape the box already overlaps does not block it. |
| Sensors | `ShapeDescription.Sensor(size)`: never blocks; after each step, `SensorEvents` lists the shapes of moving bodies that entered or left it. |
| `Step(dt)` | Advances the world and computes the step's events; they stay readable until the next step. No game code runs during it. |

## A character

`CharacterController2D` is a side-view character: a kinematic body and the rules that move it. It works on both
backends, because it only uses `Move`.

<!-- code: samples/Examples/Examples/PlatformerExample.cs#state -->
```csharp
private static readonly Vector2 Size = new(10, 14);
private CharacterController2D _hero = null!;
private BodyId _lift;                            // a moving platform: a kinematic body the game moves
private readonly List<Coin> _coins = [];
private int _collected;

public RectF Player => new(_hero.Position.X - Size.X / 2, _hero.Position.Y - Size.Y / 2, Size.X, Size.Y);
public int Collected => _collected;
public int CoinsLeft => _coins.Count;

public override void Enter()
{
    // The scene's physics world: the scene steps it after Update, hashes it and disposes it.
    var world = Physics.World;
    var level = world.CreateBody(default, BodyDescription.Static());
    world.AddTiles(level, _grid, _grid.Layer.Width, _grid.Layer.Height);
    _lift = world.CreateBody(default, BodyDescription.Kinematic(new Vector2(192, 99)));
    world.AddShape(_lift, ShapeDescription.Box(new Vector2(32, 6)));
    // A character: a kinematic box and the rules that move it. JumpReleaseGravityScale 1 gives every jump the
    // same height; the default cuts a jump short when the button is released.
    _hero = Physics.Track(CharacterController2D.Create(world, default, new Vector2(45, 127), Size,
        new CharacterMotion { Gravity = 700, MaxFallSpeed = 400, MoveSpeed = 90, JumpSpeed = 280, JumpReleaseGravityScale = 1 }));
    foreach (var (x, y) in TileLayer.Find(Rows, 'c'))
        _coins.Add(new Coin { Position = new Vector2(x * Tile + 4, y * Tile + 4) });
}
```

<!-- code: samples/Examples/Examples/PlatformerExample.cs#update -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

    // The lift moves first; the character standing on it is carried along in its own step.
    var world = Physics.World;
    world.SetPosition(_lift, new Vector2(192 + MathF.Sin(Age) * 48, 99));

    // The controller takes intent, not input: walls, floors, ceilings, one-way platforms and the lift are its job.
    _hero.Step(dt, new CharacterInput(
        Input.Axis("left", "right"),
        JumpPressed: Input.Pressed("confirm"),
        JumpHeld: Input.Down("confirm"),
        Drop: Input.Down("down") && Input.Pressed("confirm")));      // down + jump: drop through a one-way platform

    // Objects against the player: the game compares rectangles.
    var player = Player;
    foreach (var coin in _coins)
        if (coin.Bounds.Intersects(player)) { coin.Collected = true; _collected++; }
    _coins.RemoveAll(static coin => coin.Collected);      // removed after the walk, never during it
    foreach (var coin in _coins) coin.Update(dt);
}
```

The controller takes intent (`CharacterInput`: a move axis, jump pressed and held, drop), never input devices, so a
player and an AI drive it the same way. What it does with it:

| Rule | Parameter of `CharacterMotion` |
| --- | --- |
| Gravity with a fall clamp | `Gravity`, `MaxFallSpeed`, `FallGravityScale` |
| Running, with optional acceleration | `MoveSpeed`, `Acceleration`, `Deceleration`, `AirControl` |
| Jump; a tap is a short hop | `JumpSpeed`, `JumpReleaseGravityScale` (1: every jump the same height) |
| Coyote time: a jump just after walking off a ledge | `CoyoteTime` |
| Jump buffering: a jump pressed just before landing | `JumpBuffer` |
| Drop through a one-way platform | `CharacterInput.Drop`, `DropThroughTime` |
| Air jumps | `AirJumps` (1: a double jump) |
| Wall slide and wall jump | `WallSlideSpeed`, `WallJumpSpeed`, `WallJumpLockTime` (off by default) |
| Staying on ground that falls away (slopes, steps) | `GroundSnap` |
| A gravity for the top of a jump: it floats while jump is held | `ApexGravityScale`, `ApexSpeed` |
| A skid: reversing at speed brakes before it turns | `SkidDeceleration`, `SkidSpeed` |
| Corner correction: a head that would clip the corner of a ceiling is moved past it | `CornerCorrection` |
| A ceiling that sends the character down at once | `CeilingReboundSpeed` |
| A run-up that jumps higher | `RunJumpBonus` |

What a game reads back, without deriving it again: `IsGrounded`, `Ground`, `IsOnWall`, `WallNormal`, `IsJumping`,
`IsSkidding`, `AirTime`, `LandingSpeed`, `Velocity`, `LastMove`, and `Events` for the step (`Landed`, `LeftGround`,
`Jumped`, `HitCeiling`, `HitWall`, `DroppedThrough`, `Skidded`, `CornerCorrected`). `Launch(velocity)` throws the character (a spring, a knock-back); `Teleport` places it.

A character standing on a body the game moved (a platform moved with `SetPosition` or `Move`) is carried by it. A
character at rest is not moved at all. On Box2D a capsule character walks up and down slopes; give it a `GroundSnap`
of a few units so it stays on the slope going down. The tile backend has no slopes.

`TopDownController2D` is the top-down counterpart: no gravity, a direction as intent, sliding along walls, optional
acceleration:

```csharp
var hero = TopDownController2D.Create(Physics.World, entity, start, new Vector2(12, 12));
hero.Step(dt, new Vector2(Input.Axis("left", "right"), Input.Axis("up", "down")));
```

Both controllers are deterministic; `Physics.Track(controller)` adds one to the scene's state hash.

### A feel of its own

How a character feels is its numbers. A platform game with four gravities (rising, rising with jump released, the
top of the jump, falling), a skid, corner correction and a run-up bonus is one `CharacterMotion`:

<!-- code: samples/PhysicsPlayground/ClassicScene.cs#motion -->
```csharp
// Every number of the character's feel, in pixels and seconds.
private static readonly CharacterMotion Walk = new()
{
    MoveSpeed = 92, Acceleration = 520, Deceleration = 620, AirControl = 0.8f,
    SkidDeceleration = 1500, SkidSpeed = 30,             // reversing above 30 px/s brakes hard before it turns
    JumpSpeed = 302, RunJumpBonus = 26,                  // a run-up adds up to 26 px/s to the jump
    Gravity = 640,                                       // while it rises with jump held
    JumpReleaseGravityScale = 1900f / 640,               // while it rises with jump released: a tap is a hop
    ApexGravityScale = 380f / 640, ApexSpeed = 48,       // under 48 px/s near the top: the jump floats
    FallGravityScale = 1050f / 640, MaxFallSpeed = 270,  // falling
    CoyoteTime = 0.1f, JumpBuffer = 0.12f,
    CornerCorrection = 4,                                // up to 4 px sideways past a corner
    CeilingReboundSpeed = 20,                            // a bumped head comes down at once
};
// Running is another record: the game swaps them, the controller keeps its state.
private static readonly CharacterMotion Run = Walk with { MoveSpeed = 152, Acceleration = 600 };
```

`Motion` can be replaced at any time (running, water, a power-up): the controller keeps its state. Each step the
game sends intent and reads what happened, for its own effects:

<!-- code: samples/PhysicsPlayground/ClassicScene.cs#step -->
```csharp
Hero.Motion = Input.Down("cancel") ? Run : Walk;
Hero.Step(dt, new CharacterInput(Input.Axis("left", "right"), Input.Pressed("confirm"), Input.Down("confirm")));

// What the step did, for the game's effects.
var feet = Hero.Position + new Vector2(0, Size.Y / 2);
if (Hero.IsSkidding) { SkidSteps++; _dust.Add((feet, 0.2f)); }
if ((Hero.Events & CharacterEvents.CornerCorrected) != 0) Corrections++;
if ((Hero.Events & CharacterEvents.Landed) != 0)
{
    HardestLanding = MathF.Max(HardestLanding, Hero.LandingSpeed);
    if (Hero.AirTime > 0.3f) _dust.Add((feet, 0.35f));
}
if ((Hero.Events & CharacterEvents.HitCeiling) != 0)
    foreach (var contact in Hero.LastMove.Contacts)
        if (contact.Shape == _hidden) Revealed = true;                    // the head found the block
```

Solids that depend on how the character meets them are a rule of the character, `Solid`: it is asked about each
shape near the character before each of its moves, and a shape it returns false for is passed through. A hidden block
that only a head coming from below finds is an ordinary shape and one rule:

<!-- code: samples/PhysicsPlayground/ClassicScene.cs#hidden -->
```csharp
// A hidden block is a shape like any other. What makes it hidden is the character's rule for it: it is solid
// only for a head that comes up from below, until it has been found.
_hidden = world.AddShape(_room, ShapeDescription.Box(new Vector2(16, 16), new Vector2(210, 118)));
Hero = Physics.Track(CharacterController2D.Create(world, default, new Vector2(40, 161), Size, Walk));
Hero.Solid = (hero, shape) =>
{
    if (shape != _hidden || Revealed) return true;
    float head = hero.Position.Y - Size.Y / 2, under = world.BoundsOf(_hidden).Bottom;
    return hero.Velocity.Y < 0 && head >= under - 1;
};
```

What the parameters do not cover, the game writes over the controller instead of replacing it:

- **A sideways model of its own** (momentum kept in the air, ice, conveyor belts): set `Acceleration` and
  `Deceleration` to 0, so the controller never changes the sideways speed, and write `Velocity` before each step.
  Walls still stop it.
- **Another gravity for a while** (gliding, water, a low-gravity room): replace `Motion`.
- **Solids by state** (a character that passes enemies while it flashes, blocks of a colour that is switched off):
  change the mask of `Filter`; layers are cheaper than a rule.
- **A world for a game that has only a tile grid**: `Physics.World.AddTiles(body, grid, columns, rows)` on the
  scene's world is the whole setup, and moving platforms are kinematic bodies the character is carried by.

A rule of `Solid` sees one shape at a time, so a conditional solid is a shape of its own, not a cell of a tile grid.
It may read the world (`BoundsOf`, `PositionOf`) but must not query or change it.

## Bodies and forces

A dynamic body is moved by the solver: gravity pulls it, it collides, rests, stacks and turns. Dynamic bodies need
the Box2D backend.

<!-- code: samples/PhysicsSubstitution/Crates.cs#crate -->
```csharp
private static BodyId Crate(IPhysicsWorld2D world, Vector2 position, float rotation)
{
    var body = world.CreateBody(Owner, BodyDescription.Dynamic(position) with { Rotation = rotation });
    world.AddShape(body, ShapeDescription.Box(new Vector2(16, 16)));
    return body;
}
```

| To | Call |
| --- | --- |
| Push for as long as something acts (thrust, wind) | `ApplyForce(body, force)` every step; with a point, off-centre forces also turn the body |
| Change the velocity at once (a jump, a hit, an explosion) | `ApplyImpulse(body, impulse)`: the velocity changes by impulse / mass |
| Turn | `ApplyTorque`, `ApplyAngularImpulse`, `SetAngularVelocity`, `AngularVelocityOf` |
| Set directly | `SetVelocity`, `SetPosition`, `SetRotation` (teleports: no collision on the way) |
| Tune one body | `BodyDynamics`: `LinearDamping`, `AngularDamping`, `GravityScale`, `FixedRotation`, `AllowSleep`, at creation (`BodyDescription.Dynamics`) or later (`SetDynamics`) |
| Sleep | a resting body falls asleep and costs nothing; `IsAwake`, `SetAwake`. Forces and impulses wake it unless `wake: false` |
| Read mass | `MassOf`, `CenterOfMassOf`, `InertiaOf` (mass comes from the shapes' `Density`) |

Forces are mass × world units per second squared, impulses mass × world units per second, in the units of the world
(`Box2DWorldOptions.UnitsPerMeter`), so a pixel-space game writes pixels throughout:

<!-- code: samples/PhysicsPlayground/PlaygroundScene.cs#drive -->
```csharp
if (Input.Pressed("pause")) { Game.Quit(); return; }
if (Input.Pressed("up")) { Game.Scenes.Push(new ClassicScene()); return; }   // the character room
if (Input.Pressed("down")) { Game.Scenes.Push(new TileScene()); return; }    // the tile level
var world = Physics.World;

// Throttle: the wheels' motors aim for a speed; the joints do the rest.
float throttle = Input.Axis("left", "right");
foreach (var wheel in _wheels) world.SetJointMotor(wheel, true, throttle * MotorSpeed, 9000);

// A hop: an impulse through the centre of mass changes the velocity at once.
if (Input.Pressed("confirm")) world.ApplyImpulse(Chassis, world.Up * world.MassOf(Chassis) * 140);
```

## Joints

A joint ties two bodies together (`PhysicsCapabilities.Joints`, the Box2D backend). `JointDescription` has a factory
for each kind; anchors and axes are world points and directions as the bodies are when the joint is created.

| Joint | Factory | Use | Options |
| --- | --- | --- | --- |
| Revolute | `Revolute(a, b, anchor)` | hinges, pendulums, chains, limbs | limit (radians), motor (torque), spring |
| Distance | `Distance(a, b, anchorA, anchorB)` | rods and springs | `Length`, spring, shortest and longest length, motor |
| Rope | `Rope(a, b, anchorA, anchorB, maxLength)` | slack up to a length, taut beyond | a distance joint with a limit and no spring force |
| Prismatic | `Prismatic(a, b, anchor, axis)` | pistons, lifts, sliding doors | limit (translation), motor (force), spring |
| Weld | `Weld(a, b, anchor)` | gluing bodies; breakable structures | spring for a soft weld |
| Wheel | `Wheel(chassis, wheel, anchor, axis)` | vehicles | suspension spring, limit, motor (torque) |
| Target | `Target(body, anchor)` | dragging a body with the pointer | spring (`Hertz`, `DampingRatio`), `MaxForce`; move it with `SetJointTarget` |

A chain is a row of bodies, each pinned to the one before it:

<!-- code: samples/PhysicsPlayground/PlaygroundScene.cs#chain -->
```csharp
// A chain: each link is pinned to the one before it; the first hangs from the ceiling.
var previous = room;
for (int i = 0; i < 8; i++)
{
    var link = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(60, 13 + i * 10)));
    world.AddShape(link, ShapeDescription.Box(new Vector2(3, 10)) with { Density = 2 });
    world.CreateJoint(JointDescription.Revolute(previous, link, new Vector2(60, 8 + i * 10)));
    previous = link;
}
ChainEnd = previous;
```

A rope:

<!-- code: samples/PhysicsPlayground/PlaygroundScene.cs#rope -->
```csharp
// A rope: slack until the lantern is 40 pixels from the hook, taut beyond.
Lantern = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(130, 20)));
world.AddShape(Lantern, ShapeDescription.Circle(5));
Rope = world.CreateJoint(JointDescription.Rope(room, Lantern, new Vector2(110, 8), new Vector2(130, 20), 40));
_lines.Add(Rope);
```

A vehicle is a chassis and wheels on wheel joints; the spring is the suspension and the motor drives:

<!-- code: samples/PhysicsPlayground/PlaygroundScene.cs#cart -->
```csharp
// A cart: a chassis on two wheels, each on a wheel joint whose spring is the suspension and whose motor drives it.
Chassis = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(60, 152)));
world.AddShape(Chassis, ShapeDescription.Box(new Vector2(36, 8)));
foreach (float x in (ReadOnlySpan<float>)[46, 74])
{
    var wheel = world.CreateBody(default, BodyDescription.Dynamic(new Vector2(x, 161)));
    world.AddShape(wheel, ShapeDescription.Circle(6) with { Friction = 1.5f });
    _wheels.Add(world.CreateJoint(JointDescription.Wheel(Chassis, wheel, new Vector2(x, 161), world.Up) with
    {
        EnableSpring = true, Hertz = 6, DampingRatio = 0.7f,
        EnableMotor = true, MotorSpeed = 0, MaxMotorForce = 9000,
    }));
}
```

After creation: `SetJointLimit`, `SetJointMotor`, `SetJointSpring`, `SetJointTarget`, `SetJointLength`;
`AnchorAOf` and `AnchorBOf` (world points now), `ReactionForceOf`, `ReactionTorqueOf`, and `JointCoordinateOf` (a
revolute joint's angle, a prismatic joint's translation, a distance joint's length). A joint with a `BreakForce` is
destroyed at the end of a step in which it had to apply more than that to hold; `BrokenJoints` lists the joints that
broke in the latest step. Destroying a body destroys its joints. Joints are part of the world's state hash.

## Queries and contacts

Every backend answers these:

| Query | Result |
| --- | --- |
| `RayCast(from, to, filter, out hit)` | the closest shape the segment enters: shape, owner, point, normal and fraction |
| `Overlap(area, filter, results)` | the shapes that truly overlap a rectangle (sharing an edge does not count), written into your buffer |
| `BoundsOf(shape)` | a shape's world bounds now |

A backend that declares `ShapeQueries` (both of the engine's do) also answers these:

| Query | Result |
| --- | --- |
| `RayCastAll(from, to, filter, hits)` | every shape the segment enters, nearest first, one hit per shape: a shot that goes through several targets |
| `ShapeCast(shape, from, to, filter, out hit)` | the first shape a moving box, circle or capsule would touch: how far it gets (`Fraction`), where the two touch and the surface normal. "Is there room to stand up", "what would this dash hit" |
| `Overlap(shape, center, filter, results)` | the shapes under a box, a circle or a capsule: an explosion's radius, a sword's reach |
| `OverlapPoint(point, filter, results)` | the shapes that hold a point: what is under the pointer |
| `ClosestPoint(point, maxDistance, filter, out hit)` | the nearest surface within a distance, with the point on it and how far it is |

The shape of a query is a `QueryShape`: `QueryShape.Box(size)`, `QueryShape.Circle(radius)` or
`QueryShape.Capsule(radius, height)` (a capsule stands along the world's up direction). It is not a shape of the
world, so the tile backend takes circles and capsules in queries although its bodies have boxes only.

<!-- code: samples/PhysicsPlayground/PlaygroundScene.cs#beam -->
```csharp
// A beam from the cart to the right wall: every shape it goes through, nearest first.
var from = world.PositionOf(Chassis) + new Vector2(20, 0);
_beamHits = Math.Min(world.RayCastAll(from, new Vector2(312, from.Y), default, _beam), _beam.Length);
```

<!-- code: samples/PhysicsPlayground/PlaygroundScene.cs#blast -->
```csharp
// A blast: every dynamic body within reach of the cart is thrown away from it, nearer ones harder.
if (Input.Pressed("cancel"))
{
    var centre = world.PositionOf(Chassis);
    Span<ShapeId> near = stackalloc ShapeId[32];
    int count = Math.Min(world.Overlap(QueryShape.Circle(BlastRadius), centre, default, near), near.Length);
    foreach (var shape in near[..count])
    {
        var body = world.BodyOf(shape);
        if (body == Chassis || world.KindOf(body) != BodyKind.Dynamic) continue;
        var away = world.PositionOf(body) - centre;
        float reach = 1 - MathF.Min(1, away.Length() / BlastRadius);
        world.ApplyImpulse(body, Vector2.Normalize(away) * (world.MassOf(body) * 220 * reach));
    }
    Blasts++;
}
```

Rules they share:

- Results go into a span you provide, and the count returned is the whole count: when it exceeds the span's length,
  only the first ones were written.
- Queries leave sensors out unless the filter sets `IncludeSensors`.
- Layers decide which shapes a move or a query sees; `QueryFilter.Where` leaves single shapes out
  (`new QueryFilter { Where = shape => world.BodyOf(shape) != self }` for a ray that must not hit the body that fires
  it). It is asked once per shape near the query, before it runs; keep the delegate in a field rather than making one
  each step.
- Edges follow one rule on both backends:
  - A point on a shape's edge or corner is not inside it, and touching is not overlapping: a circle exactly one
    radius from a wall does not overlap it.
  - A ray hits a shape where it enters it. One that starts inside a shape does not hit it. One that starts on its
    edge hits it at fraction 0 going in, and nothing going out or along the edge. One that only runs along an edge
    or clips a corner hits nothing.
  - A cast shape does not hit what it overlaps where it starts. What it only touches there it hits at fraction 0
    when it moves into it, and not when it leaves it or slides along it. That is what `Move` does from the same
    place, so a cast answers "can I move there": a box standing on a floor, cast along it, is stopped by the wall and
    not by the floor.
  - A one-way platform is hit only from above, coming down.
- On the tile backend a cast stops flush against what it hits. On Box2D it stops within a millimetre of that, and two
  shapes that overlap by less than half a centimetre (Box2D lets resting bodies sink that far) count as touching.
- A tile grid is one shape: a ray that crosses several of its cells reports one hit, at the first one.

### Contacts

A kinematic body's contacts are in its `MoveResult` (and a character's in `LastMove`). `ContactEvents` reports the
pairs with a dynamic body that began or stopped touching in the latest step; a begin has the contact normal and the
`Point` where the two touch. `SensorEvents` reports entries and exits.

A backend that declares `ContactDetail` (Box2D) says what a contact is made of:

- `ContactImpacts` lists the hits of the latest step: every pair whose surfaces met faster than the world's impact
  speed, with the point, the normal, the `Speed` they approached at and the `Impulse` the contact applied. The impulse
  grows with the masses as well as the speed, so it is the number for damage; the speed is the number for the
  loudness of a sound. A body that bounces or rolls reports again each time it hits.
- `ContactsOf(body, results)` lists the points at which a body touches other shapes now, each with the other shape,
  the normal (pointing from the other shape towards the body: up for a body that rests on a floor), the separation
  and the impulse the solver applied there in the latest step: how hard a crate presses on a plate, whether a
  dynamic body stands on something.

<!-- code: samples/PhysicsPlayground/PlaygroundScene.cs#impacts -->
```csharp
// Impacts of the step that just ran: how fast the two surfaces met and the impulse that stopped them.
foreach (ref readonly var hit in world.ContactImpacts)
{
    Impacts++;
    HardestImpact = MathF.Max(HardestImpact, hit.Impulse);
    _sparks.Add((hit.Point, 0.25f));
}
```

Events and impacts stay readable until the next step: a scene reads them in `Update`, which runs before its physics
world steps again. The impact speed is `Box2DWorldOptions.ImpactSpeed` (one metre per second by default); contacts
slower than that are not impacts. On the tile backend `ContactImpacts` is empty and `ContactsOf` is refused: it has
no dynamic bodies.

## Tile levels

`AddTiles(body, grid, columns, rows)` attaches a tile grid to a static body, as one shape: solid cells block, one-way
cells are platforms. Both backends take it, each in its own way:

| | Tile backend | Box2D backend |
| --- | --- | --- |
| When the grid is read | at every query: changing a tile changes collisions at once | once, when the tiles are added: remove the shape and add the tiles again after changing them |
| What the level is | its cells | the outline of its solid cells: one smooth boundary for each region of open space |
| A body that slides along a row of tiles | rests and slides exactly | meets no seam between tiles: a flat row is one straight edge |
| Slopes (`TileSlopes`) | refused when the tiles are added | `TileFlags.SlopeRight` (rising to the right) and `TileFlags.SlopeLeft` cells: half tiles cut along their diagonal; a row of them is one straight ramp |
| One-way cells | a platform at the top of the cell | a run of them is one platform |
| The inside of solid tiles | solid | hollow: a dynamic body placed inside solid tiles is not pushed out. Overlaps, point queries and the nearest surface still answer that it is inside |

<!-- code: samples/PhysicsPlayground/TileScene.cs#tiles -->
```csharp
private const int Tile = 16, Solid = 1, Ledge = 2, Up = 3, Down = 4;

// '#' solid, '=' one-way platform, '/' a slope that rises to the right, '\' one that rises to the left.
private static readonly string[] Rows =
[
    "####################",
    "#..................#",
    "#..................#",
    "#....====..........#",
    "#..................#",
    "#...........\\......#",
    "#...........#\\.....#",
    "#......../####\\....#",
    "#......./######\\...#",
    "#....../########\\..#",
    "####################",
];

private readonly TileGrid _grid = new(
    TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Solid, ['='] = Ledge, ['/'] = Up, ['\\'] = Down }),
    Tile,
    new Dictionary<int, TileFlags>
    {
        [Solid] = TileFlags.Solid, [Ledge] = TileFlags.OneWay, [Up] = TileFlags.SlopeRight, [Down] = TileFlags.SlopeLeft,
    });
```

<!-- code: samples/PhysicsPlayground/TileScene.cs#world -->
```csharp
// This scene makes its own world instead of the game's: one whose Box2D ids the game may read.
_world = new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 16, YDown = true, NativeAccess = true });
Systems.Add(new ScenePhysics(_world) { SyncTransforms = false });

// The whole level is one shape of one static body: the outline of its tiles.
var level = _world.CreateBody(default, BodyDescription.Static());
Level = _world.AddTiles(level, _grid, _grid.Layer.Width, _grid.Layer.Height);
```

On both backends the level is one shape with one surface. The faces between two solid tiles are not surfaces, so a
shape that starts sunk into tiles is not stopped by the next tile. A one-way tile is a platform at the top of its
cell: moves, rays and cast shapes meet its top edge, and overlaps, point queries and the nearest surface see it a
quarter of a tile thick (`PhysicsContract.OneWayTileThickness`).

**Slopes need a capsule character.** A character that walks on slope tiles is a capsule
(`ShapeDescription.Capsule`) with a `GroundSnap` of a few units: it walks up a ramp and over the joint onto flat ground
without meeting a wall, on the ground all the way. A box character does not climb: `Move` takes a box along x and then
along y, and the foot of a ramp stops the first of the two like a wall. `CharacterController2D.Create` makes a box;
for a level with slopes make the body yourself:

```csharp
var body = world.CreateBody(entity, BodyDescription.Kinematic(start));
world.AddShape(body, ShapeDescription.Capsule(5, 14));
var hero = new CharacterController2D(world, body, new CharacterMotion { GroundSnap = 4 });
```

A slope tile under a ceiling, steeper or gentler slopes and slopes on the tile backend are not there: build those
from polygon shapes on Box2D.

**One call per level.** The outline has no seams inside one `AddTiles` call. Two areas added with two calls are two
outlines: where they meet there is a seam, as between two boxes side by side, and a dynamic box that slides over it
can stop there. Add a level with one call; for a level kept in pieces, give `AddTiles` one `ICollisionGrid` that
answers for all of them.

**Size.** On Box2D the cost of a level is its outline, not its tiles: about 0.7 KB of native memory for each straight
edge. A 250 × 250 map of random tiles (46,000 edges, far more than a drawn level of that size has) takes a tenth of
a second to add and 27 MB; a 1000 × 1000 one (750,000 edges) takes 1.6 seconds and 520 MB, and 0.15 seconds to
remove. The tile backend keeps nothing for a grid: its size costs nothing.

## Box2D's own API

The physics contract covers what the engine's backends share and what most games need of Box2D. For the rest (an
explosion, the surface speed of a conveyor belt, a motor joint that drives a velocity, wind) a game calls Box2D's C
functions itself. The package's native library exports them; the world gives the ids they take, when it was made for
it:

<!-- code: samples/PhysicsPlayground/TileScene.cs#native -->
```csharp
// Box2D's own explosion: no member of the physics contract does this. The game declares the function it calls.
[StructLayout(LayoutKind.Sequential)]
private struct ExplosionDef
{
    public ulong MaskBits;
    public Vector2 Position;                     // metres, y up: Box2D's units, not the game's
    public float Radius, Falloff, ImpulsePerLength;
}

[DllImport(Box2DNative.Library)]
private static extern void b2World_Explode(B2WorldId world, in ExplosionDef explosion);

private void Explode(Vector2 at)
{
    var explosion = new ExplosionDef
    {
        MaskBits = ulong.MaxValue,
        Position = new Vector2(at.X / 16, -at.Y / 16),
        Radius = 3, Falloff = 2, ImpulsePerLength = 4,
    };
    b2World_Explode(_world.NativeIds.World, explosion);
}
```

`Box2DWorldOptions.NativeAccess` is the switch, where the world is made; without it `NativeIds` throws. `NativeIds`
gives `World`, `Body(body)`, `Shape(shape)` and `Joint(joint)` as `B2WorldId`, `B2BodyId`, `B2ShapeId` and
`B2JointId`, laid out as Box2D declares them. What it costs:

- **Units are Box2D's**: metres, radians, y up, whatever the world's options map the game's units to.
- **The engine does not know what you changed.** It is not in the state hash and not in `DescriptionOf`; bodies,
  shapes and joints created through Box2D have no engine id, so queries and events skip them.
- **An id lives as long as its engine object.** After `DestroyBody`, `RemoveShape` or `DestroyJoint` (or after the
  world is disposed) the Box2D id you read before is dead; passing it to Box2D is undefined. Read ids when you use
  them rather than keeping them.
- **Destroying an engine object through Box2D is not supported.** The engine then refuses that body or joint with an
  `InvalidOperationException` when it is next used, and its shapes, contacts and events are in an undefined state:
  destroy engine objects with the engine.
- **Stepping the world through Box2D** (`b2World_Step`) moves the bodies but is not a step of the engine: `StepCount`
  does not move, and the events of those steps (contacts, sensors, impacts, broken joints) are never reported. Step
  with `Step`.
- **Box2D's API changes between its releases.** `Box2DPhysicsWorld2D.Box2DVersion` says which one the package carries
  (3.2.0); a later package may carry another.
- **Platforms.** Windows, Linux, macOS and Android. Not the browser (the runtime there does not pass these ids), and
  not iOS.
- **No call while the world steps**, and none from another thread.
- A tile shape is many Box2D shapes and has no id of its own: `Shape` refuses it.

## What differs on Box2D

- **Units.** Box2D works in metres with y up. `Box2DWorldOptions { UnitsPerMeter = 16, YDown = true }` makes the world
  take and return pixels in the engine's y-down space. Box2D is tuned for moving objects of 0.1 to 10 metres.
- **Resting.** `Move` stops a box a little short of the surface in its way: it rests up to `Skin` away (1.5 cm), never
  inside; a capsule may sink into the floor by up to the same amount. The tile backend rests exactly (`Skin` is 0).
- **Events.** Events of the same step can come in another order than on the tile backend, and a touch can be reported
  one step earlier or later.
- **Angles** go through Box2D's own sine and cosine: an angle reads back within about 0.002 radians of the one set.
- **Determinism.** Identical results on every platform for the same build; never identical to the tile backend or to
  another Box2D version.
- **Lifetime.** A world owns native memory. A scene's world is disposed with the scene; dispose a world you made
  yourself. Box2D keeps at most 128 worlds alive in a process.

| Platform | Box2D package |
| --- | --- |
| Windows x64, Linux x64, macOS (Apple silicon and Intel), browsers | supported |
| Android (arm64, x64), iOS (arm64) | experimental: the library is included for these devices |

## Seeing it

`Game.DebugDraw.Physics(world)` draws every shape of a world, coloured by body kind, the tiles of tile geometry and a
normal for each contact that began in the latest step. See [diagnosing a game](diagnostics.md).

## Limits

- Tile backend: axis-aligned boxes only, no slopes, no dynamic bodies, no joints, no contact detail.
- Box2D backend: no solids that push kinematic bodies (the character controller carries a character standing on a
  moved body on either backend); tile geometry is read when it is added and is hollow inside; slope tiles are the two
  diagonals of a tile, and only a capsule character walks on them; a level costs memory by the edges of its outline
  ([Tile levels](#tile-levels)).
- A query shape is a box, a circle or a standing capsule: no rotated or polygon query shapes.
- 2D only.

## Common mistakes

- **Stepping the world yourself in a scene.** `Scene.Physics` steps it after `Update`; a second `Step` runs physics
  twice as fast.
- **Applying a force once.** A force lasts one step. Apply it every step, or use an impulse.
- **Moving a dynamic body with `SetPosition` every step.** That teleports it through everything. Use forces, impulses
  or `SetVelocity`, or make it kinematic and use `Move`.
- **Pixel-sized Box2D worlds without `UnitsPerMeter`.** A 16-unit box is a 16-metre box: it falls in slow motion. Set
  the scale.
- **Reading a character's ground from `ContactEvents`.** Kinematic bodies never appear there: read `IsGrounded` or
  `LastMove`.

## Related

- [Tile maps and collision](world.md): tile grids, `TileMotion` for a box that needs no world
- [Scenes](scenes.md): scene systems, the scene world and its clock
- API: [`IPhysicsWorld2D`](../api/MicroEngine.Physics.IPhysicsWorld2D.md),
  [`CharacterController2D`](../api/MicroEngine.Physics.CharacterController2D.md),
  [`ScenePhysics`](../api/MicroEngine.Scenes.ScenePhysics.md),
  [`JointDescription`](../api/MicroEngine.Physics.JointDescription.md),
  [`TilePhysicsWorld2D`](../api/MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md)
