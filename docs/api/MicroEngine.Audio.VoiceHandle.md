# Struct VoiceHandle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A sound playing in an [AudioEngine](MicroEngine.Audio.AudioEngine.md): a voice slot and the generation of the sound that took it. Once the
sound ends, is stopped or is stolen by the voice limit, the handle is stale and calls with it do nothing.
<code>default</code> is [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None), which [Play](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Play_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__) also
returns for a sound the voice policy refused.

```csharp
public readonly struct VoiceHandle : IEquatable<VoiceHandle>
```

#### Implements

[IEquatable<VoiceHandle\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Audio_VoiceHandle__ctor_System_Int32_System_UInt32_"></a> VoiceHandle\(int, uint\)

A sound playing in an [AudioEngine](MicroEngine.Audio.AudioEngine.md): a voice slot and the generation of the sound that took it. Once the
sound ends, is stopped or is stolen by the voice limit, the handle is stale and calls with it do nothing.
<code>default</code> is [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None), which [Play](MicroEngine.Audio.AudioEngine.md#MicroEngine_Audio_AudioEngine_Play_MicroEngine_Audio_AudioClip_MicroEngine_Audio_PlayParameters__) also
returns for a sound the voice policy refused.

```csharp
public VoiceHandle(int Index, uint Generation)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The voice slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the sound in the slot; 0 only for [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None).

## Properties

### <a id="MicroEngine_Audio_VoiceHandle_Generation"></a> Generation

The generation of the sound in the slot; 0 only for [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Audio_VoiceHandle_Index"></a> Index

The voice slot.

```csharp
public int Index { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_VoiceHandle_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Audio.VoiceHandle.md#MicroEngine_Audio_VoiceHandle_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_VoiceHandle_None"></a> None

No sound.

```csharp
public static VoiceHandle None { get; }
```

#### Property Value

 [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

## Methods

### <a id="MicroEngine_Audio_VoiceHandle_Deconstruct_System_Int32__System_UInt32__"></a> Deconstruct\(out int, out uint\)

```csharp
public void Deconstruct(out int Index, out uint Generation)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Audio_VoiceHandle_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_VoiceHandle_Equals_MicroEngine_Audio_VoiceHandle_"></a> Equals\(VoiceHandle\)

```csharp
public bool Equals(VoiceHandle other)
```

#### Parameters

`other` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_VoiceHandle_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_VoiceHandle_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Audio_VoiceHandle_op_Equality_MicroEngine_Audio_VoiceHandle_MicroEngine_Audio_VoiceHandle_"></a> operator ==\(VoiceHandle, VoiceHandle\)

```csharp
public static bool operator ==(VoiceHandle left, VoiceHandle right)
```

#### Parameters

`left` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`right` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Audio_VoiceHandle_op_Inequality_MicroEngine_Audio_VoiceHandle_MicroEngine_Audio_VoiceHandle_"></a> operator \!=\(VoiceHandle, VoiceHandle\)

```csharp
public static bool operator !=(VoiceHandle left, VoiceHandle right)
```

#### Parameters

`left` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

`right` [VoiceHandle](MicroEngine.Audio.VoiceHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
