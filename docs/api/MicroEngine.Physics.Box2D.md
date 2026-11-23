# Namespace MicroEngine.Physics.Box2D

<div data-pagefind-weight="0.3">

The Box2D backend of the physics contract (optional package): dynamic bodies, joints, shape queries with contact detail, tile levels as chains, and access to Box2D itself for what the contract does not cover.

Guide: [Choosing a backend](../manual/physics.md#choosing-a-backend)  

### Classes

 [Box2DNative](MicroEngine.Physics.Box2D.Box2DNative.md)

The way out of the physics contract: the Box2D ids behind a [Box2DPhysicsWorld2D](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md), for a game that
calls Box2D's own C API for something the contract does not cover (an explosion, a conveyor's surface speed, a
motor joint that drives a velocity, wind). Asked for with [NativeAccess](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_NativeAccess) and read
from [NativeIds](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_NativeIds).

 [Box2DPhysicsWorld2D](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md)

The Box2D v3 backend of [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md): native Box2D (the release its native layer pins) behind the
engine's flat C layer, so no Box2D type reaches the game. It passes the common contract and declares
[Declared](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Declared): dynamic bodies, rotation, circles, capsules, convex polygons, continuous collision,
friction and restitution, mass properties, one-way platforms, joints, shape queries, contact detail, filter
predicates and tile geometry with slopes (a level is the outline of its tiles, as Box2D chains). It does not declare
[MovingSolids](MicroEngine.Physics.PhysicsCapabilities.md). [NativeIds](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_NativeIds) is the way out to Box2D's own API.

 [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md)

How a [Box2DPhysicsWorld2D](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md) maps the game's world units onto Box2D, which works in metres with y up.
The defaults are Box2D's own: one world unit is a metre and y grows upward. A pixel-art game that keeps its pixel,
y-down space (the engine's 2D space) sets [UnitsPerMeter](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_UnitsPerMeter) (for example 32) and [YDown](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_YDown); the
world then takes and returns pixels and every conversion happens inside it.

### Structs

 [B2BodyId](MicroEngine.Physics.Box2D.B2BodyId.md)

Box2D's <code>b2BodyId</code>, laid out as Box2D declares it.

 [B2JointId](MicroEngine.Physics.Box2D.B2JointId.md)

Box2D's <code>b2JointId</code>, laid out as Box2D declares it.

 [B2ShapeId](MicroEngine.Physics.Box2D.B2ShapeId.md)

Box2D's <code>b2ShapeId</code>, laid out as Box2D declares it.

 [B2WorldId](MicroEngine.Physics.Box2D.B2WorldId.md)

Box2D's <code>b2WorldId</code>, laid out as Box2D declares it, to pass to Box2D's own functions.

</div>
