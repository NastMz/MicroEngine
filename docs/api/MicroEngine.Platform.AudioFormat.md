# Struct AudioFormat

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

The PCM format a sink plays: interleaved 32-bit float samples, [Channels](MicroEngine.Platform.AudioFormat.md#MicroEngine_Platform_AudioFormat_Channels) per frame, at
[SampleRate](MicroEngine.Platform.AudioFormat.md#MicroEngine_Platform_AudioFormat_SampleRate) frames per second.

```csharp
public readonly struct AudioFormat : IEquatable<AudioFormat>
```

#### Implements

[IEquatable<AudioFormat\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Platform_AudioFormat__ctor_System_Int32_System_Int32_"></a> AudioFormat\(int, int\)

The PCM format a sink plays: interleaved 32-bit float samples, [Channels](MicroEngine.Platform.AudioFormat.md#MicroEngine_Platform_AudioFormat_Channels) per frame, at
[SampleRate](MicroEngine.Platform.AudioFormat.md#MicroEngine_Platform_AudioFormat_SampleRate) frames per second.

```csharp
public AudioFormat(int SampleRate, int Channels)
```

#### Parameters

`SampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Frames per second.

`Channels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Samples per frame (2 for stereo).

## Properties

### <a id="MicroEngine_Platform_AudioFormat_Channels"></a> Channels

Samples per frame (2 for stereo).

```csharp
public int Channels { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Platform_AudioFormat_SampleRate"></a> SampleRate

Frames per second.

```csharp
public int SampleRate { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Platform_AudioFormat_Deconstruct_System_Int32__System_Int32__"></a> Deconstruct\(out int, out int\)

```csharp
public void Deconstruct(out int SampleRate, out int Channels)
```

#### Parameters

`SampleRate` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Channels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Platform_AudioFormat_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_AudioFormat_Equals_MicroEngine_Platform_AudioFormat_"></a> Equals\(AudioFormat\)

```csharp
public bool Equals(AudioFormat other)
```

#### Parameters

`other` [AudioFormat](MicroEngine.Platform.AudioFormat.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_AudioFormat_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Platform_AudioFormat_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Platform_AudioFormat_op_Equality_MicroEngine_Platform_AudioFormat_MicroEngine_Platform_AudioFormat_"></a> operator ==\(AudioFormat, AudioFormat\)

```csharp
public static bool operator ==(AudioFormat left, AudioFormat right)
```

#### Parameters

`left` [AudioFormat](MicroEngine.Platform.AudioFormat.md)

`right` [AudioFormat](MicroEngine.Platform.AudioFormat.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_AudioFormat_op_Inequality_MicroEngine_Platform_AudioFormat_MicroEngine_Platform_AudioFormat_"></a> operator \!=\(AudioFormat, AudioFormat\)

```csharp
public static bool operator !=(AudioFormat left, AudioFormat right)
```

#### Parameters

`left` [AudioFormat](MicroEngine.Platform.AudioFormat.md)

`right` [AudioFormat](MicroEngine.Platform.AudioFormat.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
