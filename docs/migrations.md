# Migration notes

What changes between SDK versions and how to update a game. MicroEngine is in Alpha ([Versions](guide/sdk.md)):
a version may change public APIs incompatibly, and when it does, the change is listed here with the version that makes
it. Newest first.

## Next release

Not published yet. Behaviour that changes:

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
- Runs with `--hidden`, `--script` (any fixed-step run) open no sound device: `--report` prints `audio: unavailable`
  there. **Migration:** a check that must prove a device opens passes `--audio device`
  (`PlatformRequest.AudioOutput = AudioOutput.Device` for hosts).
- Without `--platform` or `MICROENGINE_PLATFORM`, a default platform that cannot start no longer ends the launch: the
  next one is tried (desktop: SDL, raylib, SDL's software renderer) and the reason is printed. A named platform still
  fails with exit code 1.
- With VSync, real-time runs count frame time in refresh periods (`IPlatform.DisplayRate`). A custom platform keeps the
  old timing until it reports a rate; `--pacing measured` keeps it for any run.
- `IPlatform.DisplayRate` is a new interface member with a default (0): existing implementations compile unchanged.

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
