# Namespace MicroEngine.Physics

<div data-pagefind-weight="0.3">

The 2D physics contract: worlds of bodies owned by entities, box shapes with collision layers, collide-and-slide moves with their contacts, ray and overlap queries, sensors with buffered events, declared capabilities and the typed capability error, and the module that steps worlds and writes body poses to transforms.

Guide: [Physics worlds](../manual/world.md#physics-worlds)  

### Namespaces

 [MicroEngine.Physics.Tiles](MicroEngine.Physics.Tiles.md)

### Classes

 [CollisionLayers](MicroEngine.Physics.CollisionLayers.md)

The collision layers of the contract: 64 bits, any meaning the game gives them. A shape is in the layers of its
[Category](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Category) and collides with the layers of its [Mask](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Mask); two
shapes collide only when each one's mask holds the other's category.

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

A request needed a physics capability the backend does not declare ([Capabilities](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Capabilities)). It
is thrown before anything changes: the world is as it was.

 [PhysicsContract](MicroEngine.Physics.PhysicsContract.md)

The version of the 2D physics contract ([IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)); a change to its rules raises it.

 [PhysicsModule](MicroEngine.Physics.PhysicsModule.md)

The physics module: registers a [PhysicsSystem](MicroEngine.Physics.PhysicsSystem.md) service and steps it in [Physics](MicroEngine.Kernel.FramePhase.md)
(system [SystemName](MicroEngine.Physics.PhysicsModule.md#MicroEngine_Physics_PhysicsModule_SystemName)), once per fixed step, after the simulation's [FixedUpdate](MicroEngine.Kernel.FramePhase.md).

 [PhysicsSync](MicroEngine.Physics.PhysicsSync.md)

Copies body poses to the transforms of the entities that own them.

 [PhysicsSystem](MicroEngine.Physics.PhysicsSystem.md)

Steps physics worlds, each optionally bound to the scene world whose entities own its bodies. A bound physics world
steps only in the engine steps its scene world stepped in, by that world's scaled step ([Delta](MicroEngine.Scenes.WorldClock.md#MicroEngine_Scenes_WorldClock_Delta)),
so a paused, frozen or covered scene's physics stands still too; after the step, the pose of every kinematic and
dynamic body is written to its owner's transform ([WriteTransforms](MicroEngine.Physics.PhysicsSync.md#MicroEngine_Physics_PhysicsSync_WriteTransforms_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_IWorld_)), which the scene world
interpolates for drawing. An unbound world steps by the engine's fixed step.

### Structs

 [BodyDescription](MicroEngine.Physics.BodyDescription.md)

What [CreateBody](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_CreateBody_MicroEngine_Scenes_EntityId_MicroEngine_Physics_BodyDescription__) creates.

 [BodyId](MicroEngine.Physics.BodyId.md)

A body of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md): a slot index and the generation of the body that held the slot when the
id was made. A destroyed body's slot is reused with a new generation, so an old id never reaches the new body.
<code>default</code> is [None](MicroEngine.Physics.BodyId.md#MicroEngine_Physics_BodyId_None) (generation 0 is never issued).

 [ContactEvent](MicroEngine.Physics.ContactEvent.md)

Two shapes began or stopped touching during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_); only pairs with at least
one dynamic body report them (a kinematic body's contacts come from [MoveResult](MicroEngine.Physics.MoveResult.md)).

 [MoveContactBuffer](MicroEngine.Physics.MoveContactBuffer.md)

The contacts of one [MoveResult](MicroEngine.Physics.MoveResult.md), stored inline (no allocation).

 [MoveContact](MicroEngine.Physics.MoveContact.md)

A surface a moving body touched ([Contacts](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_Contacts)).

 [MoveResult](MicroEngine.Physics.MoveResult.md)

What [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) did: where the body ended, how far it went, and every surface it was stopped
by or touches at the end (within the backend's [Skin](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Skin)), each one with its normal. A
kinematic body's contacts and its ground come from here, never from [ContactEvents](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ContactEvents).

 [QueryFilter](MicroEngine.Physics.QueryFilter.md)

Which shapes a query ([RayCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__), [Overlap](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__)) or a
[Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) considers. A shape matches when its category is in the filter's
[Mask](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Mask) and the filter's [Category](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Category) is in the shape's mask. <code>default</code> is
[Default](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Default): category [Default](MicroEngine.Physics.CollisionLayers.md#MicroEngine_Physics_CollisionLayers_Default), every layer, sensors left out.

 [RayHit](MicroEngine.Physics.RayHit.md)

The closest shape a ray hit ([RayCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__)).

 [SensorEvent](MicroEngine.Physics.SensorEvent.md)

A shape entered or left a sensor during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_). The ids and owners are copied
when the event is made, so an exit caused by destroying a shape still names it.

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

What [AddShape](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_AddShape_MicroEngine_Physics_BodyId_MicroEngine_Physics_ShapeDescription__) attaches to a body. Build one with [Box](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Box_System_Numerics_Vector2_System_Numerics_Vector2_) (common) or a capability's factory.

 [ShapeId](MicroEngine.Physics.ShapeId.md)

A shape of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md) (a box, a tile grid, or a shape of a declared capability), attached to one
body. Generation-checked like [BodyId](MicroEngine.Physics.BodyId.md); <code>default</code> is [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None).

### Interfaces

 [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

A 2D physics world: bodies owned by entities, shapes with collision layers, kinematic collide-and-slide moves, ray
and overlap queries, sensors, and a step. The members below are the common contract every backend passes (one
conformance suite); everything else is a declared capability ([Capabilities](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Capabilities)), and a request that needs
an undeclared one throws [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md) without changing the world.

### Enums

 [BodyKind](MicroEngine.Physics.BodyKind.md)

How a body moves.

 [ContactEventKind](MicroEngine.Physics.ContactEventKind.md)

Whether a contact event is a begin or an end.

 [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

What a physics backend offers beyond the common contract of [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md). Every backend passes the
common conformance suite; each flag it declares has a suite of its own that runs only on backends declaring it. A
request that needs an undeclared flag fails with [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md): a backend never emulates
a capability silently.

 [SensorEventKind](MicroEngine.Physics.SensorEventKind.md)

Whether a sensor event is an entry or an exit.

 [ShapeKind](MicroEngine.Physics.ShapeKind.md)

The geometry of a shape.

</div>
