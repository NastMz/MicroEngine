# ARCH-1 P3 — SDL_GPU 3D composited with 2D on one device

Throwaway prototype (ARCH-1 Phase II, prototype P3; tier-1 half of the graphics contract spike, directive A2). It is
not part of `MicroEngine.slnx` or CI and does not reference the engine. Engineering material: never published.

## Question

Can SDL_GPU, driven from C# through the binding the engine already uses (`ppy.SDL3-CS` 2026.1002.1, as in
`src/MicroEngine.Sdl`):

- (a) draw a depth-tested 3D mesh with a perspective camera, and
- (b) composite 2D on the same device, both through SDL_Renderer's `gpu` driver sharing its device
  (`SDL_GetGPURendererDevice` / `SDL_CreateGPURenderer`, `SDL_CreateGPURenderState`, GPU texture interop) and through
  a pure SDL_GPU 2D pass?

And: do `System.Numerics` projection/look-at conventions match SDL_GPU clip space; how are shaders supplied per
backend; what does this imply for the single `IGraphicsDevice` contract (annex A2 §6.1, directive A2)?

## Verdict

**Yes on both paths, on D3D12 and Vulkan, on two vendors.** 142 checks pass (18 numeric, 124 on pixels); the pure
SDL_GPU path and the SDL_Renderer interop path produce byte-identical frames. System.Numerics' default right-handed
helpers map directly onto SDL_GPU clip space with no correction matrix and no transpose on upload. Offline shaders
work: one HLSL source per shader, DXC (NuGet) for DXIL and SPIR-V, SPIRV-Cross for MSL.

Recommendation for the contract: the **tier-1 adapter should be pure SDL_GPU with its own built-in 2D pipelines**;
SDL_Renderer interop works but adds an ordering hazard and a silent-failure mode (below) that a single contract would
have to document as exceptions. The A2 sketch needs the additions listed in [contract feedback](#contract-feedback);
the most significant: depth attachment and load/store operations on passes, pipelines bound to target formats,
buffer/texture updates, readback, stage-qualified uniforms, per-usage format support, and a tier-0 story for vertex
buffers so one 2D batcher can drive both tiers.

## Environment

| Item | Value |
| --- | --- |
| OS | Windows 11 Pro 10.0.26300, x64 |
| .NET | SDK 10.0.403, runtime 10.0.14 |
| SDL | 3.5.0 (`SDL-3.5.0-ea7a2da`), native library from `ppy.SDL3-CS` 2026.1002.1; SDL_GPU drivers compiled in: `direct3d12`, `vulkan` |
| GPU 0 | NVIDIA GeForce RTX 4070 Ti SUPER — D3D12 driver 32.0.16.1088, Vulkan driver 610.88 (Vulkan 1.4.341, conformance 1.4.3.3) |
| GPU 1 | AMD Radeon(TM) Graphics (integrated) — D3D12 driver 32.0.21042.62, Vulkan "AMD proprietary driver" 2.0.353 |
| Shader tools | DXC 1.8.2505.32 (`Microsoft.Direct3D.DXC` NuGet), SPIRV-Cross (`Silk.NET.SPIRV.Cross` 2.22.0 NuGet) |
| Software rasterizers | none available (see [software rasterizers](#software-rasterizers-lavapipe-warp)) |

## Layout and how to run

```
experiments/arch-1/p3-sdl-gpu-3d/
  shaders/*.hlsl          one HLSL source per shader (SDL_GPU register-space rules in the comments)
  shaders/compiled/       committed blobs: .dxil, .spv, .msl + .json reflection (65 KB total)
  ShaderBuild/            offline compiler: HLSL -> DXIL + SPIR-V (DXC) -> MSL + reflection (SPIRV-Cross)
  P3Gpu/                  the prototype (math, gpu, renderer, all, compare)
  captures/               three reference frames (angle 0 pure SDL_GPU on Vulkan, angle -30 on D3D12,
                          angle 0 through SDL_Renderer on D3D12 with the custom-shader quad bottom right)
```

```
dotnet run --project ShaderBuild -c Release -- shaders shaders/compiled   # only when a .hlsl changes
dotnet run --project P3Gpu -c Release -- all                # visible windows, 120 rotating frames, then captures
dotnet run --project P3Gpu -c Release -- all --hidden       # captures only
dotnet run --project P3Gpu -c Release -- gpu --driver vulkan --low-power --debug
dotnet run --project P3Gpu -c Release -- compare a.png b.png
```

The exit code is the number of failed checks. Captures go to `./out` (ignored) unless `--out` is given.

## Method

**Scene** (`P3Gpu/Scene.cs`), 640×480, shared by both variants:

- Three cubes with outward counter-clockwise faces and one colour per face: a near cube (front face red, back face
  magenta) at z=1, a larger far cube (front face green) at z=−2 behind it, and a small blue marker at world (+1.8,
  +1.2, 0). The near cube is **drawn before** the far one, so only the depth test can keep it in front. Back faces
  are culled (`CULL_BACK`, `FRONTFACE_COUNTER_CLOCKWISE`); an inverted winding convention would show the magenta back
  face instead of red.
- Camera: `Matrix4x4.CreateLookAt` orbiting at distance 6 and `CreatePerspectiveFieldOfView(60°, 4/3, 0.1, 100)`;
  each object draws with `World * View * Projection` pushed as one vertex uniform.
- Clip probes drawn with an identity matrix: quads at clip z = −0.25 (top-left), +0.25 (top-right) and 1.25
  (bottom-right). With a 0..1 depth range only the middle one survives clipping.
- 2D overlay in pixel coordinates (`CreateOrthographicOffCenter(0, W, H, 0, 0, 1)`): an opaque yellow quad over the
  near cube, a half-alpha (texture alpha 128) blue quad over the clear colour and one over the far cube; straight-alpha
  "over" blending, point sampling.
- Clear colour (32, 64, 96): byte-exact so the clear check is exact.

**Variant 1, pure SDL_GPU** (`PureGpu.cs`, `Mesh3D.cs`): one device forced to `direct3d12` or `vulkan`; per frame one
command buffer with a 3D render pass (RGBA8 colour target + depth target, both cleared) and a 2D render pass (same
colour target, `LOAD`, no depth, sprite pipeline with blending); a copy pass downloads the colour target into a
transfer buffer; the target is blitted to the swapchain; submit with a fence, wait, map, save PNG (`SDL_SavePNG`).

**Variant 2, SDL_Renderer `gpu` driver** (`RendererInterop.cs`), in two directions:

- *renderer-first*: `SDL_CreateRendererWithProperties(name "gpu", shader-format booleans)` with `SDL_HINT_GPU_DRIVER`,
  then `SDL_GetGPURendererDevice`;
- *device-first*: `SDL_CreateGPUDevice` by the app, then `SDL_CreateGPURenderer(device, window)`.

The 3D pass (same `Mesh3D` code) is recorded in the app's own command buffer on the renderer's device into a GPU
texture, which is wrapped as an `SDL_Texture` with `SDL_PROP_TEXTURE_CREATE_GPU_TEXTURE_POINTER` (no copy) and drawn
with `SDL_RenderTexture`; the overlay is drawn with ordinary SDL textures, colour mod and `SDL_BLENDMODE_BLEND`. One
extra quad (bottom right) uses an offline-compiled custom fragment shader through `SDL_CreateGPURenderState` +
`SDL_SetGPURenderStateFragmentUniforms` (inverts the colour: (1, 0.5, 0) → (0, 0.5, 1)). Readback with
`SDL_RenderReadPixels`. A last step observes ordering between the app's submissions and the renderer's queued draws.

**Checks** (`Checks.cs`): each names the property it evaluates. Exact properties (positions, face colours, clipping,
clear colour, opaque overlay) are exact; blended values allow ±1 per channel, the named source being 8-bit
quantization of a blended (or 0.5) value. Expected pixel positions come from projecting world points with the same
System.Numerics matrices on the CPU. Each variant captures two deterministic frames (camera at 0° with the overlay,
−30° without) after the rotating frames.

## Results

| Run | D3D12 (NVIDIA) | Vulkan (NVIDIA) | D3D12 (AMD, `--low-power`) | Vulkan (AMD, `--low-power`) |
| --- | --- | --- | --- | --- |
| Pure SDL_GPU, 3D + 2D passes | 20/20 | 20/20 | 20/20 | 20/20 |
| SDL_Renderer gpu, renderer-first | 21/21 | 21/21 | not run | not run |
| SDL_Renderer gpu, device-first | 21/21 | 21/21 | not run | not run |
| Numeric conventions (`math`) | 18/18 (no GPU) | | | |

Final full run (`all`, visible windows, 120 rotating frames per variant): all 142 checks passed. Visible runs present
through `SDL_WaitAndAcquireGPUSwapchainTexture` + `SDL_BlitGPUTexture` at about 9.9 ms per frame (display vsync).
Device creation without debug mode: 340–570 ms on D3D12 and Vulkan alike. `--debug` runs: the D3D12 debug layer
was active ("Validation layers enabled") and reported only performance warnings (#820/#821: clear value differs from
the texture's optimized clear value), including on the interop path; Vulkan validation layers are not installed here
("Validation layers not found, continuing without validation").

### Image comparison (`P3Gpu compare`)

| Pair | Differing pixels | Max channel delta |
| --- | --- | --- |
| D3D12 vs Vulkan, same adapter (pure path; NVIDIA both angles, AMD angle −30) | 0 | 0 |
| Pure SDL_GPU vs SDL_Renderer interop, angle −30 | 0 | 0 |
| Pure SDL_GPU vs SDL_Renderer interop, angle 0 | 1600 (exactly the 40×40 custom-shader quad, drawn only by the interop path) | — |
| renderer-first D3D12 vs device-first Vulkan | 0 | 0 |
| NVIDIA vs AMD, angle 0 | 422 | 1 |
| NVIDIA vs AMD, angle −30 | 8340 (645 on edges, 7695 interior) | 1 |

Rasterization coverage is identical across vendors (every difference is ±1, none is a coverage difference); the ±1
interior differences are consistent with vendors rounding exact halves differently: several face colours × 255 end
in .5 (0.3, 0.5, 0.7 shades), as do the half-alpha blends (not traced pixel by pixel). This is a concrete, named source of variation for the tolerance policy
("±1/255 from 8-bit quantization"), and it means a reference image shared across adapters needs that ±1 even for flat
interior colours unless test colours are chosen as exact byte values.

### Observations that matter for the contract

1. **Interop ordering (SDL_Renderer + app command buffers).** The renderer's queued draws run when it submits its own
   command buffer, at `SDL_RenderPresent` or `SDL_RenderReadPixels`. Test: clear the shared texture red and submit;
   `SDL_RenderTexture` it; clear it blue and submit; read back.

   | Between the draw and the second clear | Second clear with `cycle` | Renderer showed |
   | --- | --- | --- |
   | nothing | no | blue (the later write) |
   | `SDL_FlushRenderer` | no | blue |
   | nothing | yes | blue |
   | `SDL_FlushRenderer` | yes | red (the earlier content) |

   Same on D3D12 and Vulkan, both directions. `SDL_FlushRenderer` records but does not submit; only flush + cycling
   protects content the renderer already queued. Safe rule: per frame, submit all app GPU passes first, then issue
   renderer draws, then present. On the pure SDL_GPU path the order is simply the recording order.
2. **Custom render-state shaders fail silently on D3D12 if their varyings do not match SDL's.** SDL's renderer DXIL
   expects the fragment inputs as `TEXCOORD0` (colour) and `TEXCOORD1` (uv), although SDL's HLSL sources say
   `COLOR0`/`TEXCOORD0` (its DXIL is built with shadercross). A shader compiled straight with DXC using `COLOR0`
   created fine (`SDL_CreateGPUShader`, `SDL_CreateGPURenderState` succeeded) but pipeline creation failed at draw time
   (`Could not create graphics pipeline state! ... (0x80070057)`): the quad was not drawn and only `SDL_GetError`
   said so. Vulkan (locations) was unaffected. Fixed by using `TEXCOORD0/1`.
3. **Backend selection: `SDL_HINT_GPU_DRIVER` wins over `SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING`.** With the hint set
   to `direct3d12` and the property to `vulkan`, the device is D3D12 (observed, and it first made the "Vulkan" run
   silently use D3D12). A backend must check `SDL_GetGPUDeviceDriver` after creation.
4. **Depth formats differ per adapter and API.** NVIDIA (both APIs) and AMD on D3D12 support D16/D24/D32F (+S8); AMD
   on Vulkan supports neither `D24_UNORM` nor `D24_UNORM_S8_UINT`, so the run picked `D32_FLOAT`.
5. **Swapchain textures cannot be the readback source** in this design (SDL_GPU swapchain textures are colour-target
   only); every capture renders to an offscreen target and blits to the swapchain. `SDL_WaitAndAcquireGPUSwapchainTexture`
   can return a null texture (minimised window), so "present" must tolerate a frame with no back buffer.
6. **Resource release is deferred by SDL.** Transfer buffers were released right after submitting the upload, shaders
   right after pipeline creation; SDL keeps them alive until the GPU is done. No device-lost API exists in the
   binding's SDL_GPU surface (no function or event for it).

## Numeric conventions: System.Numerics vs SDL_GPU

Checked numerically in `Conventions.cs` (18/18) and confirmed on pixels on both backends.

| Item | System.Numerics | SDL_GPU | Result and evidence |
| --- | --- | --- | --- |
| Depth range | `CreatePerspectiveFieldOfView` maps view z = −near → 0, −far → 1 | NDC z in [0, 1] | Match. Probes: clip z −0.25 and 1.25 clipped, 0.25 drawn (D3D12, Vulkan) |
| Handedness | `CreateLookAt`, `CreatePerspectiveFieldOfView` right-handed (view forward = −Z); `*LeftHanded` variants exist in .NET 10 (+Z forward, same 0..1 depth) | NDC left-handed: x right, y up, z into the screen | The right-handed projection does the flip; no extra matrix. World +X appears on the right |
| Y direction | view +Y → NDC +Y | NDC +Y is up on D3D12 **and** Vulkan (SDL handles Vulkan's flip) | Top-right probe at the top on both; marker drawn at the CPU-projected pixel (exact colour) |
| Viewport | — | origin top-left; pixel = ((x+1)/2·W, (1−y)/2·H) | Every CPU-projected check point hit the expected face colour |
| Multiplication | row vectors: `Vector4.Transform(v, M)` = v·M; `World * View * Projection` composes left to right | — | `Transform(v, A*B) == Transform(Transform(v, A), B)` |
| Upload | `Matrix4x4` memory is row-major (M11..M14, …; translation at floats 12–14) | HLSL default `column_major` cbuffer packing reads it transposed | Upload the raw 64 bytes and write `mul(M, v)` in HLSL: equals C#'s v·M. No transpose. SPIRV-Cross MSL keeps `M * v` |
| Winding | an outward face that is CCW seen from outside stays CCW in NDC (positive signed area) | `FRONTFACE_COUNTER_CLOCKWISE` + `CULL_BACK` | The red front face is visible, the magenta back face never |
| 2D | `CreateOrthographicOffCenter(0, W, H, 0, 0, 1)`: (0,0) → NDC (−1, +1), y down, z 0 | — | SDL_GPU sprite pass and SDL_Renderer overlay are pixel-identical |
| Texture rows | — | row 0 is the top in render targets, downloads and when SDL_Renderer samples a wrapped SDL_GPU texture | No flips anywhere |

The engine can use the stock right-handed System.Numerics helpers for tier 1 unchanged; the contract should state
these conventions once (right-handed world, row vectors, depth 0..1, NDC y up, pixel origin top-left).

## Shader toolchain

- **Authoring:** HLSL, one file per stage, following SDL_GPU's binding rules: vertex stage textures/samplers in
  `space0`, uniform buffers in `space1`; fragment stage textures/samplers in `space2`, uniform buffers in `space3`;
  vertex inputs use semantic `TEXCOORD<location>` (SDL's D3D12 default). Shaders meant for `SDL_CreateGPURenderState`
  must also match the renderer's varyings (`TEXCOORD0` colour, `TEXCOORD1` uv; sampler slot 0 = the drawn texture).
- **DXIL and SPIR-V:** DXC 1.8.2505.32 from the `Microsoft.Direct3D.DXC` NuGet package
  (`-T vs_6_0|ps_6_0 -E main`, plus `-spirv -fspv-target-env=vulkan1.0`). The Windows SDK 10.0.26100 `dxc.exe` cannot
  emit SPIR-V ("SPIR-V CodeGen not available").
- **MSL:** SPIRV-Cross (C API through `Silk.NET.SPIRV.Cross` 2.22.0, which ships `spirv-cross.dll`), MSL 2.1, entry
  point `main0`; the generated code uses `[[buffer(0)]]` for the first uniform block, `[[texture(0)]]`/`[[sampler(0)]]`
  and `[[attribute(n)]]` from the locations. **Not executed** (no Mac in this run); treat as unverified.
- **Reflection:** SPIRV-Cross also reports the counts `SDL_CreateGPUShader` needs (samplers, storage textures/buffers,
  uniform buffers); `ShaderBuild` writes them as `<name>.json`, the same role as shadercross's JSON output. The
  runtime picks the blob by `SDL_GetGPUDeviceDriver` (`direct3d12` → DXIL, `vulkan` → SPIR-V, `metal` → MSL).
- **Sizes:** DXIL 2.8–4.2 KB per shader, SPIR-V 0.4–1.4 KB, MSL 0.35–0.8 KB source.
- **shadercross itself was not used**: it has no package or tagged release (R1 §1.4); this pipeline uses the same two
  components ahead of time, so nothing compiles at run time. Gap found: shadercross and direct DXC differ in DXIL
  varying semantics (observation 2), which matters only for shaders that must link with SDL's own vertex shaders.
- **Portability of the toolchain:** the DXC NuGet package carries Windows binaries only (x64, x86, arm64). Building
  shaders on Linux or macOS CI needs DXC's Linux/macOS release builds or committed blobs (committed here).

## Contract feedback

Measured against annex A2 §6.1 and directive A2.

### Must be first-class (needed to do this prototype at all)

| Mechanism | What the prototype needed | A2 sketch |
| --- | --- | --- |
| Buffers | usage (vertex, index; later storage/indirect), size, **initial data and updates** (SDL_GPU uploads go through transfer buffers and a copy pass outside render passes) | `CreateBuffer` only; no way to fill or update — add `UpdateBuffer`/`UpdateTexture` (backend batches copies before the next pass) |
| Textures | format, usage flags (sampled, colour target, depth target), size, mips; initial data and sub-rectangle updates | present (`TextureDescription`, `TextureUsage`) |
| Render targets | a pass = up to 4 colour textures + optional depth texture, each with **load op (clear/load/don't care) and store op**; depth clear value | `BeginPass(RenderTarget?, PassClear)` has neither depth attachment nor load/store ops; 2D-over-3D needs colour `LOAD` without depth |
| Depth formats | D16, D24, D32F (+S8), chosen per device | only `Depth24Stencil8`; AMD Vulkan lacks D24 |
| Samplers | immutable, separate from textures | matches |
| Shaders | per-backend blob set + entry point (`main` / `main0`) + stage + **reflection counts** | `CreateShader(ShaderDescription)`: description must carry all of these |
| Pipelines | vertex layout (stride, attributes: location/format/offset), topology, cull mode, front face, fill, depth test/write/compare, blend per target, **colour and depth target formats** | "blend/depth/raster state + shaders": target formats and vertex layout missing; a pipeline only works with targets of its formats |
| Uniforms | pushed per **stage** and slot (4 per stage), cbuffer layout; matrices uploaded raw | `SetUniforms<T>(int slot, …)` has no stage |
| Draws | indexed (16/32-bit indices) and non-indexed | only `DrawIndexed`; add `Draw(vertexCount, firstVertex)` and index element size |
| Readback | async download of a texture region + fence (conformance tests, captures) | absent |
| Presentation | acquire may yield no back buffer; blit from an offscreen target | `Present()` assumes one; `BeginPass(null)` = back buffer cannot be read back |

### What tier 0 (SDL_Renderer, raylib) can implement as built-in pipelines

- Sprite / textured geometry with a fixed vertex format (position, uv, colour), each supported blend state, point or
  linear sampling, render targets, scissor. Evidence: the SDL_Renderer overlay and the SDL_GPU sprite pipeline produced
  **identical bytes** for straight-alpha quads over 3D content, so tier 0 and tier 1 built-ins can share reference
  images.
- **Vertex buffers on tier 0:** SDL_Renderer (`SDL_RenderGeometry`) and raylib take CPU arrays, not GPU buffers. If the
  batcher writes `GpuBuffer`s, tier 0 needs CPU-backed "stream" buffers behind the same type, or the batcher forks —
  which would break "both tiers drive the same 2D batcher". Decide this in the contract spike.
- Not on tier 0: depth, custom vertex layouts, custom vertex shaders, 3D.
- SDL_Renderer's `gpu` driver can take custom **fragment** shaders (fixed vertex stage, inputs colour + uv, sampler
  slot 0, fragment uniforms) but nothing else. If the engine wants that (palette swap, post effects on the tier-0 SDL
  path), it is a distinct capability from full custom pipelines (`Shaders`).

### Capabilities (declared, typed failure when missing)

- `DepthBuffer` plus the supported depth formats; per-format **usage** support (`SDL_GPUTextureSupportsFormat`), not a
  flat `IReadOnlySet<TextureFormat>`.
- Shader formats (DXIL / SPIR-V / MSL) and, if wanted, `FragmentShaders` (SDL_Renderer `gpu` driver) separate from
  `Shaders` (full pipelines).
- Limits SDL_GPU documents (not exercised beyond one target and one slot): 4 colour targets per pass, 4 uniform
  slots per stage.
- Custom shaders must be **validated eagerly**: observation 2 shows a shader and render state that are created
  successfully and then dropped at draw time. To honour "never silent degradation" the backend should build a probe
  pipeline when a shader/material is created and raise the typed error there.

### Threading, lifetime, submission

- One thread here (window, renderer, device). SDL documents `SDL_CreateGPURenderer` and `SDL_CreateGPURenderState` as
  "call on the thread that created the renderer/device"; nothing multithreaded was tested.
- Release is deferred by SDL_GPU (observation 6): the contract's `Dispose` can be immediate for the caller.
- Per-frame updated resources need cycling (`cycle` on uploads, passes and maps) or a ring; when SDL_Renderer reads
  them, cycling only protects content after `SDL_FlushRenderer` (observation 1).
- Submission order = execution order on the one queue. With SDL_Renderer in the mix, its work runs at
  present/readback; the contract must either forbid interleaving or order all app passes before renderer draws.
- Backend selection must be verified after creation (observation 3).

### Where this contradicts or refines the A2 sketch

1. `BeginPass(RenderTarget? target, in PassClear clear)`: needs colour attachments + optional depth attachment with
   load/store ops; `null` = back buffer conflicts with readback and with "no back buffer this frame".
2. `PipelineDescription` must include vertex layout and target formats; the 2D batcher's built-in pipelines become a
   cache keyed by (blend state, colour format, depth format or none).
3. `CreateBuffer` is marked "requires TexturedGeometry" but tier 0 has no GPU buffers: see tier 0 above.
4. `SetUniforms` needs the stage; `DrawIndexed` needs a non-indexed sibling and index size.
5. `TextureFormat` needs D16/D24/D32F(+S8); capabilities need per-usage format support.
6. Missing: buffer/texture updates, readback, a tier-1 note that `Present` may skip a frame.
7. `DeviceLost`: SDL_GPU offers no device-lost signal; the event can only be raised by backends that have one.
8. Directive A2 rejected "specialized 2D/3D interfaces"; this prototype supports that: the 2D overlay is just a second
   pass with a different pipeline on the same device and command buffer, needing nothing a 3D pass does not.

## Software rasterizers (lavapipe, WARP)

Not run. No lavapipe ICD is installed (`vulkaninfo --summary` lists only the NVIDIA and AMD GPUs), and SDL_GPU exposes
no property or hint to request the D3D12 WARP adapter (the binding's `SDL_PROP_GPU_DEVICE_CREATE_*` list has none);
forcing WARP system-wide (DirectX control panel) was out of scope. Whether SDL_GPU's D3D12 backend falls back to WARP
on a machine with no hardware adapter (a GitHub Windows runner) is **unknown**; Linux CI with Mesa lavapipe is the
likelier route for the Phase II "3D in CI on a software rasterizer" requirement and needs its own check (including
DXC/SPIRV-Cross availability there, or committed blobs).

## Risks and limits

- SDL 3.5.0 is the version packaged by `ppy.SDL3-CS` 2026.1002.1 (an odd minor, i.e. a development series); results
  may move with point releases.
- Metal and the MSL blobs were not executed; Android Vulkan, iOS and the browser are out of scope.
- No glTF loader (a procedural cube mesh; the A5 vertical slice still needs minimal glTF).
- Vulkan validation layers were absent, so only D3D12's debug layer checked API usage.
- Interop ordering was characterised for one texture and one frame pattern; other sharing patterns (renderer render
  targets read by the app) were not tested.
- The vendor ±1 rounding differences mean cross-adapter reference images need the quantization tolerance even on flat
  colours.
