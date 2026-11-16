# Large worlds and nearby objects

A game with a few hundred objects walks all of them every step and never thinks about it. This page is for the games
that outgrow that: a swarm whose members look at their neighbours, towers that choose a target among thousands of
enemies, bullets tested against everything they might hit, a world many screens wide where only what is near the
player matters.

| To do this | Use |
| --- | --- |
| Find the objects within a distance of a point | `SpatialIndex2D<T>.QueryCircle` |
| Find the objects inside a rectangle (the view, a room, an explosion's box) | `SpatialIndex2D<T>.Query` |
| Move and draw only what is near the camera | a query with `Camera2D.VisibleBounds` |
| Skip drawing one object that is off screen | `Camera2D.IsVisible` |
| Find what a body would hit, or cast a ray through the level | the physics world ([Physics worlds](../manual/world.md#physics-worlds)) |
| Draw a tile map of any size | `TileRenderer.Draw`, which draws only the tiles in view |
| Load and release a region's art | asset scopes and `Preload` ([Assets](../manual/assets.md#levels-and-scenes-scopes)) |

Nothing here is switched on by default and nothing changes how a scene's world works: an index is an object your game
creates when it has a question about places.

## When walking everything is enough

Walking a world is cheap: reading and moving 100,000 entities through `All<T>` and `Local` takes about half a
millisecond on a desktop. What does not scale is asking a question about every object *for* every object. A thousand
objects that each look at all the others make a million comparisons a step, about a fifth of a 60 Hz frame; two
thousand make four million and fill it. If only a handful of objects ask (the player, a few towers), walk the world
and keep the code simple.

## An index of places

`SpatialIndex2D<T>` keeps items of your own under a point or a rectangle of the world and finds them by area. `T` is
whatever a query should give back: an `EntityId`, an attachment, a number that indexes your own arrays. Items do not
need a physics body, and the index is not part of the scene's world: create one for each thing you need to find.

<!-- code: samples/Examples/Examples/NearbyExample.cs#setup -->
```csharp
private sealed class Firefly
{
    public SpatialHandle Place;   // where the index has it
    public Vector2 Velocity;
    public bool Lit;
}

private static readonly RectF Meadow = new(0, 0, 1600, 900);
private readonly SpatialIndex2D<EntityId> _index = new(cellSize: 64);   // about the size of the usual query
private readonly List<EntityId> _found = [];                              // reused by every query: no garbage
private readonly Camera2D _camera = new(320, 180) { Bounds = Meadow, FollowSharpness = 8 };
private Vector2 _lantern = Meadow.Center;

public override void Enter()
{
    for (int i = 0; i < 3000; i++)
    {
        var id = Objects.Create();
        var at = new Vector2(Game.Random.NextSingle() * Meadow.Width, Game.Random.NextSingle() * Meadow.Height);
        Objects.Local(id).Position2D = at;
        Objects.Attach(id, new Firefly
        {
            Place = _index.Add(id, at),   // a point; Add(id, rectangle) files something with a size
            Velocity = new Vector2(Game.Random.Next(-20, 21), Game.Random.Next(-20, 21)),
        });
    }
    _camera.CenterOn(_lantern);
}
```

`Add` returns a `SpatialHandle`: keep it with the object (here in its attachment), pass it to `Move` whenever the
object moves and to `Remove` when it goes. A handle of a removed item is stale, as an entity's id is: `Contains` says
false and `Move` throws.

## Who is near

<!-- code: samples/Examples/Examples/NearbyExample.cs#near -->
```csharp
// The ones within 40 pixels of the lantern, whatever the cell they are in.
_found.Clear();
Near = _index.QueryCircle(_lantern, 40, _found);
foreach (var id in _found) Objects.Get<Firefly>(id).Lit = true;
```

- A circle query finds every item with a point within the radius of the centre; a rectangle query every item that
  overlaps or touches the area. Edges count as inside.
- Results go into a list you keep (it is added to, not cleared) or into a `Span<T>`, such as a `stackalloc` buffer.
  With a span the call returns how many items there are, which may be more than the span holds: the first ones are
  written.
- The order of the results depends only on the calls made on the index, so two runs of the same game get the same
  results in the same order. It is not by distance: to pick the nearest, compare the distances of what came back.
- A query allocates nothing. A move allocates only when the item reaches more cells than the index has room for.

## Only what is near the view

In a world much larger than the screen, a query with the camera's `VisibleBounds` gives the objects worth updating,
and another gives the ones to draw. What is outside keeps its place and its state and costs nothing.

<!-- code: samples/Examples/Examples/NearbyExample.cs#region -->
```csharp
// The active region: the view and a margin around it. A firefly outside it keeps its place and is not touched.
_found.Clear();
Awake = _index.Query(_camera.VisibleBounds.Inflate(64), _found);
foreach (var id in _found)
{
    var firefly = Objects.Get<Firefly>(id);
    ref var local = ref Objects.Local(id);
    var p = local.Position2D + firefly.Velocity * dt;
    if (p.X < Meadow.Left || p.X > Meadow.Right) firefly.Velocity.X = -firefly.Velocity.X;
    if (p.Y < Meadow.Top || p.Y > Meadow.Bottom) firefly.Velocity.Y = -firefly.Velocity.Y;
    local.Position2D = p = Vector2.Clamp(p, Meadow.Position, Meadow.Position + Meadow.Size);
    _index.Move(firefly.Place, p);   // tell the index where it is now
    firefly.Lit = false;
}
```

<!-- code: samples/Examples/Examples/NearbyExample.cs#draw -->
```csharp
r.PushCamera(_camera);
r.DrawRect(Meadow, Color.Hex("38b764"), 2);
r.FillCircle(_lantern, 40, Color.Hex("ffcd75").WithAlpha(0.15f));
// What the view shows: a firefly is 2 pixels wide, so one 2 pixels outside the view still shows.
Span<EntityId> visible = stackalloc EntityId[512];
Drawn = Math.Min(visible.Length, _index.Query(_camera.VisibleBounds.Inflate(2), visible));
foreach (var id in visible[..Drawn])
{
    bool lit = Objects.Get<Firefly>(id).Lit;
    r.FillCircle(Objects.DrawPosition2D(id), lit ? 2 : 1, lit ? Color.Hex("ffcd75") : Color.Hex("a7f070"));
}
r.FillCircle(_lantern, 3, Color.Hex("f4f4f4"));
r.PopCamera();
```

The margin around the view is your game's decision: wide enough that nothing visibly wakes up at the edge of the
screen. So is what sleeping means. An enemy far away may stand still, move on a coarser timer or be removed and
remembered as a line of data; the index only tells you which is which.

If every object must keep moving but only some are drawn, `Camera2D.IsVisible(bounds)` skips the ones off screen
without an index. The test itself has a cost for every object, so in a world of hundreds of thousands a query is the
cheaper way to find the few thousand in view.

## Choosing the cell size

The index is a grid of square cells; `cellSize` is the side of one, in world units. A query looks at the cells its
area reaches, so:

- Make a cell about the size of the usual query: the radius a tower sees, the distance at which neighbours matter.
  For a view query, a cell a few times smaller than the view works well.
- Cells much smaller than the queries make each query visit many cells; cells much larger make it look at many items
  it then discards. Both still give the right answer.
- An item that covers more than 64 cells (the floor of a level, a boss as large as the screen) is kept apart and
  tested by every query. A few of those are fine; if most of your items are that large, the cells are too small.
- A move inside the item's cells only updates its place. Objects that cross a cell every step cost more to move.

## With physics, the camera, tile maps and assets

Each of these answers its own question, and none needs the others:

- **The index** answers "what is near" for whatever your game files in it, bodies or not.
- **The physics world** answers what a body hits: `Move`, `RayCast` and `Overlap` look only at the shapes near the
  area asked for. Give a body to what must collide, not to everything that must be found. In the tile backend a
  body costs nothing in a step in which your game does not move it, so objects outside the active region need no
  special handling there.
- **The camera** answers what is visible (`VisibleBounds`, `IsVisible`).
- **Tile maps** are already drawn by what is visible, and collisions read only the cells a body touches, so a large
  map costs memory, not time.
- **Assets** load and unload by scope. A world made of regions can keep one map file and one scope per region, start
  `Preload` for the next region's files before the player reaches it and dispose the scope of the region left behind.
  A map loads whole: to load a world in parts, save it as several maps.

## Costs and limits

- A circle query that finds a dozen neighbours among tens of thousands of items takes well under a microsecond, and
  moving an item within its cell a few nanoseconds. A swarm of 20,000 in which every member counts its neighbours
  every step fits in a 60 Hz frame on a desktop.
- An index keeps between 100 and 200 bytes per item, and grows only when items are added.
- A world of 200,000 entities of which 2,000 are near the view updates and draws in under a millisecond through an
  index. Drawing all 200,000 would take the whole frame.
- Destroying an entity costs the same in a world of a hundred entities as in one of a million: the world does not
  walk itself to release what a step destroyed.
- One index is not safe to change from two threads. Queries change nothing, so several threads may query at once
  while none adds, moves or removes.
- There is no nearest-first order, no ray or segment query and no index over three dimensions. There is no automatic
  region system: no chunks that load themselves, no entities put to sleep by the engine.

## Related

- The "Nearby objects" example (`samples/Examples/Examples/NearbyExample.cs`): three thousand fireflies in a meadow
  five screens wide, of which the ones near the view move and the ones near the lantern light up.
- [The scene world](../manual/scenes.md#the-scene-world): entities, transforms and attachments.
- [Tile maps and collision](../manual/world.md): tile maps, bodies and the physics worlds.
- [Navigation](navigation.md): `DistanceField` sends many agents to one goal without a query each.
- API: [`SpatialIndex2D<T>`](../api/MicroEngine.Scenes.SpatialIndex2D-1.md),
  [`SpatialHandle`](../api/MicroEngine.Scenes.SpatialHandle.md).
