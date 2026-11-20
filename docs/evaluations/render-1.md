# RENDER-1: one texture type, materials on the normal draw path, graphics resources as assets

Issue #174, task T3 of [`odd/tasks/consolidation-1.md`](../../odd/tasks/consolidation-1.md). Decision:
[ADR 0064](../decisions/0064-drawing-api-over-the-device-layer.md), which amends
[ADR 0057](../decisions/0057-materials-on-the-2d-batcher.md). Run on 2026-11-19 on Windows 11 (Direct3D 12 for the
tier-1 device). Statements are **demonstrated** (run here, with a test or a capture that was looked at) unless marked
**not verified**.

## A. Need

A developer outside the repository who wants a shader on a sprite, a picture drawn off screen, or a blend state beyond
three modes should write that, not the integration between two layers of the engine. The audit
([CONSOLIDATION-1](consolidation-1.md) §2.2) found the integration was the developer's.

## B. Current experience

Before this change, the water example needed, in order: (1) ask for `sdl-gpu`; (2) cast `Game.Graphics` to
`DeviceGraphics` and check `Has(Shaders)`; (3) create a second `Renderer2D`; (4) ship and load a copy of the sprite
vertex shader; (5) parse and create the fragment shader; (6) `new Material2D`; (7) create a render target on the
device; (8) write a struct with the layout and padding of the `cbuffer`; (9) in `Draw`, cast the renderer, `Flush()`,
`Begin`/`Draw`/`End` twice on the private batcher; (10) dispose five objects, and keep a second drawing path for
devices without shaders. About 45 lines that are not the effect. A mismatch between the struct and the block gave
wrong pixels, not an error. A render target could not be a `Texture` or a `Sprite`.

## C. Alternatives

| Choice | Taken | Rejected, and why |
| --- | --- | --- |
| One texture type | `Texture` exposes its device texture (`Native`); `DeviceGraphics.Wrap` goes the other way | Merging `Texture` with `Devices.Texture`: the headless platform has no device, and putting the reference device under it would change every headless capture the demos' tests compare. A public `DeviceTexture` constructor alone: still needs the cast for the common case, a render target |
| State on the draw path | Scoped `PushState(DrawState)` with nullable parts | Separate `SetSampler`/`SetMaterial` setters like `SetBlendMode`: nothing restores them, and a material left set breaks the next scene. Reusing the batcher's `RenderState`: its scissor is in target pixels and collides with `SetClip` |
| Targets | `PushTarget(RenderTarget2D)`; the batch retargets | A renderer per target (`new DeviceRenderer(batcher, graphics, target)`, what the examples did): a second batcher and a manual flush to order them |
| Interface evolution | Four members with defaults that throw | Abstract members: every `IRenderer` test double in games' test projects stops compiling for a feature it does not use. Defaults that do nothing: a double would record a target's draws as the canvas's |
| Vertex stage | Embed the tier-1 device's compiled sprite vertex shader in `MicroEngine.Graphics2D` | A built-in shader on the device contract: changes every backend for what one embedded file gives |
| Parameter names | Reflection in the `.shader` file (version 2), from SPIRV-Cross | Reflecting at run time: needs a shader compiler in the game. A source generator from HLSL: a second toolchain. Keeping layout structs: the silent mismatch stays |
| No shaders on the device | A declared fallback on the material, applied by the renderer | Refusing (ADR 0057): every game writes the second path. Emulating shaders on the CPU: a second shading language implementation |
| Ownership | Three loader kinds in the existing asset store | A resource registry beside it: a second lifetime model |

## D. Target experience

```csharp
// Before: Enter (after the casts and checks) and Exit
_batcher = new Renderer2D(device);
_vertex = Dev.CompiledShader.Parse(Assets.Bytes("shaders/sprite.vert.shader")).Create(device);
_fragment = Dev.CompiledShader.Parse(Assets.Bytes("shaders/water.frag.shader")).Create(device);
_water = new Material2D(_vertex, _fragment);
_above = device.CreateRenderTarget(new Dev.RenderTargetDescription(320, 180));
// ... _above, _water, _fragment, _vertex, _batcher disposed by hand

// After
_assets = Assets.CreateScope("water");
_water = _assets.Material("shaders/water.frag.shader");
_above = _assets.RenderTarget("water/above", 320, 180);
// ... _assets.Dispose()
```

```csharp
// Before: Draw
canvas.Flush();
_batcher.Begin(new BatchSettings(_above, Blend: Dev.BlendState.Opaque));
_batcher.Draw(canvas.Target.Color!, Whole, Matrix3x2.Identity, ColorF.White);
_batcher.End();
_water.SetUniforms(new WaterParameters { Tint = ..., Time = _time, ... });   // a struct laid out as the cbuffer
_batcher.Begin(new BatchSettings(canvas.Target, Blend: Dev.BlendState.Opaque) { Material = _water });
_batcher.Draw(_above.Color!, Water, Matrix3x2.CreateTranslation(0, Surface), ColorF.White);
_batcher.End();

// After
r.PushTarget(_above); /* draw the world */ r.PopTarget();
r.Draw(_above.Texture, Whole, Vector2.Zero, Color.White);
_water.Set("Time", _time);                                   // by name, checked
r.PushState(DrawState.Opaque with { Material = _water });
r.Draw(_above.Texture, Water, new Vector2(0, Surface), Color.White);
r.PopState();
```

The same code runs on every device; where shaders cannot run the water is flat (the fallback) and one line says so.

## E. Responsibility boundary

The engine owns: the bridge between the drawing API and the device, the vertex stage, parameter reflection and
checking, the behaviour without shaders, and resource lifetime. The game owns: the fragment shader, its parameters'
values, what it draws into which target, and the fallback it wants. The engine still ships no effect.

## F. Integration

`DeviceRenderer` (SDL, raylib), `HeadlessRenderer` and the UI view's renderer implement the four members; the headless
rasterizer draws into targets, computes any blend state and applies fallbacks, and state hashes do not depend on
drawing. Canvas prescale and whole-pixel presentation are untouched (the canvas target is restored by the pop; the
conformance suite runs through the platforms' real composition). The asset store's diagnostics count render targets
as texture memory and report a material loaded during play as a hitch, which the Examples sample now avoids by
loading its materials in `Load`. NativeAOT and trimming: no reflection; the embedded shader is a manifest resource.

## G. Trade-offs

- Colours through `IRenderer` are 8-bit; the batcher's are floats. The light example differs from its previous
  capture by 1/255 in 990 of 57,600 canvas pixels (a tint of 0.85 is now 217/255).
- A material's vertex stage is fixed, and the first texture of a shader is always the draw's texture.
- A default interface member is invisible on a concrete type, so `DeviceGraphics` and `HeadlessGraphicsDevice` repeat
  the convenience overloads.
- The canvas is not readable as a texture: a scene draws into its own target to post-process, and a pass over every
  scene's finished frame stays at the device level until the graphics initiative that follows (#181).
- Batching: no measurement was run; there was no concrete doubt. A frame that pushes nothing takes the same path as
  before and the batcher's exact counters are held by a test (8 sprites, 1 draw call, 1 submission; a pushed state
  equal to the current one adds no draw call; a target adds two submissions).

## H. Decision

Remodel, not rebuild: ADR 0064. The device contract, the batcher's algorithm and every backend are unchanged; the
drawing API became a view of them.

## I. Validation

- Renderer conformance (`tests/MicroEngine.Desktop.Tests/RendererConformance.cs`): 21 rules, 6 of them new, on the
  headless rasterizer, SDL's default driver, Direct3D 11, Direct3D 12, OpenGL, Vulkan, SDL's GPU render driver,
  software, raylib and the tier-1 `sdl-gpu` device (new in the suite, where materials must run their shader).
- Captures of the Examples tour before and after, on `sdl-renderer` and on `sdl-gpu`: water, post-processing and
  masks are byte-identical on both; light differs as said in G; the captures were looked at.
- Unit tests for the batcher's counters, wrapping, the headless renderer's record, parameter checking, the file
  format (version 1 still read) and asset ownership.
- **Not verified**: Vulkan and Metal for materials (the tier-1 device ran on Direct3D 12 here; Vulkan only through
  SDL's 2D render driver, which runs no shaders), Linux, macOS, browsers and phones.
