# Interface IWorld

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

The objects of a scene: entities with an id, a transform (with an optional parent) and typed attachments. The world
runs no game logic: it stores, identifies, orders, interpolates and destroys. Game code programs against this
interface; the scene that owns the world drives its step and draw hooks ([SceneWorld](MicroEngine.Scenes.SceneWorld.md)).

```csharp
public interface IWorld
```

## Remarks

The rules of the contract:

<ol><li><b>Ids</b> are generation-checked; a released slot is reused with a new generation. For a stale id
[IsAlive](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_IsAlive_MicroEngine_Scenes_EntityId_) and [TryGet``1](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_TryGet__1_MicroEngine_Scenes_EntityId___0__) are false; [Local](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Local_MicroEngine_Scenes_EntityId_), [Get``1](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Get__1_MicroEngine_Scenes_EntityId_) and
[ParentOf](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_ParentOf_MicroEngine_Scenes_EntityId_) throw `InvalidOperationException` naming the id and why.</li><li><b>Destroy</b> takes effect at once for queries ([IsAlive](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_IsAlive_MicroEngine_Scenes_EntityId_) false, [Count](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Count) drops, every
iteration skips it and its subtree). Its transform and attachments stay readable through its id until the world ends
the step; then [IDestroyedCallback](MicroEngine.Scenes.IDestroyedCallback.md) attachments are told in destroy order and the slot is released, never
during an iteration. Destroying a destroyed, stale or [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None) id does nothing.</li><li><b>Creation</b> takes effect at once: the id is valid and its transform writable. An iteration already running
does not visit it; every later one does. There is no spawn queue.</li><li><b>Iteration</b>: [All``1](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_All__1) walks the attachments of one type of live entities in
attach order, stable under removal. Attachments are keyed by the type argument, not the runtime type. <code>foreach</code>
allocates nothing.</li><li><b>Interpolation</b>: drawing reads [WorldMatrix](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_WorldMatrix_MicroEngine_Scenes_EntityId_) or [DrawPosition2D](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_DrawPosition2D_MicroEngine_Scenes_EntityId_) at the world's
[Blend](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Blend), from each entity's transform at the start of the step to its current one; at blend 1, or when
nothing moved, the result is exactly the simulated transform. A child's matrix is its interpolated local matrix
times its parent's.</li><li><b>Teleport</b>: a new, teleported or re-parented entity is drawn at its current transform until the next step
starts; a teleported parent carries its children.</li><li><b>Step tracking</b>: [Blend](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Blend) is the engine's blend when this world stepped in the engine's latest
fixed step, otherwise 1 (paused, under an overlay, in a transition), so a frozen world is drawn as its last step.</li><li><b>Clock</b>: [Clock](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Clock) advances once per step of this world; at scale 1 the scaled step is the engine's
step bit for bit.</li></ol>

## Properties

### <a id="MicroEngine_Scenes_IWorld_Blend"></a> Blend

The blend drawing uses this frame: the engine's, or 1 when this world did not step in the latest engine step.

```csharp
float Blend { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_IWorld_Clock"></a> Clock

The world's clock: scaled time, time scale and freezes (hitstop).

```csharp
WorldClock Clock { get; }
```

#### Property Value

 [WorldClock](MicroEngine.Scenes.WorldClock.md)

### <a id="MicroEngine_Scenes_IWorld_Count"></a> Count

Live entities (destroyed ones not counted).

```csharp
int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_IWorld_All__1"></a> All<T\>\(\)

The attachments of type <code class="typeparamref">T</code> of live entities, in attach order; <code>foreach</code> allocates nothing.

```csharp
Attachments<T> All<T>() where T : class
```

#### Returns

 [Attachments](MicroEngine.Scenes.Attachments\-1.md)<T\>

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_IWorld_Attach__1_MicroEngine_Scenes_EntityId___0_"></a> Attach<T\>\(EntityId, T\)

Attaches <code class="paramref">attachment</code> under the type <code class="typeparamref">T</code>; replaces one already there, in its place.

```csharp
T Attach<T>(EntityId entity, T attachment) where T : class
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

### <a id="MicroEngine_Scenes_IWorld_Children_MicroEngine_Scenes_EntityId_"></a> Children\(EntityId\)

The live direct children of <code class="paramref">parent</code>, in the order they were given that parent; <code>foreach</code>
allocates nothing. A child created during the walk is visited by the next one; re-parenting during it throws.

```csharp
EntityChildren Children(EntityId parent)
```

#### Parameters

`parent` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [EntityChildren](MicroEngine.Scenes.EntityChildren.md)

### <a id="MicroEngine_Scenes_IWorld_Create_MicroEngine_Scenes_EntityId_"></a> Create\(EntityId\)

Creates an entity at [Identity](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Identity), under <code class="paramref">parent</code> when given.

```csharp
EntityId Create(EntityId parent = default)
```

#### Parameters

`parent` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [EntityId](MicroEngine.Scenes.EntityId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

<code class="paramref">parent</code> is stale or destroyed.

### <a id="MicroEngine_Scenes_IWorld_Destroy_MicroEngine_Scenes_EntityId_"></a> Destroy\(EntityId\)

Destroys the entity and its subtree (see the rules); nothing for a destroyed, stale or none id.

```csharp
void Destroy(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_IWorld_Detach__1_MicroEngine_Scenes_EntityId_"></a> Detach<T\>\(EntityId\)

Removes the attachment of type <code class="typeparamref">T</code>; false when there was none.

```csharp
bool Detach<T>(EntityId entity) where T : class
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_IWorld_DrawPosition2D_MicroEngine_Scenes_EntityId_"></a> DrawPosition2D\(EntityId\)

The world X/Y drawn this frame (at [Blend](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Blend)); no matrix work for a root or under translation-only parents.

```csharp
Vector2 DrawPosition2D(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Scenes_IWorld_Get__1_MicroEngine_Scenes_EntityId_"></a> Get<T\>\(EntityId\)

The attachment of type <code class="typeparamref">T</code>.

```csharp
T Get<T>(EntityId entity) where T : class
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

### <a id="MicroEngine_Scenes_IWorld_IsAlive_MicroEngine_Scenes_EntityId_"></a> IsAlive\(EntityId\)

Whether <code class="paramref">entity</code> exists and was not destroyed.

```csharp
bool IsAlive(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_IWorld_Local_MicroEngine_Scenes_EntityId_"></a> Local\(EntityId\)

The transform relative to the parent, by reference (write through it).

```csharp
ref Transform Local(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [Transform](MicroEngine.Scenes.Transform.md)

### <a id="MicroEngine_Scenes_IWorld_ParentOf_MicroEngine_Scenes_EntityId_"></a> ParentOf\(EntityId\)

The parent, or [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None) for a root.

```csharp
EntityId ParentOf(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_IWorld_SetParent_MicroEngine_Scenes_EntityId_MicroEngine_Scenes_EntityId_"></a> SetParent\(EntityId, EntityId\)

Moves the entity under <code class="paramref">parent</code> ([None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None): to the root), keeping its local
transform; it is drawn at its current transform until the next step starts.

```csharp
void SetParent(EntityId entity, EntityId parent)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

`parent` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The move would make the entity its own ancestor, or an id is stale or destroyed.

### <a id="MicroEngine_Scenes_IWorld_Teleport_MicroEngine_Scenes_EntityId_"></a> Teleport\(EntityId\)

Draws the entity (and so its children) at its current transform until the next step starts.

```csharp
void Teleport(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Scenes_IWorld_TryGet__1_MicroEngine_Scenes_EntityId___0__"></a> TryGet<T\>\(EntityId, out T?\)

The attachment of type <code class="typeparamref">T</code>, if any (also of an entity destroyed this step).

```csharp
bool TryGet<T>(EntityId entity, out T? attachment) where T : class
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

`attachment` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_IWorld_WorldMatrix_MicroEngine_Scenes_EntityId_"></a> WorldMatrix\(EntityId\)

The world matrix drawn this frame (at [Blend](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Blend)).

```csharp
Matrix4x4 WorldMatrix(EntityId entity)
```

#### Parameters

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [Matrix4x4](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

</div>
