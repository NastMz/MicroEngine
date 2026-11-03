# Interface IAudioStream

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Streamed audio (music decoded while it plays). The engine reads it only in [Pump](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Pump), on the main
thread outside the simulation step, ahead of the sink.

```csharp
public interface IAudioStream : IDisposable
```

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Audio_IAudioStream_Channels"></a> Channels

1 or 2.

```csharp
int Channels { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_IAudioStream_SampleRate"></a> SampleRate

Frames per second.

```csharp
int SampleRate { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Audio_IAudioStream_Read_System_Span_System_Single__"></a> Read\(Span<float\>\)

Fills <code class="paramref">buffer</code> with interleaved samples; returns how many it wrote, 0 at the end.

```csharp
int Read(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_IAudioStream_Rewind"></a> Rewind\(\)

Goes back to the start (looping).

```csharp
void Rewind()
```

</div>
