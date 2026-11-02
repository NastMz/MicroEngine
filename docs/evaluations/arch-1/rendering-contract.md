# Rendering contract (ARCH-1 stage 0)

The written rules every `IRenderer` follows today, the source of variation each one allows, and how they are checked.
They resolve defect D5 of the [dossier](../arch-1.md) (§3.1) and are the first layer of the validation strategy (§8,
tiers T1 and T2): each backend is judged against these rules, not against another backend. The same rules are in the
XML documentation of the members they govern (`IRenderer`, `Texture`, `src/MicroEngine/Graphics/IGraphics.cs`);
this page adds the reasons and the evidence. Stage 1 moves the conformance suite and the reference rasterizer into
`MicroEngine.Testing`; stage 2a carries the rules into the new graphics contract (premultiplied alpha will change rule
R6, deliberately).

## Rules

| # | Rule | Exact or tolerance (named source of variation) | Backend changed to satisfy it |
| --- | --- | --- | --- |
| R1 | **Clear** replaces every pixel of the canvas with the colour, alpha included, whatever the clip and the blend mode, and replaces what was drawn before it in the frame. The clip still applies to later draws. | exact | raylib (scissor lifted for the clear; pending batch drawn first) |
| R2 | **Rasterization of axis-aligned rectangles and sprites.** Pixel (x, y) is the square [x, x+1) × [y, y+1), centre (x+0.5, y+0.5). A rectangle or unrotated sprite covers exactly the pixels whose centre lies inside it, with the top-left rule: a centre on the left or top edge is inside, on the right or bottom edge outside. A 1 × 1 rectangle at (2.5, 2.5) covers (2, 2) only. | exact. raylib: an edge less than 1/128 px below a pixel centre counts as on it (its tie correction, below) | raylib (geometry moved up 1/128 px) |
| R3 | **Sampling.** Nearest: the texel under the pixel centre. Linear: bilinear mix of the four texels around the centre (texel centres at +0.5), the texture's edges clamped (never wrapped). Straight (not premultiplied) interpolation. | nearest exact; linear within ±2/255 of the bilinear kernel (sampler interpolation precision) | raylib (textures created with clamp; raylib repeats by default) |
| R4 | **A disposed texture** throws `ObjectDisposedException` from `Draw` and `DrawStretched` on every renderer, before any backend call; nothing is drawn. | exact | SDL (drew nothing, silently), raylib (drew an opaque black quad); both now call the core check |
| R5 | **A texture of another graphics device** throws `ArgumentException` ("…was created by another graphics device…", parameter `texture`), not `InvalidCastException`; nothing is drawn. | exact | SDL and raylib (cast failed with `InvalidCastException`); headless accepted anything (it still does in counting mode, for existing tests) |
| R6 | **Blending**, straight alpha, source s after the tint, alpha a, canvas d, each channel clamped to [0, 1], results rounded to nearest 8-bit: `Alpha` s·a + d·(1−a), alpha a + d.a·(1−a); `Additive` s·a + d, alpha kept; `Multiply` s·d + d·(1−a), alpha kept. | exact on GPU drivers; ±1/255 on SDL's software renderer (8-bit quantization: it rounds down) | none (all backends already agreed on RGB) |
| R7 | **Captures are opaque**: every captured pixel has alpha 255, the colour channels as stored in the canvas. | exact | raylib (its alpha blend wrote 0xBF into the canvas alpha) |
| R8 | **Clip** is a screen-space rectangle (the camera does not move it); X, Y, Width and Height are truncated to whole pixels. | exact | none |
| R9 | **Camera**: draws move by `Camera2D.RenderOffset`; cameras replace, not combine. | exact | none |
| R10 | **Circles, lines, triangles** cover their shape: the centre (or interior) is covered, nothing lies beyond the shape plus one edge pixel, and the number of pixels lit is within a band of the area. | circles ±15 % of πr² (36-segment polygon on GPU backends); lines ±25 % of length × thickness (edge pixels of a slanted quad); triangles ±20 % of the area. SDL's software renderer has its own algorithm: circles [0.85, 1.35], lines [0.5, 2.0] and end points within 1 px (measured 41 vs 32 pixels for r = 3.5 in the dossier probe; 0.63× for a 2-px line, 2× for a 1-px diagonal) | none: the difference is a legitimate one, not forced to identity |

## Why these choices

- **Clear ignores the clip (R1).** SDL_Renderer's `SDL_RenderClear` already clears the whole target on every driver,
  and the future SDL_GPU path clears through the render pass's load operation (`SDL_GPU_LOADOP_CLEAR`), which also
  covers the whole target and knows no scissor. Choosing the other rule would have forced both SDL paths to emulate
  a clipped clear with a rectangle; raylib (OpenGL's `glClear` honours the scissor box) was the only outlier. raylib also
  batches draws but clears immediately, so a draw made before a `Clear` in the same frame survived it; it is now drawn
  first.
- **Top-left rule with centres at +0.5 (R2).** It is the rule of Direct3D, Vulkan, Metal and WebGPU, so every SDL
  driver (including `gpu`) and a future SDL_GPU device follow it natively; the probe measured SDL lighting row 2 for
  y = 2.5 on all drivers. raylib draws into a render texture stored upside down, so OpenGL's tie rule includes the
  bottom edge instead of the top one (row 3). raylib moves all canvas geometry up by 1/128 px (more than the 1/256 px
  sub-pixel precision GPUs use, so ties are resolved, and small enough that only edges within 1/128 px below a centre
  are affected). Columns already agreed.
- **Resource errors in core (R4, R5).** `Texture.CheckDrawable<TTexture>(texture, device)` (core) makes the checks and
  builds the message; every renderer calls it first, so backends fail alike and a new backend gets the behaviour by
  calling one method. `Texture.Device` records the creating device (a new protected constructor; textures made with
  the old one report null and are checked by type only). A validation layer wrapping any renderer (so a third-party
  backend could not skip the check) needs the runner (`GameRunner`) and belongs to stage 1's composition work.
- **Opaque captures (R7).** The canvas is shown on an opaque backdrop; SDL already forced alpha to 255 when writing
  captures, raylib now does too. raylib's canvas alpha itself still follows OpenGL's default alpha blend
  (a·a + d.a·(1−a)); whether the presented frame should be composited with that alpha is an open question for stage
  2a, outside these rules.
- **Shapes within bands (R10).** The software renderer rasterizes polygons with its own algorithm; forcing pixel
  identity would mean reimplementing it, and the contract does not promise edge pixels of shapes.

## How the rules are checked

| Check | Where | Runs |
| --- | --- | --- |
| Conformance suite: each rule as a test on the specification-tests pattern (abstract `RendererConformance`, one subclass per backend: headless CPU reference, SDL default and each driver `direct3d11`, `direct3d12`, `opengl`, `vulkan`, `gpu`, `software`, and raylib). Each assertion names its property and tolerance. | `tests/MicroEngine.Desktop.Tests/RendererConformance.cs`, `RendererConformanceBackends.cs` | with the native tests; drivers a system lacks are skipped, the default SDL and raylib are required under `MICROENGINE_NATIVE_TESTS=1`; the headless reference runs everywhere |
| CPU reference rasterizer, rule by rule, through the public rasterizing `HeadlessRenderer` | `src/MicroEngine/Graphics/ReferenceRasterizer.cs`; `tests/MicroEngine.Tests/ReferenceRasterizerTests.cs` | every .NET test run |
| Recorded commands (backend-neutral text, screen pixels) | `HeadlessRenderer.Commands` | every .NET test run |
| Property checks on scripted captures (sprite at its state's position ±0, HUD region changes with its value), demonstrated on Meadow Run | `tests/MeadowRun.Tests/CaptureProperties.cs`, `CapturePropertyTests.cs` | every .NET test run |
| Cross-backend pixel comparison of one probe frame: an informative report in the test output, not a pass criterion | `tests/MicroEngine.Desktop.Tests/PlatformEquivalenceTests.cs` | with the native tests |

The headless platform rasterizes only on request (`HeadlessPlatform.Rasterize`, `HeadlessPlatform.For(settings,
rasterize: true)`); by default it still counts calls, so existing tests keep their behaviour.
