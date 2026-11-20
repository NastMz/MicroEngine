# Class Reverb

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio.Effects](MicroEngine.Audio.Effects.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A room: the bus is heard with the reflections of a space around it, from a small room to a hall
([RoomSize](MicroEngine.Audio.Effects.Reverb.md#MicroEngine_Audio_Effects_Reverb_RoomSize)). The design is Freeverb's (Jezar at Dreampoint, public domain): eight comb filters and four
all-pass filters for each side.

```csharp
public sealed class Reverb : AudioEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md) ← 
[Reverb](MicroEngine.Audio.Effects.Reverb.md)

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

### <a id="MicroEngine_Audio_Effects_Reverb__ctor"></a> Reverb\(\)

Creates a medium room, mixed in at 0.3.

```csharp
public Reverb()
```

## Properties

### <a id="MicroEngine_Audio_Effects_Reverb_Damping"></a> Damping

How fast the high frequencies die in the space, 0 (bright, hard walls) to 1 (dull, soft walls); 0.5 by default.

```csharp
public float Damping { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Reverb_Dry"></a> Dry

Gain of the sound itself, 0..1 (1 by default).

```csharp
public float Dry { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Reverb_RoomSize"></a> RoomSize

How large the space sounds, 0 (a small room) to 1 (a hall that rings for many seconds); 0.5 by default.

```csharp
public float RoomSize { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Reverb_Wet"></a> Wet

Gain of the reflections, 0..1 (0.3 by default).

```csharp
public float Wet { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Reverb_Width"></a> Width

How wide the reflections are in stereo, 0 (the same on both sides) to 1 (default).

```csharp
public float Width { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_Effects_Reverb_OnParameter_System_Int32_System_Single_"></a> OnParameter\(int, float\)

Rendering thread: parameter <code class="paramref">index</code> takes <code class="paramref">value</code> from the next frame
processed. Every parameter is delivered once when the effect enters a chain, then at each change.

```csharp
protected override void OnParameter(int index, float value)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Reverb_Prepare_System_Int32_System_Int32_"></a> Prepare\(int, int\)

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

### <a id="MicroEngine_Audio_Effects_Reverb_Process_System_Span_System_Single__"></a> Process\(Span<float\>\)

Rendering thread: processes <code class="paramref">buffer</code> in place. The samples are interleaved by channel
(<code>buffer.Length / channels</code> frames) and blocks follow one another without gaps, of any length from one frame.

```csharp
protected override void Process(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_Effects_Reverb_Reset"></a> Reset\(\)

Rendering thread: forgets what the effect has heard (delay lines, filter memory), when it enters a chain or leaves bypass.

```csharp
protected override void Reset()
```

</div>
