# Class SfxParams

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

Parameters for a retro sound effect (in the spirit of sfxr). Every game sound in the
samples is described with one of these, so audio is original, tweakable and license-free.

```csharp
public sealed record SfxParams : IEquatable<SfxParams>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SfxParams](MicroEngine.Audio.SfxParams.md)

#### Implements

[IEquatable<SfxParams\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Audio_SfxParams__ctor"></a> SfxParams\(\)

```csharp
public SfxParams()
```

## Properties

### <a id="MicroEngine_Audio_SfxParams_Arpeggio"></a> Arpeggio

Optional arpeggio: frequency multipliers stepped every [ArpeggioStep](MicroEngine.Audio.SfxParams.md#MicroEngine_Audio_SfxParams_ArpeggioStep) seconds.

```csharp
public float[]? Arpeggio { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]?

### <a id="MicroEngine_Audio_SfxParams_ArpeggioStep"></a> ArpeggioStep

Seconds each [Arpeggio](MicroEngine.Audio.SfxParams.md#MicroEngine_Audio_SfxParams_Arpeggio) multiplier lasts (default 0.05).

```csharp
public float ArpeggioStep { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_Attack"></a> Attack

Seconds to rise from silence to full volume.

```csharp
public float Attack { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_Crush"></a> Crush

Bit-crush to N levels (0 disables) for crunchy hits.

```csharp
public int Crush { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_SfxParams_Decay"></a> Decay

Seconds to fade to silence after the sustain. The sound lasts <code>Attack + Sustain + Decay</code> seconds.

```csharp
public float Decay { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_EndFrequency"></a> EndFrequency

Frequency at the end of the sound (exponential slide). Null keeps it constant.

```csharp
public float? EndFrequency { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Audio_SfxParams_Frequency"></a> Frequency

Start frequency in Hz.

```csharp
public float Frequency { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_LowPass"></a> LowPass

Simple one-pole low-pass in Hz (0 disables). Softens harsh square/noise sounds.

```csharp
public float LowPass { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_Sustain"></a> Sustain

Seconds held at full volume after the attack.

```csharp
public float Sustain { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_VibratoDepth"></a> VibratoDepth

Vibrato depth as a fraction of the frequency (0 disables; 0.02 is a light wobble).

```csharp
public float VibratoDepth { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_VibratoRate"></a> VibratoRate

Vibrato speed in Hz (default 8).

```csharp
public float VibratoRate { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_Volume"></a> Volume

Peak amplitude, 0..1 (default 0.5).

```csharp
public float Volume { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_SfxParams_Wave"></a> Wave

The oscillator waveform (square by default).

```csharp
public Wave Wave { get; init; }
```

#### Property Value

 [Wave](MicroEngine.Audio.Wave.md)

## Methods

### <a id="MicroEngine_Audio_SfxParams__Clone__"></a> <Clone\>$\(\)

```csharp
public SfxParams <Clone>$()
```

#### Returns

 [SfxParams](MicroEngine.Audio.SfxParams.md)

### <a id="MicroEngine_Audio_SfxParams_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_SfxParams_Equals_MicroEngine_Audio_SfxParams_"></a> Equals\(SfxParams?\)

```csharp
public bool Equals(SfxParams? other)
```

#### Parameters

`other` [SfxParams](MicroEngine.Audio.SfxParams.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_SfxParams_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_SfxParams_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Audio_SfxParams_op_Equality_MicroEngine_Audio_SfxParams_MicroEngine_Audio_SfxParams_"></a> operator ==\(SfxParams?, SfxParams?\)

```csharp
public static bool operator ==(SfxParams? left, SfxParams? right)
```

#### Parameters

`left` [SfxParams](MicroEngine.Audio.SfxParams.md)?

`right` [SfxParams](MicroEngine.Audio.SfxParams.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_SfxParams_op_Inequality_MicroEngine_Audio_SfxParams_MicroEngine_Audio_SfxParams_"></a> operator \!=\(SfxParams?, SfxParams?\)

```csharp
public static bool operator !=(SfxParams? left, SfxParams? right)
```

#### Parameters

`left` [SfxParams](MicroEngine.Audio.SfxParams.md)?

`right` [SfxParams](MicroEngine.Audio.SfxParams.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
