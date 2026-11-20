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
factors would waste a fifth of a small screen. The frame is always whole window pixels: at a fractional scale each
side is the nearest whole size (480 × 270 in a 1366 × 768 window lands on 1365 × 768), centred, with the odd pixel of
letterbox on the right or at the bottom.

### HD canvases and smooth scaling

Pixel-perfect scaling is the default, not a rule: `GameSettings.Presentation` chooses how the canvas reaches the window, starting from one of two presets.

| `Presentation` | The frame | Sampling | For |
| --- | --- | --- | --- |
| `Presentation.PixelArt` (default) | whole factors when they fill enough of the window | nearest | pixel art: square, even pixels |
| `Presentation.HighResolution2D` | the largest size of the canvas's shape that fits | linear | HD art, photos, vector-style art, scalable text |

```csharp
public override GameSettings Settings { get; } = new("My HD Game", 1280, 720, 1)
{
    Presentation = Presentation.HighResolution2D,   // textures default to linear sampling too
};
```

An HD canvas is drawn at its own resolution and scaled to the window in one step, so a 1280×720 game fills a 1920×1080
or a 2560×1440 screen smoothly and shrinks into a smaller window. A window that would not fit the display opens at the
largest size that does, and HD canvases allow windows down to a quarter of their size. Captures (`--captures`) of a
smooth canvas are written at its size; pixel-art captures stay three times larger (`PlatformRequest.CaptureScale`).

### Presentation: one choice per field

A presentation states each choice separately (how the scale is
rounded, how the canvas is sampled on its way to the window, the textures' default filter, the initial window size, the
capture scale, camera snapping), and the two presets are just values of those fields:

| `Presentation` field | `Presentation.PixelArt` | `Presentation.HighResolution2D` |
| --- | --- | --- |
| `Rounding`: how the scale to the window is rounded | `IntegerUnlessWasteful`: whole factors unless they fill less than `WastefulThreshold` (80 %) of what fits | `Fractional`: the largest size that fits |
| `Aspect`: the window's other shape | `Keep` (letterbox) | `Keep` |
| `Upscale`: how the canvas is sampled when scaled | `SamplerDescription.PointClamp` (nearest) | `SamplerDescription.LinearClamp` |
| `Prescale`: the whole factor the canvas is enlarged by, with nearest sampling, before `Upscale` samples it | 1 (none) | 1 (none) |
| `DefaultSampler`: textures created without a filter | nearest | linear |
| `CaptureScale`: size factor of captures | 3 | 1 |
| `WholeFactorWindows`: windows open at whole factors | yes | no |
| `CameraPixelSnap`: cameras snap to whole pixels by default | yes | no |

Start from a preset and change only what differs; any combination is allowed:

```csharp
public override GameSettings Settings { get; } = new("My Game", 480, 270, 3)
{
    // whole-factor scaling, but the canvas and its textures sampled smoothly, captures at the canvas's size
    Presentation = Presentation.PixelArt with
    {
        Upscale = SamplerDescription.LinearClamp,
        DefaultSampler = SamplerDescription.LinearClamp,
        CaptureScale = 1,
    },
};
```

`Rounding` also has `Integer` (never fractional while a whole factor fits) and `Aspect` has `Stretch` (fill the whole
window, pixels no longer square) and `Expand`: the scale and square pixels stay, and the canvas grows
along the window's free side so a wider (or taller) window shows more of the world instead of letterbox
(`Presentation.CanvasSize`; the configured size is the minimum, and `IRenderer.Width`/`Height` report the canvas's current
size). `TextureFilter`, when set, still wins over `DefaultSampler`. The on-screen touch controls place the frame by the
same presentation (`FrameLayout.Fit` and `VirtualControls.Frame` take it), so touches land where the canvas is drawn. `SamplerDescription` lives in `MicroEngine.Graphics.Devices`.

`Prescale` is for pixel art that fills a window whose scale is not whole (a 480 × 270 game on a 1366 × 768 screen,
2.84×). Nearest sampling gives canvas pixels of two and three window pixels there, and linear sampling alone blurs the
picture. With a linear `Upscale` and `Prescale = Presentation.AutoPrescale` the canvas is first enlarged with nearest
sampling by the smallest whole factor that covers the frame, and that enlargement is sampled linearly: every canvas
pixel keeps the same size, with one blended window pixel where two meet. At a whole scale the picture is exactly the
nearest one, so the setting costs nothing where pixels were already even; at other scales it draws the canvas one more
time per frame.

```csharp
Presentation = Presentation.PixelArt with
{
    Rounding = ScaleRounding.Fractional,          // fill the window
    Upscale = SamplerDescription.LinearClamp,
    Prescale = Presentation.AutoPrescale,         // even pixels at any scale
},
```

A fixed factor (2, 3...) below the scale gives softer edges; a prescale changes nothing with a nearest `Upscale` or
on a canvas drawn smaller than its size. `Presentation.PrescaleFor` gives the factors a frame gets. The SDL and raylib
windows are composed this way, and a capture of the window (`IViewCapture.CaptureScreen`, the headless platform's
included) samples the same enlargement.

### Filtering: nearest or linear

Every texture has a `TextureFilter`:
`Nearest` keeps hard pixel edges, `Linear` blends neighbouring pixels when a texture is drawn scaled, rotated or between
pixels. The game's default (`GameSettings.TextureFilter`, or nearest for `Presentation.PixelArt` and linear for `Presentation.HighResolution2D`) applies
to every texture and atlas created without a filter of its own; each one can choose:

```csharp
var photo = Assets.Texture("backgrounds/sky.jpg", TextureFilter.Linear);
var tiles = Assets.Atlas("atlases/tiles.json", filter: TextureFilter.Nearest);   // crisp pixel tiles in an HD game
hero.Texture.Filter = TextureFilter.Nearest;                                       // changes at any time
var page = Graphics.CreateTexture(image, TextureFilter.Linear);
```

Glyph pages of TTF/OTF fonts take their filter from `FontOptions.Filter`; when that is null, they follow the game's
default like every other texture (the presentation's `DefaultSampler`), and `FontOptions.Pixel` chooses nearest itself. `ScalableFont.Filter`
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
    /// <summary>Draws with a blend state, a sampler and a material until PopState.</summary>
    void PushState(in DrawState state);
    void PopState();
    /// <summary>Draws into a render target instead of the canvas until PopTarget.</summary>
    void PushTarget(RenderTarget2D target);
    void PopTarget();
}
```

Source: [`src/MicroEngine.Graphics2D/Drawing/IGraphics.cs`](../../src/MicroEngine.Graphics2D/Drawing/IGraphics.cs)

- `DrawOptions` carries `FlipX`, `FlipY`, `Rotation` (radians), `Origin` (pivot in source pixels) and `Scale`.
- `BlendMode` is `Alpha`, `Additive` or `Multiply`.
- `PushCamera`/`PopCamera` must balance within a frame; both backends throw otherwise. So must `PushState`/`PopState`
  and `PushTarget`/`PopTarget`.
- `DrawState` is a blend state, a sampler and a material (`Material2D`, a fragment shader of the game's own); a part
  left out keeps what the renderer was drawing with. `DrawState.Opaque`, `Alpha`, `Additive` and `Multiply` are the four
  blend states every device has.
- `RenderTarget2D` is a picture to draw into, whose `Texture` is then drawn like any texture. Inside a target the
  renderer starts like a frame (the target's size, no camera, no clip, no state); the pop restores the canvas as it was.
- [Render targets and shaders](../guide/render-targets-and-shaders.md) shows both at work, with what a material draws
  on a device that runs no shaders.

Every backend draws by the same rules.

- Pixel (x, y) is the square from (x, y) to (x + 1, y + 1). A rectangle or an unrotated sprite covers exactly the
  pixels whose centre (x + 0.5, y + 0.5) lies inside it; a centre on its left or top edge counts as inside, one on its
  right or bottom edge as outside. A 1 × 1 rectangle at (2.5, 2.5) covers pixel (2, 2) only, on SDL and on raylib.
- Nearest sampling shows the texel under each pixel's centre; linear sampling mixes the four texels around it and
  stops at the texture's edge (it never wraps around to the opposite edge).
- `Clear` replaces every pixel of the canvas whatever the clip and blend mode, and replaces what was drawn before it;
  the clip still applies to the draws after it. `SetClip` truncates its rectangle to whole pixels.
- Blending: with source colour s (after the tint), its alpha a and canvas colour d, `Alpha` writes s·a + d·(1 − a),
  `Additive` s·a + d, `Multiply` s·a·d + d·(1 − a). The canvas is premultiplied (colours stored times their alpha), so
  `Multiply` with a translucent colour darkens in proportion to its opacity, a translucent `Clear` stores its colour
  times its alpha (what the window shows), and linear sampling of transparent edges does not darken them.
- Drawing a disposed texture throws `ObjectDisposedException`, and a texture created by another graphics device
  throws `ArgumentException`, on every backend; nothing is drawn. A render target drawn into itself throws
  `InvalidOperationException`.
- A material draws through its shader on a device that runs shaders (`sdl-gpu`) and with its declared fallback on
  every other, the headless platform included; its parameters are checked by name the same way on all of them.
  `Game.Graphics.SupportsShaders` says which.
- Captures are opaque (alpha 255) on every backend.
- Circles, lines and triangles cover their shape, but the pixels along their edges may differ between backends.

Extension methods in [`RendererExtensions.cs`](../../src/MicroEngine.Graphics2D/Drawing/RendererExtensions.cs) add
`Draw(sprite, position, tint?, flipX)` (places the sprite's origin at the position), `DrawPivoted` (rotate and
scale around the origin), `FillEllipse`, `DrawRect`, `FillGradient`, `DrawNineSlice` (an image stretched to any
size with its corners kept, from a `NineSlice`) and `Panel` (a bordered UI box from a `PanelStyle`, or its `Image`).

## Draw order

A renderer draws in the order of the calls: what is drawn later covers what was drawn before. A `SortedRenderer`
removes that dependency for the draws that need it. It is a renderer itself: between `Begin` and `End` it keeps every
draw with the layer and depth of that moment, and `End` sends them to the real renderer by **layer** (lower first),
then by **depth** (smaller first, so a larger depth covers), then in the order of the calls.

<!-- code: samples/Examples/Examples/DepthExample.cs#sorted -->
```csharp
var world = _sorted.Begin(r);      // a SortedRenderer the scene keeps: draws wait until End
DrawWorld(world, world);
_sorted.End();                     // sent by layer, then depth, then the order of the calls
```

Objects standing on the ground cover each other correctly with the Y of their feet as the depth: the walker goes
behind the trees below it and in front of those above, wherever the code draws it. Layers separate what never mixes
(shadows under everything that stands, markers over it):

<!-- code: samples/Examples/Examples/DepthExample.cs#world -->
```csharp
/// <summary>The same calls for both pictures; <paramref name="order"/> is null when nothing sorts them.</summary>
private void DrawWorld(IRenderer r, SortedRenderer? order)
{
    // The walker first and the trees after it: in call order every tree would cover the walker.
    order?.At(layer: 0, depth: Walker.Y);                       // the depth of an object on the ground: its feet
    r.Draw(_atlas["hero0"], Walker);
    foreach (var tree in Trees)
    {
        order?.At(layer: -1);                                   // shadows: under everything that stands
        r.FillCircle(tree + new Vector2(0, 1), 9, Color.Black.WithAlpha(0.25f));
        order?.At(layer: 0, depth: tree.Y);
        r.FillRect(new RectF(tree.X - 2, tree.Y - 14, 4, 14), Color.Hex("5d275d"));
        r.FillCircle(tree - new Vector2(0, 22), 13, Color.Hex("257179"));
        r.FillCircle(tree - new Vector2(4, 26), 5, Color.Hex("38b764"));
    }
    order?.At(layer: 1);                                        // over everything, wherever the walker is
    r.FillTriangle(Walker - new Vector2(3, 24), Walker - new Vector2(-3, 24), Walker - new Vector2(0, 19), Color.Hex("ffcd75"));
}
```

- Anything that draws into an `IRenderer` draws into a sorted one: sprites, fonts, tile renderers, particles. Set
  `Layer` and `Depth` (or `At(layer, depth)`) before the draws they apply to.
- Each draw keeps the camera, blend mode, clip and states (`PushState`) it was recorded under, whatever position it
  ends up in. A block starts with no camera, no clip and normal blending of its own; a camera, a state or a clip the
  renderer already has stays under it. A block that sets a clip leaves the renderer without one.
- `Clear`, `PushTarget` and `PopTarget` throw `InvalidOperationException` inside a block: a clear and a change of
  target have no place in an order. Draw into render targets with the renderer itself, before `Begin` or after `End`.
- The order is the same on every platform and every run: draws with equal layer and depth keep the order of the calls.
- Draws that end up next to each other with the same texture and state share a draw call, as always; sorting by depth
  can separate draws that call order kept together. Keep one `SortedRenderer` per scene and reuse it: it keeps its
  memory between frames.

## Post-processing

A `PostProcess` is a chain of full-screen effects over a picture. A scene's (`Scene.PostProcess`) covers what that
scene draws; the game's (`Game.PostProcess`) covers the whole canvas. Add effects and the engine does the rest: it
draws into a picture of the canvas's size, runs the effects in order and draws the result.

```csharp
public override void Enter() =>
    PostProcess.Add(new Bloom { Threshold = 0.6f, Strength = 1.2f }).Add(new Vignette { Strength = 0.6f });
```

- The ready effects are `ColorGrade`, `Blur`, `Bloom`, `Vignette`, `Scanlines`, `Crt` and `ShaderEffect` (a pass
  through a material of the game's own); a game writes others by deriving from `PostEffect`.
- A chain runs on every platform. Where the device runs no shaders (`Game.Graphics.SupportsShaders` is false: every
  device but `sdl-gpu`, browser builds included), `Vignette` and `Scanlines` draw the same picture, `ColorGrade`,
  `Blur`, `Bloom` and `Crt` draw a stated simpler form, and `ShaderEffect` is left out. `effect.SupportOn(Game.Graphics)`
  answers `Full`, `Reduced` or `None`.
- A chain with no enabled effect costs nothing. The chain owns the targets its effects need and the engine releases a
  scene's when the scene leaves the stack and the game's when the game ends.
- Post-processing only draws: it is not part of a scripted run's state.

[Post-processing](../guide/render-targets-and-shaders.md#post-processing) has the effects' values, what each draws
without shaders, and how to write an effect.

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
with trimming, pivots and Aseprite animation tags ([Assets](assets.md#atlases-from-packing-tools)).

The resulting `SpriteAtlas` returns `Sprite`s by name (`atlas["star"]`), lists `Names`, collects animation frames
with `Frames("walk")`, and owns the texture (`Dispose` it in `Unload`). A `Sprite` is a texture region plus its
origin. `Frames` returns a shared, read-only `IReadOnlyList<Sprite>`, collected once per name, so it can be
called while drawing. Duplicate names and unknown names throw.

## Animation

`AnimationClip(frames, framesPerSecond, loop = true)` is an immutable frame sequence; `FrameAt(time)` picks a
frame. `Animator` plays named clips: `Add(name, clip)`, `Play(name, restart = false)` (playing the current clip
again does not restart it), `Update(dt)`, `Frame`, `Speed`, `Finished` (for non-looping clips). Source:
[`src/MicroEngine.Graphics2D/Drawing/Animation.cs`](../../src/MicroEngine.Graphics2D/Drawing/Animation.cs). Games may equally pick
frames themselves from `atlas.Frames(...)` and a timer. Values that move through several keys, and moments the game
reacts to during a clip (a footstep, the frame a swing lands), are tracks and timelines: [Animation](../guide/animation.md).

## Camera

`Camera2D(viewWidth, viewHeight)` has a `Position` (world point at the top-left of the view) and supports:

- `Follow(target, dt)` with `FollowSharpness` (0 snaps) and a `DeadZone` around the view centre; `CenterOn(target)`.
- `Bounds`: keeps the view inside the world (centred when the world is smaller than the view).
- `Shake(trauma)`: trauma 0..1 decays over time; offset is trauma² × `MaxShake`. Call `Update(dt)` every step.
- `PixelSnap` (default on), `RenderOffset`, `WorldToScreen`, `ScreenToWorld`, `IsVisible(rect, margin)`.

Draw world content between `r.PushCamera(camera)` and `r.PopCamera()`; HUD drawn outside stays in screen space.

A camera can be drawn somewhere its rules do
not keep it. Set `DrawPosition` before drawing (for example to the camera's position interpolated between the last two
updates, a scene world's `DrawPosition2D` of an entity that follows the camera) and drawing reads it: `RenderOffset`,
`WorldToView`, `ViewToWorld`, `WorldToScreen`, `ScreenToWorld`, `VisibleBounds` and `IsVisible` use `DrawnPosition`
(`DrawPosition` when set, else `Position`). `Position`, `View`, `Center`, `Follow`, `CenterOn` and `Clamp` stay the
rules', so drawing never moves the camera the game simulates. `DrawPosition` stays until it is set again or set to null.

```csharp
public void Draw(IRenderer r, Stage stage, float time, float blend)
{
    stage.Camera.DrawPosition = stage.CameraDrawPosition;
    // ...
    var cam = stage.Camera;
    DrawBackdrop(r, stage, time);
    r.PushCamera(cam);
    DrawTiles(r, stage, time, background: true);
    // ...
    stage.Particles.Draw(r, blend);
    r.PopCamera();

    DrawLighting(r, stage, time);
    // ...
}
```

Source: [`games/MeadowRun/Scenes/StageView.cs`](../../games/MeadowRun/Scenes/StageView.cs)

The first line sets the camera's interpolated position for this frame; the rules keep `Camera.Position`, and the
parallax drawn by `DrawBackdrop` reads `cam.DrawnPosition`.

## Parallax backgrounds

`ParallaxLayer(texture)` is drawn in screen space (no camera pushed) with `Draw(r, cameraPosition, time)`.
Init properties: `Factor` (0 fixed, 1 moves with the world; default 0.5), `VerticalFactor`, `Y`, `Drift`
(pixels per second), `RepeatX` (default true), `Tint`. Source:
[`src/MicroEngine.Graphics2D/Drawing/Parallax.cs`](../../src/MicroEngine.Graphics2D/Drawing/Parallax.cs).

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

TrueType and OpenType fonts at any size are `ScalableFont`s, loaded with `Assets.Font`; both kinds
implement `IFont`.

Characters, measuring, wrapping and limits: [Text](text.md).

## Tile maps

`TileLayer` is a grid of integer ids (`Parse(rows, legend)` builds one from ASCII rows); `Autotile.Mask4` computes
a 4-neighbour mask for picking edge sprites. `TileRenderer.Draw(r, layer, tileSize, camera, spriteFor)` draws only
the visible cells and asks the game which sprite each id uses; id 0 is skipped.

```csharp
TileRenderer.Draw(r, layer, Tiles.Size, cam, TileLookup(stage));
// ...
private Func<int, int, int, Sprite?> TileLookup(Stage stage)
{
    if (_tileStage != stage || _tileFor is null)
    {
        string theme = stage.Def.Theme;
        var layer = stage.Def.Layer;
        _tileFor = (_, x, y) => Tiles.For(art, theme, layer, x, y);
        _tileStage = stage;
    }
    return _tileFor;
}
```

Source: [`games/NeonRift/Scenes/StageView.cs`](../../games/NeonRift/Scenes/StageView.cs)

The lookup is built once per stage, so drawing the map creates no new delegate each frame.

Collision over the same layers (`TileGrid`, `TileMotion`) is described in [Tile maps and collision](world.md).

## The world view and the UI view

A frame has two views. The **world view** is the canvas: drawn at
the canvas's resolution (`Scene.Draw`), with the scene's camera, and scaled into the window as the presentation says. The
**UI view** is drawn at the window's resolution over it (`Scene.DrawUi`), in UI units (window pixels divided by
the UI scale), so interface text and shapes stay sharp however small the canvas is: a 320 × 180
pixel-art world in a 1280 × 720 window gets its interface drawn on 1280 × 720 pixels.

The UI scale is `Presentation.UiScale`. Left unset (null, the default), it is the surface's pixel density: 1 on an
ordinary screen, 2 on a screen with twice the pixels per logical unit (a high-density laptop or phone), so the interface
keeps its physical size there; the headless platform's density is 1 (`HeadlessPlatform.PixelDensity` stands for another
screen in tests). A value overrides it: `UiScale = 1` draws one UI unit per window pixel on every screen, `UiScale = 2`
makes the interface twice as large. `Presentation.UiScaleFor(density)` gives the scale a surface gets, and
`Game.Views.UiScale` the one in use.

```csharp
public sealed class PlayScene : Scene
{
    public PlayScene(Game game) => WorldCamera = game.CreateCamera();     // pushed around Draw by the engine

    public override void Draw(IRenderer world) => DrawLevel(world);       // world coordinates, canvas pixels
    public override void DrawUi(IRenderer ui) =>                          // UI units at the window's resolution
        Game.Font.Draw(ui, $"SCORE {score}", new Vector2(16, 16), Color.White, scale: 2);
    public override bool UiContains(Vector2 uiPoint) => pauseButton.Contains(uiPoint);
    // ...
}
```

`Game.Views` maps positions between window pixels, canvas pixels and UI units (`CanvasToUi` places a label over a world
object), gives the pointer in the UI view (`UiPointer`) and in a camera's world (`WorldPointer(camera)`), and gives it to
the UI first: while it is over a visible scene's interface (`UiContains`, or its widgets, see [UI widgets](ui.md)),
`PointerOnUi` is true and the world pointer is not present. Transitions cover both views. The SDL, raylib and headless
platforms offer the UI view (`IScreenView`); a platform without one draws `DrawUi` into the canvas, where UI units are
canvas pixels. The UI view starts with the first thing a scene draws in it.

Captures (`--captures`) record what the player sees. A frame whose scenes drew an interface is captured as the window:
the letterbox, the world scaled into its frame with the presentation's filter and prescale, and the UI view over both,
at the window's size. A frame without one is captured as the canvas, at the presentation's capture scale. Tests that need the
world alone read it from the platform's `IViewCapture` capability (`CaptureWorld`, `SaveWorldScreenshot`), which also
gives the UI view (`CaptureUi`) and the composite (`CaptureScreen`); the headless platform draws its UI view when it
rasterizes.

## The 2D batcher and the camera

`IRenderer` is a façade over the 2D batcher (`Renderer2D`, in
`MicroEngine.Graphics2D.dll`), which runs on the [graphics device](#the-graphics-device) of the platform: a frame's
draws reach the GPU as one indexed draw per run of consecutive draws with the same texture, blend mode and clip, so
drawing from one atlas costs one draw call. `SdlPlatform.Canvas.Renderer.Statistics` and
`RaylibPlatform.Canvas.Renderer.Statistics` count the last frame's sprites, draw calls and flushes exactly.

A state pushed on `IRenderer` is the batcher's state, and a pushed render target is the batch's target: a frame that
uses them is still one batch, in the order its draws were made, and a frame that uses neither batches exactly as
before.

Code that needs more than `IRenderer` can use `Renderer2D` directly: a `Matrix3x2` transform per draw and a transform
stack, a state stack (blend state, sampler, scissor, material), sort modes (`Deferred`, `Texture`, `BackToFront`,
`FrontToBack`), a depth per draw, any render target, premultiplied colours and premultiplied blending by default.
Textures cross between the two levels both ways: `texture.Native` is the device texture behind a texture of the
drawing API, and `DeviceGraphics.Wrap` makes a device texture or render target one the drawing API draws.

`Camera2D` has `Zoom` (around the view's centre) and `Rotation` (radians, clockwise on screen); `WorldToView` is its
whole transform and `VisibleBounds` the world area it shows. `PixelSnap` rounds its translation to whole canvas
pixels; its default comes from the presentation (`Presentation.CameraPixelSnap`: on for `PixelArt`, off for
`HighResolution2D`). `Game.CreateCamera()` makes a canvas-sized camera with the game's defaults, and
`new Camera2D(width, height, presentation)` takes them from any presentation; each camera can still set `PixelSnap`.
`new Camera2D(width, height)` keeps the pixel-art default.

## The graphics device

Under the 2D drawing API sits one graphics device contract that
every backend implements, in the `MicroEngine.Graphics.Devices` namespace (`MicroEngine.Graphics.dll`, carried by the
`MicroEngine` package). Games do not need it to draw sprites, to draw into render targets or to draw through shaders
of their own; it is for engine modules, tools, backend authors and games that record their own passes.

```csharp
public interface IGraphicsDevice : IDisposable
{
    GraphicsCapabilities Capabilities { get; }   // tier, optional features, formats per usage, limits, API in use
    Texture CreateTexture(in TextureDescription description, ReadOnlySpan<byte> initialData = default);
    RenderTarget CreateRenderTarget(in RenderTargetDescription description);
    Sampler GetSampler(in SamplerDescription description);              // samplers are values, cached by the device
    GpuBuffer CreateBuffer(in BufferDescription description);           // vertex and index buffers, every tier
    Pipeline GetBuiltInPipeline(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat = null);
    CommandList BeginCommands();                                        // passes, bindings, draws, updates
    void Submit(CommandList commands);
    ReadbackRequest ReadPixels(RenderTarget target, RectI region);      // completed by a fence
    bool TryAcquireSurfaceTarget(out RenderTarget? target);             // false while minimised
    void Present();
    // plus CreateShader and CreatePipeline for devices that declare custom shaders, and the optional DeviceLost event
}
```

- **One contract, declared capabilities.** Devices differ only in `GraphicsCapabilities`: the tier, optional features
  (`RenderTargets`, `AddressRepeat`, `AddressMirror`, `Mipmaps`, `CustomBlend`, `Shaders`, `DepthBuffer`, `Instancing`,
  `SeparateFilters`), the formats each usage supports and limits that are guaranteed minimums. A request that needs
  something the device does not declare fails at once with `GraphicsCapabilityException`, which names the operation,
  the device and the missing feature or requirement; nothing degrades silently.
- **Premultiplied colour.** Colours and texel data are premultiplied (colour channels already multiplied by alpha), so
  linear filtering and scaling never darken the edges of transparent art. The four blend presets (`Opaque`,
  `AlphaPremultiplied`, `Additive`, `Multiply`) work on every device.
- **The built-in pipeline.** `BuiltInPipeline.Textured` draws `Vertex2D` triangles (position in target pixels, texture
  coordinate, premultiplied colour) with one texture; it needs no shader, so the same 2D code runs on every device.
- **Written rules, the same everywhere.** A triangle covers the pixels whose centre lies inside it (a centre on a top
  or left edge counts as inside); clears ignore viewport and scissor; the scissor is in target pixels; commands run in
  the order they were submitted and recorded; the window's target (`TryAcquireSurfaceTarget`) expires at `Present` and
  cannot be read back: captures render to a `RenderTarget`, read it back, then draw it to the window.
- **Resources.** Every resource belongs to the device that created it and has a `Handle` with a generation that stops
  resolving once the resource is disposed; the device releases it only when no submitted work uses it any more. Using
  a resource of another device, or a disposed one, throws when the command is recorded.
- **Counters.** `GraphicsDeviceBase.Counters` counts submitted lists, passes, draw calls, pipeline changes, texture
  bindings and uploaded bytes exactly, the same on every device, so tests can hold a scene to a draw-call budget.
- **The reference device.** `ReferenceGraphicsDevice` implements the contract on the CPU, rule by rule; it needs no
  window and draws identically on every machine.
- **The programmable device.** Carried by the `MicroEngine.Sdl` package, `SdlGpuDevice` (`MicroEngine.Graphics.SdlGpu`, device name `sdl-gpu`) implements the contract on SDL_GPU
  (Direct3D 12, Vulkan, Metal) at tier 1; a game draws with it by asking the SDL platform for it
  (`GraphicsSettings.Device = "sdl-gpu"` or `--graphics sdl-gpu`; see [Backends](backends.md)). It offers custom shaders compiled ahead of time, depth attachments, instancing and every blend state, as well
  as everything tier 0 offers. `Capabilities.ApiName` and `Capabilities.Adapter` say which API and adapter SDL chose,
  software Vulkan included; check them after creation rather than assuming the requested API. Depth formats are
  declared per adapter (some have no 24-bit depth): pick one that `Capabilities.Supports(format,
  TextureUsage.DepthStencil)` accepts. Matrices follow System.Numerics (right-handed `CreateLookAt` and
  `CreatePerspectiveFieldOfView`, uploaded as they are, `mul(M, v)` in HLSL), front faces are counter-clockwise and
  depth runs from 0 (near) to 1 (far).
- **Shaders compiled ahead of time.** Custom shaders are HLSL
  compiled by [`microengine shaders`](../guide/cli.md#shaders) into `.shader` files with code for every API and the
  names, places and sizes of their parameters. A game draws with one as a material (`Assets.Material(path)`), which
  falls back by declaration where shaders cannot run. At the device level, `Assets.Shader(path)` (or
  `CompiledShader.Parse(bytes)`) reads one and `compiled.Create(device)` creates it in the format the device runs (DXIL
  on Direct3D 12, SPIR-V on Vulkan, MSL on Metal); on a device without custom shaders that call throws
  `GraphicsCapabilityException`.

A backend derives from `GraphicsDeviceBase`, which validates every call before the backend sees it, so errors and the
moment they are raised are identical on every device; the backend implements only the members whose names end in
`Core`.

## The 3D path

In the repository only, not in a package yet (`MicroEngine.Graphics3D`, with the programmable device above; the
`samples/Slice3D` sample shows every piece together). It is a minimal path that proves 3D on the same device as 2D, not
a 3D renderer:

- **Meshes as assets.** `loaders.AddMeshes()` registers the `mesh` asset kind on an `AssetLoaderRegistry`: glTF 2.0
  files (`.gltf` with its buffers and images beside it or embedded, or `.glb`) become `MeshData` (positions, normals,
  the first texture coordinate, triangle indices and each primitive's base colour factor and PNG or JPEG texture), with
  the default scene's node transforms applied. Skins, animation, morph targets, other material properties, sparse data
  and required extensions are refused with an `InvalidDataException` naming them.
- **`Camera3D`**: position, target, up, vertical field of view, near and far planes; `View`, `Projection(aspect)` and
  `Project(point, width, height)` (where a world point lands in pixels) use the System.Numerics conventions above.
- **`Renderer3D`**: `Upload(mesh)` puts a mesh on the device; each frame is `Begin(target, camera)`, `Draw(mesh,
  world)` or `DrawWorld(world)`, then `End()`, one pass with depth testing and back faces culled. `DrawWorld` draws every
  entity with a `MeshRenderer` attachment at its interpolated world matrix, so a transform hierarchy moves smoothly
  between fixed steps. There is one material: the base colour texture times the base colour and a tint, lit by one
  directional light (`LightDirection`) with an `Ambient` term.
- **Targets.** `Begin` takes a target with colour in the renderer's format: one from `renderer.CreateTarget(width,
  height)` (colour plus a depth format the device supports), or one without depth, such as the game's canvas, when the
  renderer depth-tests against a depth texture of its own of the same size. The 2D batcher drawing into the same target
  afterwards, without a clear, lands over the 3D.
- **In a game.** A game on `sdl-gpu` creates the renderer on the device its textures live on
  (`((DeviceGraphics)Game.Graphics).Device`) and draws 3D from its scene's `Draw` between two flushes of the canvas:
  the scene's renderer is a `DeviceRenderer`, whose `Flush()` submits what the frame drew so far (the frame's clear
  first) and whose `Target` is the canvas. The scene's world (`Scene.Objects`) is stepped by the engine and drawn at the
  frame's blend; `DrawWorld(Objects)` draws its meshes there; 2D drawn after the 3D lands over it, and `DrawUi` draws the
  UI view at the window's resolution over both.
- On a device without custom shaders or depth, `new Renderer3D(device)` throws `GraphicsCapabilityException`: tier-0
  devices have no 3D path.

## Limitations

- Shaders run on the `sdl-gpu` device only; elsewhere a material draws with its fallback and a post-processing effect
  with a shader draws its simpler form or nothing. A material is a fragment shader; there is no stencil buffer and no
  multisampling.
- Images load as 32-bit RGBA textures without mipmaps. A device texture in another format (a single channel, a
  compressed format the device declares) can be created on the device and drawn through `DeviceGraphics.Wrap`.
- No sprite batching control beyond packing art into atlases. Draw order is call order unless a `SortedRenderer` orders the draws.
- Tile maps come from code (ASCII rows) or from the Tiled editor through the optional `MicroEngine.Tiled` package ([Maps from Tiled](world.md#maps-from-tiled)); there is no map editor.
- On Windows with NVIDIA's driver 32.0.16.1088, a process that creates and destroys many `sdl-gpu` devices on Vulkan in
  quick succession can, rarely, be ended by a fault inside the driver's own background thread. A game keeps one device
  for its whole run and is not affected; tools that open devices repeatedly can use Direct3D 12 on such machines.
