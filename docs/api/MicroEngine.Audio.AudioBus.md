# Class AudioBus

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Audio](MicroEngine.Audio.md)  
Assembly: MicroEngine.Audio.dll  
Guide: [Audio](../manual/audio.md)  

A named mix stage: the sounds and buses under it are scaled by its [Volume](MicroEngine.Audio.AudioBus.md#MicroEngine_Audio_AudioBus_Volume). Buses form a tree under
"master": a bus is created under a parent that already exists and is never moved, so there is no cycle.

```csharp
public sealed class AudioBus
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AudioBus](MicroEngine.Audio.AudioBus.md)

## Properties

### <a id="MicroEngine_Audio_AudioBus_Name"></a> Name

The bus's name.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Audio_AudioBus_Parent"></a> Parent

The bus it plays into; null for "master".

```csharp
public AudioBus? Parent { get; }
```

#### Property Value

 [AudioBus](MicroEngine.Audio.AudioBus.md)?

### <a id="MicroEngine_Audio_AudioBus_Volume"></a> Volume

Gain of everything on the bus, clamped to 0..4; it changes at the frame of the current step.

```csharp
public float Volume { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
