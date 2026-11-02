# R1 — Rendering architecture and platform/graphics abstraction: industry research

Phase I architecture audit of MicroEngine. Date of research: 2026-11-02.
Scope: how other engines and graphics layers solved rendering and platform abstraction, what they reversed, and what
transfers to MicroEngine (C#/.NET 10, 2D today; SDL3 `SDL_Renderer` native, raylib/WebGL in browsers, headless;
immediate-mode `IRenderer`, CPU `PixelImage` uploaded as textures, virtual canvas with integer scaling; ADR 0017,
ADR 0024 already defer a capability-based renderer to a "GRAPHICS-1" mission on SDL_GPU).

Conventions: **[F]** = documented fact with a source; **[I]** = inference or interpretation by this report;
**[U]** = unverified / sources conflict. Repository files read (read-only): `docs/architecture.md`,
`docs/decisions/0017-sdl3-native-platform.md`, `docs/decisions/0024-graphics-configuration.md`.

---

## 1. SDL3: SDL_Renderer, SDL_GPU, shadercross, main callbacks

### 1.1 SDL_GPU — what it is
- [F] SDL_GPU is a modern explicit-ish GPU API: create a device, claim a window (or render offscreen), create static
  resources up front, then per frame acquire a command buffer, encode render/compute/copy passes and submit. Pipelines
  are precompiled state objects; samplers are separate objects; uploads go through transfer buffers; readback uses
  download + fence; four uniform slots per stage; "cycling" gives ring-buffer semantics for resources still in use;
  coordinate system is left-handed like D3D12/Metal. Render passes can target up to four color textures and one depth
  texture. (https://wiki.libsdl.org/SDL3/CategoryGPU)
- [F] Backends: Vulkan (Windows, Linux, Switch, "certain Android devices"), D3D12 (Windows 10+, Xbox), Metal (macOS
  10.14+, iOS/tvOS 13+ on A9+; **iOS/tvOS simulators unsupported**). (https://wiki.libsdl.org/SDL3/CategoryGPU)
- [F] Origin: commit by thatcosmonaut (Evan Hemsley, lead; the MoonWorks/Refresh lineage), API co-designed by Caleb
  Cornett, Ethan Lee (FNA) on production/QA, Andrei Alexeyev on the SDL_Render driver; Ryan C. Gordon credited for
  the original proposal. Committed to SDL on 2024-08-29. (https://discourse.libsdl.org/t/sdl-add-the-sdl-gpu-api/53845)
- [F] **Reversal**: the D3D11 backend shipped in previews was removed (PR #11456, Nov 2024) because it relied on
  deferred contexts with "spotty hardware support", forced "awkward feature compromises", served a dwindling market,
  and "the increased maintenance burden of this backend does not seem worth it"; D3D12 was taught to ingest DXBC
  instead. (https://github.com/libsdl-org/SDL/pull/11456)
- [I] Consequence: SDL_GPU has *no* GL/GLES/WebGL fallback by design. Android devices without a conformant Vulkan
  driver, iOS simulators and browsers cannot use it today.

### 1.2 SDL_GPU on the web — current status (Nov 2026)
- [F] Feature request #10768 "SDL3 GPU Backend for WebGPU Target" (opened 2024-09) is still open; an earlier PR #12046
  was closed (2025-03). (https://github.com/libsdl-org/SDL/issues/10768, https://github.com/libsdl-org/SDL/pull/12046)
- [F] PR #16020 "GPU: Experimental WebGPU SDLGPU Backend" (opened 2026-07-18, still open, draft): runs the 34 SDL_GPU
  examples on Windows/Linux/Web in Chromium; Firefox mostly; Safari/macOS/iOS/Android untested; native path uses Dawn
  (wgpu-native dropped Aug 2026); WGSL only, through the author's own partial parser ("doesn't support all valid WGSL");
  SPIR-V→WGSL in shadercross is future work; no thread safety, no cycling, no mipmap generation. Maintainer
  flibitijibibo: "WebGPU is for sure going to be at the bottom of the driver list", and the C++ (Dawn) dependency
  means it cannot be on by default. No merge timeline. (https://github.com/libsdl-org/SDL/pull/16020)
- [I] Do not plan MicroEngine's browser path on SDL_GPU before this PR is merged, released, and has a shader story
  through shadercross. Realistic earliest: an SDL 3.6-era release; treat as **unknown**.

### 1.3 SDL_Renderer and its GPU driver
- [F] SDL 3.4.0 (2026-01-01) added `SDL_CreateGPURenderer()`/`SDL_GetGPURendererDevice()` — a 2D renderer running on
  SDL_GPU — plus `SDL_CreateGPURenderState()`/`SDL_SetGPURenderState()`/`SDL_SetGPURenderStateFragmentUniforms()`
  for **custom fragment shaders** in the 2D renderer, YUV/HDR in the GPU 2D renderer, creating an SDL texture from an
  existing GPU texture (`SDL_PROP_TEXTURE_CREATE_GPU_TEXTURE_POINTER`), and `SDL_SCALEMODE_PIXELART`. Later 3.4.x
  point releases fixed multi-threaded crashes on Vulkan/Metal (3.4.6) and render-target switching artifacts on Vulkan
  (3.4.12). Latest seen: 3.4.18 (2026-10-02). (https://github.com/libsdl-org/SDL/releases,
  https://wiki.libsdl.org/SDL3/SDL_GPURenderStateCreateInfo)
- [F] Render state create info holds one fragment shader, extra sampler bindings, storage textures and storage buffers.
  (https://wiki.libsdl.org/SDL3/SDL_GPURenderStateCreateInfo)
- [F] `SDL_SetRenderLogicalPresentation` provides letterbox/integer-scale presentation; warning against applying
  logical presentation both to a render target and to the window ("double-letterboxing").
  (https://wiki.libsdl.org/SDL3/SDL_SetRenderLogicalPresentation)
- [F] Forum report: `SDL_SCALEMODE_PIXELART` is good at integer scales, weaker at non-integer scales.
  (https://discourse.libsdl.org/t/strategies-for-scaling-on-high-dpi-displays-in-sdl-3/65838)
- [I] This is the **bridge** MicroEngine needs: the same `SDL_Renderer` API it already uses can be backed by SDL_GPU
  where available, gain custom fragment shaders (post effects, palette swaps, CRT, lighting composites), and share a
  device with hand-written SDL_GPU passes (via `SDL_GetGPURendererDevice` and GPU-texture interop) — without dropping
  the GLES/D3D11/software drivers that SDL_Renderer still offers for weak devices.
- [I] Limit: SDL_Renderer's custom-shader path exists only on the `gpu` driver; the shader must be supplied in that
  backend's format (SPIR-V/DXIL/MSL), so MicroEngine still needs a shader cross-compile step to use it.

### 1.4 SDL_shadercross
- [F] Library + CLI translating HLSL or SPIR-V into DXBC, DXIL, SPIR-V, MSL or HLSL; runtime or offline (offline
  preferred); depends on SPIRV-Cross and DXC (dxcompiler/dxil) or vkd3d-utils for DXBC on non-Windows; no tagged
  release visible. (https://github.com/libsdl-org/SDL_shadercross, https://wiki.libsdl.org/SDL3/CategoryGPU)
- [I] For a code-first .NET engine, ship **precompiled** per-backend blobs produced at build time (MSBuild task or
  `microengine` CLI step) rather than runtime translation: runtime shadercross drags DXC/SPIRV-Cross native binaries
  onto every platform, including mobile.

### 1.5 SDL3 main callbacks
- [F] `SDL_AppInit/SDL_AppIterate/SDL_AppEvent/SDL_AppQuit`: exist because iOS wants `main` to return and be called
  back, Emscripten requires the browser to own the loop, and Wayland can drive frame timing; on other platforms SDL
  simulates the loop so the same code runs everywhere. Do not block in `SDL_AppInit`.
  (https://wiki.libsdl.org/SDL3/README-main-functions)
- [I] MicroEngine already runs iOS through `SDL_RunApp` and has a "page-driven host that cannot block" in the browser
  (ADR 0024). A single callback-shaped loop (`Init/Iterate/Event/Quit`) in the engine core, with the blocking loop as
  a simulation for desktops, removes a class of per-host divergence. This matters more than the graphics API choice.

---

## 2. FNA / FNA3D and MonoGame

### 2.1 FNA3D
- [F] FNA3D (2020) split XNA's graphics into a C library with runtime-selectable drivers. At FNA 20.09: D3D11 default
  on Windows, Metal on Apple, Vulkan experimental; Metal was deprecated and removed in 22.01 in favour of
  Vulkan/MoltenVK. (https://www.gamingonlinux.com/2020/09/fna-2009-is-out-with-the-new-fna3d-brings-experimental-vulkan-support/,
  https://github.com/FNA-XNA/FNA3D/releases?page=4)
- [F] Today FNA3D lists "SDL_GPU, OpenGL, and Direct3D" implementations and requires SDL 3.2.0+; the `src/` tree has
  `FNA3D_Driver_SDL.c`, `FNA3D_Driver_OpenGL.c`, `FNA3D_Driver_D3D11.c` — the standalone Vulkan driver file is gone.
  Shaders remain D3D9 Effect binaries through MojoShader. (https://github.com/FNA-XNA/FNA3D,
  https://github.com/FNA-XNA/FNA3D/tree/master/src)
- [I] Lesson: the FNA team, who co-built SDL_GPU, **replaced their own Vulkan (and earlier Metal) drivers** with the
  SDL_GPU driver, keeping OpenGL for legacy/web and D3D11 for compatibility. Maintaining one backend per API was the
  cost they chose to stop paying. Their shader compatibility (D3D9 FX bytecode via MojoShader) is an XNA-legacy
  constraint MicroEngine does not have and should not copy.

### 2.2 MonoGame
- [F] MonoGame 3.8.5 (2026-07-15) adds **DesktopVK** (SDL2 + Vulkan + FAudio, from Stadia-era code), intended to
  "eventually replace our DesktopGL platform", and **WindowsDX12** (D3D12, GDK-compatible), expected to replace the
  D3D11 target "in the coming few years". It names the old dependency model as a problem — around a dozen C# wrappers
  (OpenTK, SharpDX…), upstream delays, forks, handwritten GL P/Invokes — and moves to a single native C/C++ library.
  The content pipeline is being replaced by a code-centric Content Builder ("early days"); 3.9 promises "a new unified
  shader compiler". (https://monogame.net/blog/2026-07-15-3.8.5-release-2026/)
- [F] DX12 and Vulkan were promoted to release platforms after extra hardening; 3.8.5.1 followed with DX12/Vulkan fixes.
  (https://gamedev.net/news/4745-monogame-v385-released/)
- [I] MonoGame's long-standing regrets, visible in this release: per-platform packages (DesktopGL vs WindowsDX) that
  behave differently, a shader toolchain (MGFX) tied to platform-specific compilers, a data-file content pipeline, and
  many managed wrappers. MicroEngine already avoided most of these (code-first content, one SDL binding). Keep it that
  way: one native platform library, one shader build step, no per-platform game projects.

### 2.3 Murder engine
- [F] Murder is "a pixel art ECS game engine built on top of FNA", with the Bang ECS. (https://github.com/isadorasophia/murder)
- [I] Evidence that a pixel-art-first engine can live on an XNA-style SpriteBatch device without a render graph.

---

## 3. Godot: from Visual Server to RenderingServer + RenderingDevice

- [F] Godot 4's Vulkan move required re-implementing the renderer "entirely from scratch" over two years; GLES2 was
  dropped as a 4.x target and GLES3 promised for 4.1, as a separate, simpler renderer.
  (https://godotengine.org/article/about-godot4-vulkan-gles3-and-gles2/)
- [F] Three renderers today: Forward+ (desktop), Mobile (tile GPUs, sub-passes), Compatibility (OpenGL; the only
  choice on the web, via WebGL 2). Forward+/Mobile run on **RenderingDevice** over Vulkan, D3D12 or Metal; since 4.4
  they fall back Vulkan↔D3D12 and then to Compatibility. "Compatibility is usually good enough for 2D".
  (https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html)
- [F] RenderingDevice "presents a similar level of abstraction as WebGPU"; Compatibility does **not** use it and its
  core shaders are "entirely different". Shaders: GLSL source; D3D12 via SPIR-V→DXIL with Mesa NIR; Metal via
  SPIRV-Cross. 2D renders in LDR sRGB separate from 3D HDR linear. (https://docs.godotengine.org/en/stable/engine_details/architecture/internal_rendering_architecture.html)
- [F] Servers: singletons implementing rendering, physics, audio; scene nodes sit above them and talk via RIDs
  (opaque handles). (https://docs.godotengine.org/en/stable/engine_details/architecture/godot_architecture_diagram.html)
- [F] **2D batching was lost in the 4.0 rewrite** on the RD renderers and restored only in 4.4 (PR #92797, merged
  2024-09-11, borrowing the Compatibility renderer's batcher). Example: a huge TileMap went from >573,440 draw calls to
  36 (≈10→130 FPS on M1); the idle Vulkan editor burned CPU due to missing batching; follow-ups: a subpixel AA
  regression and exceeding the guaranteed 4 descriptor sets, which broke some mobile devices.
  (https://github.com/godotengine/godot/pull/92797)
- [F] Pixel art: stretch mode `viewport` (render at base size, then scale) vs `canvas_items` (render 2D at target
  resolution); `integer` stretch scale mode added in 4.2; recommended pixel-art set-up: small base (640×360),
  `viewport`, `keep`/`expand`, `integer`. (https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html)
- [F] "Snap 2D transforms/vertices to pixel" are an ongoing source of jitter, 1-px gaps and confusion; 4.3 simplified
  the snapping to one rounding per CanvasItem. (https://forum.godotengine.org/t/snap-2d-transforms-to-pixel/118943,
  https://forum.godotengine.org/t/aiming-for-pixel-perfection-in-godot/79368,
  https://forum.godotengine.org/t/characterbody2d-with-snap-2d-transforms-to-pixel-enabled-causes-1px-gap/897)

Lessons [I]:
1. Godot ended with **two renderers on two abstractions** (RD and OpenGL) because the web and old devices could not be
   served by the modern one. That is the "inevitable complexity" of covering the web before WebGPU is universal.
2. A rewrite to a modern device silently regressed the 2D path for two releases. Any MicroEngine move to SDL_GPU must
   carry the batcher and the draw-call count as **measured, tested invariants** (benchmarks of tile maps, particle
   counts) from day one.
3. MicroEngine's integer-scaled virtual canvas is exactly Godot's recommended `viewport` + `integer` mode; keep it
   as the default. Expose a separate "HD / canvas_items" style path (draw at output resolution with a camera scale)
   for smooth games rather than snapping knobs per node.

---

## 4. Portable graphics layers

| Layer | Backends | Web | Shaders | Status / notes |
|---|---|---|---|---|
| raylib `rlgl` | GL 1.1, 2.1, 3.3, 4.3, ES2, ES3 (+ software option) | WebGL via ES | GLSL per GL version | Immediate-mode pseudo-GL 1.1 API; default batch 8192 elements (2048 on ES2), 256 draw calls per batch, 32-deep matrix stack. GL only. (https://raw.githubusercontent.com/raysan5/raylib/master/src/rlgl.h) |
| sokol_gfx | GLES3/WebGL2, GL 3.3, D3D11, Metal, WebGPU, Vulkan (experimental) | WebGL2 and WebGPU | sokol-shdc offline cross-compiler generating per-backend headers | No runtime translation; resources are pooled handles. (https://github.com/floooh/sokol, https://floooh.github.io/2018/06/17/handles-vs-pointers.html) |
| bgfx | D3D11/12, Metal, Vulkan, GL 2.1–ES 3.1, WebGL 1/2, WebGPU (Dawn native only) | WebGL | shaderc (custom GLSL dialect) | Multithreaded renderer by default, view/sort-key submission. (https://bkaradzic.github.io/bgfx/overview.html) |
| wgpu / wgpu-native | Vulkan, Metal, D3D12, GL/GLES (best effort, ANGLE on Apple), WebGPU on web, WebGL2 via naga→GLSL | Both | WGSL, naga translates | Rust; C bindings via wgpu-native. (https://github.com/gfx-rs/wgpu) |
| Veldrid (.NET) | D3D11, Vulkan, Metal, GL 3, GLES 3 | No | SPIR-V via Veldrid.SPIRV | Author stopped public updates Feb 2023. (https://github.com/veldrid/veldrid) |
| Silk.NET (.NET) | Bindings: GL, Vulkan, DirectX, WebGPU, SDL, … | Not documented | n/a (bindings) | 3.0 rewrite in progress; 2.x ad hoc. (https://github.com/dotnet/Silk.NET) |
| SDL_GPU | Vulkan, D3D12, Metal | Not yet (PR #16020) | SPIR-V/DXIL/DXBC/MSL; shadercross | Stable ABI since SDL 3.2; actively fixed. |

Observations [I]:
- Every layer that serves both web and native **today** carries a GL/WebGL backend (sokol, bgfx, wgpu, Godot
  Compatibility, raylib). The only modern-only layer (SDL_GPU) has no web.
- All successful layers rely on **offline shader cross-compilation** (sokol-shdc, bgfx shaderc, naga, shadercross,
  Godot's build-time GLSL embedding). Runtime translation is the exception.
- .NET's own portable layer (Veldrid) went dormant; binding-level options (Silk.NET) shift all abstraction work onto
  the engine. A .NET engine is better served by binding a maintained C layer (SDL_GPU, sokol, wgpu-native) than by
  writing its own RHI.
- The Forge (not researched in depth; https://github.com/ConfettiFX/The-Forge) is a high-end cross-platform renderer
  framework aimed at AAA/console, not a fit for MicroEngine's scope [I].

---

## 5. Render architecture patterns

### 5.1 Render-world extraction (Bevy)
- [F] Bevy runs rendering in a separate ECS world; the Extract stage is the only sync point copying main-world data;
  then Prepare/Queue/PhaseSort/Render; motivation: pipelined rendering of frame N while simulating N+1.
  (https://bevy-cheatbook.github.io/gpu/intro.html; pipelined rendering issue https://github.com/bevyengine/bevy/issues/5082)
- [F] **Reversal**: 0.15 made the render world *retained* ("We no longer clear each frame"), syncing entities with
  `SyncToRenderWorld` instead of re-extracting everything. (https://bevy.org/news/bevy-0-15/)
- [F] 0.17 decoupled user-facing APIs (cameras, sprites, meshes, UI) from `bevy_render` so third-party renderers can
  replace it; new `RenderStartup` schedule. (https://bevy.org/news/bevy-0-17/)
- [I] Extraction pays off when simulation and rendering run on different threads and the scene is large. For a 2D
  engine whose draw is already a cheap immediate-mode walk, it adds duplication; Bevy itself backed off the
  "re-extract everything each frame" form. Lesson for MicroEngine: separate *what to draw* (data) from *how* (backend)
  by recording an immediate-mode **command list** per frame — a lightweight, thread-handoff-capable form of extraction
  — rather than a second world.

### 5.2 Render graphs (Unreal RDG, Unity URP)
- [F] Unreal RDG: immediate-mode recording of passes and resource descriptors into a graph, then compile/execute;
  automates split barriers, transient allocation/aliasing, culling of unused passes, async compute; sits **above** the
  RHI, whose command lists pass lambdas record into. (https://dev.epicgames.com/documentation/en-us/unreal-engine/render-dependency-graph-in-unreal-engine)
- [F] Unity: Built-In pipeline "is deprecated and will be made obsolete in a future release" (supported through 6.7
  LTS); URP/HDRP are SRPs; in URP render graph is on by default and the non-render-graph path is no longer developed;
  HDRP enters maintenance in Unity's 2026 strategy. (https://docs.unity.com/en-us/engine/current/manual/render-pipelines/overview,
  https://unity.com/topics/render-pipelines-strategy-for-2026, https://docs.unity3d.com/Manual//urp/upgrade-guide-unity-6.html)
- [I] Unity's decade-long split (Built-in → URP + HDRP → converge on URP) is the cautionary tale: two
  scriptable pipelines with diverging feature sets fragmented assets, shaders and tutorials. Lesson: one pipeline with
  scalable features beats several pipelines. Render graphs solve barrier/aliasing problems that only appear with many
  intermediate targets; SDL_GPU already hides barriers. MicroEngine should **not** build a render graph now; a fixed,
  ordered list of passes (scene → lights → post → UI → present) covers 2D and simple 3D.

### 5.3 Resource handles, samplers, lifetime
- [F] sokol-style pooled handles with generation counters detect use-after-free and allow resources to be relocated.
  (https://floooh.github.io/2018/06/17/handles-vs-pointers.html)
- [F] SDL_GPU, WebGPU, Vulkan, D3D12, Metal all separate sampler objects from textures; Godot uses RIDs.
  (https://wiki.libsdl.org/SDL3/CategoryGPU, https://docs.godotengine.org/en/stable/engine_details/architecture/godot_architecture_diagram.html)
- [I] MicroEngine currently fixes the filter at texture creation (`IGraphicsDevice`: "turning PixelImages into
  textures with a filter"). Any future device API should model **sampler state separately** (filter, wrap) and let a
  draw choose it, otherwise the same atlas cannot be drawn nearest in-game and linear in a zoomed UI. Textures should be
  value-type handles owned by the device, with explicit `Dispose` and deferred destruction after the frame that last
  used them (SDL_GPU cycling/WebGPU semantics).

### 5.4 2D batching on a 3D-capable device
- [F] Godot's RD batcher lifted from the GL renderer, and its failures (descriptor limits on mobile) show the batcher
  is the core of a 2D renderer on a modern API. (https://github.com/godotengine/godot/pull/92797)
- [F] rlgl's batch limits (8192 elements, 256 draw calls per batch) define raylib's 2D throughput envelope.
  (https://raw.githubusercontent.com/raysan5/raylib/master/src/rlgl.h)
- [I] Pattern: sprites → vertices in a CPU-side dynamic vertex buffer (cycled each frame), sorted/merged by
  (texture, sampler, blend, shader), one draw per run; atlases keep runs long. The same device draws 3D meshes with a
  depth buffer in an earlier pass. SDL_Renderer's GPU driver already implements this batcher; MicroEngine only needs
  its own when it must mix 2D with its own 3D passes or exceed SDL_Renderer's feature set.

### 5.5 Render targets and resolution policy
- [F] Godot `viewport` + `integer` = render at base resolution into an offscreen target, scale by whole factors;
  `canvas_items` = draw at output resolution. (https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html)
- [F] SDL logical presentation provides integer scaling; `SDL_SCALEMODE_PIXELART` helps at non-integer scales only
  partially. (https://wiki.libsdl.org/SDL3/SDL_SetRenderLogicalPresentation, https://discourse.libsdl.org/t/strategies-for-scaling-on-high-dpi-displays-in-sdl-3/65838)
- [I] Keep MicroEngine's own canvas target (not SDL's logical presentation) so the same policy exists on raylib and
  headless; add `PixelArt` filtering as an option for non-integer fallback (e.g. phones where integer fit wastes too
  much screen). Pixel-perfect vs smooth should be a **canvas policy**, not per-sprite snapping flags (Godot's snap
  settings are a recurring support burden).

### 5.6 Camera/transform math in .NET engines
- [F] Stride keeps its own `Stride.Core.Mathematics`; discussion #2210 proposes moving to System.Numerics; historic
  reasons: System.Numerics did not exist / was slower, lacked by-ref overloads; layout-compatible types require
  `Unsafe.As` for interop. (https://github.com/stride3d/stride/discussions/2210, https://forums.stride3d.net/t/using-system-numerics/219,
  https://github.com/stride3d/stride/issues/2)
- [F] `System.Numerics.Matrix4x4` exposes `CreatePerspectiveFieldOfView` and a separate `...LeftHanded` overload
  (added in recent .NET), i.e. right-handed by default. (https://learn.microsoft.com/en-us/dotnet/api/system.numerics.matrix4x4.createperspectivefieldofview)
- [F] FNA/MonoGame inherit XNA's own `Microsoft.Xna.Framework.Matrix` (row-vector, right-handed) [F for XNA heritage;
  convention per XNA docs, not re-fetched here — **[U]**].
- [I] Use System.Numerics (`Vector2/3/4`, `Matrix3x2` for 2D, `Matrix4x4`, `Quaternion`) in public APIs from the
  start: SIMD-accelerated, zero-dependency, interoperable with Silk.NET and most .NET libraries; Stride's own-math
  choice is now debt. Document conventions once: System.Numerics uses row vectors (`v * M`), so matrices compose
  left-to-right; SDL_GPU clip space is left-handed with depth 0..1 (D3D/Metal/WebGPU style) — pick projection helpers
  accordingly and test them with a known-frame capture. Note: Matrix4x4 row-vector convention is per .NET docs
  **[U]: not re-fetched in this session**.

---

## 6. Web graphics in 2026

- [F] WebGPU status (gpuweb implementation status wiki): Chromium shipped on Windows/macOS/ChromeOS (113), Android 12+
  on ARM/Qualcomm/Intel (121), Linux only on Intel Gen12+ (144) and NVIDIA under Wayland (147), Windows ARM64 behind a
  flag; Firefox shipped on Windows (141) and macOS (145/147), Linux Nightly only, Android behind a flag (both expected
  in 2026); Safari 26 on macOS, iOS/iPadOS, visionOS enabled by default.
  (https://github.com/gpuweb/gpuweb/wiki/Implementation-Status)
- [F] Spec still a W3C Candidate Recommendation Draft. (https://en.wikipedia.org/wiki/WebGPU)
- [I] WebGL2 remains the only universal baseline in Nov 2026 (Linux desktops, many Android GPUs, Firefox on
  Linux/Android). Every engine surveyed that ships on the web keeps a WebGL2 path (Godot Compatibility, wgpu "webgl"
  feature, sokol GLES3, bgfx, raylib).
- [F] .NET WebAssembly links native C/C++ through Emscripten using `NativeFileReference` (.c, .o, .a, .bc, .wasm);
  prebuilt archives must match the Emscripten version used by the .NET runtime; native packages carry `browser-wasm`
  assets. SkiaSharp is the reference example. (https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-native-dependencies)
- [I] So the options for MicroEngine on the web are: (a) keep raylib (GL ES via WebGL), (b) build **SDL3 for
  Emscripten** with .NET's Emscripten version and use `SDL_Renderer`'s GLES2/WebGL driver — unifying the API with
  native (SDL 3.4 lists improved Emscripten support, `SDL_WINDOW_FILL_DOCUMENT`, persistent storage),
  (c) wgpu-native/Dawn WebGPU with WebGL2 fallback (two web paths, C++/Rust toolchain in the build), (d) SDL_GPU WebGPU
  once merged. ADR 0017 already notes SDL code ships unused in browser packages (~150 KB Brotli).

---

## 7. Cross-cutting lessons

### Problems solved, mistakes, reversals
| Who | Initial design | Mistake / cost found | Reversal / rebuild |
|---|---|---|---|
| SDL_GPU | D3D11 + D3D12 + Vulkan + Metal | D3D11 deferred contexts unreliable, maintenance | D3D11 removed before 3.2 |
| FNA3D | Own D3D11/Metal/Vulkan/GL drivers | Metal driver, then Vulkan driver upkeep | Metal dropped (22.01); SDL_GPU driver replaces own Vulkan |
| MonoGame | Per-API platforms over many C# wrappers | Divergence, wrapper rot, forks | Single native library; DesktopVK/DX12 to replace GL/DX11; code-first content builder |
| Godot | GLES2+GLES3 (3.x), Vulkan-only plan for 4.0 | Web and old GPUs unreachable; 2D batching lost | GLES3 Compatibility re-added (4.1); RD batching restored (4.4); Vulkan↔D3D12 fallback (4.4) |
| Bevy | Extract-and-clear render world | Per-frame re-extraction cost/complexity | Retained render world (0.15); renderer decoupled from user API (0.17) |
| Unity | Fixed Built-in pipeline → URP + HDRP | Two pipelines fragment ecosystem | Built-in deprecated; HDRP maintenance; converge on URP + render graph |
| Veldrid | .NET-owned RHI | Single-maintainer bus factor | Public development stopped (2023) |

### Proven solutions
- A thin, explicit device (SDL_GPU / WebGPU-level) + offline shader cross-compilation + a 2D batcher on top.
- Opaque handles for GPU resources, separate samplers, deferred destruction.
- Render at a fixed virtual resolution into an offscreen target and integer-scale it for pixel art.
- Callback-shaped main loop for iOS/web/Wayland.
- A capability query + automatic fallback chain (Godot 4.4, ADR 0024 "preferences fall back, demands do not").

### Inevitable complexity
- Until WebGPU is universal, a web-capable engine has **two GPU paths** (modern + GL/WebGL) or stays on a GL-class
  feature set everywhere.
- Shaders need a build step and per-backend blobs; reflection (binding counts per stage) must be carried with them.
- Mobile driver variance (Android Vulkan quality, descriptor limits) needs per-device fallback and on-device testing.

### What NOT to copy
- Writing a per-API renderer (FNA, MonoGame, Godot RD all paid for it; MicroEngine's ADR 0017 already rejected it).
- XNA/D3D9 effect files, MGCB-style data-file content pipelines.
- Multiple parallel high-level pipelines (Unity URP/HDRP).
- A render graph or a second ECS world before there are many intermediate targets and a threaded renderer.
- Per-node pixel-snap switches (Godot) instead of one canvas policy.
- Engine-owned math types (Stride).

---

## 8. Recommendation: graphics backend strategy for MicroEngine

### 8.1 Options
| Option | Native | Web | 3D-ready | Shaders | Cost | Main risk |
|---|---|---|---|---|---|---|
| A. Keep SDL_Renderer (native) + raylib (web) | Yes, all SDL drivers incl. GLES/software | raylib WebGL | No (2D only; geometry API at most) | none / raylib GLSL | Lowest | Ceiling: no custom shaders off the `gpu` driver, no depth, no 3D; two backends forever |
| B. SDL_Renderer on its `gpu` driver + SDL_GPU passes on the same device; GL-class SDL_Renderer drivers as fallback | Vulkan/D3D12/Metal, fallback GLES/D3D11/software (2D only) | raylib or SDL-Emscripten WebGL (2D only) until SDL_GPU WebGPU | Yes on SDL_GPU | SPIR-V/DXIL/MSL via shadercross, offline | Medium | Capability tiers: 3D and custom shaders absent on fallback devices and the web |
| C. WebGPU everywhere (wgpu-native or Dawn) | Vulkan/D3D12/Metal via wgpu | WebGPU, WebGL2 fallback via wgpu | Yes | WGSL (naga) | High | New native dependency (Rust/C++), mobile packaging, iOS simulator/Android maturity, binding maintenance in .NET, duplicates SDL's windowing integration |
| D. SDL_GPU everywhere, wait for WebGPU backend | Vulkan/D3D12/Metal | Only after PR #16020 ships | Yes | shadercross | Medium | Web timeline unknown; Android without Vulkan and iOS simulator excluded |

### 8.2 Recommended direction [I]
**Option B, staged, with D as its natural end state.**
1. Keep `IRenderer` (immediate mode) as the 2D contract; internally record a per-frame command list so backends can
   batch and, later, a render thread can consume it.
2. Introduce a separate, optional **capability-tiered device** (`IGpuDevice`-like) modelled on SDL_GPU/WebGPU
   concepts: buffers, textures, samplers, shaders, pipelines, render passes, command buffers; handles with generation
   counters; queried capabilities (`Shaders`, `RenderTargets`, `Depth`, `Compute`, `Instancing`). Tier 0 = current
   SDL_Renderer/raylib/headless; Tier 1 = SDL_GPU (via SDL_Renderer's `gpu` driver + `SDL_GetGPURendererDevice`).
3. Shaders: author in HLSL (or GLSL→SPIR-V), compile offline to SPIR-V/DXIL/MSL in the `microengine` tool / MSBuild,
   ship blobs + reflection JSON; never require shadercross at runtime on mobile.
4. Web: keep raylib as the Tier-0 browser path now; prototype SDL3-for-Emscripten via `NativeFileReference` to see if
   one SDL backend can serve all six platforms; revisit SDL_GPU on WebGPU when it is released, with WebGL2 kept as the
   fallback for years (Firefox Linux/Android, Linux Chrome).
5. 3D only on Tier 1; games query capabilities and degrade (as ADR 0024 already does for platform features).

### 8.3 Risks
- Two tiers mean two code paths for every effect that needs shaders; mitigate by keeping Tier-0 features a strict
  subset and testing both with the existing reference-frame captures.
- SDL_GPU on Android: Vulkan driver quality varies; needs device testing (ADR 0012 platform experiments).
- iOS simulator unsupported by SDL_GPU Metal → CI must test Tier 1 on device or macOS only.
- Shader toolchain (DXC, SPIRV-Cross) is native and platform-specific at build time; pin and cache it in CI.
- Coordinate/handedness mismatch between System.Numerics helpers and SDL_GPU clip space — pin with captures.
- SDL 3.4.x GPU renderer fixes are recent (multithread crash, Vulkan render-target artifacts) — track point releases.

### 8.4 Prototypes needed to decide
1. **SDL_Renderer `gpu` driver parity**: run the four demo games on the `gpu` driver vs default drivers on Windows
   (D3D12/Vulkan), Linux (Vulkan/llvmpipe→lavapipe), Android, macOS/iOS; compare reference frames, draw calls, frame
   time, start-up time.
2. **Custom fragment shader via `SDL_CreateGPURenderState`**: one palette-swap and one full-screen post effect,
   shaders compiled offline by shadercross for SPIR-V/DXIL/MSL; measure build-tool friction.
3. **Mixed 2D + SDL_GPU pass**: draw a depth-tested 3D mesh with SDL_GPU on the renderer's device, composite with
   SDL_Renderer 2D via GPU-texture interop; validates the 2.5D/3D path without a second device.
4. **SDL3 in the browser**: build SDL3 with .NET 10's Emscripten version, link via `NativeFileReference`, run one demo
   on SDL_Renderer/WebGL; compare size and frame time with raylib; check the main-callback/page-driven loop.
5. **WebGPU spike (optional)**: wgpu-native or SDL PR #16020 branch in a browser to estimate the future web Tier 1
   and the WGSL shader path; decide only after SDL's WebGPU backend lands.

---

## Sources
- SDL GPU category: https://wiki.libsdl.org/SDL3/CategoryGPU
- SDL_GPU commit announcement: https://discourse.libsdl.org/t/sdl-add-the-sdl-gpu-api/53845
- SDL PR #11456 (remove D3D11 backend): https://github.com/libsdl-org/SDL/pull/11456
- SDL issue #10768 (WebGPU backend request): https://github.com/libsdl-org/SDL/issues/10768
- SDL PR #12046 (closed WebGPU backend): https://github.com/libsdl-org/SDL/pull/12046
- SDL PR #16020 (experimental WebGPU backend): https://github.com/libsdl-org/SDL/pull/16020
- SDL releases: https://github.com/libsdl-org/SDL/releases
- SDL_GPURenderStateCreateInfo: https://wiki.libsdl.org/SDL3/SDL_GPURenderStateCreateInfo
- SDL_SetRenderLogicalPresentation: https://wiki.libsdl.org/SDL3/SDL_SetRenderLogicalPresentation
- SDL forum on HiDPI scaling / PIXELART: https://discourse.libsdl.org/t/strategies-for-scaling-on-high-dpi-displays-in-sdl-3/65838
- SDL main callbacks: https://wiki.libsdl.org/SDL3/README-main-functions
- SDL_shadercross: https://github.com/libsdl-org/SDL_shadercross
- FNA3D: https://github.com/FNA-XNA/FNA3D, https://github.com/FNA-XNA/FNA3D/tree/master/src, https://github.com/FNA-XNA/FNA3D/releases?page=4
- FNA 20.09 coverage: https://www.gamingonlinux.com/2020/09/fna-2009-is-out-with-the-new-fna3d-brings-experimental-vulkan-support/
- MonoGame 3.8.5: https://monogame.net/blog/2026-07-15-3.8.5-release-2026/, https://gamedev.net/news/4745-monogame-v385-released/
- Murder engine: https://github.com/isadorasophia/murder
- Godot on Vulkan/GLES: https://godotengine.org/article/about-godot4-vulkan-gles3-and-gles2/
- Godot renderers: https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html
- Godot internal rendering architecture: https://docs.godotengine.org/en/stable/engine_details/architecture/internal_rendering_architecture.html
- Godot architecture diagram: https://docs.godotengine.org/en/stable/engine_details/architecture/godot_architecture_diagram.html
- Godot PR #92797 (RD 2D batching): https://github.com/godotengine/godot/pull/92797
- Godot multiple resolutions: https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html
- Godot pixel snap threads: https://forum.godotengine.org/t/snap-2d-transforms-to-pixel/118943, https://forum.godotengine.org/t/aiming-for-pixel-perfection-in-godot/79368, https://forum.godotengine.org/t/characterbody2d-with-snap-2d-transforms-to-pixel-enabled-causes-1px-gap/897
- raylib rlgl.h: https://raw.githubusercontent.com/raysan5/raylib/master/src/rlgl.h
- sokol: https://github.com/floooh/sokol; handles vs pointers: https://floooh.github.io/2018/06/17/handles-vs-pointers.html
- bgfx overview: https://bkaradzic.github.io/bgfx/overview.html
- wgpu: https://github.com/gfx-rs/wgpu
- Veldrid: https://github.com/veldrid/veldrid
- Silk.NET: https://github.com/dotnet/Silk.NET
- The Forge: https://github.com/ConfettiFX/The-Forge
- Bevy: https://bevy-cheatbook.github.io/gpu/intro.html, https://github.com/bevyengine/bevy/issues/5082, https://bevy.org/news/bevy-0-15/, https://bevy.org/news/bevy-0-17/
- Unreal RDG: https://dev.epicgames.com/documentation/en-us/unreal-engine/render-dependency-graph-in-unreal-engine
- Unity pipelines: https://docs.unity.com/en-us/engine/current/manual/render-pipelines/overview, https://unity.com/topics/render-pipelines-strategy-for-2026, https://docs.unity3d.com/Manual//urp/upgrade-guide-unity-6.html
- Stride math: https://github.com/stride3d/stride/discussions/2210, https://github.com/stride3d/stride/issues/2, https://forums.stride3d.net/t/using-system-numerics/219
- System.Numerics Matrix4x4: https://learn.microsoft.com/en-us/dotnet/api/system.numerics.matrix4x4.createperspectivefieldofview
- WebGPU status: https://github.com/gpuweb/gpuweb/wiki/Implementation-Status, https://en.wikipedia.org/wiki/WebGPU
- .NET WebAssembly native dependencies: https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-native-dependencies
