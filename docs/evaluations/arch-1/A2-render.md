# A2 — Rendering, graphics and platform backends (ARCH-1, Phase I audit)

> **Superseded points (owner's approval, 2026-11-02).** This annex is kept as written; its findings stand. The device
> contract of §6.1 is now integrated, amended, in [dossier §6.4](../arch-1.md#64-fundamental-contracts-sketch) as the
> single graphics contract for every tier: built-in pipelines (sprite, textured geometry, with each supported blend
> state) on every tier, so the 2D batcher needs no custom shaders; capabilities that include tier, shader formats and
> limits; and one typed `GraphicsCapabilityException` for an undeclared capability instead of silent rejection or
> degradation. Specialized 2D/3D interfaces and per-mechanism extensions were rejected (dossier §7, ADR 0028). The
> contract is frozen only after the stage 1 spike (tier 0 on SDL_Renderer, tier 1 on SDL_GPU through P3), and the
> tier-1 SDL_GPU device and 3D vertical slice are Phase II work, not "later" (dossier §9, §9.1). Where this annex and
> the dossier differ, the dossier wins.

Auditor: domain A2. Repository: `C:\Proyectos\MicroEngine`, branch `docs/arch-1` at `5706285` (includes the unmerged
ASSET-1 work). Read-only on the repository; experiments ran on a copy of `src/MicroEngine`, `src/MicroEngine.Sdl` and
`src/MicroEngine.Raylib` under the scratch directory (see "Experiments").

Vision measured against: a generalist, modular, multi-platform engine for 2D, 2.5D and 3D, with plug-and-play defaults
that are coherent, documented and replaceable.

---

## 1. Inventory

### 1.1 Contracts (core, `src/MicroEngine`)

| Type | Where | What it is |
| --- | --- | --- |
| `IRenderer` | `Graphics/IGraphics.cs:115-152` | 13 members: `Width`, `Height`, `Clear`, `Draw(Texture, RectF, Vector2, Color, in DrawOptions)`, `DrawStretched`, `FillRect`, `FillCircle`, `FillTriangle`, `DrawLine`, `PushCamera(Camera2D)`, `PopCamera`, `SetBlendMode`, `SetClip`. Immediate mode, virtual-canvas pixels. |
| `IGraphicsDevice` | `Graphics/IGraphics.cs:155-166` | `DefaultFilter {get;set;}`, `CreateTexture(PixelImage, TextureFilter)`, `CreateTexture(PixelImage)` (default method). No dispose, no capabilities, no other resource kinds. |
| `Texture` | `Graphics/IGraphics.cs:19-86` | Abstract class: `Width`, `Height`, `IsDisposed`, `Bounds`, `SizeInBytes` (hard-coded 4 B/px, line 34), mutable `Filter` (line 37), `Update(...)` region upload, abstract `Upload`/`ApplyFilter`/`Release`. |
| `TextureFilter`, `CanvasScaling`, `BlendMode`, `DrawOptions` | `Graphics/IGraphics.cs:9,16,92,100` | `Nearest/Linear`; `Pixel/Smooth`; `Alpha/Additive/Multiply`; flip/rotation/origin/scale. |
| `IPlatform` | `Core/Platform.cs:386-443` | Window + `IGraphicsDevice Graphics` + `IAudioDevice` + `IInputSource` + time + `PollEvents` + `BeginFrame(): IRenderer` + `EndFrame` + `SaveScreenshot` + `ToggleFullscreen` + `Letterbox` + optional `Touch`, `Wait`, `DisplayRate`, `Suspended`. |
| `GameSettings` | `Core/Platform.cs:347-370` | `VirtualWidth=320`, `VirtualHeight=180`, `Scale=4`, `Scaling=Pixel`, `TextureFilter?` (null follows `Scaling`, line 361), `Background`, `Letterbox`, `Graphics`. |
| `GraphicsSettings`, `GameScreen`, `IDisplayControl`, `IPlatformDiagnostics` | `Core/GraphicsSettings.cs` | Backend/renderer-name preference lists (strings), VSync, frame limit, GPU preference, window mode, display. |
| `PlatformProfile`, `PlatformRequest`, `PlatformParts` | `Core/PlatformProfiles.cs:10-140` | Replaceable parts: only `Audio` and `Input` (lines 10-18). `CaptureScale` derived from `Scaling` (line 43). |
| `FrameLayout` | `Input/Touch.cs:80-104` | The letterbox/integer-scale rule shared by all backends — lives in the Input namespace/file. |
| `WindowSizing` | `Core/WindowSizing.cs` | Initial/minimum window size; integer factors for `Pixel`. |
| `IOverlayCanvas` | `Input/Touch.cs:~55-76` | A second drawing contract, in window pixels, for touch controls only (circle, ring, triangle, rect, text, image). |
| `Camera2D` | `Graphics/Camera2D.cs` | `sealed`; translation only ("no zoom or rotation", line 10); `PixelSnap = true` default (line 49); `RenderOffset` is the only thing renderers read. |
| `PixelImage` | `Graphics/PixelImage.cs` | CPU RGBA8 straight-alpha image; the only source of textures. |
| `BitmapFont`, `ScalableFont`/`SizedFont`/`FontOptions`, `IFont` | `Graphics/BitmapFont.cs`, `ScalableFont.cs`, `IFont.cs` | Built-in code font (always nearest, `BitmapFont.cs:58`); FontStashSharp glyph pages as engine `Texture`s, drawn through `IRenderer.Draw` per glyph. |
| `LightMap`, `ParticleSystem`, `ParallaxLayer`, `RendererExtensions` | `Graphics/*.cs` | Effects built only from `IRenderer` primitives (LightMap: a multiply-blended grid of `FillRect`s instead of a light buffer). |
| `HeadlessPlatform`/`HeadlessRenderer`/`HeadlessGraphicsDevice` | `Core/HeadlessPlatform.cs:12-262` | Counts calls; textures hold no pixels; no rasterizer. |

### 1.2 Backends

| Backend | Where | Rendering path |
| --- | --- | --- |
| SDL 3 (`sdl`, `sdl-<driver>`) | `src/MicroEngine.Sdl/SdlPlatform.cs` (854 lines; renderer 676-853, device 616-646, texture 648-674) | `SDL_CreateRenderer` (SDL_Renderer 2D) on D3D11/D3D12/Vulkan/OpenGL/GLES/Metal/`gpu`/software; draws into an `SDL_TEXTUREACCESS_TARGET` texture of the virtual size (line 160), whose scale mode follows `Scaling` (162); `EndFrame` letterboxes, draws the touch overlay, presents and (first time) starts the gamepad subsystem (434-446). |
| raylib (`raylib`, browser) | `src/MicroEngine.Raylib/RaylibPlatform.cs` (renderer 342-432, device 285-320) | `LoadRenderTexture` of the virtual size (99), filter follows `Scaling` (100); `DrawTexturePro` per sprite; GL batching inside raylib. |
| Headless | `src/MicroEngine/Core/HeadlessPlatform.cs` | No pixels. |

Implementations of `IRenderer` in the repository: 3 backends + `ScalableFont.NoRenderer` + 4 test doubles. Use sites:
`IRenderer` appears in 20 core files (55 draw calls), 51 game files (343 calls), 35 sample files (56 calls), the
template (2) and 20 test files (30).

### 1.3 What the documentation already admits

`docs/manual/rendering.md:253-257`: no shaders, render targets or post-processing; no mipmaps or compressed formats,
32-bit RGBA only; no batching control; draw order is call order. ADR 0024 defers a capability-based renderer to
"GRAPHICS-1" (SDL_GPU next to SDL_Renderer and raylib WebGL). ADR 0025 defers GPU formats, mipmaps and SDF text to
GRAPHICS-1. `docs/manual/backends.md` "Replacing a part": window, renderer, graphics device and presentation "share one
graphics context, so they are replaced together by choosing another profile".

---

## 2. Experiments

Scratch root: `<scratch>\exp-render\`

- `repo\` — copy of the root props, `global.json`, `NuGet.config`, `src\Directory.Build.props` and the three engine
  projects (no `bin`/`obj`), so nothing was built inside the repository.
- `repo\exp\Probe\` — rendering probes run through the public API (`PlatformProfile.Create`, hidden window,
  `CaptureScale = 1`, 64×16 canvas), on `sdl-direct3d11`, `sdl-direct3d12`, `sdl-opengl`, `sdl-vulkan`, `sdl-gpu`,
  `sdl-software` and `raylib` (Windows 11, this machine). Captures in `out\`.
- `repo\exp\Stub\` — a substitute renderer/device/platform (command recorder) that runs a `Game` through `GameRunner`.

### 2.1 Probe results (verbatim output, condensed)

| Probe | D3D11 / D3D12 / OpenGL / Vulkan / gpu | software | raylib | Headless contract |
| --- | --- | --- | --- | --- |
| P1 linear-filtered 2×1 texture (opaque red, transparent black) stretched over white; ideal (premultiplied) keeps R=255 | mid `#BF8383FF`, minR=191 | `#BF8484FF`, 191 | `#BF8383BF`, 191 (**alpha 0xBF in the capture**) | n/a |
| P2 `SetClip(0,0,8,8)` then `Clear(red)` | 1024 of 1024 red (**clip ignored**) | 1024 | **64** (clip respected) | not modelled |
| P3 draw a disposed texture | no exception, nothing drawn (white) | same | no exception, **opaque black quad** | throws `ObjectDisposedException` (`HeadlessPlatform.cs:243-247`); doc: "Drawing a disposed texture fails" (`IGraphics.cs:27`) |
| P4 draw a texture made by another device | `InvalidCastException ... to type 'MicroEngine.Sdl.SdlTexture'` | same | `InvalidCastException ... 'RaylibTexture'` | accepts anything |
| P5 1×1 sprite and 1×1 rect at (x.5, 2.5) | lights row **2** | row 2 | lights row **3** | n/a |
| P5 `FillCircle` r=3.5 / 1-px diagonal line, pixels lit | 32 / 10 | **41 / 20** | 32 / 10 | n/a |
| P6 Multiply (255,0,0,128) over grey 128; Additive (100,0,0,128) | `#C04040`, `#B28080` | `#BF3F3F`, `#B28080` | `#C04040`, `#B28080` | n/a |

### 2.2 Stub substitution result

`exp\Stub\Program.cs`: a `RecordingRenderer` (13 members), `RecordingDevice` (3 members + a 3-method `Texture`
subclass) and a `RecordingPlatform` (12 members) — about 85 lines — ran a game (camera, run-time atlas, built-in font,
additive circle) for 3 frames: `frame 0: 10 commands: clearx1, spritex6, blend:Additivex1, circlex1, blend:Alphax1`.
Observations:

- It only works because the stub borrows `HeadlessPlatform`'s input, audio and clock. There is no seam to plug a
  renderer into the SDL or raylib *window*: `PlatformParts` has no renderer/graphics part (`PlatformProfiles.cs:10-18`),
  and `SdlInput`, `SdlTouchScreen`, `SdlRenderer`, `SdlGraphicsDevice` and `SdlPlatform.Window` are `internal`
  (`SdlPlatform.cs:613,616,648,676`). A custom renderer on a real window means re-implementing (or forking) the
  854-line `SdlPlatform`, including input, touch, audio, lifecycle, display control and pacing.
- The game declared a 1280×720 canvas while the stub renderer reported 320×180: nothing noticed
  (`Game.Width => Settings.VirtualWidth`, `Game.cs:87`, vs `IRenderer.Width` from the platform).
- Text arrives as one `Draw` per glyph (5 for "Hello"); batching is entirely the backend's (SDL_Renderer's and raylib's
  internal batches), which the contract neither promises nor lets the game control.

---

## 3. Findings

Classification: CP confirmed problem · AR architectural risk · DL design limitation · LS legitimate specialization ·
MC missing capability · NE needs experiment. Severity: critical / high / medium / low.

| ID | Finding | Class | Severity |
| --- | --- | --- | --- |
| R1 | `IRenderer` is a closed, 2D-only immediate-mode API narrower than both of its own backends; no path to 2.5D/3D, shaders, materials, render targets | DL + AR | **critical** (for the stated vision) |
| R2 | No renderer/graphics-device seam: window, device, renderer, presentation, touch overlay and even gamepad start-up are fused per backend | CP | **high** |
| R3 | The backend contract is defined by "whatever SDL_Renderer/raylib do": measured divergences in clear-vs-clip, pixel centres, disposed textures, alpha channel, rasterization | CP | **high** |
| R4 | Resource model: textures only from CPU RGBA8 `PixelImage`; filter is texture state (no samplers); no formats, mipmaps, targets, readback, buffers; no device-loss handling; texture identity by downcast | MC + AR | **high** |
| R5 | Straight (non-premultiplied) alpha end to end: linear filtering produces dark fringes on every backend | CP | medium |
| R6 | Implicit couplings and pixel-art defaults embedded in core and backends (16 items, §4) | CP | medium (aggregate high) |
| R7 | One fixed-resolution virtual canvas is the only render path; letterbox only; canvas immutable at run time; no native-resolution layer except a separate touch-only contract | DL | medium |
| R8 | Cameras and transforms: `Camera2D` is sealed, translation-only, non-composable; no matrices anywhere; `Color` is 8-bit straight sRGB-agnostic | DL + MC | medium |
| R9 | Render state is write-only: no blend/clip getter or stack; helpers clobber caller state | CP | low |
| R10 | Two sources of truth for the canvas size; backend-specific strings in core settings | AR | low |
| R11 | Font pipeline is managed and backend-identical; captures read the virtual canvas | LS (with caveats) | — |
| R12 | Headless has no rasterizer: the only executable definition of "correct pixels" is per-backend reference hashes | AR (cross-domain: infra) | medium |

### R1 — `IRenderer` is a closed 2D immediate-mode API, narrower than its backends

1. **What exists.** 13 members in virtual pixels (`IGraphics.cs:115-152`); positions are `Vector2`, areas `RectF`;
   the only transform is a translation from `Camera2D.RenderOffset` (`SdlPlatform.cs:827-831`,
   `RaylibPlatform.cs:405-409`); three fixed blend modes; one screen-space clip; one renderer per frame from
   `IPlatform.BeginFrame()` (`Platform.cs:431`).
2. **Problem.** There is no vocabulary for depth, matrices, vertex/index data, textured geometry, UV/address modes,
   render targets, shaders, materials, passes or sort keys. Adding any of them to the interface breaks all eight
   implementations; adding 3D requires a different contract altogether. Worse, the API is not limited by its backends
   but by an intersection chosen early: both bindings already ship features the contract hides —
   SDL3-CS 2026.1002.1 exports `SDL_BLENDMODE_BLEND_PREMULTIPLIED`, `SDL_ComposeCustomBlendMode`,
   `SDL_SetRenderTextureAddressMode`, `SDL_RenderGeometryRaw` (textured), `SDL_SetRenderViewport/Scale`,
   `SDL_CreateGPURenderState`/`SDL_SetGPURenderState` (custom fragment shaders on the `gpu` renderer),
   `SDL_GetGPURendererDevice`; Raylib-cs 8.1.0 exports `BlendMode.AlphaPremultiply`, `LoadShader`/`BeginShaderMode`,
   `BeginMode3D`, `SetTextureWrap`, `GenTextureMipmaps` (checked by string search in the package DLLs). Render targets
   are already used internally by both backends for the canvas (`SdlPlatform.cs:160`, `RaylibPlatform.cs:99`).
   `LightMap` (a 4-px grid of multiply `FillRect`s, `LightMap.cs:97-127`) exists because a light buffer could not be
   expressed.
3. **Evidence.** Above, plus `rendering.md:255-257`, ADR 0024 "GRAPHICS-1" paragraph, ADR 0025 consequences.
   Blast radius of any change: ~490 call sites in games/samples/core/tests.
4. **Industry.** XNA/MonoGame/FNA layer `SpriteBatch` (a 2D batcher with `Begin(sortMode, blendState, samplerState,
   depthStencilState, rasterizerState, effect, transformMatrix)`) on top of a `GraphicsDevice`; the batcher is a
   client of the device, not the device's contract. Godot separates a `RenderingServer` (canvas items + 3D scenarios)
   from the `RenderingDevice` RHI. bgfx, sokol_gfx, SDL_GPU and WebGPU all converge on
   device → resources (buffer/texture/sampler/shader/pipeline) → command buffer → pass → submit. raylib itself layers
   its 2D/3D API on `rlgl`.
5. **Alternatives.** (a) Keep `IRenderer`, add optional capability interfaces (`IRenderTargets`, `IShaders`) via
   type tests — cheap, but fragments the API and never gets 3D. (b) Default interface methods on `IRenderer` — avoids
   breaks, still 2D-shaped. (c) Layer: a small RHI-style device contract with capability tiers; a 2D batcher and
   (later) a 3D renderer are *clients* of it; the current `IRenderer` survives as the batcher's façade.
6. **Proposal.** (c). See §6. The SDL_Renderer and raylib backends become a "tier 0" device (sprites, textured
   triangles, blend states, targets, scissor, address modes — all supported by both) and SDL_GPU (desktop/mobile) +
   WebGPU/WebGL (browser) become "tier 1" (shaders, pipelines, depth).
7. **Consequences.** Largest change of the domain; every backend rewritten around a device; `IRenderer` call sites can
   remain source-compatible through the batcher façade, so games need not change in the first step.

### R2 — No renderer/graphics-device seam; the trio (and more) is fused

1. **What exists.** `IPlatform` exposes `Graphics` and `BeginFrame()` but a host may replace only `Audio` or add
   `Input` (`PlatformProfiles.cs:10-18`, `backends.md` "Replacing a part"). `SdlPlatform`'s constructor creates window,
   renderer, frame target, device, renderer wrapper, touch screen (given the `SDL_Renderer*`, `SdlPlatform.cs:164`),
   input and audio in one sequence (120-169). `EndFrame` letterboxes, draws touch controls, presents and starts the
   gamepad subsystem (434-446).
2. **Problem.** A different renderer (SDL_GPU, a 3D renderer, a debug/recording renderer, a custom batcher) cannot
   reuse the SDL window, input, touch, audio, lifecycle and display control: they are internal and created by the same
   constructor. The window↔GPU-device binding is a real constraint (one swapchain claims the window), but input, audio,
   touch hit-testing, pacing and gamepad start-up have no reason to depend on the renderer. Gamepad initialization in
   the present path (`SdlPlatform.cs:445`) is an input concern living in rendering. The touch overlay draws with the
   SDL renderer, so it would also need re-implementing.
3. **Evidence.** Stub experiment §2.2; `SdlPlatform.cs:613` (`internal SDL_Window* Window`), 616/648/676 (internal
   device/texture/renderer); `PlatformParts`.
4. **Industry.** SDL itself separates `SDL_Window` from `SDL_Renderer` and from `SDL_GPUDevice`
   (`SDL_ClaimWindowForGPUDevice`); Godot has `DisplayServer` (windows, input events) separate from
   `RenderingServer`; MonoGame `GameWindow` vs `GraphicsDevice`; Veldrid `Sdl2Window` vs `GraphicsDevice` created
   from a `SwapchainSource`.
5. **Alternatives.** (a) Add `PlatformParts.Renderer` with a factory taking a native window handle — minimal but leaks
   handles through the core. (b) Split `IPlatform` into `IWindowHost` (surface, size, density, events, lifecycle),
   `IGraphicsBackend` (device + swapchain for a surface), input and audio providers, composed by the profile.
6. **Proposal.** (b): an `ISurface` (native handle + pixel size + density + resize/loss events) produced by the window
   host and consumed by a graphics backend factory; presentation (letterbox/overlay) moves to an engine-owned
   compositor drawn with the device. Gamepad start-up moves to the input provider.
7. **Consequences.** `SdlPlatform` splits into window/input/audio/graphics pieces; raylib, which fuses GLFW window,
   GL context and input inside one library, remains a single "integrated backend" that provides all parts at once —
   acceptable as long as the contract allows both shapes.

### R3 — The backend contract is implicit and the backends disagree

1. **What exists.** XML comments define a few rules; everything else is what each library does. Per-backend visual
   references (`web/tests/reference/*.json`, grid-mean tolerance 3) absorb differences.
2. **Problem.** Measured (§2.1): `Clear` ignores the clip on every SDL driver and respects it on raylib; a sprite or
   rect at y = 2.5 lands on row 2 with SDL and row 3 with raylib (pixel-centre/rounding rule undefined, probably the
   render-texture Y flip); a disposed texture draws nothing on SDL and an opaque black quad on raylib, while the
   contract says drawing it "fails" and only headless enforces it; raylib's blend writes alpha 0xBF into the canvas
   where SDL writes 0xFF, so raylib captures are not opaque; the software renderer rasterizes circles/lines with
   ~30-100 % more pixels. A game that is correct on one backend can be wrong on another, and "replaceable backend" has
   no executable definition.
3. **Evidence.** Probe outputs; `SdlRenderer.Clear` (`SdlPlatform.cs:714-718`, SDL clears the whole target),
   `RaylibRenderer.Clear` (`RaylibPlatform.cs:368`, GL clear honours scissor); `HeadlessRenderer.Draw`
   (`HeadlessPlatform.cs:243-247`) vs `SdlRenderer.Draw` (`SdlPlatform.cs:753-755`) and `RaylibRenderer.Draw`
   (`RaylibPlatform.cs:370-372`).
4. **Industry.** Vulkan/D3D/WebGPU specify rasterization rules (top-left fill rule, pixel centres at .5) and validation
   layers; WebGPU's CTS and Vulkan's CTS are conformance suites; Direct3D's WARP is a reference rasterizer; MonoGame
   carries a per-platform test suite with reference images and tolerances.
5. **Alternatives.** (a) Document each divergence. (b) Normalize in the backends (scissor clear by `FillRect`,
   half-pixel offset in raylib, alpha write mask). (c) A thin engine-side validation layer over every device
   (disposed/foreign resources, unbalanced state) plus a backend conformance suite.
6. **Proposal.** (b)+(c): write the rules down (pixel centres, fill rule, what `Clear` touches, alpha channel of the
   canvas, error on disposed/foreign resources), enforce the resource rules in core so every backend fails alike, and
   add a conformance test suite each device must pass (geometric invariants and tolerances, not hashes — cross-domain
   with the infrastructure auditor).
7. **Consequences.** Some references change once; new backends get a definition of done.

### R4 — Resource model: CPU-first RGBA8 textures, texture = sampler, no lifetime/loss story

1. **What exists.** `CreateTexture(PixelImage, TextureFilter)` only (`IGraphics.cs:163`); `SizeInBytes => W*H*4`
   (34); `Filter` mutable on the texture (37-47); `Update` region uploads; device keeps a `HashSet` of live textures
   and disposes them with the platform (`SdlPlatform.cs:618-645`); `IGraphicsDevice` is not `IDisposable`; textures
   are downcast to the backend type (`(SdlTexture)texture`, `SdlPlatform.cs:755,773`; `(RaylibTexture)texture`, `RaylibPlatform.cs:372,383`).
2. **Problem.** No texture descriptions (format, mip count, usage: sampled/target/storage), so no R8/RG/float/HDR,
   compressed formats, mipmaps (documented shimmer, `rendering.md:57-60`), render targets, or readback. The filter is
   texture state, so the same texture cannot be drawn sharp in one place and smooth in another without mutating shared
   state; there is no address/wrap mode (parallax and tiling must emit many quads). Device loss is not handled: no
   handler for `SDL_EVENT_RENDER_DEVICE_RESET`/`DEVICE_LOST` (grep of `src/MicroEngine.Sdl` finds none, though the
   binding exports both), and nothing retains CPU copies, so a reset loses every texture including FontStash glyph
   pages. Foreign textures fail with `InvalidCastException` (probe P4) instead of a contract error.
3. **Evidence.** As cited; probe P4; binding string search.
4. **Industry.** D3D10+/Vulkan/WebGPU/SDL_GPU separate texture and sampler objects; MonoGame keeps `SamplerState` in the
   device/batch, not the texture; XNA 4 and MonoGame handle `DeviceLost/DeviceReset`; Godot imports textures to GPU
   formats with mipmaps and VRAM compression per platform.
5. **Alternatives.** (a) Add a `TextureDescription` overload and a `SamplerState` parameter to draw calls while keeping
   `PixelImage` overloads. (b) Full resource API (§6).
6. **Proposal.** `TextureDescription` + `Sampler` as values (immutable, cached by the device), resource handles that
   carry their owning device (validated in core), an explicit `DeviceLost` event and a "restorable" policy (keep the
   `PixelImage`/asset path to re-upload), `IGraphicsDevice : IDisposable`. Keep `Texture.Filter` only as the default
   sampler for draws that do not pass one.
7. **Consequences.** Asset pipeline (A-assets) gains format/mip processing; ADR 0025's "filter is a property of each
   texture" is reversed for sampling (a texture keeps a *default* sampler).

### R5 — Straight alpha: dark fringes on linear sampling

1. **What exists.** `Color` is "8-bit-per-channel, non-premultiplied" (XML docs); `BlendMode.Alpha` maps to
   `SDL_BLENDMODE_BLEND`/raylib `BLEND_ALPHA`; decoded PNGs and glyph pages (`new PixelImage(w,h)`, i.e. transparent
   *black*, `ScalableFont.cs:279`) are uploaded as is.
2. **Problem.** Bilinear sampling interpolates colour with transparent black before blending: measured R=191 instead of
   255 on all seven backends (P1). Every `Smooth` game (linear by default), every linearly sampled sprite edge and
   every scaled/rotated glyph gets darkened edges. Run-time atlases extrude one border pixel for linear
   (`Sprite.cs:99`), which hides seams but not the fringe at alpha edges.
3. **Evidence.** P1 captures `out\*-fringe.png`.
4. **Industry.** XNA 4.0 switched to premultiplied alpha by default (content processor premultiplies, `BlendState.AlphaBlend`
   is premultiplied); MonoGame and FNA kept it; Godot offers premultiplied blend and "fix alpha border" import.
5. **Alternatives.** Premultiply at load/upload and use premultiplied blend (both backends support it, R1); or
   alpha-bleed colour into transparent texels at import (cheaper, keeps straight alpha, fixes sampling only).
6. **Proposal.** Decide one alpha convention for the engine (recommend premultiplied in GPU memory, straight in
   `PixelImage`), applied by the upload path, with blend modes defined for it; glyph pages initialised to transparent
   white meanwhile.
7. **Consequences.** Changes reference captures for linear content; `Multiply`/`Additive` formulas must be restated.

### R6 — Implicit couplings and pixel-art defaults (summary; full list in §4)

1. **What exists.** One enum (`CanvasScaling`) drives present filter, integer fitting, initial window size, default
   texture filter and capture scale; one font option (`Antialiasing`) drives the glyph-page filter (the ASSET-1 case);
   pixel-art defaults (320×180, ×4, `PixelSnap`, 1-px helper geometry, F11) live in core and backends.
2. **Problem.** A developer cannot ask for combinations the enum does not encode (integer scaling with a sharp-bilinear
   present, smooth fill with nearest present, HD canvas with nearest default textures *and* 1× captures, antialiased
   glyphs sampled nearest...) without discovering hidden rules; documentation already disagrees with code:
   `rendering.md:46-47` says the game's default filter "applies to every texture, atlas and font page created without
   a filter of its own", but `ScalableFont` ignores `DefaultFilter` (`ScalableFont.cs:68`).
3. **Evidence.** §4.
4. **Industry.** Godot splits the same space into independent project settings: `stretch/mode` (disabled,
   canvas_items, viewport), `stretch/aspect` (ignore, keep, keep_width, keep_height, expand), `stretch/scale_mode`
   (fractional, integer) and `default_texture_filter`; Unity's Pixel Perfect Camera is an opt-in component.
5. **Alternatives.** Keep `CanvasScaling` as a *preset* that expands to explicit, individually overridable settings.
6. **Proposal.** `Presentation` record (stretch mode, aspect policy, scale rounding, present filter) + explicit
   `DefaultSampler`; `CanvasScaling.Pixel/Smooth` become named presets; captures choose their own scale; fonts take the
   sampler from the same default unless overridden.
7. **Consequences.** Removes all R6 couplings without losing plug-and-play: presets are still one line.

### R7 — A single fixed-resolution canvas is the only render path

1. **What exists.** Every draw goes to a target of `VirtualWidth×VirtualHeight` created once (`SdlPlatform.cs:160`,
   `RaylibPlatform.cs:99`), then scaled into the window with letterbox (`FrameLayout.Fit`, `Touch.cs:89-99`). Fonts are
   rasterized in canvas pixels. The only window-resolution drawing is the touch overlay through `IOverlayCanvas`, with
   per-backend debug fonts (SDL 8×8 `SDL_RenderDebugText`, `SdlTouchScreen.cs:217-226`; raylib 10-px default font,
   `RaylibTouchScreen.cs:125-132`).
2. **Problem.** HD UI/text over a low-res world (the most common pixel-art+HD hybrid) is impossible; a 1280×720
   `Smooth` game on a 4K display shows upscaled text; ultrawide/portrait windows can only letterbox (no "expand"); the
   canvas cannot change at run time (resolution options, dynamic resolution, split-screen viewports); a 3D renderer
   needs per-view targets and a compositor, not one canvas.
3. **Evidence.** As cited; `GameSettings` is init-only; no viewport API.
4. **Industry.** Godot `canvas_items` vs `viewport` stretch and `CanvasLayer`; Unity Pixel Perfect Camera "upscale
   render texture" toggle; MonoGame users render the world to a `RenderTarget2D` and UI at back-buffer resolution.
5. **Alternatives.** (a) Add a second "screen layer" to `IPlatform`. (b) Views/targets as first-class: the engine
   composites any number of views (each with resolution policy) into the surface.
6. **Proposal.** (b) on top of render targets (R4): `View` = target + camera + presentation rule; the default preset is
   one pixel view, letterboxed — today's behaviour. Touch overlay becomes an ordinary screen-resolution view.
7. **Consequences.** `IOverlayCanvas` disappears; captures must name which view/target they read.

### R8 — Cameras and transforms are 2D-translation-only

1. **What exists.** `Camera2D` is `sealed`, "no zoom or rotation" (`Camera2D.cs:10,26`); renderers read only
   `RenderOffset`; `PushCamera` *replaces* the offset (cameras do not compose, `IGraphics.cs:141-143`); `SetClip` is
   screen space and untransformed; `PixelSnap` defaults to true. `Matrix3x2`/`Matrix4x4`/`Vector3`/`Quaternion` appear
   nowhere in `src`, `games` or `samples` (grep). `Vector2` is `System.Numerics` (good); `RectF` is engine-defined;
   `Color` is 8-bit, straight, with no colour-space notion.
2. **Problem.** Zoom, rotation, nested transforms (UI panels, scene graphs), parallax via matrices, 2.5D (isometric
   with depth sort) and any 3D camera need new types and a new `IRenderer` signature. HDR/linear lighting has no colour
   type.
3. **Evidence.** As cited.
4. **Industry.** MonoGame passes `Matrix` to `SpriteBatch.Begin`; Godot `Camera2D` has zoom/rotation and composes
   `Transform2D`s; Unity/Godot keep a float linear colour type separate from 8-bit storage.
5. **Alternatives.** Add zoom to `Camera2D` only (fast, dead end) vs a transform stack on the batcher plus a camera
   abstraction producing view/projection matrices.
6. **Proposal.** `PushTransform(in Matrix3x2)` on the 2D batcher, `ICamera { Matrix4x4 View; Matrix4x4 Projection; }`
   with `Camera2D` (zoom, rotation, snapping as an option) and later `Camera3D`; `ColorF` (float, linear) for
   shading/lighting next to `Color` (storage).
7. **Consequences.** Cross-domain with World/Entities (transform hierarchy) and Physics (shared transform type).

### R9 — Write-only render state

`SetBlendMode`/`SetClip` have no getter or stack; `LightMap.Draw` and game code reset to `BlendMode.Alpha`
(`LightMap.cs:93,126`; `games/MeadowRun/Gameplay/Actors/Boss.cs:40-42`), silently breaking a caller that had another
mode active; clip cannot nest (UI scroll views inside clipped panels). Fix: `Push/PopState` or batch-scoped state
(`Begin(BatchSettings)`). Class CP, low.

### R10 — Duplicate truths and backend strings in core

`Game.Width` reads settings (`Game.cs:87`) while drawing code reads `IRenderer.Width` from the platform; the stub ran a
1280×720 game on a 320×180 renderer unnoticed (AR, low). `GraphicsSettings.Renderers` takes SDL driver names
(`GraphicsSettings.cs:210-214`) and diagnostics are string-keyed — acceptable as preferences, but they should be
typed capabilities once a device exposes them (LS/AR, low).

### R11 — Font path and captures (legitimate, with caveats)

The font path is managed and identical across backends: FontStashSharp rasterizes into engine `Texture`s via
`Texture.Update` and draws through `IRenderer.Draw` (`ScalableFont.cs:268-307`); `BitmapFont` is code-built and always
nearest (`BitmapFont.cs:58`). This is a good, portable choice for a 2D-first engine. Caveats: per-glyph draw calls
rely on backend batching; no SDF/MSDF without shaders (ADR 0025); text resolution is tied to the canvas (R7); glyph
pages are lost on device reset (R4). Captures read the virtual canvas before presentation (`SdlPlatform.cs:466-470`,
`RaylibPlatform.cs:181-188`) — deterministic per backend, but raylib captures carry non-opaque alpha (R3) and
`CaptureScale` is coupled to `Scaling` (§4).

### R12 — No reference rasterizer

`HeadlessGraphicsDevice` stores no pixels and `HeadlessRenderer` only counts (`HeadlessPlatform.cs:184-262`), so the
only executable definition of correct output is per-backend SHA/grid references (`web/tests/reference/sdl.json`:
"tolerance": 3, separate files for Linux Mesa, macOS, Android, iOS). A small CPU reference rasterizer implementing the
written rules of R3 would let core tests assert pixels without a GPU and give backends a conformance oracle. AR,
medium; owned jointly with the infrastructure auditor.

---

## 4. Implicit assumptions and couplings (systematic list)

| # | Decision taken for the developer | Where | Coupled to / assumption | Proposed handling |
| --- | --- | --- | --- | --- |
| 1 | Default texture filter follows canvas scaling | `Core/Platform.cs:361` | `Scaling` → sampler | Explicit `DefaultSampler`; preset sets it |
| 2 | Glyph-page filter follows antialiasing, ignoring the game default | `Graphics/ScalableFont.cs:68`; contradicts `docs/manual/rendering.md:46-47` | `Antialiasing` → sampler (ASSET-1 case) | Font sampler from game default unless set |
| 3 | Present (upscale) filter follows canvas scaling | `SdlPlatform.cs:162`, `RaylibPlatform.cs:100` | `Scaling` → present filter | `Presentation.Filter` (incl. sharp-bilinear) |
| 4 | Integer-vs-fractional fitting and the 80 % rule follow scaling | `Input/Touch.cs:92-93` | `Scaling` → rounding policy; magic 0.8 | `Presentation.Rounding` + threshold |
| 5 | Initial window size uses whole factors for `Pixel` | `Core/WindowSizing.cs:33` | `Scaling` → window size | Same presentation policy object |
| 6 | Capture scale is 3 for `Pixel`, 1 for `Smooth` | `Core/PlatformProfiles.cs:43` | `Scaling` → test artefacts | Capture option, independent |
| 7 | Run-time atlas extrusion decided by the filter at build; filter mutable later | `Graphics/Sprite.cs:99` vs `IGraphics.cs:37` | build-time layout ↔ run-time sampler | Always extrude (or by declared max sampler) |
| 8 | `DefaultFilter` is mutable device-global state written by `Game.Initialize` | `Core/Game.cs:144`, `IGraphics.cs:161` | creation time → result | Immutable defaults passed in |
| 9 | Letterbox is the only aspect policy | `Input/Touch.cs:89-99` | canvas aspect fixed | Aspect policy (keep/expand/...) |
| 10 | Canvas size fixed at start, one canvas | `SdlPlatform.cs:160`, `RaylibPlatform.cs:99` | single view, no res changes | Views/targets (R7) |
| 11 | Default canvas 320×180 at ×4; headless default 320×180 | `Core/Platform.cs:347`, `HeadlessPlatform.cs:17` | pixel art | Keep as template/preset, not record default |
| 12 | Camera snaps to whole pixels by default; no zoom/rotation | `Graphics/Camera2D.cs:49,10` | pixel art, 2D | Option on a general camera |
| 13 | Glyphs snap to whole canvas pixels by default | `Graphics/ScalableFont.cs:27,299` | pixel canvas | Snap to *target* pixels |
| 14 | Helper geometry in 1-px units: `Panel` (1–2 px offsets), `FillEllipse` (1-px spans), `FillGradient` (one rect per pixel row: 1080 rects on a 1080p canvas) | `Graphics/RendererExtensions.cs:473-516` | pixel art, no gradients/vertex colours | Vertex-coloured quads/geometry; pixel style as a theme |
| 15 | F11 and Alt+Enter always toggle full screen and are still delivered to the game | `SdlPlatform.cs:365-367`, `RaylibPlatform.cs:147` | desktop, single binding | Engine action with a rebindable/disableable default |
| 16 | Straight alpha everywhere | `Color` docs, blend mapping `SdlPlatform.cs:836-842` | colour convention | Declared convention (R5) |
| 17 | Overlay text uses each backend's debug font | `SdlTouchScreen.cs:217-226`, `RaylibTouchScreen.cs:125-132` | backend-specific look | Engine font on a screen view |
| 18 | Gamepad subsystem starts inside `EndFrame` | `SdlPlatform.cs:445` | render ↔ input | Input provider lifecycle |
| 19 | Touch overlay is drawn with the SDL renderer | `SdlPlatform.cs:164,442` | touch ↔ renderer | Overlay as a view (R7) |
| 20 | One renderer, one canvas, one camera offset per frame; single-view/single-player screen | `Platform.cs:431`, `IGraphics.cs:141-147` | no split-screen, no multi-view | Views (R7) |

---

## 5. Answers to the brief's questions

- **Does `IRenderer` bake in SDL_Renderer/raylib limits?** Worse: it bakes in a subset of them (R1). Shaders,
  materials, depth and matrices are missing; render targets, textured geometry, premultiplied/custom blend and address
  modes exist in both backends but are hidden.
- **Where are pixel-art assumptions embedded?** §4 items 1, 3-6, 11-15; most collapse into `CanvasScaling` and record
  defaults.
- **Texture/sampler separation, lifetime, device loss, upload path?** Not separated; lifetime is explicit dispose with
  a device sweep at shutdown; device loss unhandled; upload is synchronous CPU→GPU from RGBA8 only (R4).
- **2D-only math?** `Vector2` from System.Numerics, engine `RectF`, no matrices, translation-only camera (R8). The math
  library choice is fine; the missing piece is transforms in the contract.
- **Could 3D, SDL_GPU or a custom renderer be added without breaking the contract?** No. A custom renderer can only be
  added as a whole `IPlatform` (stub: ~85 lines without a window; a real window means forking `SdlPlatform`); SDL_GPU
  can be selected as an SDL_Renderer driver (`sdl-gpu` produced pixels identical to D3D11 in every probe) but its
  device/shaders are not reachable; 3D needs a new contract (R1, R2).
- **Is the window/renderer/device trio coupled necessarily?** Window↔swapchain/device binding: yes, by the APIs.
  Renderer↔device, and both ↔ input/touch/audio/pacing: no (R2).
- **Font path per backend?** Identical managed path for game text (R11); overlay text differs per backend (§4 #17).
- **Captures and determinism?** Deterministic per backend; not across backends (R3), and raylib captures are not
  opaque. Pixel-identical cross-backend output is not a reasonable target; a written rasterization contract plus
  tolerance/geometry-based conformance is (R3, R12).

---

## 6. Proposed target for this domain

Principles: layered (device → renderers → presentation), capability-tiered (a backend declares what it can do; the
engine and games query), explicit defaults expressed as presets, every resource validated in core, one written
rasterization/alpha contract.

### 6.1 Surface and device (RHI-lite)

```csharp
namespace MicroEngine.Graphics.Device;

/// Produced by the window host; consumed by a graphics backend. Raylib-style integrated backends implement both.
public interface ISurface
{
    nint NativeWindow { get; }            // opaque; only backends read it
    (int Width, int Height) PixelSize { get; }
    float PixelDensity { get; }
    event Action? Resized;
    event Action? Lost;                   // mobile surface destroyed, GPU reset
}

public interface IGraphicsBackendFactory
{
    string Name { get; }                                  // "sdl-renderer", "sdl-gpu", "webgl2", "headless-ref"
    string? UnsupportedReason(ISurface? surface);
    IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options);
}

[Flags] public enum GraphicsFeatures
{
    None = 0, RenderTargets = 1, TexturedGeometry = 2, AddressModes = 4, Mipmaps = 8, CustomBlend = 16,
    Shaders = 32, DepthBuffer = 64, Instancing = 128, ComputeShaders = 256, CompressedTextures = 512, FloatTextures = 1024,
}

public sealed record GraphicsCapabilities(GraphicsFeatures Features, int MaxTextureSize, IReadOnlySet<TextureFormat> Formats,
    ShaderFormat ShaderFormats, string ApiName, string? Adapter);

public interface IGraphicsDevice : IDisposable
{
    GraphicsCapabilities Capabilities { get; }
    event Action<DeviceLostReason>? DeviceLost;           // resources marked lost; restorable ones re-uploaded
    Texture CreateTexture(in TextureDescription desc, ReadOnlySpan<byte> initialData = default);
    RenderTarget CreateRenderTarget(in RenderTargetDescription desc);
    Sampler GetSampler(in SamplerDescription desc);        // immutable, cached
    GpuBuffer CreateBuffer(in BufferDescription desc);     // requires TexturedGeometry
    Shader CreateShader(in ShaderDescription desc);        // requires Shaders
    Pipeline CreatePipeline(in PipelineDescription desc);  // blend/depth/raster state + shaders
    CommandList BeginCommands();
    void Submit(CommandList commands);
    void Present();                                         // swapchain of the surface
}

public enum TextureFormat { Rgba8Unorm, Rgba8UnormSrgb, R8Unorm, Rg8Unorm, Rgba16Float, Bc7, Etc2Rgba8, Astc4x4, Depth24Stencil8 }
public enum AlphaMode { Premultiplied, Straight }
public readonly record struct TextureDescription(int Width, int Height, TextureFormat Format = TextureFormat.Rgba8Unorm,
    int MipLevels = 1, TextureUsage Usage = TextureUsage.Sampled, AlphaMode Alpha = AlphaMode.Premultiplied, string? Name = null);
public enum Filter { Nearest, Linear }
public enum AddressMode { Clamp, Repeat, Mirror }
public readonly record struct SamplerDescription(Filter Min = Filter.Nearest, Filter Mag = Filter.Nearest,
    Filter Mip = Filter.Nearest, AddressMode U = AddressMode.Clamp, AddressMode V = AddressMode.Clamp)
{
    public static readonly SamplerDescription PointClamp = new();
    public static readonly SamplerDescription LinearClamp = new(Filter.Linear, Filter.Linear, Filter.Linear);
}

/// Every resource knows its device; core validates ownership and disposal before a backend sees the call.
public abstract class GraphicsResource : IDisposable
{
    public IGraphicsDevice Device { get; }
    public bool IsDisposed { get; }
    public bool IsLost { get; }
    protected GraphicsResource(IGraphicsDevice device) { Device = device; }
    public void Dispose() { /* ... */ }
}

public sealed class CommandList
{
    public void BeginPass(RenderTarget? target, in PassClear clear);  // null = surface back buffer
    public void SetViewport(in RectF viewport);
    public void SetScissor(in RectF? scissor);
    public void SetPipeline(Pipeline pipeline);
    public void SetTexture(int slot, Texture texture, Sampler sampler);
    public void SetVertices(GpuBuffer buffer); public void SetIndices(GpuBuffer buffer);
    public void SetUniforms<T>(int slot, in T data) where T : unmanaged;
    public void DrawIndexed(int indexCount, int firstIndex = 0, int baseVertex = 0, int instances = 1);
    public void EndPass();
}
```

Tier 0 backends (SDL_Renderer, raylib 2D batch) implement a fixed set of built-in pipelines (sprite, coloured geometry)
and reject `CreateShader`; tier 1 (SDL_GPU on native, WebGPU or WebGL2 in browsers) implement everything. A
`headless-ref` backend is a CPU rasterizer implementing the written rules (R12).

### 6.2 2D batcher on top (today's `IRenderer` becomes its façade)

```csharp
namespace MicroEngine.Graphics;

public enum SortMode { Deferred, Immediate, Texture, BackToFront, FrontToBack }

public sealed record BlendState(BlendFactor SrcColor, BlendFactor DstColor, BlendOp ColorOp,
    BlendFactor SrcAlpha, BlendFactor DstAlpha, BlendOp AlphaOp)
{
    public static readonly BlendState AlphaPremultiplied = /* One, InvSrcAlpha */ default!;
    public static readonly BlendState Additive = default!;
    public static readonly BlendState Multiply = default!;
}

public readonly record struct BatchSettings(Matrix3x2 Transform, BlendState? Blend = null, SamplerDescription? Sampler = null,
    SortMode Sort = SortMode.Deferred, Material? Material = null, RectF? Scissor = null);

public interface IRenderer2D            // implemented once, in core, over IGraphicsDevice
{
    RenderTarget? Target { get; }
    int Width { get; }
    int Height { get; }
    void Begin(in BatchSettings settings);
    void End();
    void PushTransform(in Matrix3x2 transform);
    void PopTransform();
    void PushState(BlendState? blend = null, SamplerDescription? sampler = null, RectF? scissor = null);
    void PopState();
    void Draw(Texture texture, RectF source, in Matrix3x2 world, Color tint, float depth = 0);
    void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default); // today's
    void DrawGeometry(Texture? texture, ReadOnlySpan<Vertex2D> vertices, ReadOnlySpan<ushort> indices);
    void FillRect(RectF rect, Color color);   // and the rest of today's primitives as helpers over DrawGeometry
}

/// Kept for source compatibility: today's 13 members, implemented by an adapter over IRenderer2D.
public interface IRenderer { /* unchanged members */ }

public sealed class Material                 // requires GraphicsFeatures.Shaders; a no-op name on tier 0
{
    public Shader? FragmentShader { get; init; }
    public void Set<T>(string name, in T value) where T : unmanaged;
    public void Set(string name, Texture texture, SamplerDescription sampler);
}
```

### 6.3 Cameras, transforms and views

```csharp
namespace MicroEngine.Graphics;

public interface ICamera
{
    Matrix4x4 View { get; }
    Matrix4x4 Projection(float aspect);
}

public sealed class Camera2D : ICamera          // today's follow/dead zone/shake kept; zoom, rotation added
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; } = 1;
    public float Rotation { get; set; }
    public PixelSnap Snap { get; set; } = PixelSnap.None;   // presets turn it on for pixel art
    public Matrix3x2 WorldToView { get; }
    public Matrix4x4 View { get; }
    public Matrix4x4 Projection(float aspect) => default;
}

public enum StretchMode { None, Canvas, Viewport }               // Godot's disabled / canvas_items / viewport
public enum AspectPolicy { Stretch, Keep, KeepWidth, KeepHeight, Expand }
public enum ScaleRounding { Fractional, Integer, IntegerUnlessWasteful }
public sealed record Presentation(StretchMode Stretch, AspectPolicy Aspect, ScaleRounding Rounding,
    Filter PresentFilter, float WastefulThreshold = 0.8f)
{
    public static Presentation PixelArt { get; } = new(StretchMode.Viewport, AspectPolicy.Keep, ScaleRounding.IntegerUnlessWasteful, Filter.Nearest);
    public static Presentation Smooth { get; } = new(StretchMode.Viewport, AspectPolicy.Keep, ScaleRounding.Fractional, Filter.Linear);
    public static Presentation Native { get; } = new(StretchMode.Canvas, AspectPolicy.Expand, ScaleRounding.Fractional, Filter.Linear);
}

public sealed record View(string Name, (int Width, int Height)? Resolution, Presentation Presentation, ICamera? Camera, int Order);
```

`GameSettings` keeps one-line presets (`Presentation = Presentation.PixelArt`, `DefaultSampler = PointClamp`), each
field overridable; `CanvasScaling` becomes an alias for the first two presets during the transition.

### 6.4 Now / prepare / later

| Horizon | Work | Why now |
| --- | --- | --- |
| **Now** (before more API ships; compatible with current backends) | Write the rasterization/alpha/resource contract and make backends agree on it (clear vs clip, pixel centres, alpha write, disposed and foreign resources fail in core); premultiplied alpha decision; break the §4 couplings into explicit settings with presets (`Presentation`, `DefaultSampler`, capture scale, font sampler); move F11 to a rebindable engine action; blend/clip state stack; move `FrameLayout` out of Input; device-reset handler that at least reports; fix the `rendering.md` font-filter statement | Cheap, removes hidden behaviour, every later layer depends on these rules |
| **Prepare** (GRAPHICS-1 foundation) | Split `IPlatform` into window host / input / audio / graphics backend (`ISurface`, `IGraphicsBackendFactory`, `PlatformParts.Graphics`); `IGraphicsDevice` v2 with capabilities, `TextureDescription`, samplers, render targets and textured geometry implemented on SDL_Renderer and raylib (tier 0); 2D batcher with `Matrix3x2` transforms and the `IRenderer` adapter; `Camera2D` zoom/rotation; views and compositor (HD UI over pixel world; overlay as a view); CPU reference rasterizer for conformance tests | Needed before 2.5D and before adding a second device; keeps games source-compatible |
| **Later** | SDL_GPU device (native) and WebGPU/WebGL2 device (browser) as tier 1: shaders (SPIR-V/DXIL/MSL via SDL_shadercross or precompiled), materials, depth, instancing; 3D renderer and `Camera3D` on the same device; GPU texture formats/mipmaps from the asset pipeline; SDF/MSDF text; linear colour/HDR | Large, measured work; the contracts above let it land without breaking 2D games |

---

## 7. Cross-domain questions

1. **Core/lifecycle:** should the frame be `BeginFrame/EndFrame` on the platform, or an engine-owned render graph of
   views submitted to a device? Who owns presentation pacing once present moves to the device?
2. **World/Entities:** which transform type is shared (a `Transform2D` struct over `Matrix3x2`, and a 3D one), and
   does the renderer consume a scene graph or only draw commands?
3. **Assets:** with `TextureDescription`/formats/mips, does the pipeline move to build-time processing (premultiply,
   mip generation, GPU formats per platform), reopening ADR 0025's "files ship as they are"?
4. **Input:** pointer mapping depends on presentation (`SdlPlatform.ToCanvas`, `FrameLayout.ToCanvas`); with multiple
   views, which view receives pointer coordinates? F11 as an engine action belongs to which map?
5. **Platforms:** raylib fuses window, GL context and input — keep it as an "integrated backend", or replace browsers'
   path with SDL3 on emscripten + WebGL2/WebGPU to have one window host everywhere?
6. **Infrastructure/validation:** replace per-backend hashes with a written contract + CPU reference rasterizer +
   per-backend tolerance/geometric invariants? Captures must stop depending on `CanvasScaling` and must be opaque.
7. **UI:** `RendererExtensions.Panel`/gradients and `UiTheme` are pixel-unit; does UI move to a screen-resolution view
   with its own scale factor?
