# Class BiquadFilter

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio.Effects](MicroEngine.Audio.Effects.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A second-order filter (12 dB per octave): the base of [LowPass](MicroEngine.Audio.Effects.LowPass.md) and [HighPass](MicroEngine.Audio.Effects.HighPass.md). A change of
[Cutoff](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Cutoff) or [Resonance](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Resonance) glides over a fiftieth of a second instead of clicking.

```csharp
public abstract class BiquadFilter : AudioEffect
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md) ← 
[BiquadFilter](MicroEngine.Audio.Effects.BiquadFilter.md)

#### Derived

[HighPass](MicroEngine.Audio.Effects.HighPass.md), 
[LowPass](MicroEngine.Audio.Effects.LowPass.md)

#### Inherited Members

[AudioEffect.GetParameter\(int\)](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_GetParameter\_System\_Int32\_), 
[AudioEffect.SetParameter\(int, float\)](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_SetParameter\_System\_Int32\_System\_Single\_), 
[AudioEffect.Prepare\(int, int\)](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Prepare\_System\_Int32\_System\_Int32\_), 
[AudioEffect.OnParameter\(int, float\)](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_OnParameter\_System\_Int32\_System\_Single\_), 
[AudioEffect.Reset\(\)](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Reset), 
[AudioEffect.Process\(Span<float\>\)](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Process\_System\_Span\_System\_Single\_\_), 
[AudioEffect.Process\(Span<float\>, ReadOnlySpan<float\>\)](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Process\_System\_Span\_System\_Single\_\_System\_ReadOnlySpan\_System\_Single\_\_), 
[AudioEffect.Bypass](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Bypass), 
[AudioEffect.Faulted](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Faulted), 
[AudioEffect.Error](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_Error), 
[AudioEffect.SidechainBus](MicroEngine.Audio.AudioEffect.md\#MicroEngine\_Audio\_AudioEffect\_SidechainBus), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Properties

### <a id="MicroEngine_Audio_Effects_BiquadFilter_Cutoff"></a> Cutoff

The frequency, in hertz, where the filter starts to cut (clamped to 10..20000, and below half the sample rate).

```csharp
public float Cutoff { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_BiquadFilter_Resonance"></a> Resonance

How much the filter emphasizes the cutoff frequency (its Q), 0.1..20: 0.7071 by default, which is flat; higher
values ring at the cutoff.

```csharp
public float Resonance { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_Effects_BiquadFilter_OnParameter_System_Int32_System_Single_"></a> OnParameter\(int, float\)

Rendering thread: parameter <code class="paramref">index</code> takes <code class="paramref">value</code> from the next frame
processed. Every parameter is delivered once when the effect enters a chain, then at each change.

```csharp
protected override void OnParameter(int index, float value)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Effects_BiquadFilter_Prepare_System_Int32_System_Int32_"></a> Prepare\(int, int\)

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

### <a id="MicroEngine_Audio_Effects_BiquadFilter_Process_System_Span_System_Single__"></a> Process\(Span<float\>\)

Rendering thread: processes <code class="paramref">buffer</code> in place. The samples are interleaved by channel
(<code>buffer.Length / channels</code> frames) and blocks follow one another without gaps, of any length from one frame.

```csharp
protected override void Process(Span<float> buffer)
```

#### Parameters

`buffer` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="MicroEngine_Audio_Effects_BiquadFilter_Reset"></a> Reset\(\)

Rendering thread: forgets what the effect has heard (delay lines, filter memory), when it enters a chain or leaves bypass.

```csharp
protected override void Reset()
```

</div>
