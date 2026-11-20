# Delegate ReadTransform<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

Reads the transform of a scene entity into the component of the row linked to it.

```csharp
public delegate void ReadTransform<T>(in Transform local, ref T component) where T : struct
```

#### Parameters

`local` [Transform](MicroEngine.Scenes.Transform.md)

`component` T

#### Type Parameters

`T`

</div>
