# Class SceneSystem

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A part of a scene that lives with it: the engine starts it when the scene is on the stack, steps it with the scene
([Update](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Update_System_Single_) before the scene's own update, [LateUpdate](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_LateUpdate_System_Single_) after it), adds its state to the
scene's state hash and stops it when the scene leaves, with no call from the scene's own <code>Update</code>,
<code>HashState</code> or <code>Exit</code>. Add one to [Systems](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Systems), or give every scene one through
[AddSystem``1](MicroEngine.Scenes.SceneManager.md#MicroEngine_Scenes_SceneManager_AddSystem__1_System_Func_MicroEngine_Scenes_Scene___0__). A component store, a physics world, a set of state machines and a
scheduler are scene systems.

```csharp
public abstract class SceneSystem : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystem](MicroEngine.Scenes.SceneSystem.md)

#### Derived

[SceneTimers](MicroEngine.Scenes.SceneTimers.md), 
[StateMachines](MicroEngine.Scenes.StateMachines.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
public sealed class Weather : SceneSystem
{
    public float Rain { get; private set; }
    protected override void Update(float dt) => Rain = MathF.Min(1, Rain + dt * 0.1f);
    public override void HashState(StateHasher hash) => hash.Add(Rain);
}

// in a scene
public override void Enter() => Systems.Add(new Weather());
public override void Draw(IRenderer r) => DrawRain(r, Systems.Get<Weather>().Rain);
```

## Constructors

### <a id="MicroEngine_Scenes_SceneSystem__ctor"></a> SceneSystem\(\)

```csharp
protected SceneSystem()
```

## Properties

### <a id="MicroEngine_Scenes_SceneSystem_Game"></a> Game

The scene's game. Available from [Start](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Start) on.

```csharp
public Game Game { get; }
```

#### Property Value

 [Game](MicroEngine.Core.Game.md)

### <a id="MicroEngine_Scenes_SceneSystem_IsAttached"></a> IsAttached

Whether the system is in a scene.

```csharp
public bool IsAttached { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SceneSystem_Order"></a> Order

Where the system runs among the scene's systems: lower first, and systems of the same order in the order they
were added. Stopping runs in the reverse order. Read once, when the system is added. 0 by default.

```csharp
public virtual int Order { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_SceneSystem_Scene"></a> Scene

The scene this system belongs to.

```csharp
public Scene Scene { get; }
```

#### Property Value

 [Scene](MicroEngine.Scenes.Scene.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The system is not in a scene (before it is added, after it was removed).

## Methods

### <a id="MicroEngine_Scenes_SceneSystem_BeginDraw_System_Single_"></a> BeginDraw\(float\)

Called once per drawn frame before the scene's [Draw](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Draw_MicroEngine_Graphics_IRenderer_), for a system that prepares what
is drawn (positions blended between two steps). It must not change simulation state: a run that draws no frame
never calls it.

```csharp
protected virtual void BeginDraw(float blend)
```

#### Parameters

`blend` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far the frame lies between the last two steps (0 to 1); 1 when the scene was not updated in the latest step.

### <a id="MicroEngine_Scenes_SceneSystem_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the system's simulation state to the scene's state hash, after the scene's coroutines, tweens, objects and
widgets and before the scene's own [HashState](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_HashState_MicroEngine_Kernel_StateHasher_). The engine adds the system's type name
first. The default adds nothing.

```csharp
public virtual void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Scenes_SceneSystem_LateUpdate_System_Single_"></a> LateUpdate\(float\)

Called after the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) in the same step, before the scene's world releases
what was destroyed: the place to apply what the step decided (deferred changes, positions written back).

```csharp
protected virtual void LateUpdate(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The same step [Update](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Update_System_Single_) received.

### <a id="MicroEngine_Scenes_SceneSystem_Start"></a> Start\(\)

Called once, when the scene is on the stack and has its game: before the scene's [Enter](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Enter)
for a system added earlier, at once for one added from [Enter](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Enter) on.

```csharp
protected virtual void Start()
```

### <a id="MicroEngine_Scenes_SceneSystem_Stop"></a> Stop\(\)

Called once when the system is removed or its scene leaves the stack, after the scene's
[Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit): release what the system owns. [Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene) is still readable.

```csharp
protected virtual void Stop()
```

### <a id="MicroEngine_Scenes_SceneSystem_Update_System_Single_"></a> Update\(float\)

Advances the system by one fixed step, after the scene's coroutines, tweens and widgets and before the scene's
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_). Not called while the scene is not updated (under a blocking overlay, during
the covering half of a transition).

```csharp
protected virtual void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The step the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) receives: the step on the world's clock once the scene has objects, the engine's step otherwise.

</div>
