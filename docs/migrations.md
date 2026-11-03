# Migration notes

What changes between SDK versions and how to update a game. MicroEngine is in Alpha ([Versions](guide/sdk.md)):
a version may change public APIs incompatibly, and when it does, the change is listed here with the version that makes
it. Newest first.

## Next release

Not published yet. Behaviour that changes:

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
  `IEnumerable<...>` compiles unchanged; rebuild against the new SDK.
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
  field; `GameSettings.Scaling` keeps working and picks the preset `Presentation.PixelArt` or
  `Presentation.HighResolution2D`, which reproduce what `Pixel` and `Smooth` did. `PlatformRequest.CaptureScale`
  defaults to the presentation's `CaptureScale`, and `WindowSizing.Initial` uses its `WholeFactorWindows`. Nothing to
  change; to combine choices `Scaling` could not express, set `Presentation`
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
