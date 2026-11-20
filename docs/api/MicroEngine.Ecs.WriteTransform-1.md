# Delegate WriteTransform<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

Writes a row's component to the transform of the scene entity it is linked to.

```csharp
public delegate void WriteTransform<T>(in T component, ref Transform local) where T : struct
```

#### Parameters

`component` T

`local` [Transform](MicroEngine.Scenes.Transform.md)

#### Type Parameters

`T`

</div>
