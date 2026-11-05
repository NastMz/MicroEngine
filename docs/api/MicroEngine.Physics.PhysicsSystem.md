# Class PhysicsSystem

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

Steps physics worlds, each optionally bound to the scene world whose entities own its bodies. A bound physics world
steps only in the engine steps its scene world stepped in, by that world's scaled step ([Delta](MicroEngine.Scenes.WorldClock.md#MicroEngine_Scenes_WorldClock_Delta)),
so a paused, frozen or covered scene's physics stands still too; after the step, the pose of every kinematic and
dynamic body is written to its owner's transform ([WriteTransforms](MicroEngine.Physics.PhysicsSync.md#MicroEngine_Physics_PhysicsSync_WriteTransforms_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_IWorld_)), which the scene world
interpolates for drawing. An unbound world steps by the engine's fixed step.

```csharp
public sealed class PhysicsSystem
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PhysicsSystem](MicroEngine.Physics.PhysicsSystem.md)

## Constructors

### <a id="MicroEngine_Physics_PhysicsSystem__ctor"></a> PhysicsSystem\(\)

```csharp
public PhysicsSystem()
```

## Properties

### <a id="MicroEngine_Physics_PhysicsSystem_Count"></a> Count

How many physics worlds are registered.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Physics_PhysicsSystem_Add_MicroEngine_Physics_IPhysicsWorld2D_MicroEngine_Scenes_SceneWorld_"></a> Add\(IPhysicsWorld2D, SceneWorld?\)

Registers <code class="paramref">physics</code>, bound to <code class="paramref">world</code> when given.

```csharp
public void Add(IPhysicsWorld2D physics, SceneWorld? world = null)
```

#### Parameters

`physics` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`world` [SceneWorld](MicroEngine.Scenes.SceneWorld.md)?

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

It is already registered.

### <a id="MicroEngine_Physics_PhysicsSystem_Remove_MicroEngine_Physics_IPhysicsWorld2D_"></a> Remove\(IPhysicsWorld2D\)

Stops stepping <code class="paramref">physics</code>; false when it was not registered.

```csharp
public bool Remove(IPhysicsWorld2D physics)
```

#### Parameters

`physics` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_PhysicsSystem_Run_MicroEngine_Kernel_FrameContext_"></a> Run\(FrameContext\)

Steps every registered world for one fixed step of <code class="paramref">frame</code> (what [PhysicsModule](MicroEngine.Physics.PhysicsModule.md) schedules).

```csharp
public void Run(FrameContext frame)
```

#### Parameters

`frame` [FrameContext](MicroEngine.Kernel.FrameContext.md)

</div>
