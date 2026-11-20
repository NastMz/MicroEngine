# Class EcsComponents

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

The component types a store's rows are made of, and what the store does with each: whether it is simulation state
(part of the state hash), whether drawing blends it between two steps, and whether it is the place of a row linked
to a scene entity. Build one for the game, usually in a static field, and give it to every store
([AddEcs](MicroEngine.Ecs.EcsExtensions.md#MicroEngine_Ecs_EcsExtensions_AddEcs_MicroEngine_Scenes_Scene_MicroEngine_Ecs_EcsComponents_)).

```csharp
public sealed class EcsComponents
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EcsComponents](MicroEngine.Ecs.EcsComponents.md)

## Examples

```csharp
public static readonly EcsComponents Components = new EcsComponents()
    .AddState<Position>().AddState<Velocity>().AddTag<Frozen>().Add<Sprite>()
    .Interpolate<Position>(static (in Position a, in Position b, float t) => new Position { Value = Vector2.Lerp(a.Value, b.Value, t) })
    .SyncTransform<Position>(static (in Position p, ref Transform local) => local.Position2D = p.Value);
```

## Remarks

Every component type of every row must be added here. A NativeAOT build cannot make the library's arrays for a
type nobody named, so adding a type also names it for the library (<code>ArrayRegistry.Add</code>); and so that a
forgotten type is found long before such a build, a store refuses a row with a component that is not here in every
build, at the end of the system that made it.

## Constructors

### <a id="MicroEngine_Ecs_EcsComponents__ctor"></a> EcsComponents\(\)

```csharp
public EcsComponents()
```

## Properties

### <a id="MicroEngine_Ecs_EcsComponents_Count"></a> Count

How many component types were added.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Ecs_EcsComponents_Add__1"></a> Add<T\>\(\)

Adds a component type that is not simulation state: what drawing needs (a sprite, a colour), a cache, anything
that can be rebuilt. It is left out of the state hash.

```csharp
public EcsComponents Add<T>() where T : struct
```

#### Returns

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The type was already added, or a store already uses these components.

### <a id="MicroEngine_Ecs_EcsComponents_AddState__1"></a> AddState<T\>\(\)

Adds a component type that is simulation state: each row's value is part of the state hash, through the component's own [HashState](MicroEngine.Kernel.IStateHashed.md#MicroEngine_Kernel_IStateHashed_HashState_MicroEngine_Kernel_StateHasher_).

```csharp
public EcsComponents AddState<T>() where T : struct, IStateHashed
```

#### Returns

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The type was already added, or a store already uses these components.

### <a id="MicroEngine_Ecs_EcsComponents_AddState__1_MicroEngine_Ecs_ComponentHash___0__"></a> AddState<T\>\(ComponentHash<T\>\)

Adds a component type that is simulation state, hashed by <code class="paramref">hash</code>: for a type the game cannot make implement [IStateHashed](MicroEngine.Kernel.IStateHashed.md).

```csharp
public EcsComponents AddState<T>(ComponentHash<T> hash) where T : struct
```

#### Parameters

`hash` [ComponentHash](MicroEngine.Ecs.ComponentHash\-1.md)<T\>

#### Returns

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The type was already added, or a store already uses these components.

### <a id="MicroEngine_Ecs_EcsComponents_AddTag__1"></a> AddTag<T\>\(\)

Adds a component type without data whose presence on a row is simulation state (frozen, shielded, selected): which rows have it is part of the state hash.

```csharp
public EcsComponents AddTag<T>() where T : struct
```

#### Returns

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The type was already added, or a store already uses these components.

### <a id="MicroEngine_Ecs_EcsComponents_Contains_System_Type_"></a> Contains\(Type\)

Whether <code class="paramref">type</code> was added.

```csharp
public bool Contains(Type type)
```

#### Parameters

`type` [Type](https://learn.microsoft.com/dotnet/api/system.type)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Ecs_EcsComponents_Interpolate__1_MicroEngine_Ecs_Blender___0__"></a> Interpolate<T\>\(Blender<T\>\)

Marks an added component type for drawing between two steps: each store keeps its value from the start of the
step and [Interpolated``1](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Interpolated__1) reads it blended with <code class="paramref">blend</code>.

```csharp
public EcsComponents Interpolate<T>(Blender<T> blend) where T : struct
```

#### Parameters

`blend` [Blender](MicroEngine.Ecs.Blender\-1.md)<T\>

#### Returns

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The type was not added, or a store already uses these components.

### <a id="MicroEngine_Ecs_EcsComponents_SyncTransform__1_MicroEngine_Ecs_WriteTransform___0__MicroEngine_Ecs_ReadTransform___0__"></a> SyncTransform<T\>\(WriteTransform<T\>, ReadTransform<T\>?\)

Makes an added component type the place of linked rows: when a step ends, <code class="paramref">toEntity</code> writes
the component of each row linked with [RowMovesEntity](MicroEngine.Ecs.EcsLink.md) to its scene entity's transform, so
the scene world's hierarchy, drawing, physics and spatial index see the row where it is. With
<code class="paramref">fromEntity</code>, a row linked with [EntityMovesRow](MicroEngine.Ecs.EcsLink.md) reads its entity's
transform when a step starts instead.

```csharp
public EcsComponents SyncTransform<T>(WriteTransform<T> toEntity, ReadTransform<T>? fromEntity = null) where T : struct
```

#### Parameters

`toEntity` [WriteTransform](MicroEngine.Ecs.WriteTransform\-1.md)<T\>

`fromEntity` [ReadTransform](MicroEngine.Ecs.ReadTransform\-1.md)<T\>?

#### Returns

 [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The type was not added, or a store already uses these components.

</div>
