# GRAPHICS-2: 2D presentation, scaling and antialiasing

Issue #110. Task record: [`odd/tasks/graphics-2.md`](../../odd/tasks/graphics-2.md). Measured on 2026-11-13 on Windows 11,
NVIDIA GeForce RTX 4070 Ti SUPER, SDL_GPU on Direct3D 12, three 1920 × 1080 displays at scale 1.

**Result.** At 1080p full screen Neon Rift is drawn at exactly 4× with nearest sampling: every canvas pixel is a 4 × 4
block, as sharp as the art allows. The picture degrades at window sizes that are not a whole factor of the canvas (a
1366 × 768 laptop, a resized window, a phone): the choices were pixels of two different widths (nearest), a blurred
picture (linear) or a smaller picture with borders (whole factors). The missing general-purpose hook was a way to keep
even pixels at such a scale. It is implemented as `Presentation.Prescale`; antialiasing in the sense of MSAA or a
post-process filter would not have helped, and none is added.

## 1. What each demo declares

| Game | Canvas | Window | Presentation | At 1920 × 1080 | At 1366 × 768 | At 2560 × 1440 |
| --- | --- | --- | --- | --- | --- | --- |
| Neon Rift | 480 × 270 (`games/NeonRift/RiftGame.cs:64`) | 3× | `PixelArt` (default) | 4× exact | 2.84× fractional | 5×, 80 × 45 border |
| Meadow Run | 320 × 180 (default, `games/MeadowRun/MeadowGame.cs:62`) | 4× | `PixelArt` | 6× exact | 4×, 43 × 24 border | 8× exact |
| Courtyard Key | 320 × 180 (`games/CourtyardKey/CourtyardKeyGame.cs:44`) | 4× | `PixelArt` | 6× exact | 4×, border | 8× exact |
| Trail Duel | 320 × 180 (`games/TrailDuel/TrailDuelGame.cs:47`) | 4× | `PixelArt` | 6× exact | 4×, border | 8× exact |

`PixelArt` is whole factors unless the largest one fills less than 80 % of what fits (`ScaleRounding.IntegerUnlessWasteful`,
`src/MicroEngine.Graphics/Presentation.cs`), nearest for the canvas and for textures. For Neon Rift at 1366 × 768 the
whole factor (2×) fills 70 %, so the frame goes fractional at 2.84× with nearest sampling: canvas pixels are two or
three window pixels wide, which is the reported "overly pixelated" look (uneven strokes in text and outlines, shimmer
when the camera scrolls). The 320 × 180 games never meet it on common displays. The windowed/full-screen difference is
the same rule seen at two sizes: the window opens at a whole factor (`WholeFactorWindows`), full screen takes the
display's size.

The SDL window is created with `SDL_WINDOW_HIGH_PIXEL_DENSITY` (`src/MicroEngine.Sdl/SdlPlatform.cs:182`), so the
system does not stretch the picture on a scaled display; no blur comes from there.

## 2. What the layers already allow

- **Internal resolution**: the canvas is any size (`GameSettings.VirtualWidth/Height`); `AspectMode.Expand` grows it
  with the window.
- **Scale rounding**: `Fractional`, `Integer`, `IntegerUnlessWasteful` with its threshold.
- **Upscale filter**: `Presentation.Upscale` is any `SamplerDescription` (nearest or linear), independent of the
  textures' default sampler (`DefaultSampler`) and of each texture's own filter.
- **Hybrid resolution**: the UI view is drawn at the window's resolution over the world (`Presentation.UiScale`,
  `Scene.DrawUi`), and `Game.Views.CanvasToUi` maps a canvas position into it
  (`src/MicroEngine/Core/GameViews.cs:76`), so text, a HUD or an effect can be drawn at full resolution over a
  low-resolution world today.
- **Device**: render targets that can be sampled, samplers as values, `ReadPixels`; the 2D batcher draws with the
  built-in textured pipeline only (`src/MicroEngine.Graphics2D/Renderer2D.cs`, `IRenderer2D` remarks), and
  `RenderTargetDescription` has no sample count.

## 3. Comparison on Neon Rift

One frame of stage 1 (`auto0750` of the autopilot tour) composed into windows of four sizes with each option, on the
CPU reference device through the engine's own presenter, and the same options on the real window (Direct3D 12) at
1366 × 768, captured from the screen. Crops of the HUD text and of a drone were inspected at 3× enlargement.

| Option | 1920 × 1080 (4×) | 1366 × 768 (2.84×) | Window pixels holding an exact canvas colour at 1366 × 768 |
| --- | --- | --- | --- |
| `PixelArt` (default) | even 4 × 4 blocks | pixels 2 or 3 wide: uneven strokes | 99.9 % |
| `Rounding = Integer` | same | 2×: even, 960 × 540 in the middle of the window (half its area) | 100 % |
| `Rounding = Fractional`, nearest | same | same as the default here | 99.9 % |
| `Rounding = Fractional`, `Upscale` linear | **blurred** (79 % of the window's pixels differ from nearest) | blurred | 24 % |
| the same with `Prescale = AutoPrescale` | **identical to nearest, byte for byte** | even pixels, one blended pixel at each edge | 71 % (79 % at 1600 × 900, 87 % at 2560 × 1440) |
| the same with `Prescale = 2` | edges blended over 2 pixels | softer than auto | |

Observations:

- A linear `Upscale` alone blurs at every scale, whole ones included. It suits high-resolution art, not a small canvas.
- The prescale is exactly nearest whenever the scale is whole, so a pixel-art game loses nothing by choosing it; it
  only changes the pictures that were uneven.
- Whole factors stay the crispest choice when the border is acceptable; that is a game's decision, and it is already a
  field.

Frame time, Neon Rift stage 1 under the autopilot, 1200 updates, no VSync, no limit (interval between frames):

| | p50 | p95 | p99 |
| --- | --- | --- | --- |
| 1920 × 1080, `PixelArt` | 1.72 ms | 2.91 ms | 3.69 ms |
| 1920 × 1080, linear | 1.82 ms | 2.72 ms | 3.51 ms |
| 1920 × 1080, linear + auto prescale (no extra pass: the scale is whole) | 1.82 ms | 2.62 ms | 3.39 ms |
| 1366 × 768, `PixelArt` | 1.48 ms | 2.07 ms | 2.40 ms |
| 1366 × 768, fractional nearest | 1.52 ms | 2.21 ms | 2.72 ms |
| 1366 × 768, linear | 1.46 ms | 2.15 ms | 2.64 ms |
| 1366 × 768, linear + auto prescale (one extra pass into 1440 × 810) | 2.07 ms | 2.98 ms | 3.49 ms |

The extra pass costs about 0.6 ms a frame on this machine, uncapped; the frame stays under a fifth of a 60 Hz budget.
The window runs include about two seconds at the window's opening size (3×) before the resize. A slow integrated GPU
or a phone has not been measured.

### Reproducing

```bash
# The canvas captures (scale 3), then the options on the real window:
RIFT_START=stage RIFT_AUTO=1 RIFT_GOD=1 dotnet run --project games/NeonRift -c Release -- --script games/NeonRift/scripts/autotour.script --hidden --mute --captures captures
RIFT_PRESENT=sharp RIFT_START=stage RIFT_AUTO=1 RIFT_GOD=1 dotnet run --project games/NeonRift -c Release -- --window fullscreen --mute --frames 1200 --vsync off --fps-limit 0 --graphics sdl-gpu --profile sharp.json
```

`RIFT_PRESENT` is `integer`, `nearest`, `smooth` or `sharp` (unset: the game's pixel art). Resize the window to a size
that is not a multiple of 480 × 270 to see the difference. The compositions at other window sizes come from
`CanvasPresenter` on `ReferenceGraphicsDevice` with a `HostSurface` of that size, as
`tests/MicroEngine.Graphics.Tests/CanvasPresenterTests.cs` does for one row.

## 4. What was missing, and what is implemented

**Implemented: `Presentation.Prescale`** (`src/MicroEngine.Graphics/Presentation.cs`). The whole factor the canvas is
enlarged by, with nearest sampling, before `Upscale` samples it: 1 by default (nothing changes for any existing game),
a fixed factor, or `Presentation.AutoPrescale`, the smallest factor that covers the frame, limited to the device's
largest render target. `Presentation.PrescaleFor` gives the factors of a frame. `CanvasPresenter`
(`src/MicroEngine.Graphics2D/CanvasPresenter.cs`) composes the window: letterbox, canvas (through the enlargement when
there is one), UI view. It needs no shader, so it runs on the SDL renderer device and on SDL_GPU alike. The SDL
platform (desktop, browser, phones) uses it when a prescale is set, and its capture of a frame with a UI samples the
same enlargement.

```csharp
Presentation = Presentation.PixelArt with
{
    Rounding = ScaleRounding.Fractional,          // fill the window
    Upscale = SamplerDescription.LinearClamp,
    Prescale = Presentation.AutoPrescale,          // even pixels, antialiased edges; exact at whole scales
},
```

**Not implemented, each a decision for the owner** (they are outside this change's edit surface or change more than a
presentation field):

1. *The other composers ignore the prescale.* `DeviceCanvas.Compose` (`src/MicroEngine/Graphics/DeviceCanvas.cs:414`),
   the headless platform's `ScreenCapture.Compose` (`src/MicroEngine/Core/ViewCapture.cs:37`) and the raylib backend
   still sample the canvas directly. `DeviceCanvas.Compose` should delegate to `CanvasPresenter`, and
   `ScreenCapture.Compose` should take the presentation, so every backend and the headless screen captures agree. The
   manual's presentation table (`docs/manual/rendering.md`) needs the new row.
2. *Neon Rift's default.* Making `sharp` the game's presentation would improve it on displays that are not a multiple
   of 480 × 270 and change nothing on 1080p, 1440p-with-border excepted (it would fill the window instead). It changes
   a demo's default visuals, so it is left as `RIFT_PRESENT`.
3. *A canvas rendered above its size.* Drawing the 480 × 270 world into a target N times larger, with the game's
   units unchanged, would smooth rotated and scaled sprites, lines and sub-pixel motion (the "internal resolution"
   lever). `DeviceRenderer` already has the unit scale the UI view uses; the hook would be a `Presentation` field read
   by `DeviceCanvas`, plus the pointer and capture mappings. It also breaks the pixel grid (mixed pixel sizes), which
   Neon Rift's art does not want; no evidence here asks for it.
4. *A custom shader on the 2D batcher or on the present pass.* It would open CRT, scanline or xBR-style filters to
   games without building any into the engine. It belongs with the 2D material work (GRAPHICS-1), not with
   presentation fields.
5. *Fractional frames are not snapped to whole window pixels.* `Presentation.Fit` returns 1365.33 × 768 for
   480 × 270 in 1366 × 768, so the last column mixes with the letterbox (the 0.1 % above). Rounding the frame would
   change every game's layout by up to a pixel.

MSAA is not on the list: sprites are axis-aligned textured quads whose edges are inside the textures, where
multisampling does nothing.
