# Class Delay

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio.Effects](MicroEngine.Audio.Effects.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

An echo: the bus is heard again [Time](MicroEngine.Audio.Effects.Delay.md#MicroEngine_Audio_Effects_Delay_Time) seconds later, and each repeat again, [Feedback](MicroEngine.Audio.Effects.Delay.md#MicroEngine_Audio_Effects_Delay_Feedback)
times as loud.

```csharp
public sealed class Delay : AudioEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md) ← 
[Delay](MicroEngine.Audio.Effects.Delay.md)

#### Inherited Members

[AudioEffect.Bypass](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Bypass), 
[AudioEffect.Faulted](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Faulted), 
[AudioEffect.Error](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Error), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Audio_Effects_Delay__ctor"></a> Delay\(\)

Creates an echo of 0.3 seconds.

```csharp
public Delay()
```

## Fields

### <a id="MicroEngine_Audio_Effects_Delay_MaxTime"></a> MaxTime

The longest [Time](MicroEngine.Audio.Effects.Delay.md#MicroEngine_Audio_Effects_Delay_Time), in seconds.

```csharp
public const float MaxTime = 2
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Properties

### <a id="MicroEngine_Audio_Effects_Delay_Dry"></a> Dry

Gain of the sound itself, 0..1 (1 by default).

```csharp
public float Dry { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Delay_Feedback"></a> Feedback

How loud each repeat is against the one before, 0..0.95 (0.35 by default; 0 for a single repeat).

```csharp
public float Feedback { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Delay_Time"></a> Time

Seconds between repeats, 0.001..[MaxTime](MicroEngine.Audio.Effects.Delay.md#MicroEngine_Audio_Effects_Delay_MaxTime) (0.3 by default). A change glides, bending the pitch of what is in the echo for a moment, as a tape delay does.

```csharp
public float Time { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Delay_Wet"></a> Wet

Gain of the echoes, 0..1 (0.5 by default).

```csharp
public float Wet { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_Effects_Delay_OnParameter_System_Int32_System_Single_"></a> OnParameter\(int, float\)

Rendering thread: parameter <code class="paramref">index</code> takes <code class="paramref">value</code> from the next frame
processed. Every parameter is delivered once when the effect enters a chain, then at each change.

```csharp
protected override void OnParameter(int index, float value)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Delay_Prepare_System_Int32_System_Int32_"></a> Prepare\(int, int\)

Called on the game's thread when the effect is added to a bus, before it processes anything: the place to
allocate delay lines and tables for the mixer's format.

```csharp
protected override void Prepare(int sampleRate, int channels)
```

#### Parameters

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Frames per second.

`channels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per frame, 1 or 2.

### <a id="MicroEngine_Audio_Effects_Delay_Process_System_Span_System_Single__"></a> Process\(Span<float\>\)

Rendering thread: processes <code class="paramref">buffer</code> in place. The samples are interleaved by channel
(<code>buffer.Length / channels</code> frames) and blocks follow one another without gaps, of any length from one frame.

```csharp
protected override void Process(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_Effects_Delay_Reset"></a> Reset\(\)

Rendering thread: forgets what the effect has heard (delay lines, filter memory), when it enters a chain or leaves bypass.

```csharp
protected override void Reset()
```

</div>
