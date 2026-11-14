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
device canvas (`DeviceCanvas.Compose`), which the SDL and raylib platforms present with, composes through it when a
prescale is set, and `ScreenCapture.Compose` takes the presentation, so the captures of the window on the SDL, raylib
and headless platforms sample the same enlargement. Without a prescale each of those paths runs the code it ran
before (§5).

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

1. *Done in the follow-up (§5):* the other composers apply the prescale.
2. *Neon Rift's default (kept, by the owner's decision of 2026-11-13).* Making `sharp` the game's presentation would improve it on displays that are not a multiple
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
5. *Done in the follow-up (§5):* `Presentation.Fit` gives whole-pixel frames. The sentence this item had ("the last
   column mixes with the letterbox") was wrong: nothing is blended, see §5.

MSAA is not on the list: sprites are axis-aligned textured quads whose edges are inside the textures, where
multisampling does nothing.

## 5. Follow-up of 2026-11-13: real captures, `Fit`, the prescale everywhere

Each finding is labelled: **demonstrated failure** (reproduced, with a test or a capture), **hypothesis** (reasoned,
not reproduced) or **user-perceptible difference** (what someone looking at the screen can see).

### 5.1 Captures of the presented picture

Six pictures of one Neon Rift frame, read back from the SDL_GPU device (Direct3D 12, RTX 4070 Ti SUPER): the device's
surface texture after the frame was composed, which `Present` copies to the swapchain with a nearest copy of the same
size and format (`SdlGpuDevice.PresentCore`). They are the bytes sent to the window, not a composition by the
reference device or by a tool. The window is hidden and borderless, resized to the client size named.

- Same frame: stage 1 under the autopilot (`RIFT_START=stage RIFT_AUTO=1 RIFT_GOD=1`), deterministic time, seed 1,
  stopped after update 750. The 480 × 270 canvas read back in each run has the same SHA-256
  (`A023601C12E93E34…`) in all eight runs (the three options and the default, at both sizes); the frame draws no UI
  view, so the canvas is the whole picture.
- `SdlGpuPresentedTests` holds the readback to the capture of the window (`CaptureScreen`) within 2/255 per channel
  for nearest, linear, a fixed prescale and the automatic one.

| | 1366 × 768 (frame 1365 × 768, 2.84×) | 1920 × 1080 (4×) |
| --- | --- | --- |
| default (`PixelArt`) | byte-identical to nearest | byte-identical to nearest |
| nearest | 100 % of the frame's pixels hold an exact canvas colour | 100 % |
| smooth (linear) | 24.3 %; 45.9 % of the window differs from nearest | 21.5 %; 46.6 % differs |
| sharp (linear + auto prescale, 3× then 4×) | 71.2 %; 17.5 % differs from nearest | byte-identical to nearest |

What the pictures show, full size and in 4× crops of the `PULSE` label and of a drone:

- **User-perceptible difference**, 1366 × 768, nearest (the default): letter strokes and outlines are two or three
  window pixels wide depending on where they fall (the stems of `P`, `U` and `L` differ in the crop). At full size
  in a still it is hard to see; it shows in the crop.
- **User-perceptible difference**, smooth: soft at both sizes, visibly so at full size (text, stars, drone
  outlines). At 1080p, where nearest is exact, it only loses sharpness.
- **User-perceptible difference**, sharp at 1366 × 768: strokes of even width with a one-pixel soft edge; at full
  size it reads as the nearest picture. At 1080p it *is* the nearest picture.
- **Hypothesis**: the uneven pixels of nearest shimmer when the camera scrolls, and sharp removes it. Stills cannot
  show it; no motion capture was made.
- **Demonstrated**: the default at 1366 × 768 is the fractional nearest picture (2× would fill 70 %, under the 80 %
  threshold), and at 1080p the default is exact. Neon Rift's default presentation is unchanged
  (`LaunchOptionsTests`).
- Not captured: the SDL renderer device, which is the desktop default (`--graphics sdl-gpu` selects SDL_GPU), and
  the raylib window. The compose path is the same code (`DeviceCanvas.Compose`) on each device.

The figures of §3 came from the reference device and a screen grab; its "79 % of the window's pixels differ" for
linear at 1080p is not what the device presents (46.6 %) and is superseded by this table.

### 5.2 `Presentation.Fit`

Consumers: the SDL and raylib platforms (`FrameRect`: where the canvas is composed, and the pointer mapping), the
headless platform (`IScreenView.WorldFrame`), `FrameLayout.Fit` and `VirtualControls.Frame` (touch mapping and the
on-screen controls), and through `WorldFrame` the UI view's canvas mapping. All use the frame as a continuous
rectangle; none depends on a fraction.

- **Demonstrated failure**: at a scale that is not whole the frame's size was a fraction of a pixel (1365.33 × 768
  for 480 × 270 in 1366 × 768; 1244.44 × 700 in 1366 × 700). A device paints the pixels whose centres the quad
  covers, so the picture was narrower than the frame said (1244 columns) and, because only the origin was floored,
  the letterbox was uneven: 60 columns on the left and 62 on the right in 1366 × 700
  (`TheFrameOfAWindow_IsPaintedPixelForPixel_WithEvenLetterbox`, reference device). In a one-pixel window the frame
  was about half a pixel tall.
- **Not a failure** (the earlier claim): the edge is not blended with the letterbox. Without multisampling the edge
  pixel is either canvas or letterbox; in the real 1366 × 768 capture column 1365 is the letterbox colour exactly.
- **User-perceptible difference**: at most one pixel of letterbox moving from one side to the other; none at
  1366 × 768, where the picture was already 1365 columns from column 0. With a linear upscale the picture is
  resampled over 1365 instead of 1365.33 pixels, a shift of at most a third of a pixel at the right edge.
- **Fix** (local to `Fit`): each side is the nearest whole size, at least one pixel and never more than the area;
  the position rule is unchanged. Frames that were already whole are returned bit for bit: every whole factor from
  1× to 8× of six canvas sizes with every rounding mode, and 164 of the 390 frames of the old-rule table
  (`PresentationSettingsTests`); the other 226 were fractional and are now the same frame within a pixel. Tests that
  pinned a fractional size (1365.33, 411.75) now pin the whole one.
- **Hypothesis, not changed**: a safe area whose own origin is a fraction of a pixel (a phone with a fractional
  pixel density) still gives a frame starting off the pixel grid in portrait; no device was available to show it.
- The frame of the real device before the fix was not captured; the 1366 × 768 capture after it shows what the
  centre rule gave before as well (1365 columns from column 0).

### 5.3 The prescale on every path

- **Demonstrated failure** (before): with a prescale, `DeviceCanvas.Compose` presented linear alone (window pixel 1
  of the test row `E6001A`, not red), the headless `CaptureScreen` did the same, and the raylib platform's capture
  gave 170 where the SDL one gave 212 for the same frame.
- **Fixed**: `DeviceCanvas.Compose` presents through `CanvasPresenter` when `Prescale != 1`; `ScreenCapture.Compose`
  has an overload taking the presentation, used by the three platforms; it samples the enlargement without building
  it. The composite equals the frame the reference device presents within 1/255 for auto, fixed, none and stretched
  presentations at five window sizes, and the SDL_GPU window within 2/255.
- **`Prescale == 1` is unchanged**: `DeviceCanvas.Compose` takes its previous branch (the file's only change is the
  new branch before it); the rows it presents for nearest and linear at 2.5× were recorded on the unchanged code
  and are pinned (`Without_a_prescale_the_window_is_composed_as_it_always_was`); the new `ScreenCapture.Compose`
  overload equals the old one byte for byte for five presentations and five frames; the SDL platform's compose call
  is the one on `main`.
- **Not verified**: the pixels of a raylib window with a prescale. The raylib platform runs the presenter without
  error and its capture is right; its window was not read back.
