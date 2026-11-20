# Class LowPass

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio.Effects](MicroEngine.Audio.Effects.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Lets through what is below [Cutoff](MicroEngine.Audio.Effects.BiquadFilter.md#MicroEngine_Audio_Effects_BiquadFilter_Cutoff) and cuts what is above: a muffled sound (music behind
a pause menu, under water, behind a wall).

```csharp
public sealed class LowPass : BiquadFilter
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEffect](MicroEngine.Audio.AudioEffect.md) ← 
[BiquadFilter](MicroEngine.Audio.Effects.BiquadFilter.md) ← 
[LowPass](MicroEngine.Audio.Effects.LowPass.md)

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

## Examples

```csharp
var muffle = new LowPass { Cutoff = 800, Bypass = true };
Audio.Bus("music").Add(muffle);
// When the game pauses and resumes:
muffle.Bypass = !paused;
```

## Constructors

### <a id="MicroEngine_Audio_Effects_LowPass__ctor"></a> LowPass\(\)

Creates a low-pass filter at 1000 Hz.

```csharp
public LowPass()
```

</div>
