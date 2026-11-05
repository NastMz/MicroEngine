# Class PhysicsSync

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

Copies body poses to the transforms of the entities that own them.

```csharp
public static class PhysicsSync
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PhysicsSync](MicroEngine.Physics.PhysicsSync.md)

## Methods

### <a id="MicroEngine_Physics_PhysicsSync_Teleport_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_IWorld_MicroEngine_Physics_BodyId_System_Numerics_Vector2_"></a> Teleport\(IPhysicsWorld2D, IWorld, BodyId, Vector2\)

Teleports <code class="paramref">body</code> to <code class="paramref">position</code> and its owner with it: the body is placed without
collision, the owner's transform is written and it is drawn there at once ([Teleport](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Teleport_MicroEngine_Scenes_EntityId_)).

```csharp
public static void Teleport(IPhysicsWorld2D physics, IWorld world, BodyId body, Vector2 position)
```

#### Parameters

`physics` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`world` [IWorld](MicroEngine.Scenes.IWorld.md)

`body` [BodyId](MicroEngine.Physics.BodyId.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_PhysicsSync_WriteTransforms_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_IWorld_"></a> WriteTransforms\(IPhysicsWorld2D, IWorld\)

Writes the origin (and, on backends with [Rotation](MicroEngine.Physics.PhysicsCapabilities.md), the angle) of every
kinematic and dynamic body of <code class="paramref">physics</code> to its owner's transform in <code class="paramref">world</code>
([Position2D](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Position2D), [Rotation2D](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Rotation2D)). Bodies whose owner is not alive are
skipped; static bodies are never written.

```csharp
public static void WriteTransforms(IPhysicsWorld2D physics, IWorld world)
```

#### Parameters

`physics` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`world` [IWorld](MicroEngine.Scenes.IWorld.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

An owner has a parent: body poses are world positions, so owners must be root entities.

</div>
