# ADR 0046: Choosing the tier-1 device through the normal composition path

## Status

Accepted (2026-11-04). Extends [ADR 0041](0041-tier1-device-on-sdl-gpu.md) (the device), [ADR 0043](0043-minimal-3d-path.md)
(the 3D path, whose consequence "not selectable by the engine's loop" this closes) and
[ADR 0038](0038-one-composition-path.md) (one composition path). Evidence: ARCH-1 §9.1 item 2 and the tests below.
Graphics contract v1 unchanged.

## Context

The 3D slice ran outside the engine: the sample opened its own window, drove `FrameClock` and `SceneWorld` itself and
created the tier-1 device by hand. Phase II needs the tier-1 device chosen like any other platform option, by the game or
the launcher, and a game drawing 3D, 2D and its UI view from an ordinary scene under the engine's loop.

## Decision

- **A graphics device is a start-time option**: `GraphicsSettings.Device` (null: the platform's default), overridden for
  one run by the launcher's `--graphics NAME` (`LaunchRequest.Graphics`, `LaunchArguments.Graphics`).
- **Profiles declare the devices they offer**: `PlatformProfile.GraphicsDevices` (empty: only the platform's own).
  `PlatformProfile.Create` refuses a request naming another device with `PlatformConfigurationException` naming what it
  offers, so the launcher's fallback moves to the next platform and says why, and a platform named with `--platform`
  stops. No platform ever runs a game on another device than the one requested.
- **The SDL backend registers both devices**: the `sdl` profile offers `sdl-renderer` (default, tier 0) and `sdl-gpu`
  (tier 1); fixed-driver profiles (`sdl-software`...) only `sdl-renderer`. On `sdl-gpu` the platform creates
  `SdlGpuDevice` on its window (the first preferred renderer naming `direct3d12`, `vulkan` or `metal` chooses the API;
  `--gpu power-saving` prefers the low-power adapter); where SDL_GPU cannot start it stops with the factory's reason and
  how to run instead. The canvas, presentation, captures and UI view are the device-contract code both devices share;
  the differences are named: VSync is fixed at creation (SDL_GPU's present mode), and on-screen touch controls (drawn
  with SDL_Renderer) are not drawn. `MicroEngine.Sdl` references and carries `MicroEngine.Graphics.SdlGpu.dll`.
  `SdlPlatform.Device` becomes `GraphicsDeviceBase` (migration note).
- **3D from a scene**: `DeviceRenderer` gains `Target` (the canvas during the frame) and `Flush()` (submits what the batch
  recorded so far, the frame's clear first, and keeps the frame open), so a scene puts a `Renderer3D` pass between
  2D flushes. `Renderer3D.Begin` accepts a target without depth (the canvas) and then depth-tests against a depth texture
  of its own of the same size.
- **`samples/Slice3D` is an ordinary game**: a `Configure` entry point, `DesktopLauncher.Run`, `GameSettings.Graphics.Device
  = "sdl-gpu"`, its world in `Scene.Objects` stepped at 30 Hz by the engine and drawn at the frame's blend, 2D HUD on the
  canvas and a label in the UI view; a script captures a deterministic frame (run in CI on lavapipe).

## Evidence

Reference device: `Flush` puts device work between the 2D drawn before and after it; without `Flush` the frame's clear
covers it; outside a frame it does nothing. Tier 1 (D3D12, Vulkan; lavapipe in CI): the same ordering with a real 3D
pass; a game launched with `--graphics sdl-gpu` draws its world's 3D, 2D over it and its UI view at the window's
resolution on the tier-1 device; `sdl-software` and an unknown device name are refused naming what is offered; SDL_GPU
that cannot start (Metal asked for on Windows/Linux) stops with the reason and never runs on tier 0. Launcher tests:
`--graphics` parsing and override, a device reaching only a platform that offers it, no change without a device.

## Consequences

- Games keep tier 0 by default; tier 1 is an explicit request with a clear failure.
- An intermittent crash of the test host on the development machine (Windows 11, NVIDIA RTX 4070 Ti SUPER, SDL_GPU
  tests): 2 in 10 full runs and 1 in 4 Vulkan-only runs on the pre-merge 6a-1 build; 0 in 12 (main after 6a-1) and 0 in
  10 (6a-2) Vulkan-only runs; 1 in 40 full `Gpu`-category runs on 6a-4. **Explained: a defect in NVIDIA's driver, an
  environmental incompatibility, not in SDL or the engine.** The full dump of the last crash, read with Microsoft's
  native debugger (cdb, Microsoft symbol server; SDL3 ships without a pdb), shows:
  - The fault is an access violation (`c0000005`, read of `ffffffffffffffff`) on a thread whose entry point is in
    `nvoglv64.dll` 32.0.16.1088 (NVIDIA's OpenGL/Vulkan driver, which exports `vk_icdNegotiateLoaderICDInterfaceVersion`).
    Its whole stack is `RtlUserThreadStart` > `BaseThreadInitThunk` > `nvoglv64!DrvValidateVersion+0xe015`: no SDL,
    engine, OBS or Overwolf frame.
  - The faulting instruction is `call qword ptr [rbx+10h]`: the driver calls through an object whose memory has already
    been freed and reused. It now holds a UTF-16 path into NVIDIA's own shader cache
    (`...\AppData\Local/NVIDIA/GLCache/.../*.bin`), so the call target is text. The driver thread used one of its own
    objects after freeing it.
  - At that moment the test thread was opening the next test's hidden window (`SDL_CreateWindow` > `NtUserGetDC`), after
    the previous test had disposed its device. `SdlGpuDevice` disposes in the order SDL requires
    (`SDL_WaitForGPUIdle`, release resources, release the window, `SDL_DestroyGPUDevice`), so a driver thread outliving
    its device is the driver's race, which appears only when one process creates and destroys many Vulkan devices in
    quick succession (the conformance suite does so per test; a game keeps one).
  - The OBS and Overwolf capture hooks are loaded in the process but are on no stack of the fault, so they are not the
    cause.

  Nothing in SDL or the engine changes. CI keeps running the tier-1 conformance on lavapipe whenever the device changes;
  the manual states the limit.
- The browser, Android and iOS hosts keep tier 0 (no SDL_GPU path there in Phase II).
