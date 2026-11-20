# Class ScenePhysics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A scene's physics world ([Physics](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Physics)): a scene system that steps the world after the scene's
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_), by the same step (the step on the scene world's clock, so slow motion slows physics and
a frozen or covered scene's physics stands still), writes the pose of every moving body to its owner's transform,
adds the world to the scene's state hash and disposes it when the scene leaves the stack.

```csharp
public sealed class ScenePhysics : SceneSystem, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystem](MicroEngine.Scenes.SceneSystem.md) ← 
[ScenePhysics](MicroEngine.Scenes.ScenePhysics.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Inherited Members

[SceneSystem.HashState\(StateHasher\)](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_HashState\_MicroEngine\_Kernel\_StateHasher\_), 
[SceneSystem.Scene](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_Scene), 
[SceneSystem.Game](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_Game), 
[SceneSystem.IsAttached](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_IsAttached), 
[SceneSystem.Order](MicroEngine.Scenes.SceneSystem.md\#MicroEngine\_Scenes\_SceneSystem\_Order), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Examples

```csharp
public override void Enter()
{
    var level = Physics.World.CreateBody(default, BodyDescription.Static());
    Physics.World.AddTiles(level, grid, grid.Layer.Width, grid.Layer.Height);
    _hero = Physics.Track(CharacterController2D.Create(Physics.World, Objects.Create(), start, new Vector2(12, 14)));
}

public override void Update(float dt) =>
    _hero.Step(dt, new CharacterInput(Input.Axis("left", "right"), Input.Pressed("jump"), Input.Down("jump")));
```

## Remarks

<p>The backend is whatever [PhysicsBackend](MicroEngine.Core.Game.md#MicroEngine_Core_Game_PhysicsBackend) makes, the tile backend by default: a game
that wants another one names it there, once, and its scenes do not change. A scene that needs its own world adds
the system itself before reading [Physics](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Physics):
<code>Systems.Add(new ScenePhysics(world))</code>.</p>
<p>Game code moves kinematic bodies, steps character controllers and applies forces in the scene's
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_); the world then steps, so its events are ready for the next update.</p>

## Constructors

### <a id="MicroEngine_Scenes_ScenePhysics__ctor"></a> ScenePhysics\(\)

A scene's physics on the tile backend.

```csharp
public ScenePhysics()
```

### <a id="MicroEngine_Scenes_ScenePhysics__ctor_MicroEngine_Physics_IPhysicsWorld2D_"></a> ScenePhysics\(IPhysicsWorld2D\)

A scene's physics on <code class="paramref">world</code>, which the scene owns from now on: it is disposed with the scene.

```csharp
public ScenePhysics(IPhysicsWorld2D world)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

## Properties

### <a id="MicroEngine_Scenes_ScenePhysics_SyncTransforms"></a> SyncTransforms

Whether body poses are written to their owners' transforms after each step (true by default; only when the
scene has [Objects](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Objects)). A scene whose bodies are not owned by entities turns it off.

```csharp
public bool SyncTransforms { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_ScenePhysics_World"></a> World

The physics world.

```csharp
public IPhysicsWorld2D World { get; }
```

#### Property Value

 [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

## Methods

### <a id="MicroEngine_Scenes_ScenePhysics_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the world's state (when the backend hashes it) and every tracked state, in the order they were tracked.

```csharp
public override void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Scenes_ScenePhysics_LateUpdate_System_Single_"></a> LateUpdate\(float\)

Called after the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) in the same step, before the scene's world releases
what was destroyed: the place to apply what the step decided (deferred changes, positions written back).

```csharp
protected override void LateUpdate(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The same step [Update](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Update_System_Single_) received.

### <a id="MicroEngine_Scenes_ScenePhysics_Stop"></a> Stop\(\)

Called once when the system is removed or its scene leaves the stack, after the scene's
[Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit): release what the system owns. [Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene) is still readable.

```csharp
protected override void Stop()
```

### <a id="MicroEngine_Scenes_ScenePhysics_Track__1___0_"></a> Track<T\>\(T\)

Adds <code class="paramref">state</code> (a character controller, anything whose state belongs with the physics) to what
the scene hashes after the world, and returns it.

```csharp
public T Track<T>(T state) where T : class, IStateHashed
```

#### Parameters

`state` T

#### Returns

 T

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_ScenePhysics_Untrack_MicroEngine_Kernel_IStateHashed_"></a> Untrack\(IStateHashed\)

Stops hashing <code class="paramref">state</code>; false when it was not tracked.

```csharp
public bool Untrack(IStateHashed state)
```

#### Parameters

`state` [IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
