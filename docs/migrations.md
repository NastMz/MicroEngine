# Migration notes

What changes between SDK versions and how to update a game. MicroEngine is in Alpha ([Versions](guide/sdk.md)):
a version may change public APIs incompatibly, and when it does, the change is listed here with the version that makes
it. Newest first.

## Next release

### Saved bindings: later formats are refused, and a screen saves its own actions

`InputBindingSet.FromJson` throws `FormatException` for a set whose `version` is above
`InputBindingSet.CurrentVersion` (or below 1); `LoadBindings` returns false for such a file and sets it aside, and
`ImportBindings` returns 0 for such a set. Before, it was applied as "every action, no control". `ImportBindings`
no longer replaces an action whose saved controls are all unknown to this version: the action keeps its controls and
is not counted in the result. An action saved with an empty list is still unbound.

The four methods have an optional `actions` parameter. A rebinding screen should pass the actions it offers,
`Input.SaveBindings(storage, actions: Rebindable)` and `Input.LoadBindings(storage, actions: Rebindable)`: a file
written without it holds every action of the map, and loading it back replaces what the game binds in code,
including bindings a newer version of the game changed. A file saved before loads with `actions` as well; the
actions it names beyond those are ignored.

### A scripted run, a headless run and a stepped game no longer use the player's saves

`Game.Saves` and `host.SaveStore(gameId)` are now on a store in memory, empty at start, in a run nobody plays: one
that has a script, advances in deterministic time, has no window or ends by itself (`--script`,
`--time deterministic`, `--headless` in any time mode, `--frames N`, `RunOptions.MaxUpdates`, `GameRunner` or
`InitializeForTest` on `HeadlessPlatform`), unless the run names a medium or the composition sets `host.Storage`
([Scripted runs and tests](manual/persistence.md#scripted-runs-and-tests)).

- A script that relied on saves left by an earlier run names them: `--saves DIR`. So does a check that starts a
  game with `--frames N` and then looks at the save folder.
- The crash report of such a run is no longer in the game's folder under the user's data: it is in the captures'
  folder (with a script) or the working directory, and its path is printed on standard error as before.
  `HostEnvironment.CrashDirectory` still decides when it is set.
- A test that started a game twice and expected the second to find the first one's saves in the default folder
  hands both the same medium: `game.InitializeForTest(platform, saves: storage)` with one `MemorySaveStorage`, or
  `RunOptions.Saves` for a run.
- A game with a `saveDirectory` parameter and an override of `CreateSaves` for its tests can delete both. Until it
  does, they keep working: the override's fallback to `base.CreateSaves()` follows the new rule.
- A game that uses `Saves` in its constructor, with no store of its own, now throws `InvalidOperationException` in
  such a run instead of using the player's saves. Use `Saves` from `Configure`, `Load` or later.
- `Game.InitializeForTest` has a third, optional parameter (`saves`); a call with two arguments compiles as before.
- The SDK now has a type named `MemorySaveStorage` in `MicroEngine.Core`, and the examples no longer write one by
  hand: their storage of their own is `SingleFileSaveStorage`, every slot in one file
  ([A save storage](manual/extending.md#a-save-storage)). A game that copied the examples' `MemorySaveStorage`
  deletes the copy and uses the SDK's; until it does, a file that imports both namespaces gets an ambiguous
  reference. The SDK's keeps what `SetAside` took out of a slot apart (`ReadSetAside(slot)`), not in a slot named
  `<slot>.corrupt`, and takes plain slot names only.

### `TextureFilter` has a third value, and two devices declare mip levels

`TextureFilter.LinearMipmap` joins `Nearest` and `Linear`. Code that decides between two samplers with
`filter == TextureFilter.Linear` treats the new value as nearest: compare with `TextureFilter.Nearest` instead, as the
engine now does, and add the case to any `switch` over the enum.

For authors of a graphics device: `CommandKind` gained `GenerateMipmaps` (`CommandList.GenerateMipmaps`). A device that
declares `GraphicsFeatures.Mipmaps` executes it; one that does not never receives it, because a texture with more
than one level cannot be created there. `sdl-gpu` and raylib on desktop now declare the feature, so
`GraphicsCapabilities.Features` differs from before on them.

### The examples' post-processing shaders are gone; a chain replaces hand-written passes

Nothing in the SDK was removed, but a game that copied `PostProcessExample`'s `Pass` helper and its `glow` and
`combine` shaders can replace them with `Scene.PostProcess` and the ready effects (`Bloom`, `Vignette`, `Scanlines`):
see [Post-processing](guide/render-targets-and-shaders.md#post-processing). The hand-written passes keep working.

### `CanvasPresenter` and `DeviceCanvas` for platform authors

`DeviceCanvas.Compose` has an overload that takes the screen's post-processing chain, and
`CanvasPresenter.Present` one that draws into a target instead of the window. A platform built on `DeviceCanvas`
offers the window stage by implementing `IScreenEffects` and passing its chain to `Compose`; one that does not keeps
working, and `Game.ScreenPostProcess` then runs on the canvas. A frame presented through the chain is captured with
`DeviceCanvas.CapturePresented`.
### Navigation: the radius of a route takes part in the search

`NavGrid.FindPath(finder, from, to, waypoints, radius)` used to search as if the agent were one cell wide and use
`radius` only to straighten the route. For an agent wider than a cell (`radius` above half a cell) the search now
avoids passages the body does not fit through, so a route that squeezed through a one-cell gap goes around or ends at
the nearest place the body reaches (`PathStatus.Unreachable`). For every radius above zero, a route to a point so
close to a wall that the body would touch it now ends beside that point, where the body is clear. A route that starts
on costly ground (`SetCost`) and whose cheapest path leaves it now leaves it by that path: the cost of the cell the
agent stands in no longer lets a straightened leg cross ground that costly (`FindPath`, `Smooth`). Routes of
agents no wider than a cell through open ground are unchanged.

`NavGrid.IsOpen(path, from)` gained the optional parameter `radius`, and `DistanceField`'s constructor the optional
parameter `radius`: source compiles unchanged; code compiled against the earlier package must be rebuilt.

### An atlas clip repeats as its data says

`SpriteAtlas.Clip(string name, bool loop = true)` is now `Clip(string name, bool? loop = null)`: without the argument
the clip repeats as its file says. Only an Aseprite tag with a repeat count (and a `.sheet` clip with
`"loop": false`) differs from before: it now plays once. Pass `loop: true` to keep it repeating. Code compiled against
the earlier SDK must be recompiled.

`SpriteAtlas.Animations` lists the clips in the file's order (it was unordered).

### Rotated atlas frames load; `AtlasData.Parse` no longer refuses them

`AtlasData.Parse` used to throw for a JSON frame with `"rotated": true`. It now returns the frame with
`AtlasRegion.Rotated` set, and `Assets.Atlas` and `SpriteAtlas.Load` turn it upright. `SpriteAtlas.Create(texture, data)`
throws for such data, because a texture cannot be turned: load through `Assets.Atlas`, or build with
`new SpriteAtlasBuilder().AddSheet(data.ToSheet(image))`. `AtlasRegion` and `AtlasAnimation` gained an optional last
parameter each (`Rotated`, `Loop`).

### `Preload` accepts what it used to refuse

`Assets.Preload("music/theme.ogg")` and other files that are not images, atlases or fonts no longer throw
`ArgumentException`: they load. A file whose extension no loader reads still throws, naming the file. A preload by
path of a `.json` file still treats it as an atlas.
### Audio: buses are objects, and the mixer has a limiter

Source that calls `Audio.AddBus`, `Audio.SetBusVolume`, `Audio.Set` or `Audio.PlayMusic` compiles unchanged; code
compiled against an earlier package must be rebuilt, because these signatures changed:

| Member | Before | Now |
| --- | --- | --- |
| `AudioMixer.AddBus`, `AudioEngine.AddBus` | returned nothing / the bus | both return the `AudioBus` |
| `AudioMixer.SetBusVolume(name, volume)` | two parameters | an optional `fadeSeconds` |
| `AudioMixer.Set(voice, volume, pan, pitch)`, `AudioEngine.Set`, `AudioEngine.SetPitch` | no time | an optional `seconds` |
| `AudioMixer.PlayMusic(name, fadeSeconds)` | two parameters | an optional `crossfade` |
| `AudioEngineSettings` | | a new `Limiter` property |

Behaviour that changed: a game's mix that goes over full scale is now turned down by the limiter after `master`
instead of being clipped by the sound card. A mix within full scale is unchanged, sample for sample. To get the old
behaviour:

```csharp
Audio.Limiter.Bypass = true;
```

`Compressor`, `Delay`, `Limiter` and `Reverb` are now type names in `MicroEngine.Audio.Effects`; a game with types of
those names that imports that namespace qualifies one of them.
### The physics contract is version 2

`IPhysicsWorld2D` gained members (dynamics control, joints, tile geometry, `BoundsOf`). Every one has a default that
refuses with `PhysicsCapabilityException`, so a physics backend written outside the engine still compiles; it
implements the members of the capabilities it declares. Games are affected by three renames and moves:

| Before | Now |
| --- | --- |
| `TilePhysicsWorld2D.GridOf(shape)` | `TilesOf(shape)`, a member of `IPhysicsWorld2D` |
| `Box2DPhysicsWorld2D.MassOf`, `BoundsOf` | the same members, now on `IPhysicsWorld2D` |
| `ICollisionGrid` and `TileFlags` in `MicroEngine.Physics.Tiles.dll` | the same types and namespace (`MicroEngine.World`), in `MicroEngine.Physics.dll`; recompile |

`Box2DPhysicsWorld2D` now declares `PhysicsCapabilities.Joints`, `ShapeQueries`, `ContactDetail`, `FilterPredicates`,
`TileGeometry` and `TileSlopes`, and the Box2D package's native library is layer version 6: the managed assembly and
the native library come from the same package version. The library also exports Box2D's own functions.

Code that tested `(world.Capabilities & PhysicsCapabilities.TileGeometry) == 0` to build a Box2D level from boxes now
finds tile geometry there: call `AddTiles`, or keep the boxes.

`QueryFilter` has a `Where` predicate; a physics backend written outside the engine declares
`PhysicsCapabilities.FilterPredicates` when it honours it and refuses a filter that has one otherwise.
`TilePhysicsWorld2D.AddTiles(body, grid, columns, rows)` refuses a grid with `TileFlags.SlopeRight` or `SlopeLeft`
tiles (new flags, values 256 and 512). A `ContactEvent` has a `Point`. The state hash of a `CharacterController2D` has
three more values, so recorded hashes of scenes that track one change.

A scene that created a world with `new` and registered it with `PhysicsSystem.Add` keeps working. To let the scene
own it instead, read `Physics.World` (or `Systems.Add(new ScenePhysics(world))` for a world you make) and remove the
registration, the step and the disposal: do not do both, or the world steps twice.

A scene class that names the `MicroEngine.Physics` namespace as `Physics.Something` now finds the scene's `Physics`
property first: write `MicroEngine.Physics.Something` or add a `using`.

### A graphics device of the drawing API creates render targets

`MicroEngine.Graphics.IGraphicsDevice` (what `Game.Graphics` is) has a new member without a default,
`CreateRenderTarget(int width, int height, TextureFilter filter)`. Games are unaffected. A class that implements the
interface (a test double that wraps another device, a backend that is not built on the device contract) must add it;
a wrapper forwards it:

```csharp
public RenderTarget2D CreateRenderTarget(int width, int height, TextureFilter filter) => _inner.CreateRenderTarget(width, height, filter);
```

`IRenderer` has four new members with defaults, `PushState`, `PopState`, `PushTarget` and `PopTarget`. A renderer that
does not implement them (a test double that records calls) still compiles and throws `NotSupportedException` when a
game under test pushes a state or a target; add the four members to it then.

### Compiled shader files are format version 2

`microengine shaders` writes `.shader` files of format version 2, which add the names, places and sizes of each
shader's parameters. Files of version 1 still load. Two things change for them:

- A material made from a version 1 file cannot set parameters by name (`Set`, `SetTexture(name, ...)` throw
  `InvalidOperationException` saying so); `SetUniforms` by slot still works. Run `microengine shaders` again to get
  names. The compiled code is the same.
- `microengine shaders --check` reports version 1 files as out of date.

`Material2D.SetUniforms` now refuses data whose size is not the size of the shader's block when the file carries it
(`ArgumentOutOfRangeException`); before, a struct of the wrong size gave wrong values. `Material2D.Vertex`,
`Fragment` and `Device` are null on a material created where shaders cannot run (`IsSupported` is false).
### Bindings as data are one list of controls

`InputMap.ExportBindings` returns each action's controls in `ActionBindingData.Controls`, as text (`"Key:Space"`,
`"Pad:A"`, `"Mouse:Secondary"`, `"Stick:Left.Up"`, `"Axis:RightTrigger+"`, `"Chord:LeftAlt|RightAlt+Enter"`), and
`InputBindingSet.Version` is 2. `Keys`, `Buttons`, `Stick`, `Axes` and `Chords` are the first format: they are still
read by `ImportBindings`, `LoadBindings` and `InputBindingSet.FromJson`, so saved files and sets a game kept in its own
save load as before, but an exported set leaves them empty. Before, an exported set filled those five and had no
`Controls`.

To update, code that edits an exported set (a controls screen that swaps a key between two actions) reads and writes
`Controls`, or works on the map directly: `InputControl.FromPress(press)` is the control of a press,
`ActionsBoundTo(control)` finds who has it, `Unbind(action, control)` takes it away and `Bind(action, control)` gives
it. `GetBindings(action)` still returns `Keys`, `Buttons`, `Stick`, `Axes` and `Chords`.

### A map never holds the same control twice for one action

`Bind` skips a control the action already has. Before, `Bind("jump", Key.Space)` called twice listed Space twice in
`GetBindings("jump").Keys`. To update, code that counted on the duplicate (none of the engine's did) needs no change
other than its expectations.

### Listening for a press and the mouse's other buttons

`InputSystem.ListenForPress(pressed)` hears keys and gamepad buttons, as before; `ListenForPress(pressed,
PressKinds.All)` also takes the next mouse button and wheel turn. `InputMap.BindFrom` returns true for those presses
too and binds them; before, it returned false for anything but a key or a gamepad button. To update, a controls screen
that passed every event of a custom listener to `BindFrom` and relied on false for mouse events checks
`InputControl.FromPress(press)?.Device` first.

A press of the mouse's secondary, middle, back or forward button that an input layer takes is captured by itself:
the layer receives that button's release, and the pointer's moves and the primary button are routed as usual. Before,
such a press sent every mouse event to that layer until the primary button was released. To update, a layer that
relied on receiving the moves after a right click takes them in `Handle` itself.

### `InputDevice.Mouse` and new members of input types

`InputMap.LastDevice` reports `InputDevice.Mouse` when a mouse button, the wheel or pointer motion pressed an action
(a finger that presses what the primary button is bound to reports `Touch`). A `switch` over `InputDevice` with no
default case gets a new value; treat it as the keyboard for prompts. `PointerState` has new members (`Back`,
`Forward`, `Delta`, `Kind`, `PressedButtons`, `ReleasedButtons`), which take part in its equality: code that compares
a whole `PointerState` with one it built compares the members it means instead. `InputEvent.PointerMove` takes an
optional `delta` after `time`, and `IInputSource` has `RightStick`, with a default that reads the two axes.
### A storage lists its slots

`IStorage`, and `ISaveStorage` with it, has a new member: `IReadOnlyList<SlotInfo> List()`. A storage of your own no
longer compiles until it has one. Return every slot that holds data, without the copies `SetAside` made, each as
`new SlotInfo(name, size, lastWrite)`; pass `null` for the time when the storage keeps none. `FileSaveStorage` and
`BrowserStorage` already do. A page of your own that hosts a browser game adds a `storage.keys(prefix)` function to
the `microengine` JavaScript module, returning the `localStorage` keys that start with `prefix`; the tool's page has
it.

### Saves read the slot before writing it

`SaveStore.Save` reads what the slot holds before it replaces it, so a storage of your own sees one `Read` for each
`Write`. The calls without a schema, `Save(slot, data)` and `Save(slot, data, typeInfo)`, now return `false` and
leave the slot alone when it holds a save with a version (a `"@version"` property), when the game has used that slot
through a `SaveSchema<T>`, or when the medium cannot read it; a slot that never had a version is written as before,
with the same text. A slot that could not be read is protected until a later load of it succeeds, where before only
`Delete` lifted the protection.

### Slot names that end with `.backup` are refused

`SaveStore` throws `ArgumentException` for a slot whose name ends with `.backup`, in any letter case: that is where it
keeps the original of a save it migrated (`<slot>.v<version>.backup`). `Backups`, `LoadBackup` and `DeleteBackup`
take those names. Rename a slot of your own that ends that way; to keep its data, read it once through
`Saves.Storage` under the old name and save it under the new one.

### A game has a member named `Saves`

`Game.Saves` is the game's store. A game class that declares its own `Saves` member now hides it and the compiler
warns (`CS0108`), which fails a build that treats warnings as errors. Remove the member and use the inherited one. A
game that built its store with a folder of its own overrides `CreateSaves()` instead:

```csharp
protected override SaveStore CreateSaves() =>
    _saveDirectory is null ? base.CreateSaves() : new SaveStore(SaveId, _saveDirectory);
```

`Game.Saves` keeps its slots under `Game.SaveId`, the application id of the project. A released game that built its
store with another id (`new SaveStore("MyGame")`) overrides `SaveId` to return that id, or its players' saves stay in
the old folder, unread. `TrackAchievements(catalog, store)` still takes a store; without one it uses `Saves`.

### `Save` and `Load` with a literal `null` third argument

`store.Save(slot, data, null)` and `store.Load<T>(slot, null)` no longer compile: the argument could be the type's
metadata or its schema. Neither accepts `null`; pass one of them.

### Hand-written save versions

Nothing has to change: a game that keeps a version property in its saved class and upgrades old saves itself keeps
working. To move to a schema, see
[A save that already has a version of its own](manual/persistence.md#a-save-that-already-has-a-version-of-its-own).

### A submitted command list holds nothing

Once `IGraphicsDevice.Submit(commands)` returns, the `CommandList` is empty: `Commands` has no commands and `Payload`
throws `InvalidOperationException`. The device records its next list into the same memory, so drawing a frame no
longer allocates in proportion to what it draws. Before, a submitted list kept its commands and their data for as long
as it was referenced. Games that draw through `IRenderer2D`, the drawing API or a `CommandList` they record and submit
are unaffected, and a backend reads a list while it executes it, as before.

To update, code that inspects a list (a test, a capture tool) reads `Commands` and `Payload` before submitting it and
keeps a copy of what it needs.

### Saves without metadata warn in trimmed and NativeAOT projects

`SaveStore.Save(slot, data)` and `SaveStore.Load<T>(slot)` are marked as needing reflection. A project that sets
`PublishTrimmed`, `IsTrimmable` or `EnableTrimAnalyzer` gets warning `IL2026` at each of those calls when it builds,
and one that sets `PublishAot`, `IsAotCompatible` or `EnableAotAnalyzer` also gets `IL3050`. With
`TreatWarningsAsErrors` the build fails there, and a `NoWarn` for `IL2104` does not cover it. A project with none of
those settings builds as before.

To update, declare the save types in a `JsonSerializerContext` and pass its metadata:
`Save(slot, data, typeInfo)` and `Load(slot, typeInfo)` read and write the same files
([Trimmed and NativeAOT publishing](guide/platforms.md#trimmed-and-nativeaot-publishing)).

### Game ids that are paths out of the data folder are refused

`SaveStore.DefaultDirectory(gameId)`, and with it `SaveStore.DefaultStorage(gameId)` and `new SaveStore(gameId)`
outside browsers, throw `ArgumentException` for an id that starts with `/` or `\`, starts with a drive (`C:`), or has
a part made only of dots and spaces (`.`, `..`, `...`). Before, such an id placed the save folder outside the user's `MicroEngine` data folder. Other
ids keep their folder, an id with a folder of its own (`"Studio/Game"`) included, and browser keys are unchanged.

To update, pass the game's id (`com.example.mygame`). To keep saves in a folder of your choice, pass it as the
directory: `new SaveStore(gameId, directory)`.

### Slot names that are not plain names are refused

`SaveStore.Save`, `Load`, `Exists` and `Delete`, and the members of `FileSaveStorage`, throw `ArgumentException` for a
slot that is `null`, empty or white space, contains `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|` or a control
character below U+0020, contains half of a surrogate pair, ends with a dot or a space, ends with `.corrupt` in any
letter case, or is a Windows device name such as `CON` or `NUL`
([Slot names](manual/persistence.md#slot-names)). Before, a slot such as `"../other"` or an absolute path read and
wrote a file outside the save folder, an empty slot wrote a file named `.json`, and a slot with a folder
(`"saves/1"`) was written by browser storage and refused by file storage unless the folder already existed. Names
without those characters, dots inside the name included (`"profile.v2"`), keep the file they had.

To update, pass a plain name. A game that used a slot with a folder renames it (`"saves.1"`); to keep reading the old
data in a browser, read it once from the old key with `BrowserStorage` directly and save it under the new name. A game
that grouped files in folders gives each group its own `SaveStore` over that directory.

A file that an earlier version wrote under a name that is now refused (`"name."` wrote `name..json`, `"CON"` wrote
`CON.json`, `"save.corrupt"` wrote `save.corrupt.json`) stays in the save folder, and `Load`, `Exists` and `Delete` no longer reach it. A game that used such a
slot renames the file once at startup, before it opens its store, with `File.Move` from the old file name to the new
slot's (`name.json`).

`InputMap.SaveBindings` and `LoadBindings` throw `ArgumentException` for a slot that is `null`, empty or white space,
and the members of `BrowserStorage` for one that is `null` or empty. They pass every other name to the storage, so
with `FileSaveStorage` a bindings slot that is not a plain name throws `ArgumentException` as above, where it threw
`IOException` or wrote outside the folder before. To update, pass a plain name, or leave the default
(`"input-bindings"`).

### Several changes to one sound in the same update apply at the same instant

Two or more changes to one playing sound in a single update (volume, pan, pitch, pause or resume) take effect together
at that update's point in the audio, where the first of them used to take effect up to the output delay earlier. A
single change per update, `Set(volume, pan)` in one call, stopping, fades and bus volumes sound as before.

What can sound different: a sound paused and resumed, or paused and changed, in the same update continues from a
position up to the output delay later than before; and `PauseMusic` or `MusicVolume` during a music fade applies at
the fade's current point instead of slightly ahead of it. No code needs to change.

### Sounds are heard sooner

On the SDL platform a sound reaches the device about 65 ms after the update that played it, where it took about
120 ms; the same rule applies in browsers, whose output queue is 100 ms. Sounds played in consecutive updates are
still exactly one update apart. Recordings made with `CaptureAudioDevice` or `--audio-capture`, and every
deterministic run, are unchanged.

What can need a change: a game that delayed its pictures or shifted its timing windows to match the sound (a rhythm
game's audio offset) was compensating for the longer delay; reduce that offset by about 55 ms on the SDL platform.

### `Presentation.Fit` returns whole pixels

The frame `Presentation.Fit` returns has a whole width and height at every window size, where a scale that is not
whole gave fractions (1365.33 × 768 for a 480 × 270 canvas in 1366 × 768). It differs from the earlier frame by less
than a pixel on each side, and at a whole scale it is the same frame. Pictures, pointer positions and the frame the
virtual controls use follow it.

What can need a change: code or a test that compared the frame with a fractional size compares it with the whole
one, and a game that rounded the frame itself can use it as returned.

### An output that stops no longer plays what it missed

When the output stops taking audio while the game goes on (a page whose sound the browser holds suspended, a device
that went away) and then plays again, it plays what the game is at. Sounds that ended meanwhile are not heard; a
sound still within its length, a loop and music from a clip go on from where the game is; streamed music goes on
from where it stopped. The sounds played meanwhile used to be kept and heard together within a second of the output
playing again, and sounds played after it stayed late by up to the length of the stop.

What can need a change: nothing in most games. A game that relied on a sound played into a silent output being heard
later (a jingle played before the first click on a page) plays it when it knows the output is back, or loops it;
`AudioEngine.SkippedFrames` grows while the output is stopped. Pausing, stopping and volumes behave as before.

## 0.6.0-alpha.1

### Removed UI components

`DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `Toast`, `AchievementToasts`, `AchievementsScene`,
`Game.UseAchievements` and `Game.AchievementToasts`, deprecated in 0.5.0-alpha.2, are removed: a game that still uses
them no longer builds. **Migration:** follow [Deprecated UI components](#deprecated-ui-components) below, which gives
the widgets that replace each one; a game that keeps one unchanged copies its source from the repository at tag
`sdk-v0.5.0-alpha.2` into the game.

### `MicroEngineGameFactory` removed

The `MicroEngineGameFactory` project property, deprecated in 0.5.0-alpha.1, is no longer read: `microengine package web`,
`android` and `ios` stop on a game without a `Configure(HostBuilder)` entry point and show the method to add.
**Migration:** add the entry point to the game class, pass it to the launcher in `Program.cs`, and delete the property
from the project file:

```csharp
public static void Configure(HostBuilder host) => host.UseGame(() => new MainGame());
```

```csharp
return DesktopLauncher.Run(MainGame.Configure, args);
```

## 0.5.0-alpha.2

### Deprecated UI components

`DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `Toast`, `AchievementToasts`, `AchievementsScene`,
`Game.UseAchievements` and `Game.AchievementToasts` are deprecated: a game that uses them builds with warning CS0618,
which names the replacement, and keeps working. They are removed in a later release. **Migration:**

- **`DialogueBox` and `Typewriter`.** Build the box from widgets: a `Panel` holding a `Label` with `Wrap` and a fixed
  `Width`, its `VisibleCharacters` raised a little every update, and a blinking arrow faded with `Opacity`
  ([Dialogue](examples/focused.md#dialogue)). `Label.VisibleLines` tells how many lines are showing; split long text
  into pages with `font.Wrap`. Dialogue that names controls formats each page with `Game.Prompts.Format`.
- **`SelectionList`.** Put `Button`s, or widgets of your own, in a `Stack` as a scene's `Widgets.Content`; a disabled
  widget (`Enabled = false`) is skipped by the cursor, and `Widgets.FocusChanged` gives the cursor sound
  ([Menus](manual/ui.md#menus)). A menu that must keep its own look draws its items itself in a widget of its own
  (`DrawContent` reads `IsFocused`), as the Title menu example does.
- **`UseAchievements` and `AchievementToasts`.** Call `TrackAchievements(catalog, store)` instead: progress, saving and
  the `engine.achievements` system are the same. Show unlocks from `Achievements.Unlocked`: a banner in a scene's
  widgets, or one that stays up while scenes change, drawn from `Game.DrawAboveScenes` and advanced by a frame system
  added in `Configure` ([Showing achievements](manual/achievements.md#showing-achievements)).
- **`AchievementsScene`.** Build the list as rows in a `ScrollView` over `Achievements.All`, in an overlay scene of your
  own ([Showing achievements](manual/achievements.md#showing-achievements)).
- **`Toasts`.** Slide a `Panel` in a `Layer` with `Offset` and `Opacity` ([Placing widgets
  anywhere](manual/ui.md#placing-widgets-anywhere)), or draw banners from `Game.DrawAboveScenes`.

To keep one of them unchanged for now, copy its source into your game: the four demos keep copies of `SelectionList`,
the unlock banners and the achievements screen this way.

## 0.5.0-alpha.1

This version is a breaking Alpha: the engine was rebuilt as separate modules, and several public APIs change. The checklist below gives each breaking change and what to do about it. The entries after it give
the details, newest first.

### Upgrading a game

1. **Assemblies and namespaces.** The `MicroEngine` package now carries the engine as several assemblies beside
   `MicroEngine.dll`: `Kernel`, `Platform`, `Hosting`, `Graphics`, `Graphics2D`, `Text`, `Content`, `Input`,
   `Input.Touch`, `Audio`, `Scene`, `Physics`, `Physics.Tiles`, `UI`, `Saves` and `Achievements` (each named
   `MicroEngine.<name>.dll`). `MicroEngine.Sdl` also carries `MicroEngine.Graphics.SdlGpu.dll`. A type that moved
   without a namespace change is forwarded by `MicroEngine.dll`, so source and compiled code find it as before.
   **Migration:** keep the same package references. One namespace changes: `Synth`, `SfxParams`, `Wave`, `Song` and
   `Instrument` are in `MicroEngine.Audio.Retro`, so add `using MicroEngine.Audio.Retro;` where you make sounds or music
   in code. Several namespaces are new: `MicroEngine.Hosting`, `MicroEngine.Scenes`, `MicroEngine.Physics`,
   `MicroEngine.Graphics.Devices`, `MicroEngine.Saves` and `MicroEngine.Achievements`. A file that imports both
   `MicroEngine.Graphics` and `MicroEngine.Graphics.Devices` names the `Texture` or `IGraphicsDevice` it means.
2. **Hosting entry point.** A game is composed once, by `public static void Configure(HostBuilder host)`, and every
   host (desktop, browser, Android, iOS, tests) runs that composition. **Migration:** add the method
   (`host.UseGame(() => new MainGame())`), move what `Program.cs` composed around the game into it, and start with
   `DesktopLauncher.Run(MainGame.Configure, args)`. `DesktopLauncher.Run(game, args)` keeps working on desktop, but
   `microengine package` needs the method for web and phone builds: without it, and without the deprecated
   `<MicroEngineGameFactory>`, packaging fails and shows the method to add. Read developer variables with
   `host.Environment.Variable(name)`, not `Environment.GetEnvironmentVariable`: phones pass them only there. Hosts of
   your own replace the removed statics (`ContentFolder.DefaultRoot`, `SaveStore.DefaultStorage`) with `host.Content`
   and `host.Storage`.
3. **Scenes and the world.** `Entity` and `EntityList<T>` are removed, and there is no compatibility package.
   **Migration:** keep a scene's objects in `Scene.Objects` (an `IWorld`). Create an entity, attach your own class to
   it, walk your objects with `All<T>()` and remove them with `Destroy`. Or keep plain classes in a `List<T>` of your own.
   The world interpolates positions between updates, so delete your own previous-position code and call `Teleport`
   where an object jumps. Read `Objects` in `Enter` or `Update`, never first in `Draw`, which now throws. Use
   `Scene.EngineStep` for what keeps moving through a hitstop, and `Camera2D.DrawPosition` instead of moving a camera
   around drawing. A member of your own named `Objects` or `EngineStep` hides the engine's (warning CS0108): rename it.
4. **Physics.** `TileMotion.Move` returns `TileMoveResult` (namespace `MicroEngine.World`); the name `MoveResult` now
   belongs only to the physics worlds (`MicroEngine.Physics`). **Migration:** rename the type where you store or return
   it; `var` needs no change. `TileMotion` is now a swept test, so check any level that relied on passing through thin
   solids. The physics worlds are new and optional. A Box2D backend exists in the repository but is not part of this
   version's packages.
5. **Input.** Input is an event stream with device ids, players and layers, and single-player games read `Game.Input`
   as before. **Migration:** code that treated a present `Input.Pointer` as "a mouse is here" checks `Input.Pointers`
   for `PointerKind.Mouse`, because fingers on no control are pointers now. A system added before `engine.touch` adds
   itself before `engine.input.events`. A game that wants F11 or Alt+Enter for itself unbinds the engine's
   `fullscreen` action. Touch controls are an opt-in module: they show on touch-first screens, or once the game sets a
   layout. A game that wants them on a touch laptop or a desktop browser overrides `CreateTouchLayout`. A tap shorter
   than a frame is down in exactly one update, for keys, pad buttons and touch controls alike.
6. **Audio.** Every backend plays through one managed mixer, and sounds start one output buffer after the update that
   plays them (60 ms on SDL, 100 ms in browsers). **Migration:** a mixer you create yourself calls `Pump()` once per
   frame. Sound and music files can load through `Game.Assets` (`Assets.Sound`, `Assets.Music`, `Audio.AddMusic`).
   The `Audio.Retro` namespace change is in item 1.
7. **Content.** `Game.Assets` reads from the composition's content source, which can be a folder, a zip archive or
   a list. File names must match their letter case on every system. **Migration:** fix any file whose case differs
   from the code. Read `Assets.Source` instead of `Assets.Content` when the content may not be a folder. The
   `<None Include="Content/**" ... />` item is no longer needed.
8. **Presentation presets.** `GameSettings.Presentation` states how the canvas reaches the window. The presets are
   `Presentation.PixelArt` (the default) and `Presentation.HighResolution2D`. The UI view's scale now defaults to the
   screen's pixel density. **Migration:** a pixel-art game changes nothing. A game whose interface must keep one UI unit
   per window pixel sets `UiScale = 1`. A game that used a translucent `BlendMode.Multiply` uses an opaque colour.
   An antialiased `ScalableFont` that must stay linear in a pixel-art game passes `FontOptions(Filter:
   TextureFilter.Linear)`. Code written for the unreleased `GameSettings.Scaling` uses `Presentation`.
9. **Achievements and saves.** Achievements are an optional module, declared before the engine is composed.
   **Migration:** move `UseAchievements(...)` from `Load` to the game's constructor or `Configure(EngineBuilder)`.
   Calling it from `Load` now throws. `SaveStore` is unchanged.
10. **Browser platform.** Browser packages run on SDL 3, not raylib. Desktops keep raylib as the alternative
    platform. **Migration:** a game that references `MicroEngine.Desktop` changes nothing. A game that references only
    `MicroEngine.Raylib` and is packaged for the web adds `MicroEngine.Sdl`. Drop `--raylib-wasm` from
    `microengine package web`. A browser host of your own uses the `BrowserLauncher`, `BrowserPage` and
    `SdlPlatform.BrowserProfile` of `MicroEngine.Sdl`.
11. **Custom platforms and devices.** A platform of your own implements the new platform contract
    (`IPlatformHost`: `Clock`, `PumpEvents`, `MainSurface`, `CanBlock`, `TryGetCapability<T>`), and offers optional
    features as capabilities. **Migration:** follow the entries on `IPlatform` below. A custom graphics device or
    texture implements filters and region updates.
12. **Time.** The loop owns time. Scripted and headless runs use deterministic time and open no sound device.
    **Migration:** a check that must hear a device passes `--audio device`. Delete `UpdateRate` from headless
    platforms and `FixedStep` from platform requests.

### Changes in detail

Behaviour that changes:

- Every package that holds SDL's binaries carries SDL's licence file: desktop packages have `licenses/SDL-LICENSE.txt`
  and `licenses/raylib-LICENSE.txt`, Android packages `assets/licenses/SDL-LICENSE.txt` and iOS apps
  `licenses/SDL-LICENSE.txt`, as web packages already did; the `MicroEngine.Sdl` package carries
  `native/browser-wasm/SDL-LICENSE.txt` beside `SDL3.a`. **Migration:** none; a script that checks a package's file
  list expects the new files.
- A tap shorter than a frame on a touch control is down in exactly one fixed update, the first that reads it, as a
  key's or a gamepad button's is: when a slow frame catches up with several updates, the later ones see it released
  (it used to stay down in every update of its frame). A control still held stays down in every update. Nothing to
  change in a game that reads `Pressed`; code that samples `VirtualControls` itself (not through the game's loop) calls
  the new `VirtualControls.EndUpdate()` after each of its updates.
- Reading `Scene.Objects` for the first time while the game draws (from `Draw` or `DrawUi`) throws an
  `InvalidOperationException` instead of creating the world there: a world created by drawing was stepped and hashed
  only in runs that drew a frame. **Migration:** a scene whose drawing alone reads its world (its `Blend`, say) reads
  `Objects` once in `Enter`; a scene that creates entities in `Enter` or `Update` needs no change.
- New: `Scene.EngineStep`, the engine's step before the world's clock scales or freezes it, for what keeps moving
  through a hitstop or slow motion. **Migration:** code that computed `1f / Game.Settings.UpdateRate` or read
  `Game.TimeSettings.FixedDelta` in a scene's update can read `EngineStep`; a scene member named `EngineStep` now hides
  the inherited one (compiler warning CS0108): rename it or delete it.
- New: `Camera2D.DrawPosition` and `Camera2D.DrawnPosition`, so a camera is drawn at an interpolated position while
  the rules keep `Position`. **Migration:** code that moved a camera to its drawn position before drawing and moved it
  back afterwards sets `DrawPosition` instead, and reads `VisibleBounds` (not `View`) and `DrawnPosition` (not
  `Position`) where it draws.
- Phone hosts (Android, iOS) hand a test's `--env` pairs to the game as its host's developer variables
  (`HostEnvironment.Variables`) and no longer write them into the process environment. **Migration:** game code that
  read such a variable with `Environment.GetEnvironmentVariable` reads it through the `HostEnvironment` its
  `Configure` receives (`host.Environment.Variable(name)`), as on desktop and in pages.
- Browsers run the SDL platform: `microengine package web` builds the page on SDL 3 compiled to WebAssembly (shipped in
  the `MicroEngine.Sdl` package) instead of raylib; desktops keep raylib as an alternative platform. The page, its
  input, touch controls, sound, saves and embedding are unchanged. **Migration:** a game that references
  `MicroEngine.Desktop` needs nothing. A game that references only `MicroEngine.Raylib` and is packaged for the web adds
  `MicroEngine.Sdl` (or uses `MicroEngine.Desktop`); a game that references only `MicroEngine.Sdl` can now be packaged
  for the web. Scripts that passed `--raylib-wasm` to `microengine package web` drop it (`--sdl-wasm FILE` names another
  SDL library). Code that used the browser types of `MicroEngine.Raylib` (`BrowserLauncher`, `BrowserPage`,
  `RaylibPlatform.BrowserProfile`) uses those of `MicroEngine.Sdl` (`BrowserLauncher`, `BrowserPage`,
  `SdlPlatform.BrowserProfile`), which have the same members. The `MicroEngine.Raylib` package no longer carries
  `native/browser-wasm/raylib.a` (nor `MicroEngineWebRaylibFromPackage`); `MicroEngine.Sdl` carries
  `native/browser-wasm/SDL3.a`. A web package's `licenses/` folder now holds SDL's licence.
- `Synth`, `SfxParams`, `Wave`, `Song` and `Instrument` moved from the `MicroEngine.Audio` namespace to
  `MicroEngine.Audio.Retro` (same assembly, same members). **Migration:** add `using MicroEngine.Audio.Retro;` to each
  file that makes sounds or music in code; code that wrote `MicroEngine.Audio.Synth` in full writes
  `MicroEngine.Audio.Retro.Synth`. The rest of the audio API (`AudioClip`, `AudioMixer`, `IAudioDevice`...) stays in
  `MicroEngine.Audio`.
- `TileMotion.Move` returns a `TileMoveResult` (namespace `MicroEngine.World`) instead of a `MoveResult`, with the same
  members (`Bounds`, `Position`, `HitLeft`, `HitRight`, `HitWall`, `HitCeiling`, `HitFloor`, `Touched`); the name
  `MoveResult` belongs to the physics contract (`MicroEngine.Physics.MoveResult`) alone. **Migration:** rename the
  type where you store or return it (`MoveResult last` becomes `TileMoveResult last`); `var` needs no change.
- For games that followed the unreleased `GameSettings.Scaling` (it was never in a published SDK): `Scaling` and the
  `CanvasScaling` enum are removed, together with `GameSettings.EffectivePresentation`; `GameSettings.Presentation` is
  never null and defaults to `Presentation.PixelArt`. **Migration:** `Scaling = CanvasScaling.Smooth` becomes
  `Presentation = Presentation.HighResolution2D`, `Scaling = CanvasScaling.Pixel` is the default and can be deleted, and
  `EffectivePresentation` reads `Presentation`. `FrameLayout.Fit`, `ITouchOverlay.Frame` and `VirtualControls.Frame`
  take the game's `Presentation` (null is `Presentation.PixelArt`) instead of a `CanvasScaling`: the presets place the
  frame exactly where `Pixel` and `Smooth` did, and other presentations (`Rounding = Integer`, `Aspect = Stretch` or
  `Expand`) are now followed by `FrameLayout.Fit` and by `VirtualControls.Frame` while no shown control covers the
  frame, so touches map to where the canvas is drawn (a frame that moves clear of the controls keeps the configured
  canvas shape).
  `FrameLayout.Fit` throws `ArgumentOutOfRangeException` for a canvas size that is not positive.

- The SDL platform can draw with a second graphics device: `GraphicsSettings.Device = "sdl-gpu"` (or `--graphics
  sdl-gpu`) selects SDL_GPU (tier 1: custom shaders, depth, the 3D path) instead of SDL's renderer. Nothing changes for
  a game that does not ask for it. `SdlPlatform.Device` is now a `GraphicsDeviceBase` (it was `SdlRendererDevice`):
  cast it when you need the SDL_Renderer device (`(SdlRendererDevice)platform.Device`), which it is unless the game
  asked for `sdl-gpu`. Other platforms refuse a request naming a device they do not offer
  (`PlatformProfile.GraphicsDevices`).
- `DeviceRenderer` (the renderer the device-backed platforms give a scene) has `Target` and `Flush()`, to put device work
  such as a 3D pass between the 2D drawn before and after it. Code that does not call them is unchanged.
- Touch controls are an optional module: `Game.Touch` is active (drawn, read, taking the touches over it) on touch-first
  screens (phones, tablets, touch-only browsers) or once the game sets a layout of its own (overriding
  `CreateTouchLayout`, or setting `Game.Touch.Layout` in `Configure`, `Load` or a scene); otherwise it is inactive and
  costs nothing (`VirtualControls.IsActive`). `Game.Touch` always exists, now from `Configure` on. **Migration:** a game
  that relied on the default controls appearing on a touch laptop or a desktop browser sets a layout:
  `protected override TouchLayout CreateTouchLayout() => TouchLayout.CreateDefault();`. Tests that touch the headless
  screen of a game without a layout set `platform.TouchScreen.TouchFirst = true` first. The touch types (`ITouchScreen`,
  `TouchEvent`, `ITouchOverlay`, `IOverlayCanvas`, `FrameLayout`, `TouchLayout` and its controls, `VirtualControls`,
  `TouchStyle`, `ITouchSkin`, `DefaultTouchSkin`) moved to the new `MicroEngine.Input.Touch.dll` (carried by the
  `MicroEngine` package) with their namespace unchanged.

- Achievements are an optional engine module: a game declares them before the engine is composed, and a game that
  never declares them has no achievement service, no `engine.achievements` frame system and no achievement file.
  **Migration:** move the `UseAchievements(catalog, store, theme)` call out of `Load` into the game's constructor or
  into `public override void Configure(EngineBuilder builder)` (a store created in `Load` moves with it); code that
  needs the running game (`AchievementToasts.Top`, `Achievements.Unlocked += ...` playing a sound) stays in `Load`.
  Calling `UseAchievements` from `Load` or later now throws an `InvalidOperationException` that says so. A declared
  game's tracker and store are services (`Services.Get<AchievementTracker>()`, `Services.Get<SaveStore>()`);
  compositions without the `Game` façade add `AchievementsModule` (namespace `MicroEngine.Achievements`) and
  `SavesModule` (namespace `MicroEngine.Saves`) themselves.
- `SaveStore`, `ISaveStorage` and `FileSaveStorage` moved to the new `MicroEngine.Saves.dll` (carried by the
  `MicroEngine` package) with their namespace unchanged; nothing to change.
- The fonts (`IFont`, `FontExtensions`, `BitmapFont`, `ScalableFont`, `SizedFont`, `FontOptions`) moved to the new
  `MicroEngine.Text.dll` (carried by the `MicroEngine` package) with their `MicroEngine.Graphics` namespace unchanged,
  and `Game.Assets.Font` loads through it; nothing to change.
- `Achievement`, `AchievementStatus`, `AchievementTracker`, `AchievementSaveData` and `AchievementRecord` moved to the
  new `MicroEngine.Achievements.dll` (carried by the `MicroEngine` package) with their namespace unchanged; nothing to
  change.
- `Typewriter`, `SelectionList` and `UiTheme` moved to the new `MicroEngine.UI.dll` (carried by the `MicroEngine`
  package) with their `MicroEngine.UI` namespace unchanged; nothing to change. The new widget tree (`Scene.Widgets`,
  `UiRoot`, `Label`, `Button`, `Panel`, `Stack`, `TextField`, `Toasts`) is opt-in: a scene that never reads `Widgets`
  behaves as before. A game type called `Widgets` inside a scene now needs its namespace there.
- `Presentation.UiScale` is a `float?`: null (the default) makes the UI scale the surface's pixel density (2 on a
  high-density screen, 1 headless) instead of 1. **Migration:** a game whose interface must keep one UI unit per window
  pixel everywhere sets `UiScale = 1`; code that read the scale reads `Game.Views.UiScale` or
  `Presentation.UiScaleFor(density)`.
- `AchievementToasts` shows its banners through the UI's `Toasts` (`AchievementToasts.Banners`); a banner's heading is
  `Heading` as it was when the achievement was unlocked.
- A run's capture of a frame whose scenes drew an interface in the UI view (`DrawUi`, `Widgets`) is the window as the
  player saw it (world scaled into its frame, letterbox, UI over both) at the window's size; frames without one are
  captured as before. **Migration:** a test that needs the world alone reads it from the platform's `IViewCapture`
  capability (`CaptureWorld`, `SaveWorldScreenshot`). A playing transition is drawn over the UI view only in frames
  that drew an interface there; the canvas shows it over the world as before.

- A frame has a world view and a UI view: `Scene.DrawUi(IRenderer ui)` draws the interface at the window's resolution
  over the world (UI units: window pixels / the UI scale), `Scene.UiContains` says where it takes the pointer,
  `Scene.WorldCamera` is pushed around `Draw`, and `Game.Views` maps positions and the pointer between window pixels,
  canvas pixels and UI units. Nothing to change: a scene without `DrawUi` draws as before. Platforms of your own may
  offer the UI view as the `IScreenView` capability; without it `DrawUi` draws in the canvas.
- `Presentation.Aspect = AspectMode.Expand` grows the canvas with the window instead of letterboxing; the canvas's
  current size is `IRenderer.Width`/`Height` (and `Game.Views.CanvasSize`), while `Game.Width`/`Height` stay the
  configured size.
- Glyph pages of TTF/OTF fonts created without `FontOptions.Filter` follow the game's default filter (the presentation's
  `DefaultSampler`) instead of choosing from `Antialiasing`; `FontOptions.Pixel` now sets nearest itself. **Migration:** a
  pixel-art game whose antialiased `ScalableFont` must stay linear passes `new FontOptions(Filter: TextureFilter.Linear)`.

- Scenes can hold their objects in a world (`Scene.Objects`, an `IWorld` in the `MicroEngine.Scenes` namespace, from
  the new `MicroEngine.Scene.dll` the `MicroEngine` package carries), and a `Scene<TResult>` can return a value to the
  scene below (`Game.Scenes.Push(scene, onResult)`, `Finish(value)`). Nothing changes for a scene that does not read
  `Objects`: it has no world and its `Update` receives the engine's step as before. **Migration:** a scene of yours
  that declares a member named `Objects` now hides the inherited one (compiler warning CS0108): rename yours, or read
  the engine's as `base.Objects`. Once a scene reads `Objects`, its `Update` receives the step on the world's clock
  (equal to the engine's step unless the game scales or freezes that clock).
- `Entity` and `EntityList<T>` (namespace `MicroEngine.World`) are removed from the SDK. **Migration:** keep a
  scene's objects in `Scene.Objects` (an `IWorld`: create an entity, attach your own class to it, walk them with
  `All<T>()`, destroy them with `Destroy`), or in plain classes of your own held in a `List<T>`, as the platformer
  example now keeps its coins. What maps where: an `Entity` added to a list is `Objects.Create()` plus
  `Objects.Attach(id, yourObject)`; `Position` is `Objects.Local(id).Position2D`; `Alive` and `Destroy()` are
  `Objects.IsAlive(id)` and `Objects.Destroy(id)`; `Removed()` is an attachment implementing `IDestroyedCallback`,
  told when the world ends the step; `EntityList.Update` is a `foreach` over `Objects.All<T>()`; `OfType<T>()` and
  `Overlapping(area)` are filtered walks over it. A new entity is visible at once to every walk that starts after it
  is created (there is no deferred add to flush).
- `TileMotion.Move` is a swept test: a fast box stops against an extra solid rectangle thinner than a sub-step instead of
  passing through it, and an extra solid whose top edge is inside the lower half of a box moving down (a platform that
  rose into a standing player) lifts the box onto its top, so `HitFloor` and `IsOnGround` are true instead of the box
  ending inside it. Moves with nothing thin in their way end exactly where they did. Nothing to change; a game that
  relied on passing through thin rectangles makes them thicker or leaves them out of `solids`.
- The tile types (`TileLayer`, `TileGrid`, `TileFlags`, `ICollisionGrid`, `Autotile`) and `TileMotion` with its
  result (now `TileMoveResult`, see above) moved to the new `MicroEngine.Physics.Tiles.dll` (carried by the
  `MicroEngine` package) with their namespace, `MicroEngine.World`, unchanged, and `MicroEngine.dll` forwards them.
  `TileRenderer` stays in `MicroEngine.dll`.
- New: physics worlds (`MicroEngine.Physics`, `MicroEngine.Physics.Tiles`; [Physics worlds](manual/world.md#physics-worlds)).
  Their move result is `MicroEngine.Physics.MoveResult`; the tile mover's is `TileMoveResult`, so a file can import
  both namespaces.
- F11 and Alt+Enter no longer switch full screen inside the SDL and raylib backends: the engine's `fullscreen` action
  (`ActionNames.Fullscreen`, bound to F11 and Alt+Enter by `InputMap.CreateDefault` and added to a map that has no such
  action) does, through `Game.Screen.ToggleFullscreen()`. Players see no change. **Migration:** a game that wants F11
  for itself, or no toggle at all, calls `Unbind("fullscreen")` (and `Bind("fullscreen", ...)` to move it) in
  `CreateInput`. `InputMap.Unbind` of an action the map does not have now creates it unbound. The frame schedule's
  `PreFixed` phase runs `engine.fullscreen` after `engine.input`; the headless platform counts the requests
  (`HeadlessPlatform.FullscreenToggles`).
- Input is an event stream with device ids, players and layers (assembly `MicroEngine.Input`, carried by the
  `MicroEngine` package; the namespace stays `MicroEngine.Input`). Single-player games change nothing: `Game.Input` is
  player 1's map and player 1 owns every device. What may need a change:
  - Fingers that land on no on-screen control now reach gameplay as pointers: `Input.Pointer` is present while such a
    finger is down (it was the mouse only) and `Input.Pointers` lists them. **Migration:** code that treated a present
    `Input.Pointer` as "a mouse is here" checks `Input.Pointers` for `PointerKind.Mouse` instead.
  - A tap shorter than a frame (a key, a gamepad button or a touch button) is kept until an update reads it, however
    many frames come first; it used to expire after eight drawn frames. Nothing to change.
  - The frame schedule's `Input` phase runs `engine.input.events` (device events and touches through the input layers)
    before `engine.touch`. **Migration:** a system added `before: "engine.touch"` to feed touches adds itself
    `before: "engine.input.events"`; `engine.input` now samples every player (`Game.InputSystem.Sample()`).
  - `VirtualControls` is an input layer (`IInputLayer`) and its frame is split into `BeginFrame`, `HandleTouch` or
    `Handle`, and `EndFrame`; `Update(screen, dt, device)` still does all three for code that drives it by hand.
  - `InputMap.CreateDefault()` takes optional `ActionNames`; `InputMap.Names` says which names the engine's own parts
    read. `PointerState` has a `Wheel`; `IInputSource` has `Pointers` (with a default). A custom `IInputSource` needs no
    change.
  - The SDL and raylib platforms deliver their input as events (`IInputBackend`, a capability) with every gamepad as a
    device of its own; SDL also offers layout key labels, rumble and text input. Their polled `Input` still answers
    for code that reads it directly, but a tap shorter than a frame now reads as held only until the next event pump
    (the engine itself keeps it until an update reads it). Prompts name keys by the player's layout on SDL.
  - `DialogueBox.Update`, `SelectionList.Update` and `AchievementsScene` consume the press they act on: after them,
    `Input.Pressed(confirm)` is false for the rest of that update and until the key is let go. **Migration:** code that
    read the same press after the widget on purpose reads the widget's result instead.
  - Input scripts have device commands (`key`, `keytap`, `pad`, `mouse`, `wheel`, `text`) that send events from the
    script's own devices; the existing commands are unchanged.
  - A host's extra polled sources (`PlatformRequest.ExtraInput`) are devices of their own: on a backend that delivers
    events, player 1 reads them alongside it, and they can be paired with other players. A platform that offers
    `IInputBackend` as a capability is read through its events; its polled `Input` is then not read again.
- A game is composed once, by a `Configure` entry point, and every host runs that composition: the desktop
  `Program.cs`, and the browser, Android and iOS hosts `microengine package` builds. The entry point is a
  `public static void Configure(HostBuilder host)` method (namespace `MicroEngine.Hosting`) that chooses the game with
  `host.UseGame(() => new MainGame())` and may add platforms (`host.Platforms().Add(...)`), change the platform request
  (`host.ConfigureRequest(...)`) or the engine (`host.ConfigureEngine(...)`). `microengine` finds it in the built game,
  so web and phone builds no longer need a project property. **Migration:** add the method to your game class and
  pass it to the launcher: `return DesktopLauncher.Run(MainGame.Configure, args);`. Move what `Program.cs` composed
  around the game (a catalog, request changes) into it. `<MicroEngineGameFactory>` is deprecated and has no default
  any more: a game without an entry point that still declares it packages with a warning; one with neither fails with
  a message showing the method to add. `DesktopLauncher.Run(game, args)` and its other overloads keep working.
- `Game.Assets` reads content from any source the composition names (`host.Content`): a folder, a zip archive
  (`MicroEngine.Content.ZipContentSource`, `ContentSources.Open("game.zip")`) or files a host lists and opens itself
  (`ListedContentSource`); a source other than a folder used to be refused with `NotSupportedException`. The new
  `Assets.Source` is the source in use; `Assets.Content` (a `ContentFolder`) now throws `InvalidOperationException`
  when the content is not a folder. `AssetLoadException` moved to the new `MicroEngine.Content.dll` (inside the
  `MicroEngine` package) with its namespace, `MicroEngine.Core`, unchanged, and `MicroEngine.dll` forwards it: nothing
  to change in games. `Assets.Statistics.AudioBytes` (new) counts the sounds and music files loaded through `Assets`;
  `TextureBytes` still counts textures only.
- Every audio device is the engine's one mixer (`MixerAudioDevice` over an `IAudioSink`): the same voice limit (32),
  priorities and stealing, panning, resampling and fades on SDL, raylib, in browsers and in `CaptureAudioDevice`.
  SDL plays through a stream a thread keeps 60 ms ahead, browsers through an AudioWorklet 100 ms ahead, so sounds start
  that much after the update that played them; OGG music decodes in managed code on every backend (the `MicroEngine`
  package now depends on NVorbis), MP3 stays raylib-only. `CaptureAudioDevice` now applies the voice limit and decodes
  OGG music (it captured it as silence). The audio types moved to the new `MicroEngine.Audio.dll` (inside the
  `MicroEngine` package) with their namespace unchanged, and `MicroEngine.dll` forwards them: nothing to change in
  games. `AudioMixer.CurrentMusic` names a song from the moment it is asked for, also while it still renders in slices
  (browsers), where it used to be null until rendered; songs render in slices once per frame (`AudioMixer.Pump`, which
  the game calls) instead of during updates. A mixer you create yourself calls `Pump()` once per frame.
- Sound and music files load through `Game.Assets` from any content source: `Assets.Sound(path)` (an `AudioClip`)
  and `Assets.Music(path)` (an `EncodedAudio`), played with `Audio.AddSound(name, clip)` and the new
  `Audio.AddMusic(name, music)`. `AddSoundFile` and `AddMusicFile` keep reading file-system paths.
- Scripted runs in browsers advance one fixed step per frame with seed 1, exactly as on desktop and phones (they were
  real-time, so their captures changed from run to run), and ask for no sound device unless `--audio device` is
  given, like desktop scripted runs. Nothing to change in games.
- No engine default is process-wide any more. `ContentFolder.DefaultRoot` is read-only (the `Content` folder next to the
  program) and `SaveStore.DefaultStorage` is a method returning the platform's default medium: the page's localStorage
  in browsers (`MicroEngine.Platform.BrowserStorage`, the same keys as before), files elsewhere. **Migration:** a host
  that set `ContentFolder.DefaultRoot` sets `host.Content = new FolderContentSource(folder)` in its composition; one that
  set `SaveStore.DefaultStorage` sets `host.Storage = gameId => new MyStorage(gameId)` and the game creates its store
  with `host.SaveStore(gameId)` (or passes the storage to `new SaveStore(storage)`). `BrowserSaveStorage` is replaced by
  `MicroEngine.Platform.BrowserStorage`. Saves stay where they were on every platform: the Android and iOS folders
  (the app's files folder; `Library/Application Support`) are now part of `SaveStore.DefaultDirectory` instead of an
  environment variable the phone hosts set.
- The browser page's classes are per page: `BrowserTouch` and `BrowserLifecycle` are replaced by a `BrowserPage` the
  host owns (`Pointer`, `Screen`, `Hidden`, `Vibrator`), and `BrowserLauncher.CreateSession(createGame)` by
  `BrowserLauncher.CreateSession(configure, page)`, whose `GameSession` starts each run with a `LaunchRequest`
  (`session.Start(new LaunchRequest { ScriptText = script })`). Only a browser host of your own is affected: the
  packages `microengine` builds use the new host.
- `Game.Random` is a `SeededRandom` (a `Random` subclass in `MicroEngine.Kernel`) that draws exactly what
  `new Random(seed)` drew, so games see the same values; `game.Random.GetType()` is no longer `Random`. `Game`,
  `Scene`, `InputMap`, `CoroutineRunner` and `Tweener` implement `IStateHashed` and have a public `HashState` method.
  **Migration:** a scene or game that already declared its own `HashState(StateHasher)` method marks it `override`;
  one with another member named `HashState` renames it.
- The platform contracts live in `MicroEngine.Platform` (namespace and assembly, carried by the `MicroEngine` package):
  `IPlatform` now extends `IPlatformHost`. A custom platform implements `Clock` (an `IMonotonicClock`; `Now` moved
  there), `PumpEvents(sink)` (replaces `PollEvents`, `CloseRequested` and `Suspended`: it delivers quit requests,
  suspension and resumption, focus and resize events to the sink, and returns false while suspended), `MainSurface`,
  `CanBlock` and `TryGetCapability<T>`, and declares `Letterbox` (no default any more). `IPlatform.RealTime`,
  `DisplayRate`, `Wait` and `Touch` are removed. **Migration:** offer optional features as capabilities:
  `IDisplayControl`, `IPlatformDiagnostics`, `ITouchScreen`, `IDisplayPacing` (the old `DisplayRate`) and
  `IPreciseDelay` (the old `Wait`; a platform that cannot block reports `CanBlock = false` instead of returning false).
  Code that cast a platform (`platform as IDisplayControl`) calls `platform.TryGetCapability<IDisplayControl>(out var
  display)`. A platform that wraps another forwards every member, `TryGetCapability` included.
- `IPlatform.RealTime` is replaced by a stated preset: a platform may offer a `RunPreset` capability whose `TimeMode`
  applies to runs where neither the run (`RunOptions.TimeMode`) nor the game (`Configure`) chooses. The headless
  platform's preset is `RunPreset.Testing` (deterministic), so tests that run a game headless keep one step per frame
  with no change; SDL and raylib state none, so their runs stay real-time. **Migration:** a custom platform that
  returned `RealTime => false` declares `RunPreset.Testing` as a capability; one that returned true needs nothing.
  `new HeadlessPlatform { Preset = new RunPreset() }` gives a headless platform with no preset.
- `PlatformRequest.FixedStep` and `DesktopOptions.FixedStep` are removed. **Migration:** a request that must not wait
  for the display sets `WaitForDisplay = false`; a host that shows a start screen sets
  `StartGamepadsAfterFirstFrame = true`; silence is `AudioOutput.Silent` or `Mute` (`AudioOutput.Automatic` opens a
  device on every platform; the launcher's `--audio auto` stays silent for deterministic runs).
- `DesktopOptions` has no positional constructor any more: every option is set by name, so a call written for the
  earlier `DesktopOptions(scale, hidden, fixedStep, mute, captureScale)` fails to compile instead of meaning something
  else. **Migration:** `new DesktopOptions(2, true, false, true)` becomes
  `new DesktopOptions { Scale = 2, Hidden = true, Mute = true }` (drop the fixed-step value; set
  `WaitForDisplay = false` where it kept a visible window from waiting for the display).
- A profile's request parts are composed into the backend, never wrapped: `PlatformProfile.Create` returns the backend
  itself. **Migration:** a custom backend whose profile lets hosts replace audio or add input builds its devices with
  `request.CreateAudio(...)` and `request.CombineInput(...)`; a profile that ignores them is refused with
  `PlatformConfigurationException`.
- `ISaveStorage` extends `MicroEngine.Platform.IStorage` (same members) and `SaveStore` also accepts any `IStorage`;
  `ContentFolder` is an `IContentSource` (with `Open`). Existing code compiles unchanged.

- `DialogueBox` shows every line of a page: a page that wraps to more than `Lines` lines continues on the next
  confirm (before, the extra lines were never shown). Pages written short show as before.
- `BitmapFont.Wrap` breaks a word wider than `maxWidth` between characters (before, it overflowed the line).
- `AudioMixer.PlayMusic` with the track that is fading out (towards another track or silence) brings it back
  instead of restarting it from the beginning.
- A game whose project declares an icon opens on its [start screen](guide/app-identity.md#start-screen) while it
  loads (desktop windows too). **Migration:** to keep a plain black window, set
  `<MicroEngineSplash>false</MicroEngineSplash>`.
- `IInputSource.PadStyle`, `IOverlayCanvas.Image` and `IPlatform.Letterbox` are new interface members with defaults:
  existing implementations compile unchanged (a custom overlay canvas draws no images until it implements `Image`).
- The loop owns time. Runs in deterministic time (`--script`, the new `--time deterministic`, headless runs) open no
  sound device: `--report` prints `audio: unavailable` there. A hidden window alone implies neither deterministic time
  nor silence: `--hidden` without a script runs in real time and opens a sound device where there is one.
  **Migration:** a check that must prove a device opens passes `--audio device`
  (`PlatformRequest.AudioOutput = AudioOutput.Device` for hosts); a hidden run that must be silent passes
  `--audio silent` or `--mute`; one that must be reproducible passes `--time deterministic`
  (`RunOptions.TimeMode` for hosts).
- `IPlatform.Now` is only a monotonic clock: a custom platform must not count frames or read the update rate there.
  `IPlatform.RealTime` is the time mode a run uses when neither the run nor the game chooses one;
  `PlatformRequest.FixedStep` asks for deterministic time and no longer changes a backend's clock.
  `HeadlessPlatform.UpdateRate` is removed: the loop takes the rate from `GameSettings.UpdateRate` (or
  `TimeSettings.FixedRate`). **Migration:** delete `UpdateRate = ...` from headless platforms; read the game's
  `TimeSettings` instead of `IPlatform.RealTime` to know whether time follows the wall clock.
- `Game` has new members (`Configure`, `Services`, `Schedule`, `Lifecycle`, `Log`, `TimeSettings`, `Frame`,
  `TryGetCapability`). **Migration:** a game class with a member of one of those names renames it or marks it `new`.
- Without `--platform` or `MICROENGINE_PLATFORM`, a default platform that cannot start no longer ends the launch: the
  next one is tried (desktop: SDL, raylib, SDL's software renderer) and the reason is printed. A named platform still
  fails with exit code 1.
- With VSync, real-time runs count frame time in refresh periods (`IPlatform.DisplayRate`). A custom platform keeps the
  old timing until it reports a rate; `--pacing measured` keeps it for any run.
- `IPlatform.DisplayRate` and `IPlatform.Wait` are new interface members with defaults: existing implementations
  compile unchanged.
- `EntityList<T>` is walked without allocating: `GetEnumerator` returns `EntityList<T>.Enumerator`, `OfType<TEntity>`
  returns `EntityList<T>.TypedEntities<TEntity>` and `Overlapping` returns `EntityList<T>.OverlappingEntities` (structs
  that are still `IEnumerable`, so LINQ works on them). **Migration:** code that stored the results as
  `IEnumerable<...>` compiles unchanged; rebuild against the new SDK. (Superseded in the same version: `EntityList<T>`
  and `Entity` are removed; see the entry on `Entity` above.)
- `SpriteAtlas.Frames` returns a shared, read-only `IReadOnlyList<Sprite>` (collected once per name) instead of a new
  `Sprite[]` per call. **Migration:** use `Count` instead of `Length`, and `IReadOnlyList<Sprite>` for fields that held
  the array.
- A game without any scene runs (it used to fail with an index error).
- Content file names must match their letter case exactly on every system: `ContentFolder` (and `Game.Assets`) refuse
  `Hero.png` for a file named `hero.png` on Windows and macOS too, naming the real spelling, because Linux, Android and
  browsers never found it. **Migration:** correct the spelling in the code or rename the file.
- Every file under `Content/` is copied to the output and to packages without a project item. **Migration:** the
  `<None Include="Content/**" ... />` item can be removed (keeping it does no harm); a project that must not ship its
  `Content` folder sets `<MicroEngineContentItems>false</MicroEngineContentItems>`. Two files whose names differ only in
  letter case fail the build (ME0010).
- `IGraphicsDevice` has `DefaultFilter` and `CreateTexture(image, filter)`; `Texture` subclasses implement
  `Upload(x, y, width, height, pixels)` and `ApplyFilter(filter)`. **Migration:** custom graphics devices and textures
  (tests' fakes, your own platform) implement them; `CreateTexture(image)` keeps working through the default filter.
- `PixelImage.FromPng` and `PixelImage.Load` read 16-bit and interlaced PNGs (they used to throw
  `NotSupportedException`), and `Load` reads JPEG, BMP, TGA, GIF and PSD too.
- `DialogueBox.Draw` and `SelectionList.Draw` take an `IFont`; `SelectionList.Draw`'s `lineHeight` is a `float?` whose
  default (the font's line height plus a fifth) is 12 for the built-in font, as before. Calls with `BitmapFont` compile
  and draw unchanged.
- `SpriteAtlas.Frames(name)` also finds frames numbered from 1 when there is no `name0`.
- Windows larger than the display open at the largest size that fits (a `Scale` of 4 on a small laptop used to open a
  window bigger than the screen).
- The 2D drawing model (`IRenderer`, `Texture`, `IGraphicsDevice`, `PixelImage`, sprites, atlases, animation, particles,
  light, parallax and their helpers) lives in `MicroEngine.Graphics2D.dll`, carried by the `MicroEngine` package, with its
  namespace unchanged; `MicroEngine.dll` forwards every moved type. Nothing to change: source and compiled code find the
  types as before. The headless renderer and the reference rasterizer stay in `MicroEngine.dll`; the fonts moved
  later to `MicroEngine.Text.dll` (above).
- The SDL and raylib platforms draw through the graphics device: `IRenderer` is a façade over the 2D batcher, and the
  canvas is premultiplied. Behaviour that changes: `BlendMode.Multiply` with a translucent colour now computes
  s·a·d + d·(1 − a) (it computed s·d + d·(1 − a), which brightened as the colour became more transparent); a
  translucent `Clear` stores its colour times its alpha, as the window always showed it, and captures now match the
  window; raylib's window no longer darkens where the canvas is translucent; circles, lines and triangles have the same
  edge pixels on SDL and raylib. Opaque drawing is unchanged (captures of the demos differ by at most 3/255 per
  channel). `Camera2D` moved to `MicroEngine.Graphics2D.dll` with its namespace unchanged and gained `Zoom`, `Rotation`,
  `WorldToView` and `VisibleBounds`; `Game.CreateCamera()` and `new Camera2D(width, height, presentation)` take its
  pixel snap from the presentation (`CameraPixelSnap`: on for pixel art, off for high-resolution 2D), while
  `new Camera2D(width, height)` keeps snapping as before. **Migration:** a game that relied on the old translucent `Multiply` uses an
  opaque colour or `Color.Lerp(Color.White, shade, alpha)`.
- `GameSettings.Presentation` (a `Presentation` record) states how the canvas reaches the window, one choice per
  field, starting from the presets `Presentation.PixelArt` (the default) and `Presentation.HighResolution2D`.
  `PlatformRequest.CaptureScale` defaults to the presentation's `CaptureScale`, and `WindowSizing.Initial` uses its
  `WholeFactorWindows`. Nothing to change for a pixel-art game
  ([Presentation](manual/rendering.md#presentation-one-choice-per-field)).
- New, nothing to change: the `MicroEngine` package carries a fourth assembly, `MicroEngine.Graphics.dll`, with the
  graphics device contract (namespace `MicroEngine.Graphics.Devices`: `IGraphicsDevice`, `GraphicsDeviceBase`, command
  lists, render targets, the CPU `ReferenceGraphicsDevice`; [The graphics device](manual/rendering.md#the-graphics-device)).
  Its `IGraphicsDevice` and `Texture` are not the `MicroEngine.Graphics` types games use today; code that imports both
  namespaces names the one it means.

## 0.4.0-alpha.2

Fixes that change behaviour a game may rely on:

- `Animator.Speed` refuses a negative value or NaN with `ArgumentOutOfRangeException` (it used to make `Frame` throw
  later). Use 0 to pause.
- `SelectionList.SetOptions` refuses an empty list with `ArgumentException`, like the constructor; `Index` is clamped
  to the options.
- `Synth.Layer` refuses clips that are not mono at 44.1 kHz with `ArgumentException` (it used to mix them wrongly).
- A tap that starts and ends within one frame now reports `Released` in the next update.
- Scene changes requested while a transition plays apply after its swap, in the order requested.
- `AudioMixer.PlayMusic(name, 0)` starts the music at full volume; `CoroutineRunner.StopAll` finishes coroutines that
  had not run yet; `SaveStore.Delete` lets an unreadable slot be saved again; `ContentFolder` refuses paths outside its
  folder in `Exists` too.

## 0.4.0-alpha.1

The first public SDK. Game code builds and behaves as before;
what changes is how a game is referenced, identified and packaged.

- **Engine from packages.** A game outside this repository references `MicroEngine.Desktop` (or `MicroEngine.Sdl`)
  at `0.4.0-alpha.1` instead of the engine's projects. **Migration:** replace the `ProjectReference` to
  `src/MicroEngine.Desktop/MicroEngine.Desktop.csproj` with
  `<PackageReference Include="MicroEngine.Desktop" Version="0.4.0-alpha.1" />`.
- **Identity in the project file.** Name, id, version, icon and orientation are MSBuild properties
  ([Application identity](guide/app-identity.md)); the build now checks them. **Migration:** add `ApplicationTitle`,
  `ApplicationId` (needed for macOS, Android and iOS packages), `MicroEngineIcon`, and `MicroEngineGameFactory` when
  the game's class is not `<RootNamespace>.MainGame` or its constructor takes arguments.
- **Browser and phone hosts come with `microengine`.** A game is packaged from its own project; it no longer needs
  a host project or a registry entry. **Migration:** `microengine package web`, `android` or `ios` in the game's
  folder (or with `--project`).
- **New, compatible:** `AppIdentity`, `AppOrientation`, `PlatformRequest.Identity`, `DesktopOptions.Icon`. A
  platform of your own may read `request.Identity` to name its window and set its icon; ignoring it is fine.
- **Version numbers.** The SDK uses numbered pre-releases (`0.4.0-alpha.1`, `alpha.2`...) and tags `sdk-v<version>`.

## 0.3.0-alpha

Unified input and touch controls. Games that
only bind actions and read `Down`/`Pressed`/`Released`/`Axis` build and behave as before; one type was removed.

- **`TouchPad` removed.** On-screen controls are now per game (`Game.CreateTouchLayout`, `VirtualControls`) and press
  actions, not gamepad buttons. A game that did nothing gets `TouchLayout.CreateDefault()`: a d-pad and A/B/C/pause
  buttons pressing the default actions `left`/`right`/`up`/`down`/`confirm`/`cancel`/`action`/`pause`, as the old
  pad's buttons did through `PadButton` bindings. **Migration:** if your game bound extra actions to `PadButton.A`,
  `B`, `X` or `RightShoulder` so the old pad would press them, describe them in a layout instead:

  ```csharp
  protected override TouchLayout CreateTouchLayout() => TouchLayout.Of(
      new TouchDPad { Offset = new(104, 100) },
      new TouchButton("jump", "jump", "A") { Anchor = TouchAnchor.BottomRight, Offset = new(70, 92), Also = ["confirm"] });
  ```

  Code that read `TouchPad` from a backend (`SdlPlatform` used it internally) reads `IPlatform.Touch` instead.
- **Stick directions.** The left stick now has a radial dead zone (0.2) and presses directions by 45° sectors past a
  threshold (`StickDeadZone`, `StickThreshold`, `StickMode`) instead of per axis beyond 0.5. A straight push behaves as
  before; a diagonal now presses both directions from about 22.5° off the axis. Set `StickMode = StickDirections.Four`
  for grid movement or menus.
- **New, additive.** `InputMap.Value`, `AnalogAxis`, `Vector`, `BindAxis`, `Unbind`, `GetBindings`, `AddSource`,
  `LastDevice`, `PadConnected`, `Pointer`; `PadAxis`, `PadButton.LeftStick`/`RightStick`; `IActionSource`;
  `IInputSource.GetPadAxis`/`PadConnected`/`Pointer` and `IPlatform.Touch` with default implementations (existing
  implementations compile unchanged); `Scene.Describe()` for diagnostics; input script commands `touch`, `tap`, `lift`;
  `HeadlessPlatform.TouchScreen`; `Scene.TouchActions` and `VirtualControls.SetContext` (controls by context);
  `ITouchOverlay.Frame`, `VirtualControls.KeepFrameClear` and `MinimumFrameFraction` (the frame fitted around shown
  controls). An `ITouchOverlay` of your own keeps the plain letterbox through the default `Frame`.
- **Frame placement.** Backends fit the frame inside the window's safe area, place it at the top on portrait screens,
  and scale fractionally when whole factors would fill less than 80 % of the space (`FrameLayout.Fit`). While touch
  controls are shown and would cover it, the frame moves to the top and shrinks until none does. Captures are
  unchanged; on a phone the picture may sit differently than before.

## 0.2.0-alpha

The first versioned SDK. Nothing a game calls was removed or renamed; a game written for the unversioned engine builds
unchanged.

- **Versions.** The SDK version is `MicroEngineVersion` in `Directory.Build.props`; engine assemblies carry it. Give your
  game its own `<Version>` in its project file.
- **`--report`.** A new launcher argument that prints the platform that runs and whether audio opened.
- **Native library errors.** A backend whose native library the system cannot load (for example SDL on a Linux with an
  older glibc) now ends the run with `error: platform '<name>' could not load its native library` and exit code 1
  instead of an unhandled exception. A host that caught `DllNotFoundException` around `GameLauncher.Run` no longer sees it.
- **macOS bundles.** `DesktopLauncher` reads content from `<App>.app/Contents/Resources/Content` when the executable runs
  inside an application bundle and has no `Content` folder next to it.
- **Content folders.** If your project copies `Content/**` to its output and keeps C# files there (as the demos did),
  exclude them: `<None Include="Content/**" Exclude="Content/**/*.cs" CopyToOutputDirectory="PreserveNewest" />`.
