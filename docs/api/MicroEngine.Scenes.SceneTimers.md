# Class SceneTimers

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Calls actions after a time or at intervals, on the scene's time ([Timers](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Timers)): the scene's own
[Timers](MicroEngine.Timers.md), stepped for it. Timers advance before the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) by the step it
receives, so they slow down and freeze with the world's clock; an unscaled timer advances by
[EngineStep](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_EngineStep) instead. They stop with the scene, and their state is part of its state hash. For a
sequence of steps over time use a coroutine ([Coroutines](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Coroutines)); for the rules of a game kept outside
a scene, create a [Timers](MicroEngine.Timers.md) and step it yourself.

```csharp
public sealed class SceneTimers : SceneSystem, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystem](MicroEngine.Scenes.SceneSystem.md) ← 
[SceneTimers](MicroEngine.Scenes.SceneTimers.md)

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
Timers.After(2f, SpawnBoss);
var drip = Timers.Every(0.5f, SpawnDrop);
// later
Timers.Cancel(drip);
```

## Constructors

### <a id="MicroEngine_Scenes_SceneTimers__ctor"></a> SceneTimers\(\)

```csharp
public SceneTimers()
```

## Properties

### <a id="MicroEngine_Scenes_SceneTimers_Count"></a> Count

How many timers are waiting.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_SceneTimers_After_System_Single_System_Action_System_Boolean_"></a> After\(float, Action, bool\)

Calls <code class="paramref">action</code> once, <code class="paramref">seconds</code> from now, on the update on which the time reaches them ([Timers](MicroEngine.Timers.md) gives the rule).

```csharp
public TimerHandle After(float seconds, Action action, bool unscaled = false)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

The delay; 0 calls it in the scene's next step.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count the engine's step, so the timer runs through slow motion and a frozen world.

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">seconds</code> is negative or not a number.

### <a id="MicroEngine_Scenes_SceneTimers_AfterSteps_System_Int32_System_Action_System_Boolean_"></a> AfterSteps\(int, Action, bool\)

Calls <code class="paramref">action</code> once, on the <code class="paramref">steps</code>-th step of the scene from now; a step of zero seconds (the world frozen) is not counted.

```csharp
public TimerHandle AfterSteps(int steps, Action action, bool unscaled = false)
```

#### Parameters

`steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The delay in steps; 0 and 1 both call it in the scene's next step.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count every step of the engine, so the timer runs through a frozen world.

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">steps</code> is negative.

### <a id="MicroEngine_Scenes_SceneTimers_Cancel_MicroEngine_TimerHandle_"></a> Cancel\(TimerHandle\)

Cancels a timer; false when it already ended or was cancelled. A timer may cancel itself from its action.

```csharp
public bool Cancel(TimerHandle handle)
```

#### Parameters

`handle` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SceneTimers_Clear"></a> Clear\(\)

Cancels every timer.

```csharp
public void Clear()
```

### <a id="MicroEngine_Scenes_SceneTimers_Every_System_Single_System_Action_System_Int32_System_Boolean_"></a> Every\(float, Action, int, bool\)

Calls <code class="paramref">action</code> every <code class="paramref">seconds</code>, the first time one interval from now.

```csharp
public TimerHandle Every(float seconds, Action action, int times = 0, bool unscaled = false)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

The interval, above zero. A step longer than the interval calls the action once per interval that passed.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`times` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many calls before the timer ends; 0 (the default) repeats until it is cancelled.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count the engine's step, so the timer runs through slow motion and a frozen world.

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">seconds</code> is not above zero, or <code class="paramref">times</code> is negative.

### <a id="MicroEngine_Scenes_SceneTimers_EverySteps_System_Int32_System_Action_System_Int32_System_Boolean_"></a> EverySteps\(int, Action, int, bool\)

Calls <code class="paramref">action</code> every <code class="paramref">steps</code> steps of the scene, the first time that many steps from now.

```csharp
public TimerHandle EverySteps(int steps, Action action, int times = 0, bool unscaled = false)
```

#### Parameters

`steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The interval in steps, above zero.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`times` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many calls before the timer ends; 0 (the default) repeats until it is cancelled.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count every step of the engine, so the timer runs through a frozen world.

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">steps</code> is not above zero, or <code class="paramref">times</code> is negative.

### <a id="MicroEngine_Scenes_SceneTimers_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the system's simulation state to the scene's state hash, after the scene's coroutines, tweens, objects and
widgets and before the scene's own [HashState](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_HashState_MicroEngine_Kernel_StateHasher_). The engine adds the system's type name
first. The default adds nothing.

```csharp
public override void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Scenes_SceneTimers_IsActive_MicroEngine_TimerHandle_"></a> IsActive\(TimerHandle\)

Whether <code class="paramref">handle</code> names a timer that is still waiting.

```csharp
public bool IsActive(TimerHandle handle)
```

#### Parameters

`handle` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SceneTimers_Remaining_MicroEngine_TimerHandle_"></a> Remaining\(TimerHandle\)

What is left before the next call of a timer, in seconds or in steps as it was added; 0 when it ended or was cancelled.

```csharp
public float Remaining(TimerHandle handle)
```

#### Parameters

`handle` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SceneTimers_Stop"></a> Stop\(\)

Called once when the system is removed or its scene leaves the stack, after the scene's
[Exit](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Exit): release what the system owns. [Scene](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Scene) is still readable.

```csharp
protected override void Stop()
```

### <a id="MicroEngine_Scenes_SceneTimers_Update_System_Single_"></a> Update\(float\)

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
