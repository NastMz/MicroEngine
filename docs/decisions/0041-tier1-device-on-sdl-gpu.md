# ADR 0041: The tier-1 graphics device on pure SDL_GPU

## Status

Accepted (2026-11-03). Implements tier 1 of [ADR 0028](0028-graphics-device-contract.md) (graphics device contract
version 1, unchanged). Evidence: ARCH-1 §6.4 and §9.1 item 2, architect directives 2 and 5 (E1-E14), prototype P3
([`experiments/arch-1/p3-sdl-gpu-3d`](../../experiments/arch-1/p3-sdl-gpu-3d/README.md)) and the contract spike
([`experiments/arch-1/graphics-spike`](../../experiments/arch-1/graphics-spike/README.md), tier-1 adapter), annex R1 and
R3 (software rasterizers in CI).

## Context

The device contract has two tiers that differ only in declared capabilities. Tier 0 runs on SDL_Renderer, raylib and
the CPU reference device. Tier 1 (custom shaders, depth) existed only as the spike's adapter, outside the solution, so
nothing in the repository proved that the contract's programmable half holds, and no CI check rendered with a GPU API
at all (Linux CI renders SDL_Renderer through OpenGL on Mesa llvmpipe).

## Decision

- **`MicroEngine.Graphics.SdlGpu`**, a new assembly that references only the graphics contract, the platform contracts
  (and through them the kernel) and SDL3 (`ppy.SDL3-CS`, the same pinned version as `MicroEngine.Sdl`). Its factory
  `SdlGpuBackendFactory` ("sdl-gpu") creates `SdlGpuDevice`, which derives from `GraphicsDeviceBase` like every device.
  It is not a package yet: nothing ships it until a host selects it.
- **Pure SDL_GPU**, never SDL_Renderer's `gpu` driver (directive 2 item 1): the device owns its built-in `Textured`
  pipeline (offline-compiled sprite shaders, embedded) and pushes the viewport projection itself; it restores a custom
  pipeline's vertex uniform slot 0 after a built-in draw, because SDL_GPU uniforms belong to the command buffer and the
  contract says uniform data persist for the rest of the list (E9).
- **Identity after creation.** The requested API is passed as both the driver hint and the name property, then the
  device reports what SDL chose (`ApiName`, `Adapter`, and `DriverName`/`DriverVersion`/`DriverInfo` for logs).
  Software Vulkan is accepted (`VULKAN_REQUIRE_HARDWARE_ACCELERATION` false), so lavapipe is a valid adapter.
- **Present mode set explicitly** on the swapchain; a mode the window does not support is the typed error at creation.
  Surface passes render into a device-owned texture in the swapchain format, copied to the swapchain at `Present` (E12),
  so a list that renders to the surface executes at `Submit` like any other. `SurfaceCopies` counts those copies.
- **Capabilities from SDL**: texture formats per usage from `SDL_GPUTextureSupportsFormat` (AMD Vulkan has no 24-bit
  depth), shader formats from `SDL_GetGPUShaderFormats`, limits as guaranteed minimums (E10). Depth formats are
  attachments only (no depth sampling in contract version 1). Offscreen devices (no surface) are supported.
- **Conformance.** The device conformance suite (tests/Shared/Graphics) gains the tier-1 rules that were not checked
  yet: depth test for every supported depth format (and the clear depth as the comparison value), clip depth 0..1,
  counter-clockwise front faces and culling, System.Numerics matrices uploaded as-is landing at their CPU projection,
  and uniform persistence across built-in draws; on tier 0 each asserts the typed error. It runs on SDL_GPU's default
  driver, Direct3D 12 and Vulkan (tests/MicroEngine.Desktop.Tests, category `Gpu`).
- **CI on lavapipe.** The native Linux job installs Mesa's Vulkan drivers and runs the `Gpu` tests with the Vulkan
  loader pointed at lavapipe only; `MICROENGINE_GPU_ADAPTER=llvmpipe` makes every device creation fail unless SDL_GPU
  reports that adapter, and the adapter line is written to the job summary.

## Consequences

- Tier 1 is executable and checked on every change to the engine, on a software rasterizer, without a GPU runner.
- The suite's custom shaders are committed blobs (tests/Shared/Graphics/Shaders), compiled from their HLSL by the
  offline toolchain, because DXC's packaged binaries run only on Windows.
- Risks kept: SDL 3.5.0 is a development series; Metal and MSL are not executed (no macOS GPU run); mobile and the
  per-frame surface copy cost on phones are unmeasured; WARP is not reachable through SDL_GPU's properties.
