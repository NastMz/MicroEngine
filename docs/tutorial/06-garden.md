# 6. Tile map and camera

Part 3 turns the lawn into a garden: a map of 40x20 tiles (640x320 pixels, twice the screen in each direction) with
hedges, gems and slimes, a camera that follows the gardener, collisions, sound and a HUD. This step builds the map and
the camera.

## More art in code

`GardenArt.cs` grows a hedge tile, a gem and a slime. The tiles:

<!-- code: samples/GemGarden/part3-garden/GardenArt.cs#tiles -->
```csharp title="GardenArt.cs"
/// <summary>Grass: two greens mixed by value noise and an ordered dither.</summary>
public static PixelImage Grass() => new PixelImage(16, 16)
    .Map((x, y, _) => Art.Dither(x, y, Art.ValueNoise(x, y, 6) * 0.45f) ? Palette['d'] : Palette['g']);

/// <summary>Hedge: dark leaves with light speckles and an outline.</summary>
public static PixelImage Hedge() => new PixelImage(16, 16)
    .Map((x, y, _) => Art.Hash(x, y, 7) > 0.75f ? Palette['g'] : Palette['d'])
    .DrawRect(0, 0, 16, 16, Palette['o']);
```

The gem is a **pixel map**: one text row per pixel row, one character per pixel, `.` for transparent, every other
character looked up in the palette:

<!-- code: samples/GemGarden/part3-garden/GardenArt.cs#palette -->
```csharp title="GardenArt.cs"
// One character per colour: the rows below are pixel maps, '.' is transparent.
private static readonly Dictionary<char, Color> Palette = Art.Palette(
    "o=#1a1c2c", "c=#41a6f6", "w=#ffffff", "p=#b13e53", "g=#38b764", "d=#257179");

public static PixelImage Gem() => PixelImage.FromMap(Palette,
    "....o....",
    "...oco...",
    "..occco..",
    ".occwcco.",
    "occccccco",
    ".occccco.",
    "..occco..",
    "...oco...",
    "....o....");
```

The slime is two such maps, at rest and squashed: see the [checkpoint's `GardenArt.cs`](../../samples/GemGarden/part3-garden/GardenArt.cs).
All of it joins the atlas in `MainGame.Load`:

<!-- code: samples/GemGarden/part3-garden/MainGame.cs#load -->
```csharp title="MainGame.cs"
protected override void Load()
{
    var content = new ContentFolder();
    Sprites = new SpriteAtlasBuilder()
        .AddGrid("hero", content.Image("hero.png"), 16, 16, origin: new Vector2(8, 16))
        .Add("gem", GardenArt.Gem(), new Vector2(4, 4))
        .AddFrames("slime", GardenArt.Slime(), new Vector2(6, 8))
        .Add("grass", GardenArt.Grass())
        .Add("hedge", GardenArt.Hedge())
        .Build(Graphics);

    LoadSounds(content);
    Scenes.Push(new TitleScene(this));
}

protected override void Unload() => Sprites.Dispose();
```

`AddFrames("slime", images, origin)` adds `slime0` and `slime1` with one origin; `LoadSounds` comes in
[step 8](08-sound.md).

## The map as text

Create `World/Level.cs`. MicroEngine has no level editor: maps are written in code, and the most readable way is text
rows, one character per tile:

<!-- code: samples/GemGarden/part3-garden/World/Level.cs#map -->
```csharp title="World/Level.cs"
public const int TileSize = 16;
private const int Grass = 1, Hedge = 2;

// '#' hedge, '.' grass, 'P' the hero's start, '*' a gem, 's' a slime. 40 x 20 tiles: 640 x 320 pixels.
private static readonly string[] Map =
[
    "########################################",
    "#P....*........#...........*...........#",
    "#..........*...#.....s.........*.......#",
    "#...####.......#..........######.......#",
    "#...#..*.......#..*.......#....#...*...#",
    "#...#...............*.....#..*.#.......#",
    "#...####...s..........s...#....#.......#",
    "#.........................##..##...s...#",
    "#..*.......######..............*.......#",
    "#..........#....#.....*................#",
    "#####..*...#.*..#..........#######.....#",
    "#..........#....#....s.....#.....#..*..#",
    "#...s......##.###..........#..*..#.....#",
    "#..................*.......#.....#.....#",
    "#..*.......*...............###.###.....#",
    "#.......................s.........*....#",
    "#...######.....*.....#######...........#",
    "#...#....#...........#.....#.....*.....#",
    "#.*......#.....s.....#..*..#...........#",
    "########################################",
];
```

The constructor turns the rows into the engine's tile types:

<!-- code: samples/GemGarden/part3-garden/World/Level.cs#build -->
```csharp title="World/Level.cs"
private readonly SpriteAtlas _sprites;

public Level(SpriteAtlas sprites)
{
    _sprites = sprites;
    Layer = TileLayer.Parse(Map, new Dictionary<char, int>
    {
        ['#'] = Hedge, ['.'] = Grass, ['P'] = Grass, ['*'] = Grass, ['s'] = Grass,
    });
    Grid = new TileGrid(Layer, TileSize, new Dictionary<int, TileFlags> { [Hedge] = TileFlags.Solid });
    PlayerStart = CellCenter(TileLayer.Find(Map, 'P').Single());
    GemSpots = [.. TileLayer.Find(Map, '*').Select(CellCenter)];
    SlimeStarts = [.. TileLayer.Find(Map, 's').Select(CellCenter)];
}

public TileLayer Layer { get; }
/// <summary>What blocks movement: hedges are solid, and so is everything outside the map.</summary>
public TileGrid Grid { get; }
/// <summary>The whole garden in world pixels, for the camera's bounds.</summary>
public RectF Area => new(0, 0, Grid.PixelWidth, Grid.PixelHeight);
public Vector2 PlayerStart { get; }
public IReadOnlyList<Vector2> GemSpots { get; }
public IReadOnlyList<Vector2> SlimeStarts { get; }

/// <summary>World position of the centre of a map cell.</summary>
private static Vector2 CellCenter((int X, int Y) cell) => new((cell.X + 0.5f) * TileSize, (cell.Y + 0.5f) * TileSize);
```

- A `TileLayer` is a grid of integer tile ids. `TileLayer.Parse` maps each character through the legend; characters
  not in it become 0, "empty".
- A `TileGrid` adds what each id *means* for movement: `TileFlags.Solid` (blocks), `OneWay` (a platform you can jump
  through from below), `Hazard`, `Water`, or four `Custom` flags of your own. Cells outside the map are solid by
  default, so the garden is closed even without its border.
- `TileLayer.Find(rows, 'P')` lists the cells holding a marker: where things start. The gems and slimes are not tiles;
  they are objects at those positions (step 7).

## World and screen coordinates

Positions in the garden are **world pixels**: (0, 0) is the garden's top-left corner and (640, 320) its bottom-right.
The screen shows 320x180 of them. The **camera** decides which part: everything drawn between `r.PushCamera(camera)`
and `r.PopCamera()` is in world pixels and shifted by the camera; everything drawn after `PopCamera` is in screen
pixels, which is where a HUD belongs.

The play scene creates the level and the camera:

<!-- code: samples/GemGarden/part3-garden/Scenes/PlayScene.cs#fields -->
```csharp title="Scenes/PlayScene.cs"
private static readonly ParticleSpec Sparkle = new()
{
    StartColor = Color.Hex("73eff7"), EndColor = Color.Hex("41a6f600"), Speed = (20, 60), Life = (0.3f, 0.6f),
};

private readonly MainGame _game;
private readonly Level _level;
private readonly Player _player;
private readonly List<Vector2> _gems;
private readonly List<Slime> _slimes;
private readonly Camera2D _camera;
private readonly ParticleSystem _sparkles = new(capacity: 128);

public PlayScene(MainGame game)
{
    _game = game;
    _level = new Level(game.Sprites);
    _player = new Player(game.Sprites, _level.PlayerStart);
    _gems = [.. _level.GemSpots];
    _slimes = [.. _level.SlimeStarts.Select(p => new Slime(game.Sprites, p))];
    // The camera shows a 320x180 window of the 640x320 garden and never looks outside it.
    _camera = new Camera2D(game.Width, game.Height) { Bounds = _level.Area };
    _camera.CenterOn(_player.Position);
}

public int Gems { get; private set; }
public int TotalGems => _level.GemSpots.Count;
```

and draws the world through it:

<!-- code: samples/GemGarden/part3-garden/Scenes/PlayScene.cs#draw -->
```csharp title="Scenes/PlayScene.cs"
public override void Draw(IRenderer r)
{
    // World: everything between PushCamera and PopCamera is drawn in garden coordinates.
    r.PushCamera(_camera);
    _level.Draw(r, _camera);
    foreach (var gem in _gems) r.Draw(_game.Sprites["gem"], gem + new Vector2(0, MathX.Wave(Age, 1.5f, 1.5f)));
    foreach (var slime in _slimes) slime.Draw(r);
    _player.Draw(r);
    _sparkles.Draw(r);
    r.PopCamera();

    // HUD: drawn after PopCamera, so it stays in screen coordinates.
    DrawHud(r);
}
```

The level draws its tiles with `TileRenderer`, which asks the game which sprite each tile id uses and skips the tiles
the camera cannot see:

<!-- code: samples/GemGarden/part3-garden/World/Level.cs#draw -->
```csharp title="World/Level.cs"
/// <summary>Draws the tiles the camera sees. Call it between PushCamera and PopCamera.</summary>
public void Draw(IRenderer r, Camera2D camera) =>
    TileRenderer.Draw(r, Layer, TileSize, camera, (id, _, _) => id == Hedge ? _sprites["hedge"] : _sprites["grass"]);
```

The gems bob up and down: `MathX.Wave(time, frequency, amplitude)` is a sine wave, and `Age` the scene's time.

## Following the gardener

The camera follows the gardener in the scene's `Update` ([step 7](07-collisions.md) shows the whole method):

<!-- code: samples/GemGarden/part3-garden/Scenes/PlayScene.cs#camera -->
```csharp title="Scenes/PlayScene.cs"
_camera.Follow(_player.Position, dt);
_camera.Update(dt);
```

- `Follow(target, dt)` moves the view towards the target smoothly; `FollowSharpness` sets how fast (0 snaps), and
  `DeadZone` lets the target move freely in the middle of the screen before the camera moves.
- `Bounds = _level.Area` keeps the view inside the garden: near an edge the camera stops and the gardener walks to the
  side of the screen.
- `Update(dt)` advances camera shake ([step 7](07-collisions.md)).
- `camera.WorldToScreen(p)` and `ScreenToWorld(p)` convert positions, for example to place a marker on the HUD over a
  world object or to know what the mouse points at.

The [camera example](../examples/focused.md#camera) moves a marker over a large world with every option visible.

## Next

The gardener still walks through hedges. [Step 7](07-collisions.md) stops him, and adds gems to collect and slimes to
avoid.
