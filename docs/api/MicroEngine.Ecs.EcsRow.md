# Class EcsRow

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

The attachment of a scene entity that is linked to a row ([Link](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Link_Arch_Core_Entity_MicroEngine_Scenes_EntityId_MicroEngine_Ecs_EcsLink_)): the way from the entity to
its row. The world tells it when the entity is released, and the row goes with it.

```csharp
public sealed class EcsRow : IDestroyedCallback
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EcsRow](MicroEngine.Ecs.EcsRow.md)

#### Implements

[IDestroyedCallback](MicroEngine.Scenes.IDestroyedCallback.md)

## Properties

### <a id="MicroEngine_Ecs_EcsRow_Row"></a> Row

The row. Ask [TryGetRow](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_TryGetRow_MicroEngine_Scenes_EntityId_Arch_Core_Entity__) rather than trusting it: the row may be gone.

```csharp
public Entity Row { get; }
```

#### Property Value

 Entity

### <a id="MicroEngine_Ecs_EcsRow_Store"></a> Store

The store the row is in.

```csharp
public EcsStore Store { get; }
```

#### Property Value

 [EcsStore](MicroEngine.Ecs.EcsStore.md)

## Methods

### <a id="MicroEngine_Ecs_EcsRow_OnDestroyed_MicroEngine_Scenes_EntityId_"></a> OnDestroyed\(EntityId\)

Called once; the entity's transform and attachments are still readable, and it may destroy other entities.

```csharp
public void OnDestroyed(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

</div>
