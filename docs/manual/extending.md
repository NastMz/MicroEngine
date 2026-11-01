# Extending the engine

MicroEngine is a set of small contracts with official implementations behind them. A game, a tool or a port can supply
its own implementation of most of them without changing the engine. This page lists the extension points and shows
each with code from the [focused examples](../examples/focused.md), which compile against the published SDK and run in
MicroEngine's tests.

## Extension points

| You want to | Implement | Plug it in with |
| --- | --- | --- |
| press actions from something that is not a key (accessibility switch, network player, replay, AI helper) | `IActionSource` | `Input.AddSource(source)` |
| read another device library at the device level | `IInputSource` | `PlatformRequest.ExtraInput` ([Backends](backends.md#replacing-a-part)) |
| keep saves somewhere else (memory, a cloud service, a console API) | `ISaveStorage` | `new SaveStore(storage)` or `SaveStore.DefaultStorage` |
| a scene transition of your own | subclass `Transition` | `Scenes.Push/Replace(scene, transition)` |
| another audio device (for example SDL's audio under raylib) | `IAudioDevice` | `PlatformRequest.Audio` ([Backends](backends.md#replacing-a-part)) |
| change or wrap a platform, or add a new one | `IPlatform` | a `PlatformProfile` in the launcher's `PlatformCatalog` |
| game objects with deferred spawning and removal | subclass `Entity` | `EntityList<T>` ([Tile maps and collision](world.md#entities)) |
| your own on-screen controls | records of `TouchLayout` | `Game.CreateTouchLayout` ([Input](input.md#touch-controls)) |

The window, the renderer and the graphics device share one graphics context, so they are replaced together, as a
platform, never one by one.

## An input source

<!-- code: samples/Examples/Examples/CustomSourceExample.cs#source -->
```csharp
/// <summary>
/// An action source of your own: anything that is not a key can press actions (an accessibility switch, a network
/// player, a replay). This one presses "confirm" once a second. The game reads it like any key.
/// </summary>
public sealed class Metronome : IActionSource
{
    public float Time;
    public bool IsDown(string action) => action == "confirm" && Time % 1f < 0.1f;
    public InputDevice Device => InputDevice.Other;
}
```

<!-- code: samples/Examples/Examples/CustomSourceExample.cs#use -->
```csharp
public override void Enter() => Input.AddSource(_metronome);
public override void Exit() => Input.RemoveSource(_metronome);

public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    _metronome.Time += dt;
    if (Input.Pressed("confirm")) _beats++;   // the metronome and the keyboard both count
}
```

`IsDown(action)` is asked once per fixed step for every action; it is the only member you must write. Three more have
default implementations: `Device` (what `Input.LastDevice` reports when the source presses something; `Other` by
default), `Value(action)` (1 when down; return 0..1 for an analog source) and `Actions` (actions the source presses that
no key is bound to, so `Input` reads them).
The source is read before every step together with the keyboard, gamepad and touch controls; presses shorter than a
step still produce one `Pressed`.

## A save storage

<!-- code: samples/Examples/Extending.cs#save-storage -->
```csharp
/// <summary>
/// Where save slots live is an ISaveStorage: files on desktops and phones, localStorage in browsers. This one keeps them
/// in memory (tests, a demo kiosk); a cloud service or a console's save API would implement the same six members.
/// </summary>
public sealed class MemorySaveStorage : ISaveStorage
{
    private readonly Dictionary<string, string> _slots = [];

    public string Location => "memory";
    public string? Read(string slot) => _slots.GetValueOrDefault(slot);
    public void Write(string slot, string text) => _slots[slot] = text;
    public bool Exists(string slot) => _slots.ContainsKey(slot);
    public void Delete(string slot) => _slots.Remove(slot);
    public void SetAside(string slot)
    {
        if (_slots.Remove(slot, out var text)) _slots[slot + ".corrupt"] = text;
    }
}
```

`SaveStore` keeps its guarantees on top of any storage: it serializes, sets aside data it cannot parse
(`SetAside`), never overwrites a slot it could not read, and turns refused writes into `false`. A storage only reports
failures as `IOException` or `UnauthorizedAccessException`. Use it for one store with `new SaveStore(storage)`, or for
every store a game creates by setting `SaveStore.DefaultStorage = gameId => new MyStorage(gameId)` before the game is
constructed (the browser launcher does exactly that with its `localStorage` storage).

## A transition

<!-- code: samples/Examples/Extending.cs#transition -->
```csharp
/// <summary>A transition of your own: two curtains that close, then open on the new scene.</summary>
public sealed class CurtainTransition(float duration = 0.6f) : Transition(duration)
{
    // Cover goes 0 -> 1 while the old scene is hidden; the scene changes at 1, then Cover goes back to 0.
    public override void Draw(IRenderer r)
    {
        float half = r.Width / 2f * Cover;
        r.FillRect(new RectF(0, 0, half, r.Height), Color.Hex("b13e53"));
        r.FillRect(new RectF(r.Width - half, 0, half, r.Height), Color.Hex("b13e53"));
    }
}
```

The base class runs the timing: `Cover` rises from 0 to 1 over the first half of `Duration`, the scene changes, then
`Cover` falls back to 0. `Draw` paints the cover over the whole screen in screen pixels. The examples' list opens every
example with this transition.

## A platform

A platform is an `IPlatform`: time, events, a renderer per frame, the graphics, audio and input devices, screenshots,
fullscreen and suspension. Writing one for a new library is a project of its own ([Writing a new
backend](backends.md#writing-a-new-backend)); wrapping an existing one is a few lines. This wrapper counts presented
frames, and a profile makes it selectable from the command line:

<!-- code: samples/Examples/Extending.cs#platform -->
```csharp
/// <summary>
/// A platform of your own does not have to start from nothing: this one wraps any other platform and counts the
/// frames it presents (telemetry, a frame-rate overlay, a recorder would hook in the same place).
/// </summary>
public sealed class CountingPlatform(IPlatform inner) : IPlatform
{
    public long Frames { get; private set; }

    public IGraphicsDevice Graphics => inner.Graphics;
    public IAudioDevice Audio => inner.Audio;
    public IInputSource Input => inner.Input;
    public ITouchScreen? Touch => inner.Touch;
    public bool CloseRequested => inner.CloseRequested;
    public double Now => inner.Now;
    public bool RealTime => inner.RealTime;
    public bool Suspended => inner.Suspended;
    public void PollEvents() => inner.PollEvents();
    public IRenderer BeginFrame() => inner.BeginFrame();
    public void EndFrame()
    {
        inner.EndFrame();
        Frames++;
    }
    public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
    public void ToggleFullscreen() => inner.ToggleFullscreen();
    public void Dispose()
    {
        Console.WriteLine($"{Frames} frames presented");
        inner.Dispose();
    }

    /// <summary>A profile for the launcher's catalog: "--platform sdl-counted" runs SDL through this wrapper.</summary>
    public static PlatformProfile Wrap(PlatformProfile profile) => new(
        $"{profile.Name}-counted", $"{profile.Description}, counting frames",
        request => new CountingPlatform(profile.Create(request)))
    {
        CheckSupport = () => profile.UnsupportedReason,   // runs wherever the wrapped platform runs
        Replaceable = profile.Replaceable,
    };
}
```

The game's `Program.cs` adds the profile to the desktop catalog:

<!-- code: samples/Examples/Program.cs#launch -->
```csharp title="Program.cs"
// The official desktop catalog (sdl, raylib, sdl-software, headless) plus one platform of our own:
// "--platform sdl-counted" runs SDL through CountingPlatform (Extending.cs).
var catalog = DesktopLauncher.CreateCatalog().Add(CountingPlatform.Wrap(SdlPlatform.Profile));
return DesktopLauncher.Run(new ExamplesGame(), args, catalog);
```

```text
$ microengine run --project samples/Examples --platform sdl-counted -- --frames 30
done: 30 updates, 30 frames
30 frames presented
```

- A `PlatformProfile` has a name (for `--platform` and `MICROENGINE_PLATFORM`), a description (for
  `--list-platforms`), a factory from `PlatformRequest` to `IPlatform`, `CheckSupport` (null when it can run here,
  otherwise the reason) and `Replaceable` (the parts a host may supply: audio, extra input).
- The `PlatformRequest` carries the game's settings and identity, window scale, hidden and fixed-step modes, mute and
  the replaced parts; a factory passes it on.
- The catalog's first profile that can run is the default. `DesktopLauncher.CreateCatalog()` holds `sdl`, `raylib`,
  `sdl-software` and `headless`; a game that ships SDL only builds its own with `new PlatformCatalog()`.

## What is not an extension point

- **Hosts for phones and browsers** are built by `microengine package`; a game does not provide its own Android
  activity, iOS app or web page in 0.4.0-alpha.2.
- **Rendering features beyond `IRenderer`** (shaders, render targets) cannot be added from a game: the renderer contract
  has no hook for them.
- **The game loop** (fixed step, order of a frame) is the engine's; a host that drives frames itself uses
  `GameSession` ([Game lifecycle and time](lifecycle.md#loop-runner-and-session)).

## Related

- [Backends and platforms](backends.md): the official platforms, choosing one, replacing a part, writing a backend
- [Architecture](../architecture.md): how the layers depend on each other
- [Public contracts](../reference/contracts.md): the interfaces in full
