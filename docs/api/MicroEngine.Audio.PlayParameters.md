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

### <a id="MicroEngine_Audio_PlayParameters_Loop"></a> Loop

Whether the sound restarts when it reaches its end.

```csharp
public bool Loop { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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
