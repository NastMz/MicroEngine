# Draft: release notes for the next SDK version

Engineering record, not published. The ARCH-1 closing unit wrote this draft because `docs/guide/release-notes.md`
(the published page) was outside its edit surfaces. Its "Next release" section lists only changes merged before the
reconstruction's stages 2-6.

How to use it: in the release pull request ([releasing](../../releasing.md), step 1), merge the text below into
`docs/guide/release-notes.md`. It goes into the existing "Next release" section, which `node sdk/version.mjs release
<version>` turns into `## <version>`. Keep the items that are already there. The text contains no engineering codes,
because the site refuses them. Every breaking change has its migration step in
[`docs/migrations.md`](../../migrations.md), "Next release", "Upgrading a game".

The links in the block below are relative to `docs/guide/`, where the text goes, so they are kept as plain text
here. Their anchors are the ones `docs/migrations.md` already links to; check them again when you paste.

````markdown
**This version is a breaking Alpha.** The engine is rebuilt as modules. Several public APIs change; the
[migration notes](../migrations.md#next-release) open with an upgrade checklist.

**New**

- [The scene world](../manual/scenes.md#the-scene-world): `Scene.Objects` (an `IWorld`) holds a scene's objects as
  entities with 3D-storage transforms, a parent hierarchy and attachments of your own classes, interpolated between
  updates by the engine (`Teleport` to jump). It has world clocks for slow motion and hitstops (`Clock.Scale`,
  `Clock.Freeze`), `Scene.EngineStep`, `Camera2D.DrawPosition`, and scene results (`Scene<TResult>`, `Finish`).
- [Physics worlds](../manual/world.md#physics-worlds): a 2D physics contract (`IPhysicsWorld2D`) with declared
  capabilities and a typed error (`PhysicsCapabilityException`). The tile backend (`TilePhysicsWorld2D`) brings a swept
  mover, one-way platforms, moving solids and a grid broad phase.
- UI widgets: `Scene.Widgets` (`UiRoot`, `Label`, `Button`, `Panel`, `Stack`, `TextField` with IME, `Toasts`) as the
  highest input layer, with renamable widget actions and focus navigation. A UI view at the window's resolution
  (`Scene.DrawUi`, `Game.Views`, `Presentation.UiScale` following the screen's density). Captures show what the
  player sees.
- Input as events: device ids, every gamepad as a device of its own, players and pairing, input layers, layout key
  labels, rumble, text input, key chords, a rebindable `fullscreen` action, and scripted device events.
- A second graphics device on SDL: `GraphicsSettings.Device = "sdl-gpu"` (or `--graphics sdl-gpu`) draws through
  SDL_GPU, with depth and custom shaders. `microengine shaders` compiles HLSL ahead of time into `.shader` files
  (`CompiledShader`); compiling needs Windows. The graphics device contract (`MicroEngine.Graphics.Devices`) comes
  with a CPU reference device for tests.
- `Presentation` (`GameSettings.Presentation`) with the presets `PixelArt` and `HighResolution2D`, and
  `AspectMode.Expand`. `Camera2D` gains `Zoom`, `Rotation`, `WorldToView` and `VisibleBounds`.
- One composition per game: the `Configure(HostBuilder)` entry point runs on desktop, in browsers, on phones and in
  tests (`HostBuilder.Content`, `Storage`, `ConfigureEngine`). Content can come from zip archives and listed sources,
  and sounds and music load as assets (`Assets.Sound`, `Assets.Music`).
- One audio mixer on every platform, with the same voice limit, priorities, panning and fades. OGG music plays
  everywhere. SDL plays through a feeder thread, browsers through an AudioWorklet.
- Determinism checks: a per-step state hash (`StateHasher`, `IStateHashed`, `--state-trace FILE`) reports the first
  step where two runs differ.
- Optional modules: achievements, saves and touch controls cost nothing in a game that does not use them.

**Changed**

- Browsers run on SDL 3 compiled to WebAssembly, not raylib. Packages are about 190 KB smaller. Desktops keep raylib
  as the alternative platform.
- `Entity` and `EntityList<T>` are removed in favour of `Scene.Objects`.
- The retro synthesizer (`Synth`, `Song`, ...) is in `MicroEngine.Audio.Retro`.
- `TileMotion.Move` returns `TileMoveResult`.
- Achievements are declared before the engine is composed.
- Touch controls are opt-in outside touch-first screens.
- A tap shorter than a frame is down in exactly one update for every device.
- See the [migration notes](../migrations.md#next-release).

**Faster**

- The frame loop allocates nothing per frame.
- The audio feeder thread renders without garbage.

**Not in this version**

These are in the repository, not in the packages:

- the 3D path (glTF meshes, `Camera3D`, `Renderer3D`);
- the optional Box2D physics backend.

Metal shaders are generated but not yet run on Apple devices.
````
