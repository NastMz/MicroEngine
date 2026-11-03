# Interface IDestroyedCallback

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

An attachment told when its entity is released (at the end of the step its entity was destroyed in).

```csharp
public interface IDestroyedCallback
```

## Methods

### <a id="MicroEngine_Scenes_IDestroyedCallback_OnDestroyed_MicroEngine_Scenes_EntityId_"></a> OnDestroyed\(EntityId\)

Called once; the entity's transform and attachments are still readable, and it may destroy other entities.

```csharp
void OnDestroyed(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

</div>
