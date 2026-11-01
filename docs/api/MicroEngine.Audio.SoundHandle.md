# Class SoundHandle

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

An opaque handle to a loaded sound effect.

```csharp
public sealed class SoundHandle
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SoundHandle](MicroEngine.Audio.SoundHandle.md)

## Constructors

### <a id="MicroEngine_Audio_SoundHandle__ctor_System_Int32_System_String_"></a> SoundHandle\(int, string\)

An opaque handle to a loaded sound effect.

```csharp
public SoundHandle(int id, string name)
```

#### Parameters

`id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Audio_SoundHandle_Id"></a> Id

Identifier assigned by the audio device that loaded the sound.

```csharp
public int Id { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_SoundHandle_Name"></a> Name

The name the sound was registered under.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)
