# Enum EcsLink

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

Which side of a link is moved by the game, and so which side follows ([SyncTransform``1](MicroEngine.Ecs.EcsComponents.md#MicroEngine_Ecs_EcsComponents_SyncTransform__1_MicroEngine_Ecs_WriteTransform___0__MicroEngine_Ecs_ReadTransform___0__)).

```csharp
public enum EcsLink : byte
```

## Fields

`EntityMovesRow = 1` 

The entity is moved by something else (a physics body, a tween, the scene); when a step starts its transform is read into the row.



`RowMovesEntity = 0` 

The row is moved by the store's systems; when a step ends its place is written to the entity's transform.



`Unsynced = 2` 

Nothing is copied: the two only find each other and are destroyed together.

</div>
