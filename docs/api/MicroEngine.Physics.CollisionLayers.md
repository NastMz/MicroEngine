# Class CollisionLayers

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

The collision layers of the contract: 64 bits, any meaning the game gives them. A shape is in the layers of its
[Category](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Category) and collides with the layers of its [Mask](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Mask); two
shapes collide only when each one's mask holds the other's category.

```csharp
public static class CollisionLayers
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CollisionLayers](MicroEngine.Physics.CollisionLayers.md)

## Fields

### <a id="MicroEngine_Physics_CollisionLayers_All"></a> All

Every layer.

```csharp
public const ulong All = 18446744073709551615
```

#### Field Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Physics_CollisionLayers_Default"></a> Default

The layer shapes and queries are in by default (bit 0).

```csharp
public const ulong Default = 1
```

#### Field Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

</div>
