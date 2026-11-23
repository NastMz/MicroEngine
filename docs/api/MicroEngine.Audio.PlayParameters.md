# Struct PlayParameters

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

How a sound plays.

```csharp
public readonly struct PlayParameters : IEquatable<PlayParameters>
```

#### Implements

[IEquatable<PlayParameters\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Audio_PlayParameters__ctor"></a> PlayParameters\(\)

Creates the defaults: full volume, original pitch, centred, once, priority 0, on the "sfx" bus.

```csharp
public PlayParameters()
```

## Properties

### <a id="MicroEngine_Audio_PlayParameters_Bus"></a> Bus

The bus the sound plays through ("sfx" by default; the engine also has "music" and "master").

```csharp
public string Bus { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Audio_PlayParameters_Delay"></a> Delay

Seconds from the current step's frame to the sound's first frame (0, the default: it starts with the step),
exact to the output frame: a sound scheduled on a beat. A value that is not a number, or negative, counts as 0.

```csharp
public double Delay { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_PlayParameters_FadeIn"></a> FadeIn

Seconds the sound takes to rise from silence to [Volume](MicroEngine.Audio.PlayParameters.md#MicroEngine_Audio_PlayParameters_Volume), frame by frame (0, the default: it starts
at its volume). A value that is not a number counts as 0.

```csharp
public float FadeIn { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_PlayParameters_Loop"></a> Loop

Whether the sound restarts when it reaches its end.

```csharp
public bool Loop { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_PlayParameters_LoopRegion"></a> LoopRegion

With [Loop](MicroEngine.Audio.PlayParameters.md#MicroEngine_Audio_PlayParameters_Loop), the part of the sound that repeats: it plays from where it starts to the region's end,
then goes back to the region's start each time (an intro played once, then a loop). Null (the default) for the
region the sound carries ([LoopRegion](MicroEngine.Audio.AudioClip.md#MicroEngine_Audio_AudioClip_LoopRegion), a stream's
[LoopRegion](MicroEngine.Audio.ISeekableAudioStream.md#MicroEngine_Audio_ISeekableAudioStream_LoopRegion)), and the whole sound when it carries none.

```csharp
public LoopRegion? LoopRegion { get; init; }
```

#### Property Value

 [LoopRegion](MicroEngine.Audio.LoopRegion.md)?

### <a id="MicroEngine_Audio_PlayParameters_Pan"></a> Pan

Stereo position, 0 (left) to 1 (right); 0.5 is centred, as is a value that is not a number. Balance panning: the
far side is attenuated.

```csharp
public float Pan { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_PlayParameters_Pitch"></a> Pitch

Playback speed factor, 1 for the original pitch (clamped to 0.01..100; a value that is not a number counts as 1).
Streams play at their own pitch.

```csharp
public float Pitch { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Audio_PlayParameters_Priority"></a> Priority

The voice policy's rank: when every voice is busy, a new sound steals the oldest sound of the lowest priority, if
that priority is not higher than its own; otherwise the new sound is refused.

```csharp
public int Priority { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_PlayParameters_StartAt"></a> StartAt

Where in the sound it starts, in seconds from its beginning (0, the default); clamped to its length. A value
that is not a number, or negative, counts as 0. A stream starts there only when it can seek
([ISeekableAudioStream](MicroEngine.Audio.ISeekableAudioStream.md)); another stream starts at its beginning.

```csharp
public double StartAt { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_PlayParameters_Volume"></a> Volume

Volume, 0..1 (clamped); a value that is not a number counts as 1.

```csharp
public float Volume { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Audio_PlayParameters_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_PlayParameters_Equals_MicroEngine_Audio_PlayParameters_"></a> Equals\(PlayParameters\)

```csharp
public bool Equals(PlayParameters other)
```

#### Parameters

`other` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_PlayParameters_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_PlayParameters_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Audio_PlayParameters_op_Equality_MicroEngine_Audio_PlayParameters_MicroEngine_Audio_PlayParameters_"></a> operator ==\(PlayParameters, PlayParameters\)

```csharp
public static bool operator ==(PlayParameters left, PlayParameters right)
```

#### Parameters

`left` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

`right` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_PlayParameters_op_Inequality_MicroEngine_Audio_PlayParameters_MicroEngine_Audio_PlayParameters_"></a> operator \!=\(PlayParameters, PlayParameters\)

```csharp
public static bool operator !=(PlayParameters left, PlayParameters right)
```

#### Parameters

`left` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

`right` [PlayParameters](MicroEngine.Audio.PlayParameters.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
