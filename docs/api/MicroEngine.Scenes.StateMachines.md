# Class StateMachines

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Runs state machines for a scene ([Machines](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Machines)): each machine added is stepped every update before
the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_), with the same step, in the order they were added, and is part of the
scene's state hash. It suits the machines of the scene itself (the flow of a level, a boss fight's phases, a
cutscene). A machine that belongs to an object is better stepped by that object, where its place among the object's
rules is explicit; such a machine needs no runner, only a call to its <code>Update</code>.

```csharp
public sealed class StateMachines : SceneSystem, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystem](MicroEngine.Scenes.SceneSystem.md) ← 
[StateMachines](MicroEngine.Scenes.StateMachines.md)

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
var flow = Machines.Add(new StateMachine<Phase>(Phase.Intro));
flow.State(Phase.Intro).After(2f, Phase.Playing);
flow.State(Phase.Playing).When(() => _player.Dead, Phase.GameOver);
```

## Constructors

### <a id="MicroEngine_Scenes_StateMachines__ctor"></a> StateMachines\(\)

```csharp
public StateMachines()
```

## Properties

### <a id="MicroEngine_Scenes_StateMachines_Count"></a> Count

How many machines the scene runs.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_StateMachines_Add__1___0_"></a> Add<T\>\(T\)

Adds <code class="paramref">machine</code> and returns it. Added during a step, it runs from the next one.

```csharp
public T Add<T>(T machine) where T : class, IStateMachine
```

#### Parameters

`machine` T

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The machine was already added.

### <a id="MicroEngine_Scenes_StateMachines_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the system's simulation state to the scene's state hash, after the scene's coroutines, tweens, objects and
widgets and before the scene's own [HashState](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_HashState_MicroEngine_Kernel_StateHasher_). The engine adds the system's type name
first. The default adds nothing.

```csharp
public override void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Scenes_StateMachines_Remove_MicroEngine_IStateMachine_"></a> Remove\(IStateMachine\)

Stops running <code class="paramref">machine</code>; false when the scene did not run it.

```csharp
public bool Remove(IStateMachine machine)
```

#### Parameters

`machine` [IStateMachine](MicroEngine.IStateMachine.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_StateMachines_Stop"></a> Stop\(\)

Called once when the system is removed or its scene leaves the stack, after the scene's
[Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit): release what the system owns. [Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene) is still readable.

```csharp
protected override void Stop()
```

### <a id="MicroEngine_Scenes_StateMachines_Update_System_Single_"></a> Update\(float\)

Advances the system by one fixed step, after the scene's coroutines, tweens and widgets and before the scene's
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_). Not called while the scene is not updated (under a blocking overlay, during
the covering half of a transition).

```csharp
protected override void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The step the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) receives: the step on the world's clock once the scene has objects, the engine's step otherwise.

</div>
