# Delegate ComponentHash<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

Adds a component's simulation state to a hash, for a component type that does not implement [IStateHashed](MicroEngine.Kernel.IStateHashed.md).

```csharp
public delegate void ComponentHash<T>(in T component, StateHasher hash) where T : struct
```

#### Parameters

`component` T

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

#### Type Parameters

`T`

</div>
