# Enum SoundRolloff

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

How a sound's volume falls between an emitter's [MinDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MinDistance) and its [MaxDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MaxDistance).

```csharp
public enum SoundRolloff
```

## Fields

`Inverse = 1` 

Falls fast near the source and slowly far from it, as sound does in the open (volume in proportion to one over the distance), brought to silence at the maximum distance.



`Linear = 0` 

A straight line from full volume to silence: predictable, and what a small level wants.



`Smooth = 2` 

An S-shaped curve: nearly full volume for a while, a fall in the middle, a gentle arrival at silence.

</div>
