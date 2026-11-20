# Class Limiter

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio.Effects](MicroEngine.Audio.Effects.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Keeps a mix within a ceiling: when a frame would go over [Threshold](MicroEngine.Audio.Effects.Limiter.md#MicroEngine_Audio_Effects_Limiter_Threshold), the gain drops at once to what
keeps it there, then recovers over [Release](MicroEngine.Audio.Effects.Limiter.md#MicroEngine_Audio_Effects_Limiter_Release). It looks at nothing ahead, so it adds no delay; and while
the mix stays within the threshold (and the gain has recovered) every sample passes exactly as it is.

```csharp
public sealed class Limiter : AudioEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md) ← 
[Limiter](MicroEngine.Audio.Effects.Limiter.md)

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

## Remarks

The mixer has one after "master" ([Limiter](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Limiter)), so a loud moment is turned down instead of
clipped by the device. On a bus of its own it tames one group of sounds.

## Constructors

### <a id="MicroEngine_Audio_Effects_Limiter__ctor"></a> Limiter\(\)

Creates a limiter at full scale that recovers in a tenth of a second.

```csharp
public Limiter()
```

## Properties

### <a id="MicroEngine_Audio_Effects_Limiter_Release"></a> Release

Seconds the gain takes to come back after a peak, 0.001..5 (0.1 by default).

```csharp
public float Release { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Limiter_Threshold"></a> Threshold

The largest sample value let through, 0.05..4 (1, full scale, by default).

```csharp
public float Threshold { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_Effects_Limiter_OnParameter_System_Int32_System_Single_"></a> OnParameter\(int, float\)

Rendering thread: parameter <code class="paramref">index</code> takes <code class="paramref">value</code> from the next frame
processed. Every parameter is delivered once when the effect enters a chain, then at each change.

```csharp
protected override void OnParameter(int index, float value)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Limiter_Prepare_System_Int32_System_Int32_"></a> Prepare\(int, int\)

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

### <a id="MicroEngine_Audio_Effects_Limiter_Process_System_Span_System_Single__"></a> Process\(Span<float\>\)

Rendering thread: processes <code class="paramref">buffer</code> in place. The samples are interleaved by channel
(<code>buffer.Length / channels</code> frames) and blocks follow one another without gaps, of any length from one frame.

```csharp
protected override void Process(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_Effects_Limiter_Reset"></a> Reset\(\)

Rendering thread: forgets what the effect has heard (delay lines, filter memory), when it enters a chain or leaves bypass.

```csharp
protected override void Reset()
```

</div>
