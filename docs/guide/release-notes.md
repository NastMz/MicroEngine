# SDK release notes

One section per SDK version, newest first. The SDK is in Alpha: a version may change public APIs incompatibly, and
every such change is in the [migration notes](../migrations.md). Releases are tagged `sdk-v<version>`; the demo
releases (`v0.1.0`, `v0.2.0`...) are separate.

## Next release

### Graphics: draw order, post-processing, the window stage and mip levels

- **Draw order that does not depend on call order.** A `SortedRenderer` keeps the draws made between `Begin` and
  `End` and sends them by layer, then depth, then call order, each with the camera, blending, clip and state it was
  recorded under; objects on the ground sort by the Y of their feet. It is a renderer, so everything that draws into
  one draws into it, on every platform ([Draw order](../manual/rendering.md#draw-order)).

### Content: sheets, animations, names, validation and packing

- A **sheet description** (a `.sheet` file next to its images) says how images are cut: cell size, margin, spacing,
  named strips, single sprites, pivots, and clips with a rate or a duration per frame. `Assets.Atlas("art/enemies.sheet")`
  loads it into an ordinary atlas; `Assets.Sheet(path)` gives the frames to pack with others
  (`SpriteAtlasBuilder.AddSheet`) ([Sheets and animations](../manual/assets.md#sheets-and-animations)).
- **Clips come from the data.** `atlas.Clip(name)` plays a clip with the timing, direction and repeat its file gives,
  and `atlas.CreateAnimator()` returns an `Animator` holding every clip of the atlas
  ([Animation](animation.md#sprite-animations-from-data)).
- A new optional package, **`MicroEngine.Aseprite`**, loads `.aseprite` and `.ase` files as the artist saved them:
  `Assets.Aseprite("hero.aseprite")`. Layers are flattened with their blend modes, frame tags become clips, slices
  become sprites and pivots ([Aseprite files](../manual/assets.md#aseprite-files)).
- Frames a packing tool stored **rotated** (TexturePacker and Free Texture Packer JSON) load upright instead of being
  refused.
- **`Assets.Preload` takes every kind**: sounds, music, sheets, materials, render targets and the kinds a game
  registers, by path or with the options of the direct calls (`Assets.Preload(load => load.Atlas(path, options))`).
  A loader can keep its decoding off the game's thread (`AssetLoader.Background`, `StagedAssetLoader`);
  `AssetBatch.Current` names the file being loaded ([Loading screens](../manual/assets.md#loading-screens-preload)).
- **`microengine content`**: `check` reports content that would fail at run time, `names` writes file, sprite and clip
  names as C# constants, `pack` packs a folder into an atlas offline; `names` and `pack` have `--check` for CI
  ([The tool](cli.md#content)).
- `SpriteAtlasBuilder.Pack` packs without a graphics device (optionally trimming transparent margins) and
  `PackedAtlas.ToJson` writes the atlas in the JSON the engine reads; `AtlasData.ToSheet` cuts a tool's atlas back
  into frames; `AssetStore.Read` loads an asset without storing it.

A ninth package: `MicroEngine.Tiled`, optional, for maps made in the Tiled editor. A tenth: `MicroEngine.Navigation`,
optional, for agents that find their way through a level. An eleventh: `MicroEngine.Ecs`, optional, for games
modelled as components and systems.

**Added**

- Effects on audio buses ([Buses and effects](../manual/audio.md#buses-and-effects)): `Audio.Bus(name)` is an
  `AudioBus` with a chain of effects (`Add`, `Insert`, `Remove`). Built in, in `MicroEngine.Audio.Effects`: `LowPass`,
  `HighPass`, `Compressor` (with `Sidechain`, and `Compressor.Duck(bus)` to turn music down under a voice line),
  `Reverb`, `Delay` and `Limiter`. Their properties change while they play, without clicks, and `Bypass` switches one
  out. A game writes its own by deriving from `AudioEffect` ([an audio effect](../manual/extending.md#an-audio-effect)).
  A mix that uses no effect sounds exactly as before.
- A limiter after the `master` bus, on in every game (`Audio.Limiter`): a mix louder than full scale is turned down
  instead of clipped; a mix within full scale is not touched.
- Fades ([Fades and crossfades](../manual/audio.md#fades-and-crossfades)): `Audio.Set(voice, ..., seconds:)` moves a
  voice's volume, pan and pitch over a time, `PlayParameters.FadeIn` raises a sound from silence,
  `AudioBus.FadeTo` and `Audio.SetBusVolume(name, volume, fadeSeconds)` fade a bus, and
  `Audio.PlayMusic(name, crossfade:)` overlaps the new track with the old one. A fade or ramp lasts at most
  `int.MaxValue` output frames (over twelve hours); a longer or endless time is that long.
- `Audio.SupportsVoices` tells whether `PlayVoice` returns voices on the current device (false headless, muted or
  without audio hardware). The Examples sample has "Effects on buses".
- [Physics](../manual/physics.md) for more kinds of game:
  - A scene has a physics world: `Scene.Physics` (`ScenePhysics`) is made on first use, stepped after the scene's
    `Update` on its world's clock, written to the owners' transforms, part of the state hash and disposed with the
    scene. `Game.PhysicsBackend` names the backend once: the tile backend by default.
  - Character controllers on either backend: `CharacterController2D` (gravity with a fall clamp, variable jump height,
    coyote time, jump buffering, drops through one-way platforms, air jumps, wall slide and wall jump, slopes on Box2D,
    carried by moving platforms, events such as `Landed` and `HitCeiling`) and `TopDownController2D`. They take intent,
    not input.
  - Dynamics on the physics contract: `ApplyForce`, `ApplyImpulse`, `ApplyTorque`, `ApplyAngularImpulse` (at the centre
    or at a point), `AngularVelocityOf`, `SetAngularVelocity`, `SetRotation`, `BodyDynamics` (damping, gravity scale,
    fixed rotation, sleep), `IsAwake`, `SetAwake`, `MassOf`, `CenterOfMassOf`, `InertiaOf`, `BoundsOf`.
  - Joints (`PhysicsCapabilities.Joints`, on Box2D): revolute, distance and rope, prismatic, weld, wheel and target
    joints, with limits, motors, springs, anchors, reaction forces and a break force.
  - `AddTiles` and `TilesOf` are members of `IPhysicsWorld2D`, so debug drawing shows tile geometry on any backend.
  - A capability error names the backend that declares what is missing.
  - Box2D worlds can be created and disposed from different threads at the same time (two worlds created at once
    could end the process).
  - The `PhysicsPlayground` sample: a chain, a rope and a cart on sprung, motor-driven wheels.
- Scene systems: a `SceneSystem` added to `Scene.Systems` is started, stepped before and after the scene's `Update`,
  added to the scene's state hash and stopped when the scene leaves, with no call from the scene
  ([Scene systems](../manual/scenes.md#scene-systems)). `Scenes.AddSystem` gives one to every scene, and
  `Scenes.Entered` and `Exited` report scenes going onto and off the stack.
- `Scene.Timers`: `After` and `Every` call an action after a time or at intervals on the scene's time, with
  `Cancel`, an unscaled option and their state in the state hash ([Timers](../manual/scenes.md#timers)).
- `StateMachine<TState>` and `StateMachine<TState, TEvent>` ([State machines](state-machines.md)): a state's entry,
  update and exit, ways out by condition (`When`), by time (`After`) and by event (`On`, `Fire`), substates, the time in
  the state, the state hash and snapshots for saves. Stepping and firing allocate nothing. `Scene.Machines` steps and
  hashes a scene's own machines.
- For modules: `Lifecycle.Started` and `Stopping`, and `EngineBuilder.StateSections`, which adds a named section of
  a module's state to the state hash.
- The mouse in actions ([The mouse in actions](../manual/input.md#the-mouse-in-actions)): mouse buttons (primary,
  secondary, middle, back, forward), the wheel (`InputControl.WheelUp`, `WheelDown`, `WheelLeft`, `WheelRight`: pressed
  for one update per notch) and pointer motion (`InputControl.PointerLeft` and its siblings: an analog value) are bound
  to actions like keys, with `InputMap.Bind(action, params InputControl[])`. They are read, consumed by widgets and
  on-screen controls, paired with players, rebound by listening (`ListenForPress(pressed, PressKinds.All)`,
  `InputControl.FromPress`) and saved like keys. `InputMap.Motion` reads four actions as an amount with no limit,
  `PointerSensitivity` scales pointer motion, `Unbind(action, control)` and `ActionsBoundTo(control)` serve a controls
  screen, and `PointerNames` names the mouse's controls. `InputDevice.Mouse` is what `LastDevice` reports for them. `Game.Prompts` lists an action's mouse controls
  after its keys (`PromptKind.Pointer`).
- `Input.Pointer` reports every button with its edges (`Pressed(button)`, `Released(button)`, `Down(button)`), the
  back and forward buttons, how far the pointer moved in the update (`Delta`) and what kind of pointer it is
  (`Kind`).
- `ICursorControl`, a platform capability: hide the system's cursor and lock it so the mouse reports motion only.
  SDL offers it on desktops and in browsers, raylib too; `HeadlessPlatform.Cursor` remembers what a game asked.
- `InputMap.BindStick` binds a direction of either stick read as a whole, and `RightStickDeadZone` is the right
  stick's radial dead zone, for twin-stick aiming and menus on the right stick.
- Input scripts press any mouse button (`mouse X,Y down secondary`), click (`click X,Y [button]`) and turn the wheel
  sideways (`wheel D H`); `InputScript.Mouse` takes a button and `Click` and `Wheel` build the same in code.
- The "Mouse actions" example: click to move, wheel to zoom, a locked drag and a mouse button rebound by pressing it.
- Versioned saves. A `SaveSchema<T>` declares the version a saved type is written in and one step per earlier version
  (`Migrate(from, step)`, a change to the save as JSON); `Saves.Load(slot, schema)` brings an older save up to date
  and reports what it found (`SaveLoadResult<T>`: loaded, migrated from which version, written by a newer release,
  corrupt, missing), and `Saves.Save(slot, data, schema)` writes the version (`"@version"`). A save from a newer
  release of the game is never loaded and never replaced, with or without a schema; the original of an older save is
  copied before it is replaced (`Backups`, `LoadBackup`, `DeleteBackup` reach the copies). Saves written before a
  type had a schema are version 1 and load unchanged. With the type's `JsonTypeInfo<T>` it works in games published
  trimmed or with NativeAOT ([Versions and migrations](../manual/persistence.md#versions-and-migrations)).
- `Game.Saves`: every game has its store, on the medium its host composed (`host.Storage`), else the platform's
  default, under `Game.SaveId` (the application id). `Game.CreateSaves()` builds it by hand, and
  `TrackAchievements(catalog)` keeps the achievements in it ([The game's store](../manual/persistence.md#the-games-store)).
- `SaveStore.Slots(prefix)` lists the slots that hold a save, with their size and last write time, for a "load game"
  screen; `IStorage.List()` is the storage's side of it ([Listing slots](../manual/persistence.md#listing-slots)).
  `SaveStore.Storage` is the store's medium, so key bindings are saved where the saves are
  (`Input.SaveBindings(Saves.Storage)`).
- `MicroEngine.Tiled` loads `.tmx` and `.tmj` maps with their tilesets and templates through the game's assets
  (`Assets.TiledMap(path)`): tile layers with each cell's flips, tilesets as sprites (`TiledSprites`), objects and
  typed custom properties as data, a collision grid from a tile property the game names, and tile objects and parallax
  drawn as Tiled shows them ([Maps from Tiled](../manual/world.md#maps-from-tiled)). The `TiledLevel` sample plays a
  level saved by Tiled.
- `MicroEngine.Navigation` finds paths ([Navigation](navigation.md)): `PathFinder` gives the cheapest path over a
  `NavGrid` (cells with costs, read from a tile map with `NavGrid.FromTiles` or from a physics world with
  `NavGrid.FromPhysics`), a `NavGraph` (waypoints joined by hand, with one-way tagged links for jumps, drops and
  doors) or a game's own `INavGraph`. `NavGrid.FindPath` returns the world points to walk through, straightened for
  the agent's size; cells change while the game runs (`SetWalkable`, `SetBlocked`, `ReadTiles`) and `Version` and
  `IsOpen` tell an agent when its path closed; `DistanceField` serves many agents with one goal. The same question
  always gives the same path, and searching allocates nothing once warm. Managed code only. The `Pathfinding` sample
  plays a chaser, a unit sent by clicks, a swarm and a patrol with a door.
- `TileOrientation`, and a `TileRenderer.Draw` overload that draws each cell mirrored or turned, at an offset, tinted
  and with an extra margin for sprites larger than a cell.
- `Presentation.Prescale`: the whole factor the canvas is enlarged by, with nearest sampling, before `Upscale` samples
  it into the window. With a linear `Upscale` and `Presentation.AutoPrescale`, a pixel-art canvas shown at a scale
  that is not whole (a 480 × 270 game on a 1366 × 768 screen) keeps every pixel the same size with antialiased edges,
  instead of pixels of two widths or a blurred picture, and at a whole scale the picture is exactly the nearest one.
  1, the default, changes nothing. `Presentation.PrescaleFor` gives the factors of a frame and `CanvasPresenter`
  composes the window; SDL and raylib windows apply it, and captures of the window show it on every platform, the
  headless one included (`ScreenCapture.Compose` takes the presentation)
  ([Presentation](../manual/rendering.md#presentation-one-choice-per-field)).
- `SaveStore.Save(slot, data, typeInfo)` and `SaveStore.Load(slot, typeInfo)` save and load a game's own types with
  the metadata of its `JsonSerializerContext`, without reflection: the way to save in a game published trimmed or with
  NativeAOT. They write the same JSON as `Save(slot, data)` and `Load<T>(slot)`, and either pair reads the other's
  slots. The calls without metadata keep working and are reported by the build in a project that turns on trimming
  or NativeAOT analysis: `IL2026` with `PublishTrimmed`, `IsTrimmable` or `EnableTrimAnalyzer`, and also `IL3050` with
  `PublishAot`, `IsAotCompatible` or `EnableAotAnalyzer`
  ([Trimmed and NativeAOT publishing](platforms.md#trimmed-and-nativeaot-publishing)).
- Keyframe animation: `Track<T>` holds a value through keys in time, each with the easing curve of the way to it
  (`Track.Float`, `Track.Vector2`, `Track.Color`, `Track.Step<T>`, or any type with its own blend), and `Timeline`
  plays several tracks from one playhead with looping, speed, `Seek` and named events (`At`, `Reached`) that fire once
  per pass, in order, also across a long frame. `Seek` raises no events and finishes a timeline moved to its end;
  time only goes forwards, so `Update` refuses a negative or non-finite step ([Animation](animation.md)). The "Tracks
  and timelines" example shows them.
- `Game.Audio` plays sounds that keep playing and change while they play
  ([Sounds that keep playing](../manual/audio.md#sounds-that-keep-playing)): `PlayVoice(name, parameters)` returns a
  `VoiceHandle` for a looping or one-shot sound on any bus with a priority; `Set(voice, volume, pan, pitch)`,
  `Pause`, `Stop(voice, fadeSeconds)` and `IsPlaying` control it; `AddBus`, `SetBusVolume`, `BusVolume` and `HasBus`
  group sounds under one volume; `PlayMusicVoice` plays registered music beside the music track. The pitch of a
  playing sound changes with `AudioEngine.SetPitch`, and the changes made to one sound in one update (volume, pan,
  pitch, pause, resume) take effect at the same instant, which moves the first of them slightly later than before
  ([migration notes](../migrations.md#several-changes-to-one-sound-in-the-same-update-apply-at-the-same-instant)). The Examples sample has a looping sound that follows a moving source.
  ([Presentation](../manual/rendering.md#presentation-one-choice-per-field)).
- Render targets, blend states and shaders of your own on the normal drawing calls
  ([Render targets and shaders](render-targets-and-shaders.md)). `renderer.PushTarget(target)` draws into a
  `RenderTarget2D`, whose `Texture` is then drawn like any texture and can be a sprite's source;
  `renderer.PushState(state)` draws with a `DrawState`: a blend state (`DrawState.Opaque`, `Alpha`, `Additive`,
  `Multiply`, or one of your own), a sampler and a material. A `Material2D` is a fragment shader compiled ahead of time
  by `microengine shaders`; the engine supplies the vertex stage, parameters are set by the names they have in the
  shader (`material.Set("Time", t)`, `material.SetTexture("Glow", texture)`) and checked, and each draw keeps the
  parameters it was made with. Shaders run on the `sdl-gpu` device; on every other, browsers included, a material
  draws with the fallback it declares (`MaterialFallback`: plain, tinted, another blend state, or hidden) and
  `IsSupported` says which, so a game has one drawing path. The guide shows a light that stops at walls, masks, a
  water shader and a post-processing chain, each an example of the Examples sample; the first two use only render
  targets and blend states, so they show on every platform.
- Materials, render targets and compiled shaders are assets: `Assets.Material(path, fallback?)`,
  `Assets.RenderTarget(name, width, height, filter?)` and `Assets.Shader(path)` are shared and released with their
  scope like textures ([Assets](../manual/assets.md#gameassets)). A loader that reads no file no longer needs a
  content folder.
- A texture of the drawing API shows the device texture behind it (`Texture.Native`), reports the memory of its
  format and mip levels (`SizeInBytes`), and `DeviceGraphics.Wrap` makes a device texture or render target the game
  created (a single channel, a compressed format the device declares) one that `IRenderer` draws.
- `microengine shaders` records the name, place and size of every shader parameter in the `.shader` file (format
  version 2; version 1 files still load, without names) ([the tool's page](cli.md#shaders)).
- `SpatialIndex2D<T>` finds what is near a place ([Large worlds and nearby objects](large-worlds.md)). A game files
  its own items (entity ids, attachments, indices into its arrays) under a point or a rectangle and asks by rectangle
  (`Query`) or by circle (`QueryCircle`), without walking the world and without giving every item a physics body.
  Items move (`Move`) and leave (`Remove`) through a `SpatialHandle`; a query allocates nothing, and results
  come in an order that depends only on the calls made. A swarm of 20,000 whose members each count their neighbours
  every step fits in a 60 Hz frame on a desktop, where a walk over all of them for each one stops under 2,000, and a
  query with `Camera2D.VisibleBounds` gives what a world much larger than the view should update and draw. The
  "Nearby objects" example of the Examples sample moves, of 3,000 fireflies, the ones near the view.
- `MicroEngine.Ecs`, an Entity Component System that lives in a scene ([Entity Component Systems](entity-component-systems.md)).
  `scene.AddEcs(components)` gives a scene a component store (the Arch library's world, used directly) that is
  created and destroyed with it; `Scenes.AddEcs` gives every scene one. `EcsComponents` declares the game's
  component types and which are simulation state. `Systems` and `LateSystems` run `EcsSystem` classes or functions
  in the order they were added, before and after the scene's `Update`, each with an `Enabled` switch; `Commands`
  defers changes asked during a walk to the end of the system. `Link` joins a row to an entity of the scene's world:
  each is found from the other, destroying either destroys both, and `SyncTransform` keeps the entity where the row
  is. `Interpolated<T>()` reads a component between two steps for drawing. The store's rows, links and systems are
  in the state hash, and a row with a component that was not declared stops the game with a message that names it.
  The scene's world stays the default model; the guide says which fits what. The `EcsEffects` sample models status
  effects as components that come and go, and `EcsSwarm` keeps a swarm of 20,000 in a store beside a scene's
  entities.

**Changed**

- An action's bindings are one list of controls (`InputControl`). `ActionBindings.Controls` holds every control and
  the lists by kind are views of it; binding a control an action already has adds nothing. Saved bindings use a second
  format, one list of controls per action as text (`"Key:Space"`, `"Mouse:Secondary"`), and files of the first format
  load unchanged. `InputMap.ExportBindings` fills `ActionBindingData.Controls` and leaves the lists of the first
  format empty ([migration notes](../migrations.md#bindings-as-data-are-one-list-of-controls)).
- On-screen touch controls set to appear automatically hide while the mouse plays, as they do for the keyboard and
  the gamepad.
- A scripted `wheel` turns where the script's mouse was put last, not at the canvas's corner, so it reaches the widget
  or the game under the pointer.
- A save reads the slot before it writes it. `Save(slot, data)` and `Save(slot, data, typeInfo)` return `false` for
  a slot that holds a versioned save, and a slot a load could not read can be saved again once a later load of it
  succeeds ([migration notes](../migrations.md#saves-read-the-slot-before-writing-it)).
- A storage lists its slots: `IStorage` (and `ISaveStorage`) has a seventh member, `List()`. A slot name cannot end
  with `.backup`, where the store keeps the original of a migrated save
  ([migration notes](../migrations.md#a-storage-lists-its-slots)).
- A save slot is a plain name on every system. `SaveStore` and `FileSaveStorage` throw `ArgumentException` for a slot
  that is empty, holds a path separator or another character file names cannot hold, ends with a dot or a space, ends
  with `.corrupt` (the name unreadable data is set aside under), or is a Windows device name; names such as `"progress"` and `"profile.v2"` keep their file
  ([Slot names](../manual/persistence.md#slot-names), [migration](../migrations.md#slot-names-that-are-not-plain-names-are-refused)).
- `InputMap.SaveBindings` and `LoadBindings`, and every member of `BrowserStorage`, throw `ArgumentException` for a
  slot that is `null` or empty (for the bindings calls, also white space) instead of handing it to the storage. Other
  names go to the storage as before, and each storage decides which names it holds
  ([Who checks the name](../manual/persistence.md#who-checks-the-name)).
- Drawing a frame no longer allocates in proportion to what it draws. A graphics device records each frame into the
  memory of the command list it executed before, and the 2D batcher keeps its lists between frames: a frame of 20,000
  sprites in an SDL window allocates a few hundred bytes instead of about 8.5 MB, so a scene with many sprites, shapes
  or particles no longer runs a full garbage collection every few frames. For code that drives a graphics device
  directly: once `IGraphicsDevice.Submit` returns, the submitted `CommandList` holds nothing (`Commands` is empty and
  `Payload` throws `InvalidOperationException`); a backend reads a list while it executes it, as before.
- The tile physics world looks at the shapes that touch the area of a move, a ray, an overlap or a sensor, instead of
  every shape of the 64-unit cells the area reaches. Crowds cost less: a room with 1,000 to 4,000 kinematic boxes
  moving through each other takes about 60% less time per step, and overlaps and rays among thousands of bodies about
  half. Every result is the same as before, and a body that enters a new cell no longer allocates.
- Destroying entities costs what is destroyed, not a pass over the scene's world: a step in which a few entities are
  destroyed among 100,000 took the world about 0.9 ms and takes about 0.15 ms. Iteration order and the state hash
  are unchanged.

**Fixed**

- A secondary, middle or side mouse button pressed on a widget or another input layer kept the mouse with that layer
  until the primary button was next released, so the next left click anywhere reached nothing. Each mouse button is a
  press of its own: the layer gets that button's release and nothing else.
- raylib windows ignored the mouse's back and forward buttons.
- A scene's world of about 130,000 to 260,000 entities could take 5 to 7 ms to start every step, depending on where
  the runtime had placed two of the world's arrays in memory (seen on an AMD Zen 4 processor). It takes 0.2 to 0.4 ms.
- `AchievementTracker` with a store threw `InvalidOperationException` in a game published trimmed or with NativeAOT,
  when it loaded and when it saved. It now reads and writes its slot without reflection; the file is unchanged and
  existing achievement files load as before.
- A slot such as `"../other"` or an absolute path read, wrote and deleted a file outside the game's save folder, and
  an empty slot wrote a file named `.json`.
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
- `Presentation.Fit` places the frame on whole window pixels at every scale: 480 × 270 in 1366 × 768 is 1365 × 768,
  centred, where it was 1365.33 wide and painted with letterbox bars of unequal width. Each side is the nearest whole
  size, at least one pixel and never larger than the area; at a whole scale the frame is the one it was
  ([migration notes](../migrations.md#presentationfit-returns-whole-pixels)).
- A game id such as `"../other"` or an absolute path put the save folder outside the user's `MicroEngine` data
  folder. `SaveStore.DefaultDirectory` now throws `ArgumentException` for an id that starts with `/` or `\`, starts
  with a drive, or has a part made only of dots and spaces (`.`, `..`, `...`) ([migration](../migrations.md#game-ids-that-are-paths-out-of-the-data-folder-are-refused)).
- A slot named `<another slot>.corrupt` was replaced, and its data lost, when that other slot held unreadable data
  and was set aside.
- `BrowserStorage` used with a `null` or empty slot read and wrote the key `microengine/<gameId>/`, the prefix of the
  game's slots.
- An output that stopped and plays again no longer plays what it missed: the shots and hits of the silent stretch
  were heard together within a second (after 30 seconds, some sixty sounds at once), and sounds played afterwards
  stayed up to half a second late. The sounds now go on with the game while the output is stopped, so it comes back
  to what the game is at: ended sounds are not heard, long sounds, loops and music from clips are where the game is,
  streamed music goes on from where it stopped. `AudioEngine.SkippedFrames` counts the frames that were not played
  ([Muting, pausing and an output that stops](../manual/audio.md#muting-pausing-and-an-output-that-stops),
  [migration notes](../migrations.md#an-output-that-stops-no-longer-plays-what-it-missed)).
- Changes to a playing sound or to a bus volume made in consecutive updates each take effect at their own update's
  point in the sound. Just after a slow frame they took effect up to 67 ms early for about a second, and some were
  never heard; with a `DelayFrames` longer than an update they were always early.
- `TileRenderer` skipped the tiles a zoomed-out or rotated camera shows beyond its unzoomed view: it now culls with
  `Camera2D.VisibleBounds`.
- `microengine package windows`, `linux` and `macos` stopped with `NETSDK1004` ("assets file not found") for a game
  whose `Directory.Build.props` moves the restore output when the package's publish properties are set: the tool
  looked for the .NET runtime pack the build used without those properties. It now reads it with the same properties
  as the publish.
- Copy and paste failed now and then on Windows while another program had the clipboard open (clipboard history,
  download managers and remote desktops read it after every change): `IClipboard.GetText` gave null with raylib and
  an empty text with SDL, and `SetText` with raylib reported as taken a text that never reached the clipboard. Both
  hosts now wait for the clipboard, up to 100 ms, and `SetText` returns false when the text was not taken.
- With raylib on Windows, a clipboard read or write that found the clipboard open by another program made the next
  window opened in the same process end it (`PLATFORM: Failed to initialize graphics device`). The raylib host now
  uses the Windows clipboard directly, which leaves nothing behind for the next window.

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
