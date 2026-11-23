# Class SceneAudio

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A scene's sounds in space ([Sounds](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Sounds)): a listener and emitters in the scene's world units. After
every [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) each emitter's volume follows its distance to the listener and its pan the side
it is on, moved there over the step so nothing clicks; an emitter that follows an entity goes where the entity
goes and stops when it is destroyed; every emitter stops when the scene leaves.

```csharp
public sealed class SceneAudio : SceneSystem, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystem](MicroEngine.Scenes.SceneSystem.md) ← 
[SceneAudio](MicroEngine.Scenes.SceneAudio.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Inherited Members

[SceneSystem.HashState\(StateHasher\)](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_HashState\_MicroEngine\_Kernel\_StateHasher\_), 
[SceneSystem.Scene](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_Scene), 
[SceneSystem.Game](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_Game), 
[SceneSystem.IsAttached](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_IsAttached), 
[SceneSystem.Order](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_Order), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Examples

```csharp
public override void Enter()
{
    Sounds.Follow(_hero);                               // the listener is the hero
    Sounds.Emit("torch", torch, loop: true);            // this entity makes this sound
}

// later, in Update
Sounds.Emit("explosion", barrelPosition);               // a sound at a place, once
```

## Remarks

<p>The listener is, in this order: the entity given to [Follow](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_Follow_MicroEngine_Scenes_EntityId_), the point set in
[Listener](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_Listener), the centre of the scene's [WorldCamera](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_WorldCamera), or the world's origin.</p>
<p>An emitter out of range holds no voice of the mixer: a level can have hundreds of emitters, and only those
within earshot count against the voice limit. A looping emitter that comes into range starts where it would be had
it played all along.</p>
<p>Emitters are sound, not state: nothing here is part of the scene's state hash, and nothing a game decides
should depend on whether an emitter is audible, which differs between a device that plays and a silent one.</p>

## Constructors

### <a id="MicroEngine_Scenes_SceneAudio__ctor"></a> SceneAudio\(\)

```csharp
public SceneAudio()
```

## Properties

### <a id="MicroEngine_Scenes_SceneAudio_Bus"></a> Bus

The bus of new emitters ("sfx" by default).

```csharp
public string Bus { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Scenes_SceneAudio_Doppler"></a> Doppler

How much of the Doppler shift is applied: 0 (the default) for none, 1 for the shift of [SpeedOfSound](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_SpeedOfSound),
more to exaggerate it. A sound coming closer rises in pitch and one going away falls, by the speed at which its
distance to the listener changes; the shift is kept between half and twice the pitch. Streamed music keeps its
pitch.

```csharp
public float Doppler { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneAudio_Emitters"></a> Emitters

The emitters in the scene, in the order they were made.

```csharp
public IReadOnlyList<SoundEmitter> Emitters { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SoundEmitter](MicroEngine.Scenes.SoundEmitter.md)\>

### <a id="MicroEngine_Scenes_SceneAudio_Listener"></a> Listener

The listener's place in the world. Setting it fixes the listener there (and ends [Follow](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_Follow_MicroEngine_Scenes_EntityId_)); set it
to null to go back to the centre of the scene's camera. Reading it gives where the listener is now.

```csharp
public Vector2? Listener { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

### <a id="MicroEngine_Scenes_SceneAudio_MaxDistance"></a> MaxDistance

The distance from which new emitters are silent (320 by default).

```csharp
public float MaxDistance { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneAudio_MinDistance"></a> MinDistance

The distance within which new emitters play at full volume (16 by default; never negative).

```csharp
public float MinDistance { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneAudio_Order"></a> Order

Runs after the scene's other systems (physics, which moves the entities emitters follow).

```csharp
public override int Order { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_SceneAudio_PanDistance"></a> PanDistance

The horizontal distance from the listener at which a sound is entirely on one side; closer, it is in between.
0 (the default) for each emitter's own [MaxDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MaxDistance). Half the width of the view is a
good value for a game seen from the side: a sound at the edge of the screen then plays from that speaker.

```csharp
public float PanDistance { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneAudio_Rolloff"></a> Rolloff

The fall of new emitters between the two distances ([Linear](MicroEngine.Scenes.SoundRolloff.md) by default).

```csharp
public SoundRolloff Rolloff { get; set; }
```

#### Property Value

 [SoundRolloff](MicroEngine.Scenes.SoundRolloff.md)

### <a id="MicroEngine_Scenes_SceneAudio_SpeedOfSound"></a> SpeedOfSound

The speed of sound for the Doppler shift, in world units per second (3400 by default: 340 m/s at ten units per metre).

```csharp
public float SpeedOfSound { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneAudio_StopFade"></a> StopFade

Seconds over which every emitter fades out when the scene leaves (0.1 by default).

```csharp
public float StopFade { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Scenes_SceneAudio_Emit_System_String_MicroEngine_Scenes_EntityId_System_Boolean_System_Single_"></a> Emit\(string, EntityId, bool, float\)

Makes <code class="paramref">entity</code> the source of the registered sound <code class="paramref">sound</code>: it plays from where
the entity is, follows it, and stops when the entity is destroyed.

```csharp
public SoundEmitter Emit(string sound, EntityId entity, bool loop = false, float volume = 1)
```

#### Parameters

`sound` [string](https://learn.microsoft.com/dotnet/api/system.string)

A sound registered in <code>Audio</code> ([AddSound](MicroEngine.Audio.AudioMixer.md#MicroEngine_Audio_AudioMixer_AddSound_System_String_MicroEngine_Audio_AudioClip_)).

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

An entity of the scene's world ([Objects](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Objects)).

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it repeats until stopped (a torch, an engine) or plays once (a shout).

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

Its volume at close range, 0 to 1.

#### Returns

 [SoundEmitter](MicroEngine.Scenes.SoundEmitter.md)

The emitter, to change or stop it.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sound is registered under that name.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The entity is not alive in the scene's world.

### <a id="MicroEngine_Scenes_SceneAudio_Emit_System_String_System_Numerics_Vector2_System_Boolean_System_Single_"></a> Emit\(string, Vector2, bool, float\)

Plays the registered sound <code class="paramref">sound</code> from a place in the world.

```csharp
public SoundEmitter Emit(string sound, Vector2 position, bool loop = false, float volume = 1)
```

#### Parameters

`sound` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [SoundEmitter](MicroEngine.Scenes.SoundEmitter.md)

The emitter, to move, change or stop it.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sound is registered under that name.

### <a id="MicroEngine_Scenes_SceneAudio_Emit_MicroEngine_Audio_AudioClip_MicroEngine_Scenes_EntityId_System_Boolean_System_Single_"></a> Emit\(AudioClip, EntityId, bool, float\)

Makes <code class="paramref">entity</code> the source of <code class="paramref">clip</code> (a clip held by the game or by an asset scope, not registered under a name).

```csharp
public SoundEmitter Emit(AudioClip clip, EntityId entity, bool loop = false, float volume = 1)
```

#### Parameters

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [SoundEmitter](MicroEngine.Scenes.SoundEmitter.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The entity is not alive in the scene's world.

### <a id="MicroEngine_Scenes_SceneAudio_Emit_MicroEngine_Audio_AudioClip_System_Numerics_Vector2_System_Boolean_System_Single_"></a> Emit\(AudioClip, Vector2, bool, float\)

Plays <code class="paramref">clip</code> from a place in the world.

```csharp
public SoundEmitter Emit(AudioClip clip, Vector2 position, bool loop = false, float volume = 1)
```

#### Parameters

`clip` [AudioClip](MicroEngine.Audio.AudioClip.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`volume` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [SoundEmitter](MicroEngine.Scenes.SoundEmitter.md)

### <a id="MicroEngine_Scenes_SceneAudio_Follow_MicroEngine_Scenes_EntityId_"></a> Follow\(EntityId\)

Makes <code class="paramref">entity</code> the listener from now on: the hero, usually. When it is destroyed the listener stays where it was.

```csharp
public void Follow(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_SceneAudio_LateUpdate_System_Single_"></a> LateUpdate\(float\)

Called after the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) in the same step, before the scene's world releases
what was destroyed: the place to apply what the step decided (deferred changes, positions written back).

```csharp
protected override void LateUpdate(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The same step [Update](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Update_System_Single_) received.

### <a id="MicroEngine_Scenes_SceneAudio_Stop"></a> Stop\(\)

Called once when the system is removed or its scene leaves the stack, after the scene's
[Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit): release what the system owns. [Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene) is still readable.

```csharp
protected override void Stop()
```

### <a id="MicroEngine_Scenes_SceneAudio_StopAll_System_Single_"></a> StopAll\(float\)

Stops every emitter, fading each out over <code class="paramref">fadeSeconds</code>.

```csharp
public void StopAll(float fadeSeconds = 0.05)
```

#### Parameters

`fadeSeconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
