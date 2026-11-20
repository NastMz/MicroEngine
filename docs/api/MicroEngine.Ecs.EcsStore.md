# Class EcsStore

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

A component store that lives in a scene: the rows of an Entity Component System (the Arch library's
[World](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_World)) created when the scene starts and destroyed when it leaves, with what joins them to the
engine. The store is a scene system, so the scene steps it, hashes it and stops it; nothing in the scene's
<code>Update</code>, <code>HashState</code> or <code>Exit</code> mentions it. Add one with
[AddEcs](MicroEngine.Ecs.EcsExtensions.md#MicroEngine_Ecs_EcsExtensions_AddEcs_MicroEngine_Scenes_Scene_MicroEngine_Ecs_EcsComponents_).

```csharp
public sealed class EcsStore : SceneSystem, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystem](MicroEngine.Scenes.SceneSystem.md) ← 
[EcsStore](MicroEngine.Ecs.EcsStore.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Inherited Members

[SceneSystem.HashState\(StateHasher\)](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_HashState_MicroEngine_Kernel_StateHasher_), 
[SceneSystem.Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene), 
[SceneSystem.Game](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Game), 
[SceneSystem.IsAttached](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_IsAttached), 
[SceneSystem.Order](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Order), 
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
    var ecs = this.AddEcs(Game1.Components);
    ecs.Systems.Add(new Movement());
    ecs.Systems.Add(new Combat());
    ecs.LateSystems.Add(new Deaths());
    ecs.World.Create(new Position { Value = start }, new Velocity { Value = speed });
}
```

## Remarks

<p>The library is not hidden: [World](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_World) is Arch's world, and rows, queries and components are used
exactly as its documentation says. What the store owns is the integration:</p>
<ul><li><b>The step.</b> Before the scene's <code>Update</code>: linked rows that follow their entity read its transform,
the components marked for it are kept for drawing, then [Systems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Systems) run in order. After the scene's
<code>Update</code>: [LateSystems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_LateSystems) run in order, links whose row or entity is gone are settled, and linked
rows write their place to their entity.</li><li><b>Deferred changes.</b> [Commands](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Commands) are applied after each system and after the scene's
<code>Update</code> ([Flush](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Flush) applies them at once), so a walk never adds, removes or destroys under itself.</li><li><b>Links</b> between a row and an entity of the scene's world, found from either side and destroyed together
([Link](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Link_Arch_Core_Entity_MicroEngine_Scenes_EntityId_MicroEngine_Ecs_EcsLink_)).</li><li><b>Drawing</b> between two steps ([Interpolated``1](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Interpolated__1), [Blend](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Blend)).</li><li><b>The state hash</b>: the components added as state or as tags, the links and the systems' switches.</li><li><b>Registration</b>: a row with a component that is not in the store's [Components](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Components) fails the step.</li></ul>

## Constructors

### <a id="MicroEngine_Ecs_EcsStore__ctor_MicroEngine_Ecs_EcsComponents_"></a> EcsStore\(EcsComponents\)

Makes a store for rows of <code class="paramref">components</code>. It has no rows until its scene is on the stack.

```csharp
public EcsStore(EcsComponents components)
```

#### Parameters

`components` [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

## Properties

### <a id="MicroEngine_Ecs_EcsStore_Blend"></a> Blend

How far the frame being drawn lies between the store's last two steps (0 to 1): the frame's blend while the
scene is stepped, 1 while it is frozen (under an overlay, in a transition) and before the first frame.

```csharp
public float Blend { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Ecs_EcsStore_Commands"></a> Commands

Changes to apply when no walk is running: the library's command buffer. Ask it to create, destroy, add and
remove from inside a query; the store applies everything asked when the system that asked returns, and after
the scene's <code>Update</code>.

```csharp
public CommandBuffer Commands { get; }
```

#### Property Value

 CommandBuffer

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The store's scene is not on the stack.

### <a id="MicroEngine_Ecs_EcsStore_Components"></a> Components

The component types of the store's rows.

```csharp
public EcsComponents Components { get; }
```

#### Property Value

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

### <a id="MicroEngine_Ecs_EcsStore_IsRunning"></a> IsRunning

Whether the store has its rows: from the moment its scene is on the stack until the scene leaves.

```csharp
public bool IsRunning { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Ecs_EcsStore_LateSystems"></a> LateSystems

The systems that run after the scene's <code>Update</code>, in the order they were added: what reacts to the step (deaths, scores, what the camera follows).

```csharp
public EcsSystems LateSystems { get; }
```

#### Property Value

 [EcsSystems](MicroEngine.Ecs.EcsSystems.md)

### <a id="MicroEngine_Ecs_EcsStore_LinkCount"></a> LinkCount

How many rows are linked to an entity of the scene's world.

```csharp
public int LinkCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Ecs_EcsStore_Systems"></a> Systems

The systems that run before the scene's <code>Update</code>, in the order they were added.

```csharp
public EcsSystems Systems { get; }
```

#### Property Value

 [EcsSystems](MicroEngine.Ecs.EcsSystems.md)

### <a id="MicroEngine_Ecs_EcsStore_World"></a> World

The rows: the library's world, used as the library documents.

```csharp
public World World { get; }
```

#### Property Value

 World

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The store's scene is not on the stack (before it is pushed, after it left).

## Methods

### <a id="MicroEngine_Ecs_EcsStore_BeginDraw_System_Single_"></a> BeginDraw\(float\)

Called once per drawn frame before the scene's [Draw](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_), for a system that prepares what
is drawn (positions blended between two steps). It must not change simulation state: a run that draws no frame
never calls it.

```csharp
protected override void BeginDraw(float blend)
```

#### Parameters

`blend` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far the frame lies between the last two steps (0 to 1); 1 when the scene was not updated in the latest step.

### <a id="MicroEngine_Ecs_EcsStore_Blended__1_Arch_Core_Entity_"></a> Blended<T\>\(Entity\)

The component of <code class="paramref">row</code> as this frame draws it. For one row; a walk uses [Interpolated``1](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Interpolated__1).

```csharp
public T Blended<T>(Entity row) where T : struct
```

#### Parameters

`row` Entity

#### Returns

 T

#### Type Parameters

`T` 

### <a id="MicroEngine_Ecs_EcsStore_Flush"></a> Flush\(\)

Applies what was asked of [Commands](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Commands) now. The store calls it after each system; a system calls it
between two of its own walks when the second must see what the first asked for. Never call it inside a query.

```csharp
public void Flush()
```

### <a id="MicroEngine_Ecs_EcsStore_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the store's simulation state: how many rows it has, each component type added with <code>AddState</code> or
<code>AddTag</code> (by row, whatever order the library keeps them in), the links, and each system's name, switch
and own state. Nothing drawing writes is in it.

```csharp
public override void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Ecs_EcsStore_Interpolated__1"></a> Interpolated<T\>\(\)

The blended reader of a component type marked with [Interpolate``1](MicroEngine.Ecs.EcsComponents.md#MicroEngine_Ecs_EcsComponents_Interpolate__1_MicroEngine_Ecs_Blender___0__). Keep it in a field: a walk that draws many rows asks it for each.

```csharp
public EcsInterpolation<T> Interpolated<T>() where T : struct
```

#### Returns

 [EcsInterpolation](MicroEngine.Ecs.EcsInterpolation\-1.md)<T\>

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The type is not marked.

### <a id="MicroEngine_Ecs_EcsStore_LateUpdate_System_Single_"></a> LateUpdate\(float\)

Called after the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) in the same step, before the scene's world releases
what was destroyed: the place to apply what the step decided (deferred changes, positions written back).

```csharp
protected override void LateUpdate(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The same step [Update](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Update_System_Single_) received.

### <a id="MicroEngine_Ecs_EcsStore_Link_Arch_Core_Entity_MicroEngine_Scenes_EntityId_MicroEngine_Ecs_EcsLink_"></a> Link\(Entity, EntityId, EcsLink\)

Links <code class="paramref">row</code> to <code class="paramref">entity</code> of the scene's world, for an object that needs both:
components for the store's systems, and a transform with children, a physics body or a place in a spatial
index. Each is found from the other ([TryGetEntity](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_TryGetEntity_Arch_Core_Entity_MicroEngine_Scenes_EntityId__), [TryGetRow](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_TryGetRow_MicroEngine_Scenes_EntityId_Arch_Core_Entity__)), and destroying
either destroys both when the step ends. Most rows need no entity and are never linked.

```csharp
public void Link(Entity row, EntityId entity, EcsLink mode = EcsLink.RowMovesEntity)
```

#### Parameters

`row` Entity

A live row without a link.

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

A live entity without a link.

`mode` [EcsLink](MicroEngine.Ecs.EcsLink.md)

Which side follows the other's place ([SyncTransform``1](MicroEngine.Ecs.EcsComponents.md#MicroEngine_Ecs_EcsComponents_SyncTransform__1_MicroEngine_Ecs_WriteTransform___0__MicroEngine_Ecs_ReadTransform___0__)).

#### Remarks

Linking adds a [SceneLink](MicroEngine.Ecs.SceneLink.md) component to the row, so not inside a query.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The row or the entity is gone, or one of them is already linked.

### <a id="MicroEngine_Ecs_EcsStore_Start"></a> Start\(\)

Called once, when the scene is on the stack and has its game: before the scene's [Enter](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Enter)
for a system added earlier, at once for one added from [Enter](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Enter) on.

```csharp
protected override void Start()
```

### <a id="MicroEngine_Ecs_EcsStore_Stop"></a> Stop\(\)

Called once when the system is removed or its scene leaves the stack, after the scene's
[Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit): release what the system owns. [Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene) is still readable.

```csharp
protected override void Stop()
```

### <a id="MicroEngine_Ecs_EcsStore_TryGetEntity_Arch_Core_Entity_MicroEngine_Scenes_EntityId__"></a> TryGetEntity\(Entity, out EntityId\)

The entity <code class="paramref">row</code> is linked to; false for a row that is gone (even when a newer row took its place), not linked, or whose entity was destroyed.

```csharp
public bool TryGetEntity(Entity row, out EntityId entity)
```

#### Parameters

`row` Entity

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Ecs_EcsStore_TryGetRow_MicroEngine_Scenes_EntityId_Arch_Core_Entity__"></a> TryGetRow\(EntityId, out Entity\)

The row <code class="paramref">entity</code> is linked to; false for an entity that is gone or destroyed, not linked, or whose row is gone.

```csharp
public bool TryGetRow(EntityId entity, out Entity row)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

`row` Entity

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Ecs_EcsStore_Unlink_Arch_Core_Entity_"></a> Unlink\(Entity\)

Ends the link of <code class="paramref">row</code>, destroying neither side; false when the row is gone or not linked.

```csharp
public bool Unlink(Entity row)
```

#### Parameters

`row` Entity

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Ecs_EcsStore_Update_System_Single_"></a> Update\(float\)

Advances the system by one fixed step, after the scene's coroutines, tweens and widgets and before the scene's
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_). Not called while the scene is not updated (under a blocking overlay, during
the covering half of a transition).

```csharp
protected override void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The step the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) receives: the step on the world's clock once the scene has objects, the engine's step otherwise.

</div>
