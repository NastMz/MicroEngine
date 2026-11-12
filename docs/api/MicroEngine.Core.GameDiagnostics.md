# Class GameDiagnostics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The game's live measurements ([Diagnostics](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Diagnostics)): the last frame's times, allocations, collections,
graphics work and audio voices, and the time of every system of the frame schedule. Off by default; a run measures
while [Enabled](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_Enabled) is set or while a tool listens to the engine's meter ([MeterName](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_MeterName)), and costs
nothing measurable otherwise.

```csharp
public sealed class GameDiagnostics
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameDiagnostics](MicroEngine.Core.GameDiagnostics.md)

## Examples

```csharp
// In Load: measure, and show the numbers when F3 is held.
Diagnostics.Enabled = true;
// In DrawAboveScenes:
var frame = Diagnostics.LastFrame;
Font.Draw(renderer, $"{frame.Total:0.0} ms  {frame.Graphics.DrawCalls} draws  {frame.Allocated} B", new Vector2(4, 4), Color.White);
```

## Remarks

The same numbers are published as `Metrics` instruments of the meter
<code>"MicroEngine"</code>, so <code>dotnet-counters monitor --counters MicroEngine</code>, <code>dotnet-monitor</code> or an
OpenTelemetry exporter read them without code in the game. The runtime's own meter (<code>System.Runtime</code>) adds the
heap size, GC pauses and thread-pool figures.

## Fields

### <a id="MicroEngine_Core_GameDiagnostics_MeterName"></a> MeterName

The name of the engine's meter (`Meter`): <code>"MicroEngine"</code>.

```csharp
public const string MeterName = "MicroEngine"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Core_GameDiagnostics_Active"></a> Active

Whether frames are measured now: [Enabled](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_Enabled), or a listener of the engine's meter is attached.

```csharp
public bool Active { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameDiagnostics_Enabled"></a> Enabled

Whether the game measures every frame. Off by default; a tool listening to the engine's meter measures too ([Active](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_Active)).

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameDiagnostics_LastFrame"></a> LastFrame

The last measured frame; <code>default</code> (frame 0) until a frame is measured. During an update it is the previous frame.

```csharp
public FrameStatistics LastFrame { get; }
```

#### Property Value

 [FrameStatistics](MicroEngine.Core.FrameStatistics.md)

### <a id="MicroEngine_Core_GameDiagnostics_Systems"></a> Systems

Every system of the frame schedule with its time during the last measured frame, in the order phases run and, within
a phase, in the order its systems run. Empty until a frame is measured.

```csharp
public IReadOnlyList<SystemTiming> Systems { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SystemTiming](MicroEngine.Core.SystemTiming.md)\>

</div>
