# Tile maps and collision

The `MicroEngine.World` namespace holds what most 2D games build their levels from: grids of tiles, what each tile
means for movement, a function that moves a box through the grid without passing through walls, and a list type for
game objects. Tile collision is deliberately small and exact: axis-aligned boxes against a grid, with no level editor
format. A world of bodies with collision layers, queries and sensors, a character controller, and (with the optional
Box2D backend) forces, rotation and joints are described in [Physics](physics.md).

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

A box that needs no world moves like this:

```csharp
_velocity.Y = MathF.Min(_velocity.Y + Gravity * dt, MaxFall);
var move = TileMotion.Move(_box, _velocity * dt, grid);
_box = move.Bounds;
if (move.HitFloor || move.HitCeiling) _velocity.Y = 0;
```

A character is more than that (jumps, coyote time, one-way platforms, moving platforms): use the
[character controller](physics.md#a-character) on the scene's physics world, which reads the same grid.

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

The tutorial's player and slimes are plain classes too. A game whose objects are combinations of capabilities that
come and go can keep them in a component store instead: [Entity Component Systems](../guide/entity-component-systems.md).

## Drawing tiles

`TileRenderer.Draw(r, layer, tileSize, camera, spriteFor)` draws the cells the camera shows, zoom and rotation
included, with a one-tile margin, calling `spriteFor(id, x, y)` for each non-empty cell; return `null` to skip one.
Call it between `PushCamera` and `PopCamera`. Its longer overload also takes each cell's `TileOrientation` (mirrored
left to right, top to bottom, or across the diagonal, as map editors store it; the eight combinations cover every
quarter turn), where the layer is drawn, a tint and an extra margin for sprites larger than a cell.
`Autotile.Mask4(layer, x, y, same)` returns a 4-bit mask of matching neighbours (1 up, 2 right, 4 down, 8 left), the
usual way to choose a wall's edge or corner sprite.

## Maps from Tiled

The optional `MicroEngine.Tiled` package loads maps made in the [Tiled](https://www.mapeditor.org/) editor: `.tmx` and
`.tmj` files with their external tilesets (`.tsx`, `.tsj`) and templates (`.tx`, `.tj`), compressed or not, finite or
infinite.

```xml
<PackageReference Include="MicroEngine.Tiled" Version="0.6.0-alpha.1" />
```

`Assets.TiledMap(path)` reads the map and every file it names through the game's assets, so maps load from folders,
archives and browser packages alike, once per scope. The map is data; `new TiledSprites(map, Assets)` turns its
tileset images into textures. Load both in `Load`, as any asset:

<!-- code: samples/TiledLevel/TiledLevelGame.cs#load -->
```csharp
// The map, its tileset and its templates come through the game's assets, like any content file; the tileset's
// images become textures here, so nothing loads during play.
Map = Assets.TiledMap("map/sandbox.tmx");
Sprites = new TiledSprites(Map, Assets);
```

What the map holds:

- **Tile layers** (`map.TileLayer(name)`) are a `TileLayer` of global tile ids (0 for empty) the size of the map, and
  `OrientationAt(x, y)` for each cell. `map.Tile(id)` gives a tile's tileset, class, properties, animation frames and
  the collision shapes drawn in Tiled's tile editor.
- **Object layers** (`map.ObjectLayer(name)`, or `map.Objects` for all of them) hold `TiledObject`s: name, class,
  shape (rectangle, ellipse, point, polygon, polyline, text, tile), position in map pixels with the layer's offset
  included, size, rotation in radians, `Bounds` and `TurnedBounds`. A template's instance has the template's values
  and properties, its own winning; a tile object has its tile's class and properties under its own.
- **Properties** of the map, layers, tilesets, tiles and objects are typed: `String`, `Int`, `Float`, `Bool`, `Color`,
  `Class` (a custom class's members); a file property is a content path, an object property an object id
  (`map.Object(id)`).
- **Layers** keep their visibility, opacity, tint, offset and parallax factor, groups included.

The engine gives no class or property a meaning: what an object is, is the game's rule. The Tiled sample builds its
level from the objects the designer marked:

<!-- code: samples/TiledLevel/LevelScene.cs#level -->
```csharp
// The map is data: this game decides what its classes and properties mean.
foreach (var o in map.Objects)
{
    if (o.Properties.String("bodyType") is "static" or "dynamic") _solids.Add(o.TurnedBounds);
    if (o.Class == "coin") _coins.Add(o);
    if (o.Class == "hero")
    {
        _heroSprite = o;
        // The box stands where the sprite's feet are: a tile object's position is its bottom-left corner.
        _hero = new RectF(o.Position.X + (o.Size.X - HeroBox.X) / 2, o.Position.Y - HeroBox.Y, HeroBox.X, HeroBox.Y);
    }
}
```

For a tile map, `map.CollisionGrid(layer, "collision")` builds a `TileGrid` over a tile layer from a tile property
the game names: `true` is solid, a text lists flags (`solid`, `oneway`, `hazard`, `water`, `custom1` to `custom4`), a
whole number is the flags' bits. The grid reads the layer, so changing a cell changes collisions at once.
`CollisionGrid(layer, tile => ...)` chooses the flags in code.

`TiledSprites.Draw(r, tileLayer, camera, seconds)` draws a tile layer as `TileRenderer` does, with each cell's
orientation, the layer's offset, opacity and tint, and animated tiles at `seconds`. `Draw(r, objectLayer, ...)` draws
an object layer's tile objects as Tiled shows them: stretched to their size, standing on their bottom-left corner,
mirrored and turned about it. `DrawAll(r, camera, seconds)` draws every visible layer in order, each shifted by its
parallax (`map.ParallaxShift(layer, camera.Center)`, which a game can also use itself):

<!-- code: samples/TiledLevel/LevelScene.cs#draw -->
```csharp
// Layer by layer, as Tiled shows them, each shifted by its parallax. The "game" layer's objects move, so the
// scene draws them where the game has them instead of where the map placed them.
var map = game.Map;
foreach (var layer in map.ObjectLayers)
{
    var shift = map.ParallaxShift(layer, WorldCamera!.Center);
    if (layer.Name != "game") game.Sprites.Draw(r, layer, WorldCamera, Age, shift);
    else
    {
        foreach (var coin in _coins) game.Sprites.Draw(r, coin, Age);
        var feet = new Vector2(_hero.Center.X - _heroSprite.Size.X / 2, _hero.Bottom);
        game.Sprites.Draw(r, _heroSprite with { Position = feet, Orientation = _facingLeft ? TileOrientation.FlipX : TileOrientation.None });
    }
}
```

A 1000 × 1000 map of three compressed layers loads in about 80 ms and takes about 14 MB; drawing costs what the
camera shows (about 350 tiles of 16 pixels in a 320 × 180 view), whatever the map's size. Load one map per area for
worlds larger than that.

## Performance

`TileMotion.Move` checks only the cells the box sweeps at each sub-step: a handful of lookups per moving object.
`TileRenderer` touches only visible cells, so a large map costs what the screen shows. Both are fine for hundreds of
moving objects per step. A `TilePhysicsWorld2D` files its box shapes in a grid of 64-unit cells, so a move, a query or
a sensor looks only at the shapes near it; the results are the same as looking at every shape.

## Limits

- Tile collision is axis-aligned boxes only: no slopes, circles or rotated shapes (the optional Box2D
  [physics backend](physics.md#choosing-a-backend) has them, with forces, rotation and joints). `TileMotion` moves one
  box against rectangles that stand still during the call; a physics world's moving solids push and carry other bodies.
- One tile size per grid; no isometric or hexagonal grids.
- Maps from Tiled load with `MicroEngine.Tiled`, orthogonal ones only. Image layers and text objects are data: the
  game draws them. No other map editor's files are read (LDtk: read them yourself into a `TileLayer`).

## Common mistakes

- **Colliding with the sprite's rectangle.** Use a smaller box at the feet (top-down) or around the body (side view);
  draw the sprite from the box's position.
- **Checking `IsOnGround` after moving.** Check it before deciding to jump, then move.
- **Resetting vertical speed only on `HitFloor`.** Reset it on `HitCeiling` too, or the player sticks to ceilings.

## Physics worlds

A tile grid is also the level of a physics world: `Physics.World.AddTiles(level, grid, columns, rows)` in a scene makes
its solid and one-way cells the static geometry that characters, sensors, rays and moving platforms collide with.
[Physics](physics.md) covers the scene's world, the character controller, the two backends (tiles by default, Box2D
as an optional package), forces, joints and queries.

### The Box2D backend

`MicroEngine.Physics.Box2D` is the optional backend for bodies that fall, stack, roll and are joined together:
[choosing a backend](physics.md#choosing-a-backend) and [what differs on Box2D](physics.md#what-differs-on-box2d).

## Related

- [Tutorial step 6: tile map and camera](../tutorial/06-garden.md), [step 7: collisions](../tutorial/07-collisions.md)
- [Physics](physics.md): the scene's physics world, characters, forces and joints
- [Graphics](rendering.md#tile-maps): drawing layers
- API: [`TileMotion`](../api/MicroEngine.World.TileMotion.md), [`TileGrid`](../api/MicroEngine.World.TileGrid.md),
  [`IWorld`](../api/MicroEngine.Scenes.IWorld.md),
  [`IPhysicsWorld2D`](../api/MicroEngine.Physics.IPhysicsWorld2D.md),
  [`TilePhysicsWorld2D`](../api/MicroEngine.Physics.Tiles.TilePhysicsWorld2D.md),
  [`TiledMap`](../api/MicroEngine.Tiled.TiledMap.md), [`TiledSprites`](../api/MicroEngine.Tiled.TiledSprites.md)
