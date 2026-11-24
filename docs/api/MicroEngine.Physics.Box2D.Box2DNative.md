# Class Box2DNative

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics.Box2D](MicroEngine.Physics.Box2D.md)  
Assembly: MicroEngine.Physics.Box2D.dll  
Guide: [Choosing a backend](../manual/physics.md#choosing-a-backend)  

The way out of the physics contract: the Box2D ids behind a [Box2DPhysicsWorld2D](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md), for a game that
calls Box2D's own C API for something the contract does not cover (an explosion, a conveyor's surface speed, a
motor joint that drives a velocity, wind). Asked for with [NativeAccess](MicroEngine.Physics.Box2D.Box2DWorldOptions.md#MicroEngine_Physics_Box2D_Box2DWorldOptions_NativeAccess) and read
from [NativeIds](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_NativeIds).

```csharp
public sealed class Box2DNative
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Box2DNative](MicroEngine.Physics.Box2D.Box2DNative.md)

## Remarks

<p>The game declares the functions it calls, against [Library](MicroEngine.Physics.Box2D.Box2DNative.md#MicroEngine_Physics_Box2D_Box2DNative_Library), with these id types:</p>

```csharp
[DllImport(Box2DNative.Library)] static extern void b2Body_SetBullet(B2BodyId body, [MarshalAs(UnmanagedType.U1)] bool flag);
b2Body_SetBullet(world.NativeIds.Body(crate), true);
```

<p>What that costs:</p>
<ul><li>Units are Box2D's: metres, radians, y up, whatever [Box2DWorldOptions](MicroEngine.Physics.Box2D.Box2DWorldOptions.md) maps the game's units to.</li><li>The engine does not know what was changed: it is not in the state hash, not in
[DescriptionOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_DescriptionOf_MicroEngine_Physics_JointId_), and bodies, shapes and joints created through Box2D have no
engine id (queries and events skip them).</li><li>An id lives as long as its engine object: after the engine destroys the object (or the world) the id is dead
and passing it to Box2D is undefined. Destroying an engine object through Box2D is not supported: the engine then
refuses that body or joint with `InvalidOperationException`, and what was attached to it is in an
undefined state. Stepping the world through Box2D is not a step of the engine: [StepCount](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_StepCount)
does not move and the events of those steps are never reported.</li><li>Box2D's API changes between its releases; [Box2DVersion](MicroEngine.Physics.Box2D.Box2DPhysicsWorld2D.md#MicroEngine_Physics_Box2D_Box2DPhysicsWorld2D_Box2DVersion) says which one the
package carries.</li><li>It works where the package ships a shared library that exports Box2D: Windows, Linux, macOS and Android. In
the browser the runtime does not pass these ids by value, and on iOS it is not tested: not supported there.</li><li>No Box2D function may be called while a step runs, and worlds are not thread-safe.</li></ul>

## Fields

### <a id="MicroEngine_Physics_Box2D_Box2DNative_Library"></a> Library

The native library that holds Box2D's functions: the name for a game's own <code>DllImport</code> declarations.

```csharp
public const string Library = "me_box2d"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Physics_Box2D_Box2DNative_World"></a> World

The Box2D world.

```csharp
public B2WorldId World { get; }
```

#### Property Value

 [B2WorldId](MicroEngine.Physics.Box2D.B2WorldId.md)

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

The world was disposed.

## Methods

### <a id="MicroEngine_Physics_Box2D_Box2DNative_Body_MicroEngine_Physics_BodyId_"></a> Body\(BodyId\)

The Box2D body of <code class="paramref">body</code>.

```csharp
public B2BodyId Body(BodyId body)
```

#### Parameters

`body` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [B2BodyId](MicroEngine.Physics.Box2D.B2BodyId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DNative_Joint_MicroEngine_Physics_JointId_"></a> Joint\(JointId\)

The Box2D joint of <code class="paramref">joint</code>.

```csharp
public B2JointId Joint(JointId joint)
```

#### Parameters

`joint` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [B2JointId](MicroEngine.Physics.Box2D.B2JointId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale.

### <a id="MicroEngine_Physics_Box2D_Box2DNative_Shape_MicroEngine_Physics_ShapeId_"></a> Shape\(ShapeId\)

The Box2D shape of <code class="paramref">shape</code>.

```csharp
public B2ShapeId Shape(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [B2ShapeId](MicroEngine.Physics.Box2D.B2ShapeId.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The id is stale, or the shape is tile geometry, which is many Box2D shapes.

</div>
