# ADR 0064: The drawing API is a view of the device layer: one texture, states, materials and targets on `IRenderer`, graphics resources as assets

## Status

Accepted (2026-11-19). The SDK is in Alpha. Evidence: [RENDER-1](../evaluations/render-1.md). Issue
NastMz/MicroEngine#174, part of [CONSOLIDATION-1](../evaluations/consolidation-1.md). Amends
[ADR 0057](0057-materials-on-the-2d-batcher.md) (three of its consequences no longer hold); builds on
[0028](0028-graphics-device-contract.md), [0042](0042-offline-shader-toolchain.md) and
[0046](0046-tier1-device-selection.md).

## Context

Two type families shared `MicroEngine.Graphics`: the drawing API a game uses (`Texture`, `IGraphicsDevice`,
`IRenderer`, `Sprite`) and the device layer (`Devices.Texture`, `Renderer2D`). The bridge between them was internal
and one-way, and `IRenderer` had three blend modes and nothing else. A render target could not be a `Texture` or a
`Sprite`; a shader took a cast of `Game.Graphics`, a second batcher, a manual flush, a hand-copied vertex shader, a
parameter struct matched by layout and five disposals; a game that wanted to run everywhere wrote a second drawing
path; and shaders, materials and render targets lived outside `Assets`.

## Decision

- **One texture type, by exposure.** `Texture` stays the type games draw; it exposes the device texture behind it
  (`Native`, null on the headless platform), its real memory (`SizeInBytes` from the format and mip levels) and
  whether it can be drawn into. `DeviceGraphics.Wrap` makes any device texture or render target a `Texture` or a
  `RenderTarget2D`. The two types are not merged: the headless platform draws without a device.
- **States and targets on `IRenderer`.** `PushState(DrawState)`/`PopState` (blend state, sampler, material; a part left
  null keeps the current one) and `PushTarget(RenderTarget2D)`/`PopTarget`, scoped like `PushCamera`. A target starts
  like a frame and the pop restores the canvas's camera, clip, blending and state. On a device they are the batcher's
  state and the batch's target (`Renderer2D.SetTarget`), so a frame stays one batch. `SetBlendMode` is unchanged.
  The four members have default implementations that throw `NotSupportedException`, for renderers that are test
  doubles; every renderer of the engine implements them.
- **A material is a fragment shader.** `Material2D.Create` (through `IGraphicsDevice.CreateMaterial` or
  `Assets.Material`) takes a compiled fragment shader; the vertex stage is the engine's, embedded
  (`Material2D.SpriteVertexShader`, the tier-1 device's own sprite vertex shader).
- **Parameters by name, from the file.** The `.shader` format (version 2) carries each uniform block's slot and size,
  each value's name, offset and size and each texture's name and slot, reflected by SPIRV-Cross from SPIR-V compiled
  with Direct3D's layout rules, so one set of offsets holds for DXIL, SPIR-V and MSL. `Set(name, value)` and
  `SetTexture(name, texture)` refuse an unknown name or a wrong size with a message that lists the shader's
  parameters. Version 1 files still load, by slot only.
- **One behaviour without shaders.** On a device without `GraphicsFeatures.Shaders` (and on the headless platform) a
  material is created, checks its parameters identically, and its draws are made by the built-in pipeline with its
  declared `MaterialFallback` (plain, a tint, a blend state, or hidden). `IsSupported` is queryable. The engine does
  not emulate shaders.
- **Graphics resources are assets.** Kinds `shader`, `material` and `render target` are ordinary loaders of the asset
  store; a render target is keyed by a name the game gives, its size and its filter. No new ownership mechanism: a
  loader need not read a file, and the content source now opens on first read.
- **The device layer stays public** as the lower level (`Renderer2D`, `Devices.*`, `DeviceRenderer.Target` and
  `Flush`), required for nothing above.
- **The `Legacy` folder is `Drawing`.** Namespaces and type-forwards are unchanged.

## Consequences

- The water example went from ten steps and about 45 lines of setup, glue and disposal to two asset calls, six named
  parameters and a pushed state; the four rendering examples have one code path and no device access.
- The renderer conformance suite holds targets, states and materials to the same results on the headless rasterizer,
  every SDL render driver, raylib and, newly, the tier-1 device.
- Colours of the drawing API are 8-bit: an effect ported from the batcher's float colours can differ by one step of
  255 (the light example does, in 990 canvas pixels).
- A class that implements the drawing API's `IGraphicsDevice` must add `CreateRenderTarget`.
- A material's vertex stage is fixed. A game that needs its own builds the material on the device
  (`new Material2D(vertex, fragment, reflection)`).
- The canvas itself is not a texture, so the drawing API has no pass over the finished frame of every scene; that, a
  present-stage shader, mipmaps, compressed formats through `Assets.Texture`, stencil and multisampling are left to
  the graphics initiative that follows (NastMz/MicroEngine#181), which now has one texture type and one draw path to
  build on.
- Disposing a material inside the frame that drew with it fails when the frame is submitted; resources owned by
  scopes are released between frames.
