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
| keep saves somewhere else (one file, a cloud service, a console API) | `ISaveStorage` | `new SaveStore(storage)`, or `host.Storage` in the game's `Configure` |
| a scene transition of your own | subclass `Transition` | `Scenes.Push/Replace(scene, transition)` |
| process the sound of a group of sounds (a filter, a distortion, a meter) | subclass `AudioEffect` | `Audio.Bus(name).Add(effect)` ([an audio effect](#an-audio-effect)) |
| another audio device (for example SDL's audio under raylib) | `IAudioDevice` | `PlatformRequest.Audio` ([Backends](backends.md#replacing-a-part)) |
| change or wrap a platform, or add a new one | `IPlatform` | a `PlatformProfile` in the launcher's `PlatformCatalog` |
| a part of a scene that the engine steps, hashes and stops with it (a store, a world, a scheduler) | subclass `SceneSystem` | `Scene.Systems.Add(system)`, or `Scenes.AddSystem` for every scene ([Scene systems](scenes.md#scene-systems)) |
| state of a module in the state hash, or work at start-up and shutdown | a section, a handler | `builder.StateSections.Add`, `builder.Lifecycle.Started` and `Stopping` ([Modules](lifecycle.md#modules-and-the-frame-schedule)) |
| game objects with deferred spawning and removal | your own classes attached to entities | `Scene.Objects`, an `IWorld` ([The scene world](scenes.md#the-scene-world)) |
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
/// Where save slots live is an ISaveStorage: files on desktops and phones, localStorage in browsers, memory in a test.
/// This one keeps every slot in one file, for a system that gives a game a single save file, or to copy a whole
/// set of saves as one thing. A cloud service or a console's save API would implement the same seven members.
/// </summary>
public sealed class SingleFileSaveStorage(string path) : ISaveStorage
{
    private const string Aside = ".corrupt";   // what SetAside keeps a slot's unreadable text under
    private Dictionary<string, string>? _slots;

    public string Location => path;
    public string? Read(string slot) => Slots.GetValueOrDefault(slot);
    public bool Exists(string slot) => Slots.ContainsKey(slot);

    public void Write(string slot, string text)
    {
        Slots[slot] = text;
        Flush();
    }

    public void Delete(string slot)
    {
        if (Slots.Remove(slot)) Flush();
    }

    // Best effort: a medium that fails here leaves the slot where it was, and SaveStore will not write over it.
    public void SetAside(string slot)
    {
        try
        {
            if (!Slots.Remove(slot, out var text)) return;
            Slots[slot + Aside] = text;
            Flush();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    // A copy set aside is not a slot. One file has one time of writing, which says nothing about one slot: null.
    public IReadOnlyList<SlotInfo> List() =>
        [.. Slots.Where(s => !s.Key.EndsWith(Aside)).Select(s => new SlotInfo(s.Key, s.Value.Length, null))];

    // Read once, on first use. A file that cannot be read throws IOException: the store then reports "could not load".
    private Dictionary<string, string> Slots
    {
        get
        {
            if (_slots is not null) return _slots;
            var slots = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                try
                {
                    using var file = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
                    foreach (var slot in file.RootElement.EnumerateObject()) slots[slot.Name] = slot.Value.GetString() ?? "";
                }
                catch (Exception e) when (e is System.Text.Json.JsonException or InvalidOperationException)
                {
                    throw new IOException($"The save file '{path}' is not one this storage wrote.", e);
                }
            }
            return _slots = slots;
        }
    }

    // The whole file, written beside it and moved over it, so a crash never leaves half a file.
    private void Flush()
    {
        using var stream = new MemoryStream();
        using (var json = new System.Text.Json.Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            foreach (var (slot, text) in _slots!) json.WriteString(slot, text);
            json.WriteEndObject();
        }
        if (Path.GetDirectoryName(path) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
        File.WriteAllBytes(path + ".tmp", stream.ToArray());
        File.Move(path + ".tmp", path, overwrite: true);
    }
}
```

The example keeps every slot in one file: `new SaveStore(new SingleFileSaveStorage(path))`. For saves in memory the
SDK has `MemorySaveStorage`, which is also where a scripted run and a test keep theirs
([Scripted runs and tests](persistence.md#scripted-runs-and-tests)).

`SaveStore` keeps its guarantees on top of any storage: it serializes, versions and migrates, sets aside data it
cannot parse (`SetAside`), never overwrites a slot it could not read or use, and turns refused writes into `false`. A
storage only reports failures as `IOException` or `UnauthorizedAccessException`, and lists the slots it holds
(`List`) with what it knows about each: a size, and the time of the last write or `null`. Install it for the game
from the composition: the game's `Configure` entry point sets `host.Storage = saveId => new MyStorage(saveId)` and
`Game.Saves` is a store on it
([One composition for every platform](backends.md#one-composition-for-every-platform)). Or use it for one store
built by hand, `new SaveStore(storage)`. There is no process-wide setting.

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

## An audio effect

An effect processes the mix of a bus in place, one block of samples at a time. The built-in ones
([Buses and effects](audio.md#buses-and-effects)) are written against the same class.

<!-- code: samples/Examples/Extending.cs#audio-effect -->
```csharp
/// <summary>
/// An audio effect of your own: the volume of a bus wobbles <see cref="Rate"/> times a second. Added to a bus like the
/// built-in ones: <c>Audio.Bus("sfx").Add(new Tremolo { Rate = 6 })</c>.
/// </summary>
public sealed class Tremolo : AudioEffect
{
    // The audio thread's own copies: it never reads what the game's thread writes.
    private float _rate = 5, _depth = 0.5f, _phase, _step;
    private int _channels = 2;

    public Tremolo() : base(parameters: 2)
    {
        SetParameter(0, 5);
        SetParameter(1, 0.5f);
    }

    /// <summary>Wobbles per second.</summary>
    public float Rate { get => GetParameter(0); set => SetParameter(0, Math.Clamp(value, 0.1f, 40)); }
    /// <summary>How far the volume drops, 0 (not at all) to 1 (to silence).</summary>
    public float Depth { get => GetParameter(1); set => SetParameter(1, Math.Clamp(value, 0, 1)); }

    // The game's thread, once, when the effect is added to a bus: the place to allocate.
    protected override void Prepare(int sampleRate, int channels) => (_step, _channels) = (MathF.Tau / sampleRate, channels);

    // The audio thread, at the exact moment in the sound of the update that set the property.
    protected override void OnParameter(int index, float value)
    {
        if (index == 0) _rate = value;
        else _depth = value;
    }

    protected override void Reset() => _phase = 0;

    // The audio thread: interleaved samples, changed in place. No allocation, no lock, no exception.
    protected override void Process(Span<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i += _channels)
        {
            float gain = 1 - _depth * (0.5f - 0.5f * MathF.Cos(_phase));
            _phase += _step * _rate;
            if (_phase >= MathF.Tau) _phase -= MathF.Tau;
            for (int c = 0; c < _channels; c++) buffer[i + c] *= gain;
        }
    }
}
```

Two threads touch an effect, and the base class keeps them apart. The game's thread creates it and sets its
properties; `Process` runs on the thread that renders audio (in a browser, the game's own thread between frames). A
property setter calls `SetParameter`, which keeps the value for the getter and sends it to the audio thread, where
`OnParameter` receives it at the moment in the sound of the update that set it. An effect therefore keeps two sets of
fields: the ones behind its properties and the ones `Process` reads.

| Member | Called on | For |
| --- | --- | --- |
| `Prepare(sampleRate, channels)` | the game's thread, once in the effect's life, the first time it is added to a bus of a device that plays | allocating delay lines and tables |
| `OnParameter(index, value)` | the audio thread | every parameter once when the effect enters a chain, then each change |
| `Reset()` | the audio thread | forgetting what it heard: entering a chain, leaving `Bypass` |
| `Process(buffer)` | the audio thread | interleaved samples (`buffer.Length / channels` frames), any block length from one frame |
| `Process(buffer, sidechain)` | the audio thread | the same, with the mix of the bus named by `SidechainBus` |

`Process` must not allocate, lock, wait or throw. An effect that throws is taken out of the mix for good: `Faulted`
turns true, `Error` holds the exception, and the bus goes on without it. An effect belongs to one bus at a time; it
can be removed and added again, or moved to another bus, and starts clean (`Reset`) each time. `Process` never sets
the effect's own properties. Keep
`Process` independent of how its blocks are cut (count frames yourself rather than blocks), and the same game run
gives the same samples on every platform.

## A platform

A platform is an `IPlatform`: time, events, a renderer per frame, the graphics, audio and input devices, screenshots,
fullscreen and suspension. Writing one for a new library is a project of its own ([Writing a new
backend](backends.md#writing-a-new-backend)); building on an existing one is a few lines. This profile runs another
platform and reports it as it starts, selectable from the command line:

<!-- code: samples/Examples/Extending.cs#platform -->
```csharp
/// <summary>
/// A platform of your own does not have to start from nothing: a profile can build on another one. This one runs any
/// platform with the request it is given and reports, as it starts, which platform the launcher got and whether it has
/// sound (telemetry or a host's diagnostics would hook in the same place). The platform it returns is the original
/// one, so every feature it has stays available.
/// </summary>
public static class ReportingPlatform
{
    /// <summary>A profile for the launcher's catalog: "--platform sdl-reported" runs SDL and says so.</summary>
    public static PlatformProfile Wrap(PlatformProfile profile) => new(
        $"{profile.Name}-reported", $"{profile.Description}, reported at start",
        request =>
        {
            var platform = profile.Create(request);
            Console.WriteLine($"started {platform} ({(platform.Audio.IsAvailable ? "with" : "without")} sound)");
            return platform;
        })
    {
        CheckSupport = () => profile.UnsupportedReason,   // runs wherever the original platform runs
        Replaceable = profile.Replaceable,
    };
}
```

The game's `Program.cs` adds the profile to the desktop catalog:

<!-- code: samples/Examples/Program.cs#launch -->
```csharp title="Program.cs"
// The official desktop catalog (sdl, raylib, sdl-software, headless) plus one platform of our own:
// "--platform sdl-reported" runs SDL and reports it at start (ReportingPlatform, Extending.cs).
var catalog = DesktopLauncher.CreateCatalog().Add(ReportingPlatform.Wrap(SdlPlatform.Profile));
return DesktopLauncher.Run(new ExamplesGame(), args, catalog);
```

```text
$ microengine run --project samples/Examples --platform sdl-reported -- --frames 30
started SDL renderer direct3d11 (with sound)
```

- A `PlatformProfile` has a name (for `--platform` and `MICROENGINE_PLATFORM`), a description (for
  `--list-platforms`), a factory from `PlatformRequest` to `IPlatform`, `CheckSupport` (null when it can run here,
  otherwise the reason) and `Replaceable` (the parts a host may supply: audio, extra input).
- The `PlatformRequest` carries the game's settings and identity, window scale, hidden mode, mute, the replaced parts
  and explicit options such as `WaitForDisplay` and `StartGamepadsAfterFirstFrame`
  ([Platform request options](../reference/configuration.md#platform-request-options)); a factory passes it on.
- A class that wraps a platform implements every member of `IPlatform` and forwards it, including the host contract
  `IPlatform` extends and its capability query (`TryGetCapability<T>`), so the wrapped platform's optional features stay
  available; a profile like the one above, which returns the original platform,
  needs none of it.
- The catalog's first profile that can run is the default. `DesktopLauncher.CreateCatalog()` holds `sdl`, `raylib`,
  `sdl-software` and `headless`; a game that ships SDL only builds its own with `new PlatformCatalog()`.

## What is not an extension point

- **Hosts for phones and browsers** are built by `microengine package`; a game does not provide its own Android
  activity, iOS app or web page.
- **A renderer of your own.** A game adds rendering techniques with what `IRenderer` draws with (render targets,
  blend states, materials from its own fragment shaders: [Render targets and shaders](../guide/render-targets-and-shaders.md))
  or, below it, by recording passes on the [graphics device](rendering.md#the-graphics-device). It does not replace the
  renderer or add a graphics device: those come with the platform. Stencil, multisampling and vertex shaders of a
  material are not offered at either level.
- **The game loop** (fixed step, order of a frame) is the engine's; a host that drives frames itself uses
  `GameSession` ([Game lifecycle and time](lifecycle.md#loop-runner-and-session)). Within it, a game adds its own systems
  to the frame's phases, replaces built-in services and edits the time policy from `Game.Configure`
  ([Modules and the frame schedule](lifecycle.md#modules-and-the-frame-schedule)).

## Related

- [Backends and platforms](backends.md): the official platforms, choosing one, replacing a part, writing a backend
- [Architecture](../architecture.md): how the layers depend on each other
- [Public contracts](../reference/contracts.md): the interfaces in full
