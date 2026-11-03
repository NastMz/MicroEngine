# Struct EntityChildren

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

The live direct children of an entity, in parenting order ([Children](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Children_MicroEngine_Scenes_EntityId_)).

```csharp
public readonly struct EntityChildren : IEnumerable<EntityId>, IEnumerable
```

#### Implements

[IEnumerable<EntityId\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

## Methods

### <a id="MicroEngine_Scenes_EntityChildren_GetEnumerator"></a> GetEnumerator\(\)

Starts a walk over the children the entity has now.

```csharp
public EntityChildren.Enumerator GetEnumerator()
```

#### Returns

 [EntityChildren](MicroEngine.Scenes.EntityChildren.md).[Enumerator](MicroEngine.Scenes.EntityChildren.Enumerator.md)

</div>
