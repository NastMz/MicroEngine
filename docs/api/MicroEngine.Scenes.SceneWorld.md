# Class SceneWorld

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

The engine's [IWorld](MicroEngine.Scenes.IWorld.md): entity slots with generations, local and step-start transforms, a parent/child
tree, and one dense array per attachment type. Games program against [IWorld](MicroEngine.Scenes.IWorld.md); whoever owns the world
(its scene) calls [BeginStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginStep_System_Int64_System_Single_) before the world's step, [EndStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_EndStep) after it and
[BeginDraw](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginDraw_System_Int64_System_Single_) before drawing it.

```csharp
public sealed class SceneWorld : IWorld, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneWorld](MicroEngine.Scenes.SceneWorld.md)

#### Implements

[IWorld](MicroEngine.Scenes.IWorld.md), 
[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Remarks

Not thread-safe. Allocates only when it grows (entities, attachment types, per-type capacity). Its simulation state
(ids, hierarchy, transforms, which attachments each entity has, the state of attachments that are
[IStateHashed](MicroEngine.Kernel.IStateHashed.md), and the clock) takes part in the per-step state hash; step-start transforms, teleports and
the blend are drawing state and do not.

## Constructors

### <a id="MicroEngine_Scenes_SceneWorld__ctor"></a> SceneWorld\(\)

```csharp
public SceneWorld()
```

## Properties

### <a id="MicroEngine_Scenes_SceneWorld_Blend"></a> Blend

The blend drawing uses this frame: the engine's, or 1 when this world did not step in the latest engine step.

```csharp
public float Blend { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneWorld_Clock"></a> Clock

The world's clock: scaled time, time scale and freezes (hitstop).

```csharp
public WorldClock Clock { get; }
```

#### Property Value

 [WorldClock](MicroEngine.Scenes.WorldClock.md)

### <a id="MicroEngine_Scenes_SceneWorld_Count"></a> Count

Live entities (destroyed ones not counted).

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_SceneWorld_SteppedAt"></a> SteppedAt

The engine step this world last stepped in (-1: never).

```csharp
public long SteppedAt { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Scenes_SceneWorld_All__1"></a> All<T\>\(\)

The attachments of type <code class="typeparamref">T</code> of live entities, in attach order; <code>foreach</code> allocates nothing.

```csharp
public Attachments<T> All<T>() where T : class
```

#### Returns

 [Attachments](MicroEngine.Scenes.Attachments\-1.md)<T\>

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_SceneWorld_Attach__1_MicroEngine_Scenes_EntityId___0_"></a> Attach<T\>\(EntityId, T\)

Attaches <code class="paramref">attachment</code> under the type <code class="typeparamref">T</code>; replaces one already there, in its place.

```csharp
public T Attach<T>(EntityId entity, T attachment) where T : class
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

`attachment` T

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The entity is stale or destroyed.

### <a id="MicroEngine_Scenes_SceneWorld_BeginDraw_System_Int64_System_Single_"></a> BeginDraw\(long, float\)

Sets [Blend](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_Blend) for this frame: <code class="paramref">engineBlend</code> when this world stepped in
<code class="paramref">latestEngineStep</code>, otherwise 1 (the world is frozen: drawn as its last step).

```csharp
public void BeginDraw(long latestEngineStep, float engineBlend)
```

#### Parameters

`latestEngineStep` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`engineBlend` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneWorld_BeginStep_System_Int64_System_Single_"></a> BeginStep\(long, float\)

Starts a step of this world inside engine step <code class="paramref">engineStep</code> (the engine's update count): releases
what is still pending, keeps every transform as the step's start (the blend origin), ends every teleport and
advances [Clock](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_Clock) by <code class="paramref">dt</code>.

```csharp
public float BeginStep(long engineStep, float dt)
```

#### Parameters

`engineStep` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

The scaled length of the step: <code class="paramref">dt</code> exactly at scale 1, 0 while frozen.

### <a id="MicroEngine_Scenes_SceneWorld_Children_MicroEngine_Scenes_EntityId_"></a> Children\(EntityId\)

The live direct children of <code class="paramref">parent</code>, in the order they were given that parent; <code>foreach</code>
allocates nothing. A child created during the walk is visited by the next one; re-parenting during it throws.

```csharp
public EntityChildren Children(EntityId parent)
```

#### Parameters

`parent` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [EntityChildren](MicroEngine.Scenes.EntityChildren.md)

### <a id="MicroEngine_Scenes_SceneWorld_Create_MicroEngine_Scenes_EntityId_"></a> Create\(EntityId\)

Creates an entity at [Identity](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Identity), under <code class="paramref">parent</code> when given.

```csharp
public EntityId Create(EntityId parent = default)
```

#### Parameters

`parent` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [EntityId](MicroEngine.Scenes.EntityId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

<code class="paramref">parent</code> is stale or destroyed.

### <a id="MicroEngine_Scenes_SceneWorld_Destroy_MicroEngine_Scenes_EntityId_"></a> Destroy\(EntityId\)

Destroys the entity and its subtree (see the rules); nothing for a destroyed, stale or none id.

```csharp
public void Destroy(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_SceneWorld_Detach__1_MicroEngine_Scenes_EntityId_"></a> Detach<T\>\(EntityId\)

Removes the attachment of type <code class="typeparamref">T</code>; false when there was none.

```csharp
public bool Detach<T>(EntityId entity) where T : class
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_SceneWorld_DrawPosition2D_MicroEngine_Scenes_EntityId_"></a> DrawPosition2D\(EntityId\)

The world X/Y drawn this frame (at [Blend](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Blend)); no matrix work for a root or under translation-only parents.

```csharp
public Vector2 DrawPosition2D(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Scenes_SceneWorld_EndStep"></a> EndStep\(\)

Ends the step: tells [IDestroyedCallback](MicroEngine.Scenes.IDestroyedCallback.md) attachments and releases destroyed entities.

```csharp
public void EndStep()
```

### <a id="MicroEngine_Scenes_SceneWorld_Get__1_MicroEngine_Scenes_EntityId_"></a> Get<T\>\(EntityId\)

The attachment of type <code class="typeparamref">T</code>.

```csharp
public T Get<T>(EntityId entity) where T : class
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

There is none, or the id is stale.

### <a id="MicroEngine_Scenes_SceneWorld_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the world's simulation state: the clock, then each entity in creation order (id, state, parent, local
transform, and each attachment's type in the order the world first stored that type, followed by the
attachment's own state when it is [IStateHashed](MicroEngine.Kernel.IStateHashed.md)). Drawing never changes it.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Scenes_SceneWorld_IsAlive_MicroEngine_Scenes_EntityId_"></a> IsAlive\(EntityId\)

Whether <code class="paramref">entity</code> exists and was not destroyed.

```csharp
public bool IsAlive(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SceneWorld_Local_MicroEngine_Scenes_EntityId_"></a> Local\(EntityId\)

The transform relative to the parent, by reference (write through it).

```csharp
public ref Transform Local(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [Transform](MicroEngine.Scenes.Transform.md)

### <a id="MicroEngine_Scenes_SceneWorld_ParentOf_MicroEngine_Scenes_EntityId_"></a> ParentOf\(EntityId\)

The parent, or [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None) for a root.

```csharp
public EntityId ParentOf(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_SceneWorld_SetParent_MicroEngine_Scenes_EntityId_MicroEngine_Scenes_EntityId_"></a> SetParent\(EntityId, EntityId\)

Moves the entity under <code class="paramref">parent</code> ([None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None): to the root), keeping its local
transform; it is drawn at its current transform until the next step starts.

```csharp
public void SetParent(EntityId entity, EntityId parent)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

`parent` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The move would make the entity its own ancestor, or an id is stale or destroyed.

### <a id="MicroEngine_Scenes_SceneWorld_Teleport_MicroEngine_Scenes_EntityId_"></a> Teleport\(EntityId\)

Draws the entity (and so its children) at its current transform until the next step starts.

```csharp
public void Teleport(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_SceneWorld_TryGet__1_MicroEngine_Scenes_EntityId___0__"></a> TryGet<T\>\(EntityId, out T?\)

The attachment of type <code class="typeparamref">T</code>, if any (also of an entity destroyed this step).

```csharp
public bool TryGet<T>(EntityId entity, out T? attachment) where T : class
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

`attachment` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_SceneWorld_WorldMatrix_MicroEngine_Scenes_EntityId_"></a> WorldMatrix\(EntityId\)

The world matrix drawn this frame (at [Blend](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Blend)).

```csharp
public Matrix4x4 WorldMatrix(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [Matrix4x4](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

</div>
