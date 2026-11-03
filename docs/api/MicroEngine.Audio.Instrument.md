# Class Instrument

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A tracker-style voice: waveform plus an ADSR envelope (seconds, sustain level 0..1).

```csharp
public sealed record Instrument : IEquatable<Instrument>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Instrument](MicroEngine.Audio.Instrument.md)

#### Implements

[IEquatable<Instrument\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Audio_Instrument__ctor_MicroEngine_Audio_Wave_System_Single_System_Single_System_Single_System_Single_System_Single_System_Single_System_Single_System_Single_System_Single_"></a> Instrument\(Wave, float, float, float, float, float, float, float, float, float\)

A tracker-style voice: waveform plus an ADSR envelope (seconds, sustain level 0..1).

```csharp
public Instrument(Wave Wave, float Volume = 0.3, float Attack = 0.005, float Decay = 0.08, float SustainLevel = 0.6, float Release = 0.06, float Pan = 0.5, float VibratoDepth = 0, float VibratoRate = 5, float LowPass = 0)
```

#### Parameters

`Wave` [Wave](MicroEngine.Audio.Wave.md)

`Volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Attack` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Decay` [float](https://learn.microsoft.com/dotnet/api/system.single)

`SustainLevel` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Release` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

`VibratoDepth` [float](https://learn.microsoft.com/dotnet/api/system.single)

`VibratoRate` [float](https://learn.microsoft.com/dotnet/api/system.single)

`LowPass` [float](https://learn.microsoft.com/dotnet/api/system.single)

## Fields

### <a id="MicroEngine_Audio_Instrument_Bass"></a> Bass

A round triangle wave, for bass lines.

```csharp
public static readonly Instrument Bass
```

#### Field Value

 [Instrument](MicroEngine.Audio.Instrument.md)

### <a id="MicroEngine_Audio_Instrument_Bell"></a> Bell

A sine with a long decay, for chimes.

```csharp
public static readonly Instrument Bell
```

#### Field Value

 [Instrument](MicroEngine.Audio.Instrument.md)

### <a id="MicroEngine_Audio_Instrument_Lead"></a> Lead

A bright 25% pulse with light vibrato, for melodies.

```csharp
public static readonly Instrument Lead
```

#### Field Value

 [Instrument](MicroEngine.Audio.Instrument.md)

### <a id="MicroEngine_Audio_Instrument_Pad"></a> Pad

A soft, filtered saw with slow attack and release, for chords.

```csharp
public static readonly Instrument Pad
```

#### Field Value

 [Instrument](MicroEngine.Audio.Instrument.md)

### <a id="MicroEngine_Audio_Instrument_Pluck"></a> Pluck

A short, filtered square, for arpeggios and accompaniment.

```csharp
public static readonly Instrument Pluck
```

#### Field Value

 [Instrument](MicroEngine.Audio.Instrument.md)

## Properties

### <a id="MicroEngine_Audio_Instrument_Attack"></a> Attack

```csharp
public float Attack { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_Decay"></a> Decay

```csharp
public float Decay { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_LowPass"></a> LowPass

```csharp
public float LowPass { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_Pan"></a> Pan

```csharp
public float Pan { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_Release"></a> Release

```csharp
public float Release { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_SustainLevel"></a> SustainLevel

```csharp
public float SustainLevel { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_VibratoDepth"></a> VibratoDepth

```csharp
public float VibratoDepth { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_VibratoRate"></a> VibratoRate

```csharp
public float VibratoRate { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_Volume"></a> Volume

```csharp
public float Volume { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_Wave"></a> Wave

```csharp
public Wave Wave { get; init; }
```

#### Property Value

 [Wave](MicroEngine.Audio.Wave.md)

## Methods

### <a id="MicroEngine_Audio_Instrument__Clone__"></a> <Clone\>$\(\)

```csharp
public Instrument <Clone>$()
```

#### Returns

 [Instrument](MicroEngine.Audio.Instrument.md)

### <a id="MicroEngine_Audio_Instrument_Deconstruct_MicroEngine_Audio_Wave__System_Single__System_Single__System_Single__System_Single__System_Single__System_Single__System_Single__System_Single__System_Single__"></a> Deconstruct\(out Wave, out float, out float, out float, out float, out float, out float, out float, out float, out float\)

```csharp
public void Deconstruct(out Wave Wave, out float Volume, out float Attack, out float Decay, out float SustainLevel, out float Release, out float Pan, out float VibratoDepth, out float VibratoRate, out float LowPass)
```

#### Parameters

`Wave` [Wave](MicroEngine.Audio.Wave.md)

`Volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Attack` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Decay` [float](https://learn.microsoft.com/dotnet/api/system.single)

`SustainLevel` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Release` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Pan` [float](https://learn.microsoft.com/dotnet/api/system.single)

`VibratoDepth` [float](https://learn.microsoft.com/dotnet/api/system.single)

`VibratoRate` [float](https://learn.microsoft.com/dotnet/api/system.single)

`LowPass` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_Instrument_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_Instrument_Equals_MicroEngine_Audio_Instrument_"></a> Equals\(Instrument?\)

```csharp
public bool Equals(Instrument? other)
```

#### Parameters

`other` [Instrument](MicroEngine.Audio.Instrument.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_Instrument_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_Instrument_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Audio_Instrument_op_Equality_MicroEngine_Audio_Instrument_MicroEngine_Audio_Instrument_"></a> operator ==\(Instrument?, Instrument?\)

```csharp
public static bool operator ==(Instrument? left, Instrument? right)
```

#### Parameters

`left` [Instrument](MicroEngine.Audio.Instrument.md)?

`right` [Instrument](MicroEngine.Audio.Instrument.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_Instrument_op_Inequality_MicroEngine_Audio_Instrument_MicroEngine_Audio_Instrument_"></a> operator \!=\(Instrument?, Instrument?\)

```csharp
public static bool operator !=(Instrument? left, Instrument? right)
```

#### Parameters

`left` [Instrument](MicroEngine.Audio.Instrument.md)?

`right` [Instrument](MicroEngine.Audio.Instrument.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
