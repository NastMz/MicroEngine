# Class MusicHandle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.dll  
Guide: [Audio](../manual/audio.md)  

An opaque handle to loaded, loopable music.

```csharp
public sealed class MusicHandle
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MusicHandle](MicroEngine.Audio.MusicHandle.md)

## Constructors

### <a id="MicroEngine_Audio_MusicHandle__ctor_System_Int32_System_String_"></a> MusicHandle\(int, string\)

An opaque handle to loaded, loopable music.

```csharp
public MusicHandle(int id, string name)
```

#### Parameters

`id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Audio_MusicHandle_Id"></a> Id

Identifier assigned by the audio device that loaded the music.

```csharp
public int Id { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Audio_MusicHandle_Name"></a> Name

The name the music was registered under.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
