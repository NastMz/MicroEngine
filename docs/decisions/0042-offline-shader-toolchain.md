# ADR 0042: Shaders compiled ahead of time by `microengine shaders`

## Status

Accepted (2026-11-03). Completes prototype P2 (ARCH-1 §9, table of prototypes: "one palette-swap and one full-screen
effect with shaders compiled offline"). Builds on directive 2 item 15 (toolchain input from P3:
[`experiments/arch-1/p3-sdl-gpu-3d`](../../experiments/arch-1/p3-sdl-gpu-3d/README.md), "Shader toolchain").

## Context

The tier-1 device ([ADR 0041](0041-tier1-device-on-sdl-gpu.md)) accepts custom shaders only as code a tool produced
ahead of time (contract rule: shaders are validated at creation, never compiled at draw). SDL_GPU takes DXIL on Direct3D 12, SPIR-V on Vulkan and MSL on Metal, plus the resource counts of each shader.
SDL_shadercross does this at run time but has no package or release; P3 showed that the same two components (DXC and
SPIRV-Cross) run ahead of time from NuGet packages. The repository needs one tool for the device's own built-in shaders,
the conformance suite's shaders and games' shaders, and a file a game ships.

## Decision

- **`CompiledShader`** (`MicroEngine.Graphics`, a helper over the contract, no contract change): one shader stage with
  code per format and the reflected `ShaderResources`, stored as a `.shader` file (UTF-8 JSON, base64 code, fixed
  field order so the same compilation gives the same bytes; `version` 1). `DescriptionFor(capabilities)` gives each API
  its native format (DXIL on `direct3d12`, MSL on `metal`, SPIR-V on `vulkan`); a device without
  `GraphicsFeatures.Shaders`, or with no format in common, gets `GraphicsCapabilityException`.
- **`microengine shaders`** compiles `*.vert.hlsl` / `*.frag.hlsl` (entry `main`, shader model 6.0, SDL_GPU register
  spaces) into `.shader` files: DXC for DXIL (`-Qstrip_debug`) and SPIR-V (`-spirv -fspv-target-env=vulkan1.0`),
  SPIRV-Cross for MSL 2.1 and reflection. Sources are passed by file name from their own folder, so no machine path
  reaches the output (compiling the same file from two folders gave identical bytes). `--check` compares by parsed
  content (Git's line-ending conversion of the JSON does not count as a change).
- **DXC is not redistributed.** `dxil.dll` is under Microsoft's licence terms ("solely for use on Windows"), with data
  collection clauses, so the MIT tool package does not carry it: the tool restores `Microsoft.Direct3D.DXC` 1.8.2505.32
  into the developer's NuGet packages folder on first use (or uses `MICROENGINE_DXC`). SPIRV-Cross (Apache 2.0, through
  Silk.NET, MIT) ships with the tool. Games ship only compiled shaders.
- **Windows only for compiling.** The DXC package has Windows binaries only; elsewhere the command stops with exit code 3
  and says why. Compiled files load on every platform, so the repository commits them, and a Windows test
  (`MicroEngine.Cli.Tests`) recompiles every shader folder in the repository and fails when a committed file is stale.
- **The device's built-ins use the same files**: `MicroEngine.Graphics.SdlGpu` embeds its sprite shaders as `.shader`
  files and loads them through `CompiledShader`.
- **P2 evidence** (device conformance suite): a palette swap (an indexed image through a 256-colour palette, recoloured
  by updating only the palette) and a full-screen effect (the scene desaturated by a fragment uniform), exact or within
  ±1/255 on tier 1; on tier 0 creating either shader is the typed error naming `Shaders`.

## Consequences

- A game with shaders needs a Windows machine (or a Windows CI job) to change them; reading them needs nothing.
- MSL is generated and its entry point is `main0`, but it has not been executed (no Metal run); SDL_GPU's Metal
  binding of vertex buffers and uniforms to MSL buffer indices is unverified.
- Non-Windows DXC (the release archives on GitHub, or a future package) would remove the Windows requirement; it is not
  pursued here.
