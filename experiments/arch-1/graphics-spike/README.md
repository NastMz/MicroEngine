# ARCH-1 graphics contract spike — one device contract, tier 0 on SDL_Renderer, tier 1 on SDL_GPU

Throwaway spike (ARCH-1 Phase II, end of stage 1; dossier §9 gate "graphics contract frozen"). It is not part of
`MicroEngine.slnx` or CI and does not reference the engine. Engineering material: never published.

## Question

Can **one** `IGraphicsDevice` contract (resources, samplers, buffers, shaders, pipelines including built-in
pipelines, command lists with passes and attachments, updates, readback with a fence, surface target acquisition,
capabilities and the typed `GraphicsCapabilityException`) be implemented, without ad hoc exceptions, by

- a **tier-0** adapter on SDL_Renderer (default driver per P1: Direct3D 11 on Windows), with buffers as device-owned
  CPU memory submitted through `SDL_RenderGeometryRaw`, built-in Sprite/TexturedGeometry pipelines with the supported
  blend states, render targets, samplers and scissor, refusing custom shaders and depth with the typed error, and
- a **tier-1** adapter on pure SDL_GPU (per P3 and directive 2), with its own built-in pipelines plus custom shaders
  and depth,

both driving the **same** minimal 2D batcher with no per-tier branch?

Binding inputs: dossier §6.4, §8 (tolerance policy), §9; architect directives 1 (A2), 2 (the 16 revisions from P3,
which override §6.4 where they differ) and 3 (P1/P4 verdicts); P3 (`../p3-sdl-gpu-3d`), P1 (`../p1-sdl-gpu-driver`),
annex A2 §6.

## Verdict

**FREEZE WITH CHANGES.** Both adapters implement the proposed contract in [`Contract/`](Contract) completely, the same
batcher (which references nothing but the contract) drives both, and every place where an adapter needed something
§6.4 did not express was resolved by a contract change listed in [the diff](#proposed-contract-diff-vs-dossier-64). After those
changes the [ad hoc exception list](#ad-hoc-exceptions) has **no open entry**. The conformance suite passes on all five
device configurations (125/125 on NVIDIA; tier 1 also 50/50 on AMD), the scenario passes all 84 property checks, and
the 2D scene is **byte-identical** across tier 0 (Direct3D 11) and tier 1 (Direct3D 12 and Vulkan), on NVIDIA and on
AMD.

The changes to freeze are the ones in [the diff](#proposed-contract-diff-vs-dossier-64): directive 2 applied in full,
plus the additions this spike found necessary (separate filter and mirror capabilities, attachment formats on
built-in pipelines, the built-in vertex-space rule, explicit present modes, `ISurface.IsMinimized`, a requirement field
on the typed error, `RenderTarget.Color`, device-owned samplers and built-ins, limits as guaranteed minimums, no uniform
buffers, no scissor flag). The [open risks](#open-risks) do not change the contract's shape; they are stage 2a and
stage 6 work.

## Layout and how to run

```
experiments/arch-1/graphics-spike/
  Contract/      the proposed contract: descriptors, resources, IGraphicsDevice, CommandList (core validation),
                 GraphicsDeviceBase (core half every backend inherits). References only the BCL. THE ARTIFACT TO FREEZE.
  Tier0Sdl/      "sdl-renderer": SDL_Renderer adapter (ppy.SDL3-CS 2026.1002.1)
  Tier1SdlGpu/   "sdl-gpu": pure SDL_GPU adapter; embeds P3's sprite shaders as its built-in pipeline
  Batcher/       Renderer2D: Begin/Draw/DrawGeometry/End over IGraphicsDevice only (no backend reference)
  Host/          SdlWindowSurface: the ISurface an SDL host would produce (tests and scenario only)
  Conformance/   MSTest: the abstract suite (DeviceConformance) and its runs per backend (Backends.cs)
  Scenario/      the scene on every device, pixel diff statistics, present-mode timing
  shaders/       HLSL + compiled blobs copied from P3 (sprite.*: tier-1 built-ins; mesh.vert, color.frag: depth tests)
  captures/      the scenario's frames on NVIDIA (2D on all three devices, 3D + 2D on tier 1)
```

```
dotnet test Conformance -c Release                                   # all devices (one hidden 64x48 window per test)
dotnet test Conformance -c Release --filter "TestCategory!=Visible"  # without the minimise test (opens a visible window)
ARCH1_LOW_POWER=1 dotnet test Conformance -c Release                 # tier 1 on the integrated adapter (AMD here)
dotnet run --project Scenario -c Release -- scene [--out dir] [--low-power] [--tier0-api opengl|vulkan|software|...]
dotnet run --project Scenario -c Release -- compare a.png b.png
dotnet run --project Scenario -c Release -- present [--frames n]     # visible windows, windowed and desktop full screen
```

Shaders are not recompiled here: `../p3-sdl-gpu-3d/ShaderBuild` produced the blobs (DXC 1.8.2505.32 + SPIRV-Cross).

## Environment

| Item | Value |
| --- | --- |
| OS | Windows 11 Pro 10.0.26300, x64 |
| .NET | SDK 10.0.403, runtime 10.0.14; MSTest 4.4.1 |
| SDL | **3.5.0** (`SDL-3.5.0-ea7a2da`), native library bundled by `ppy.SDL3-CS` 2026.1002.1 — a development series (directive 2 item 16, directive 3) |
| GPU 0 | NVIDIA GeForce RTX 4070 Ti SUPER (D3D11 via SDL_Renderer; D3D12 and Vulkan via SDL_GPU) |
| GPU 1 | AMD Radeon(TM) Graphics, integrated (selected with `PreferLowPower`) |
| Display | 1920×1080 at 100 Hz |
| Software rasterizers | none available (lavapipe/WARP untested, as in P3) |

## The contract (summary)

The full text, with every rule in XML documentation, is [`Contract/`](Contract) (≈1,640 lines; the build fails on an
undocumented public member). The shape:

- **`IGraphicsDevice`**: `Capabilities`; optional `DeviceLost`; `CreateTexture`, `CreateRenderTarget`, `GetSampler`
  (cached, device-owned), `CreateBuffer` (every tier), `CreateShader`, `CreatePipeline`,
  `GetBuiltInPipeline(kind, blend, colorFormat, depthFormat?)`; `BeginCommands`, `Submit`;
  `ReadPixels(RenderTarget, RectI) → ReadbackRequest` (fence: `IsCompleted`, `Wait()`, `Pixels`);
  `TryAcquireSurfaceTarget(out RenderTarget)`; `Present()`.
- **`GraphicsDeviceBase`** (core): validates every call (ownership, disposal, sizes, ranges, capabilities, formats per
  usage) before the backend's `*Core` method runs, caches samplers and built-in pipelines, owns surface-target expiry.
  Backends implement only the `*Core` members, so errors are identical on every tier.
- **`CommandList`** (core, sealed): `BeginPass(PassDescription{ColorAttachment, DepthAttachment?})` with load
  (Load/Clear/DontCare) and store (Store/DontCare) and clear values; `SetViewport(RectI)`, `SetScissor(RectI?)`,
  `SetPipeline`, `SetTexture(slot, texture, sampler)`, `SetVertices`, `SetIndices(buffer, IndexSize)`,
  `SetUniforms(stage, slot, bytes)`, `Draw(count, first, instances)`, `DrawIndexed(count, firstIndex, baseVertex,
  instances)`, `EndPass`; `UpdateBuffer` and `UpdateTexture` between passes. Validated as it is recorded; a backend
  reads the result as `Commands` + `Payload`.
- **Capabilities**: tier, `GraphicsFeatures` (RenderTargets, AddressRepeat, AddressMirror, Mipmaps, CustomBlend,
  Shaders, DepthBuffer, Instancing, ComputeShaders, SeparateFilters), shader formats, `Supports(format, usage)`,
  built-in blend states (at least the four presets), present modes, limits (guaranteed minimums), `ApiName` and
  adapter **as reported after creation**.
- **Written rules** the suite checks: colours premultiplied; built-in vertices (`Vertex2D`, 32 bytes) are target pixels
  relative to the viewport origin, y down, pixel centres at +0.5; coverage by pixel centre; clears ignore viewport and
  scissor; scissor absolute in target pixels; bindings, viewport and scissor are pass-scoped, uniforms persist for the
  rest of the list; execution in `Submit` order and recording order (updates included); `ReadPixels` sees every list
  submitted before it; surface targets expire at `Present`, cannot be sampled or read back; depth 0..1, CCW front faces,
  System.Numerics matrices uploaded as-is (P3 conventions).

## Adapters: how each maps the contract

| Contract | Tier 0, SDL_Renderer | Tier 1, SDL_GPU |
| --- | --- | --- |
| Buffer | `byte[]` owned by the device; draws pass pointers with stride 32 to `SDL_RenderGeometryRaw` (no conversion: `Vertex2D` is laid out for it) | `SDL_GPUBuffer`; updates through upload transfer buffers in a copy pass (no cycling, so partial updates keep the rest) |
| Command list | executed at `Submit` into SDL's own queue (SDL copies geometry, so later updates cannot affect earlier draws) | one command buffer per list; consecutive updates share a copy pass, passes are render passes |
| Built-in pipeline | an SDL blend mode (`SDL_ComposeCustomBlendMode`) | adapter-owned pipeline per (kind, blend, formats) from P3's sprite shaders; pushes `CreateOrthographicOffCenter(0, vw, vh, 0, 0, 1)` as vertex uniform 0 |
| Sampler | `SDL_SetTextureScaleMode` + `SDL_SetRenderTextureAddressMode` per draw | `SDL_GPUSampler`, cached |
| Viewport / scissor | `SDL_SetRenderViewport`; scissor converted from absolute to SDL's viewport-relative clip rectangle | `SDL_SetGPUViewport` / `SDL_SetGPUScissor` (absolute) |
| Readback | `SDL_RenderReadPixels` + `SDL_ConvertSurface`; the request is complete on return | download copy pass + `SDL_SubmitGPUCommandBufferAndAcquireFence`; `SDL_QueryGPUFence` / `SDL_WaitForGPUFences` |
| Surface target | SDL's default target; `SDL_RenderPresent` | a device-owned texture in the swapchain format, blitted to the swapchain at `Present` (`SDL_WaitAndAcquireGPUSwapchainTexture`) |
| Present mode | `SDL_SetRenderVSync(1/0)` | `SDL_SetGPUSwapchainParameters`, always explicit (K-P1-1) |
| Capabilities | features probed (blend modes via `SDL_SetTextureBlendMode`), formats from the renderer's texture-format list, max texture size from its properties | formats per usage from `SDL_GPUTextureSupportsFormat`; shader formats from `SDL_GetGPUShaderFormats`; limits from SDL_GPU's fixed constants and API minimums |
| Custom shaders, depth, instancing | refused by core with `GraphicsCapabilityException` before the adapter is called | implemented |

## Ad hoc exceptions

Every place an adapter needed something the §6.4 sketch does not express, or would have had to bend it. The freeze
criterion is that this list is empty or resolved by a contract change. Status after the proposed contract: **all
resolved; none open.**

| # | Adapter | What §6.4 could not express or forced to bend | Resolution |
| --- | --- | --- | --- |
| E1 | tier 0 | SDL_Renderer has one scale mode per texture: no separate min/mag filters | contract change: `GraphicsFeatures.SeparateFilters`; `Min != Mag` requires it (typed error otherwise) |
| E2 | tier 0 | SDL_Renderer wraps (`SDL_TEXTURE_ADDRESS_WRAP`) but has no mirror; §6.4's single `AddressModes` flag cannot say "repeat yes, mirror no" | contract change: `AddressRepeat` and `AddressMirror` replace `AddressModes` |
| E3 | tier 0 | no mipmaps, yet `SamplerDescription.LinearClamp` has `Mip = Linear`; gating the mip filter would make the preset fail on tier 0 | contract rule: the mip filter applies only to textures with more than one level; `Mipmaps` is gated at `CreateTexture(MipLevels > 1)` |
| E4 | tier 0 | `SDL_RenderGeometry` takes pixel coordinates and no matrix, so a built-in pipeline cannot take a transform uniform on tier 0 | contract rule: built-in vertices are target pixels relative to the viewport origin; built-ins have no uniforms; the batcher transforms on the CPU (tier 1 pushes the projection itself) |
| E5 | tier 1 | SDL_GPU pipelines are bound to attachment formats (and the swapchain is BGRA8); `GetBuiltInPipeline(kind, blend)` cannot produce a pipeline for the surface | contract change: `GetBuiltInPipeline(kind, blend, colorFormat, depthFormat?)`, cached; `SetPipeline` rejects a format mismatch |
| E6 | both | "minimised: no back buffer this frame" needs host knowledge; §6.4's `ISurface` has no such member (an SDL adapter could query the window, a non-SDL one could not) | contract change: `ISurface.IsMinimized` |
| E7 | both | errors for formats, shader formats and present modes are capability errors but not `GraphicsFeatures` flags | contract change: `GraphicsCapabilityException.Requirement` (with `Missing = None`) |
| E8 | both | to sample a render target the contract needs its texture; §6.4 has no link from `RenderTarget` to `Texture` | contract change: `RenderTarget.Color` (owned, null for the surface) |
| E9 | tier 1 | SDL_GPU uniforms are per command buffer; the built-in projection push overwrote a custom pipeline's vertex slot 0 | contract rule "uniform data persist for the rest of the list" made it observable; the adapter restores the caller's data (test `Uniforms_PersistForTheRestOfTheList_AcrossBuiltInDraws`, RED without the restore) |
| E10 | tier 1 | SDL_GPU reports no limits | contract rule: limits are guaranteed minimums (API minimum when not queryable: 4096 Vulkan, 16384 D3D12, 8192 Metal; SDL_GPU's constants for slots, attributes, uniform bytes) |
| E11 | tier 1 | SDL_GPU has no uniform buffers (`BufferUsage.Uniform`) and scissor is dynamic state (`RasterState.Scissor`) | contract change: both removed |
| E12 | tier 1 | the swapchain texture belongs to the command buffer that acquired it and is presented when that buffer is submitted; recording surface passes into it would break "lists execute at Submit" (a later `ReadPixels` would not see them) | no contract change: the adapter renders the surface into its own texture and blits at `Present`. Cost: one full-surface blit per frame ([risks](#open-risks)) |
| E13 | tier 0 | SDL_Renderer does not expose its back buffer format | contract text: the surface target's `ColorFormat` is the format its pipelines must declare; a tier-0 backend reports its nominal back-buffer format (Bgra8Unorm) because its built-ins are format-independent |
| E14 | tier 0 | `SDL_RenderGeometryRaw` validates every index against the vertex count and fails the call; GPUs do not validate | contract text: index values are not validated; an index outside the bound buffer is undefined behaviour (tier 0 may throw at `Submit`) |

Adapter mappings that needed no rule (SDL's viewport-relative clip rectangle; tier-0 readback completing immediately,
which the fence contract allows; `LoadOp.DontCare` treated as `Load` on tier 0) are not exceptions.

## Results

### Conformance suite (MSTest, `Conformance/`)

25 tests in `DeviceConformance`, each run unchanged against every configuration. Every test runs on every tier: a test
of an undeclared feature asserts the typed error instead (`Depth_…`, `Uniforms_…`, `Sampler_RepeatAndMirror_…`,
`UndeclaredCapabilities_…`).

| Configuration | API reported | Result |
| --- | --- | --- |
| Tier 0, SDL_Renderer default driver | `direct3d11` | 25/25 |
| Tier 0, SDL_Renderer `opengl` | `opengl` | 25/25 |
| Tier 0, SDL_Renderer `vulkan` | `vulkan` | 25/25 |
| Tier 1, SDL_GPU, NVIDIA | `direct3d12` | 25/25 |
| Tier 1, SDL_GPU, NVIDIA | `vulkan` | 25/25 |
| Tier 1, SDL_GPU, AMD (`ARCH1_LOW_POWER=1`, adapter logged) | `direct3d12` | 25/25 |
| Tier 1, SDL_GPU, AMD | `vulkan` | 25/25 |

Total 125/125 on NVIDIA (37 s) and 50/50 for tier 1 on AMD (25 s). What the tests check (property, comparison):

| Rule | Tests | Comparison |
| --- | --- | --- |
| Sprite placement, integer scale, mirroring, untextured coverage | `Sprite_*`, `Geometry_*` | exact (bounding boxes and every texel) |
| Blend presets (Opaque, AlphaPremultiplied, Additive, Multiply) | `Blend_EveryPreset_…` | ±1/255 per channel, source: 8-bit quantization of the blended value (P3: exact halves round differently per vendor) |
| Nearest vs linear | `Sampler_NearestAndLinear_…` | nearest exact; linear: clamped ends exact, monotonic, within ±2/255 of the bilinear kernel (sources: 8-bit sub-texel weights + output quantization) |
| Repeat / mirror | `Sampler_RepeatAndMirror_…` | exact tiling, or the typed error naming the feature |
| Scissor, viewport | `Scissor_*`, `Viewport_*` | exact rectangles; clear ignores scissor; scissor absolute while the viewport offsets positions |
| Render target round trip | `RenderTarget_RoundTripIsExact` | exact, including a translucent texel copied with `Opaque` |
| Readback fence | `Readback_FenceCompletes_…` | `Wait` then `IsCompleted`; a second request completes by polling alone; a region equals the same pixels of a whole read (exact) |
| Ordering | `Updates_TakeEffectInRecordingOrder`, `UpdateTexture_…`, `Uniforms_…` | exact |
| Capability errors | `UndeclaredCapabilities_FailWithTheTypedError` | for each of Mipmaps, SeparateFilters, AddressMirror, AddressRepeat, CustomBlend, DepthBuffer, Shaders, Instancing: typed error with `Missing` = the feature and `Device` = the backend when undeclared, success when declared; an unsupported format and an undeclared shader format: typed error with `Requirement` |
| Depth | `Depth_TestKeepsTheNearerSurface…` | tier 1: near quad drawn first survives, exact colours and exact clip-space bounds; tier 0: typed error |
| Core validation | `Recording_*`, `Ranges_*`, `Resources_*`, `Pipelines_*` | exact exception types (nesting, ranges, foreign and disposed resources, format mismatch, sampling the pass's own target, device-owned objects) |
| Surface | `SurfaceTarget_*` | acquire, draw, present, expiry; not readable; `TryAcquireSurfaceTarget` false while minimised (visible window) |
| Batcher | `Batcher_IssuesOneDrawPerRunOfTheSameTexture` | exact draw count (3 for A, A, B, A) and submission order |

**The suite detects defects** (RED observed by mutation, then restored): absolute scissor passed to SDL without the
viewport conversion (tier 0: `Viewport_…` fails); the built-in projection built from the target instead of the viewport
(tier 1, both APIs: `Viewport_…` fails); the colour-format check removed from `SetPipeline` (all tiers:
`Pipelines_…` fails); the uniform restore disabled (tier 1, both APIs: `Uniforms_…` fails).

### Scenario: the same batcher on both tiers (`Scenario scene`)

One 160×120 scene drawn by identical batcher code: a 4× nearest checker, a half-alpha premultiplied quad, additive and
multiply batches, a 30° rotated untextured quad, a render target round trip at 2×, a linear gradient, a scissored fill,
an opaque and a half-alpha overlay; read back and checked by property (exact positions and coverage, ±1/255 colours,
kernel bounds for filtering). On tier 1 the same 2D layer is then composited over a depth-tested mesh (P3's
`mesh.vert`/`color.frag`; two cubes, near drawn first, `CreateLookAt` + `CreatePerspectiveFieldOfView` as in P3) drawn
first into a target with depth. Each device also draws its read-back frame to its surface target and presents it (the
capture path of directive 2 item 3).

| Device | Checks | Notes |
| --- | --- | --- |
| tier0-sdl-renderer (D3D11) | 16/16 | the 3D half is refused with `GraphicsCapabilityException` (DepthBuffer) |
| tier1-sdl-gpu-direct3d12 | 34/34 | 2D + 3D-under-2D |
| tier1-sdl-gpu-vulkan | 34/34 | 2D + 3D-under-2D |

Coverage of the rotated quad is checked exactly against the written rule (a pixel is covered when its centre is inside;
240 pixels, none ambiguous): it holds on all three devices.

**Pixel comparison** (frames in [captures/](captures)):

| Pair | Differing pixels | Max channel delta |
| --- | --- | --- |
| tier 0 D3D11 vs tier 1 D3D12, 2D scene | 0 of 19,200 | 0 |
| tier 0 D3D11 vs tier 1 Vulkan, 2D scene | 0 | 0 |
| tier 1 D3D12 vs Vulkan, 2D scene | 0 | 0 |
| tier 1 D3D12 vs Vulkan, 3D + 2D | 0 | 0 |
| NVIDIA vs AMD, tier 1, each of the four frames above (`--low-power`, then `compare`) | 0 | 0 |
| tier 0 `opengl`, `vulkan`, `direct3d12`, `gpu` drivers vs tier 1, 2D scene (`--tier0-api`) | 0 | 0 |
| tier 0 `software` driver vs tier 1, 2D scene | 218 | 223 (200 pixels Δ1, 18 pixels Δ223) |

The scene's colours were chosen as byte-exact values, so no ±1 was needed between devices here; the tolerance remains
necessary in general (P3 measured ±1 between vendors for colours ending in .5).

The **software** SDL_Renderer is not conformant: its rotated-quad coverage differs from the pixel-centre rule in 18
pixels (the Δ223 pixels), and it does not declare `CustomBlend` (its blend probe fails, correctly). The other 15 of its
16 checks pass. See [risks](#open-risks).

### Present modes (`Scenario present`, K-P1-1)

120 frames after 10 warm-up frames, 640×480 windowed and 1920×1080 desktop full screen, 100 Hz display. The tier-1
device always sets its present mode explicitly (`SDL_SetGPUSwapchainParameters`).

| Device | VSync windowed | VSync full screen | Immediate windowed | Immediate full screen |
| --- | --- | --- | --- | --- |
| tier 0 SDL_Renderer D3D11 | 10.00 ms | 10.08 ms | 0.12 ms | 0.17 ms |
| tier 1 SDL_GPU D3D12 | 10.00 ms | 10.08 ms | 0.84 ms | **0.81 ms** |
| tier 1 SDL_GPU Vulkan | 10.00 ms | 10.08 ms | 0.47 ms | 0.96 ms |

With the present mode set explicitly, **K-P1-1 does not reproduce** on pure SDL_GPU: D3D12 desktop full screen runs
unthrottled with `Immediate`. P1 observed the wait through SDL_Renderer's `gpu` driver; why that path waits was not
investigated here. This is the measurement directive 3 asks the tier-1 device's conformance to make; it lives in the
scenario because it needs visible windows, full screen and a display, and it reports timings rather than pass/fail. Present modes reported: NVIDIA VSync/Immediate/Mailbox on both APIs;
AMD Vulkan VSync/Immediate (no Mailbox).

### Other measurements

- Device creation (hidden window): tier 0 D3D11 160–200 ms; tier 1 D3D12 390–520 ms, Vulkan 355–425 ms (NVIDIA);
  AMD D3D12 739 ms, Vulkan 461 ms.
- Depth formats: NVIDIA D16/D24S8/D32 on both APIs; AMD D3D12 all three; **AMD Vulkan D16 and D32 only** (no D24, as
  P3 found) — `Supports(format, usage)` expresses it and the tests pick the first supported format.
- Tier-0 declared features (D3D11, OpenGL, Vulkan, D3D12, gpu drivers): `RenderTargets | AddressRepeat | CustomBlend`;
  software driver: `RenderTargets | AddressRepeat`.

## Proposed contract diff vs dossier §6.4

Directive 2 is applied in full (items 1–14; 15 and 16 are toolchain and risk records). Below, every difference
between §6.4 and `Contract/`, with its source: **D2-n** = directive 2 item n, **E-n** = the exception above it resolves,
**S** = needed to state a rule the suite checks.

| Area | §6.4 | Proposed | Source |
| --- | --- | --- | --- |
| Surface target | `BeginPass(null)` = back buffer | `bool TryAcquireSurfaceTarget(out RenderTarget)`; surface target expires at `Present`, cannot be sampled or read back | D2-3 |
| Readback | none | `ReadbackRequest ReadPixels(RenderTarget, RectI)` with `IsCompleted`, `Wait()`, `Pixels`; offscreen targets only | D2-4 |
| Device lost | required event | optional; SDL_GPU never raises it | D2-5 |
| Passes | `BeginPass(target, PassClear)` | `BeginPass(PassDescription{ColorAttachment(target, load, store, clear), DepthAttachment?(target, load, store, clearDepth)})` | D2-6 |
| Updates | none | `UpdateBuffer(buffer, offset, data)`, `UpdateTexture(texture, region, data)`, between passes, in recording order | D2-8 |
| Uniforms | `SetUniforms<T>(slot, in T)` | `SetUniforms(ShaderStage, slot, ReadOnlySpan<byte>)` (+ typed overload); persist for the rest of the list; slot must be declared by the bound pipeline | D2-9, E9 |
| Draws | `DrawIndexed(count, first, base, instances)` | + `Draw(count, first, instances)`; `SetIndices(buffer, IndexSize.Bits16/Bits32)`; index values unvalidated (UB outside the buffer) | D2-10, E14 |
| Formats | `Depth24Stencil8` | `D16Unorm`, `D24UnormS8`, `D32Float`, + `Bgra8Unorm` (swapchain); `Capabilities.Supports(TextureFormat, TextureUsage)` replaces the format set; `FloatTextures` and `CompressedTextures` flags dropped (per-format support says it) | D2-11, E5 |
| Shaders | formats declared | creation validates format and reflected resources (`ShaderResources`); errors at creation, never at draw | D2-12 |
| Identity | `ApiName` | `ApiName` and `Adapter` as reported after creation; `Backend` (factory name) in capabilities | D2-13 |
| Buffers | "requires TexturedGeometry"; `BufferUsage` Vertex/Index/Uniform | every tier; `Vertex | Index` only; `TexturedGeometry` flag removed (mandatory) | D2-2, E11 |
| Built-in pipelines | `GetBuiltInPipeline(kind, blend)` | `GetBuiltInPipeline(kind, blend, colorFormat, depthFormat?)`, device-owned (disposing throws) and cached | E5 |
| Built-in vertex space | unstated | `Vertex2D` (32 bytes, fixed layout): pixels relative to the viewport origin, y down; no uniforms; colour = texel × vertex colour | E4 |
| Address modes | `AddressModes` flag | `AddressRepeat`, `AddressMirror` | E2 |
| Filters | — | `SeparateFilters` flag; mip filter ignored for single-level textures | E1, E3 |
| Present | `Present()` | + `GraphicsDeviceOptions.PresentMode` (explicit, never left to the driver) and `Capabilities.PresentModes`; unsupported → typed error at creation | K-P1-1 |
| Surface | `ISurface` | + `IsMinimized` | E6 |
| Typed error | `Operation`, `Device`, `Missing` | + `Requirement` for formats, shader formats, present modes (`Missing = None`) | E7 |
| Render target | separate type | + `Color` (owned sampled texture; null for the surface), `ColorFormat`, `DepthFormat`, `IsSurface`, `Bounds` | E8 |
| Samplers | `GraphicsResource` | device-owned, cached value objects (not disposable) | S |
| Limits | fields | guaranteed minimums (may be conservative, never optimistic) | E10 |
| Raster | `RasterState(Cull, Front, Scissor)` | `RasterState(Cull, Front)`; scissor is dynamic and always on | E11 |
| Viewport/scissor | `RectF` | `RectI`; viewport must lie inside the target; scissor absolute, clipped to the target | S |
| Validation | "validated in core" | `GraphicsDeviceBase` + `CommandList`: the exception types and the moment they are thrown are part of the contract (see `CommandList` docs) | S |
| Blend presets | names only | defined factors: Opaque, AlphaPremultiplied, Additive (keeps dst alpha), Multiply (premultiplied, keeps dst alpha); `BlendFactor`/`BlendOp` limited to what SDL_Renderer composes | S |
| `ISurface` comment | "integrated backends provide both" | removed (P4: no integrated-backend concept) | directive 3 |

Not changed: tiers, `IGraphicsBackendFactory` (`UnsupportedReason` covers tier 0's "needs a window"), the resource
base (`Device`, `IsDisposed`, `IsLost`), deferred destruction, `BuiltInPipeline { Sprite, TexturedGeometry }`.

## Open risks

1. **SDL 3.5.0 is a development series** (bundled by ppy.SDL3-CS 2026.1002.1): `SDL_SetRenderTextureAddressMode`, the
   per-draw scale mode and `SDL_GPU` itself are recent APIs. Pin and re-run this suite on every SDL update.
2. **Software and emulated rasterizers.** The software SDL_Renderer violates the coverage rule (18 of 240 pixels); if
   the engine ever falls back to it, it must be refused (or declared non-conformant), not tolerated. lavapipe and WARP
   remain untested for tier 1 (directive 2 item 16).
3. **Surface blit cost on tier 1** (E12): one full-surface copy per frame. Negligible on the desktop GPUs measured;
   unmeasured on mobile (bandwidth). If it matters, a later contract version may let a list declared "final for the
   frame" render straight into the swapchain; that would be an additive rule, not a change of shape.
4. **Mobile formats**: SDL_GPU 3.5 has no ETC2 format; ASTC is unsupported on these desktop GPUs. Compressed textures
   are expressible (`Supports`), but the Android asset story needs a decision (stage 2c/6).
5. **Not exercised here**: mipmaps (generation and upload of levels), sRGB formats, several colour attachments, instanced
   tier-1 draws beyond acceptance, `DeviceLost` (SDL_Renderer reports device reset through the host's event queue, so
   the host must forward it to the device — a host/device wiring rule for stage 1's platform split), web (P4: SDL
   browser launcher, tier 0 only), Metal, phones, raylib (the second desktop tier-0 implementation, stage 2a).
6. **Batcher scope**: the spike's batcher is the minimum the contract must carry (Begin/Draw/DrawGeometry/End).
   `PushState/PopState`, sort modes, the `depth` parameter and `Material` of §6.4's `IRenderer2D` are not exercised;
   none needs a device feature beyond what the suite covers, except `Material` (custom shaders: tier 1 only, by
   declared capability).
7. **Two identical built-ins**: `Sprite` and `TexturedGeometry` have identical rules in this proposal. Keeping both costs
   nothing; merging them is a simplification for the architect to decide.
8. **CPU-side uploads**: tier 1 creates one upload transfer buffer per update (correct, not fast); a ring buffer is
   stage 2a work and does not change the contract.

## Directive 2 checklist

| Item | Status in the spike |
| --- | --- |
| 1 pure SDL_GPU tier 1 with own built-ins | done (`Tier1SdlGpu`) |
| 2 buffers on every tier, same batcher | done; batcher has no backend reference |
| 3 `TryAcquireSurfaceTarget`, readback only on targets | done; tested (expiry, refusal to read, minimised) |
| 4 `ReadPixels` with fence; synchronous helper in Testing | fence done; the synchronous helper is `request.Wait()` + `Pixels` (the tests' `Read`), to ship in `MicroEngine.Testing` |
| 5 optional `DeviceLost` | done (declared; never raised by either adapter) |
| 6 attachments with load/store/clear | done (one colour + optional depth) |
| 7 pipelines carry layout and formats | done; mismatch refused at `SetPipeline` |
| 8 `UpdateBuffer` / `UpdateTexture` | done; ordering tested |
| 9 `SetUniforms(stage, slot, bytes)` | done; persistence tested |
| 10 `Draw` / `DrawIndexed`, `IndexSize` | done |
| 11 depth formats, `Supports(format, usage)` | done; AMD Vulkan without D24 handled |
| 12 shader formats validated at creation | done; undeclared format → typed error |
| 13 identity after creation | done; tests assert `ApiName` |
| 14 conventions | used unchanged (scene camera, mesh upload, CCW, depth 0..1) |
| 15 toolchain | reused P3's blobs; not re-evaluated |
| 16 risks | SDL 3.5.0 recorded; lavapipe/WARP still untested |
