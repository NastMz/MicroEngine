# Class OfflineAudioSink

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

The test sink: renders into memory with no device and no clock. Each [Pump](MicroEngine.Audio.OfflineAudioSink.md#MicroEngine_Audio_OfflineAudioSink_Pump) renders exactly one
simulation step of frames (the step's share of the rate, so 44100 Hz at 60 steps alternates 735-frame steps exactly),
in blocks of at most the block size, so the output depends only on what the game played, never on time.

```csharp
public sealed class OfflineAudioSink : IAudioSink, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[OfflineAudioSink](MicroEngine.Audio.OfflineAudioSink.md)

#### Implements

IAudioSink, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Audio_OfflineAudioSink__ctor_MicroEngine_Platform_AudioFormat_System_Int32_System_Int32_System_Int32_"></a> OfflineAudioSink\(AudioFormat, int, int, int\)

Creates an offline sink.

```csharp
public OfflineAudioSink(AudioFormat format, int updateRate = 60, int blockFrames = 1024, int latencyFrames = 0)
```

#### Parameters

`format` AudioFormat

The format to render.

`updateRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Simulation steps per second: one pump renders one step.

`blockFrames` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The largest block asked of the mixer at once.

`latencyFrames` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The latency it reports (0: sounds start at their step's first frame).

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A count is out of range.

## Properties

### <a id="MicroEngine_Audio_OfflineAudioSink_Format"></a> Format

The format the sink plays.

```csharp
public AudioFormat Format { get; }
```

#### Property Value

 AudioFormat

### <a id="MicroEngine_Audio_OfflineAudioSink_Frames"></a> Frames

Frames rendered so far.

```csharp
public long Frames { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Audio_OfflineAudioSink_LatencyFrames"></a> LatencyFrames

Frames rendered ahead of the device (queue target plus device period): the mixer's minimum scheduling delay.

```csharp
public int LatencyFrames { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_OfflineAudioSink_Samples"></a> Samples

Everything rendered so far, interleaved.

```csharp
public IReadOnlyList<float> Samples { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_OfflineAudioSink_Schedule"></a> Schedule

[HostPump](MicroEngine.Platform.AudioSinkSchedule.md): rendering happens in [Pump](MicroEngine.Audio.OfflineAudioSink.md#MicroEngine_Audio_OfflineAudioSink_Pump).

```csharp
public AudioSinkSchedule Schedule { get; }
```

#### Property Value

 AudioSinkSchedule

## Methods

### <a id="MicroEngine_Audio_OfflineAudioSink_Dispose"></a> Dispose\(\)

Stops rendering.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Audio_OfflineAudioSink_Pump"></a> Pump\(\)

Renders the next simulation step of frames.

```csharp
public void Pump()
```

### <a id="MicroEngine_Audio_OfflineAudioSink_Start_MicroEngine_Platform_AudioRender_"></a> Start\(AudioRender\)

Starts calling <code class="paramref">render</code>. Page-driven hosts call it inside the user gesture that starts the game.

```csharp
public void Start(AudioRender render)
```

#### Parameters

`render` AudioRender

</div>
