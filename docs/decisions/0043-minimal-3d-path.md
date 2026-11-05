# ADR 0043: A minimal 3D path on the tier-1 device

## Status

Accepted (2026-11-03). Builds on [ADR 0041](0041-tier1-device-on-sdl-gpu.md) (tier-1 device),
[ADR 0042](0042-offline-shader-toolchain.md) (shaders compiled ahead of time), [ADR 0030](0030-one-object-model.md)
(one object model with 3D transforms and interpolation) and [ADR 0034](0034-content-handles-loaders-sources.md) (asset
kinds). Evidence: ARCH-1 §9.1 item 2 (the 3D vertical slice) and the tests listed below. Graphics contract v1 is
unchanged.

## Context

Phase II counts the architecture as ready for 3D only when a real implementation exercises the contracts end to end:
a mesh loaded as an asset, a world with a transform hierarchy and engine interpolation, a perspective camera, depth,
one material with an offline-compiled shader, composited with 2D on the same device, tested in CI on a software
rasterizer. It is explicitly not a 3D game: no lighting model beyond the one material, no skinning or animation.

## Decision

- **`MicroEngine.Graphics3D`**, a new assembly referencing the graphics contract, content and the scene world (the
  architecture test checks it), plus StbImageSharp for glTF images. Not packaged.
- **glTF through a minimal reader, not SharpGLTF.** The slice needs positions, normals, one UV set, indices, node
  transforms and one base colour texture. A reader of about 300 lines on `System.Text.Json` covers that, is
  trimming/AOT-safe like the other content assemblies, adds no dependency, and refuses every feature it does not read
  (required extensions, skins, morph targets, sparse accessors, non-triangle modes) with an error naming it. SharpGLTF
  would bring the whole glTF object model and a larger dependency for features the slice must not use. The reader is
  the `mesh` asset kind (`MeshAssets.AddMeshes`), so meshes load through the asset store and content sources.
- **`Camera3D`** wraps the System.Numerics conventions fixed by the contract (right-handed `CreateLookAt`,
  `CreatePerspectiveFieldOfView`, depth 0..1) and projects world points to pixels for tests.
- **`Renderer3D`**: meshes uploaded once (32-byte `MeshVertex`, 32-bit indices, premultiplied base colour texture); a
  frame records one pass with a depth attachment (cleared) and the colour attachment cleared or loaded; one custom
  pipeline (`basic.vert`/`basic.frag`, compiled by `microengine shaders`, embedded) with depth less/write and back-face
  culling; per-draw vertex uniforms (world-view-projection, world) and fragment uniforms (base colour × tint, light).
  `DrawWorld(IWorld)` draws `MeshRenderer` attachments at `IWorld.WorldMatrix`, i.e. the engine's interpolation.
- **Composition with 2D** is ordinary contract use: the 2D batcher draws into the same target after the 3D pass
  without clearing; no special interface (directive A2).
- **Tier 0** gets `GraphicsCapabilityException` from the renderer's constructor.

## Evidence

- Property tests on SDL_GPU (`Slice3DTests`, D3D12 and Vulkan on NVIDIA, Vulkan on AMD, lavapipe in CI): the nearer
  mesh hides the farther one in either draw order (fails with depth testing removed); four known world points land on
  the pixels `Camera3D.Project` gives, with each silhouette inside its projected corners ±1 px; an opaque and a
  half-alpha 2D quad over the 3D frame (exact, and premultiplied over within ±1/255); a parent moved and turned in one
  step draws its child at blend 0, 0.5 and 1 exactly where the interpolated hierarchy puts it; a glTF asset loaded
  through the store shows its texture's four texels on the right quadrants.
- CPU tests: the glTF reader (exact vertices from .gltf and .glb, column-major node matrices, TRS nodes moving normals,
  computed normals, refused features) and the typed error on the reference device.
- `samples/Slice3D`: a three-level hierarchy at 30 Hz drawn at the frame clock's blend, a glTF crate made in the
  repository, a 2D HUD over the 3D. Its `--measure-e12` mode measures the surface copy of ADR 0041 (see the PR record).

## Consequences

- The tier-1 device is still not selectable by the engine's `Game` loop or `SdlPlatform`; the sample composes the
  frame clock, the world and the device itself. Wiring a tier-1 platform is later work.
- Lighting, materials beyond the one, shadows, skinning, animation, instancing of meshes and culling are out of scope.
