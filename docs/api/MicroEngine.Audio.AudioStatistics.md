# Struct AudioStatistics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

What [Statistics](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Statistics) reports about the mixer's voices.

```csharp
public readonly struct AudioStatistics : IEquatable<AudioStatistics>
```

#### Implements

[IEquatable<AudioStatistics\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Audio_AudioStatistics__ctor_System_Int32_System_Int32_System_Int32_"></a> AudioStatistics\(int, int, int\)

What [Statistics](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_Statistics) reports about the mixer's voices.

```csharp
public AudioStatistics(int ActiveVoices, int MaxVoices, int RefusedSounds)
```

#### Parameters

`ActiveVoices` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Sounds playing now, music included.

`MaxVoices` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Sounds that can play at once.

`RefusedSounds` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Sounds the voice policy refused since the start because every voice played something of higher priority.

## Properties

### <a id="MicroEngine_Audio_AudioStatistics_ActiveVoices"></a> ActiveVoices

Sounds playing now, music included.

```csharp
public int ActiveVoices { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioStatistics_MaxVoices"></a> MaxVoices

Sounds that can play at once.

```csharp
public int MaxVoices { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioStatistics_RefusedSounds"></a> RefusedSounds

Sounds the voice policy refused since the start because every voice played something of higher priority.

```csharp
public int RefusedSounds { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Audio_AudioStatistics_Deconstruct_System_Int32__System_Int32__System_Int32__"></a> Deconstruct\(out int, out int, out int\)

```csharp
public void Deconstruct(out int ActiveVoices, out int MaxVoices, out int RefusedSounds)
```

#### Parameters

`ActiveVoices` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`MaxVoices` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`RefusedSounds` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioStatistics_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioStatistics_Equals_MicroEngine_Audio_AudioStatistics_"></a> Equals\(AudioStatistics\)

```csharp
public bool Equals(AudioStatistics other)
```

#### Parameters

`other` [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioStatistics_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioStatistics_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Audio_AudioStatistics_op_Equality_MicroEngine_Audio_AudioStatistics_MicroEngine_Audio_AudioStatistics_"></a> operator ==\(AudioStatistics, AudioStatistics\)

```csharp
public static bool operator ==(AudioStatistics left, AudioStatistics right)
```

#### Parameters

`left` [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

`right` [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioStatistics_op_Inequality_MicroEngine_Audio_AudioStatistics_MicroEngine_Audio_AudioStatistics_"></a> operator \!=\(AudioStatistics, AudioStatistics\)

```csharp
public static bool operator !=(AudioStatistics left, AudioStatistics right)
```

#### Parameters

`left` [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

`right` [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
