# Class Compressor

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio.Effects](MicroEngine.Audio.Effects.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Turns a bus down while it, or another bus, is loud. Over [Threshold](MicroEngine.Audio.Effects.Compressor.md#MicroEngine_Audio_Effects_Compressor_Threshold), each decibel of level gives only
1/[Ratio](MicroEngine.Audio.Effects.Compressor.md#MicroEngine_Audio_Effects_Compressor_Ratio) of a decibel out. With [Sidechain](MicroEngine.Audio.Effects.Compressor.md#MicroEngine_Audio_Effects_Compressor_Sidechain) naming another bus, it is that bus's level that
counts: music turned down while a voice line plays (ducking).

```csharp
public sealed class Compressor : AudioEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md) ← 
[Compressor](MicroEngine.Audio.Effects.Compressor.md)

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

## Examples

```csharp
Audio.AddBus("voice");
Audio.Bus("music").Add(Compressor.Duck("voice"));   // the music drops 12 dB under every voice line
```

## Constructors

### <a id="MicroEngine_Audio_Effects_Compressor__ctor"></a> Compressor\(\)

Creates a compressor: threshold -18 dB, ratio 4, attack 10 ms, release 200 ms.

```csharp
public Compressor()
```

## Properties

### <a id="MicroEngine_Audio_Effects_Compressor_Attack"></a> Attack

Seconds to turn down when the level rises, nine tenths of the way, 0.0001..2 (0.01 by default).

```csharp
public float Attack { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Compressor_Makeup"></a> Makeup

Decibels added after compressing, -24..24 (0 by default).

```csharp
public float Makeup { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Compressor_Range"></a> Range

The most the bus is ever turned down, in decibels, 0..60 (60 by default: no limit that matters).

```csharp
public float Range { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Compressor_Ratio"></a> Ratio

Decibels of level over the threshold for one decibel out, 1..100 (4 by default; 1 changes nothing).

```csharp
public float Ratio { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Compressor_Release"></a> Release

Seconds to come back when the level falls, nine tenths of the way, 0.001..5 (0.2 by default).

```csharp
public float Release { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Compressor_Sidechain"></a> Sidechain

The bus whose level decides (the bus's own when null): name the bus that should be heard over this one.

```csharp
public string? Sidechain { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Set while on a bus: no such bus, or this bus plays into it.

### <a id="MicroEngine_Audio_Effects_Compressor_Threshold"></a> Threshold

The level, in decibels below full scale, over which the bus is turned down (-80..0; -18 by default).

```csharp
public float Threshold { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_Effects_Compressor_Duck_System_String_System_Single_System_Single_System_Single_"></a> Duck\(string, float, float, float\)

A compressor that ducks its bus under bus <code class="paramref">under</code>: whenever that bus sounds, this one drops by
<code class="paramref">decibels</code> within <code class="paramref">attack</code> seconds, and comes back over
<code class="paramref">release</code> seconds when it falls silent.

```csharp
public static Compressor Duck(string under, float decibels = 12, float attack = 0.05, float release = 0.4)
```

#### Parameters

`under` [string](https://learn.microsoft.com/dotnet/api/system.string)

`decibels` [float](https://learn.microsoft.com/dotnet/api/system.single)

`attack` [float](https://learn.microsoft.com/dotnet/api/system.single)

`release` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Compressor](MicroEngine.Audio.Effects.Compressor.md)

### <a id="MicroEngine_Audio_Effects_Compressor_OnParameter_System_Int32_System_Single_"></a> OnParameter\(int, float\)

Rendering thread: parameter <code class="paramref">index</code> takes <code class="paramref">value</code> from the next frame
processed. Every parameter is delivered once when the effect enters a chain, then at each change.

```csharp
protected override void OnParameter(int index, float value)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_Compressor_Prepare_System_Int32_System_Int32_"></a> Prepare\(int, int\)

Called once in the effect's life, on the game's thread, the first time it is added to a bus of a device that
plays, before the rendering thread processes anything: the place to allocate delay lines and tables for the
mixer's format. It is not called again when the effect is removed and added again, so it never runs while the
rendering thread uses the effect. On a device without sound it is not called at all.

```csharp
protected override void Prepare(int sampleRate, int channels)
```

#### Parameters

`sampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Frames per second.

`channels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per frame, 1 or 2.

### <a id="MicroEngine_Audio_Effects_Compressor_Process_System_Span_System_Single__"></a> Process\(Span<float\>\)

Rendering thread: processes <code class="paramref">buffer</code> in place. The samples are interleaved by channel
(<code>buffer.Length / channels</code> frames) and blocks follow one another without gaps, of any length from one frame.

```csharp
protected override void Process(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_Effects_Compressor_Process_System_Span_System_Single__System_ReadOnlySpan_System_Single__"></a> Process\(Span<float\>, ReadOnlySpan<float\>\)

Rendering thread, for an effect with a [SidechainBus](MicroEngine.Audio.AudioEffect.md#MicroEngine_Audio_AudioEffect_SidechainBus): processes <code class="paramref">buffer</code> in place,
with the same frames of the named bus in <code class="paramref">sidechain</code>. The default ignores them.

```csharp
protected override void Process(Span<float> buffer, ReadOnlySpan<float> sidechain)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`sidechain` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_Effects_Compressor_Reset"></a> Reset\(\)

Rendering thread: forgets what the effect has heard (delay lines, filter memory), when it enters a chain or leaves bypass.

```csharp
protected override void Reset()
```

</div>
