# ADR 0057: Developer-authored rendering is render targets, blend states and materials on the 2D batcher; effects are examples, never engine features

## Status

Accepted (2026-11-14): the owner accepted it on the ground that the SDK is in Alpha, where a change of public API
carries no compatibility commitment. Evidence:
[GRAPHICS-1](../evaluations/graphics-1.md). Task record: [`odd/tasks/graphics-1.md`](../../odd/tasks/graphics-1.md).
Builds on [ADR 0028](0028-graphics-device-contract.md) (the device contract),
[0041](0041-tier1-device-on-sdl-gpu.md), [0042](0042-offline-shader-toolchain.md) (shaders compiled ahead of time) and
[0046](0046-tier1-device-selection.md) (`DeviceRenderer.Flush`/`Target`).

Amended (2026-11-19) by [ADR 0064](0064-drawing-api-over-the-device-layer.md): "Tier 0 stays shader-free" still
holds for shaders, but a material no longer refuses there, it draws with a declared fallback; a material needs only its
fragment shader (the engine supplies the vertex stage); parameters are set by name, from reflection in the compiled
file; and "a render target as a texture of the drawing API" is decided: it is one. The rest of this record stands.

Amended (2026-11-20) by [ADR 0071](0071-post-processing-window-stage-mip-levels-draw-order.md): "Effects are
examples" no longer holds for post-processing: the engine ships a chain (`PostProcess`) and ready effects, with a
stated form where no shader runs. "A pass at the window's resolution or on the composition of the canvas" is decided:
`Game.ScreenPostProcess`. Mipmap generation is in the contract (`CommandList.GenerateMipmaps`). Light with occlusion,
masks and water stay examples; stencil, several colour targets and multisampling stay outside the contract, for the
reasons in [GRAPHICS-3](../evaluations/graphics-3.md), section 5, not because no scenario asked. Compiling shaders
on Linux is still open.

## Context

The device contract has render targets, blend states and, on tier 1, custom shaders with uniforms; a scene reaches the
device and the canvas through `DeviceGraphics` and `DeviceRenderer`. GRAPHICS-1 built four techniques as a consumer:
a light occluded by walls and a mask needed nothing new and ran on every device, browsers included; a water shader and
a post-processing chain ran on `sdl-gpu` only through a render pass the game wrote by hand for each quad, because the
2D batcher drew with the built-in pipeline only. A shader therefore could not be applied to anything the batcher
draws (sprites, geometry, text, in a camera's transform). Frameworks on SDL_GPU solve this with a material the batcher
takes (GRAPHICS-1 §6).

## Decision

- **A material on the batcher.** `Material2D` (`MicroEngine.Graphics2D`) is a vertex and a fragment shader the game
  created, the fragment stage's uniform data and its textures from slot 1 on. `RenderState.Material` and
  `BatchSettings.Material` make the batcher draw with it; everything else in the state applies unchanged. The batcher
  supplies the pixel-to-clip matrix (vertex uniform slot 0) and the draw's texture (fragment slot 0). A draw keeps the
  parameters set when it was recorded. Pipelines are created per blend state and target format on first use.
- **Additive only.** The device contract (`IGraphicsDevice`, `CommandList`) does not change. The two members are
  `init` properties, so the records' constructors and deconstructors are as they were, and a batch without a material
  is drawn as before.
- **Tier 0 stays shader-free.** A device without `GraphicsFeatures.Shaders` refuses when the shaders are created
  (`GraphicsCapabilityException`), as for any undeclared capability; there is no fallback inside the engine. A game
  that wants to run everywhere keeps a plain path.
- **Effects are examples.** Light with occlusion, masks, water and post-processing live in `samples/Examples` and in
  the guide. The engine ships no lighting system, no mask type, no post-processing stack and no shader library;
  `LightMap` stays the only built-in light and is not extended.
- **No new dependency.** The toolchain stays DXC and SPIRV-Cross ahead of time. SDL_shadercross, Slang, Linux DXC and
  the frameworks compared in GRAPHICS-1 §6 are not adopted here.

## Consequences

- A game on `sdl-gpu` applies its own shaders to sprites, geometry, text and whole targets with the batcher it
  already knows; the same code path serves a one-quad pass and a material on a thousand sprites.
- Shader parameters are matched to the shader's block by layout: a struct that does not follow the block gives wrong
  values, not an error. Named parameters need reflection in the compiled shader file and are deferred.
- Shaders do not run on the default SDL device, on raylib or in browsers. Browser shaders need a second programmable
  device and shader target, which is a backend, not an extension; deferred until SDL_GPU has a web backend.
- Not decided here, for the owner (GRAPHICS-1 §7): compiling shaders on Linux, a pass at the window's resolution or on
  the composition of the canvas, and a render target as a texture of the drawing API.
- Stencil, several colour targets, multisampling and mipmap generation remain outside the contract; each would be an
  extension on every backend, and no scenario asked for one.
