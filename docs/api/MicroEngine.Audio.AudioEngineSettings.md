# Class AudioEngineSettings

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

Settings of an [AudioEngine](MicroEngine.Audio.AudioEngine.md).

```csharp
public sealed record AudioEngineSettings : IEquatable<AudioEngineSettings>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)

#### Implements

[IEquatable<AudioEngineSettings\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Audio_AudioEngineSettings__ctor"></a> AudioEngineSettings\(\)

```csharp
public AudioEngineSettings()
```

## Properties

### <a id="MicroEngine_Audio_AudioEngineSettings_DelayFrames"></a> DelayFrames

Frames between a step and the sounds it starts; raised to the sink's [LatencyFrames](MicroEngine.Platform.IAudioSink.md#MicroEngine_Platform_IAudioSink_LatencyFrames), so
every sound starts at an exact frame whatever block sizes the sink renders. Null for the sink's latency.

```csharp
public int? DelayFrames { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Audio_AudioEngineSettings_MaxVoices"></a> MaxVoices

Sounds that can play at once (32 by default); the voice policy decides beyond that.

```csharp
public int MaxVoices { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngineSettings_StreamAheadSeconds"></a> StreamAheadSeconds

Output frames of streamed music decoded ahead (by [Pump](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Pump)); half a second by default.

```csharp
public double StreamAheadSeconds { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Audio_AudioEngineSettings_UpdateRate"></a> UpdateRate

Simulation steps per second: the engine stamps each sound with the output frame of its step.

```csharp
public int UpdateRate { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Audio_AudioEngineSettings__Clone__"></a> <Clone\>$\(\)

```csharp
public AudioEngineSettings <Clone>$()
```

#### Returns

 [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)

### <a id="MicroEngine_Audio_AudioEngineSettings_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEngineSettings_Equals_MicroEngine_Audio_AudioEngineSettings_"></a> Equals\(AudioEngineSettings?\)

```csharp
public bool Equals(AudioEngineSettings? other)
```

#### Parameters

`other` [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEngineSettings_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_AudioEngineSettings_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Audio_AudioEngineSettings_op_Equality_MicroEngine_Audio_AudioEngineSettings_MicroEngine_Audio_AudioEngineSettings_"></a> operator ==\(AudioEngineSettings?, AudioEngineSettings?\)

```csharp
public static bool operator ==(AudioEngineSettings? left, AudioEngineSettings? right)
```

#### Parameters

`left` [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)?

`right` [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_AudioEngineSettings_op_Inequality_MicroEngine_Audio_AudioEngineSettings_MicroEngine_Audio_AudioEngineSettings_"></a> operator \!=\(AudioEngineSettings?, AudioEngineSettings?\)

```csharp
public static bool operator !=(AudioEngineSettings? left, AudioEngineSettings? right)
```

#### Parameters

`left` [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)?

`right` [AudioEngineSettings](MicroEngine.Audio.AudioEngineSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
