# Class EcsInterpolation<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

The values a component type had when the step started, kept by the store for a type marked with
[Interpolate``1](MicroEngine.Ecs.EcsComponents.md#MicroEngine_Ecs_EcsComponents_Interpolate__1_MicroEngine_Ecs_Blender___0__), and the way drawing reads a row between that value and the current one
([Interpolated``1](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Interpolated__1)). The game copies nothing.

```csharp
public sealed class EcsInterpolation<T> where T : struct
```

#### Type Parameters

`T` 

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EcsInterpolation<T\>](MicroEngine.Ecs.EcsInterpolation\-1.md)

## Remarks

A row that did not have the component when the step started (a new row, a component added during the step, a row
that took a destroyed row's place) is read where it is, like a new entity of the scene's world. So is every row
while the scene is not stepped (under an overlay, in a transition), because the store's blend is then 1.

## Methods

### <a id="MicroEngine_Ecs_EcsInterpolation_1_Blended_Arch_Core_Entity__0__"></a> Blended\(Entity, in T\)

The component of <code class="paramref">row</code> as this frame draws it: between its value when the step started and
<code class="paramref">now</code>, at the store's [Blend](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Blend). For a walk that already has the current
value in hand.

```csharp
public T Blended(Entity row, in T now)
```

#### Parameters

`row` Entity

`now` T

#### Returns

 T

### <a id="MicroEngine_Ecs_EcsInterpolation_1_Blended_Arch_Core_Entity_"></a> Blended\(Entity\)

The component of <code class="paramref">row</code> as this frame draws it, reading its current value from the store.

```csharp
public T Blended(Entity row)
```

#### Parameters

`row` Entity

#### Returns

 T

</div>
