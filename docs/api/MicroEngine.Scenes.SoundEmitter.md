# Class SoundEmitter

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A sound with a place in a scene's world ([Emit](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_Emit_System_String_MicroEngine_Scenes_EntityId_System_Boolean_System_Single_)): each step
its volume follows its distance to the listener and its pan its side of the listener. Its properties can be
changed while it plays.

```csharp
public sealed class SoundEmitter
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SoundEmitter](MicroEngine.Scenes.SoundEmitter.md)

## Properties

### <a id="MicroEngine_Scenes_SoundEmitter_Bus"></a> Bus

The bus it plays through, read when its sound starts.

```csharp
public string Bus { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Scenes_SoundEmitter_Clip"></a> Clip

The clip it plays, or null when it plays a registered sound.

```csharp
public AudioClip? Clip { get; }
```

#### Property Value

 [AudioClip](MicroEngine.Audio.AudioClip.md)?

### <a id="MicroEngine_Scenes_SoundEmitter_Curve"></a> Curve

A curve of the game's own instead of [Rolloff](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_Rolloff): given how far the listener is between
[MinDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MinDistance) (0) and [MaxDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MaxDistance) (1), it returns the gain (clamped to 0..1). Null
(the default) for [Rolloff](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_Rolloff).

```csharp
public Func<float, float>? Curve { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[float](https://learn.microsoft.com/dotnet/api/system.single), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

### <a id="MicroEngine_Scenes_SoundEmitter_Entity"></a> Entity

The entity it follows, or [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None) for an emitter at a fixed [Position](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_Position). When the entity is destroyed the emitter stops.

```csharp
public EntityId Entity { get; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_SoundEmitter_Gain"></a> Gain

The gain the last step gave it for its distance, 0 to 1, before [Volume](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_Volume); 0 out of range.

```csharp
public float Gain { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SoundEmitter_IsActive"></a> IsActive

Whether the emitter is still in its scene: false once stopped, once its sound ended (when it does not loop), once its entity was destroyed or once the scene left.

```csharp
public bool IsActive { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SoundEmitter_IsAudible"></a> IsAudible

Whether a voice of the mixer is playing its sound now (false out of range, on a device without sound, and when every voice was busy with sounds of higher priority).

```csharp
public bool IsAudible { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SoundEmitter_Loop"></a> Loop

Whether the sound repeats until the emitter is stopped; a sound that does not is over when it ends.

```csharp
public bool Loop { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SoundEmitter_MaxDistance"></a> MaxDistance

From this distance on it is silent, and holds no voice (world units; the scene's [MaxDistance](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_MaxDistance) when the emitter was made).

```csharp
public float MaxDistance { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SoundEmitter_MinDistance"></a> MinDistance

Within this distance of the listener it plays at its full [Volume](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_Volume) (world units; the scene's [MinDistance](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_MinDistance) when the emitter was made).

```csharp
public float MinDistance { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SoundEmitter_Pan"></a> Pan

The stereo position the last step gave it: 0 (left) to 1 (right).

```csharp
public float Pan { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SoundEmitter_Pitch"></a> Pitch

Its pitch: the playback speed factor, 1 for the sound's own (the scene's Doppler shift, if any, multiplies it). A value that is not a number counts as 1.

```csharp
public float Pitch { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SoundEmitter_PlayedPitch"></a> PlayedPitch

The pitch the last step gave it, Doppler shift included.

```csharp
public float PlayedPitch { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SoundEmitter_Position"></a> Position

Where it is in the world, in world units. Set it to move an emitter that follows no entity; one that follows an entity reads the entity's place every step.

```csharp
public Vector2 Position { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Scenes_SoundEmitter_Priority"></a> Priority

The voice policy's rank of its sound, read when its sound starts (0 by default).

```csharp
public int Priority { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_SoundEmitter_Rolloff"></a> Rolloff

The shape of the fall between the two distances.

```csharp
public SoundRolloff Rolloff { get; set; }
```

#### Property Value

 [SoundRolloff](MicroEngine.Scenes.SoundRolloff.md)

### <a id="MicroEngine_Scenes_SoundEmitter_Sound"></a> Sound

The registered sound it plays, or null when it plays a clip.

```csharp
public string? Sound { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Scenes_SoundEmitter_Volume"></a> Volume

Its volume at [MinDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MinDistance) or closer, 0 to 1 (1 by default). A value that is not a number counts as 1.

```csharp
public float Volume { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Scenes_SoundEmitter_Stop_System_Single_"></a> Stop\(float\)

Stops the sound, fading it out over <code class="paramref">fadeSeconds</code>, and takes the emitter out of the scene.

```csharp
public void Stop(float fadeSeconds = 0.05)
```

#### Parameters

`fadeSeconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
