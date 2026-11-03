# Interface IAudioSink

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

A PCM audio output, version 1 of the contract: the engine's one mixer renders, the sink plays. The sink chooses the
thread that renders ([Schedule](MicroEngine.Platform.IAudioSink.md#MicroEngine_Platform_IAudioSink_Schedule)).

```csharp
public interface IAudioSink : IDisposable
```

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

<p>Threading rules, part of the contract:</p>
<ol><li>[AudioRender](MicroEngine.Platform.AudioRender.md) runs on exactly one thread at a time, the sink's choice (its own thread, the
browser's main thread, a test thread); the mixer is written once, thread-agnostic.</li><li>Game code reaches the mixer only through a single-producer, single-consumer command queue; rendering never
takes a lock shared with the main thread.</li><li>Rendering never allocates and never touches files: sounds and music are decoded from memory already loaded,
and streamed music is decoded ahead outside rendering.</li><li>The host calls [Pump](MicroEngine.Platform.IAudioSink.md#MicroEngine_Platform_IAudioSink_Pump) once per frame after the simulation steps, on every host; it costs nothing on
[SinkThread](MicroEngine.Platform.AudioSinkSchedule.md) sinks, so the loop does not branch on the platform.</li><li>`Dispose` returns only after the sink has stopped calling the render function.</li><li>Page-driven hosts start the sink inside the user gesture that starts the game, and suspend audio while the
page is hidden.</li></ol>
<p>
Determinism rule for the engine mixer: a sound starts at an exact output frame, its simulation step plus a fixed
delay of at least [LatencyFrames](MicroEngine.Platform.IAudioSink.md#MicroEngine_Platform_IAudioSink_LatencyFrames), so the output does not depend on the block sizes a sink asks for.
</p>

## Properties

### <a id="MicroEngine_Platform_IAudioSink_Format"></a> Format

The format the sink plays.

```csharp
AudioFormat Format { get; }
```

#### Property Value

 [AudioFormat](MicroEngine.Platform.AudioFormat.md)

### <a id="MicroEngine_Platform_IAudioSink_LatencyFrames"></a> LatencyFrames

Frames rendered ahead of the device (queue target plus device period): the mixer's minimum scheduling delay.

```csharp
int LatencyFrames { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Platform_IAudioSink_Schedule"></a> Schedule

Who calls the render function.

```csharp
AudioSinkSchedule Schedule { get; }
```

#### Property Value

 [AudioSinkSchedule](MicroEngine.Platform.AudioSinkSchedule.md)

## Methods

### <a id="MicroEngine_Platform_IAudioSink_Pump"></a> Pump\(\)

[HostPump](MicroEngine.Platform.AudioSinkSchedule.md) sinks render until their queue holds its target;
[SinkThread](MicroEngine.Platform.AudioSinkSchedule.md) sinks do nothing. Main thread, once per host frame, after the steps.

```csharp
void Pump()
```

### <a id="MicroEngine_Platform_IAudioSink_Start_MicroEngine_Platform_AudioRender_"></a> Start\(AudioRender\)

Starts calling <code class="paramref">render</code>. Page-driven hosts call it inside the user gesture that starts the game.

```csharp
void Start(AudioRender render)
```

#### Parameters

`render` [AudioRender](MicroEngine.Platform.AudioRender.md)

</div>
