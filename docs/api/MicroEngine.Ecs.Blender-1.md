# Delegate Blender<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

Blends a component between its value when the step started and its value now, for drawing.

```csharp
public delegate T Blender<T>(in T from, in T to, float blend) where T : struct
```

#### Parameters

`from` T

The value when the step started.

`to` T

The value now.

`blend` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far the frame lies between the two (0 to 1).

#### Returns

 T

#### Type Parameters

`T`

</div>
