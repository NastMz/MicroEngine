# Contracts

This page lists the public types a game and a backend depend on, with their real signatures from `src/`. They are
the stable surface the four games are written against: the game base class and its settings, scenes, the
platform boundary and its device interfaces, save storage, input actions and achievements. Bodies are omitted
with `// ...`; each block links its source file.

## Game

[`src/MicroEngine/Core/Game.cs`](../../src/MicroEngine/Core/Game.cs), namespace `MicroEngine.Core`.

```csharp
public abstract class Game
{
    public abstract GameSettings Settings { get; }
    public IGraphicsDevice Graphics => Platform.Graphics;
    public AudioMixer Audio { get; private set; } = null!;
    public InputMap Input { get; private set; } = null!;
    public VirtualControls Touch { get; private set; } = null!;
    public SceneManager Scenes { get; private set; } = null!;
    public BitmapFont Font { get; private set; } = null!;
    public float Time { get; private set; }
    public long UpdateCount { get; private set; }
    public Random Random { get; private set; } = new(1);
    public bool QuitRequested { get; private set; }
    public int Width => Settings.VirtualWidth;
    public int Height => Settings.VirtualHeight;
    public AchievementTracker Achievements => // ...
    public bool HasAchievements => _achievements is not null;
    public AchievementToasts? AchievementToasts { get; private set; }
    protected IPlatform Platform => // ...

    protected abstract void Load();
    protected virtual void Unload() { }
    protected virtual InputMap CreateInput() => InputMap.CreateDefault();
    protected virtual TouchLayout CreateTouchLayout() => TouchLayout.CreateDefault();
    public void Quit() => QuitRequested = true;
    protected AchievementTracker UseAchievements(IEnumerable<Achievement> catalog, SaveStore store, UiTheme? theme = null)
    public void InitializeForTest(IPlatform platform, int seed = 1) => Initialize(platform, seed);
    public void StepForTest()
}
```

| Member | Contract |
| --- | --- |
| `Load` | Called once after the platform exists. Build atlases and sounds, push the first scene |
| `Unload` | Called once before the platform is disposed |
| `CreateInput` | Override to change or add action bindings |
| `CreateTouchLayout` | Override to choose the on-screen controls ([Touch controls](../manual/input.md#touch-controls)) |
| `Touch` | The on-screen controls at run time: shown, pressed, geometry, `SetVisible`, `Layout`. Next SDK version: available from `Configure`; active on touch-first screens or once the game sets a layout, inactive and free otherwise (`IsActive`) |
| `Random` | Deterministic per run; seeded by the launcher (seed 1 under a script) |
| `UseAchievements` | Declares the achievements. Next SDK version: called from the constructor or `Configure` (before the engine is composed), it adds the achievements module; from `Load` or later it throws `InvalidOperationException` |
| `Achievements` | Throws until `UseAchievements` was called |
| `InitializeForTest` / `StepForTest` | Run `Load`, then advance one fixed update after sampling input, without a loop |

## GameSettings

[`src/MicroEngine/Core/Platform.cs`](../../src/MicroEngine/Core/Platform.cs).

```csharp
public sealed record GameSettings(string Title, int VirtualWidth = 320, int VirtualHeight = 180, int Scale = 4)
{
    public int UpdateRate { get; init; } = 60;
    public Color Background { get; init; } = Color.Black;
}
```

All options are described in [Configuration](configuration.md#gamesettings).

## Scene and SceneManager

[`src/MicroEngine/Scenes/Scene.cs`](../../src/MicroEngine/Scenes/Scene.cs), namespace `MicroEngine.Scenes`.

```csharp
public abstract class Scene
{
    public Game Game => // ...
    public InputMap Input => Game.Input;
    public AudioMixer Audio => Game.Audio;
    public BitmapFont Font => Game.Font;
    public CoroutineRunner Coroutines { get; } = new();
    public Tweener Tweens { get; } = new();
    public float Age { get; private set; }
    public virtual bool IsOverlay => false;
    public virtual bool BlocksUpdateBelow => true;

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Resume() { }
    public virtual string? Describe() => null;                          // development probes
    public virtual IReadOnlyCollection<string>? TouchActions => null;   // touch controls this scene needs; null = all
    public abstract void Update(float dt);
    public abstract void Draw(IRenderer renderer);
}

public sealed class SceneManager
{
    public Scene? Current => // ...
    public int Count => _stack.Count;
    public bool InTransition => _transition is not null;
    public IReadOnlyList<Scene> Stack => _stack;
    public void Push(Scene scene, Transition? transition = null)
    public void Pop(Transition? transition = null)
    public void Replace(Scene scene, Transition? transition = null)
    public void Reset(Scene scene, Transition? transition = null)
}
```

Stack changes are scheduled and applied between updates. `Transition.Fade`, `Iris`, `Diamonds` and `Wipe` create
the built-in transitions. See [Scenes](../manual/scenes.md).

## IPlatform

[`src/MicroEngine/Core/Platform.cs`](../../src/MicroEngine/Core/Platform.cs). Everything a backend provides.

```csharp
public interface IPlatform : IDisposable
{
    IGraphicsDevice Graphics { get; }
    IAudioDevice Audio { get; }
    IInputSource Input { get; }
    bool CloseRequested { get; }
    double Now { get; }
    bool RealTime { get; }
    bool Suspended => false;
    ITouchScreen? Touch => null;
    void PollEvents();
    IRenderer BeginFrame();
    void EndFrame();
    void SaveScreenshot(string path);
    void ToggleFullscreen();
}
```

`RealTime` is true when time follows the wall clock (desktop, browser) and false when the loop advances one step
per frame (headless). `Suspended` is true while the system has taken the app away (a phone app in the background);
the loop then pauses. `Touch` is the touch screen, if any: touches, safe area, density, vibration and the overlay
layer. Implementations: `SdlPlatform`, `RaylibPlatform`, `HeadlessPlatform`.

Next SDK version (not published yet: arrives with the next SDK version): `IPlatform` extends the host contract of
`MicroEngine.Platform`, and the members above that are not drawing, devices or screenshots move into it:

```csharp
public interface IPlatformHost : IDisposable
{
    IMonotonicClock Clock { get; }
    bool PumpEvents(IEngineEventSink sink);   // Resized, FocusChanged, Suspending, Resumed, QuitRequested
    ISurface? MainSurface { get; }
    bool TryGetCapability<T>(out T? capability) where T : class;
    bool CanBlock { get; }
}

public interface ISurface
{
    nint NativeWindow { get; }
    (int Width, int Height) PixelSize { get; }
    float PixelDensity { get; }
    bool IsMinimized { get; }
    event Action? Resized;
    event Action? Lost;
}
```

`RealTime`, `Suspended`, `Touch`, `DisplayRate`, `Wait`, `CloseRequested` and `PollEvents` leave `IPlatform`: the time
mode is the loop's only, suspension and quit requests are events of the pump, and optional features are capabilities
(`IDisplayControl`, `IPlatformDiagnostics`, `ITouchScreen`, `IDisplayPacing`, `IPreciseDelay`). The rules every host
follows are in [The platform host](../manual/backends.md#the-platform-host). The same assembly holds
`IStorage` (save slots; `ISaveStorage` extends it) and `IContentSource` with `FolderContentSource`.

### Platform profiles

[`src/MicroEngine/Core/PlatformProfiles.cs`](../../src/MicroEngine/Core/PlatformProfiles.cs). How a host chooses and
assembles a platform ([Backends](../manual/backends.md#choosing-a-platform)).

| Type | Role |
| --- | --- |
| `PlatformProfile(name, description, create)` | A named way to build an `IPlatform`; `CheckSupport` says why it cannot run here; `Replaceable` lists the `PlatformParts` a host may supply |
| `PlatformCatalog` | Profiles in order of preference; `Default` is the first that can run, `Resolve(name)` finds one or throws |
| `PlatformRequest(settings)` | Window options (`Scale`, `Hidden`, `FixedStep`, `Mute`, `CaptureScale`), an optional `Audio` factory and `ExtraInput` sources; `OpensOwnAudio` tells a backend whether to open its device. Next SDK version: `FixedStep` is replaced by `WaitForDisplay` and `StartGamepadsAfterFirstFrame`, and a backend builds its devices with `CreateAudio` and `CombineInput` ([options](configuration.md#platform-request-options)) |
| `PlatformConfigurationException` | An unknown or unavailable profile, or a replacement the profile does not allow |
| `CompositeInput` | Several input sources read as one |
| `GameLauncher.Run(game, args, catalog, configure)` | The desktop command line over any catalog |

### Device interfaces

[`src/MicroEngine.Graphics2D/Legacy/IGraphics.cs`](../../src/MicroEngine.Graphics2D/Legacy/IGraphics.cs):

```csharp
public interface IRenderer
{
    int Width { get; }
    int Height { get; }
    void Clear(Color color);
    void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default);
    void DrawStretched(Texture texture, RectF source, RectF destination, Color tint);
    void FillRect(RectF rect, Color color);
    void FillCircle(Vector2 center, float radius, Color color);
    void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
    void DrawLine(Vector2 from, Vector2 to, float thickness, Color color);
    void PushCamera(Camera2D camera);
    void PopCamera();
    void SetBlendMode(BlendMode mode);
    void SetClip(RectF? screenRect);
}

public interface IGraphicsDevice
{
    Texture CreateTexture(PixelImage image);
}
```

[`src/MicroEngine.Audio/Audio.cs`](../../src/MicroEngine.Audio/Audio.cs):

```csharp
public interface IAudioDevice
{
    bool IsAvailable { get; }
    SoundHandle LoadSound(string name, AudioClip clip);
    MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true);
    MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true);
    void Play(SoundHandle sound, float volume = 1f, float pitch = 1f, float pan = 0.5f);
    void PlayMusic(MusicHandle music, float volume = 1f);
    void StopMusic();
    void SetMusicVolume(float volume);
    void PauseMusic(bool paused);
    MusicHandle? CurrentMusic { get; }
    void Update();
}
```

[`src/MicroEngine.Input/Devices.cs`](../../src/MicroEngine.Input/Devices.cs):

```csharp
public interface IInputSource
{
    bool IsKeyDown(Key key);
    bool IsPadDown(PadButton button);
    System.Numerics.Vector2 LeftStick { get; }
    float GetPadAxis(PadAxis axis) => /* left stick only */;
    bool PadConnected => false;
    PointerState Pointer => default;
}
```

[`src/MicroEngine.Input.Touch/Touch.cs`](../../src/MicroEngine.Input.Touch/Touch.cs):

```csharp
public readonly record struct TouchEvent(long Finger, TouchPhase Phase, Vector2 Position);

public interface ITouchScreen
{
    Vector2 Size { get; }
    RectF SafeArea { get; }
    float PixelsPerDp { get; }
    bool TouchFirst { get; }
    void ReadTouches(List<TouchEvent> events);
    ITouchOverlay? Overlay { get; set; }
    bool CanVibrate => false;
    bool Vibrate(float strength, int milliseconds) => false;
}

public interface ITouchOverlay
{
    void Draw(IOverlayCanvas canvas);
    RectF Frame(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight) => FrameLayout.Fit(/* ... */);
}

public interface IOverlayCanvas
{
    void FillCircle(Vector2 center, float radius, Color color);
    void Ring(Vector2 center, float radius, float thickness, Color color);
    void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color);
    void FillRect(RectF rect, Color color);
    void Text(string text, Vector2 center, float height, Color color);
}
```

Coordinates are drawable pixels. `FrameLayout.Fit(window, safeArea, width, height)` is the letterbox rule every
backend uses.

Next SDK version (not published yet: arrives with the next SDK version): these interfaces keep their namespaces and
move to their own assemblies, carried by the `MicroEngine` package: `IRenderer` and `IGraphicsDevice` to
`MicroEngine.Graphics2D.dll`, `IAudioDevice` to `MicroEngine.Audio.dll`, `IInputSource` and `InputMap` to
`MicroEngine.Input.dll`. `IGraphicsDevice` adds `DefaultFilter` and `CreateTexture(image, filter)` (the one-argument
form uses `DefaultFilter`), and `IInputSource` adds `PadStyle` and `Pointers`.

Games rarely touch these directly: they use `AudioMixer`, `InputMap` and the renderer passed to `Draw`.

## InputMap

[`src/MicroEngine.Input/InputMap.cs`](../../src/MicroEngine.Input/InputMap.cs).

```csharp
public sealed class InputMap
{
    public enum StickDirection { None, Left, Right, Up, Down }
    public float StickDeadZone { get; set; }          // 0.2
    public float StickThreshold { get; set; }         // 0.35
    public StickDirections StickMode { get; set; }    // Eight
    public float AxisDeadZone { get; set; }           // 0.1
    public InputDevice LastDevice { get; }
    public bool PadConnected { get; }
    public PointerState Pointer { get; }
    public InputMap Bind(string action, params Key[] keys)
    public InputMap Bind(string action, Key[] keys, PadButton[] buttons, StickDirection stick = StickDirection.None)
    public InputMap BindAxis(string action, PadAxis axis, bool positive = true)
    public InputMap Unbind(string action)
    public ActionBindings? GetBindings(string action)
    public InputMap AddSource(IActionSource source)
    public bool RemoveSource(IActionSource source)
    public IEnumerable<string> Actions => _bindings.Keys;
    public void Sample(IInputSource source)
    public void SetForced(string action, bool down)
    public void EndUpdate()
    public bool Down(string action)
    public bool Pressed(string action)
    public bool Released(string action)
    public int Axis(string negative, string positive)
    public float Value(string action)
    public float AnalogAxis(string negative, string positive)
    public Vector2 Vector(string left, string right, string up, string down)
    public void Reset()
    public static InputMap CreateDefault()
}
```

`Bind` adds to an existing action. `SetForced` is how scripts and tests press actions. `Down`/`Pressed`/`Released`/`Axis`
are digital; `Value`/`AnalogAxis`/`Vector` analog. Action sources implement
`IActionSource { bool IsDown(string); float Value(string); InputDevice Device; IEnumerable<string> Actions; }` (the
last three have defaults). Touch controls: `TouchLayout`, `TouchDPad`, `TouchStick`, `TouchButton`, `VirtualControls`
([Touch controls](../manual/input.md#touch-controls), [`VirtualControls.cs`](../../src/MicroEngine.Input.Touch/VirtualControls.cs)). Default bindings are listed
in [Configuration](configuration.md#default-input-actions).

## SaveStore and ISaveStorage

[`src/MicroEngine.Saves/SaveStore.cs`](../../src/MicroEngine.Saves/SaveStore.cs).

```csharp
public sealed class SaveStore
{
    public static Func<string, ISaveStorage>? DefaultStorage { get; set; }
    public string Directory => _storage.Location;
    public SaveStore(string gameId, string? directory = null)
    public SaveStore(ISaveStorage storage)
    public static string DefaultDirectory(string gameId)
    public T? Load<T>(string slot) where T : class
    public bool Save<T>(string slot, T data)
    public bool Exists(string slot) => _storage.Exists(slot);
    public void Delete(string slot) => _storage.Delete(slot);
}

public interface ISaveStorage
{
    string Location { get; }
    string? Read(string slot);
    void Write(string slot, string text);
    bool Exists(string slot);
    void Delete(string slot);
    void SetAside(string slot);
}
```

| Rule | Behaviour |
| --- | --- |
| Missing or corrupt slot | `Load` returns null; corrupt data is set aside (`<slot>.corrupt.json` for files) |
| Unreadable slot (I/O or access error) | `Load` returns null and that store never writes the slot |
| Refused write | `Save` returns false; the game keeps running |
| Where | Files under `DefaultDirectory(gameId)` by default (local application data on Windows and macOS, the XDG data folder on Linux); `localStorage` in the browser via `DefaultStorage` (next SDK version: `DefaultStorage(gameId)` is a method returning the platform's default medium, `BrowserStorage` in browsers; Android and iOS folders are part of `DefaultDirectory`) |

Implementations: `FileSaveStorage` (core; next SDK version: `MicroEngine.Saves.dll`, carried by the `MicroEngine` package, namespace unchanged), `BrowserSaveStorage` (`MicroEngine.Raylib`; next SDK version: `BrowserStorage` in `MicroEngine.Platform`). See
[Persistence](../manual/persistence.md).

`SavesModule` (next SDK version, namespace `MicroEngine.Saves`): `new SavesModule(store)` registers `store` as the engine's `SaveStore` service; it schedules no frame system.

## Achievement and AchievementTracker

[`src/MicroEngine.Achievements/Achievements.cs`](../../src/MicroEngine.Achievements/Achievements.cs) (next SDK version: in `MicroEngine.Achievements.dll`, carried by the `MicroEngine` package, namespace unchanged).

```csharp
public sealed record Achievement(string Id, string Name, string Description, int Goal = 1)
{
    public Sprite? Icon { get; init; }
}

public sealed class AchievementTracker
{
    public const string SaveSlot = "achievements";
    public event Action<Achievement>? Unlocked;
    public AchievementTracker(IEnumerable<Achievement> catalog, SaveStore? store = null)
    public IReadOnlyList<Achievement> Catalog => _catalog;
    public int Count => _catalog.Count;
    public int UnlockedCount => // ...
    public bool HasUnsavedChanges { get; private set; }
    public IReadOnlyList<AchievementStatus> All => // ...
    public AchievementStatus Get(string id)
    public bool IsUnlocked(string id) => Get(id).Unlocked;
    public int Progress(string id) => Get(id).Progress;
    public bool Unlock(string id)
    public bool AddProgress(string id, int amount = 1)
    public bool SetProgress(string id, int value)
    public bool Flush()
}
```

`Id` is the saved key; keep it stable. `Goal` 1 is immediate, larger goals are progressive. `Unlock`,
`AddProgress` and `SetProgress` return true only on the call that unlocks; unknown ids throw. See
[Achievements](../manual/achievements.md).

## Running a game

| Type | Signature | Source |
| --- | --- | --- |
| `DesktopLauncher` | `public static int Run(Game game, string[] args)` | [`DesktopLauncher.cs`](../../src/MicroEngine.Raylib/DesktopLauncher.cs) |
| `BrowserLauncher` | `public static GameSession CreateSession(Func<Game> createGame)` | [`BrowserLauncher.cs`](../../src/MicroEngine.Raylib/BrowserLauncher.cs) |
| `DesktopLauncher` (next SDK version) | `public static int Run(Action<HostBuilder> configure, string[] args)`: the game's `Configure` entry point | [`DesktopLauncher.cs`](../../src/MicroEngine.Desktop/DesktopLauncher.cs) |
| `BrowserLauncher` (next SDK version) | `public static GameSession CreateSession(Action<HostBuilder> configure, BrowserPage page)` | [`BrowserLauncher.cs`](../../src/MicroEngine.Raylib/BrowserLauncher.cs) |
| `GameHost` (next SDK version) | `public static GameHost Create(HostEnvironment environment, LaunchRequest request, Action<HostBuilder> configure, Action<HostBuilder>? defaults = null)`; `CreateGame`, `CreateRunOptions`, `CreateRequest`, `CreatePlatform`, `Run`, `Play`, `CreateSession` | [`GameHost.cs`](../../src/MicroEngine/Core/GameHost.cs) |
| `GameRunner` | `public static RunResult Run(Game game, IPlatform platform, RunOptions? options = null)` | [`GameRunner.cs`](../../src/MicroEngine/Core/GameRunner.cs) |
| `GameSession` | `GameSession(Func<Game> createGame, Func<Game, IPlatform> createPlatform)`; `Start`, `Tick`, `Stop`, `Result` (next SDK version: also `GameSession(Func<LaunchRequest, GameHost> compose)` with `Start(LaunchRequest)`) | [`GameSession.cs`](../../src/MicroEngine/Core/GameSession.cs) |
| `RunResult` | `record RunResult(long Updates, long Frames, IReadOnlyList<string> Captures, Exception? Error)` | [`GameRunner.cs`](../../src/MicroEngine/Core/GameRunner.cs) |

See [Lifecycle](../manual/lifecycle.md) and [Backends](../manual/backends.md).
