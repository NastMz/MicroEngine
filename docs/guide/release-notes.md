# SDK release notes

One section per SDK version, newest first. The SDK is in Alpha: a version may change public APIs incompatibly, and
every such change is in the [migration notes](../migrations.md). Releases are tagged `sdk-v<version>`; the demo
releases (`v0.1.0`, `v0.2.0`...) are separate.

## Next release

A ninth package: `MicroEngine.Tiled`, optional, for maps made in the Tiled editor.

**Added**

- `MicroEngine.Tiled` loads `.tmx` and `.tmj` maps with their tilesets and templates through the game's assets
  (`Assets.TiledMap(path)`): tile layers with each cell's flips, tilesets as sprites (`TiledSprites`), objects and
  typed custom properties as data, a collision grid from a tile property the game names, and tile objects and parallax
  drawn as Tiled shows them ([Maps from Tiled](../manual/world.md#maps-from-tiled)). The `TiledLevel` sample plays a
  level saved by Tiled.
- `TileOrientation`, and a `TileRenderer.Draw` overload that draws each cell mirrored or turned, at an offset, tinted
  and with an extra margin for sprites larger than a cell.
- `Game.Audio` plays sounds that keep playing and change while they play
  ([Sounds that keep playing](../manual/audio.md#sounds-that-keep-playing)): `PlayVoice(name, parameters)` returns a
  `VoiceHandle` for a looping or one-shot sound on any bus with a priority; `Set(voice, volume, pan, pitch)`,
  `Pause`, `Stop(voice, fadeSeconds)` and `IsPlaying` control it; `AddBus`, `SetBusVolume`, `BusVolume` and `HasBus`
  group sounds under one volume; `PlayMusicVoice` plays registered music beside the music track. The pitch of a
  playing sound changes with `AudioEngine.SetPitch`, and the changes made to one sound in one update (volume, pan,
  pitch, pause, resume) take effect at the same instant, which moves the first of them slightly later than before
  ([migration notes](../migrations.md#several-changes-to-one-sound-in-the-same-update-apply-at-the-same-instant)). The Examples sample has a looping sound that follows a moving source.

**Fixed**

- Sounds are heard about 55 ms sooner on the SDL platform: a sound reaches the device about 65 ms after the update
  that played it instead of about 120 ms, and no longer stays later after a slow frame. The output queue was waited
  for twice; in browsers, whose queue is 100 ms, the same rule applies
  ([migration notes](../migrations.md#sounds-are-heard-sooner)).
- A value that is not a number (`float.NaN`) no longer reaches the sound: `AudioEngine.SetPitch` with it ended the
  handle while the sound played on, and as a volume, pan, pitch or bus volume it silenced or corrupted the output.
  `Play` uses the default for such a value; `Set`, `SetPitch`, bus volumes, `MusicVolume` and `SfxVolume` ignore it.
- When the output stopped taking audio while the game went on (a page whose sound the browser holds suspended), the
  mixer's queue filled and further calls were lost: a looping sound stopped then played on with no handle once the
  output resumed. Stops, pauses and the latest values of each sound and bus now apply when the output plays again,
  and a sound that cannot be queued is refused (`VoiceHandle.None`).
- `TileRenderer` skipped the tiles a zoomed-out or rotated camera shows beyond its unzoomed view: it now culls with
  `Camera2D.VisibleBounds`.
- `microengine package windows`, `linux` and `macos` stopped with `NETSDK1004` ("assets file not found") for a game
  whose `Directory.Build.props` moves the restore output when the package's publish properties are set: the tool
  looked for the .NET runtime pack the build used without those properties. It now reads it with the same properties
  as the publish.

## 0.6.0-alpha.1

An eighth package: `MicroEngine.Physics.Box2D`, optional, for dynamic bodies. Diagnostics for finding out why a game
misbehaves, and packaging with a .NET SDK newer than 10. The UI components and the project property deprecated earlier
are removed: read the [migration notes](../migrations.md#060-alpha1) before updating.

**New**

- [The Box2D backend](../manual/world.md#the-box2d-backend) is published: `MicroEngine.Physics.Box2D` runs Box2D 3.2
  behind `IPhysicsWorld2D` (dynamic bodies, rotation, circles, capsules, polygons, continuous collision, friction and
  mass) and carries Box2D's native library for Windows, Linux, macOS, Android, iOS and browsers. The tile backend
  stays in `MicroEngine` and needs no extra package.
- `Game.Diagnostics` measures a running game when you ask it to: the last frame's events, update, render and present
  times, fixed steps, allocated bytes, collections per generation, the graphics work it submitted and the audio voices
  (`LastFrame`), and the time of every frame system (`Systems`). `FrameSchedule.MeasureTime` and `Times(phase)` give
  the per-system times to code that drives the schedule itself. Off by default, and free while off.
- The engine publishes its frame time, phase times, allocations, draw calls and voices on the
  `System.Diagnostics.Metrics` meter `"MicroEngine"`: `dotnet-counters monitor --counters MicroEngine` shows them
  live, and attaching a listener turns the measurements on.
- `Game.DebugDraw` draws lines, rectangles, circles, points and text from any code, in world space (under the scene's
  camera) or screen space, for one frame or a number of seconds; calls do nothing until `DebugDraw.Enabled` is set.
  `DebugDraw.Physics(world)` draws every shape of a physics world, the tiles in view and the contacts of the latest
  step.
- Physics worlds read shapes back: `IPhysicsWorld2D.ShapesOf(body, span)` and `DescriptionOf(shape)`, declared by the
  new `PhysicsCapabilities.ShapeInspection` (both built-in backends declare it; a backend of your own that does not
  refuses them with `PhysicsCapabilityException`). `TilePhysicsWorld2D.GridOf(shape)` returns the tiles of a tile
  shape.
- `InputSystem.Trace` and `InputSystem.Routed` list the frame's input events with the layer that took each one (a
  menu, the on-screen controls) or none when it reached gameplay. The game's diagnostics turn the trace on while they
  measure.
- When a run fails, the desktop launcher writes a crash report (`CrashReport`) to the game's save folder and names it
  after the error: game and engine versions, system, platform facts, the last measured frame and its slowest systems,
  and the error with its stack trace.
- `PlatformRequest.Log`: what the SDL and raylib backends report (no audio device, no gamepads, a renderer that did
  not start) now goes through the game's `ILog` instead of straight to standard error. With the default log the
  console shows the same lines.
- `IGraphicsDevice.Counters` (in `MicroEngine.Graphics.Devices`) and `AudioMixer.Statistics` read draw calls and voices
  without casting to the backend's class.

**Removed**

- `DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `Toast`, `AchievementToasts`, `AchievementsScene`,
  `Game.UseAchievements` and `Game.AchievementToasts`, deprecated in 0.5.0-alpha.2. Build them from widgets and call
  `TrackAchievements` ([migration notes](../migrations.md#060-alpha1)).
- The `MicroEngineGameFactory` project property, deprecated in 0.5.0-alpha.1. `microengine package` builds browser,
  Android and iOS packages from the game's `Configure(HostBuilder)` entry point only, and stops with the method to add
  when a game has none. The tutorial's Gem Garden declares the entry point from its first step.

**Fixed**

- `microengine package` works with a .NET SDK newer than the game's .NET 10, such as .NET SDK 11: Windows and Linux
  packages stopped because the tool looked for the licence of the SDK's own runtime (11.0) instead of the .NET 10
  runtime the build used. Every package now carries the licence and notices of the runtime pack the build resolved,
  and `build-info.json` names that runtime's version.
- `microengine doctor` accepts any .NET SDK from 10 on, and `doctor web` names the workload that SDK needs for a .NET 10
  game: `wasm-tools-net10` with .NET SDK 11. `microengine info` shows the game's target framework (`net10.0`) instead
  of the SDK's runtime version.

## 0.5.0-alpha.2

UI primitives for any game's interface, and a seventh package: `MicroEngine.Text.Shaping`, optional, for Arabic,
Hebrew and other complex scripts. Nothing breaks; the genre components are deprecated and keep working.

**New**

- [Pointer events on widgets](../manual/ui.md#controls-of-your-own): a widget receives the press that starts on it
  (`OnPointerDown`), that press's moves wherever the pointer goes (`OnPointerMove`), its release (`OnPointerUp`) or its
  cancellation (`OnPointerCancel`), with positions in UI units. The wheel goes to the widget under the pointer and up
  to the first container that takes it (`OnWheel`). The focused examples add a settings screen whose slider, toggle
  and value selector are widgets built on them.
- [Scrolling content](../manual/ui.md#long-content): `ScrollView` shows content longer than its fixed height (or
  width), scrolling to the focused widget, with the wheel and with a drag, also one that starts on a button inside it.
  Any container can cut its children at its bounds (`Widget.ClipsChildren`) and take a drag from a child
  (`Widget.InterceptsDrag`), and widgets set nested clips with `UiContext.ClipTo`. The focused examples add a long list.
- [Grids](../manual/ui.md#grids): `Grid` places children in rows of equal cells (`Columns`, `Spacing`, `Align`).
  The focused examples add an inventory, and their list of examples is now built from widgets in a scrolling view.
- [Widgets anywhere](../manual/ui.md#placing-widgets-anywhere): `Layer` places each child against an edge or at a
  point of the UI view (a speech bubble over a character, a banner in a corner) and lets the pointer through where it
  has none. Every widget has an `Offset` and an `Opacity` that apply to everything in it, for slides and fades
  (`UiContext.Fade` for colours of a widget's own); `Widget.BlocksPointer` lets a container's empty areas through. The
  focused examples add bubbles and banners.
- [Text that wraps and reveals](../manual/ui.md#text-that-wraps-and-reveals): a `Label` with `Wrap` and a fixed
  `Width` breaks its text between words to fit, and `VisibleCharacters` reveals it a little at a time without moving
  its lines (`Lines` and `VisibleLines` give what it draws). The focused examples' dialogue is now built from a panel,
  labels and buttons, and its speech bubble wraps.
- [Your own look](../manual/ui.md#your-own-look): `NineSlice` is an image that stretches to any size with its corners
  kept, drawn with `DrawNineSlice`. `PanelStyle.Image` draws panels and buttons with one (`ImageTint` colours it), so
  a game gives the widgets its own frames; a focused button tints its image with the accent. The focused examples add
  a menu with a wooden and a parchment frame.
- [Menus](../manual/ui.md#menus): `UiRoot.FocusChanged` reports every move of the focus with the widget it left and
  the one it reached (`FocusChange`), for a cursor sound or a description that follows the cursor. The focused examples
  add a title menu built on the widget tree, with a cursor that follows the mouse.
- [Achievements of your own look](../manual/achievements.md#showing-achievements): `TrackAchievements` turns
  achievements on without the engine's banners, and `Game.DrawAboveScenes` draws what belongs to the game above every
  scene and transition, such as an unlock banner. The focused examples add an achievements list and unlock banners
  built from widgets.
- [Selection and the clipboard in text fields](../manual/ui.md#text-entry): a `TextField` selects with Shift and the
  arrows, Home or End, or with a drag of the pointer (`Select`, `SelectAll`, `SelectedText`), and Ctrl or Command with
  A, C, X and V selects all, copies, cuts and pastes; typing replaces the selection. `IClipboard` is the system
  clipboard's text, a platform capability on desktops and in browsers, and `Copy`, `Cut` and `Paste` let a game's own
  buttons use it. The focused examples add a name entry with copy and paste buttons.
- [Shaped text](../manual/text.md#shaped-text-arabic-hebrew-and-indic-scripts): the optional
  `MicroEngine.Text.Shaping` package draws Arabic, Hebrew, Indic and other scripts whose letters change shape with
  their neighbours or run right to left. `ShapedFont` shapes text with HarfBuzz on every platform, browsers included,
  with fallback faces for the letters a face lacks, and lays out lines that mix directions with Unicode's
  bidirectional algorithm (an English name or a number inside an Arabic sentence). `At(size)` is an `IFont` for
  widgets, wrapping and measuring, and `IsRightToLeft` says which lines to align to the right.
  `microengine package` adds HarfBuzz's licence to the packages of games that use it. The focused examples add
  English, Arabic, Hebrew and lines that mix them from one font.

**Deprecated**

- `DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `Toast`, `AchievementToasts`, `AchievementsScene`,
  `Game.UseAchievements` and `Game.AchievementToasts`: the engine ships primitives, and each of these is a few widgets
  of the game's own. They still work, with a warning that names the replacement, and are removed in a later release
  ([migration notes](../migrations.md#050-alpha2)). The tutorial, the focused examples and the demos no longer use
  them.

**Fixed**

- A `TextField` inside a container that clips no longer removes that container's clip after drawing.
- `microengine package web` and `package android` work with a .NET SDK released before its workload set (10.0.404
  with workload set 10.403): they took the .NET licence files from the runtime pack of the SDK's own runtime version,
  which the workload had not installed, and now take them from the pack it installed.

## 0.5.0-alpha.1

**This version is a breaking Alpha.** The engine is rebuilt as modules. Several public APIs change; the
[migration notes](../migrations.md#050-alpha1) open with an upgrade checklist.

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

- [Action prompts](../manual/input.md#showing-controls-to-the-player): `Game.Prompts` names the control that presses an
  action on the device being played (keyboard, Xbox, PlayStation or Nintendo pad, touch layout), follows remapping and
  device changes; `Format("Press {confirm}")`, `TextAll`, `Directions`; `PromptText` draws them as glyphs;
  `DialogueBox.Prompts` resolves them in dialogue; the `prompts` script command.
- [Touch control looks](../manual/input.md#look-styles-skins-and-images): `TouchStyle` (shape, colours, outline,
  press feedback, images) per kind or per control, `ITouchSkin` for a look of your own, `IOverlayCanvas.Image`.
- [Start screen](app-identity.md#start-screen): Android and iOS launch screens, the browser loading page and the
  engine's screen while the game loads, in the game's colour and image, with optional "Made with MicroEngine"
  (`MicroEngineSplash*` properties, `AppIdentity.Splash*`, `StartScreen`, `RunOptions.FirstFrame`).
- Android adaptive and round icons at every density (`MicroEngineIconBackground`); a web app manifest for browser
  packages.
- `GameSettings.Letterbox` (the colour around the frame), `AchievementToasts.Bottom`, `PixelImage.Resized`,
  `PixelImage.FitInside`, `○ □ △` in the built-in font.
- [Frame pacing](../manual/lifecycle.md#fixed-step-and-frame-order): with VSync, frame time is counted in refresh
  periods (`IPlatform.DisplayRate`, `RunOptions.PaceToDisplay`, `--pacing`), so irregular wake-ups no longer give
  frames with no update followed by frames with two; `Game.FrameBlend` for drawing between updates on 75, 100 or
  144 Hz displays; scene transitions and `ParticleSystem.Draw(renderer, blend)` draw between updates too.
- [Measuring a game](../manual/lifecycle.md#measuring-a-game): `--profile FILE` (`FrameProfiler`, `FrameSummary`,
  `IPlatformDiagnostics`), per-frame times, allocations and start phases as JSON or CSV.
- [Audio for tests](../manual/audio.md#devices-and-silence): `--audio auto|device|silent` (`AudioOutput`),
  `--audio-capture FILE` and `CaptureAudioDevice`, `SdlAudioDevice.TryCreate(driver)`.
- `AudioMixer.AddMusic(name, Song)` renders songs in the background, out of `Load` (in browsers, a few milliseconds per
  update); a song with an invalid note or drum fails there, in `Load`.
- [Choosing a platform](../manual/backends.md#choosing-a-platform): the launcher tries the next platform when the
  default one cannot start.
- [Graphics options](../manual/backends.md#graphics-options): `GameSettings.Graphics` (`GraphicsSettings`) chooses the
  platform, graphics APIs, VSync, a frame limit independent of VSync, the GPU (`GpuPreference`), the window mode
  (`WindowMode`) and the display; `Game.Screen` (`GameScreen`, `DisplayInfo`, `IDisplayControl`) lists the displays and
  changes VSync, the frame limit, the window mode and the display while the game runs; launcher options `--renderer`,
  `--vsync`, `--fps-limit`, `--gpu`, `--window`, `--display`.
- [Assets](../manual/assets.md): `Game.Assets` (`AssetCache`) loads, shares and releases textures, atlases and fonts
  from `Content/`, with scopes for levels (`CreateScope`), background loading for loading screens (`Preload`,
  `AssetBatch`), `AssetLoadException` naming the file, statistics and reports of slow loads during play (also in
  `--profile`); `Content/` ships with every game without a project item.
- [Images](../manual/assets.md#images): JPEG, BMP, TGA, GIF, PSD and every PNG variant (`PixelImage.Decode`).
- [Atlases from packing tools](../manual/assets.md#atlases-from-packing-tools): TexturePacker, Free Texture Packer and
  Aseprite JSON (trimming, pivots, frame durations and tags) and Starling XML (`AtlasData`, `SpriteAtlas.Load`,
  `SpriteAtlas.Clip`, `AnimationClip` with a duration per frame); `Assets.AtlasFromFolder` packs a folder of images.
- [Scalable fonts](../manual/text.md#scalable-fonts-ttf-and-otf): TTF and OTF fonts at any size (`ScalableFont`,
  `SizedFont`, `FontOptions`), with kerning, fallback faces and `Prepare`; `IFont` for both font kinds, taken by
  `DialogueBox` and `SelectionList`.
- [HD canvases](../manual/rendering.md#hd-canvases-and-smooth-scaling): `GameSettings.Presentation` (`Presentation.HighResolution2D`
  fills the window with linear sampling), per-texture `TextureFilter` with a game default (`GameSettings.TextureFilter`),
  `Texture.Update` for regions, windows sized to fit the display.

- [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule): `Game.Configure(EngineBuilder)`
  adds systems to the frame's phases (including `Update` and `LateUpdate` at the display's rate), replaces built-in
  services in a typed registry (`Game.Services`) and subscribes to engine events (`Game.Lifecycle`: suspension,
  resumption, quit requests with veto); `Game.Log`, `Game.TryGetCapability`, `ColorF` (linear colour with premultiplied
  helpers), `Handle<T>`/`HandlePool<T>`. The kernel ships inside the `MicroEngine` package (`MicroEngine.Kernel.dll`).
- [Time](../manual/lifecycle.md#time): the loop owns time; `TimeSettings` (`FixedRate`, `MaxStepsPerFrame`,
  `MaxFrameDelta`, `Mode`), `RunOptions.TimeMode` and `--time realtime|deterministic`.

**Changed**: `DialogueBox` continues a page that does not fit instead of cutting it; `BitmapFont.Wrap` breaks words
wider than a line; scripted (deterministic-time) runs open no sound device, and a hidden window alone implies neither
deterministic time nor silence; `IPlatform.Now` is only a monotonic clock on every platform and a platform's time-mode
preference is a stated `RunPreset` (deterministic for headless runs) (`HeadlessPlatform.UpdateRate` and `PlatformRequest.FixedStep` are gone); the platform contracts are in
`MicroEngine.Platform` (`IPlatformHost`, `ISurface`, `IStorage`, `IContentSource`), optional platform features are
capabilities found with `TryGetCapability<T>`, and SDL, raylib and headless platforms report resize and focus events; content file names must match their letter case on
every system; custom graphics devices implement filters and region updates; see the
[migration notes](../migrations.md#050-alpha1).

**Faster**: the SDL window appears about a second sooner on some Windows machines (gamepads start behind the start
screen); the SDL renderer skips unchanged state; input sampling, touch controls,
`SpriteAtlas.Frames` and `BitmapFont.Measure` allocate nothing per frame; SDL's audio streams music and plays sounds
without per-frame garbage; raylib streams rendered songs from their samples (browsers no longer freeze for a song);
hidden raylib windows no longer wait for the display; the frame loop allocates nothing per frame; the audio feeder
thread renders without garbage.

**Also changed**

- Browsers run on SDL 3 compiled to WebAssembly, not raylib. Packages are about 190 KB smaller. Desktops keep raylib
  as the alternative platform.
- `Entity` and `EntityList<T>` are removed in favour of `Scene.Objects`.
- The retro synthesizer (`Synth`, `Song`, ...) is in `MicroEngine.Audio.Retro`.
- `TileMotion.Move` returns `TileMoveResult`.
- Achievements are declared before the engine is composed.
- Touch controls are opt-in outside touch-first screens.
- A tap shorter than a frame is down in exactly one update for every device.
- See the [migration notes](../migrations.md#050-alpha1).

**Not in this version.** These are in the repository, not in the packages: the 3D path (glTF meshes, `Camera3D`,
`Renderer3D`) and the optional Box2D physics backend.

## 0.4.0-alpha.2

Published 2026-11-01 on nuget.org (GitHub release `sdk-v0.4.0-alpha.2`): fixes found while writing the developer manual. **Fixed**: scene changes requested during a
transition keep their order; `CoroutineRunner.StopAll` stops coroutines that had not run yet; a tap shorter than a
frame reports `Released`; `PlayMusic` with no fade starts at full volume; `Animator.Speed` refuses negative values;
`Synth.Render` gives no NaN samples for a sound of length 0; `Synth.Layer` refuses clips it would mix wrongly;
`ContentFolder.Exists` stays inside its folder and `PathOf` no longer accepts a sibling folder with the same prefix;
`SaveStore.Delete` lets an unreadable slot be saved again; launcher errors name the option with the wrong number;
`SelectionList` keeps its index valid; the template's first scene describes `CreateInput` correctly. Behaviour changes
are in the [migration notes](../migrations.md).

## 0.4.0-alpha.1

Published 2026-11-01 on nuget.org (GitHub release `sdk-v0.4.0-alpha.1`). The first public SDK: MicroEngine can be
installed, and a game created, run and distributed, with public tools.

```bash
dotnet tool install -g MicroEngine.Cli --prerelease
microengine new MyGame
```

**Packages** (all at 0.4.0-alpha.1, MIT, .NET 10):

| Package | What it is |
| --- | --- |
| `MicroEngine` | the backend-independent core, and the build targets for the game's identity |
| `MicroEngine.Sdl` | the SDL 3 platform (Windows, Linux, macOS, Android, iOS) |
| `MicroEngine.Raylib` | the raylib platform (desktop alternative, browsers), with raylib for WebAssembly |
| `MicroEngine.Desktop` | the desktop launcher with SDL 3, raylib, SDL's CPU renderer and headless |
| `MicroEngine.Cli` | `microengine`: new, run, build, package, info, doctor |
| `MicroEngine.Templates` | `dotnet new microengine-game` |

**New**

- `microengine package windows | linux | macos | android | ios | ios-simulator | web` ([Packaging](packaging.md)).
- [Application identity](app-identity.md) declared once in the project: name, id, version, description, icon,
  orientation; turned into each system's metadata and used at run time (`AppIdentity`, `PlatformRequest.Identity`).
- Signing with the developer's own keys from environment variables ([Signing](signing.md)).
- `microengine doctor` checks a machine for every target and explains how to install what is missing.

**Changed**: hosts and demo packaging; see the [migration notes](../migrations.md#040-alpha1).

**Known limitations**: Android App Bundles (`.aab`) are not built; iOS device builds and macOS notarization need the
developer's Apple credentials; Windows executables are not code-signed by the tool; desktop packages are x64 (Windows, Linux) and arm64 (macOS) only. See
[Requirements and limitations](../reference/limitations.md).

**Upgrading** from a game that referenced the engine's projects: replace the `ProjectReference` with
`<PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />`, add the identity properties, and
package with `microengine package` instead of copying build output.
