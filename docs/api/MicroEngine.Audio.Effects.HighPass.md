# Class HighPass

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio.Effects](MicroEngine.Audio.Effects.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Lets through what is above [Cutoff](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Cutoff) and cuts what is below: a thin sound (a radio, a
telephone), or a rumble removed.

```csharp
public sealed class HighPass : BiquadFilter
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md) ← 
[BiquadFilter](MicroEngine.Audio.Effects.BiquadFilter.md) ← 
[HighPass](MicroEngine.Audio.Effects.HighPass.md)

#### Inherited Members

[BiquadFilter.Cutoff](MicroEngine.Audio.Effects.BiquadFilter.md\#MicroEngine\_Audio\_Effects\_BiquadFilter\_Cutoff), 
[BiquadFilter.Resonance](MicroEngine.Audio.Effects.BiquadFilter.md\#MicroEngine\_Audio\_Effects\_BiquadFilter\_Resonance), 
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

### <a id="MicroEngine_Audio_Effects_HighPass__ctor"></a> HighPass\(\)

Creates a high-pass filter at 200 Hz.

```csharp
public HighPass()
```

</div>
