# Enum ShapeKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

The geometry of a shape.

```csharp
public enum ShapeKind
```

## Fields

`Box = 0` 

An axis-aligned box: the common shape (and the common character shape [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) moves).



`Capsule = 2` 

A vertical capsule; needs [Capsules](MicroEngine.Physics.PhysicsCapabilities.md).



`Circle = 1` 

A circle; needs [Circles](MicroEngine.Physics.PhysicsCapabilities.md).



`Polygon = 3` 

A convex polygon; needs [Polygons](MicroEngine.Physics.PhysicsCapabilities.md).

</div>
