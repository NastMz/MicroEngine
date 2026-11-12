# Diagnosing a game

When a game stutters, a body falls through the floor, a jump never fires or a tester's copy crashes, the engine can
show you what it saw. Everything on this page is off until you ask for it and costs nothing measurable while off, so
the calls can stay in the game you ship.

| Question | Where to look |
| --- | --- |
| Which part of the frame is slow? | `Game.Diagnostics.LastFrame` and `Game.Diagnostics.Systems` |
| How many draw calls, allocations, voices? | `LastFrame.Graphics`, `LastFrame.Allocated`, `LastFrame.Audio` |
| Where are the physics shapes and contacts? | `Game.DebugDraw.Physics(world)` |
| Why did my action not fire? | `Game.InputSystem.Routed` |
| What did the engine report? | `Game.Log` (the `ILog` service) |
| Why did it crash on another machine? | the crash report the launcher writes |
| Can I watch it from outside the game? | `dotnet-counters monitor --counters MicroEngine` |

The [Diagnostics example](#an-overlay-in-a-few-lines) in `samples/Examples` puts all of it behind F3.

## Frame measurements

`Game.Diagnostics.Enabled = true` measures every frame. `LastFrame` (a `FrameStatistics`) is the last complete frame:
while your update runs, it is the previous one.

- `Events`, `Update`, `Render`, `Present` and `Total`, in milliseconds. `Update` holds the fixed steps and the
  per-frame updates; `Present` includes waiting for the display when VSync is on.
- `Steps`: the fixed steps the frame ran. Two or more mean the game is catching up.
- `Allocated`: bytes the game's thread allocated during the frame. `Gen0Collections`, `Gen1Collections` and
  `Gen2Collections` count the garbage collections during it. A steady game allocates close to nothing per frame.
- `Graphics`: the work the frame sent to the graphics device (`DrawCalls`, `Passes`, `PipelineChanges`,
  `TextureBindings`, `BytesUploaded`), and `LiveResources` at its end. On a backend whose device does not count, these
  are zeros.
- `Audio`: `ActiveVoices`, `MaxVoices` and `RefusedSounds` (sounds dropped because every voice was busy with something
  more important). `Game.Audio.Statistics` gives the same at any time.

`Game.Diagnostics.Systems` lists every system of the frame schedule with its time during the last frame: the
engine's own (`engine.scenes`, `engine.render`...) and those your `Configure` added. Sort it to find the slow one. A
system of a fixed phase adds up all the steps of the frame.

For a whole run, `--profile FILE` records every frame to a file and prints percentiles and hitches
([Measuring a game](lifecycle.md#measuring-a-game)). Diagnostics are the live view of the same measurements, and both
can run together.

## Debug drawing

`Game.DebugDraw` draws from any code, at the end of the frame, above the scenes: `Line`, `Rect`, `Circle`, `Point`
and `Text`. Items are in world space by default, under the camera of the scene on top (`Scene.WorldCamera`), or in
canvas pixels with `space: DebugSpace.Screen`. An item lasts one frame; pass `seconds` to keep it longer, which helps
with something that happens once, like a hit:

```csharp
Game.DebugDraw.Line(enemy.Position, target, Color.Red);                // this frame
Game.DebugDraw.Circle(hit.Point, 6, Color.Yellow, seconds: 1);         // a second
Game.DebugDraw.Text(new Vector2(4, 4), state.ToString(), Color.White, space: DebugSpace.Screen);
```

Calls do nothing until `Game.DebugDraw.Enabled` is set, and turning it off drops what is pending.

### The physics view

`Game.DebugDraw.Physics(world)` draws a physics world as it is now: every shape of every body in the colour of its
kind (static, kinematic, dynamic; sensors and one-way shapes in their own colours, `DebugDraw.PhysicsColors`), the
solid and one-way tiles inside the camera's view, and a normal for each contact that began during the latest step.
Call it after the world moved, every update you want to see it.

It reads the world through `IPhysicsWorld2D.ShapesOf(body, span)` and `DescriptionOf(shape)`, which your own code can
use too: they return the shapes a body has and the description each was added with. Both built-in backends declare
them (`PhysicsCapabilities.ShapeInspection`).

## Where input went

Input passes through layers before gameplay: the UI, the on-screen controls, a key-rebinding prompt, layers of your
own ([Input](input.md)). When an action does not fire, the usual cause is a layer that took the press.
`Game.InputSystem.Routed` lists the frame's events, each with the layer that took it, or `null` when it reached the
devices and your actions. It records while `InputSystem.Trace` is on, which the diagnostics turn on while they
measure.

<!-- code: samples/Examples/Examples/DiagnosticsExample.cs#input -->
```csharp
/// <summary>Keeps the last few routed events: which layer took each one, or gameplay.</summary>
private void Remember()
{
    foreach (var routed in Game.InputSystem.Routed)
    {
        if (routed.Event.Kind is not (InputEventKind.KeyDown or InputEventKind.ButtonDown or InputEventKind.PointerDown)) continue;
        var target = routed.Layer?.GetType().Name ?? "gameplay";
        _lastInput.Insert(0, $"{routed.Event.Kind} {routed.Event.Key} -> {target}");
        if (_lastInput.Count > 3) _lastInput.RemoveAt(3);
    }
}
```

The actions themselves already answer the rest: `Input.Down`, `Pressed` and `IsConsumed` (a menu that called
`Consume` took the action for the update).

## Log messages

The engine writes what it could not do through the game's log (`Game.Log`, the `ILog` service): no audio device, no
gamepads, a renderer that did not start, an asset that loaded slowly during play. By default it goes to standard
error. Register an `ILog` of your own in `Configure` (a `FileLog` you write, say) to send it to a file, the overlay or your telemetry; messages the
platform wrote before the game started are replayed into it.

```csharp
public override void Configure(EngineBuilder builder) => builder.Services.Replace<ILog>(new FileLog("game.log"));
```

## Crash reports

When a run fails, the desktop launcher writes a crash report to the game's save folder (on Windows
`%LOCALAPPDATA%\MicroEngine\<game id>`) and prints its path after the error. It holds the game and engine versions,
the system and runtime, the platform's facts (renderer, driver, display), the last measured frame and its slowest
systems when diagnostics were on, and the error with its stack trace. Ask testers for that file. `CrashReport.Describe`
and `CrashReport.Write` produce the same report from your own error handling.

## Outside the game

The engine publishes its numbers as `System.Diagnostics.Metrics` instruments of the meter `"MicroEngine"`:
`microengine.frame.time`, `.update`, `.render`, `.present`, `.allocated`, `microengine.graphics.draw_calls` and
`microengine.audio.voices`. Attaching a listener turns the measurements on, so you can watch a running game without
changing it:

```bash
dotnet-counters monitor -n MyGame --counters MicroEngine,System.Runtime
```

`System.Runtime` adds the runtime's own figures: heap size, GC pauses, thread pool. The same meter feeds
`dotnet-monitor` and OpenTelemetry exporters. For CPU profiles and allocation traces, use `dotnet-trace` or your IDE's
profiler on a desktop build; the engine does not ship a profiler of its own.

## An overlay in a few lines

The Diagnostics example turns measuring and drawing on together with one action:

<!-- code: samples/Examples/Examples/DiagnosticsExample.cs#toggle -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    if (Input.Pressed("diagnostics"))
    {
        // Measuring and drawing are separate switches; this example turns both on and off together.
        bool on = !Game.Diagnostics.Enabled;
        Game.Diagnostics.Enabled = on;
        Game.DebugDraw.Enabled = on;
    }
    Move(dt);
    // Both calls do nothing while the debug drawing is off, so they can stay in the game.
    Game.DebugDraw.Physics(_world);
    var at = _world.PositionOf(_player);
    Game.DebugDraw.Line(at, at + _velocity * 0.1f, Color.White);
    Remember();
}
```

and prints the numbers above its scene:

<!-- code: samples/Examples/Examples/DiagnosticsExample.cs#overlay -->
```csharp
private void Overlay(IRenderer r)
{
    var frame = Game.Diagnostics.LastFrame;
    var lines = new List<string>
    {
        string.Create(CultureInfo.InvariantCulture, $"FRAME {frame.Total:0.00} MS  UPDATE {frame.Update:0.00}  RENDER {frame.Render:0.00}"),
        string.Create(CultureInfo.InvariantCulture, $"{frame.Graphics.DrawCalls} DRAWS  {frame.Allocated} B  GC {frame.Collections}  VOICES {frame.Audio.ActiveVoices}"),
    };
    foreach (var system in Game.Diagnostics.Systems.OrderByDescending(s => s.Milliseconds).Take(3))
        lines.Add(string.Create(CultureInfo.InvariantCulture, $"  {system.Name} {system.Milliseconds:0.000}"));
    lines.AddRange(_lastInput);
    r.FillRect(new RectF(4, 16, 220, 8 * lines.Count + 4), Color.Black.WithAlpha(0.7f));
    for (int i = 0; i < lines.Count; i++) Font.Draw(r, lines[i].ToUpperInvariant(), new Vector2(6, 18 + 8 * i), Color.White);
}
```
