# ADR 0071: Post-processing is a chain made of the drawing API, on a scene, the canvas or the window; mip levels are a texture filter; draw order is a renderer that sorts

## Status

Accepted (2026-11-20). The SDK is in Alpha. Evidence: [GRAPHICS-3](../evaluations/graphics-3.md). Issue
NastMz/MicroEngine#181, part of [CONSOLIDATION-1](../evaluations/consolidation-1.md). Builds on
[ADR 0064](0064-drawing-api-over-the-device-layer.md) (states, materials and render targets on `IRenderer`) and
[ADR 0029](0029-presentation-replaces-canvas-scaling.md) (the presentation). Amends
[ADR 0057](0057-materials-on-the-2d-batcher.md): its "effects are examples, never engine features" no longer holds
for post-processing, and "mipmap generation remains outside the contract" no longer holds.

## Context

After ADR 0064 a game could build a post-processing chain from render targets and materials, and the example did:
three targets, two shaders and a helper, about sixty lines, for a glow, a vignette and scanlines. Nothing could
process the whole canvas or the window, the interface was never included, no device had mip levels, and the order of
drawing was the order of the calls. ADR 0057 had left a post-processing stack out because "no scenario asked for
one", a reason CONSOLIDATION-1 does not accept.

One fact decided most of the design: the default desktop device (SDL's renderer) and every browser build run no
shaders. An effect that exists only as a shader is, by default, not seen.

## Decision

- **A chain made of the drawing API.** `PostProcess` holds `PostEffect`s and runs a picture through them with
  `IRenderer` calls only (targets, states, materials), so one implementation runs on every renderer, the headless one
  included. The chain owns the picture, two targets that take turns, and per-effect targets and textures, sized to
  the picture; it is released between frames. `PostPass` is what an effect works with, so a game writes effects with
  the same calls the engine's use.
- **Ready effects, with a stated form where no shader runs.** `ColorGrade`, `Blur`, `Bloom`, `Vignette`,
  `Scanlines`, `Crt`, and `ShaderEffect` for a material of the game's own. `Vignette` and `Scanlines` are a
  generated texture and the multiply blend state on every device: the same picture everywhere. `Blur` and `Bloom`
  have a second form made of halving with linear sampling and the multiply and additive presets; `ColorGrade` keeps
  tint and brightness; `Crt` keeps its lines; `ShaderEffect` is left out. `PostEffect.SupportOn` answers `Full`,
  `Reduced` or `None`; `IGraphicsDevice.SupportsShaders` says whether shaders run. The three built-in fragment
  shaders are compiled ahead of time and embedded in `MicroEngine.Graphics2D`.
- **Pictures keep their transparency.** Effects work on premultiplied pictures and keep their alpha, so a scene's
  chain processes that scene alone and scenes under it show through.
- **Three places.** `Scene.PostProcess` (what the scene draws; released when the scene leaves the stack),
  `Game.PostProcess` (the whole canvas, at its resolution), `Game.ScreenPostProcess` (the whole window).
- **The window stage is the platform's, as a capability.** `IScreenEffects` (SDL, raylib, headless): the window's
  picture (letterbox, canvas with its prescale, UI view) is drawn off screen at the window's size by the presenter,
  run through the chain, and copied to the window. With no active chain the earlier composition runs unchanged. A
  platform without the capability: the game runs the chain on the canvas. Captures of a frame presented through the
  chain are the window as presented.
- **Mip levels are a filter.** `TextureFilter.LinearMipmap` creates the levels (and remakes them on update) where
  the device declares `Mipmaps` (`sdl-gpu`, raylib on desktop, through a new `CommandList.GenerateMipmaps`), and is
  linear with one level everywhere else. `Texture.MipLevels` says which. A filter, because a filter already travels
  through assets, atlases and the game's default.
- **Draw order is a renderer.** `SortedRenderer` records the draws made between `Begin` and `End` and replays them
  by layer, depth and call order, each under the camera, blending, clip and states it was recorded with. It wraps any
  `IRenderer`.
- **The state hash gains nothing.** All of this only draws.
- **Not added, each with its reason in the evaluation:** stencil, several colour targets, multisampling, compressed
  textures, and the shader tool on Linux and macOS (the last one left as its own unit).

## Alternatives rejected

- **Effects on the device layer** (command lists, pipelines): faster by one copy, but a second implementation per
  capability tier, and effects a game could not write with the drawing API.
- **Effects as shaders only, with the material's fallback elsewhere**: nothing to see on the default device or in a
  browser.
- **A material on `Presentation` as the final shader**: one pass, no chain, no ready effects, nothing on tier 0.
- **Drawing the interface into the canvas while effects are on**: loses the interface at the window's resolution.
- **Depth and sort on `IRenderer`**: new overloads on every renderer and test double, for a sort that a wrapper does
  everywhere.
- **Mip levels as an option beside the filter** (`TextureOptions`): a new parameter through every loader and asset
  key for what one more filter value says.
- **Uploading levels made on the CPU**: per-level upload on every backend, and still nothing on SDL's renderer.

## Consequences

- A full-screen effect is one line in a scene or a game, on every platform, and says what the device in use leaves
  out. The examples' hand-written passes and their two shaders are gone; a third shader shows a pass of the game's
  own.
- A chain costs one copy more than a hand-written one, and the window stage two window-sized targets while active.
- Three effects have two implementations to keep in step; the conformance suite draws both on every renderer.
- Mip levels are absent on SDL's default device and in browsers (SDL_Renderer has none), and present on raylib: a
  game sees the difference only in `Texture.MipLevels` and in the picture.
- `TextureFilter` has a third value: code that compared with `Linear` compares with `Nearest`.
- Open: antialiased shapes on HD canvases, compressed textures, the shader tool on Linux and macOS.
