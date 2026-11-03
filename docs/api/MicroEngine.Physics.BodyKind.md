# Enum BodyKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

How a body moves.

```csharp
public enum BodyKind
```

## Fields

`Dynamic = 2` 

Moved by the solver (gravity, velocities, contacts). Needs [DynamicBodies](MicroEngine.Physics.PhysicsCapabilities.md).



`Kinematic = 1` 

Moved only by the game, through [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) (collide-and-slide) or
[SetPosition](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SetPosition_MicroEngine_Physics_BodyId_System_Numerics_Vector2_) (teleport); characters and moving platforms. Common contract.



`Static = 0` 

Never moves by itself (level geometry). Common contract.

</div>
