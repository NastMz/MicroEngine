# Graphics

Scenes draw in immediate mode into a fixed-size virtual canvas (320×180 by default, or an HD canvas such as 1280×720)
through the `IRenderer` interface. Art is loaded from files ([Assets](assets.md)) or prepared on the CPU as
`PixelImage`s, packed into textures with `SpriteAtlasBuilder` or by a packing tool, and drawn as `Sprite`s. On top of that the engine provides animation clips, a 2D camera, parallax layers,
particles, a light map, a built-in pixel font and a tile renderer. All of it is in the `MicroEngine.Graphics`
and `MicroEngine.World` namespaces and never references the backend.

## The virtual canvas

Games draw in virtual pixels: `GameSettings.VirtualWidth × VirtualHeight` ([Lifecycle](lifecycle.md#gamesettings)).
Both platforms (SDL 3 and raylib) render into a texture of exactly that size, then scales it to the window by the largest
whole factor that fits and letterboxes the rest, so pixels stay square. When the window or browser iframe is
smaller than the canvas it scales down (never crops). On desktop F11 or Alt+Enter toggles borderless
fullscreen; in a browser the page owns fullscreen ([Embedding](../examples/embedding.md#the-iframe)).

The rule is shared by every platform (`FrameLayout.Fit`): the largest whole factor, or a fractional one when whole
factors would waste a fifth of a small screen.

### HD canvases and smooth scaling

Next SDK version (not published yet: arrives with the next SDK version). Pixel-perfect scaling is the default, not a
rule: `GameSettings.Scaling` chooses how the canvas reaches the window.

| `Scaling` | The frame | Sampling | For |
| --- | --- | --- | --- |
| `CanvasScaling.Pixel` (default) | whole factors when they fill enough of the window | nearest | pixel art: square, even pixels |
| `CanvasScaling.Smooth` | the largest size of the canvas's shape that fits | linear | HD art, photos, vector-style art, scalable text |

```csharp
public override GameSettings Settings { get; } = new("My HD Game", 1280, 720, 1)
{
    Scaling = CanvasScaling.Smooth,          // textures default to linear sampling too
};
```

An HD canvas is drawn at its own resolution and scaled to the window in one step, so a 1280×720 game fills a 1920×1080
or a 2560×1440 screen smoothly and shrinks into a smaller window. A window that would not fit the display opens at the
largest size that does, and HD canvases allow windows down to a quarter of their size. Captures (`--captures`) of a
smooth canvas are written at its size; pixel-art captures stay three times larger (`PlatformRequest.CaptureScale`).

### Filtering: nearest or linear

Next SDK version (not published yet: arrives with the next SDK version). Every texture has a `TextureFilter`:
`Nearest` keeps hard pixel edges, `Linear` blends neighbouring pixels when a texture is drawn scaled, rotated or between
pixels. The game's default (`GameSettings.TextureFilter`, or nearest for `Pixel` scaling and linear for `Smooth`) applies
to every texture and atlas created without a filter of its own; each one can choose:

```csharp
var photo = Assets.Texture("backgrounds/sky.jpg", TextureFilter.Linear);
var tiles = Assets.Atlas("atlases/tiles.json", filter: TextureFilter.Nearest);   // crisp pixel tiles in an HD game
hero.Texture.Filter = TextureFilter.Nearest;                                       // changes at any time
var page = Graphics.CreateTexture(image, TextureFilter.Linear);
```

Glyph pages of TTF/OTF fonts do not follow the game's default: their filter is `FontOptions.Filter`, or, when that is
null, linear for antialiased text (`FontOptions.Antialiasing`, the default) and nearest otherwise; `ScalableFont.Filter`
changes it later. The built-in pixel font is always nearest. Atlases packed at run time and sampled linearly get extruded edges, so
neighbouring sprites never bleed into each other. Linear sampling does not replace art made for the size it is shown
at: there are no mipmaps, so drawing a large image much smaller than its pixels shimmers; keep art within about twice
the size it is drawn at.

`Texture.Update(x, y, width, height, pixels)` replaces a rectangle of a texture (glyph pages use it), and
`Texture.SizeInBytes` gives its device memory.

## `IRenderer`

```csharp
public interface IRenderer
{
    int Width { get; }
    int Height { get; }
    void Clear(Color color);
    void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default);
    void DrawStretched(Texture texture, RectF source, RectF destination, Color tint);
    void FillRect(RectF rect, Color color);
    void FillCircle(Vector2 center, float radius, Color color);
    void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
    void DrawLine(Vector2 from, Vector2 to, float thickness, Color color);
    void PushCamera(Camera2D camera);
    void PopCamera();
    void SetBlendMode(BlendMode mode);
    /// <summary>Restricts drawing to a screen-space rectangle; pass null to remove.</summary>
    void SetClip(RectF? screenRect);
}
```

Source: [`src/MicroEngine/Graphics/IGraphics.cs`](../../src/MicroEngine/Graphics/IGraphics.cs)

- `DrawOptions` carries `FlipX`, `FlipY`, `Rotation` (radians), `Origin` (pivot in source pixels) and `Scale`.
- `BlendMode` is `Alpha`, `Additive` or `Multiply`.
- `PushCamera`/`PopCamera` must balance within a frame; both backends throw otherwise.

Extension methods in [`RendererExtensions.cs`](../../src/MicroEngine/Graphics/RendererExtensions.cs) add
`Draw(sprite, position, tint?, flipX)` (places the sprite's origin at the position), `DrawPivoted` (rotate and
scale around the origin), `FillEllipse`, `DrawRect`, `FillGradient` and `Panel` (bordered UI box from a
`PanelStyle`).

## Art in code: `PixelImage` and `Art`

`PixelImage` is a CPU-side RGBA image. It is how games author sprites procedurally and how loaded PNGs are
represented ([Assets](assets.md)).

| Group | Members |
| --- | --- |
| Create | `new PixelImage(w, h)`, `FromMap(palette, rows)`, `FromPng(bytes)`, `Load(path)`, `Clone()` |
| Paint | `Clear`, `FillRect`, `DrawRect`, `Line`, `FillCircle`, `FillEllipse`, `Paint`, `Blit`, `Blend`, indexer |
| Transform (copies) | `FlipX`, `FlipY`, `Rotate90`, `Rotated(radians, pivotX, pivotY)`, `Scale2x`, `Crop`, `Silhouette` |
| Edit in place | `Outline`, `Recolor`, `Map`, `ClearRect` |
| Query | `Width`, `Height`, `Pixels`, `OpaqueBounds()`, `ToRgba()` |

`Art` holds deterministic helpers: `Hash`, `ValueNoise`, `Dither` (Bayer 4×4), `Ramp` (palette ramp with
dithered steps) and `Palette("k=#rrggbb", ...)`.

```csharp
var palette = Art.Palette("o=#000000", "y=#ffec27", "w=#fff1e8");
var star = PixelImage.FromMap(palette,
    "....o....",
    "...oyo...",
    "ooooyoooo",
    "oyyywyyyo",
    ".oyyyyyo.",
    "..oyyyo..",
    ".oyyoyyo.",
    "oyyo.oyyo",
    "ooo...ooo");
Atlas = new SpriteAtlasBuilder().Add("star", star, new Vector2(4.5f, 4.5f)).Build(Graphics);
```

Source: [`samples/HelloEngine/Program.cs`](../../samples/HelloEngine/Program.cs)

## Atlases and sprites

`SpriteAtlasBuilder` collects named images and packs them into one power-of-two texture (shelf packing, 1 px
padding) when you call `Build(Graphics)`:

| Method | Adds |
| --- | --- |
| `Add(name, image, origin?)` | One sprite; `origin` is its pivot (for example the feet) |
| `AddFrames(name, frames, origin?)` | `name0`, `name1`, ... sharing one origin |
| `AddGrid(name, sheet, cellWidth, cellHeight, count?, origin?)` | Slices a row-major sheet into `name0..N` |

Atlases made by TexturePacker, Aseprite, ShoeBox and similar tools load with `Assets.Atlas` or `SpriteAtlas.Load`,
with trimming, pivots and Aseprite animation tags ([Assets](assets.md#atlases-from-packing-tools), next SDK version).

The resulting `SpriteAtlas` returns `Sprite`s by name (`atlas["star"]`), lists `Names`, collects animation frames
with `Frames("walk")`, and owns the texture (`Dispose` it in `Unload`). A `Sprite` is a texture region plus its
origin. Next SDK version (not published yet: arrives with the next SDK version): `Frames` returns a shared, read-only `IReadOnlyList<Sprite>`, collected once per name, so it can be
called while drawing. Duplicate names and unknown names throw.

## Animation

`AnimationClip(frames, framesPerSecond, loop = true)` is an immutable frame sequence; `FrameAt(time)` picks a
frame. `Animator` plays named clips: `Add(name, clip)`, `Play(name, restart = false)` (playing the current clip
again does not restart it), `Update(dt)`, `Frame`, `Speed`, `Finished` (for non-looping clips). Source:
[`src/MicroEngine/Graphics/Animation.cs`](../../src/MicroEngine/Graphics/Animation.cs). Games may equally pick
frames themselves from `atlas.Frames(...)` and a timer.

## Camera

`Camera2D(viewWidth, viewHeight)` has a `Position` (world point at the top-left of the view) and supports:

- `Follow(target, dt)` with `FollowSharpness` (0 snaps) and a `DeadZone` around the view centre; `CenterOn(target)`.
- `Bounds`: keeps the view inside the world (centred when the world is smaller than the view).
- `Shake(trauma)`: trauma 0..1 decays over time; offset is trauma² × `MaxShake`. Call `Update(dt)` every step.
- `PixelSnap` (default on), `RenderOffset`, `WorldToScreen`, `ScreenToWorld`, `IsVisible(rect, margin)`.

Draw world content between `r.PushCamera(camera)` and `r.PopCamera()`; HUD drawn outside stays in screen space.

```csharp
public void Draw(IRenderer r, Stage stage, float time)
{
    var cam = stage.Camera;
    DrawBackdrop(r, stage, time);
    r.PushCamera(cam);
    DrawTiles(r, stage, time, background: true);
    // ...
    stage.Particles.Draw(r);
    r.PopCamera();

    DrawLighting(r, stage, time);
    // ...
}
```

Source: [`games/MeadowRun/Scenes/StageView.cs`](../../games/MeadowRun/Scenes/StageView.cs)

## Parallax backgrounds

`ParallaxLayer(texture)` is drawn in screen space (no camera pushed) with `Draw(r, cameraPosition, time)`.
Init properties: `Factor` (0 fixed, 1 moves with the world; default 0.5), `VerticalFactor`, `Y`, `Drift`
(pixels per second), `RepeatX` (default true), `Tint`. Source:
[`src/MicroEngine/Graphics/Parallax.cs`](../../src/MicroEngine/Graphics/Parallax.cs).

## Particles

`ParticleSystem(capacity = 512, seed = 3)` is a fixed-size pool. `Emit(at, spec, count, inheritVelocity)` spawns
particles described by a `ParticleSpec` (start/end colour, life, speed and angle ranges, size, gravity, drag,
round or square, spread). Call `Update(dt)` and `Draw(r)`; set `Additive = true` for glows. Emits beyond capacity
are dropped.

```csharp
_particles.Emit(_position, new ParticleSpec { StartColor = Color.Hex("ffec27"), EndColor = Color.Hex("ff004d00"), Gravity = 80 }, 24);
```

Source: [`samples/HelloEngine/Program.cs`](../../samples/HelloEngine/Program.cs)

## Lighting

`LightMap(device, glowImage?)` darkens the finished scene and adds point lights. Each frame: `Clear()`, `Add(position,
radius, brightness, glow?, glowRadius?)` per light, then `Draw(r, camera)` or `Draw(r, cameraTopLeft, screenArea)`.
Darkness is a coarse grid (`CellSize`, default 4 px) quantised to `Levels` bands and multiplied toward `Shade`;
glows are additive halos scaled from one texture. `BrightnessAt(world)` exposes the value for game rules.
Courtyard Key fades the darkness per room:

```csharp
Light.Clear();
float darkness = Room.Def.Darkness;
// ...
Light.Darkness = darkness;
if (darkness > 0.01f)
    foreach (var a in _drawList) a.EmitLight(Light);
// Halos fade with the darkness so torches read as light, not as stickers, in lit rooms.
Light.GlowIntensity = 0.3f * darkness + 0.06f;
if (darkness > 0.01f) Light.Draw(r, -Camera.RenderOffset, WorldView);
```

Source: [`games/CourtyardKey/Scenes/PlayScene.Draw.cs`](../../games/CourtyardKey/Scenes/PlayScene.Draw.cs)

## Text: `BitmapFont`

Every game gets `Game.Font`, the built-in proportional "Micro 5x7" face authored in code. It offers `Draw`,
`DrawShadowed`, `DrawOutlined`, `DrawCentered`, `Measure`, `MeasureWidth` and greedy `Wrap(text, maxWidth)`, all
with an optional `scale`. Unknown characters render as `?`.

TrueType and OpenType fonts at any size are `ScalableFont`s, loaded with `Assets.Font` (next SDK version); both kinds
implement `IFont`.

Characters, measuring, wrapping and limits: [Text](text.md).

## Tile maps

`TileLayer` is a grid of integer ids (`Parse(rows, legend)` builds one from ASCII rows); `Autotile.Mask4` computes
a 4-neighbour mask for picking edge sprites. `TileRenderer.Draw(r, layer, tileSize, camera, spriteFor)` draws only
the visible cells and asks the game which sprite each id uses; id 0 is skipped.

```csharp
TileRenderer.Draw(r, layer, Tiles.Size, cam, (_, x, y) => Tiles.For(art, theme, layer, x, y));
```

Source: [`games/NeonRift/Scenes/StageView.cs`](../../games/NeonRift/Scenes/StageView.cs)

Collision over the same layers (`TileGrid`, `TileMotion`) is described in [Tile maps and collision](world.md).

## Limitations

- No shaders, render targets or post-processing are exposed through `IRenderer`.
- No mipmaps or compressed GPU texture formats; textures are 32-bit RGBA.
- No sprite batching control beyond packing art into atlases; draw order is call order.
- Tile maps come from code (ASCII rows); there is no map editor or Tiled importer.
