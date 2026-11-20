# Struct SceneLink

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

The component of a row that is linked to an entity of the scene's world ([Link](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Link_Arch_Core_Entity_MicroEngine_Scenes_EntityId_MicroEngine_Ecs_EcsLink_)). A query can
ask for it to walk the linked rows. The store adds and removes it; a game reads it.

```csharp
public struct SceneLink
```

## Fields

### <a id="MicroEngine_Ecs_SceneLink_Entity"></a> Entity

The entity of the scene's world.

```csharp
public EntityId Entity
```

#### Field Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Ecs_SceneLink_Mode"></a> Mode

Which side follows the other.

```csharp
public EcsLink Mode
```

#### Field Value

 [EcsLink](MicroEngine.Ecs.EcsLink.md)

</div>
