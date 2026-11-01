# 3. Drawing the hero

Part 2 of the tutorial puts the gardener on screen, makes him walk and animates him. This step covers drawing: shapes,
images made in code, a sprite sheet loaded from a PNG file, and the sprite atlas that holds them.

## Two kinds of art

MicroEngine draws pixel art from two sources, and a game may mix them freely:

- **Files**: PNG images in the project's `Content/` folder, loaded with `ContentFolder`. Gem Garden's hero is a file.
- **Code**: `PixelImage`, an image in memory you paint with C# (pixel maps, rectangles, noise, dithering). Gem
  Garden's grass, hedges, gems and slimes are code.

Either way, images become **sprites** in a **sprite atlas**: one texture on the graphics card holding many images, each
with a name and an origin.

## The hero's sprite sheet

Download [`hero.png`](../../samples/GemGarden/part2-hero/Content/hero.png) and save it over the template's
`Content/hero.png`. It is a 64x16 strip of four 16x16 frames of the gardener walking:

![The hero sprite sheet: four frames of a gardener in a green hat and blue overalls, side by side](../../samples/GemGarden/part2-hero/Content/hero.png)

Any PNG works the same way: 8-bit RGB or RGBA, greyscale or paletted, not interlaced ([Assets](../manual/assets.md)).
Because the project file copies `Content/**` next to the executable, the game finds the file at run time.

## Grass made in code

Create `GardenArt.cs`, the home of every image the game paints in code:

<!-- code: samples/GemGarden/part2-hero/GardenArt.cs -->
```csharp title="GardenArt.cs"
using MicroEngine;
using MicroEngine.Graphics;

namespace GemGarden;

/// <summary>The art drawn in code. The hero comes from Content/hero.png.</summary>
public static class GardenArt
{
    // One character per colour, used by the pixel maps and the tiles.
    private static readonly Dictionary<char, Color> Palette = Art.Palette("o=#1a1c2c", "g=#38b764", "d=#257179");

    /// <summary>Grass: two greens mixed by value noise and an ordered dither.</summary>
    public static PixelImage Grass() => new PixelImage(16, 16)
        .Map((x, y, _) => Art.Dither(x, y, Art.ValueNoise(x, y, 6) * 0.45f) ? Palette['d'] : Palette['g']);
}
```

`PixelImage.Map` sets every pixel from a function of its position. `Art.ValueNoise` gives smooth random values between
0 and 1, and `Art.Dither` turns a value into an ordered (Bayer) pattern, the classic way pixel art mixes two colours.
`Art.Palette` maps characters to colours; step 7 uses it to draw gems from text rows.

## The atlas

In `MainGame.cs`, add a property for the atlas and build it in `Load`:

<!-- code: samples/GemGarden/part2-hero/MainGame.cs#load -->
```csharp title="MainGame.cs"
protected override void Load()
{
    var content = new ContentFolder();
    Sprites = new SpriteAtlasBuilder()
        .AddGrid("hero", content.Image("hero.png"), 16, 16, origin: new Vector2(8, 16))
        .Add("grass", GardenArt.Grass())
        .Build(Graphics);

    Scenes.Push(new TitleScene(this));
}

protected override void Unload() => Sprites.Dispose();
```

with the property next to `Settings`:

```csharp
public SpriteAtlas Sprites { get; private set; } = null!;
```

- `new ContentFolder()` points at the `Content` folder next to the executable; `content.Image("hero.png")` decodes the
  PNG into a `PixelImage`. A missing file fails here with its full path in the message.
- `AddGrid("hero", sheet, 16, 16, ...)` cuts the sheet into 16x16 cells, left to right and top to bottom, named
  `hero0`, `hero1`, `hero2`, `hero3`.
- The **origin** is the point of the sprite placed at the position you draw it at. `(8, 16)` is the middle of the
  bottom edge: the gardener's feet. Drawing him at a position means "his feet are here", which makes sorting, shadows
  and collisions at the feet natural.
- `Build(Graphics)` packs everything into one texture. `Graphics` (the game's graphics device) exists from `Load` on,
  which is why art is built there and not in a constructor.
- The atlas owns its texture; `Unload` releases it when the game ends.

`Load` needs `using System.Numerics;` (for `Vector2`), `using MicroEngine.Core;` (for `ContentFolder`) and
`using MicroEngine.Graphics;` at the top of the file.

## Drawing

The play scene now draws a lawn and the hero. Its `Draw`:

<!-- code: samples/GemGarden/part2-hero/Scenes/PlayScene.cs#draw -->
```csharp title="Scenes/PlayScene.cs"
public override void Draw(IRenderer r)
{
    // The same 16x16 grass sprite, drawn over the whole screen.
    var grass = _game.Sprites["grass"];
    for (int y = 0; y < Game.Height; y += grass.Height)
    for (int x = 0; x < Game.Width; x += grass.Width)
        r.Draw(grass, new Vector2(x, y));

    _player.Draw(r);
    Font.DrawShadowed(r, "MOVE: ARROWS, WASD, STICK   RUN: SHIFT, X, B", new Vector2(4, 4), Color.White, Color.Black);
}
```

`Sprites["grass"]` looks a sprite up by name (an unknown name throws, so typos fail at once). `r.Draw(sprite, position)`
places the sprite's origin at the position; the grass's origin is its top-left corner, the default.

The hero draws himself, in a `Player` class you write in the next two steps. Its `Draw` method:

<!-- code: samples/GemGarden/part2-hero/World/Player.cs#draw -->
```csharp title="World/Player.cs"
public void Draw(IRenderer r)
{
    r.FillEllipse(Position, 6, 2, Color.Black.WithAlpha(0.3f));   // a soft shadow under the feet
    r.Draw(_animator.Frame, Position, flipX: _facingLeft);
}
```

First a shape, then a sprite. The renderer draws in the order you call it, so the shadow is under the hero. Shapes need no
image at all:

| Call | Draws |
| --- | --- |
| `r.FillRect(rect, color)`, `r.DrawRect(rect, color, thickness)` | filled or outlined rectangles |
| `r.FillCircle(center, radius, color)`, `r.FillEllipse(center, rx, ry, color)` | circles and ellipses |
| `r.FillTriangle(a, b, c, color)`, `r.DrawLine(from, to, thickness, color)` | triangles and lines |
| `r.FillGradient(rect, top, bottom, bands)` | a vertical gradient, optionally banded |
| `r.Draw(sprite, position, tint, flipX)` | a sprite, optionally tinted or mirrored |
| `r.DrawPivoted(sprite, pivot, rotation, scale)` | a sprite rotated and scaled around its origin |

`Color.Black.WithAlpha(0.3f)` is 30 % opaque black. `flipX` mirrors the sprite, so one set of frames serves both
directions. `_animator.Frame` is the current animation frame: [step 5](05-animation.md) explains it.

More on drawing, including textures, blend modes and clipping: [Graphics](../manual/rendering.md) and the
[shapes and sprites examples](../examples/focused.md#shapes-and-textures).

## Next

The hero is drawn but cannot move. [Step 4](04-input.md) reads the player's input.
