# Namespace MicroEngine.Ecs

<div data-pagefind-weight="0.3">

An Entity Component System for a scene (optional package): a component store that the scene creates, steps, hashes and destroys, ordered systems, deferred changes, links between rows and scene entities, and blended drawing. Built on Arch.

Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

### Classes

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

The component types a store's rows are made of, and what the store does with each: whether it is simulation state
(part of the state hash), whether drawing blends it between two steps, and whether it is the place of a row linked
to a scene entity. Build one for the game, usually in a static field, and give it to every store
([AddEcs](MicroEngine.Ecs.EcsExtensions.md#MicroEngine_Ecs_EcsExtensions_AddEcs_MicroEngine_Scenes_Scene_MicroEngine_Ecs_EcsComponents_)).

 [EcsExtensions](MicroEngine.Ecs.EcsExtensions.md)

The store from a scene, and for every scene of a game.

 [EcsInterpolation<T\>](MicroEngine.Ecs.EcsInterpolation\-1.md)

The values a component type had when the step started, kept by the store for a type marked with
[Interpolate``1](MicroEngine.Ecs.EcsComponents.md#MicroEngine_Ecs_EcsComponents_Interpolate__1_MicroEngine_Ecs_Blender___0__), and the way drawing reads a row between that value and the current one
([Interpolated``1](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Interpolated__1)). The game copies nothing.

 [EcsRow](MicroEngine.Ecs.EcsRow.md)

The attachment of a scene entity that is linked to a row ([Link](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Link_Arch_Core_Entity_MicroEngine_Scenes_EntityId_MicroEngine_Ecs_EcsLink_)): the way from the entity to
its row. The world tells it when the entity is released, and the row goes with it.

 [EcsStore](MicroEngine.Ecs.EcsStore.md)

A component store that lives in a scene: the rows of an Entity Component System (the Arch library's
[World](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_World)) created when the scene starts and destroyed when it leaves, with what joins them to the
engine. The store is a scene system, so the scene steps it, hashes it and stops it; nothing in the scene's
<code>Update</code>, <code>HashState</code> or <code>Exit</code> mentions it. Add one with
[AddEcs](MicroEngine.Ecs.EcsExtensions.md#MicroEngine_Ecs_EcsExtensions_AddEcs_MicroEngine_Scenes_Scene_MicroEngine_Ecs_EcsComponents_).

 [EcsSystem](MicroEngine.Ecs.EcsSystem.md)

One behaviour of a store: a class with the queries it walks and an [Update](MicroEngine.Ecs.EcsSystem.md#MicroEngine_Ecs_EcsSystem_Update_System_Single_) the store calls once per
step, in the order the systems were added ([Systems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Systems) before the scene's update,
[LateSystems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_LateSystems) after it). What it asks of [Commands](MicroEngine.Ecs.EcsSystem.md#MicroEngine_Ecs_EcsSystem_Commands) is applied when its update
returns, before the next system runs.

 [EcsSystems](MicroEngine.Ecs.EcsSystems.md)

The systems of one place in a store's step, in the order they run ([Systems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Systems),
[LateSystems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_LateSystems)). The order is the order of the <code>Add</code> calls: it is written once, where the
scene is set up, and read there.

### Structs

 [SceneLink](MicroEngine.Ecs.SceneLink.md)

The component of a row that is linked to an entity of the scene's world ([Link](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Link_Arch_Core_Entity_MicroEngine_Scenes_EntityId_MicroEngine_Ecs_EcsLink_)). A query can
ask for it to walk the linked rows. The store adds and removes it; a game reads it.

### Enums

 [EcsLink](MicroEngine.Ecs.EcsLink.md)

Which side of a link is moved by the game, and so which side follows ([SyncTransform``1](MicroEngine.Ecs.EcsComponents.md#MicroEngine_Ecs_EcsComponents_SyncTransform__1_MicroEngine_Ecs_WriteTransform___0__MicroEngine_Ecs_ReadTransform___0__)).

### Delegates

 [Blender<T\>](MicroEngine.Ecs.Blender\-1.md)

Blends a component between its value when the step started and its value now, for drawing.

 [ComponentHash<T\>](MicroEngine.Ecs.ComponentHash\-1.md)

Adds a component's simulation state to a hash, for a component type that does not implement [IStateHashed](MicroEngine.Kernel.IStateHashed.md).

 [ReadTransform<T\>](MicroEngine.Ecs.ReadTransform\-1.md)

Reads the transform of a scene entity into the component of the row linked to it.

 [WriteTransform<T\>](MicroEngine.Ecs.WriteTransform\-1.md)

Writes a row's component to the transform of the scene entity it is linked to.

</div>
