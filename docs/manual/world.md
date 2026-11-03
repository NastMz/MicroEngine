# Tile maps and collision

The `MicroEngine.World` namespace holds what most 2D games build their levels from: grids of tiles, what each tile
means for movement, a function that moves a box through the grid without passing through walls, and a list type for
game objects. It is deliberately small: there is no physics engine (no forces, masses, slopes or rotating bodies) and no
level editor format. Movement rules are game code that calls these pieces.

## The pieces

| Type | Role |
| --- | --- |
| `TileLayer` | a grid of integer tile ids (0 means empty); built in code or from text rows with `Parse` |
| `TileFlags` | what a tile does to movement: `Solid`, `OneWay`, `Hazard`, `Water`, `Custom1`..`Custom4` |
| `TileGrid` | a layer plus a table from id to flags, and the tile size: the collision world (`ICollisionGrid`) |
| `TileMotion` | moves a box through a grid: `Move`, `IsOnGround`, `TouchedFlags`, `IsSolidAt` |
| `TileRenderer` | draws the visible cells of a layer, asking the game which sprite each id uses |
| `Autotile` | `Mask4`: which of the four neighbours match, to pick edge and corner sprites |
| `Entity`, `EntityList<T>` | optional base class and list for game objects, with safe spawning and removal (removed in the next SDK version: a scene keeps its objects in `Scene.Objects`, an `IWorld`, [The scene world](scenes.md#the-scene-world)) |

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

`TileMotion.Move(box, delta, grid, solids = null, ignoreOneWay = false)` returns where the box ends up and what it hit:

| `MoveResult` member | Meaning |
| --- | --- |
| `Bounds`, `Position` | the box after the move |
| `HitLeft`, `HitRight`, `HitWall` | stopped horizontally |
| `HitFloor`, `HitCeiling` | stopped vertically |
| `Touched` | the union of the flags of every tile the box overlaps afterwards (`Hazard`, `Water`, custom flags) |

How it works, and why it matters:

- **X first, then Y.** A box moving diagonally into a wall stops on one axis and keeps sliding on the other.
- **Sub-steps of at most half a tile**, so a fast box never tunnels through a one-tile wall.
- **Solid** tiles block from every side. **One-way** tiles block only a box moving down onto their top edge from above:
  jump up through them, land on them, and pass `ignoreOneWay: true` to drop through.
- **`solids`**: extra rectangles that block like solid tiles (moving platforms, doors, push blocks). A rectangle the
  box already overlaps does not block it, so a box is never stuck inside one.
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

With an `EntityList` (removed in the next SDK version: a scene keeps its objects in `Scene.Objects`, an `IWorld`), `list.Overlapping(rect)` returns the living entities whose bounds overlap a rectangle. Next SDK version (not published yet: arrives with the next SDK version):
walking an `EntityList`, `OfType<T>()` or `Overlapping` with `foreach` allocates nothing. There is no
spatial index: checking every pair is fast for the few hundred objects a scene of a pixel-art game holds.
A game with thousands of objects would bucket them by tile itself.

## Entities

Removed in the next SDK version (not published yet): `Entity` and `EntityList<T>` leave the SDK. A scene keeps its
objects in `Scene.Objects`, an `IWorld` with entity ids, transforms, attachments, interpolation for drawing and a
clock ([The scene world](scenes.md#the-scene-world)), or in plain classes of its own, as the platformer example does
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

In the published SDK, `Entity` is an optional base class: a `Position`, a `Hitbox` relative to it (`Bounds` in
world pixels), virtual `Update` and `Draw`, a `Depth` for draw order, and `Destroy()`; `EntityList<T>` updates and
draws a list of them:

- Adding during an update is deferred to the end of that update, and destroyed entities leave the list then (calling
  `Removed()`), so entities can spawn and destroy each other while the list is iterated.
- `Draw(r, camera)` sorts by `Depth` (top-down games use the feet's Y, so lower objects draw in front) and skips
  entities outside the camera's view.

Using `Entity` was always a choice, not a requirement: the tutorial's player and slimes are plain classes. MicroEngine has no
entity-component system.

## Drawing tiles

`TileRenderer.Draw(r, layer, tileSize, camera, spriteFor)` draws the cells the camera sees (with a one-tile margin),
calling `spriteFor(id, x, y)` for each non-empty cell; return `null` to skip one. Call it between `PushCamera` and
`PopCamera`. `Autotile.Mask4(layer, x, y, same)` returns a 4-bit mask of matching neighbours (1 up, 2 right, 4 down,
8 left), the usual way to choose a wall's edge or corner sprite.

## Performance

`TileMotion.Move` checks only the cells the box overlaps at each sub-step: a handful of lookups per moving object.
`TileRenderer` touches only visible cells, so a large map costs what the screen shows. Both are fine for hundreds of
moving objects per step.

## Limits

- Axis-aligned boxes only: no slopes, circles or rotated shapes, no continuous collision between moving objects.
- One tile size per grid; no isometric or hexagonal grids.
- No importer for map editors (Tiled, LDtk): read their files yourself into a `TileLayer` if you use one.

## Common mistakes

- **Colliding with the sprite's rectangle.** Use a smaller box at the feet (top-down) or around the body (side view);
  draw the sprite from the box's position.
- **Checking `IsOnGround` after moving.** Check it before deciding to jump, then move.
- **Resetting vertical speed only on `HitFloor`.** Reset it on `HitCeiling` too, or the player sticks to ceilings.

## Related

- [Tutorial step 6: tile map and camera](../tutorial/06-garden.md), [step 7: collisions](../tutorial/07-collisions.md)
- [Graphics](rendering.md#tile-maps): drawing layers
- API: [`TileMotion`](../api/MicroEngine.World.TileMotion.md), [`TileGrid`](../api/MicroEngine.World.TileGrid.md),
  [`IWorld`](../api/MicroEngine.Scenes.IWorld.md)
